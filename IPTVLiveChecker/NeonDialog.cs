using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IPTVLiveChecker;

/// <summary>
/// 霓虹暗夜风格（方案 E）的修复直播源窗口所需的自定义控件与调色板。
/// 所有颜色均从当前 AppTheme 派生：暗色主题呈现青/品红辉光；亮色主题自动适配主题色。
/// </summary>
internal static class NeonHelper
{
	public static GraphicsPath RoundedRectPath(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}

	public static Color WithAlpha(Color c, int alpha)
	{
		return DrawingUtils.WithAlpha(c, alpha);
	}
}

/// <summary>
/// 从 AppTheme 派生出的霓虹调色板，保证“严格适配所有主题”：暗色 = 霓虹青/品红，亮色 = 主题强调色。
/// </summary>
public class NeonPalette
{
	public bool IsDark;
	public Color FormBg;
	public Color PanelBg;
	public Color TitleBg;
	public Color Neon;
	public Color Neon2;
	public Color Label;
	public Color Muted;
	public Color InputText;
	public Color GhostText;
	public Color Glow;
	public Color PrimaryText;
	public Color Border;
	public Color FocusBorder;
	public bool SuppressGlow;

	public static NeonPalette Create(AppTheme theme, bool highContrast = false)
	{
		bool dark = DarkMessageBox.IsDarkColor(theme.Bg);
		NeonPalette p = new NeonPalette
		{
			IsDark = dark,
			SuppressGlow = highContrast
		};
		if (highContrast)
		{
			// 高对比度：纯黑/白底 + 强实体边框，去除霓虹辉光（Glow 设为面板底色融入背景不可见）
			Color border = theme.Border;
			p.FormBg = theme.Bg;
			p.PanelBg = theme.Surface;
			p.TitleBg = theme.HeaderBg;
			p.Neon = border;
			p.Neon2 = border;
			p.Label = theme.TextPrimary;
			p.Muted = theme.TextSecondary;
			p.InputText = theme.TextPrimary;
			p.GhostText = theme.TextPrimary;
			p.Glow = theme.Surface;
			p.PrimaryText = DarkMessageBox.IsDarkColor(theme.Primary) ? Color.White : Color.Black;
			p.Border = border;
			p.FocusBorder = border;
		}
		else if (dark)
		{
			// 暗色底座保持固定的中性深色（不与任何主题冲突的深蓝灰），
			// 霓虹强调色从主题的 Primary/Accent 派生，确保每个暗色主题有独特色彩语言
			p.FormBg = Color.FromArgb(10, 10, 18);
			p.PanelBg = Color.FromArgb(13, 13, 24);
			p.TitleBg = Color.FromArgb(16, 16, 28);

			// 从 theme.Primary 派生霓虹色，不足亮度时提升至可见阈值
			Color tp = theme.Primary;
			double pLum = (tp.R * 0.299 + tp.G * 0.587 + tp.B * 0.114) / 255.0;
			p.Neon = pLum > 0.45
				? tp
				: Color.FromArgb(
					Math.Min(255, tp.R + (int)((255 - tp.R) * 0.45)),
					Math.Min(255, tp.G + (int)((255 - tp.G) * 0.45)),
					Math.Min(255, tp.B + (int)((255 - tp.B) * 0.45)));

			// 从 theme.Accent 派生第二霓虹色（用于渐变终点）
			Color ta = theme.Accent;
			double aLum = (ta.R * 0.299 + ta.G * 0.587 + ta.B * 0.114) / 255.0;
			p.Neon2 = aLum > 0.40
				? ta
				: Color.FromArgb(
					Math.Min(255, ta.R + (int)((255 - ta.R) * 0.40)),
					Math.Min(255, ta.G + (int)((255 - ta.G) * 0.40)),
					Math.Min(255, ta.B + (int)((255 - ta.B) * 0.40)));

			p.Label = Color.FromArgb(
				Math.Min(255, (int)(p.Neon.R * 0.55 + 110)),
				Math.Min(255, (int)(p.Neon.G * 0.55 + 110)),
				Math.Min(255, (int)(p.Neon.B * 0.55 + 110)));
			p.Muted = Color.FromArgb(74, 85 + p.Neon.R / 10, 85 + p.Neon.G / 10);
			p.InputText = Color.FromArgb(
				Math.Min(255, 185 + p.Neon.R / 4),
				Math.Min(255, 185 + p.Neon.G / 4),
				Math.Min(255, 185 + p.Neon.B / 4));
			p.GhostText = Color.FromArgb(
				Math.Min(255, 140 + p.Neon.R / 3),
				Math.Min(255, 140 + p.Neon.G / 3),
				Math.Min(255, 140 + p.Neon.B / 3));
			p.Glow = p.Neon;
			p.PrimaryText = Color.FromArgb(6, 10 + tp.R / 12, 14 + tp.B / 12);
			p.Border = NeonHelper.WithAlpha(p.Neon, 90);
			p.FocusBorder = p.Neon;
		}
		else
		{
			p.FormBg = theme.Bg;
			p.PanelBg = theme.Surface;
			p.TitleBg = theme.HeaderBg;
			p.Neon = theme.Primary;
			p.Neon2 = theme.Accent;
			p.Label = theme.TextPrimary;
			p.Muted = theme.TextSecondary;
			p.InputText = theme.TextPrimary;
			p.GhostText = theme.TextPrimary;
			p.Glow = theme.Primary;
			p.PrimaryText = DarkMessageBox.IsDarkColor(theme.Primary) ? Color.White : Color.Black;
			p.Border = NeonHelper.WithAlpha(p.Neon, 170);
			p.FocusBorder = p.Neon;
		}
		return p;
	}
}

