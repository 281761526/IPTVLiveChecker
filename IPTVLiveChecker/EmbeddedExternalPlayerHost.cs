using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 将外部播放器（PotPlayer / MPV 等）的视频输出嵌入到本预览面板内。
/// - MPV：使用官方 <c>--wid=&lt;HWND&gt;</c> 参数直接在其父窗口句柄上绘制（最稳）。
/// - PotPlayer 等无官方嵌入接口：用 SetParent 把其主窗口挂靠到本面板，并改写窗口样式、跟随缩放（黑科技，依赖其窗口结构）。
/// 若嵌入失败（找不到窗口 / 进程异常退出），触发 <see cref="EmbedFailed"/>，由调用方决定降级为独立窗口打开。
/// </summary>
internal class EmbeddedExternalPlayerHost : Panel
{
	// ---- Win32 常量 ----
	private const int GWL_STYLE = -16;
	private const int GWL_EXSTYLE = -20;
	private const uint WS_CHILD = 0x40000000;
	private const uint WS_VISIBLE = 0x10000000;
	private const uint WS_POPUP = 0x80000000;
	private const uint WS_CAPTION = 0x00C00000;
	private const uint WS_THICKFRAME = 0x00040000;
	private const uint WS_SYSMENU = 0x00080000;
	private const uint WS_BORDER = 0x00800000;
	private const uint WS_DLGFRAME = 0x00400000;
	private const uint WS_EX_CLIENTEDGE = 0x00000200;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_NOACTIVATE = 0x0010;
	private const uint SWP_FRAMECHANGED = 0x0020;
	private const uint SWP_SHOWWINDOW = 0x0040;

	[DllImport("user32.dll")]
	private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	private Process _process;
	private IntPtr _embeddedHwnd = IntPtr.Zero;
	private System.Windows.Forms.Timer _pollTimer;
	private Label _statusLabel;
	private string _currentUrl;
	private int _pollCount;
	private bool _mpvMode;
	private bool _active;
	private bool _failed;

	/// <summary>嵌入失败时触发（reason 为原因），调用方通常降级为独立窗口打开。</summary>
	public event EventHandler<string> EmbedFailed;

	public EmbeddedExternalPlayerHost()
	{
		BackColor = System.Drawing.Color.Black;
		_statusLabel = new Label
		{
			Text = "",
			ForeColor = System.Drawing.Color.FromArgb(200, 200, 205),
			BackColor = System.Drawing.Color.Transparent,
			TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
			Font = new System.Drawing.Font("Segoe UI", 10f),
			Dock = DockStyle.Fill,
			Visible = false
		};
		Controls.Add(_statusLabel);
		_pollTimer = new System.Windows.Forms.Timer { Interval = 150 };
		_pollTimer.Tick += PollTick;
		Resize += delegate { RepositionEmbedded(); };
	}

