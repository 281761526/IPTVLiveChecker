using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 支持"磨砂玻璃透极光"的 DataGridView。
/// 关键约束：DataGridView.BackgroundColor 不允许透明色（赋值即抛 ArgumentException），
/// 因此这里绝不把背景设为透明，而是用离屏位图缓存一份与主题 GradientStops 同步相位的流动极光，
/// 再由单元格自绘在各自 CellBounds 内裁切复用，拼成无缝的流动背景；行底色由外部 ApplyTheme 设为半透明，叠加磨砂层保证可读。
///
	/// 性能：极光流动由内部独立的节流计时器驱动（约 6fps，与背景层一致），而非逐帧跟随 PhaseTick；
	/// 鼠标在网格上移动/滚动时暂停流动重绘，让出 UI 线程保证鼠标流畅与 hover 高亮，静止 280ms 后恢复。
/// </summary>
public class AuroraGrid : DataGridView
{
	private List<Color> _stops;
	private Color _frost = Color.FromArgb(165, 255, 255, 255);
	private bool _auroraOn;
	private bool _reduceMotion;

	// 离屏缓存：整张网格尺寸的极光位图，重绘时只重绘一次，单元格内裁切复用（避免每格重复绘制）
	private Bitmap _auroraBmp;

	private const int RadialCacheSize = 128;
	private static Bitmap _radialCache;
	private static ImageAttributes _radialAttrs;
	private static readonly object _radialLock = new object();

	public bool IsAuroraOn => _auroraOn;

	// 节流重绘计时器（约 9fps）+ 鼠标静止判定计时器
	private System.Windows.Forms.Timer _animTimer;
	private System.Windows.Forms.Timer _idleTimer;
	private bool _mouseActive;

	public AuroraGrid()
	{
		_animTimer = new System.Windows.Forms.Timer { Interval = 160 };
		_animTimer.Tick += (s, e) => AnimTick();
		_idleTimer = new System.Windows.Forms.Timer { Interval = 280 };
		_idleTimer.Tick += (s, e) =>
		{
			_idleTimer.Stop();
			_mouseActive = false;
			// 鼠标虽离开网格 280ms，但可能仍在相邻控件上移动（全局 IsMouseMoving），
			// 此时恢复重绘会阻塞主线程导致 hover 卡顿；交由 AnimTick 在鼠标真正静止后恢复。
			if (_auroraOn && !_reduceMotion && !ThemeFx.IsMouseMoving && IsHandleCreated && Width > 0 && Height > 0)
			{
				RenderAurora();
				Invalidate();
			}
		};
		MouseMove += (s, e) => MarkActivity();
		Scroll += (s, e) => MarkActivity();
		MouseDown += (s, e) => MarkActivity();
		MouseUp += (s, e) => MarkActivity();
		Disposed += OnDisposed;
	}

	private void MarkActivity()
	{
		_mouseActive = true;
		if (_auroraOn && !_reduceMotion)
		{
			_idleTimer.Stop();
			_idleTimer.Start();
		}
	}

	/// <summary>节流重绘：仅在不卡 UI 的条件下逐帧合成极光（鼠标活动时跳过，把线程让给输入）。</summary>
	private void AnimTick()
	{
		// 鼠标在任意位置移动时都跳过重绘（全局检测），避免离开网格后 280ms 恢复重绘阻塞主线程，
		// 导致相邻的流媒体信息栏/频道详情栏 hover 跟随卡顿。
		if (!_auroraOn || _reduceMotion || _mouseActive || ThemeFx.IsMouseMoving || !IsHandleCreated || Width <= 0 || Height <= 0)
		{
			return;
		}
		RenderAurora();
		Invalidate();
	}

	private void OnDisposed(object s, EventArgs e)
	{
		_animTimer?.Stop();
		_animTimer?.Dispose();
		_idleTimer?.Stop();
		_idleTimer?.Dispose();
		DisposeBmp();
	}

	/// <summary>配置极光。immersive 且主题声明了 GradientStops 时启用；否则关闭（回退实色）。</summary>
	public void ConfigureAurora(AppTheme theme, bool immersive)
	{
		_reduceMotion = AnimationSettings.ReduceMotion;
		if (immersive && theme != null && theme.GradientStops != null && theme.GradientStops.Count >= 2)
		{
			_stops = new List<Color>(theme.GradientStops);
			// 磨砂层：主题 BgAlt 半透明，压暗极光以保证行文字可读
			_frost = Color.FromArgb(165, theme.BgAlt.R, theme.BgAlt.G, theme.BgAlt.B);
			_auroraOn = true;
			RenderAurora();
			if (!_reduceMotion)
			{
				_animTimer.Start();
			}
		}
		else
		{
			_auroraOn = false;
			_animTimer.Stop();
			DisposeBmp();
		}
		Invalidate();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (_auroraOn)
		{
			RenderAurora();
			Invalidate();
		}
	}