/// <summary>
/// 霓虹圆角输入框：无系统边框，自绘圆角 + 聚焦辉光，所有颜色来自主题调色板。
/// </summary>
public class NeonTextBox : UserControl
{
	private readonly TextBox _tb;
	private bool _focused;
	private bool _hover;

	public int Radius { get; set; } = 7;
	private Color _backColorX = Color.FromArgb(13, 13, 24);
	public Color BackColorX
	{
		get => _backColorX;
		set
		{
			_backColorX = value;
			if (_tb != null)
			{
				_tb.BackColor = value;
			}
			Invalidate();
		}
	}
	public Color BorderColor { get; set; } = Color.FromArgb(90, 0, 255, 224);
	public Color FocusColor { get; set; } = Color.FromArgb(0, 255, 224);
	public Color GlowColor { get; set; } = Color.FromArgb(0, 255, 224);
	public bool GlowEnabled { get; set; } = true;
	private Color _textColor = Color.FromArgb(200, 255, 245);
	public Color TextColor
	{
		get => _textColor;
		set
		{
			_textColor = value;
			if (_tb != null)
			{
				_tb.ForeColor = value;
			}
		}
	}

	public new string Text
	{
		get => _tb.Text;
		set => _tb.Text = value;
	}

	public bool ReadOnly
	{
		get => _tb.ReadOnly;
		set => _tb.ReadOnly = value;
	}

	public new event EventHandler TextChanged;

	public NeonTextBox()
	{
		SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
		DoubleBuffered = true;
		BackColor = Color.Transparent;
		_tb = new TextBox
		{
			BorderStyle = BorderStyle.None,
			BackColor = BackColorX,
			ForeColor = TextColor,
			Dock = DockStyle.Fill
		};
		_tb.Margin = new Padding(0);
		Controls.Add(_tb);
		Padding = new Padding(10, 5, 10, 5);
		_tb.GotFocus += delegate
		{
			_focused = true;
			_tb.BackColor = BackColorX;
			Invalidate();
		};
		_tb.LostFocus += delegate
		{
			_focused = false;
			Invalidate();
		};
		_tb.MouseEnter += delegate
		{
			_hover = true;
			Invalidate();
		};
		_tb.MouseLeave += delegate
		{
			_hover = false;
			Invalidate();
		};
		_tb.TextChanged += delegate
		{
			TextChanged?.Invoke(this, EventArgs.Empty);
		};
	}