	/// <summary>停止当前外部播放器并清理。</summary>
	public void Stop()
	{
		_active = false;
		_failed = false;
		_pollTimer?.Stop();
		try
		{
			if (_process != null && !_process.HasExited)
			{
				// 先尝试关闭其主窗口，避免留下孤儿进程
				if (_process.MainWindowHandle != IntPtr.Zero)
				{
					PostMessage(_process.MainWindowHandle, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
				}
				_process.CloseMainWindow();
				if (!_process.WaitForExit(800))
				{
					_process.Kill();
				}
			}
		}
		catch
		{
		}
		_process = null;
		_embeddedHwnd = IntPtr.Zero;
		_mpvMode = false;
		if (_statusLabel != null) _statusLabel.Visible = false;
	}

	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	/// <summary>在预览面板内嵌入播放指定 URL。player 应优先为 PotPlayer 或 MPV。</summary>
	public void Play(string url, ExternalPlayerHelper.PlayerInfo player)
	{
		Stop();
		if (player == null || string.IsNullOrWhiteSpace(player.Path) || string.IsNullOrWhiteSpace(url))
		{
			RaiseFailed("未找到可用的外部播放器");
			return;
		}
		_currentUrl = url;
		_mpvMode = (player.Type == ExternalPlayerHelper.PlayerType.MPV);
		var psi = new ProcessStartInfo
		{
			FileName = player.Path,
			UseShellExecute = false,
			CreateNoWindow = false
		};
		if (_mpvMode)
		{
			// MPV 原生嵌入：直接把本面板句柄交给它绘制
			psi.Arguments = $"--wid={Handle.ToInt64()} --no-border --no-osd-bar \"{url}\"";
		}
		else if (player.Type == ExternalPlayerHelper.PlayerType.PotPlayer)
		{
			psi.Arguments = $"\"{url}\" /play /nomsg /nologo";
		}
		else
		{
			psi.Arguments = $"\"{url}\"";
		}
		PlayerLogger.Write("EMBED", $"启动内嵌播放器: {player.DisplayName} | mode={( _mpvMode ? "mpv --wid" : "setparent")} | args={psi.Arguments}");
		try
		{
			_process = Process.Start(psi);
		}
		catch (Exception ex)
		{
			PlayerLogger.WriteError("EMBED", ex);
			RaiseFailed("启动外部播放器失败：" + ex.Message);
			return;
		}
		if (_process == null)
		{
			RaiseFailed("外部播放器未启动");
			return;
		}
		_active = true;
		_failed = false;
		_statusLabel.Text = $"正在内嵌 {player.DisplayName} 播放…";
		_statusLabel.Visible = true;
		_pollCount = 0;
		_pollTimer.Start();
	}

	private void PollTick(object sender, EventArgs e)
	{
		if (!_active || _failed) return;
		_pollCount++;
		// MPV 通过 --wid 直接绘制到本面板，无需挂靠；只要进程还活着即视为成功
		if (_mpvMode)
		{
			if (_process != null && _process.HasExited)
			{
				RaiseFailed("MPV 进程异常退出，无法内嵌播放");
				return;
			}
			_statusLabel.Visible = false;
			_pollTimer.Stop();
			PlayerLogger.Write("EMBED", "MPV 内嵌成功（--wid 直接绘制）");
			return;
		}
		IntPtr hwnd = FindPlayerWindow();
		if (hwnd != IntPtr.Zero)
		{
			Reparent(hwnd);
			_embeddedHwnd = hwnd;
			_statusLabel.Visible = false;
			_pollTimer.Stop();
			PlayerLogger.Write("EMBED", $"PotPlayer 内嵌成功 hwnd=0x{hwnd.ToInt64():X}");
			return;
		}
		// 约 4.5 秒仍未找到窗口，判定嵌入失败
		if (_pollCount > 30)
		{
			_pollTimer.Stop();
			RaiseFailed("未能将外部播放器嵌入预览窗（已回退为独立窗口）");
		}
	}

	private IntPtr FindPlayerWindow()
	{
		if (_process == null || _process.HasExited) return IntPtr.Zero;
		int pid = _process.Id;
		IntPtr found = IntPtr.Zero;
		EnumWindows((hwnd, _) =>
		{
			GetWindowThreadProcessId(hwnd, out int wpid);
			if (wpid == pid && IsWindowVisible(hwnd))
			{
				found = hwnd;
				return false; // 取第一个可见顶级窗口即可
			}
			return true;
		}, IntPtr.Zero);
		return found;
	}

	private void Reparent(IntPtr hwnd)
	{
		try
		{
			uint style = (uint)GetWindowLong(hwnd, GWL_STYLE);
			// 去掉弹出/标题/边框/系统菜单等，改为子窗口（全部按 uint 计算，避免常量溢出 int）
			uint remove = WS_POPUP | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_BORDER | WS_DLGFRAME;
			style = (style & ~remove) | WS_CHILD | WS_VISIBLE;
			SetWindowLong(hwnd, GWL_STYLE, (int)style);
			SetWindowLong(hwnd, GWL_EXSTYLE, 0);
			SetParent(hwnd, Handle);
			// 强制应用新样式并随父容器重绘
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
			RepositionEmbedded();
		}
		catch (Exception ex)
		{
			PlayerLogger.WriteError("EMBED", ex);
			RaiseFailed("嵌入窗口样式修改失败：" + ex.Message);
		}
	}

	private void RepositionEmbedded()
	{
		if (_embeddedHwnd == IntPtr.Zero || !IsHandleCreated) return;
		try
		{
			MoveWindow(_embeddedHwnd, 0, 0, ClientSize.Width, ClientSize.Height, true);
		}
		catch
		{
		}
	}

	private void RaiseFailed(string reason)
	{
		if (_failed) return;
		_failed = true;
		_active = false;
		_pollTimer?.Stop();
		PlayerLogger.Write("EMBED", "嵌入失败：" + reason);
		EmbedFailed?.Invoke(this, reason);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_pollTimer?.Stop();
			Stop();
			_statusLabel?.Dispose();
		}
		base.Dispose(disposing);
	}
}
