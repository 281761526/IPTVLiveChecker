using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IPTVLiveChecker;

/// <summary>
/// 共享绘图工具类，统一圆角路径、颜色辅助等重复实现。
/// </summary>
internal static class DrawingUtils
{
	/// <summary>
	/// 创建圆角矩形路径（统一实现，替代各控件中重复的 7+ 份代码）。
	/// </summary>
	public static GraphicsPath RoundedRect(Rectangle rect, int radius)
	{
		int d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
		GraphicsPath path = new GraphicsPath();
		if (d <= 0)
		{
			path.AddRectangle(rect);
			return path;
		}
		path.AddArc(rect.X, rect.Y, d, d, 180f, 90f);
		path.AddArc(rect.Right - d, rect.Y, d, d, 270f, 90f);
		path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
		path.AddArc(rect.X, rect.Bottom - d, d, d, 90f, 90f);
		path.CloseFigure();
		return path;
	}

	/// <summary>
	/// 判断颜色是否为暗色（基于人眼感知亮度）。
	/// </summary>
	public static bool IsDarkColor(Color color)
	{
		return (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) < 128.0;
	}

	/// <summary>
	/// 返回指定透明度的颜色。
	/// </summary>
	public static Color WithAlpha(Color c, int alpha)
	{
		alpha = Math.Max(0, Math.Min(255, alpha));
		return Color.FromArgb(alpha, c.R, c.G, c.B);
	}

	/// <summary>
	/// 将颜色按指定比例变暗（factor 0~1）。
	/// </summary>
	public static Color Darken(Color c, double factor)
	{
		factor = Math.Max(0.0, Math.Min(1.0, factor));
		return Color.FromArgb(c.A, (int)(c.R * (1.0 - factor)), (int)(c.G * (1.0 - factor)), (int)(c.B * (1.0 - factor)));
	}

	/// <summary>
	/// 对两个颜色进行线性插值（t=0 返回 c1，t=1 返回 c2）。
	/// </summary>
	public static Color LerpColor(Color c1, Color c2, float t)
	{
		return Color.FromArgb(
			(int)((float)(int)c1.A + (float)(c2.A - c1.A) * t),
			(int)((float)(int)c1.R + (float)(c2.R - c1.R) * t),
			(int)((float)(int)c1.G + (float)(c2.G - c1.G) * t),
			(int)((float)(int)c1.B + (float)(c2.B - c1.B) * t));
	}
}