	public new Font Font
	{
		get => _tb.Font;
		set => _tb.Font = value;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle r = new Rectangle(3, 3, Width - 7, Height - 7);
		using (GraphicsPath path = NeonHelper.RoundedRectPath(r, Radius))
		{
			using (Brush b = new SolidBrush(BackColorX))
			{
				g.FillPath(b, path);
			}
			if (_focused && GlowEnabled)
			{
				for (int i = 6; i >= 1; i--)
				{
					using (Pen p = new Pen(Color.FromArgb(55 / i, GlowColor.R, GlowColor.G, GlowColor.B), i * 1.8f))
					{
						g.DrawPath(p, path);
					}
				}
			}
			Color bc = _focused ? FocusColor : (_hover ? NeonHelper.WithAlpha(BorderColor, 200) : BorderColor);
			using (Pen p = new Pen(bc, _focused ? 1.6f : 1f))
			{
				g.DrawPath(p, path);
			}
		}
	}
}

/// <summary>
/// 霓虹预览框：自绘 URL，协议/路径用 muted 色，host:port 用霓虹色并带辉光（方案 E 的标志性高亮）。
/// </summary>
public class NeonPreviewBox : Control
{
	public int Radius { get; set; } = 8;
	public Color BackColorX { get; set; } = Color.FromArgb(13, 13, 24);
	public Color BorderColor { get; set; } = Color.FromArgb(110, 0, 255, 224);
	public Color MutedColor { get; set; } = Color.FromArgb(74, 107, 102);
	public Color NeonColor { get; set; } = Color.FromArgb(0, 255, 224);
	public Color GlowColor { get; set; } = Color.FromArgb(0, 255, 224);

	public string Protocol { get; set; }
	public string Host { get; set; }
	public string Port { get; set; }
	public string Path { get; set; }

	public NeonPreviewBox()
	{
		SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
		DoubleBuffered = true;
		BackColor = Color.Transparent;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
		Rectangle r = new Rectangle(2, 2, Width - 5, Height - 5);
		using (GraphicsPath path = NeonHelper.RoundedRectPath(r, Radius))
		{
			using (Brush b = new SolidBrush(BackColorX))
			{
				g.FillPath(b, path);
			}
			using (Pen p = new Pen(BorderColor, 1.2f))
			{
				g.DrawPath(p, path);
			}
		}
		float x = r.Left + 10;
		float y = r.Top + (r.Height - (Font?.Height ?? 14)) / 2f;
		string proto = (Protocol ?? "") + "://";
		string hp = (Host ?? "") + (string.IsNullOrEmpty(Port) ? "" : ":" + Port);
		string tail = Path ?? "";
		x += DrawMuted(g, proto, x, y);
		x += DrawNeon(g, hp, x, y);
		DrawMuted(g, tail, x, y);
	}

	private float DrawMuted(Graphics g, string s, float x, float y)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0f;
		}
		SizeF sz = g.MeasureString(s, Font);
		using (Brush b = new SolidBrush(MutedColor))
		{
			g.DrawString(s, Font, b, x, y);
		}
		return sz.Width;
	}

	private float DrawNeon(Graphics g, string s, float x, float y)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0f;
		}
		SizeF sz = g.MeasureString(s, Font);
		for (int i = 3; i >= 1; i--)
		{
			using (Brush b = new SolidBrush(Color.FromArgb(85 / i, NeonColor.R, NeonColor.G, NeonColor.B)))
			{
				g.DrawString(s, Font, b, x + i, y + i);
			}
		}
		using (Brush b = new SolidBrush(NeonColor))
		{
			g.DrawString(s, Font, b, x, y);
		}
		return sz.Width;
	}
}

