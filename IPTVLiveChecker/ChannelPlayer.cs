using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace IPTVLiveChecker;

public class ChannelPlayer : UserControl, IMessageFilter, IChannelPlayer
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

	private ThemedSlider _volumeSlider;

	private ThemedSlider _progressBar;

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

	/// <summary>当前是否处于播放中（由 Playing/Stopped/Paused 事件维护的线程安全镜像，
	/// 供主窗体判断是否停止竞争型测速探针）。</summary>
	public bool IsPlaying => _lastKnownPlaying;

	// 等待“停播完成(Stopped)”信号，确保停播与播放串行，避免 WinForms 下 VLC 视频输出(HWND)
	// 拆除与播放争用导致 UI 卡死的经典问题。
	private TaskCompletionSource<bool> _stopTcs;

	private Media _currentMedia;

	private AppTheme _currentTheme;
	// 缓冲动画叠加层（主题色旋转环），仅播放加载/缓冲时显示
	private Panel _spinnerPanel;
	private System.Windows.Forms.Timer _spinnerTimer;
	private int _spinnerAngle;

	// 外部播放器内嵌宿主（VLC 播不了某链接时自动切换进来）
	private EmbeddedExternalPlayerHost _embedHost;

	// 是否已切换到外部播放器内嵌模式（避免同一链接重复触发）
	private bool _switchedToExternal;

	// 预览窗引擎：默认 VLC，可手动切到内嵌 MPV（库形态，P/Invoke libmpv-2.dll，无需 mpv.exe）
	private enum PreviewEngine { VLC, MPV }
	private PreviewEngine _engine = PreviewEngine.VLC;
	private DarkComboBox _engineCombo;

	// MPV 库形态播放器（无 exe）：手动选 MPV 引擎时托管并复用，dll 缺失时自带 fallback 提示
	private MpvPlayer _mpv;

	// 当前链接是否已被 VLC 成功渲染过（用于区分“正在缓冲”与“真播不了”）
	private bool _hasPlayedSuccessfully;

	// 播放超时判定：Play 后若干秒内未开始渲染即视为 VLC 无法播放
	private System.Windows.Forms.Timer _failTimer;

	// 全局开关：预览窗在 VLC 失败时自动切换外部播放器（内嵌）
	public static bool EnableAutoSwitchExternal { get; set; }

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
		PlayerLogger.Write("PLAYER", $"构造函数开始 | DPI={_dpiScale:F2} | VideoView.HandleCreated={_videoView.IsHandleCreated}");
		// 推迟到 VideoView Handle 创建后再初始化 VLC，避免因无 Handle 导致
		// _videoView.MediaPlayer 赋值失败 → 误进 fallback 面板
		_videoView.HandleCreated += delegate
		{
			PlayerLogger.Write("HANDLE", $"VideoView.HandleCreated 触发 | Handle={_videoView.Handle} | libVLC_ready={_libVLC != null}");
			InitializePlayer();
		};
		// 备份触发点：如果 VideoView.HandleCreated 因控件层级问题未触发，
		// ChannelPlayer 自身的 HandleCreated 和 Load 也会尝试初始化
		base.HandleCreated += delegate
		{
			PlayerLogger.Write("HANDLE", $"ChannelPlayer.HandleCreated 触发 | VideoView.Handle={_videoView?.Handle ?? IntPtr.Zero}");
			if (_libVLC == null && !base.IsDisposed)
			{
				InitializePlayer();
			}
		};
		base.Load += delegate
		{
			PlayerLogger.Write("PLAYER", $"ChannelPlayer.Load 触发 | libVLC_ready={_libVLC != null} | VideoView.Handle={_videoView?.Handle ?? IntPtr.Zero}");
			if (_libVLC == null && !base.IsDisposed)
			{
				InitializePlayer();
			}
		};
		// 关键修复：如果 HandleCreated 在事件绑定前已触发（WinForms 常见竞态），
		// 这里主动检查并补触发 InitializePlayer
		if (_videoView.IsHandleCreated && _libVLC == null && !base.IsDisposed)
		{
			PlayerLogger.Write("PLAYER", $"构造函数主动检查补初始化 | VideoView.Handle={_videoView.Handle}");
			InitializePlayer();
		}
		// 延迟初始化：在控件完全显示后再尝试一次，确保所有父容器布局已完成
		// 使用 BeginInvoke 确保在消息处理空闲时执行
		base.BeginInvoke((Action)delegate
		{
			PlayerLogger.Write("PLAYER", $"BeginInvoke 延迟初始化检查 | libVLC_ready={_libVLC != null} | VideoView.Handle={_videoView?.Handle ?? IntPtr.Zero}");
			if (_libVLC == null && !base.IsDisposed && _videoView.IsHandleCreated)
			{
				InitializePlayer();
			}
		});
		PlayerLogger.Write("PLAYER", "构造函数完成，所有初始化钩子已绑定");
		// 视频宿主占满整个控件（控制栏已移到播放窗外侧下方，不再占用控件高度）。
		// 全屏时 _videoView/_mpv 的 Parent 已切换为全屏窗，这里跳过布局约束。
		base.Layout += (s, e) =>
		{
			if (base.IsDisposed) return;
			int barH = 0;
			int w = base.ClientSize.Width;
			int h = Math.Max(0, base.ClientSize.Height - barH);
			if (_videoView != null && _videoView.Parent == this)
			{
				_videoView.SetBounds(0, 0, w, h);
			}
			if (_mpv != null && _mpv.Parent == this)
			{
				_mpv.SetBounds(0, 0, w, h);
			}
		};
		// 外部播放器内嵌宿主：默认隐藏，VLC 失败时切上来
		_embedHost = new EmbeddedExternalPlayerHost
		{
			Dock = DockStyle.Fill,
			Visible = false,
			BackColor = System.Drawing.Color.Black
		};
		_embedHost.EmbedFailed += OnEmbedFailed;
		base.Controls.Add(_embedHost);
		// MPV 库形态播放器：默认隐藏，手动切到 MPV 引擎时显示并托管
		_mpv = new MpvPlayer
		{
			Dock = DockStyle.Fill,
			Visible = false,
			BackColor = System.Drawing.Color.Black
		};
		// MPV 模式双击视频窗切换全屏（与 VLC 模式双击最大化行为一致），而非暂停/播放
		_mpv.HostDoubleClick += (s, e) => ToggleFullscreen();
		base.Controls.Add(_mpv);
		// 宿主 handle 创建后预热 mpv 上下文，使首次切到 MPV 引擎时无需同步初始化，避免切换卡顿
		_mpv.HandleCreated += (s, e) => _mpv.BeginInvoke((MethodInvoker)delegate { _mpv.Prewarm(); });
		// 缓冲动画叠加层：播放加载/缓冲时显示主题色旋转环（跟随主题换肤，尊重 ReduceMotion）
		_spinnerPanel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(18, 18, 22),
			Visible = false
		};
		_spinnerPanel.Paint += (s, e) =>
		{
			Panel p = (Panel)s;
			int cx = p.ClientSize.Width / 2;
			int cy = p.ClientSize.Height / 2;
			int r = (int)(22f * _dpiScale);
			Color c = (_currentTheme != null) ? _currentTheme.Primary : Color.White;
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			using (Pen pen = new Pen(c, (int)Math.Max(3f, 3f * _dpiScale)))
			{
				pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
				pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
				e.Graphics.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, _spinnerAngle, 300);
			}
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, c.R, c.G, c.B)))
			using (Font font = new Font("Segoe UI", 9f * _dpiScale))
			{
				StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
				e.Graphics.DrawString("缓冲中…", font, brush, cx, cy + r + (int)(10f * _dpiScale), sf);
			}
		};
		base.Controls.Add(_spinnerPanel);
		_spinnerTimer = new System.Windows.Forms.Timer { Interval = 16 };
		_spinnerTimer.Tick += (s, e) =>
		{
			_spinnerAngle = (_spinnerAngle + 9) % 360;
			bool playing = _lastKnownPlaying || (_mpv != null && _mpv.IsPlaying);
			bool aborted = (!IsAvailable || _switchedToExternal || (_fallbackPanel != null && _fallbackPanel.Visible));
			if (playing || aborted)
			{
				HideBuffering();
				return;
			}
			if (_spinnerPanel.Visible) _spinnerPanel.Invalidate();
		};
		_failTimer = new System.Windows.Forms.Timer { Interval = 10000 };
		_failTimer.Tick += FailTimer_Tick;
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
			Dock = System.Windows.Forms.DockStyle.None,
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
			BackColor = System.Drawing.Color.FromArgb(24, 24, 28),
			Visible = false
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
			Size = new System.Drawing.Size(180, 34),
			FlatStyle = System.Windows.Forms.FlatStyle.Flat,
			ForeColor = System.Drawing.Color.White,
			BackColor = System.Drawing.Color.FromArgb(0, 122, 255),
			Text = "用外部播放器打开",
			Cursor = System.Windows.Forms.Cursors.Hand,
			AutoSize = false,
			Padding = new System.Windows.Forms.Padding(14, 6, 14, 6),
			UseCompatibleTextRendering = true
		};
		this._openExternalBtn.FlatAppearance.BorderSize = 0;
		this._openExternalBtn.Click += delegate
		{
			this.OnOpenExternalRequested();
		};
		// Dock=Top 的 label 先添加，Dock=None 的 button 后添加，确保 label 占据正确的顶部空间
		this._fallbackPanel.Controls.Add(this._fallbackLabel);
		this._fallbackPanel.Controls.Add(this._openExternalBtn);
		// 触发一次初始布局（稍后 SizeChanged 会再触发一次）
		this._fallbackPanel.SizeChanged += delegate
		{
			LayoutFallbackContent();
		};
		this._fallbackPanel.VisibleChanged += delegate
		{
			if (this._fallbackPanel.Visible)
			{
				LayoutFallbackContent();
			}
		};
		this.BuildControlBar();
		base.Controls.Add(this._videoView);
		base.Controls.Add(this._fallbackPanel);
	}

	/// <summary>底部控制栏高度（含 DPI 缩放），供外部按 16:9 精确计算播放器容器高度。</summary>
	public int ControlBarHeight => (_controlBar != null) ? _controlBar.Height : ((int)(52f * _dpiScale));

	/// <summary>底部控制栏面板（移到播放窗外侧下方承载）。</summary>
	public Panel ControlBar => _controlBar;

	/// <summary>控制栏显隐状态变化通知，供主窗体同步外部容器高度。</summary>
	public event EventHandler ControlBarVisibilityChanged;

	/// <summary>重新布局 fallback 面板中的 label（顶部）和 button（剩余区域居中）。</summary>
	private void LayoutFallbackContent()
	{
		if (this._fallbackPanel == null || this._fallbackLabel == null || this._openExternalBtn == null)
		{
			return;
		}
		// 强制应用 Dock 布局，让 label 占满顶部
		this._fallbackPanel.PerformLayout();
		int panelW = this._fallbackPanel.ClientSize.Width;
		int panelH = this._fallbackPanel.ClientSize.Height;
		int logBtnW = this._openExternalBtn.Width;
		int logBtnH = this._openExternalBtn.Height;
		PlayerLogger.Write("LAYOUT", $"LayoutFallbackContent | panel={panelW}x{panelH} | btn={logBtnW}x{logBtnH} | panel.Visible={_fallbackPanel.Visible} | label.Text={_fallbackLabel.Text?.Substring(0, Math.Min(30, _fallbackLabel.Text?.Length ?? 0))}");
		// 面板尺寸还未确定时（如构造阶段 ClientSize=0），使用 BeginInvoke 延迟重试
		if (panelW <= 0 || panelH <= 0)
		{
			// 避免无限循环：只排队一次
			if (!this._layoutPending)
			{
				this._layoutPending = true;
				this.BeginInvoke((Action)delegate
				{
					this._layoutPending = false;
					LayoutFallbackContent();
				});
			}
			return;
		}
		int labelH = this._fallbackLabel.Visible ? this._fallbackLabel.Height : 0;
		int availH = Math.Max(0, panelH - labelH);
		// 按钮宽度：固定 160，但不超过面板宽度减去边距
		int btnW = Math.Min(160, panelW - 20);
		if (btnW < 80)
		{
			btnW = 80;
		}
		int btnH = this._openExternalBtn.Height > 0 ? this._openExternalBtn.Height : 34;
		int x = (panelW - btnW) / 2;
		int y = labelH + (availH - btnH) / 2;
		if (y < labelH + 10)
		{
			y = labelH + 10;
		}
		this._openExternalBtn.SetBounds(x, y, btnW, btnH);
	}

	private bool _layoutPending;

	private void BuildControlBar()
	{
		int barH = (int)(52f * _dpiScale);
		int btnSize = (int)(34f * _dpiScale);
		int pad = (int)(8f * _dpiScale);
		int y = (barH - btnSize) / 2;
		_controlBar = new Panel
		{
			Dock = DockStyle.None,
			Height = barH,
			BackColor = Color.FromArgb(18, 18, 22),
			Visible = false
		};
		// 控制栏不再放在播放器内部(会占用视频高度/被圆角窗裁切)，改由主窗体承载到播放窗外侧下方。
		_controlBar.VisibleChanged += (s, e) => ControlBarVisibilityChanged?.Invoke(this, EventArgs.Empty);
		// 引擎下拉框：使用 DarkComboBox（已自绘圆角、无边框、主题色），避免系统 ComboBox 漏白。
		Color barBack = _currentTheme != null ? _currentTheme.Surface : Color.FromArgb(18, 18, 22);
		Color btnForeFallback = _currentTheme != null ? _currentTheme.TextPrimary : Color.FromArgb(220, 220, 225);
		Color btnBackFallback = _currentTheme != null ? _currentTheme.BgAlt : Color.FromArgb(40, 40, 48);
		Color accentFallback = _currentTheme != null ? _currentTheme.Primary : Color.FromArgb(0, 122, 255);
		Color borderFallback = _currentTheme != null ? _currentTheme.Border : Color.FromArgb(68, 68, 78);

		_controlBar.BackColor = barBack;

		_engineCombo = new DarkComboBox
		{
			Width = (int)(92f * _dpiScale),
			Height = btnSize,
			Location = new Point(pad, y),
			Font = new Font("Segoe UI", 9f * _dpiScale),
			BackColor = btnBackFallback,
			ForeColor = btnForeFallback,
			BorderColor = borderFallback,
			FocusBorderColor = accentFallback,
			CornerRadius = (int)(6f * _dpiScale)
		};
		_engineCombo.Items.Add("VLC");
		_engineCombo.Items.Add("MPV");
		_engineCombo.SelectedIndex = 0;
		_engineCombo.SelectedIndexChanged += delegate { OnEngineComboChanged(); };
		_btnPlayPause = MakeIconButton(PlayerIcon.Play, btnSize, _engineCombo.Right + pad, y);
		_btnPlayPause.Click += delegate
		{
			TogglePlayPause();
		};
		_btnMute = MakeIconButton(PlayerIcon.Volume, btnSize, _btnPlayPause.Right + pad, y);
		_btnMute.Click += delegate
		{
			ToggleMute();
		};
		_volumeSlider = new ThemedSlider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 100,
			Width = (int)(70f * _dpiScale),
			Height = btnSize,
			TrackColor = DrawingUtils.IsDarkColor(barBack) ? Color.FromArgb(70, 70, 80) : Color.FromArgb(210, 210, 215),
			FillColor = accentFallback,
			ThumbColor = DrawingUtils.IsDarkColor(barBack) ? Color.White : Color.FromArgb(60, 60, 70),
			ThumbSize = (int)(14f * _dpiScale),
			TrackHeight = (int)(5f * _dpiScale)
		};
		_volumeSlider.Location = new Point(_btnMute.Right + pad, y + (int)(3f * _dpiScale));
		_volumeSlider.ValueChanged += delegate
		{
			SetVolume(_volumeSlider.Value);
		};
		int progX = _volumeSlider.Right + pad;
		int fsW = btnSize;
		int progW = Math.Max((int)(60f * _dpiScale), _controlBar.Width - progX - pad - fsW - pad - pad);
		_progressBar = new ThemedSlider
		{
			Minimum = 0,
			Maximum = 1000,
			Value = 0,
			Width = progW,
			Height = btnSize,
			TrackColor = DrawingUtils.IsDarkColor(barBack) ? Color.FromArgb(70, 70, 80) : Color.FromArgb(210, 210, 215),
			FillColor = accentFallback,
			ThumbColor = DrawingUtils.IsDarkColor(barBack) ? Color.White : Color.FromArgb(60, 60, 70),
			ThumbSize = (int)(14f * _dpiScale),
			TrackHeight = (int)(5f * _dpiScale)
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
		_controlBar.Controls.Add(_engineCombo);
		_controlBar.Controls.Add(_btnPlayPause);
		_controlBar.Controls.Add(_btnMute);
		_controlBar.Controls.Add(_volumeSlider);
		_controlBar.Controls.Add(_progressBar);
		_controlBar.Controls.Add(_btnFullscreen);
		_controlBar.Controls.Add(_statusLabel);
		_controlBar.Layout += delegate
		{
			_btnFullscreen.Location = new Point(_controlBar.Width - _btnFullscreen.Width - pad, y);
			// 控制栏宽度变化(如承载到外部后 Dock.Fill)时，按实际宽度重算进度条宽度，避免初始基于 0 宽度算错
			int progX2 = _volumeSlider.Right + pad;
			int fsW2 = btnSize;
			int progW2 = Math.Max((int)(60f * _dpiScale), _controlBar.Width - progX2 - pad - fsW2 - pad - pad);
			if (_progressBar != null) _progressBar.Width = progW2;
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
		if (_isUpdatingProgress) return;
		if (_engine == PreviewEngine.MPV)
		{
			double? dur = _mpv?.GetDuration();
			if (dur.HasValue && dur.Value > 0 && _progressBar != null)
			{
				double sec = dur.Value * _progressBar.Value / 1000.0;
				_mpv?.Seek(sec);
			}
			return;
		}
		if (_mediaPlayer == null)
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
		if (_engine == PreviewEngine.MPV)
		{
			if (_mpv == null || _progressBar == null) return;
			double? pos = _mpv.GetTimePos();
			double? dur = _mpv.GetDuration();
			if (pos.HasValue)
			{
				int val;
				if (dur.HasValue && dur.Value > 0)
				{
					val = (int)(pos.Value * 1000.0 / dur.Value);
				}
				else
				{
					long cycle = 120000L;
					val = (int)((((long)pos.Value % cycle) + cycle) % cycle * 1000L / cycle);
				}
				val = Math.Max(0, Math.Min(1000, val));
				_isUpdatingProgress = true;
				if (_progressBar.Value != val) _progressBar.Value = val;
				_isUpdatingProgress = false;
				bool? paused = _mpv.IsPaused;
				_lastKnownPlaying = !(paused ?? false);
				UpdatePlayPauseIcon();
				if (_statusLabel != null)
				{
					_statusLabel.Text = FormatMpvTime(pos.Value)
						+ (dur.HasValue && dur.Value > 0 ? " / " + FormatMpvTime(dur.Value) : "");
					_statusLabel.Visible = true;
				}
			}
			return;
		}
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

	private static string FormatMpvTime(double seconds)
	{
		if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
		int s = (int)seconds;
		int h = s / 3600;
		s %= 3600;
		int m = s / 60;
		s %= 60;
		return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m}:{s:D2}";
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
		// 防止重复初始化（HandleCreated/Load 可能多次触发）
		if (_libVLC != null)
		{
			PlayerLogger.Write("INIT", "InitializePlayer 被跳过：libVLC 已存在");
			return;
		}
		PlayerLogger.Write("INIT", $"InitializePlayer 开始 | VideoView.Handle={(_videoView != null ? _videoView.Handle : IntPtr.Zero)} | IsHandleCreated={_videoView?.IsHandleCreated} | IsDisposed={base.IsDisposed}");
		long initStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		try
		{
			// 确保 VideoView 的 handle 已创建，否则 MediaPlayer 赋值会失败
			if (_videoView != null && !_videoView.IsHandleCreated)
			{
				PlayerLogger.Write("INIT", $"强制创建 VideoView Handle");
				_ = _videoView.Handle;
				PlayerLogger.Write("INIT", $"Handle 创建完成 | Handle={_videoView.Handle}");
			}
			PlayerLogger.Write("INIT", "调用 VlcSetup.EnsureLibVlcEnvironment()");
			VlcSetup.EnsureLibVlcEnvironment();
			// 关键修复：显式调用 Core.Initialize 指定 native 库路径
			// LibVLCSharp 3.8.2 内部搜索逻辑期望 win-x64 子目录结构，
			// 但我们的 DLL 在 libvlc\ 根目录。手动 LoadLibrary 成功但 LibVLCSharp
			// 不会复用已加载模块，必须通过 Core.Initialize 传入正确路径。
			string libvlcPath = VlcSetup.GetLibVlcPath();
			PlayerLogger.Write("INIT", $"调用 Core.Initialize(path) | path={libvlcPath}");
			LibVLCSharp.Shared.Core.Initialize(libvlcPath);
			PlayerLogger.Write("INIT", "Core.Initialize 完成，创建 LibVLC 实例");
			_libVLC = new LibVLC();
			PlayerLogger.Write("INIT", "创建 MediaPlayer 实例");
			_mediaPlayer = new MediaPlayer(_libVLC);
			PlayerLogger.Write("INIT", "订阅 VLC EncounteredError 事件");
			_mediaPlayer.EncounteredError += delegate
			{
				PlayerLogger.Write("PLAY", $"VLC 遇到错误（无法播放该链接） | url={_currentName}");
				BeginInvoke((Action)delegate { HandleVlcPlaybackFailure(); });
			};
			PlayerLogger.Write("INIT", "赋值 VideoView.MediaPlayer");
			_videoView.MediaPlayer = _mediaPlayer;
			IsAvailable = true;
			_videoView.Visible = true;
			_fallbackPanel.Visible = false;
			_controlBar.Visible = true;
			long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - initStart;
			PlayerLogger.Write("INIT", $"初始化成功！耗时={elapsed}ms | IsAvailable={IsAvailable}");
		_mediaPlayer.Playing += delegate
		{
			_lastKnownPlaying = true;
			PlayerLogger.Write("PLAY", $"媒体开始播放 | url={_currentName}");
			BeginInvoke((Action)delegate
			{
				_hasPlayedSuccessfully = true;
				StopFailTimer();
				UpdatePlayPauseIcon();
				StartProgressTimer();
				// VLC 开始播放时可能重新子类化视频窗口过程，重新挂接我们的双击钩子
				AttachVideoViewHook();
			});
		};
		_mediaPlayer.Paused += delegate
		{
			_lastKnownPlaying = false;
			PlayerLogger.Write("PLAY", $"媒体暂停 | url={_currentName}");
			BeginInvoke((Action)delegate
			{
				UpdatePlayPauseIcon();
				StopProgressTimer();
			});
		};
		_mediaPlayer.Stopped += delegate
		{
			_lastKnownPlaying = false;
			PlayerLogger.Write("PLAY", $"媒体停止 | url={_currentName}");
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
			PlayerLogger.Write("PLAY", $"媒体播放结束 | url={_currentName}");
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
			PlayerLogger.WriteError("INIT", ex);
			// 显示详细的错误信息，帮助诊断 VLC 加载失败的原因
			string detail = ex.Message;
			if (ex is System.ComponentModel.Win32Exception || ex is DllNotFoundException)
			{
				detail = "无法加载 VLC 核心库。\n请删除程序目录下的 libvlc 文件夹后重启，\n程序将自动从已安装的 VLC 复制完整文件。\n\n详情：" + ex.Message;
			}
			else if (ex is InvalidOperationException)
			{
				detail = ex.Message;
			}
			else
			{
				detail = "预览播放器不可用：" + ex.GetType().Name + "\n" + ex.Message;
			}
			_fallbackLabel.Text = detail;
			// 检测可用的外部播放器，更新按钮文字
			ExternalPlayerHelper.PlayerInfo ext = ExternalPlayerHelper.FindBestPlayer();
			if (ext != null && ext.Type != ExternalPlayerHelper.PlayerType.SystemDefault)
			{
				_openExternalBtn.Text = "用 " + ext.DisplayName + " 打开";
				PlayerLogger.Write("INIT", $"VLC失败，检测到外部播放器: {ext.DisplayName} | path={ext.Path}");
			}
			else
			{
				_openExternalBtn.Text = "用系统默认播放器打开";
				PlayerLogger.Write("INIT", "VLC失败，未检测到外部播放器，使用系统默认");
			}
			LayoutFallbackContent();
		}
	}

	private void ShowBuffering()
	{
		if (_spinnerPanel == null) return;
		if (base.InvokeRequired) { base.BeginInvoke((Action)ShowBuffering); return; }
		_spinnerAngle = 0;
		_spinnerPanel.BringToFront();
		_spinnerPanel.Visible = true;
		if (!_spinnerTimer.Enabled) _spinnerTimer.Start();
	}
	private void HideBuffering()
	{
		if (_spinnerPanel == null) return;
		if (base.InvokeRequired) { base.BeginInvoke((Action)HideBuffering); return; }
		_spinnerPanel.Visible = false;
		if (_spinnerTimer.Enabled) _spinnerTimer.Stop();
	}

	public void LoadChannel(string url, string name)
	{
		_currentUrl = url;
		_currentName = name ?? string.Empty;
		// 手动选择 MPV 引擎时，直接走内嵌 MPV（保留当前 VLC 预览窗结构，仅替换渲染源），跳过 VLC 流程
		if (_engine == PreviewEngine.MPV)
		{
			EmbedMpvEngine(url, _currentName);
			return;
		}
		// 切换频道时，若之前已切到外部播放器内嵌，先退回 VLC 模式（新链接可能 VLC 能播）
		if (_switchedToExternal)
		{
			ResetToVlcMode();
		}
		// 切台前若处于全屏：先把视频交还内嵌预览窗，再让后台线程操作内嵌 HWND 切台，
		// 避免后台 Stop/Play 直接操作全屏顶层窗的 HWND 导致视频输出拆除等待卡死（预览窗切换频道卡死的根因）。
		if (_isFullscreen)
		{
			ExitFullscreen();
		}
		PlayerLogger.Write("LOAD", $"LoadChannel 进入 | name={name} | url={url} | IsAvailable={IsAvailable} | libVLC_ready={_libVLC != null} | VideoView.Handle={_videoView?.Handle ?? IntPtr.Zero}");
		// 如果 VideoView handle 已创建但还没初始化 VLC，这里先同步初始化，避免竞态
		if (!IsAvailable && _libVLC == null && _videoView != null && _videoView.IsHandleCreated && !base.IsDisposed)
		{
			PlayerLogger.Write("LOAD", "Handle已创建但VLC未初始化，同步初始化");
			InitializePlayer();
		}
		// 如果 handle 还没创建，用 BeginInvoke 延迟重试（等控件完全显示后再初始化）
		if (!IsAvailable && _libVLC == null && _videoView != null && !_videoView.IsHandleCreated && !base.IsDisposed)
		{
			PlayerLogger.Write("LOAD", "Handle未创建，进入延迟初始化分支");
			// 先显示 loading 状态
			_fallbackLabel.Text = "播放器正在初始化，请稍候...";
			_fallbackPanel.Visible = true;
			LayoutFallbackContent();
			string pendingUrl = url;
			string pendingName = name;
			base.BeginInvoke((Action)delegate
			{
				if (base.IsDisposed) return;
				PlayerLogger.Write("LOAD", "延迟初始化: 执行InitializePlayer");
				InitializePlayer();
				if (IsAvailable)
				{
					PlayerLogger.Write("LOAD", "延迟初始化成功，重新加载频道");
					// 初始化成功后重新加载频道
					LoadChannel(pendingUrl, pendingName);
				}
				else
				{
					PlayerLogger.Write("LOAD", "延迟初始化仍然失败，显示fallback");
					// 初始化仍然失败，显示 fallback
					_fallbackLabel.Text = "预览播放器初始化失败\n请点击下方按钮用外部播放器打开：\n" + pendingName;
					_fallbackPanel.Visible = true;
					LayoutFallbackContent();
				}
			});
			return;
		}
		if (string.IsNullOrWhiteSpace(url))
		{
			PlayerLogger.Write("LOAD", "URL为空，显示未选择状态");
			if (!IsAvailable)
			{
				_fallbackLabel.Text = "未选择频道";
				_fallbackPanel.Visible = true;
				LayoutFallbackContent();
			}
			return;
		}
		if (!IsAvailable)
		{
			PlayerLogger.Write("LOAD", $"播放器不可用，显示fallback | name={name}");
			_fallbackLabel.Text = "预览不可用，点击下方按钮用外部播放器打开：\n" + name;
			_fallbackPanel.Visible = true;
			LayoutFallbackContent();
			return;
		}
		// VLC 已就绪，隐藏 fallback 面板（防止上次显示 fallback 后没切回来）
		_fallbackPanel.Visible = false;
		int token = Interlocked.Increment(ref _loadToken);
		PlayerLogger.Write("LOAD", $"开始加载频道 | token={token} | name={name}");
		Uri uri;
		try
		{
			uri = new Uri(url);
		}
		catch
		{
			PlayerLogger.Write("LOAD", $"URL解析失败: {url}");
			return;
		}
		// 标记切台进行中：禁止 UI 线程在切换期间访问 libvlc，避免死锁卡死
		Interlocked.Increment(ref _switchingCount);
		ShowBuffering();
		Task.Run(() => LoadChannelCore(token, uri, name ?? string.Empty));
	}

	// 切台核心：先停播 → 等待“停播完成(Stopped)”确保 VLC 视频输出(HWND)已拆除
	// → 再播放新媒体。这一步解决了 WinForms 下停播与播放争用视频输出导致 UI 卡死的经典问题。
	private void LoadChannelCore(int token, Uri uri, string name)
	{
		PlayerLogger.Write("CORE", $"LoadChannelCore 开始 | token={token} | name={name}");
		long coreStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
					PlayerLogger.Write("CORE", $"调用 _mediaPlayer.Stop() | wasPlaying={wasPlaying}");
					_mediaPlayer?.Stop();
					PlayerLogger.Write("CORE", "Stop() 调用完成");
				}
				catch (Exception stopEx)
				{
					PlayerLogger.WriteError("CORE", stopEx);
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
		// 通过 :no-parse 选项禁用输入预解析，消除该同步阻塞（播放时切换频道卡死的根因）。
		Media media;
		MeasuredMediaInput localInput = null;
		string hlsProxyUrl = null;
		bool useInjection = UseMeasuredStreamProbe
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
			&& !IsHlsUri(uri);
		bool isHls = IsHlsUri(uri);
		if (useInjection)
		{
			// Tier B 真正形态：零额外带宽注入式精确模式。
			// 不再另开连接丢弃下载，而是把上游【同一个】连接注入 VLC 输入，
			// VLC 消费多少字节就由 MeasuredMediaInput 精确计数多少（单连接、零额外带宽）。
			localInput = new MeasuredMediaInput(uri);
			media = new Media(_libVLC, localInput, new[] { ":no-parse" });
		}
		else if (isHls && UseHlsCountingProxy)
		{
			// HLS 优先方案：本地计数代理。VLC 改连本机代理 URL，代理改写 m3u8 分片 URL 并回源计数，
			// VLC 的消费路径就是测量路径，零额外带宽。代理启动失败则回退原始 URL + 分片探针兜底。
			StopHlsProxy();
			var proxy = new HlsCountingProxy(uri.AbsoluteUri);
			hlsProxyUrl = proxy.Start();
			if (!string.IsNullOrWhiteSpace(hlsProxyUrl))
			{
                _hlsProxy = proxy;
                media = new Media(_libVLC, hlsProxyUrl);
                media.AddOption(":no-parse");
                // 方案A：本地代理按需转发导致字节计数在稳定播放期脉冲冻结，
                // 额外启动【持续型】HLS 分片探针作为持续测速源，根治链接速度归零。
                StartHlsProbeContinuous(uri.AbsoluteUri);
			}
			else
			{
				proxy.Dispose();
				media = new Media(_libVLC, uri);
				media.AddOption(":no-parse");
				if (UseHlsProbeFallback)
					StartHlsProbe(uri.AbsoluteUri);
			}
		}
		else
		{
			media = new Media(_libVLC, uri);
			media.AddOption(":no-parse");
			if (isHls && UseHlsProbeFallback && string.IsNullOrWhiteSpace(hlsProxyUrl))
				StartHlsProbe(uri.AbsoluteUri);
		}
			if (base.IsDisposed || token != _loadToken)
			{
				try { localInput?.Dispose(); }
				catch { }
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
					try { localInput?.Dispose(); }
					catch { }
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
				_measuredInput = localInput;
				_hasPlayedSuccessfully = false;
				// 新频道开始加载：清空上一流的采样基线，避免 readBytes/_lastLiveRate 跨流污染导致首帧误判或归零
				ResetStatSampler();
				PlayerLogger.Write("CORE", $"调用 _mediaPlayer.Play(media) | name={name}");
				_mediaPlayer.Play(media);
				PlayerLogger.Write("CORE", "Play() 调用完成");
			}
			long elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - coreStart;
			PlayerLogger.Write("CORE", $"LoadChannelCore 完成 | 耗时={elapsed}ms | name={name}");
			BeginInvoke((Action)delegate
			{
				if (base.IsDisposed)
				{
					return;
				}
				UpdatePlayPauseIcon();
			StartProgressTimer();
				// 启动播放超时判定：若 10s 内未真正开始渲染，则判定 VLC 无法播放该链接
				if (!_switchedToExternal && EnableAutoSwitchExternal)
				{
					StartFailTimer();
				}
		});
		}
		catch (Exception ex)
		{
			PlayerLogger.WriteError("CORE", ex);
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
		ResetStatSampler();
		StopMeasuredInput();
		StopHlsResources();
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
		StopFailTimer();
		_embedHost?.Stop();
		_mpv?.Stop();
	}

	public void StopAsync()
	{
		if (base.IsDisposed)
		{
			return;
		}
		ResetStatSampler();
		StopMeasuredInput();
		StopHlsResources();
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
		StopFailTimer();
		_embedHost?.Stop();
		_mpv?.Stop();
	}

	// ===== 自动切换外部播放器（内嵌）相关 =====

	private void StartFailTimer()
	{
		try
		{
			if (_failTimer != null)
			{
				_failTimer.Stop();
				_failTimer.Start();
			}
		}
		catch
		{
		}
	}

	private void StopFailTimer()
	{
		try
		{
			_failTimer?.Stop();
		}
		catch
		{
		}
	}

	private void FailTimer_Tick(object sender, EventArgs e)
	{
		StopFailTimer();
		if (_switchedToExternal || !IsAvailable) return;
		if (_hasPlayedSuccessfully) return; // 已成功渲染过则认为是正常缓冲/重连，不切换
		PlayerLogger.Write("PLAY", "播放超时（10s 未开始渲染），判定 VLC 无法播放该链接，尝试切换外部播放器");
		HandleVlcPlaybackFailure();
	}

	private void HandleVlcPlaybackFailure()
	{
		if (_switchedToExternal || !IsAvailable) return;
		if (!EnableAutoSwitchExternal) return;
		if (_hasPlayedSuccessfully) return;
		TryEmbedExternal(_currentUrl, _currentName);
	}

	private void TryEmbedExternal(string url, string name)
	{
		// 优先 PotPlayer（用户指定），其次 MPV（官方 --wid 嵌入更稳）
		List<ExternalPlayerHelper.PlayerInfo> players = ExternalPlayerHelper.ScanAllPlayers();
		ExternalPlayerHelper.PlayerInfo target = players.Find(p => p.Type == ExternalPlayerHelper.PlayerType.PotPlayer)
			?? players.Find(p => p.Type == ExternalPlayerHelper.PlayerType.MPV);
		if (target == null)
		{
			// 没有可内嵌的播放器，直接以独立窗口打开最佳外部播放器
			ExternalPlayerHelper.PlayerInfo best = ExternalPlayerHelper.FindBestPlayer();
			if (ExternalPlayerHelper.Play(url, best))
			{
				ShowFallback("VLC 无法播放，已用 " + best.DisplayName + " 打开（独立窗口）", false);
			}
			else
			{
				ShowFallback("该频道无法预览：" + name + "\n可改用外部播放器打开。", true);
			}
			return;
		}
		_switchedToExternal = true;
		try { _mediaPlayer?.Stop(); } catch { }
		StopFailTimer();
		// 切到内嵌宿主：隐藏 VLC 控件，显示外部播放器宿主
		if (_videoView != null) _videoView.Visible = false;
		if (_controlBar != null) _controlBar.Visible = false;
		if (_fallbackPanel != null) _fallbackPanel.Visible = false;
		_embedHost.Visible = true;
		_embedHost.BringToFront();
		PlayerLogger.Write("EMBED", $"切换到内嵌外部播放器: {target.DisplayName} | url={url}");
		_embedHost.Play(url, target);
	}

	private void OnEmbedFailed(object sender, string reason)
	{
		// 内嵌失败，降级为独立窗口打开
		BeginInvoke((Action)delegate
		{
			_embedHost.Visible = false;
			ExternalPlayerHelper.PlayerInfo best = ExternalPlayerHelper.FindBestPlayer();
			if (ExternalPlayerHelper.Play(_currentUrl, best))
			{
				ShowFallback("VLC 无法播放，已用 " + best.DisplayName + " 打开（独立窗口）", false);
			}
			else
			{
				ShowFallback("该频道无法预览：" + _currentName + "\n" + reason, true);
			}
		});
	}

	private void OnEngineComboChanged()
	{
		bool toMpv = _engineCombo != null && _engineCombo.SelectedIndex == 1;
		_engine = toMpv ? PreviewEngine.MPV : PreviewEngine.VLC;
		if (toMpv)
		{
			EmbedMpvEngine(_currentUrl, _currentName);
		}
		else
		{
			RevertToVlcEngine();
		}
	}

	/// <summary>手动切到内嵌 MPV 引擎（库形态，P/Invoke libmpv-2.dll，无需 mpv.exe）：
	/// 隐藏 VLC 渲染与专属控件，托管并复用 MpvPlayer。dll 缺失时 MpvPlayer 自带 fallback 提示。</summary>
	private void EmbedMpvEngine(string url, string name)
	{
		ApplyEngineChrome();
		try { _mediaPlayer?.Stop(); } catch { }
		StopFailTimer();
		if (_videoView != null) _videoView.Visible = false;
		if (_fallbackPanel != null) _fallbackPanel.Visible = false;
		if (_embedHost != null) { _embedHost.Visible = false; }
		if (_controlBar != null) _controlBar.Visible = true;
		_mpv.Visible = true;
		_mpv.BringToFront();
		_mpv.SetControlBarVisible(false); // 托管模式下由 ChannelPlayer 控制栏统一控制
		if (_controlBar != null) _controlBar.BringToFront(); // 确保控制栏盖在 MpvPlayer 之上
		PlayerLogger.Write("ENGINE", $"切换到内嵌 MPV 引擎(库形态) | url={url}");
		// 进度条/音量/播放暂停等控制统一转发到 MpvPlayer（见各控制分支中的 _engine 判断）
		ShowBuffering();
		if (!string.IsNullOrWhiteSpace(url))
		{
			_mpv.LoadChannel(url, name);
			_lastKnownPlaying = true;
			UpdatePlayPauseIcon();
			if (_progressBar != null) _progressBar.Value = 0;
			StartProgressTimer();
		}
	}

	private void RevertToVlcEngine()
	{
		StopProgressTimer();
		if (_mpv != null && _mpv.Visible)
		{
			_mpv.Stop();
			_mpv.Visible = false;
		}
		if (_embedHost != null && _embedHost.Visible)
		{
			_embedHost.Stop();
			_embedHost.Visible = false;
		}
		_switchedToExternal = false;
		if (_videoView != null) _videoView.Visible = true;
		if (_controlBar != null) _controlBar.Visible = true;
		ApplyEngineChrome();
		if (!string.IsNullOrWhiteSpace(_currentUrl))
		{
			// 重新走正常 VLC 加载（此时 _engine 已为 VLC）
			LoadChannel(_currentUrl, _currentName);
		}
	}

	/// <summary>MPV 模式下隐藏 VLC 专属控件，仅保留引擎下拉，避免误控隐藏的 VLC。</summary>
	private void ApplyEngineChrome()
	{
		bool mpv = _engine == PreviewEngine.MPV;
		// MPV 模式下所有控制按钮均保留（mpv 已支持全屏切换）
		if (_btnPlayPause != null) _btnPlayPause.Visible = true;
		if (_btnMute != null) _btnMute.Visible = true;
		if (_volumeSlider != null) _volumeSlider.Visible = true;
		if (_progressBar != null) _progressBar.Visible = true;
		if (_btnFullscreen != null) _btnFullscreen.Visible = true;
		if (_engineCombo != null) _engineCombo.Visible = true;
		if (_statusLabel != null)
		{
			_statusLabel.Visible = mpv;
			if (mpv)
			{
				_statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				_statusLabel.AutoSize = true;
				int rightX = Math.Max(4, _controlBar.Width - 4 - (int)(130f * _dpiScale));
				_statusLabel.Location = new Point(rightX, 8);
			}
		}
	}

	private void ResetToVlcMode()
	{
		if (_embedHost != null && _embedHost.Visible)
		{
			_embedHost.Stop();
			_embedHost.Visible = false;
		}
		_switchedToExternal = false;
		_hasPlayedSuccessfully = false;
		if (IsAvailable)
		{
			if (_videoView != null) _videoView.Visible = true;
			if (_controlBar != null) _controlBar.Visible = true;
		}
	}

	private void ShowFallback(string text, bool showButton)
	{
		if (_fallbackPanel == null || _fallbackLabel == null) return;
		_fallbackLabel.Text = text;
		_fallbackPanel.Visible = true;
		if (_openExternalBtn != null) _openExternalBtn.Visible = showButton;
		LayoutFallbackContent();
	}

	private void TogglePlayPause()
	{
		if (_engine == PreviewEngine.MPV)
		{
			if (_mpv != null && _mpv.Visible)
			{
				bool paused = _mpv.IsPaused;
				_mpv.SetPaused(!paused);
				_lastKnownPlaying = !paused;
				UpdatePlayPauseIcon();
			}
			return;
		}
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
		if (_engine == PreviewEngine.MPV)
		{
			_isMuted = !_isMuted;
			_mpv?.SetMute(_isMuted);
			UpdateMuteIcon();
			return;
		}
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
		DiagLog($"EnterFullscreen start isFs={_isFullscreen} vv={_videoView != null} avail={IsAvailable} engine={_engine}");
		if (_isFullscreen)
		{
			return;
		}
		// MPV 引擎：把 MpvPlayer 控件整体移入全屏窗（wid 嵌入的 _host 随控件父化到新窗口，mpv 自动跟随渲染）
		if (_engine == PreviewEngine.MPV)
		{
			if (_mpv == null || _mpv.IsDisposed)
			{
				return;
			}
			try
			{
				_isFullscreen = true;
				_fullscreenForm = new Form
				{
					FormBorderStyle = FormBorderStyle.None,
					WindowState = FormWindowState.Maximized,
					BackColor = Color.Black,
					KeyPreview = true,
					TopMost = true,
					ShowInTaskbar = false
				};
				Form ownerForm = this.FindForm();
				if (ownerForm != null)
				{
					_fullscreenForm.Owner = ownerForm;
				}
				_fullscreenForm.Controls.Add(_mpv);
				_mpv.Dock = DockStyle.Fill;
				_mpv.SetFullscreenMode(true);
				_mpv.SetControlBarVisible(true);
				HookFullscreenForm();
				AddFullscreenInfoBar();
				_lastVideoClickTime = DateTime.MinValue;
				_fullscreenForm.Show();
				AttachVideoViewHook();
			}
			catch (Exception ex)
			{
				DiagLog("EnterFullscreen(MPV) FAILED: " + ex);
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
			return;
		}
		if (_videoView == null)
		{
			return;
		}
		try
		{
			_isFullscreen = true;
			// 无边框沉浸式全屏，浮动信息栏显示频道名称，ESC 退出
			_fullscreenForm = new Form
			{
				FormBorderStyle = FormBorderStyle.None,
				WindowState = FormWindowState.Maximized,
				BackColor = Color.Black,
				KeyPreview = true,
				TopMost = true,
				ShowInTaskbar = false
			};
			Form ownerForm = this.FindForm();
			if (ownerForm != null)
			{
				_fullscreenForm.Owner = ownerForm;
			}
			_fullscreenForm.Controls.Add(_videoView);
			_videoView.Dock = DockStyle.Fill;
			HookFullscreenForm();
			AddFullscreenInfoBar();
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

	private void HookFullscreenForm()
	{
		if (_fullscreenForm == null)
		{
			return;
		}
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
	}

	/// <summary>
	/// 为无边框全屏窗口添加浮动信息栏：显示频道名称，鼠标静止 3 秒后自动淡出隐藏。
	/// </summary>
	private void AddFullscreenInfoBar()
	{
		if (_fullscreenForm == null) return;

		Color themeText = _currentTheme != null ? _currentTheme.TextPrimary : Color.White;
		Color themeBg = _currentTheme != null ? _currentTheme.Bg : Color.Black;
		string chName = string.IsNullOrWhiteSpace(_currentName) ? "IPTV 播放器" : _currentName;

		var infoBar = new Panel
		{
			Dock = DockStyle.Top,
			Height = 44,
			BackColor = Color.FromArgb(180, themeBg.R, themeBg.G, themeBg.B)
		};
		var lbl = new Label
		{
			Text = "  " + chName,
			Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
			ForeColor = themeText,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft
		};
		var hintLbl = new Label
		{
			Text = "ESC 退出全屏",
			Font = new Font("Microsoft YaHei UI", 8f),
			ForeColor = Color.FromArgb(160, themeText),
			Dock = DockStyle.Right,
			Width = 100,
			TextAlign = ContentAlignment.MiddleRight
		};
		infoBar.Controls.Add(lbl);
		infoBar.Controls.Add(hintLbl);
		_fullscreenForm.Controls.Add(infoBar);
		infoBar.BringToFront();

		// 鼠标静止 3 秒后淡出隐藏
		var hideTimer = new System.Windows.Forms.Timer { Interval = 3000 };
		hideTimer.Tick += delegate
		{
			hideTimer.Stop();
			var fadeTimer = new System.Windows.Forms.Timer { Interval = 30 };
			fadeTimer.Tick += delegate
			{
				int a = infoBar.BackColor.A - 25;
				if (a <= 0)
				{
					fadeTimer.Stop();
					fadeTimer.Dispose();
					infoBar.Visible = false;
				}
				else
				{
					infoBar.BackColor = Color.FromArgb(a, themeBg.R, themeBg.G, themeBg.B);
					lbl.ForeColor = Color.FromArgb(Math.Min(255, a + 40), themeText);
					hintLbl.ForeColor = Color.FromArgb(Math.Min(255, a), themeText.R * 2 / 3, themeText.G * 2 / 3, themeText.B * 2 / 3);
				}
			};
			fadeTimer.Start();
		};
		hideTimer.Start();

		// 鼠标移动时重新显示并重置计时
		EventHandler showBar = delegate
		{
			if (!infoBar.Visible)
			{
				infoBar.Visible = true;
				infoBar.BackColor = Color.FromArgb(180, themeBg.R, themeBg.G, themeBg.B);
				lbl.ForeColor = themeText;
			}
			hideTimer.Stop();
			hideTimer.Start();
		};
		_fullscreenForm.MouseMove += (s, e) => showBar(null, EventArgs.Empty);
		_fullscreenForm.FormClosed += delegate
		{
			hideTimer.Stop();
			hideTimer.Dispose();
		};
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
			// MPV 引擎：把 MpvPlayer 控件从全屏窗交还内嵌预览窗（wid 嵌入的 _host 随父化回到原窗口，mpv 自动跟随）
			if (_engine == PreviewEngine.MPV && _mpv != null && _mpv.Parent != this)
			{
				base.Controls.Add(_mpv);
				_mpv.Dock = DockStyle.Fill;
				_mpv.SetFullscreenMode(false);
				_mpv.SetControlBarVisible(false);
				if (_controlBar != null)
				{
					_controlBar.BringToFront();
				}
				AttachVideoViewHook();
				return;
			}
			if (_videoView != null && _videoView.Parent != this)
			{
				base.Controls.Add(_videoView);
				_videoView.Dock = DockStyle.None;
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
		if (_engine == PreviewEngine.MPV)
		{
			_mpv?.SetVolume(volume);
			if (_volumeSlider != null) _volumeSlider.Value = volume;
			return;
		}
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
		// MPV 托管引擎：转发到 MpvPlayer 获取播放状态（含直播中判定、进度时间）
		if (_engine == PreviewEngine.MPV)
		{
			return _mpv != null && _mpv.TryGetLiveState(out isPlaying, out timeMs, out lengthMs);
		}
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
		if (_engine == PreviewEngine.MPV)
		{
			return _mpv != null && _mpv.TryGetStreamInfo(out codec, out resolution, out fps, out bitrate, out audioChannels, out audioSampleRate);
		}
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

	// VLC 实时速率采样器基线（ReadBytes 增量采样 + 上一帧速度沿用兜底用）
	private long _lastStatReadBytes;
	private long _lastStatTickMs;
	private float _lastLiveRate;

	/// <summary>Tier B 精确模式开关：开启后改用 MeasuredMediaInput 注入式精确计数——
	/// 把上游单个连接直接注入 VLC 输入，VLC 消费多少字节就由 MeasuredStream 精确计数多少，
	/// 不另开连接、不丢弃下载，真正零额外带宽。仅对直接流（.ts/.flv 等，非 HLS）启用，
	/// HLS 因 VLC 需自行分片回源故回退常规路径。
	/// 【默认开启】以消除直链播放期 Tier A 独立探针抢带宽导致的卡顿；个别需要原生 HTTP 鉴权/重定向/Range 的源
	/// 若注入失败，可置回 false（届时主窗体 Tier A 探针会在播放期按下 IsPlaying 闸门停止，仍不抢带宽）。</summary>
	public static bool UseMeasuredStreamProbe = true;
	// 注入式精确模式的输入源（由 LoadChannelCore 创建、Stop 时释放）
	private MeasuredMediaInput _measuredInput;

	/// <summary>HLS 本地计数代理开关：零额外带宽优先方案，但为达成零额外带宽必须【劫持 VLC 的播放 URL】
	/// 让 VLC 通过本机代理回源。代理回源使用自有 HttpWebRequest，不透明（不携带 VLC 原有鉴权/自定义头/Cookie、
	/// 不支持 #EXT-X-BYTERANGE 多段复用、无 keep-alive），对防盗链/byte-range/特殊协议源会回源失败或数据错位 →
	/// 直接导致 VLC “无法播放预览”。【默认关闭】以优先保证播放功能；HLS 链接速度改由 HlsLinkProbe 独立分片探针
	/// 兜底（额外带宽但完全不侵入播放路径，探针失败也不影响播放）。确需零额外带宽时再手动置 true 并针对源调试。</summary>
	public static bool UseHlsCountingProxy = false;
	/// <summary>HLS 智能分片探针开关：兜底方案。代理不可用时独立解析 m3u8 并顺序下载分片计数，
	/// 会额外拉一条流，因此优先级低于代理。默认 true 作为可靠兜底。</summary>
	public static bool UseHlsProbeFallback = true;
	/// <summary>HLS 分片探针暖机时长(ms)：播放开始后的这段时间内探针持续拉分片以测出有代表性的链接速度，
	/// 超时后自动停止第二条拉流连接，把带宽全部让给播放，避免 1080p 等高码率源因探针抢带宽而卡顿。</summary>
	private const int HlsProbeWarmupMs = 6000;
	private HlsCountingProxy _hlsProxy;
	private HlsLinkProbe _hlsProbe;
	// HLS 探针自动停表（暖机结束后释放带宽，见 ScheduleHlsProbeAutoStop）
	private System.Threading.Timer _hlsProbeAutoStop;

	/// <summary>判断是否为 HLS（.m3u8/.m3u）直播清单。HLS 下 VLC 需自行分片回源，
	/// 单连接注入无法覆盖分片字节，故 B 模式跳过、回退常规路径。</summary>
		public static bool IsHlsUri(Uri uri)
		{
			if (uri == null) return false;
			string s = uri.AbsoluteUri;
			return IsHlsUrl(s);
		}

		/// <summary>判断某 URL 是否为 HLS 清单（字符串版，供主窗体/静态方法使用）。</summary>
		public static bool IsHlsUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url)) return false;
			return url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
				|| url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)
				|| url.IndexOf(".m3u8?", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>判断某 URL 当前是否走 B 注入式精确模式（单连接、零额外带宽）。
		/// 供主窗体决定是否跳过 Tier A 独立探针——B 已用单连接精确计数，无需再开第二个连接。</summary>
		public static bool IsMeasuredInjectionEnabledFor(string url)
		{
			if (!UseMeasuredStreamProbe || string.IsNullOrWhiteSpace(url)) return false;
			if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				&& !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
			// HLS 下 VLC 需自行分片回源，单连接注入无法覆盖分片字节，回退常规路径（此时由 HLS 代理/探针测量）
			if (IsHlsUrl(url)) return false;
			return true;
		}

		/// <summary>判断某 URL 当前是否走 HLS 本地计数代理（零额外带宽）。
		/// 供主窗体决定是否跳过 Tier A 独立探针——代理已随 VLC 消费路径计数，无需再开第二个连接。</summary>
		public static bool IsHlsProxyActiveFor(string url)
		{
			if (!UseHlsCountingProxy || string.IsNullOrWhiteSpace(url)) return false;
			if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				&& !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
			return IsHlsUrl(url);
		}

		/// <summary>当前频道是否已有 HLS 测量在运行（本地代理或分片探针）。
		/// 主窗体据此停止 Tier A 独立探针，避免对同一条 HLS 流重复建立额外连接。</summary>
		public bool IsHlsMeasurementActive => _hlsProxy != null || _hlsProbe != null;
		/// <summary>HLS 分片探针当前平滑速率（字节/s），稳定播放期持续有效。未运行则为 0。</summary>
		public float HlsProbeBps => _hlsProbe != null ? _hlsProbe.CurrentBps : 0f;
		/// <summary>HLS 分片探针是否在运行（持续测速源可用）。</summary>
		public bool IsHlsProbeRunning => _hlsProbe != null && _hlsProbe.IsRunning;


	/// <summary>停止并释放 Tier B 注入式输入（须在 Media 释放之后调用）。</summary>
	private void StopMeasuredInput()
	{
		var mi = _measuredInput;
		_measuredInput = null;
		if (mi != null)
		{
			try { mi.Dispose(); }
			catch { }
		}
	}

	private void StartHlsProbe(string m3u8Url)
	{
		StopHlsProbe();
		if (!UseHlsProbeFallback || string.IsNullOrWhiteSpace(m3u8Url)) return;
		try
		{
			_hlsProbe = new HlsLinkProbe();
			_hlsProbe.Start(m3u8Url);
			// 暖机后自动停探针：播放稳定后停掉第二条拉流连接，把带宽全留给播放，
			// 避免 1080p 等高码率源因探针抢带宽导致卡顿（零带宽测速仅直链注入式可用）。
			ScheduleHlsProbeAutoStop();
		}
		catch { _hlsProbe = null; }
	}

	/// <summary>启动一次性计时器：HlsProbeWarmupMs 后自动停止 HLS 分片探针，
	/// 释放被探针占用的第二条连接带宽，根治高码率源播放卡顿。换台/停止/重载会先 Dispose 旧计时器。</summary>
	private void ScheduleHlsProbeAutoStop()
	{
		try { _hlsProbeAutoStop?.Dispose(); }
		catch { }
		_hlsProbeAutoStop = new System.Threading.Timer(delegate
		{
			// 已在播放期：停掉竞争探针，释放带宽
			StopHlsProbe();
		}, null, HlsProbeWarmupMs, System.Threading.Timeout.Infinite);
	}

	private void StopHlsProbe()
	{
		try { _hlsProbeAutoStop?.Dispose(); }
		catch { }
		_hlsProbeAutoStop = null;
		var p = _hlsProbe;
		_hlsProbe = null;
		if (p != null)
		{
			try { p.Dispose(); }
			catch { }
		}
	}

			/// <summary>启动持续型 HLS 分片探针（不自动停）：作为稳定播放期持续测速源，
		/// 对抗 VLC 内部统计归零与本地代理按需转发导致的字节计数脉冲冻结。
		/// 代价是与播放器各占一条拉流连接（双拉流），IPTV 中低码率直播下带宽影响可忽略。</summary>
		private void StartHlsProbeContinuous(string m3u8Url)
		{
			StopHlsProbe();
			if (string.IsNullOrWhiteSpace(m3u8Url)) return;
			try
			{
				_hlsProbe = new HlsLinkProbe();
				_hlsProbe.Start(m3u8Url);
				// 注意：不调度 ScheduleHlsProbeAutoStop —— 持续运行以保证稳定播放期链接速度不归零
			}
			catch { _hlsProbe = null; }
		}

		private void StopHlsProxy()
	{
		var px = _hlsProxy;
		_hlsProxy = null;
		if (px != null)
		{
			try { px.Dispose(); }
			catch { }
		}
	}

	private void StopHlsResources()
	{
		StopHlsProxy();
		StopHlsProbe();
	}


	/// <summary>重置 VLC 实时速率采样基线。必须在停止播放 / 开始新频道加载时调用，
	/// 否则新流会继承上一条流的 readBytes 基准与 _lastLiveRate，导致首帧误判或短暂归零。</summary>
	private void ResetStatSampler()
	{
		_lastStatReadBytes = 0L;
		_lastStatTickMs = 0L;
		_lastLiveRate = 0f;
	}

	public bool TryGetPlayerStats(out long readBytes, out float inputBitrate, out int decodedFrames, out int displayedFrames)
	{
		readBytes = 0L;
		inputBitrate = 0f;
		decodedFrames = 0;
		displayedFrames = 0;
		if (_engine == PreviewEngine.MPV)
		{
			return _mpv != null && _mpv.TryGetPlayerStats(out readBytes, out inputBitrate, out decodedFrames, out displayedFrames);
		}
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
			// Tier B 真正形态：零额外带宽注入式精确计数，覆盖 LibVLC 的 ReadBytes，
			// VLC 实际消费的字节由 MeasuredMediaInput 精确统计，稳定播放后亦持续增长。
			if (UseMeasuredStreamProbe && _measuredInput != null)
				readBytes = _measuredInput.TotalBytesRead;
			// HLS 方案：本地计数代理 / 智能分片探针均走 IO 层真实字节，覆盖 LibVLC 对 HLS 统计不全的问题。
			else if (_hlsProxy != null)
				readBytes = _hlsProxy.TotalBytesRead;
			else if (_hlsProbe != null)
				readBytes = _hlsProbe.TotalBytesRead;
			// LibVLC 实时"链接速度"（字节/s）：
			// 1) 主信号 readBytes 增量采样（io 累计接收字节，加载与播放全程持续增长时最准）；
			// 2) 辅助 DemuxBitrate（加载缓冲期 LibVLC 填充良好，动态明显）；
			// 3) 二者皆 0（LibVLC 在稳定播放后常冻结/归零统计）→ 沿用上一帧实测速度，
			//    避免突变为 0 误导（稳态传输速率本应基本恒定）。
			float liveRate = 0f;
			long nowMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (_lastStatTickMs > 0L && readBytes > _lastStatReadBytes)
			{
				long dM = nowMs - _lastStatTickMs;
				if (dM > 0L)
				{
					float inst = (float)(readBytes - _lastStatReadBytes) / (float)dM * 1000f; // bytes/s
					if (inst > 0f) liveRate = inst;
				}
			}
			_lastStatReadBytes = readBytes;
			_lastStatTickMs = nowMs;
			if (liveRate == 0f && stats.DemuxBitrate > 0f)
				liveRate = stats.DemuxBitrate; // 加载期 demux 吞吐，动态
			// 播放期统计冻结/归零时，沿用上一帧速度（稳态速度本应基本恒定）
			if (liveRate == 0f && _lastLiveRate > 0f)
				liveRate = _lastLiveRate;
			_lastLiveRate = liveRate;
			inputBitrate = liveRate; // 字节/s
			decodedFrames = stats.DecodedVideo;
			displayedFrames = stats.DisplayedPictures;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool TryGetBufferDuration(out float seconds, float knownBps = 0f)
	{
		seconds = 0f;
		if (_engine == PreviewEngine.MPV)
		{
			return _mpv != null && _mpv.TryGetBufferDuration(out seconds);
		}
		// VLC (LibVLCSharp) 的 MediaStats 不直接提供缓冲时长。
		// 优先方案：用已知实时传输比特率 knownBps(bps) 估算——累计读取字节 ReadBytes 按速率
		// 折算的可播放总秒数减去已播放秒数，即为缓冲领先量（等同 MPV 的 demuxer-cache-duration）。
		// 该法依赖 StatusDisplayTick 实时算出的 _smoothedBps，故在直播下稳定可用。
		if (!IsAvailable || _mediaPlayer == null || _switchingCount > 0)
		{
			return false;
		}
		try
		{
			Media media = _mediaPlayer.Media;
			if (media == null) return false;
			MediaStats stats = media.Statistics;

			// 累计已读取字节：HLS 下 LibVLC 的 ReadBytes 不可靠，改用本地代理/分片探针的真实下载字节
			long totalRead = stats.ReadBytes;
			if (_hlsProxy != null) totalRead = _hlsProxy.TotalBytesRead;
			else if (_hlsProbe != null) totalRead = _hlsProbe.TotalBytesRead;

			// 读前瞻缓冲 = 已读取但尚未解封装的字节（网络读取领先于 demux，差值即缓冲领先量），
			// 与播放位置/直播时间轴无关，对直播流也能稳定给出正缓冲时长
			// （旧方案用 Time 减法：直播下 totalPlayableSec≈playedSec → 恒为 0 → 一直显示 “—”）。
			long demuxed = stats.DemuxReadBytes;
			long bufferedBytes = totalRead - demuxed;
			if (bufferedBytes < 0L) bufferedBytes = 0L;

			// 速率：优先已知实时速率 knownBps(bps)，其次 DemuxBitrate/InputBitrate(字节/s→×8)
			double rateBps = 0.0;
			if (knownBps > 0f) rateBps = knownBps;
			else if (stats.DemuxBitrate > 0f) rateBps = stats.DemuxBitrate * 8.0;
			else if (stats.InputBitrate > 0f) rateBps = stats.InputBitrate * 8.0;

			if (rateBps > 0.0 && bufferedBytes > 0L)
			{
				double bufSec = (bufferedBytes * 8.0) / rateBps; // bytes*8 / bps = 秒
				seconds = (float)bufSec;
				if (seconds < 0f) seconds = 0f;
				if (seconds > 300f) seconds = 300f;
				return seconds > 0f;
			}

			// 兜底：已知实时速率存在时，用「累计可播放总秒数 − 已播放秒数」估算（点播/非直播更贴切）
			if (knownBps > 0f && totalRead > 0L)
			{
				double totalPlayableSec = (totalRead * 8.0) / (double)knownBps;
				double playedSec = (_mediaPlayer.Time > 0 ? _mediaPlayer.Time / 1000.0 : 0);
				seconds = (float)(totalPlayableSec - playedSec);
				if (seconds < 0f) seconds = 0f;
				if (seconds > 300f) seconds = 300f;
				return seconds > 0f;
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>
	/// 丢帧数：解码但未正常显示的帧数。VLC 用 DecodedVideo − DisplayedPictures 估算；
	/// MPV 用 frame-drop-count + decoder-frame-drop-count。参考 YouTube Stats for nerds。
	/// </summary>
	public bool TryGetDroppedFrames(out int droppedFrames, out int totalFrames)
	{
		droppedFrames = 0;
		totalFrames = 0;
		if (!IsAvailable || _switchingCount > 0)
		{
			return false;
		}
		try
		{
			if (_mpv != null)
			{
				return _mpv.TryGetDroppedFrames(out droppedFrames, out totalFrames);
			}
			if (_mediaPlayer != null && _mediaPlayer.Media != null)
			{
				MediaStats stats = _mediaPlayer.Media.Statistics;
				int decoded = stats.DecodedVideo;
				int displayed = stats.DisplayedPictures;
				droppedFrames = Math.Max(0, decoded - displayed);
				totalFrames = decoded;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>
	/// 直播延迟（你看到的画面相对直播现场的落后秒数）。MPV 用 demuxer-cache-duration；
	/// VLC 下精确端到端延迟不可得，以缓冲领先量（TryGetBufferDuration 同义）近似，对直播流二者高度相关。
	/// </summary>
	public bool TryGetLiveLatency(out float seconds)
	{
		seconds = 0f;
		if (!IsAvailable || _switchingCount > 0)
		{
			return false;
		}
		try
		{
			if (_mpv != null)
			{
				return _mpv.TryGetLiveLatency(out seconds);
			}
			return TryGetBufferDuration(out seconds, _lastLiveRate);
		}
		catch
		{
		}
		return false;
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
		// 先尝试用 ExternalPlayerHelper 自动选择最佳播放器
		if (!string.IsNullOrWhiteSpace(_currentUrl))
		{
			ExternalPlayerHelper.PlayerInfo best = ExternalPlayerHelper.FindBestPlayer();
			PlayerLogger.Write("EXT", $"自动选择播放器: {best.DisplayName} | type={best.Type} | path={best.Path}");
			if (ExternalPlayerHelper.Play(_currentUrl, best))
			{
				return;
			}
			PlayerLogger.Write("EXT", "外部播放器启动失败，回退到事件委托");
		}
		// 如果 ExternalPlayerHelper 失败，回退到原始事件委托（由主窗体处理）
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
		bool barIsDark = DrawingUtils.IsDarkColor(theme.Surface);
		Color sliderTrack = barIsDark ? Color.FromArgb(70, 70, 80) : Color.FromArgb(210, 210, 215);
		Color sliderThumb = barIsDark ? Color.White : Color.FromArgb(60, 60, 70);
		if (_volumeSlider != null)
		{
			_volumeSlider.TrackColor = sliderTrack;
			_volumeSlider.FillColor = theme.Primary;
			_volumeSlider.ThumbColor = sliderThumb;
			_volumeSlider.Invalidate();
		}
		if (_progressBar != null)
		{
			_progressBar.TrackColor = sliderTrack;
			_progressBar.FillColor = theme.Primary;
			_progressBar.ThumbColor = sliderThumb;
			_progressBar.Invalidate();
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
		// 引擎下拉框：圆角、无边框、主题色适配，消除系统 ComboBox 漏白
		if (_engineCombo != null)
		{
			_engineCombo.BackColor = theme.Surface;
			_engineCombo.ForeColor = theme.TextPrimary;
			_engineCombo.BorderColor = theme.Border;
			_engineCombo.FocusBorderColor = theme.Primary;
			_engineCombo.ItemBackColor = theme.Surface;
			_engineCombo.ItemSelectedBackColor = theme.Primary;
			_engineCombo.ItemHoverBackColor = barIsDark
				? Color.FromArgb(Math.Min(255, theme.Surface.R + 18), Math.Min(255, theme.Surface.G + 18), Math.Min(255, theme.Surface.B + 22))
				: Color.FromArgb(Math.Max(0, theme.Surface.R - 12), Math.Max(0, theme.Surface.G - 12), Math.Max(0, theme.Surface.B - 12));
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
							Point lastPos = player._lastLeftDownPos;
							bool isDouble = player._lastLeftDownTime != DateTime.MinValue
								&& (now - player._lastLeftDownTime).TotalMilliseconds <= dctime
								&& Math.Abs(screenPos.X - lastPos.X) <= dcsize.Width
								&& Math.Abs(screenPos.Y - lastPos.Y) <= dcsize.Height;
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
			// 显式释放内嵌外部播放器宿主（虽已加入 Controls 会随父控件递归释放，这里兜底确保外部进程/管道/定时器被回收）
			_embedHost?.Dispose();
			_embedHost = null;
			if (_progressTimer != null)
			{
				_progressTimer.Stop();
				_progressTimer.Dispose();
				_progressTimer = null;
			}
			if (_spinnerTimer != null)
			{
				_spinnerTimer.Stop();
				_spinnerTimer.Dispose();
				_spinnerTimer = null;
			}
			_spinnerPanel?.Dispose();
			_spinnerPanel = null;
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
			// 关闭时先停止 HLS 持续探针：上一轮引入的持续型探针若不停止会永久后台拉流（线程+Socket 泄漏）
			try
			{
				StopHlsProbe();
			}
			catch
			{
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
