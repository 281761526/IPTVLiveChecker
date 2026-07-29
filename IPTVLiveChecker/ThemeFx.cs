using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 主题"动效 + 毛玻璃"渲染层。
/// 仅当外部主题声明了 AnimationType / GlassEnabled 时才激活，内置主题不受影响（向后兼容）。
/// </summary>
public static class ThemeFx
{
	private static readonly Dictionary<IntPtr, ThemeFxBackground> _bgTable = new Dictionary<IntPtr, ThemeFxBackground>();
	private static GlobalMouseFilter _mouseFilter;
	private static int _activeMouseDownCount;

		private class GlobalMouseFilter : IMessageFilter
	{
			public bool MouseDown;

			public bool PreFilterMessage(ref Message m)
			{
				const int WM_LBUTTONDOWN = 0x0201;
				const int WM_LBUTTONUP = 0x0202;
				const int WM_RBUTTONDOWN = 0x0204;
				const int WM_RBUTTONUP = 0x0205;
				const int WM_MBUTTONDOWN = 0x0207;
				const int WM_MBUTTONUP = 0x0208;
				switch (m.Msg)
				{
				case WM_LBUTTONDOWN:
				case WM_RBUTTONDOWN:
				case WM_MBUTTONDOWN:
					MouseDown = true;
					Interlocked.Increment(ref _activeMouseDownCount);
					break;
				case WM_LBUTTONUP:
				case WM_RBUTTONUP:
				case WM_MBUTTONUP:
					MouseDown = false;
					Interlocked.Decrement(ref _activeMouseDownCount);
					break;
				}
				return false;
			}
		}

	public static bool IsMouseActive => _activeMouseDownCount > 0;

	public static void RegisterMouseFilter()
	{
		if (_mouseFilter == null)
		{
			_mouseFilter = new GlobalMouseFilter();
			Application.AddMessageFilter(_mouseFilter);
		}
	}

	public static void UnregisterMouseFilter()
	{
		if (_mouseFilter != null)
		{
			Application.RemoveMessageFilter(_mouseFilter);
			_mouseFilter = null;
		}
	}

	/// <summary>把颜色按 alpha 转为半透明玻璃色。</summary>
	public static Color Glass(Color baseColor, int alpha)
	{
		return DrawingUtils.WithAlpha(baseColor, alpha);
	}

	/// <summary>根据主题配置主窗口的动效背景层与毛玻璃。</summary>
	public static void ApplyThemeFx(Form form, AppTheme theme)
	{
		if (form == null || theme == null)
		{
			return;
		}
		if (!form.IsHandleCreated)
		{
			form.HandleCreated += (s, e) => ApplyThemeFx(form, theme);
			return;
		}
		IntPtr handle = form.Handle;
		bool immersive = theme.GlassEnabled || !string.IsNullOrEmpty(theme.AnimationType);
		if (!immersive)
		{
			if (_bgTable.TryGetValue(handle, out ThemeFxBackground old))
			{
				old.Detach();
				if (old.Parent != null)
				{
					old.Parent.Controls.Remove(old);
				}
				old.Dispose();
				_bgTable.Remove(handle);
			}
			EnableBlurBehind(handle, false);
			return;
		}
		if (!_bgTable.TryGetValue(handle, out ThemeFxBackground bg) || bg.IsDisposed)
		{
			bg = new ThemeFxBackground();
			bg.Dock = DockStyle.Fill;
			bg.Enabled = false;
			bg.TabStop = false;
			// 添加到 Form 而非 outerWrap，确保 Z 序最低
			// 这样 outerWrap (Color.Transparent) 会显示动画背景
			form.Controls.Add(bg);
			bg.SendToBack();
			_bgTable[handle] = bg;
			bg.Disposed += (s, e) =>
			{
				if (_bgTable.TryGetValue(handle, out ThemeFxBackground b))
				{
					b.Detach();
					_bgTable.Remove(handle);
				}
			};
			// 窗口拖拽/缩放时暂停动画，防止拖影
			form.ResizeBegin += (s, e) => bg.Pause();
			form.ResizeEnd += (s, e) => bg.Resume();
			// 窗口移动 debounce：停止移动后恢复动画
			System.Windows.Forms.Timer moveTimer = null;
			form.Move += (s, e) =>
			{
				bg.Pause();
				if (moveTimer == null)
				{
					moveTimer = new System.Windows.Forms.Timer { Interval = 200 };
					moveTimer.Tick += (s2, e2) =>
					{
						moveTimer.Stop();
						bg.Resume();
					};
				}
				moveTimer.Stop();
				moveTimer.Start();
			};
		}
		bg.Configure(theme.AnimationType, ResolveStops(theme), theme.AnimationSpeed, theme.GlassBlur, theme.GlassOpacity);
		EnableBlurBehind(handle, theme.GlassBlur);
	}