/// <summary>
/// 霓虹按钮：主按钮为青→品红渐变发光，次按钮为霓虹描边幽灵按钮，悬停带辉光。
/// </summary>
public class NeonButton : Button
{
	public int Radius { get; set; } = 9;
	public Color GradientStart { get; set; } = Color.FromArgb(0, 255, 224);
	public Color GradientEnd { get; set; } = Color.FromArgb(255, 0, 200);
	public Color TextColorX { get; set; } = Color.FromArgb(6, 18, 26);
	public Color BorderColor { get; set; } = Color.FromArgb(110, 0, 255, 224);
	public Color GlowColor { get; set; } = Color.FromArgb(0, 255, 224);
	public bool IsPrimary { get; set; } = true;
	public bool GlowEnabled { get; set; } = true;

	private bool _hover;

	// 弹簧物理：按下缩放回弹（damping 1.0 / response 0.4）+ 悬停微抬 + 松手速度交接
	private readonly Spring1D _press = new Spring1D(1.0, 0.4, 1.0);
	private readonly Spring1D _lift = new Spring1D(0.0, 0.4, 1.0);
	private readonly System.Windows.Forms.Timer _springTimer = new System.Windows.Forms.Timer { Interval = 16 };

	private void StartSpring()
	{
		if (!_springTimer.Enabled)
		{
			_springTimer.Start();
		}
	}

	public NeonButton()
	{
		FlatStyle = FlatStyle.Flat;
		FlatAppearance.BorderSize = 0;
		FlatAppearance.MouseOverBackColor = Color.Empty;
		FlatAppearance.MouseDownBackColor = Color.Empty;
		UseVisualStyleBackColor = false;
		SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
		DoubleBuffered = true;
		MouseEnter += delegate
		{
			_hover = true;
			if (!AnimationSettings.ReduceMotion)
			{
				_lift.Target = -2.0;
				StartSpring();
			}
			Invalidate();
		};
		MouseLeave += delegate
		{
			_hover = false;
			if (!AnimationSettings.ReduceMotion)
			{
				_lift.Target = 0.0;
				StartSpring();
			}
			Invalidate();
		};
		MouseDown += delegate
		{
			if (AnimationSettings.ReduceMotion)
			{
				Invalidate();
				return;
			}
			_press.Target = 0.94;
			StartSpring();
		};
		MouseUp += delegate
		{
			if (AnimationSettings.ReduceMotion)
			{
				_press.SnapTo(1.0);
				Invalidate();
				return;
			}
			// 松手：目标回 1.0，当前速度被保留 → 速度交接，自然回弹
			_press.Target = 1.0;
			StartSpring();
		};
		_springTimer.Tick += delegate
		{
			bool a = _press.Step(0.016);
			bool b = _lift.Step(0.016);
			if (!a && !b)
			{
				_springTimer.Stop();
			}
			Invalidate();
		};
		UpdateRegion();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_springTimer.Stop();
			_springTimer.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateRegion();
	}

