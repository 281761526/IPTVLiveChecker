using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace IPTVLiveChecker;

public class ChannelPlayer : UserControl, IMessageFilter
{
	private enum PlayerIcon { Play, Pause, Volume, Muted, Fullscreen }

	private const int WM_LBUTTONDOWN = 0x0201;
	private const int WM_LBUTTONDBLCLK = 0x0203;
	private const int WM_NCDESTROY = 0x0082;
	private const int WH_MOUSE_LL = 14;
	private const int HC_ACTION = 0;

	[DllImport("user32.dll")]
	private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
	private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
	private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MSLLHOOKSTRUCT
	{
		public POINT pt;
		public uint mouseData;
		public uint flags;
		public uint time;
		public IntPtr dwExtraInfo;
	}

	private LibVLC _libVLC;

	private MediaPlayer _mediaPlayer;

	private VideoView _videoView;

	private IntPtr _mouseHook = IntPtr.Zero;
	private LowLevelMouseProc _mouseHookProc;
	private DateTime _lastLeftDownTime = DateTime.MinValue;
	private Point _lastLeftDownPos = new Point(int.MinValue, int.MinValue);
	private static readonly object _mouseHookLock = new object();
	private static List<ChannelPlayer> _hookInstances = new List<ChannelPlayer>();

	private Panel _fallbackPanel;

	private Label _fallbackLabel;

	private Button _openExternalBtn;

	private Panel _controlBar;

	private Button _btnPlayPause;

	private Button _btnMute;

	private TrackBar _volumeSlider;

	private TrackBar _progressBar;

	private Button _btnFullscreen;

	private Label _statusLabel;

	private System.Windows.Forms.Timer _progressTimer;

	private bool _isUpdatingProgress;

	private Form _fullscreenForm;

	private bool _isFullscreen;

	private DateTime _lastVideoClickTime = DateTime.MinValue;

	private Point _lastClickScreenPos = new Point(int.MinValue, int.MinValue);

	private DateTime _lastClickDedupeTime = DateTime.MinValue;

	private string _currentUrl;

	private string _currentName;

	private bool _isMuted;

	private float _dpiScale = 1f;

	private int _loadToken;

	// 串行化“停止旧播放→释放旧媒体→播放新媒体”，避免快速换台时 VLC 多个网络输入
	// 互相争用内核、或新任务释放了即将播放的旧媒体导致软件卡死。
	private readonly object _loadLock = new object();

	// 切换频道计数：>0 表示有切台正在进行（含重叠的快速连切）。
	// 切台期间禁止 UI 线程访问 libvlc，避免“后台 Stop 需要 UI 线程拆除视频输出(HWND)”
	// 与“UI 线程查询 libvlc 等待同一把锁”互相死锁 → 软件卡死（即“第二个链接卡死”的根因）。
	private int _switchingCount;

	// 由 Playing/Stopped/Paused 事件维护的播放状态，供 UI 线程的 UpdatePlayPauseIcon 使用，
	// 避免 UI 线程直接读 _mediaPlayer.IsPlaying 而拿到 libvlc 锁导致卡死。
	private volatile bool _lastKnownPlaying;

	public bool IsSwitching => _switchingCount > 0;

	// 等待“停播完成(Stopped)”信号，确保停播与播放串行，避免 WinForms 下 VLC 视频输出(HWND)
	// 拆除与播放争用导致 UI 卡死的经典问题。
	private TaskCompletionSource<bool> _stopTcs;

	private Media _currentMedia;

	private AppTheme _currentTheme;

	public bool IsAvailable { get; private set; }

	public string CurrentUrl => _currentUrl;

	public string CurrentName => _currentName;

	public event EventHandler OpenExternalRequested;

	public ChannelPlayer()
	{
		InitializeComponent();
		using (Graphics g = CreateGraphics())
		{
			_dpiScale = g.DpiX / 96f;
		}
		InitializePlayer();
		Application.AddMessageFilter(this);
	}

