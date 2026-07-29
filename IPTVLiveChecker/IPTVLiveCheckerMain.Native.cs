using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain
{
	private static IntPtr SafeGetWindowLongPtr(IntPtr hWnd, int nIndex)
	{
		if (IntPtr.Size == 8)
		{
			return GetWindowLongPtr(hWnd, nIndex);
		}
		return (IntPtr)GetWindowLong(hWnd, nIndex);
	}

	private static IntPtr SafeSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
	{
		if (IntPtr.Size == 8)
		{
			return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
		}
		return (IntPtr)SetWindowLong(hWnd, nIndex, (int)dwNewLong);
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

	private static void CenterForm(Form form, Form owner = null)
	{
		Rectangle screen = Screen.PrimaryScreen.WorkingArea;
		int centerX = screen.X + (screen.Width - form.Width) / 2;
		int centerY = screen.Y + (screen.Height - form.Height) / 2;
		if (owner != null)
		{
			centerX = owner.Left + (owner.Width - form.Width) / 2;
			centerY = owner.Top + (owner.Height - form.Height) / 2;
		}
		if (centerX < screen.X)
		{
			centerX = screen.X;
		}
		if (centerY < screen.Y)
		{
			centerY = screen.Y;
		}
		if (centerX + form.Width > screen.X + screen.Width)
		{
			centerX = screen.X + screen.Width - form.Width;
		}
		if (centerY + form.Height > screen.Y + screen.Height)
		{
			centerY = screen.Y + screen.Height - form.Height;
		}
		form.Location = new Point(centerX, centerY);
	}

	private static void SetFormDarkModeTitleBar(Form form, bool isDark)
	{
		if (form == null)
		{
			return;
		}
		int darkMode = (isDark ? 1 : 0);
		try
		{
			DwmSetWindowAttribute(form.Handle, 20, ref darkMode, 4);
			DwmSetWindowAttribute(form.Handle, 19, ref darkMode, 4);
		}
		catch
		{
		}
	}

	private static void SetWindowRoundedCorners(IntPtr hwnd, int preference)
	{
		try
		{
			DwmSetWindowAttribute(hwnd, 33, ref preference, 4);
		}
		catch
		{
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("user32.dll")]
	private static extern IntPtr WindowFromPoint(Point pt);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern int GetScrollInfo(IntPtr hWnd, int nBar, ref SCROLLINFO lpsi);

	private void StartMouseHook()
	{
		if (_mouseHook != IntPtr.Zero)
		{
			return;
		}
		_mouseHookProc = MouseHookCallback;
		using Process curProcess = Process.GetCurrentProcess();
		using ProcessModule curModule = curProcess.MainModule;
		_mouseHook = SetWindowsHookEx(14, _mouseHookProc, GetModuleHandle(curModule.ModuleName), 0u);
	}

	private void StopMouseHook()
	{
		if (_mouseHook != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_mouseHook);
			_mouseHook = IntPtr.Zero;
		}
	}

	private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && wParam == (IntPtr)517)
		{
			POINT pt = (POINT)Marshal.PtrToStructure(lParam, typeof(POINT));
			IntPtr hWnd = WindowFromPoint(new Point(pt.x, pt.y));
			if (hWnd != IntPtr.Zero)
			{
				GetWindowThreadProcessId(hWnd, out var pid);
				Process playerProc = null;
				if (_runningPlayer != null && !_runningPlayer.HasExited && _runningPlayer.Id == (int)pid)
				{
					playerProc = _runningPlayer;
				}
				else if (previewProcess != null && !previewProcess.HasExited && previewProcess.Id == (int)pid)
				{
					playerProc = previewProcess;
				}
				if (playerProc != null)
				{
					BeginInvoke((Action)delegate
					{
						ShowPlayerContextMenu(new Point(pt.x, pt.y));
					});
				}
			}
		}
		return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
	}

	private void ShowPlayerContextMenu(Point screenPoint)
	{
		try
		{
			ContextMenuStrip obj = new ContextMenuStrip
			{
				Font = GetFont(SF(9f)),
				Renderer = new RoundedMenuRenderer(theme),
				BackColor = Color.Transparent,
				ForeColor = theme.TextPrimary
			};
			ToolStripMenuItem showInfoItem = new ToolStripMenuItem("显示流媒体信息");
			showInfoItem.Checked = _showStreamInfoOverlay;
			showInfoItem.Click += delegate
			{
				_showStreamInfoOverlay = !_showStreamInfoOverlay;
				showInfoItem.Checked = _showStreamInfoOverlay;
				if (_showStreamInfoOverlay)
				{
					StartStreamInfoOverlay();
				}
				else
				{
					StopStreamInfoOverlay();
				}
			};
			obj.Items.Add(showInfoItem);
			obj.Show(screenPoint);
		}
		catch
		{
		}
	}
}