	private void UpdateRegion()
	{
		if (Width > 0 && Height > 0)
		{
			Region?.Dispose();
			using (GraphicsPath path = NeonHelper.RoundedRectPath(new Rectangle(0, 0, Width, Height), Radius))
			{
				Region = new Region(path);
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		// 弹簧变换：先整体上抬（悬停），再以中心为锚点缩放（按下）
		float s = (float)_press.Value;
		float ly = (float)_lift.Value;
		g.TranslateTransform(0f, ly);
		float cx = Width / 2f;
		float cy = Height / 2f;
		g.TranslateTransform(cx, cy);
		g.ScaleTransform(s, s);
		g.TranslateTransform(-cx, -cy);
		Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
		using (GraphicsPath path = NeonHelper.RoundedRectPath(r, Radius))
		{
			if (IsPrimary)
			{
				if (_hover && GlowEnabled)
				{
					for (int i = 1; i <= 5; i++)
					{
						using (Pen p = new Pen(Color.FromArgb(60 / i, GlowColor.R, GlowColor.G, GlowColor.B), i * 2f))
						{
							g.DrawPath(p, path);
						}
					}
				}
				using (LinearGradientBrush lb = new LinearGradientBrush(r, GradientStart, GradientEnd, LinearGradientMode.Horizontal))
				{
					g.FillPath(lb, path);
				}
			}
			else
			{
				using (Brush b = new SolidBrush(Color.Transparent))
				{
					g.FillPath(b, path);
				}
				using (Pen p = new Pen(_hover ? GlowColor : BorderColor, 1.3f))
				{
					g.DrawPath(p, path);
				}
			}
		}
		// 用 DrawString（而非 TextRenderer）以尊重上面的弹簧变换
		using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
		using (Brush tb = new SolidBrush(TextColorX))
		{
			g.DrawString(Text, Font, tb, ClientRectangle, sf);
		}
	}
}

/// <summary>
/// 通用霓虹外壳：为任意无边框 Form 套用方案 E·霓虹暗夜 外壳
/// （圆角发光面板 + 自定义标题栏 + 拖拽 + 关闭）。返回内容容器 Body，
/// 调用方把原本加到 dlg 的控件改加到 ctx.Body 即可，坐标保持相对对话框(0,0)不变。
/// </summary>
public static class NeonChrome
{
	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_COMPOSITED = 0x02000000;

	[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLong")]
	private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

	[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong")]
	private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

	/// <summary>
	/// 为对话框启用 WS_EX_COMPOSITED：让整棵子控件树由 DWM 离屏合成后一次性呈现，
	/// 彻底消除多层自绘面板叠加时的逐控件刷新闪烁与拖动残影。
	/// 仅用于无视频子窗口的对话框——主窗体内嵌 VLC/MPV 的渲染 HWND，全局合成会拖累视频，故不启用。
	/// </summary>
	public static void EnableComposited(Form form)
	{
		if (form == null) return;
		void Apply()
		{
			try
			{
				if (!form.IsHandleCreated || form.IsDisposed) return;
				int ex = GetWindowLong32(form.Handle, GWL_EXSTYLE);
				if ((ex & WS_EX_COMPOSITED) == 0)
				{
					SetWindowLong32(form.Handle, GWL_EXSTYLE, ex | WS_EX_COMPOSITED);
				}
			}
			catch
			{
			}
		}
		if (form.IsHandleCreated) Apply();
		else form.HandleCreated += (s, e) => Apply();
	}

	public sealed class Context
	{
		public Panel Body;
		public Panel Title;
		public NeonPalette Palette;
		public int Margin;
		public int TitleHeight;
		public int Corner;
		private Form _form;
		private Panel _titlePanel;
		private Panel _body;
		private Label[] _titleLabels;
		private Label _lblX;

		internal Context(Form form, Panel titlePanel, Panel body, Label[] titleLabels, Label lblX)
		{
			_form = form;
			_titlePanel = titlePanel;
			_body = body;
			_titleLabels = titleLabels;
			_lblX = lblX;
		}

		/// <summary>用于不经过 NeonChrome.Apply 的自定义对话框。</summary>
		public Context() { }

		/// <summary>原地更新调色板并刷新所有 NeonChrome 绘制的元素。</summary>
		public void UpdatePalette(NeonPalette newPal)
		{
			Palette = newPal;
			if (_form != null) _form.BackColor = newPal.FormBg;
			if (_body != null) _body.BackColor = newPal.PanelBg;
			if (_titlePanel != null) _titlePanel.BackColor = newPal.TitleBg;
			if (_titleLabels != null)
			{
				foreach (Label lbl in _titleLabels)
				{
					if (lbl == _lblX) continue;
					lbl.BackColor = newPal.TitleBg;
				}
			}
			if (_form != null) _form.Invalidate(true);
		}

		internal void SetControls(Panel titlePanel, Panel body, Label[] titleLabels, Label lblX)
		{
			_titlePanel = titlePanel;
			_body = body;
			_titleLabels = titleLabels;
			_lblX = lblX;
		}
	}

	public static Context Apply(Form form, NeonPalette pal, string title, float dpiScale)
	{
		form.FormBorderStyle = FormBorderStyle.None;
		form.ShowInTaskbar = false;
		// 统一为所有经 NeonChrome 承载的对话框开启离屏合成 + 双缓冲，切换主题与拖动均无闪烁/残影
		EnableComposited(form);
		try
		{
			typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				?.SetValue(form, true, null);
		}
		catch
		{
		}
		int S(int v) => (int)Math.Round(v * dpiScale);
		int W = form.ClientSize.Width;
		int H = form.ClientSize.Height;
		int m = S(16);
		int th = S(36);
		int cr = S(14);
		int px = m, py = m, pw = W - 2 * m, ph = H - 2 * m;
		form.Region = new Region(NeonHelper.RoundedRectPath(new Rectangle(0, 0, W, H), cr));
		form.BackColor = pal.FormBg;
		// 先创建 Context，让所有 Paint 闭包通过 ctx.Palette 读取（引用类型，可变）
		Context ctx = new Context(form, null, null, null, null);
		ctx.Palette = pal;
		ctx.Margin = m;
		ctx.TitleHeight = th;
		ctx.Corner = cr;
		form.Paint += (s, e) =>
		{
			NeonPalette p = ctx.Palette;
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle rect = new Rectangle(px, py, form.ClientSize.Width - 2 * m, form.ClientSize.Height - 2 * m - th);
			using (GraphicsPath path = NeonHelper.RoundedRectPath(rect, cr))
			{
				if (!p.SuppressGlow)
				{
					for (int i = 6; i >= 1; i--)
					{
						using (Pen pen = new Pen(Color.FromArgb(50 / i, p.Glow.R, p.Glow.G, p.Glow.B), i * 2f))
						{
							g.DrawPath(pen, path);
						}
					}
				}
				using (Brush b = new SolidBrush(p.PanelBg))
				{
					g.FillPath(b, path);
				}
				using (Pen pen = new Pen(NeonHelper.WithAlpha(p.Neon, 120), 1.5f))
				{
					g.DrawPath(pen, path);
				}
			}
		};
		Panel titlePanel = new Panel
		{
			Location = new Point(px, py),
			Size = new Size(pw, th),
			BackColor = pal.TitleBg
		};
		{
			int d = cr * 2;
			GraphicsPath tp = new GraphicsPath();
			tp.AddArc(0, 0, d, d, 180f, 90f);
			tp.AddArc(pw - d, 0, d, d, 270f, 90f);
			tp.AddLine(pw, cr, pw, th);
			tp.AddLine(pw, th, 0, th);
			tp.AddLine(0, th, 0, cr);
			tp.CloseFigure();
			titlePanel.Region = new Region(tp);
		}
		titlePanel.Paint += (s, e) =>
		{
			NeonPalette p = ctx.Palette;
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle r = titlePanel.ClientRectangle;
			using (GraphicsPath path = NeonHelper.RoundedRectPath(new Rectangle(0, 0, r.Width, r.Height + cr), cr))
			using (Brush b = new SolidBrush(p.TitleBg))
			{
				g.FillPath(b, path);
			}
			using (Pen pen = new Pen(NeonHelper.WithAlpha(p.Border, 120), 1f))
			{
				g.DrawLine(pen, 0, r.Height - 1, r.Width, r.Height - 1);
			}
		};
		form.Controls.Add(titlePanel);
		float fs = 10f * dpiScale;
		Label lblHead = new Label
		{
			Text = title,
			Font = new Font("Microsoft YaHei", fs, FontStyle.Bold),
			ForeColor = pal.GhostText,
			Location = new Point(S(16), S(8)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		bool closeHover = false;
		// 关闭按钮"✕"尺寸与边距：统一由 closeSize / closePad 常量控制，避免硬编码散落
		int closeSize = S(24);
		int closePad = S(6); // 距 titlePanel 右边缘的距离
		Label lblX = new Label
		{
			Text = "✕",
			Font = new Font("Microsoft YaHei", fs),
			ForeColor = pal.GhostText,
			// 距 titlePanel 右边缘 closePad，垂直居中（th - closeSize）/ 2
			Location = new Point(pw - closeSize - closePad, (th - closeSize) / 2),
			Size = new Size(closeSize, closeSize),
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand,
			// Anchor=Top|Right 保证 titlePanel 大小变化时贴右（配合下面的 Resize 双保险）
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		lblX.Paint += (s, e) =>
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle r = lblX.ClientRectangle;
			if (closeHover)
			{
				using (Brush b = new SolidBrush(Color.FromArgb(255, 59, 48)))
				{
					g.FillEllipse(b, 0, 0, r.Width - 1, r.Height - 1);
				}
			}
		};
		titlePanel.Controls.Add(lblHead);
		titlePanel.Controls.Add(lblX);
		// 闭包通过 ctx.Palette 读取，UpdatePalette 后立即生效
		lblX.MouseEnter += (s, e) =>
		{
			closeHover = true;
			lblX.ForeColor = Color.White;
			lblX.Invalidate();
		};
		lblX.MouseLeave += (s, e) =>
		{
			closeHover = false;
			lblX.ForeColor = ctx.Palette.GhostText;
			lblX.Invalidate();
		};
		lblX.Click += (s, e) =>
		{
			form.DialogResult = DialogResult.Cancel;
			form.Close();
		};
		Point dragOffset = Point.Empty;
		bool dragging = false;
		MouseEventHandler down = (s, e) =>
		{
			if (e.Button == MouseButtons.Left)
			{
				dragging = true;
				dragOffset = e.Location;
			}
		};
		MouseEventHandler move = (s, e) =>
		{
			if (dragging)
			{
				form.Location = new Point(form.Left + e.X - dragOffset.X, form.Top + e.Y - dragOffset.Y);
			}
		};
		MouseEventHandler up = (s, e) => dragging = false;
		titlePanel.MouseDown += down;
		titlePanel.MouseMove += move;
		titlePanel.MouseUp += up;
		lblHead.MouseDown += down;
		lblHead.MouseMove += move;
		lblHead.MouseUp += up;
		// titlePanel 大小变化时（窗口拉伸）重算自身圆角 Region 与关闭按钮位置
		// （Anchor=Top|Right 在 Panel 内通常有效，但自绘 Region 变更后可能导致锚点偏移，
		//  显式 Resize 处理作为双保险，确保 100% 贴右对齐）
		titlePanel.Resize += (s, e) =>
		{
			int tpw = titlePanel.ClientSize.Width;
			int tph = titlePanel.ClientSize.Height;
			// 重算 titlePanel 圆角 Region（保证大小时圆角不撕裂）
			int d = cr * 2;
			GraphicsPath tp = new GraphicsPath();
			tp.AddArc(0, 0, d, d, 180f, 90f);
			tp.AddArc(tpw - d, 0, d, d, 270f, 90f);
			tp.AddLine(tpw, cr, tpw, tph);
			tp.AddLine(tpw, tph, 0, tph);
			tp.AddLine(0, tph, 0, cr);
			tp.CloseFigure();
			titlePanel.Region?.Dispose();
			titlePanel.Region = new Region(tp);
			// 关闭按钮位置重算：保持距右边缘 closePad、垂直居中
			lblX.Location = new Point(tpw - closeSize - closePad, (tph - closeSize) / 2);
		};
		Panel body = new Panel
		{
			Location = new Point(px, py + th),
			Size = new Size(pw, ph - th),
			BackColor = pal.PanelBg,
			Padding = new Padding(0)
		};
		form.Controls.Add(body);
		form.Resize += (s, e) =>
		{
			body.Location = new Point(px, py + th);
			body.Size = new Size(form.ClientSize.Width - 2 * m, form.ClientSize.Height - 2 * m - th);
			// titlePanel 同步调整：大小变化会触发上面的 titlePanel.Resize 重算圆角 Region 与关闭按钮
			titlePanel.Location = new Point(px, py);
			titlePanel.Size = new Size(form.ClientSize.Width - 2 * m, th);
			form.Region = new Region(NeonHelper.RoundedRectPath(new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height), cr));
			form.Invalidate();
		};
		// 用构造函数注入控件引用，供 UpdatePalette 使用
		var titleLabels = new[] { lblHead, lblX };
		ctx.Body = body;
		ctx.Title = titlePanel;
		ctx.SetControls(titlePanel, body, titleLabels, lblX);
		return ctx;
	}
}