	/// <summary>从主题解析动效渐变光斑颜色；缺省时由主题主色派生。</summary>
	private static List<Color> ResolveStops(AppTheme theme)
	{
		if (theme.GradientStops != null && theme.GradientStops.Count >= 2)
		{
			return new List<Color>(theme.GradientStops);
		}
		// 暗色主题用 Primary/Accent 直接作为光斑，亮色主题用 Darken 压暗避免过曝
		bool isDark = (0.299 * theme.Bg.R + 0.587 * theme.Bg.G + 0.114 * theme.Bg.B) / 255.0 < 0.5;
		if (isDark)
		{
			return new List<Color>
			{
				theme.Bg,
				theme.Primary,
				theme.Accent,
				theme.BgAlt
			};
		}
		return new List<Color>
		{
			Darken(theme.Bg, 0.5),
			theme.Primary,
			theme.Accent,
			Darken(theme.BgAlt, 0.2)
		};
	}

	private static Color Darken(Color c, double f)
	{
		return DrawingUtils.Darken(c, f);
	}

	// ===== DWM / 毛玻璃（ACCENT_ENABLE_BLURBEHIND）=====

	[DllImport("user32.dll")]
	private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

	public static void EnableBlurBehind(IntPtr hwnd, bool enable)
	{
		try
		{
			AccentPolicy accent = new AccentPolicy
			{
				AccentState = enable ? 3 : 0,
				AccentFlags = enable ? 2 : 0,
				GradientColor = 0x00000000,
				AnimationId = 0
			};
			int size = Marshal.SizeOf(accent);
			IntPtr p = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(accent, p, false);
			WindowCompositionAttributeData d = new WindowCompositionAttributeData
			{
				Attribute = 19,
				Data = p,
				SizeOfData = size
			};
			SetWindowCompositionAttribute(hwnd, ref d);
			Marshal.FreeHGlobal(p);
		}
		catch
		{
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct AccentPolicy
	{
		public int AccentState;
		public int AccentFlags;
		public int GradientColor;
		public int AnimationId;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct WindowCompositionAttributeData
	{
		public int Attribute;
		public IntPtr Data;
		public int SizeOfData;
	}
}

/// <summary>
/// 实时重绘的动效背景层。作为主窗口最底层控件，绘制极光/霓虹/脉动等流动光斑。
/// </summary>
public class ThemeFxBackground : DoubleBufferedPanel
{
	private string _type = "aurora";
	private List<Color> _stops;
	private double _speed = 1.0;
	private System.Windows.Forms.Timer _timer;
	private double _phase;
	private bool _active;
	private bool _blurEnabled;
	private int _glassOpacity = 210;
	private Color _cachedBg0;
	private Color _cachedBg1;
	private int _cachedBaseAlpha;

	private const int RadialCacheSize = 128;
	private static Bitmap _radialCache;
	private static ImageAttributes _radialAttrs;
	private static readonly object _radialLock = new object();

	private static Bitmap EnsureRadialCache()
	{
		if (_radialCache != null) return _radialCache;
		lock (_radialLock)
		{
			if (_radialCache != null) return _radialCache;
			_radialCache = new Bitmap(RadialCacheSize, RadialCacheSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
			using (Graphics g = Graphics.FromImage(_radialCache))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				using (GraphicsPath path = new GraphicsPath())
				{
					path.AddEllipse(0, 0, RadialCacheSize, RadialCacheSize);
					using (PathGradientBrush pg = new PathGradientBrush(path))
					{
						pg.CenterColor = Color.White;
						pg.SurroundColors = new[] { Color.FromArgb(0, Color.White) };
						g.FillPath(pg, path);
					}
				}
			}
			_radialAttrs = new ImageAttributes();
		}
		return _radialCache;
	}

	public void Pause()
	{
		if (_timer != null && _timer.Enabled)
		{
			_timer.Stop();
		}
	}

	public void Resume()
	{
		if (_timer != null && _active && !AnimationSettings.ReduceMotion)
		{
			_timer.Start();
			Invalidate();
		}
	}

	public void Configure(string type, List<Color> stops, double speed, bool blurEnabled, int glassOpacity)
	{
		string newType = string.IsNullOrEmpty(type) ? "aurora" : type.ToLowerInvariant();
		if (_type != newType)
		{
			_phase = 0.0;
		}
		_type = newType;
		_stops = stops;
		_speed = speed <= 0 ? 1.0 : speed;
		_blurEnabled = blurEnabled;
		_glassOpacity = (glassOpacity > 0 && glassOpacity <= 255) ? glassOpacity : 210;
		UpdateCachedBgColors();
		if (_timer == null)
		{
			ThemeFx.RegisterMouseFilter();
			_timer = new System.Windows.Forms.Timer
			{
				Interval = 50
			};
			_timer.Tick += (s, e) =>
			{
				if (ThemeFx.IsMouseActive)
				{
					return;
				}
				_phase += 0.025 * _speed;
				if (_phase > Math.PI * 2)
				{
					_phase -= Math.PI * 2;
				}
				Invalidate();
			};
		}
		if (AnimationSettings.ReduceMotion)
		{
			_phase = 0.0;
			_timer.Stop();
		}
		else
		{
			_timer.Start();
		}
		_active = true;
		Invalidate();
	}

	public void Detach()
	{
		if (_timer != null)
		{
			_timer.Stop();
			_timer.Dispose();
			_timer = null;
		}
		_active = false;
	}

	private void UpdateCachedBgColors()
	{
		if (_stops == null || _stops.Count == 0) return;
		Color c0 = _stops[0];
		Color c1 = _stops[_stops.Count - 1];
		_cachedBg0 = DrawingUtils.Darken(c0, 0.55);
		_cachedBg1 = DrawingUtils.Darken(c1, 0.25);
		_cachedBaseAlpha = _blurEnabled ? Math.Min(180, _glassOpacity) : 255;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (!_active || _stops == null || _stops.Count == 0)
		{
			return;
		}
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.CompositingQuality = CompositingQuality.HighQuality;
		int w = ClientSize.Width;
		int h = ClientSize.Height;
		if (w <= 0 || h <= 0)
		{
			return;
		}
		using (LinearGradientBrush baseBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h),
			Color.FromArgb(_cachedBaseAlpha, _cachedBg0.R, _cachedBg0.G, _cachedBg0.B),
			Color.FromArgb(_cachedBaseAlpha, _cachedBg1.R, _cachedBg1.G, _cachedBg1.B),
			LinearGradientMode.Vertical))
		{
			g.FillRectangle(baseBrush, 0, 0, w, h);
		}
		switch (_type)
		{
		case "neon":
			DrawNeon(g, w, h);
			break;
		case "pulse":
			DrawPulse(g, w, h);
			break;
		case "fluid":
			DrawFluid(g, w, h);
			break;
		case "breath":
			DrawBreath(g, w, h);
			break;
		default:
			DrawAurora(g, w, h);
			break;
		}
	}

	private void DrawAurora(Graphics g, int w, int h)
	{
		Color col0 = _stops[1];
		Color col1 = _stops[Math.Min(2, _stops.Count - 1)];
		Color col2 = _stops[Math.Min(3, _stops.Count - 1)];

		double off0 = _phase;
		double off1 = _phase + Math.PI * 2.0 / 3.0;
		double off2 = _phase + Math.PI * 4.0 / 3.0;

		float radBase = Math.Min(w, h) * 0.45f;

		float cx0 = w * (0.5f + 0.36f * (float)Math.Sin(off0));
		float cy0 = h * (0.5f + 0.30f * (float)Math.Cos(off0 * 0.8));
		float rad0 = radBase * (0.45f + 0.08f * (float)Math.Sin(off0 * 1.3));
		DrawBlob(g, cx0, cy0, rad0, col0, 150);

		float cx1 = w * (0.5f + 0.36f * (float)Math.Sin(off1));
		float cy1 = h * (0.5f + 0.30f * (float)Math.Cos(off1 * 0.8 + 1));
		float rad1 = radBase * (0.45f + 0.08f * (float)Math.Sin(off1 * 1.3));
		DrawBlob(g, cx1, cy1, rad1, col1, 140);

		float cx2 = w * (0.5f + 0.36f * (float)Math.Sin(off2));
		float cy2 = h * (0.5f + 0.30f * (float)Math.Cos(off2 * 0.8 + 2));
		float rad2 = radBase * (0.45f + 0.08f * (float)Math.Sin(off2 * 1.3));
		DrawBlob(g, cx2, cy2, rad2, col2, 130);
	}

	private void DrawNeon(Graphics g, int w, int h)
	{
		float sweep = (float)(_phase * 60.0);
		Rectangle r = new Rectangle(0, 0, w, h);
		using (LinearGradientBrush band = new LinearGradientBrush(r, DrawingUtils.Darken(_stops[0], 0.4), DrawingUtils.Darken(_stops[_stops.Count - 1], 0.2), LinearGradientMode.ForwardDiagonal))
		{
			band.TranslateTransform(sweep, sweep);
			g.FillRectangle(band, 0, 0, w, h);
		}
		for (int i = 0; i < 2; i++)
		{
			Color col = _stops[(i % (_stops.Count - 1)) + 1];
			double off = _phase * 1.6 + i * Math.PI;
			float cx = w * (0.5f + 0.40f * (float)Math.Sin(off));
			float cy = h * (0.5f + 0.34f * (float)Math.Cos(off));
			float rad = Math.Min(w, h) * 0.40f;
			DrawBlob(g, cx, cy, rad, col, 170);
		}
	}

	private void DrawPulse(Graphics g, int w, int h)
	{
		Color col = _stops[Math.Min(1, _stops.Count - 1)];
		double pulse = 0.5 + 0.5 * Math.Sin(_phase * 1.4);
		float rad = Math.Min(w, h) * (0.30f + 0.22f * (float)pulse);
		DrawBlob(g, w / 2f, h / 2f, rad, col, (int)(120 + 90 * pulse));
		if (_stops.Count > 2)
		{
			DrawBlob(g, w * 0.25f, h * 0.30f, rad * 0.6f, _stops[2], 90);
		}
	}

	private void DrawFluid(Graphics g, int w, int h)
	{
		for (int i = 0; i < _stops.Count; i++)
		{
			double off = _phase * 0.8 + i * (Math.PI * 2.0 / _stops.Count);
			float x = w * (0.5f + 0.42f * (float)Math.Sin(off));
			float y = h * (0.5f + 0.42f * (float)Math.Cos(off * 1.1));
			DrawBlob(g, x, y, Math.Min(w, h) * 0.38f, _stops[i], 120);
		}
	}

	private void DrawBreath(Graphics g, int w, int h)
	{
		Color col0 = _stops[1];
		Color col1 = _stops[Math.Min(2, _stops.Count - 1)];
		Color col2 = _stops[Math.Min(3, _stops.Count - 1)];

		float minDim = Math.Min(w, h);
		DrawBlob(g, w * 0.3f, h * 0.4f, minDim * 0.5f, col0, 110);
		DrawBlob(g, w * 0.5f, h * 0.6f, minDim * 0.5f, col1, 110);
		DrawBlob(g, w * 0.7f, h * 0.4f, minDim * 0.5f, col2, 110);

		double b = 0.5 + 0.5 * Math.Sin(_phase * 1.2);
		int veilAlpha = (int)(26 * b);
		using (SolidBrush veil = new SolidBrush(Color.FromArgb(veilAlpha, Color.White)))
		{
			g.FillRectangle(veil, 0, 0, w, h);
		}
	}

	private void DrawBlob(Graphics g, float cx, float cy, float rad, Color col, int alpha)
	{
		if (rad < 1f)
		{
			return;
		}
		Bitmap cache = EnsureRadialCache();
		int iRad = (int)Math.Ceiling(rad);
		int size = iRad * 2;
		int x = (int)Math.Round(cx - iRad);
		int y = (int)Math.Round(cy - iRad);
		Rectangle dest = new Rectangle(x, y, size, size);
		float a = alpha / 255f;
		ColorMatrix matrix = new ColorMatrix
		{
			Matrix00 = col.R / 255f,
			Matrix11 = col.G / 255f,
			Matrix22 = col.B / 255f,
			Matrix33 = a
		};
		_radialAttrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		g.DrawImage(cache, dest, 0, 0, RadialCacheSize, RadialCacheSize, GraphicsUnit.Pixel, _radialAttrs);
	}
}
