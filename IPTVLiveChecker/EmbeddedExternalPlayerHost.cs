using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
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

	// MPV 控制用：通过 --input-ipc-server 命名管道转发音量/暂停/进度等指令
	private string _mpvPipeName;
	private NamedPipeClientStream _pipe;
	private StreamWriter _pipeWriter;
	private StreamReader _pipeReader;
	private readonly object _pipeLock = new object();
	private bool _pipeReady;
	private int _reqId = 1;

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
		DisposePipe();
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
			// MPV 原生嵌入：直接把本面板句柄交给它绘制；并开 IPC 命名管道转发音量/进度控制
			_mpvPipeName = "iptv_mpv_" + Guid.NewGuid().ToString("N").Substring(0, 12);
			psi.Arguments = $"--wid={Handle.ToInt64()} --no-border --no-osd-bar --input-ipc-server=\\\\.\\pipe\\\\{_mpvPipeName} \"{url}\"";
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
		// MPV 通过 --wid 直接绘制到本面板，无需挂靠；进程存活后尝试连接 IPC 管道以便转发控制
		if (_mpvMode)
		{
			if (_process != null && _process.HasExited)
			{
				RaiseFailed("MPV 进程异常退出，无法内嵌播放");
				return;
			}
			if (!_pipeReady && !TryConnectPipe())
			{
				// 最多约 6 秒仍未连上 IPC：嵌入仍可用，但控制不可用
				if (_pollCount > 40)
				{
					PlayerLogger.Write("EMBED", "MPV 内嵌成功，但 IPC 未连接（控制不可用）");
					_statusLabel.Visible = false;
					_pollTimer.Stop();
				}
				return;
			}
			_pipeReady = true;
			_statusLabel.Visible = false;
			_pollTimer.Stop();
			PlayerLogger.Write("EMBED", "MPV 内嵌成功（--wid 绘制 + IPC 控制）");
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

	// ---- MPV IPC 控制转发（仅 MPV 模式可用） ----

	/// <summary>MPV 引擎是否既内嵌成功又已连上 IPC，可接收控制指令。</summary>
	public bool IsMpvControllable => _mpvMode && _pipeReady;

	private void DisposePipe()
	{
		_pipeReady = false;
		try { _pipeWriter?.Dispose(); } catch { }
		try { _pipeReader?.Dispose(); } catch { }
		try { _pipe?.Dispose(); } catch { }
		_pipe = null;
		_pipeWriter = null;
		_pipeReader = null;
		_mpvPipeName = null;
	}

	private bool TryConnectPipe()
	{
		if (_pipe != null) return _pipe.IsConnected;
		if (string.IsNullOrEmpty(_mpvPipeName)) return false;
		NamedPipeClientStream p = null;
		try
		{
			p = new NamedPipeClientStream(".", _mpvPipeName, PipeDirection.InOut);
			p.Connect(1000);
			p.ReadTimeout = 800;
			_pipe = p;
			_pipeWriter = new StreamWriter(_pipe) { AutoFlush = true };
			_pipeReader = new StreamReader(_pipe);
			return true;
		}
		catch
		{
			try { p?.Dispose(); } catch { }
			return false;
		}
	}

	private void IpcSend(string json)
	{
		lock (_pipeLock)
		{
			try { _pipeWriter?.WriteLine(json); } catch { }
		}
	}

	private string IpcQuery(string json)
	{
		lock (_pipeLock)
		{
			if (_pipe == null || !_pipe.IsConnected || _pipeWriter == null || _pipeReader == null) return null;
			try
			{
				_pipeWriter.WriteLine(json);
				return _pipeReader.ReadLine();
			}
			catch
			{
				return null;
			}
		}
	}

	private int NextReqId() => Interlocked.Increment(ref _reqId);

	public void MpvSetVolume(int volume)
	{
		volume = Math.Max(0, Math.Min(100, volume));
		if (!IsMpvControllable) return;
		IpcSend("{\"command\":[\"set_property\",\"volume\"," + volume.ToString(CultureInfo.InvariantCulture) + "]}");
	}

	public void MpvSetMuted(bool muted)
	{
		if (!IsMpvControllable) return;
		IpcSend("{\"command\":[\"set_property\",\"mute\"," + (muted ? "true" : "false") + "]}");
	}

	public void MpvSetPaused(bool paused)
	{
		if (!IsMpvControllable) return;
		IpcSend("{\"command\":[\"set_property\",\"pause\"," + (paused ? "true" : "false") + "]}");
	}

	public void MpvSeek(double seconds)
	{
		if (!IsMpvControllable) return;
		IpcSend("{\"command\":[\"seek\"," + seconds.ToString("0.###", CultureInfo.InvariantCulture) + ",\"absolute\"]}");
	}

	public double? MpvGetTimePos()
	{
		if (!IsMpvControllable) return null;
		return ParseDoubleData(IpcQuery("{\"command\":[\"get_property\",\"time-pos\"],\"request_id\":" + NextReqId() + "}"));
	}

	public double? MpvGetDuration()
	{
		if (!IsMpvControllable) return null;
		return ParseDoubleData(IpcQuery("{\"command\":[\"get_property\",\"duration\"],\"request_id\":" + NextReqId() + "}"));
	}

	public bool? MpvIsPaused()
	{
		if (!IsMpvControllable) return null;
		return ParseBoolData(IpcQuery("{\"command\":[\"get_property\",\"pause\"],\"request_id\":" + NextReqId() + "}"));
	}

	private static double? ParseDoubleData(string json)
	{
		if (string.IsNullOrEmpty(json)) return null;
		int idx = json.IndexOf("\"data\"", StringComparison.Ordinal);
		if (idx < 0) return null;
		int colon = json.IndexOf(':', idx);
		if (colon < 0) return null;
		int start = colon + 1;
		while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
		if (start >= json.Length || json[start] == 'n') return null; // null
		int end = start;
		while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
		string num = json.Substring(start, end - start).Trim();
		return double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : (double?)null;
	}

	private static bool? ParseBoolData(string json)
	{
		if (string.IsNullOrEmpty(json)) return null;
		int idx = json.IndexOf("\"data\"", StringComparison.Ordinal);
		if (idx < 0) return null;
		int colon = json.IndexOf(':', idx);
		if (colon < 0) return null;
		int start = colon + 1;
		while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
		if (start >= json.Length) return null;
		if (json[start] == 't') return true;
		if (json[start] == 'f') return false;
		return null;
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