	/// <summary>把当前相位的极光渲染到离屏位图（整张网格尺寸，只此一处开销）。</summary>
	private void RenderAurora()
	{
		int w = ClientSize.Width;
		int h = ClientSize.Height;
		if (w <= 0 || h <= 0 || _stops == null || _stops.Count == 0)
		{
			return;
		}
		if (_auroraBmp == null || _auroraBmp.Width != w || _auroraBmp.Height != h)
		{
			_auroraBmp?.Dispose();
			_auroraBmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
		}
		using (Graphics g = Graphics.FromImage(_auroraBmp))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			// 合成质量降到 Default：整图合成开销显著低于 HighQuality，对 blob 观感影响极小
			g.CompositingQuality = CompositingQuality.Default;
			g.Clear(Color.Transparent);
			DrawAuroraContent(g, w, h, _reduceMotion ? 0.0 : ThemeFx.CurrentPhase);
		}
	}

	private void DrawAuroraContent(Graphics g, int w, int h, double phase)
	{
		// 基础渐变（暗化首末色阶），保证 blob 之间的底色也随主题
		Color c0 = DrawingUtils.Darken(_stops[0], 0.55);
		Color c1 = DrawingUtils.Darken(_stops[_stops.Count - 1], 0.25);
		using (LinearGradientBrush baseBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h), c0, c1, LinearGradientMode.Vertical))
		{
			g.FillRectangle(baseBrush, 0, 0, w, h);
		}
		// 三个流动光斑（与 ThemeFx 同相位）
		float radBase = Math.Min(w, h) * 0.5f;
		Color col0 = _stops[1];
		Color col1 = _stops[Math.Min(2, _stops.Count - 1)];
		Color col2 = _stops[Math.Min(3, _stops.Count - 1)];
		DrawBlob(g, w * (0.5f + 0.36f * (float)Math.Sin(phase)), h * (0.5f + 0.30f * (float)Math.Cos(phase * 0.8)), radBase * 0.5f, col0, 150);
		DrawBlob(g, w * (0.5f + 0.36f * (float)Math.Sin(phase + 2.094f)), h * (0.5f + 0.30f * (float)Math.Cos(phase * 0.8 + 1)), radBase * 0.5f, col1, 140);
		DrawBlob(g, w * (0.5f + 0.36f * (float)Math.Sin(phase + 4.188f)), h * (0.5f + 0.30f * (float)Math.Cos(phase * 0.8 + 2)), radBase * 0.5f, col2, 130);
		// 磨砂层：半透明 BgAlt 压暗极光，保证行文字可读
		using (SolidBrush frost = new SolidBrush(_frost))
		{
			g.FillRectangle(frost, 0, 0, w, h);
		}
	}

	/// <summary>在指定裁剪矩形内绘制极光（单元格自绘调用，复用到整张离屏位图）。</summary>
	public void PaintAurora(Graphics g, Rectangle clip)
	{
		if (!_auroraOn || _auroraBmp == null || clip.Width <= 0 || clip.Height <= 0)
		{
			return;
		}
		g.DrawImage(_auroraBmp, clip, clip, GraphicsUnit.Pixel);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		// DataGridView 多数情况下自己用 BackgroundColor 填充；若它调用此处（如底部空白区），则绘制缓存极光。
		if (_auroraOn && _auroraBmp != null)
		{
			Rectangle rb = e.ClipRectangle;
			if (rb.Width > 0 && rb.Height > 0)
			{
				e.Graphics.DrawImage(_auroraBmp, rb, rb, GraphicsUnit.Pixel);
			}
			return;
		}
		base.OnPaintBackground(e);
	}

	private void DisposeBmp()
	{
		_auroraBmp?.Dispose();
		_auroraBmp = null;
	}

	private static Bitmap EnsureRadialCache()
	{
		if (_radialCache != null) return _radialCache;
		lock (_radialLock)
		{
			if (_radialCache != null) return _radialCache;
			_radialCache = new Bitmap(RadialCacheSize, RadialCacheSize, PixelFormat.Format32bppPArgb);
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

	private static void DrawBlob(Graphics g, float cx, float cy, float rad, Color col, int alpha)
	{
		if (rad < 1f) return;
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