	public bool PreFilterMessage(ref Message m)
	{
		const int WM_LBUTTONDOWN = 0x0201;
		const int WM_LBUTTONDBLCLK = 0x0203;
		if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_LBUTTONDBLCLK)
		{
			return false;
		}
		if (_videoView == null || !_videoView.Visible || base.IsDisposed)
		{
			return false;
		}
		try
		{
			Point screenPos = Cursor.Position;
			bool inVideo;
			if (_isFullscreen && _fullscreenForm != null && !_fullscreenForm.IsDisposed)
			{
				Point fp = _fullscreenForm.PointToClient(screenPos);
				inVideo = _fullscreenForm.ClientRectangle.Contains(fp);
			}
			else
			{
				Point vp = _videoView.PointToClient(screenPos);
				inVideo = _videoView.ClientRectangle.Contains(vp);
			}
			DiagLog($"PreFilter msg=0x{m.Msg:X} hwnd=0x{m.HWnd.ToInt64():X} inVideo={inVideo} fs={_isFullscreen}");
			if (!inVideo)
			{
				return false;
			}
			HandleVideoClick(screenPos, fromMessageFilter: true);
		}
		catch (Exception ex)
		{
			DiagLog("PreFilter exception: " + ex.Message);
		}
		return false;
	}

	private static readonly object _diagLock = new object();
	private static string _diagPath;
	private static void DiagLog(string text)
	{
		try
		{
			if (_diagPath == null)
			{
				_diagPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "player_diag.log");
			}
			lock (_diagLock)
			{
				System.IO.File.AppendAllText(_diagPath, $"{DateTime.Now:HH:mm:ss.fff} {text}\r\n");
			}
		}
		catch
		{
		}
	}

	private void InitializeComponent()
	{
		this.Dock = System.Windows.Forms.DockStyle.Fill;
		this._videoView = new LibVLCSharp.WinForms.VideoView
		{
			Dock = System.Windows.Forms.DockStyle.Fill,
			BackColor = System.Drawing.Color.Black
		};
		this._videoView.HandleCreated += delegate
		{
			AttachVideoViewHook();
		};
		this._videoView.Click += delegate
		{
			HandleVideoClick(Cursor.Position, fromMessageFilter: false);
		};
		this._videoView.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				HandleVideoClick(Cursor.Position, fromMessageFilter: false);
			}
		};
		this._fallbackPanel = new System.Windows.Forms.Panel
		{
			Dock = System.Windows.Forms.DockStyle.Fill,
			BackColor = System.Drawing.Color.FromArgb(24, 24, 28)
		};
		this._fallbackLabel = new System.Windows.Forms.Label
		{
			Dock = System.Windows.Forms.DockStyle.Top,
			Height = 56,
			ForeColor = System.Drawing.Color.FromArgb(220, 220, 225),
			BackColor = System.Drawing.Color.Transparent,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Font = new System.Drawing.Font("Segoe UI", 11f),
			Text = "预览播放器不可用"
		};
		this._openExternalBtn = new System.Windows.Forms.Button
		{
			Size = new System.Drawing.Size(160, 34),
			FlatStyle = System.Windows.Forms.FlatStyle.Flat,
			ForeColor = System.Drawing.Color.White,
			BackColor = System.Drawing.Color.FromArgb(0, 122, 255),
			Text = "用默认播放器打开",
			Cursor = System.Windows.Forms.Cursors.Hand,
			AutoSize = true,
			AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink,
			Padding = new System.Windows.Forms.Padding(14, 6, 14, 6),
			UseCompatibleTextRendering = true
		};
		this._openExternalBtn.FlatAppearance.BorderSize = 0;
		this._openExternalBtn.Click += delegate
		{
			this.OnOpenExternalRequested();
		};
		this._fallbackPanel.Controls.Add(this._openExternalBtn);
		this._fallbackPanel.Resize += delegate
		{
			this._openExternalBtn.Location = new System.Drawing.Point((int)((float)(this._fallbackPanel.Width - this._openExternalBtn.Width) / 2f), (int)((float)(this._fallbackPanel.Height - this._openExternalBtn.Height) / 2f) + 20);
		};
		this.BuildControlBar();
		base.Controls.Add(this._videoView);
		base.Controls.Add(this._controlBar);
		base.Controls.Add(this._fallbackPanel);
	}

	/// <summary>底部控制栏高度（含 DPI 缩放），供外部按 16:9 精确计算播放器容器高度。</summary>
	public int ControlBarHeight => (_controlBar != null) ? _controlBar.Height : ((int)(52f * _dpiScale));

	private void BuildControlBar()
	{
		int barH = (int)(52f * _dpiScale);
		int btnSize = (int)(34f * _dpiScale);
		int pad = (int)(8f * _dpiScale);
		int y = (barH - btnSize) / 2;
		_controlBar = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = barH,
			BackColor = Color.FromArgb(18, 18, 22),
			Visible = false
		};
		_btnPlayPause = MakeIconButton(PlayerIcon.Play, btnSize, pad, y);
		_btnPlayPause.Click += delegate
		{
			TogglePlayPause();
		};
		_btnMute = MakeIconButton(PlayerIcon.Volume, btnSize, _btnPlayPause.Right + pad, y);
		_btnMute.Click += delegate
		{
			ToggleMute();
		};
		_volumeSlider = new TrackBar
		{
			Minimum = 0,
			Maximum = 100,
			Value = 100,
			Width = (int)(70f * _dpiScale),
			Height = (int)(24f * _dpiScale),
			TickStyle = TickStyle.None,
			BackColor = Color.FromArgb(18, 18, 22),
			ForeColor = Color.FromArgb(0, 122, 255)
		};
		_volumeSlider.Location = new Point(_btnMute.Right + pad, y + (int)(3f * _dpiScale));
		_volumeSlider.ValueChanged += delegate
		{
			SetVolume(_volumeSlider.Value);
		};
		int progX = _volumeSlider.Right + pad;
		int fsW = btnSize;
		int progW = Math.Max((int)(60f * _dpiScale), _controlBar.Width - progX - pad - fsW - pad - pad);
		_progressBar = new TrackBar
		{
			Minimum = 0,
			Maximum = 1000,
			Value = 0,
			Width = progW,
			Height = (int)(24f * _dpiScale),
			TickStyle = TickStyle.None,
			BackColor = Color.FromArgb(18, 18, 22),
			ForeColor = Color.FromArgb(0, 122, 255)
		};
		_progressBar.Location = new Point(progX, y + (int)(3f * _dpiScale));
		_progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		_progressBar.ValueChanged += OnProgressBarValueChanged;
		_btnFullscreen = MakeIconButton(PlayerIcon.Fullscreen, btnSize, 0, y);
		_btnFullscreen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		_btnFullscreen.Click += delegate
		{
			ToggleFullscreen();
		};
		_statusLabel = new Label
		{
			AutoSize = true,
			ForeColor = Color.FromArgb(170, 170, 178),
			BackColor = Color.Transparent,
			Font = new Font("Segoe UI", 8.5f * _dpiScale),
			Text = "",
			Visible = false
		};
		_controlBar.Controls.Add(_btnPlayPause);
		_controlBar.Controls.Add(_btnMute);
		_controlBar.Controls.Add(_volumeSlider);
		_controlBar.Controls.Add(_progressBar);
		_controlBar.Controls.Add(_btnFullscreen);
		_controlBar.Controls.Add(_statusLabel);
		_controlBar.Layout += delegate
		{
			_btnFullscreen.Location = new Point(_controlBar.Width - _btnFullscreen.Width - pad, y);
		};
		_progressTimer = new System.Windows.Forms.Timer
		{
			Interval = 500
		};
		_progressTimer.Tick += delegate
		{
			UpdateProgress();
		};
	}

	private void StartProgressTimer()
	{
		if (_progressTimer != null && !_progressTimer.Enabled)
		{
			_progressTimer.Start();
			UpdateProgress();
		}
	}

	private void StopProgressTimer()
	{
		if (_progressTimer != null && _progressTimer.Enabled)
		{
			_progressTimer.Stop();
		}
	}

	private void ResetProgress()
	{
		if (_progressBar == null)
		{
			return;
		}
		_isUpdatingProgress = true;
		try
		{
			_progressBar.Value = 0;
		}
		catch
		{
		}
		_isUpdatingProgress = false;
	}

	private void OnProgressBarValueChanged(object sender, EventArgs e)
	{
		if (_isUpdatingProgress || _mediaPlayer == null)
		{
			return;
		}
		try
		{
			long length = _mediaPlayer.Length;
			if (length <= 0)
			{
				return;
			}
			long time = length * _progressBar.Value / 1000L;
			_mediaPlayer.Time = time;
		}
		catch
		{
		}
	}

	private void UpdateProgress()
	{
		if (_mediaPlayer == null || _progressBar == null)
		{
			return;
		}
		try
		{
			long length = _mediaPlayer.Length;
			long time = _mediaPlayer.Time;
			int val;
			if (length > 0)
			{
				val = (int)(time * 1000L / length);
			}
			else
			{
				long cycle = 120000L;
				long mod = ((time % cycle) + cycle) % cycle;
				val = (int)(mod * 1000L / cycle);
			}
			val = Math.Max(0, Math.Min(1000, val));
			_isUpdatingProgress = true;
			if (_progressBar.Value != val)
			{
				_progressBar.Value = val;
			}
			_isUpdatingProgress = false;
		}
		catch
		{
		}
	}

	private Button MakeIconButton(PlayerIcon icon, int size, int x, int y)
	{
		Button button = new Button();
		button.Size = new Size(size, size);
		button.Location = new Point(x, y);
		button.FlatStyle = FlatStyle.Flat;
		button.ForeColor = Color.FromArgb(220, 220, 225);
		button.BackColor = Color.FromArgb(40, 40, 48);
		button.Text = "";
		button.Cursor = Cursors.Hand;
		button.FlatAppearance.BorderSize = 0;
		button.Tag = icon;
		ApplyRoundedRegion(button, 8);
		button.Paint += delegate(object s, PaintEventArgs pe)
		{
			Button b = (Button)s;
			PlayerIcon ic = b.Tag is PlayerIcon pi ? pi : icon;
			Color iconColor = b.ForeColor;
			if (b.ClientRectangle.Contains(b.PointToClient(Cursor.Position)))
			{
				Color hoverColor = (_currentTheme != null && !DrawingUtils.IsDarkColor(_currentTheme.Bg))
					? Color.FromArgb(230, 230, 235)
					: Color.FromArgb(60, 60, 75);
				pe.Graphics.FillRectangle(new SolidBrush(hoverColor), b.ClientRectangle);
			}
			DrawPlayerIcon(pe.Graphics, b.ClientRectangle, ic, iconColor);
		};
		button.MouseEnter += delegate
		{
			button.Invalidate();
		};
		button.MouseLeave += delegate
		{
			button.Invalidate();
		};
		return button;
	}

	private void ApplyRoundedRegion(Control ctrl, int radius)
	{
		using GraphicsPath path = new GraphicsPath();
		Rectangle r = new Rectangle(0, 0, ctrl.Width, ctrl.Height);
		int d = radius * 2;
		if (d >= ctrl.Width || d >= ctrl.Height)
		{
			path.AddEllipse(r);
		}
		else
		{
			path.AddArc(r.X, r.Y, d, d, 180f, 90f);
			path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
			path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
			path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
			path.CloseFigure();
		}
		ctrl.Region = new Region(path);
	}

	private void DrawPlayerIcon(Graphics g, Rectangle rect, PlayerIcon icon, Color color)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		int cx = rect.Width / 2;
		int cy = rect.Height / 2;
		int sz = (int)(rect.Width * 0.4f);
		using Brush brush = new SolidBrush(color);
		using Pen pen = new Pen(color, Math.Max(1.6f, 2f * _dpiScale));
		switch (icon)
		{
		case PlayerIcon.Play:
		{
			PointF p1 = new PointF(cx - sz / 2f, cy - sz / 2f);
			PointF p2 = new PointF(cx - sz / 2f, cy + sz / 2f);
			PointF p3 = new PointF(cx + sz / 2f, cy);
			g.FillPolygon(brush, new PointF[3] { p1, p2, p3 });
			break;
		}
		case PlayerIcon.Pause:
		{
			int bw = Math.Max(3, (int)(4f * _dpiScale));
			int gap = Math.Max(2, (int)(3f * _dpiScale));
			int bh = sz;
			Rectangle r1 = new Rectangle(cx - bw - gap / 2, cy - bh / 2, bw, bh);
			Rectangle r2 = new Rectangle(cx + gap / 2, cy - bh / 2, bw, bh);
			g.FillRectangle(brush, r1);
			g.FillRectangle(brush, r2);
			break;
		}
		case PlayerIcon.Volume:
		{
			int w = Math.Max(4, (int)(6f * _dpiScale));
			int h = (int)(sz * 0.7f);
			Rectangle body = new Rectangle(cx - sz / 3, cy - h / 2, w, h);
			g.FillRectangle(brush, body);
			PointF[] cone = new PointF[3]
			{
				new PointF(body.Right, cy - sz / 2),
				new PointF(body.Right, cy + sz / 2),
				new PointF(cx + sz / 2, cy + sz / 2)
			};
			g.FillPolygon(brush, cone);
			PointF[] wave = new PointF[5]
			{
				new PointF(cx + sz / 4, cy - sz / 4),
				new PointF(cx + sz / 3, cy - sz / 6),
				new PointF(cx + sz / 3, cy + sz / 6),
				new PointF(cx + sz / 4, cy + sz / 4),
				new PointF(cx + sz / 5, cy)
			};
			g.DrawLines(pen, wave);
			break;
		}
		case PlayerIcon.Muted:
		{
			int w = Math.Max(4, (int)(6f * _dpiScale));
			int h = (int)(sz * 0.7f);
			Rectangle body = new Rectangle(cx - sz / 3, cy - h / 2, w, h);
			g.FillRectangle(brush, body);
			PointF[] cone = new PointF[3]
			{
				new PointF(body.Right, cy - sz / 2),
				new PointF(body.Right, cy + sz / 2),
				new PointF(cx + sz / 2, cy + sz / 2)
			};
			g.FillPolygon(brush, cone);
			using Pen xpen = new Pen(color, Math.Max(1.8f, 2.2f * _dpiScale));
			g.DrawLine(xpen, cx - sz / 2, cy - sz / 2, cx + sz / 2, cy + sz / 2);
			break;
		}
		case PlayerIcon.Fullscreen:
		{
			int m = Math.Max(3, (int)(4f * _dpiScale));
			int len = (int)(sz * 0.55f);
			int half = sz / 2;
			Point[] pts = new Point[4]
			{
				new Point(cx - half, cy - half + len),
				new Point(cx - half, cy - half),
				new Point(cx - half + len, cy - half),
				new Point(cx - half, cy - half)
			};
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(cx - half, cy - half + len),
				new PointF(cx - half, cy - half),
				new PointF(cx - half + len, cy - half)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(cx + half - len, cy - half),
				new PointF(cx + half, cy - half),
				new PointF(cx + half, cy - half + len)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(cx - half, cy + half - len),
				new PointF(cx - half, cy + half),
				new PointF(cx - half + len, cy + half)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(cx + half - len, cy + half),
				new PointF(cx + half, cy + half),
				new PointF(cx + half, cy + half - len)
			});
			break;
		}
		}
	}

	private void UpdatePlayPauseIcon()
	{
		if (_btnPlayPause == null)
		{
			return;
		}
		// 不再在 UI 线程直接读 libvlc，改用事件维护的状态，避免拿锁导致卡死
		bool isPlaying = _lastKnownPlaying;
		_btnPlayPause.Tag = (isPlaying ? PlayerIcon.Pause : PlayerIcon.Play);
		_btnPlayPause.Invalidate();
	}

	private void UpdateMuteIcon()
	{
		if (_btnMute == null)
		{
			return;
		}
		_btnMute.Tag = (_isMuted ? PlayerIcon.Muted : PlayerIcon.Volume);
		_btnMute.Invalidate();
	}

	private void InitializePlayer()
	{
		try
		{
			VlcSetup.EnsureLibVlcEnvironment();
			_libVLC = new LibVLC();
			_mediaPlayer = new MediaPlayer(_libVLC);
			_videoView.MediaPlayer = _mediaPlayer;
			IsAvailable = true;
			_videoView.Visible = true;
			_fallbackPanel.Visible = false;
			_controlBar.Visible = true;
		_mediaPlayer.Playing += delegate
		{
			_lastKnownPlaying = true;
			BeginInvoke((Action)delegate
			{
				UpdatePlayPauseIcon();
				StartProgressTimer();
				// VLC 开始播放时可能重新子类化视频窗口过程，重新挂接我们的双击钩子
				AttachVideoViewHook();
			});
		};
		_mediaPlayer.Paused += delegate
		{
			_lastKnownPlaying = false;
			BeginInvoke((Action)delegate
			{
				UpdatePlayPauseIcon();
				StopProgressTimer();
			});
		};
		_mediaPlayer.Stopped += delegate
		{
			_lastKnownPlaying = false;
			// 通知正在等待停播的切台操作，可安全开始播放
			TaskCompletionSource<bool> tcs = _stopTcs;
			if (tcs != null)
			{
				_stopTcs = null;
				try
				{
					tcs.TrySetResult(true);
				}
				catch
				{
				}
			}
			BeginInvoke((Action)delegate
			{
				UpdatePlayPauseIcon();
				StopProgressTimer();
				ResetProgress();
			});
		};
		_mediaPlayer.EndReached += delegate
		{
			_lastKnownPlaying = false;
			BeginInvoke((Action)delegate
			{
				UpdatePlayPauseIcon();
				StopProgressTimer();
				ResetProgress();
			});
		};
		}
		catch (Exception ex)
		{
			IsAvailable = false;
			_videoView.Visible = false;
			_fallbackPanel.Visible = true;
			_controlBar.Visible = false;
			_fallbackLabel.Text = "预览播放器不可用：" + ex.GetType().Name + "\n将使用外部播放器打开。";
		}
	}

	public void LoadChannel(string url, string name)
	{
		_currentUrl = url;
		_currentName = name ?? string.Empty;
		if (string.IsNullOrWhiteSpace(url))
		{
			if (!IsAvailable)
			{
				_fallbackLabel.Text = "未选择频道";
			}
			return;
		}
		if (!IsAvailable)
		{
			_fallbackLabel.Text = "预览不可用，点击下方按钮用外部播放器打开：\n" + name;
			return;
		}
		int token = Interlocked.Increment(ref _loadToken);
		Uri uri;
		try
		{
			uri = new Uri(url);
		}
		catch
		{
			return;
		}
		// 标记切台进行中：禁止 UI 线程在切换期间访问 libvlc，避免死锁卡死
		Interlocked.Increment(ref _switchingCount);
		Task.Run(() => LoadChannelCore(token, uri, name ?? string.Empty));
	}

	// 切台核心：先停播 → 等待“停播完成(Stopped)”确保 VLC 视频输出(HWND)已拆除
	// → 再播放新媒体。这一步解决了 WinForms 下停播与播放争用视频输出导致 UI 卡死的经典问题。
	private void LoadChannelCore(int token, Uri uri, string name)
	{
		try
		{
			bool wasPlaying = false;
			try
			{
				wasPlaying = _mediaPlayer != null && _mediaPlayer.IsPlaying;
			}
			catch
			{
			}
			TaskCompletionSource<bool> tcs = null;
			lock (_loadLock)
			{
				if (base.IsDisposed || token != _loadToken)
				{
					return;
				}
				// 在 Stop 之前挂上完成源，确保本此停播触发的 Stopped 一定能唤醒等待
				if (wasPlaying)
				{
					tcs = new TaskCompletionSource<bool>();
					_stopTcs = tcs;
				}
				try
				{
					_mediaPlayer?.Stop();
				}
				catch
				{
				}
				Media previousMedia = _currentMedia;
				_currentMedia = null;
				if (previousMedia != null)
				{
					try
					{
						previousMedia.Dispose();
					}
					catch
					{
					}
				}
			}
			// 等待本次停播真正完成（视频输出拆除），再开始播放
			if (wasPlaying && tcs != null)
			{
				bool stopped = false;
				for (int i = 0; i < 60 && !stopped; i++)
				{
					if (base.IsDisposed || token != _loadToken)
					{
						return;
					}
					stopped = tcs.Task.Wait(100);
				}
				_stopTcs = null;
				if (base.IsDisposed || token != _loadToken)
				{
					return;
				}
			}
			if (base.IsDisposed || token != _loadToken)
			{
				return;
			}
			// 直播流无需预解析：ParseNetwork 会同步阻塞线程池最长 5 秒，
			// 快速换台时大量此类阻塞会耗尽线程池并让 VLC 输入堆积 → 软件卡死。
			Media media = new Media(_libVLC, uri);
			if (base.IsDisposed || token != _loadToken)
			{
				try
				{
					media.Dispose();
				}
				catch
				{
				}
				return;
			}
			lock (_loadLock)
			{
				if (base.IsDisposed || token != _loadToken)
				{
					try
					{
						media.Dispose();
					}
					catch
					{
					}
					return;
				}
				_currentMedia = media;
				_mediaPlayer.Play(media);
			}
			BeginInvoke((Action)delegate
			{
				if (base.IsDisposed)
				{
					return;
				}
				UpdatePlayPauseIcon();
			StartProgressTimer();
		});
		}
		catch (Exception ex)
		{
			if (!base.IsDisposed && token == _loadToken)
			{
				BeginInvoke((Action)delegate
				{
					if (base.IsDisposed)
					{
						return;
					}
					_fallbackLabel.Text = "该频道无法预览：" + ex.GetType().Name + "\n可改用外部播放器打开。";
					_fallbackPanel.Visible = true;
					_videoView.Visible = false;
					_controlBar.Visible = false;
					IsAvailable = false;
				});
			}
		}
		finally
		{
			Interlocked.Decrement(ref _switchingCount);
		}
	}

	public void Stop()
	{
		Interlocked.Increment(ref _loadToken);
		// 在后台线程停止，避免 UI 线程直接调用 Stop 与视频输出拆除死锁卡死
		Task.Run(delegate
		{
			try
			{
				_mediaPlayer?.Stop();
			}
			catch
			{
			}
			try
			{
				Media m = _currentMedia;
				_currentMedia = null;
				m?.Dispose();
			}
			catch
			{
			}
			try
			{
				if (!base.IsDisposed)
				{
					BeginInvoke((Action)delegate
					{
						if (!base.IsDisposed)
						{
							StopProgressTimer();
							ResetProgress();
							UpdatePlayPauseIcon();
						}
					});
				}
			}
			catch
			{
			}
		});
	}

	public void StopAsync()
	{
		if (base.IsDisposed)
		{
			return;
		}
		Interlocked.Increment(ref _loadToken);
		Task.Run(delegate
		{
			try
			{
				_mediaPlayer?.Stop();
			}
			catch
			{
			}
			try
			{
				Media m = _currentMedia;
				_currentMedia = null;
				m?.Dispose();
			}
			catch
			{
			}
			try
			{
				if (!base.IsDisposed)
				{
					BeginInvoke((Action)delegate
					{
						if (!base.IsDisposed)
						{
							StopProgressTimer();
							ResetProgress();
							UpdatePlayPauseIcon();
						}
					});
				}
			}
			catch
			{
			}
		});
	}

	private void TogglePlayPause()
	{
		if (!IsAvailable || _mediaPlayer == null)
		{
			return;
		}
		try
		{
			if (_lastKnownPlaying)
			{
				_mediaPlayer.Pause();
				StopProgressTimer();
			}
			else
			{
				_mediaPlayer.Play();
			StartProgressTimer();
		}
		UpdatePlayPauseIcon();
		}
		catch
		{
		}
	}

	private void ToggleMute()
	{
		if (!IsAvailable || _mediaPlayer == null)
		{
			return;
		}
		try
		{
			_isMuted = !_isMuted;
			_mediaPlayer.Mute = _isMuted;
			UpdateMuteIcon();
		}
		catch
		{
		}
	}

	private void ToggleFullscreen()
	{
		if (!IsAvailable || _videoView == null)
		{
			return;
		}
		if (_isFullscreen)
		{
			ExitFullscreen();
		}
		else
		{
			EnterFullscreen();
		}
	}

	private void HandleVideoClick(Point screenPos, bool fromMessageFilter)
	{
		DateTime now = DateTime.Now;
		bool isSameClick = screenPos == _lastClickScreenPos
			&& (now - _lastClickDedupeTime).TotalMilliseconds < 200.0;
		if (isSameClick && !fromMessageFilter)
		{
			return;
		}
		_lastClickScreenPos = screenPos;
		_lastClickDedupeTime = now;
		long sinceLast = _lastVideoClickTime == DateTime.MinValue ? -1L : (long)(now - _lastVideoClickTime).TotalMilliseconds;
		if (_lastVideoClickTime != DateTime.MinValue && (now - _lastVideoClickTime).TotalMilliseconds <= (double)SystemInformation.DoubleClickTime)
		{
			DiagLog($"HandleClick DOUBLE sinceLast={sinceLast}ms dctime={SystemInformation.DoubleClickTime} -> ToggleFullscreen");
			_lastVideoClickTime = DateTime.MinValue;
			ToggleFullscreen();
		}
		else
		{
			DiagLog($"HandleClick single sinceLast={sinceLast}ms dctime={SystemInformation.DoubleClickTime}");
			_lastVideoClickTime = now;
		}
	}

	private void EnterFullscreen()
	{
		DiagLog($"EnterFullscreen start isFs={_isFullscreen} vv={_videoView != null} avail={IsAvailable}");
		if (_isFullscreen || _videoView == null)
		{
			return;
		}
		try
		{
			_isFullscreen = true;
			// 正规窗口（带标题栏与“×”关闭按钮），双击进入最大化全屏；点 × 或按 Esc 关闭时恢复内嵌预览窗
			_fullscreenForm = new Form
			{
				FormBorderStyle = FormBorderStyle.Sizable,
				ControlBox = true,
				MaximizeBox = true,
				MinimizeBox = false,
				ShowIcon = false,
				WindowState = FormWindowState.Maximized,
				BackColor = Color.Black,
				KeyPreview = true,
				// 让独立播放器窗口置顶，避免被主窗口遮挡
				TopMost = true,
				Text = "IPTV 播放器"
			};
			Form ownerForm = this.FindForm();
			if (ownerForm != null)
			{
				_fullscreenForm.Owner = ownerForm;
			}
			_fullscreenForm.Controls.Add(_videoView);
			_videoView.Dock = DockStyle.Fill;
			_fullscreenForm.Click += delegate
			{
				HandleVideoClick(Cursor.Position, fromMessageFilter: false);
			};
			_fullscreenForm.MouseDown += delegate(object s, MouseEventArgs e)
			{
				if (e.Button == MouseButtons.Left)
				{
					HandleVideoClick(Cursor.Position, fromMessageFilter: false);
				}
			};
			_fullscreenForm.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					ExitFullscreen();
					e.Handled = true;
				}
			};
			_fullscreenForm.FormClosed += delegate
			{
				// 关闭（点 × 或 Esc）时无条件把视频交还内嵌预览窗；RestoreVideoView 内部会判断 Parent 避免重复
				_isFullscreen = false;
				RestoreVideoView();
			};
			_lastVideoClickTime = DateTime.MinValue;
			_fullscreenForm.Show();
			// 父容器切换后重新挂接消息钩子（HWND 可能重建）
			AttachVideoViewHook();
			// 切换父容器后 VideoView 的 HWND 可能重建，强制重新绑定 VLC 视频输出到新句柄
			if (_mediaPlayer != null && _videoView != null)
			{
				try
				{
					_videoView.MediaPlayer = _mediaPlayer;
				}
				catch
			{
			}
		}
	}
	catch (Exception ex)
		{
			DiagLog("EnterFullscreen FAILED: " + ex);
			_isFullscreen = false;
			try
			{
				if (_fullscreenForm != null && !_fullscreenForm.IsDisposed)
				{
					_fullscreenForm.Close();
				}
			}
			catch
			{
			}
			_fullscreenForm = null;
			RestoreVideoView();
		}
	}

	private void ExitFullscreen()
	{
		if (!_isFullscreen)
		{
			return;
		}
		_isFullscreen = false;
		Form dlg = _fullscreenForm;
		_fullscreenForm = null;
		RestoreVideoView();
		try
		{
			if (dlg != null && !dlg.IsDisposed)
			{
				dlg.Close();
			}
		}
		catch
		{
		}
	}

	private void RestoreVideoView()
	{
		try
		{
			if (_videoView != null && _videoView.Parent != this)
			{
				base.Controls.Add(_videoView);
				_videoView.Dock = DockStyle.Fill;
				_videoView.SendToBack();
				if (_controlBar != null)
				{
					_controlBar.BringToFront();
				}
				if (_fallbackPanel != null)
				{
					_fallbackPanel.BringToFront();
				}
				// 父容器切换后重新挂接消息钩子（HWND 可能重建）
				AttachVideoViewHook();
				// 父容器切换后重新绑定 VLC 视频输出到新的 HWND
				if (_mediaPlayer != null)
				{
					try
					{
						_videoView.MediaPlayer = _mediaPlayer;
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	public void SetMute(bool mute)
	{
		_isMuted = mute;
		if (_mediaPlayer != null)
		{
			try
			{
				_mediaPlayer.Mute = mute;
			}
			catch
			{
			}
		}
		UpdateMuteIcon();
	}

	public void SetVolume(int volume)
	{
		volume = Math.Max(0, Math.Min(100, volume));
		if (_mediaPlayer != null)
		{
			try
			{
				_mediaPlayer.Volume = volume;
			}
			catch
			{
			}
		}
		if (_volumeSlider != null)
		{
			_volumeSlider.Value = volume;
		}
	}

	public bool TryGetLiveState(out bool isPlaying, out long timeMs, out long lengthMs)
	{
		isPlaying = false;
		timeMs = 0L;
		lengthMs = 0L;
		if (!IsAvailable || _mediaPlayer == null)
		{
			return false;
		}
		// 切台期间禁止 UI 线程访问 libvlc，避免与后台 Stop 的 HWND 拆除死锁卡死
		if (_switchingCount > 0)
		{
			return false;
		}
		try
		{
			isPlaying = _mediaPlayer.IsPlaying;
			timeMs = _mediaPlayer.Time;
			lengthMs = _mediaPlayer.Length;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool TryGetStreamInfo(out string codec, out string resolution, out string fps, out string bitrate, out string audioChannels, out string audioSampleRate)
	{
		codec = "";
		resolution = "";
		fps = "";
		bitrate = "";
		audioChannels = "";
		audioSampleRate = "";
		if (!IsAvailable || _mediaPlayer == null)
		{
			return false;
		}
		// 切台过程中 libvlc 内部锁被后台 Stop/Play 占用，这里若访问 media.Tracks 会阻塞 UI 线程导致卡死
		if (_switchingCount > 0)
		{
			return false;
		}
		try
		{
			Media media = _mediaPlayer.Media;
			if (media == null)
			{
				return false;
			}
			MediaTrack[] tracks = media.Tracks;
			for (int i = 0; i < tracks.Length; i++)
			{
				MediaTrack track = tracks[i];
				if (track.TrackType == TrackType.Video)
				{
					VideoTrack vt = track.Data.Video;
					if (vt.Width != 0 && vt.Height != 0)
					{
						resolution = $"{vt.Width}x{vt.Height}";
					}
					if (track.Bitrate != 0)
					{
						bitrate = $"{track.Bitrate / 1000:F1} kb/s";
					}
					if (vt.FrameRateDen != 0)
					{
						fps = $"{(double)vt.FrameRateNum / (double)vt.FrameRateDen:F2} FPS";
					}
					string vCodec = TryGetCodecName(track, media);
					if (!string.IsNullOrEmpty(vCodec))
					{
						codec = vCodec;
					}
				}
				else
				{
					if (track.TrackType != TrackType.Audio)
					{
						continue;
					}
					AudioTrack at = track.Data.Audio;
					if (at.Channels != 0)
					{
						audioChannels = $"{at.Channels} 声道";
					}
					if (at.Rate != 0)
					{
						audioSampleRate = $"{at.Rate} Hz";
					}
					string aCodec = TryGetCodecName(track, media);
					if (!string.IsNullOrEmpty(aCodec))
					{
						if (string.IsNullOrEmpty(codec))
						{
							codec = aCodec;
						}
						else if (!codec.Contains(aCodec))
						{
							codec = codec + " + " + aCodec;
						}
					}
					if (track.Bitrate != 0 && string.IsNullOrEmpty(bitrate))
					{
						bitrate = $"{track.Bitrate / 1000:F1} kb/s";
					}
				}
			}
			if (string.IsNullOrEmpty(fps) && _mediaPlayer.Fps > 0f)
			{
				fps = $"{_mediaPlayer.Fps:F2} FPS";
			}
			return !string.IsNullOrEmpty(resolution) || !string.IsNullOrEmpty(codec) || !string.IsNullOrEmpty(audioChannels);
		}
		catch
		{
			return false;
		}
	}

	public bool TryGetRate(out float rate)
	{
		rate = 0f;
		if (!IsAvailable || _mediaPlayer == null)
		{
			return false;
		}
		// 切台期间禁止 UI 线程访问 libvlc，避免与后台 Stop 的 HWND 拆除死锁卡死
		if (_switchingCount > 0)
		{
			return false;
		}
		try
		{
			rate = _mediaPlayer.Rate;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool TryGetPlayerStats(out long readBytes, out float inputBitrate, out int decodedFrames, out int displayedFrames)
	{
		readBytes = 0L;
		inputBitrate = 0f;
		decodedFrames = 0;
		displayedFrames = 0;
		if (!IsAvailable || _mediaPlayer == null)
		{
			return false;
		}
		// 切台过程中 libvlc 内部锁被后台 Stop/Play 占用，这里访问 media.Statistics 会阻塞 UI 线程导致卡死
		if (_switchingCount > 0)
		{
			return false;
		}
		try
		{
			Media media = _mediaPlayer.Media;
			if (media == null)
			{
				return false;
			}
			MediaStats stats = media.Statistics;
			readBytes = stats.ReadBytes;
			inputBitrate = stats.InputBitrate;
			decodedFrames = stats.DecodedVideo;
			displayedFrames = stats.DisplayedPictures;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private string TryGetCodecName(MediaTrack track, Media media)
	{
		try
		{
			if (media != null)
			{
				string desc = media.CodecDescription(track.TrackType, track.Codec);
				if (!string.IsNullOrWhiteSpace(desc))
				{
					return desc;
				}
			}
		}
		catch
		{
		}
		if (!string.IsNullOrWhiteSpace(track.Description))
		{
			return track.Description;
		}
		uint fourcc = track.Codec;
		if (fourcc == 0)
		{
			return "";
		}
		byte[] bytes = BitConverter.GetBytes(fourcc);
		if (BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		string code = Encoding.ASCII.GetString(bytes).TrimEnd(default(char));
		if (code.Length >= 2)
		{
			return code.ToUpperInvariant();
		}
		return "";
	}

	protected virtual void OnOpenExternalRequested()
	{
		this.OpenExternalRequested?.Invoke(this, EventArgs.Empty);
	}

	public void ApplyTheme(AppTheme theme)
	{
		if (theme == null)
		{
			return;
		}
		_currentTheme = theme;
		// 控制栏背景：theme.Surface（替代硬编码 18,18,22）
		if (_controlBar != null)
		{
			_controlBar.BackColor = theme.Surface;
		}
		// TrackBar 背景/前景（强调）：theme.Surface / theme.Primary（替代 0,122,255）
		if (_volumeSlider != null)
		{
			_volumeSlider.BackColor = theme.Surface;
			_volumeSlider.ForeColor = theme.Primary;
		}
		if (_progressBar != null)
		{
			_progressBar.BackColor = theme.Surface;
			_progressBar.ForeColor = theme.Primary;
		}
		// 备用面板背景：theme.BgAlt（替代 24,24,28）
		if (_fallbackPanel != null)
		{
			_fallbackPanel.BackColor = theme.BgAlt;
		}
		// 备用面板文字：theme.TextPrimary（替代 220,220,225）
		if (_fallbackLabel != null)
		{
			_fallbackLabel.ForeColor = theme.TextPrimary;
		}
		// “用默认播放器打开”按钮：背景用 theme.Primary，文字保持白色
		if (_openExternalBtn != null)
		{
			_openExternalBtn.BackColor = theme.Primary;
			_openExternalBtn.ForeColor = Color.White;
		}
		// 状态标签文字色：theme.TextSecondary（替代 170,170,178）
		if (_statusLabel != null)
		{
			_statusLabel.ForeColor = theme.TextSecondary;
		}
		// 图标按钮文字/背景：theme.TextPrimary / theme.BgAlt（替代 220,220,225 / 40,40,48）
		Color btnFore = theme.TextPrimary;
		Color btnBack = theme.BgAlt;
		if (_btnPlayPause != null)
		{
			_btnPlayPause.ForeColor = btnFore;
			_btnPlayPause.BackColor = btnBack;
		}
		if (_btnMute != null)
		{
			_btnMute.ForeColor = btnFore;
			_btnMute.BackColor = btnBack;
		}
		if (_btnFullscreen != null)
		{
			_btnFullscreen.ForeColor = btnFore;
			_btnFullscreen.BackColor = btnBack;
		}
		// 让所有子控件重绘（Paint 委托会读取 _currentTheme 重新计算悬停态等）
		Invalidate(true);
	}

	private void AttachVideoViewHook()
	{
		lock (_mouseHookLock)
		{
			if (!_hookInstances.Contains(this))
			{
				_hookInstances.Add(this);
			}
			if (_mouseHook == IntPtr.Zero && _hookInstances.Count == 1)
			{
				_mouseHookProc = MouseHookProc;
				using (var curProc = System.Diagnostics.Process.GetCurrentProcess())
				using (var curModule = curProc.MainModule)
				{
					_mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle(curModule.ModuleName), 0);
				}
				DiagLog($"LowLevelMouseHook installed, handle=0x{_mouseHook.ToInt64():X}");
			}
		}
	}

	private void DetachVideoViewHook()
	{
		lock (_mouseHookLock)
		{
			if (_hookInstances.Contains(this))
			{
				_hookInstances.Remove(this);
			}
			if (_mouseHook != IntPtr.Zero && _hookInstances.Count == 0)
			{
				UnhookWindowsHookEx(_mouseHook);
				_mouseHook = IntPtr.Zero;
				_mouseHookProc = null;
				DiagLog("LowLevelMouseHook uninstalled");
			}
		}
	}

	private static IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode == HC_ACTION)
		{
			int msg = wParam.ToInt32();
			if (msg == WM_LBUTTONDOWN)
			{
				MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
				Point screenPos = new Point(hookStruct.pt.x, hookStruct.pt.y);
				lock (_mouseHookLock)
				{
					foreach (ChannelPlayer player in _hookInstances)
					{
						if (player.IsDisposed || player._videoView == null || !player._videoView.Visible)
						{
							continue;
						}
						bool inVideo = false;
						try
						{
							if (player._isFullscreen && player._fullscreenForm != null && !player._fullscreenForm.IsDisposed)
							{
								Point fp = player._fullscreenForm.PointToClient(screenPos);
								inVideo = player._fullscreenForm.ClientRectangle.Contains(fp);
							}
							else
							{
								Point vp = player._videoView.PointToClient(screenPos);
								inVideo = player._videoView.ClientRectangle.Contains(vp);
							}
						}
						catch
						{
						}
						if (inVideo)
						{
							// WH_MOUSE_LL 不会产生 WM_LBUTTONDBLCLK，需要自己用时间间隔+位置判断双击
							DateTime now = DateTime.Now;
							int dctime = SystemInformation.DoubleClickTime;
							Size dcsize = SystemInformation.DoubleClickSize;
							bool isDouble = player._lastLeftDownTime != DateTime.MinValue
								&& (now - player._lastLeftDownTime).TotalMilliseconds <= dctime
								&& Math.Abs(screenPos.X - player._lastLeftDownPos.X) <= dcsize.Width
								&& Math.Abs(screenPos.Y - player._lastLeftDownPos.Y) <= dcsize.Height;
							DiagLog($"[LLHook] down pos=({screenPos.X},{screenPos.Y}) isDouble={isDouble} sinceLast={(int)(now - player._lastLeftDownTime).TotalMilliseconds}ms fs={player._isFullscreen}");
							if (isDouble)
							{
								player._lastLeftDownTime = DateTime.MinValue;
								player.BeginInvoke((Action)delegate
								{
									player.ToggleFullscreen();
								});
							}
							else
							{
								player._lastLeftDownTime = now;
								player._lastLeftDownPos = screenPos;
							}
							break;
						}
					}
				}
			}
		}
		return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Application.RemoveMessageFilter(this);
			DetachVideoViewHook();
			if (_progressTimer != null)
			{
				_progressTimer.Stop();
				_progressTimer.Dispose();
				_progressTimer = null;
			}
			if (_fullscreenForm != null && !_fullscreenForm.IsDisposed)
			{
				try
				{
					_fullscreenForm.Close();
				}
				catch
				{
				}
				_fullscreenForm = null;
			}
			try
			{
				_mediaPlayer?.Stop();
			}
			catch
			{
			}
			try
			{
				_mediaPlayer?.Dispose();
			}
			catch
			{
			}
			try
			{
				_libVLC?.Dispose();
			}
			catch
			{
			}
		}
		base.Dispose(disposing);
	}
}
