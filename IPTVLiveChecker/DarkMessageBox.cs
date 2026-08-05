using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public static class DarkMessageBox
{
	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2 = 19;

	public static Func<bool> IsDarkProvider { get; set; } = () => false;

	public static Func<AppTheme> ThemeProvider { get; set; }

	public static Func<Icon> IconProvider { get; set; }

	/// <summary>全局字体工厂（入参为已乘 DPI 的字号）。由主窗体注入，使消息框字体与主界面完全一致。</summary>
	public static Func<float, Font> FontProvider { get; set; }

	public static float DpiScale { get; set; } = 1f;

	public static bool IsDarkColor(Color color)
	{
		return DrawingUtils.IsDarkColor(color);
	}

	public static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}

	/// <summary>
	/// 按主题语义色矢量绘制消息框图标（圆底 + ASCII 字形），替代 SystemIcons 固定彩色位图。
	/// 字形只用 ! × ? i 四个 ASCII 字符，任何字体下都是单色渲染、必定跟随画刷色，不存在彩色 emoji 回退。
	/// </summary>
	private static Bitmap CreateThemedIcon(int size, Color color, string glyph)
	{
		Bitmap bmp = new Bitmap(size, size);
		using (Graphics g = Graphics.FromImage(bmp))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			Rectangle circle = new Rectangle(1, 1, size - 3, size - 3);
			using (SolidBrush fill = new SolidBrush(color))
			{
				g.FillEllipse(fill, circle);
			}
			bool onDark = DrawingUtils.IsDarkColor(color);
			// 描一圈同向低透明轮廓，浅色主题下圆底也不会与窗口底色糊在一起
			using (Pen ring = new Pen(Color.FromArgb(56, onDark ? Color.White : Color.Black), 1f))
			{
				g.DrawEllipse(ring, circle);
			}
			Color glyphColor = (onDark ? Color.White : Color.FromArgb(28, 28, 30));
			using (Font f = new Font("Segoe UI", (float)size * 0.54f, FontStyle.Bold, GraphicsUnit.Pixel))
			using (SolidBrush gb = new SolidBrush(glyphColor))
			using (StringFormat sf = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			})
			{
				g.DrawString(glyph, f, gb, new RectangleF(1f, 0f, size - 3, size - 2), sf);
			}
		}
		return bmp;
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	public static void ApplyDarkTitleBar(IntPtr hwnd, int darkMode)
	{
		int dm = darkMode;
		try
		{
			DwmSetWindowAttribute(hwnd, 20, ref dm, 4);
		}
		catch
		{
		}
		try
		{
			DwmSetWindowAttribute(hwnd, 19, ref dm, 4);
		}
		catch
		{
		}
	}

	public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
	{
		return Show(null, text, caption, buttons, icon);
	}

	public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
	{
		AppTheme t = ThemeProvider?.Invoke();
		bool isDark;
		Color bgColor;
		Color textColor;
		Color accentColor;
		Color accentHover;
		Color btnColor;
		Color btnHoverColor;
		Color btnBorderColor;
		Color btnFg;
		Color btnAccentFg;
		// 图标语义色：用于矢量绘制消息框图标，替代 Windows 系统固定彩色图标
		Color iconWarn;
		Color iconError;
		Color iconInfo;
		if (t != null)
		{
			isDark = IsDarkColor(t.Bg);
			bgColor = t.Bg;
			textColor = t.TextPrimary;
			accentColor = t.PlayBtnBg;
			accentHover = t.PrimaryDark;
			btnColor = t.Surface;
			btnHoverColor = t.SelectRow;
			btnBorderColor = t.Border;
			btnFg = t.TextPrimary;
			iconWarn = t.WarnColor;
			iconError = t.ErrorColor;
			iconInfo = t.Primary;
			btnAccentFg = ((t.PlayBtnText.A > 0) ? t.PlayBtnText : Color.White);
		}
		else
		{
			isDark = IsDarkProvider();
			bgColor = (isDark ? Color.FromArgb(40, 40, 50) : Color.White);
			textColor = (isDark ? Color.FromArgb(230, 230, 240) : Color.FromArgb(40, 40, 40));
			accentColor = Color.FromArgb(66, 133, 244);
			accentHover = Color.FromArgb(86, 153, 254);
			btnColor = (isDark ? Color.FromArgb(60, 60, 75) : Color.FromArgb(240, 240, 245));
			btnHoverColor = (isDark ? Color.FromArgb(80, 80, 100) : Color.FromArgb(220, 220, 230));
			btnBorderColor = (isDark ? Color.FromArgb(90, 90, 110) : Color.FromArgb(200, 200, 205));
			btnFg = (isDark ? Color.White : Color.FromArgb(50, 50, 50));
			iconWarn = Color.FromArgb(245, 158, 11);
			iconError = Color.FromArgb(239, 68, 68);
			iconInfo = Color.FromArgb(66, 133, 244);
			btnAccentFg = Color.White;
		}
		// 字体跟随全局字体设置（主题/DPI 一并适配），FontProvider 缺省时回落雅黑
		Font msgFont;
		try
		{
			msgFont = FontProvider?.Invoke(10.5f * DpiScale) ?? new Font("Microsoft YaHei UI", 10.5f * DpiScale);
		}
		catch
		{
			msgFont = new Font("Microsoft YaHei UI", 10.5f * DpiScale);
		}
		using Form dlg = new Form();
		// 离屏合成：消息框内含多层自绘按钮/图标，开启后切换主题与移动窗口均无闪烁残影
		NeonChrome.EnableComposited(dlg);
		dlg.Text = caption;
		dlg.StartPosition = FormStartPosition.Manual;
		dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
		dlg.MaximizeBox = false;
		dlg.MinimizeBox = false;
		dlg.BackColor = bgColor;
		dlg.ForeColor = textColor;
		dlg.Font = msgFont;
		dlg.ShowInTaskbar = false;
		dlg.TopMost = true;
		// 初始透明，由 Shown 事件做淡入（避免无动画时的闪现）
		dlg.Opacity = 0;
		try
		{
			Icon appIcon = IconProvider?.Invoke();
			if (appIcon != null)
			{
				dlg.Icon = appIcon;
			}
		}
		catch
		{
		}
		int paddingH = (int)(24f * DpiScale);
		int paddingV = (int)(20f * DpiScale);
		int iconSize = (int)(40f * DpiScale);
		int iconGap = (int)(16f * DpiScale);
		int btnGap = (int)(16f * DpiScale);
		int btnW = (int)(85f * DpiScale);
		int btnH = (int)(34f * DpiScale);
		int btnPadding = (int)(16f * DpiScale);
		int minWidth = (int)(320f * DpiScale);
		Label lblText = new Label
		{
			Text = text,
			ForeColor = textColor,
			BackColor = bgColor,
			AutoSize = false,
			Size = new Size(minWidth - paddingH * 2 - iconSize - iconGap, 0),
			Font = msgFont
		};
		lblText.PerformLayout();
		int textW = Math.Max(lblText.PreferredWidth, minWidth - paddingH * 2 - iconSize - iconGap);
		int textH = lblText.PreferredHeight;
		int contentW = paddingH + iconSize + iconGap + textW + paddingH;
		int contentH = paddingV + Math.Max(iconSize, textH) + btnPadding + btnH + paddingV;
		dlg.ClientSize = new Size(Math.Max(contentW, minWidth), contentH);
		int iconX = paddingH;
		int iconY = paddingV;
		int textX = paddingH + iconSize + iconGap;
		int textY = paddingV;
		int btnY = contentH - paddingV - btnH;
		lblText.Location = new Point(textX, textY);
		lblText.Size = new Size(textW, textH);
		dlg.Controls.Add(lblText);
		PictureBox picIcon = new PictureBox
		{
			Size = new Size(iconSize, iconSize),
			Location = new Point(iconX, iconY),
			BackColor = bgColor,
			SizeMode = PictureBoxSizeMode.AutoSize
		};
		// 图标：原为 SystemIcons.Information/Warning/Error/Question（Windows 系统固定彩色位图，
		// 切换主题时纹丝不动，是消息框唯一不换肤的元素）。改为按主题语义色矢量绘制，随主题即时换肤。
		Color iconColor;
		string iconGlyph;
		switch (icon)
		{
		case MessageBoxIcon.Exclamation:
			iconColor = iconWarn;
			iconGlyph = "!";
			break;
		case MessageBoxIcon.Hand:
			iconColor = iconError;
			iconGlyph = "×";
			break;
		case MessageBoxIcon.Question:
			iconColor = accentColor;
			iconGlyph = "?";
			break;
		default:
			iconColor = iconInfo;
			iconGlyph = "i";
			break;
		}
		picIcon.Image = CreateThemedIcon(iconSize, iconColor, iconGlyph);
		dlg.Controls.Add(picIcon);
		// PictureBox.Dispose 不会释放 Image，这里显式回收，避免每弹一次消息框泄漏一张位图
		dlg.FormClosed += delegate
		{
			Image oldImg = picIcon.Image;
			picIcon.Image = null;
			oldImg?.Dispose();
		};
		switch (buttons)
		{
		case MessageBoxButtons.OK:
		{
			int btnX = (dlg.ClientSize.Width - btnW) / 2;
			Button button5 = new Button();
			button5.Text = "确定";
			button5.DialogResult = DialogResult.OK;
			button5.Location = new Point(btnX, btnY);
			button5.Size = new Size(btnW, btnH);
			button5.BackColor = accentColor;
			button5.ForeColor = btnAccentFg;
			button5.FlatStyle = FlatStyle.Flat;
			button5.FlatAppearance.BorderSize = 0;
			button5.Font = msgFont;
			Button btnOK2 = button5;
			btnOK2.FlatAppearance.BorderSize = 0;
			btnOK2.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
			btnOK2.MouseEnter += delegate
			{
				btnOK2.BackColor = accentHover;
			};
			btnOK2.MouseLeave += delegate
			{
				btnOK2.BackColor = accentColor;
			};
			dlg.Controls.Add(btnOK2);
			dlg.AcceptButton = btnOK2;
			break;
		}
		case MessageBoxButtons.YesNo:
		{
			int btnGroupW2 = btnW * 2 + btnGap;
			int btnStartX2 = (dlg.ClientSize.Width - btnGroupW2) / 2;
			Button button3 = new Button();
			button3.Text = "是";
			button3.DialogResult = DialogResult.Yes;
			button3.Location = new Point(btnStartX2, btnY);
			button3.Size = new Size(btnW, btnH);
			button3.BackColor = accentColor;
			button3.ForeColor = btnAccentFg;
			button3.FlatStyle = FlatStyle.Flat;
			button3.FlatAppearance.BorderSize = 0;
			button3.Font = msgFont;
			Button btnYes = button3;
			btnYes.FlatAppearance.BorderSize = 0;
			btnYes.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
			btnYes.MouseEnter += delegate
			{
				btnYes.BackColor = accentHover;
			};
			btnYes.MouseLeave += delegate
			{
				btnYes.BackColor = accentColor;
			};
			dlg.Controls.Add(btnYes);
			Button button4 = new Button();
			button4.Text = "否";
			button4.DialogResult = DialogResult.No;
			button4.Location = new Point(btnStartX2 + btnW + btnGap, btnY);
			button4.Size = new Size(btnW, btnH);
			button4.BackColor = btnColor;
			button4.ForeColor = btnFg;
			button4.FlatStyle = FlatStyle.Flat;
			button4.FlatAppearance.BorderSize = 0;
			button4.Font = msgFont;
			Button btnNo = button4;
			btnNo.FlatAppearance.BorderColor = btnBorderColor;
			btnNo.FlatAppearance.BorderSize = 1;
			btnNo.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
			btnNo.MouseEnter += delegate
			{
				btnNo.BackColor = btnHoverColor;
			};
			btnNo.MouseLeave += delegate
			{
				btnNo.BackColor = btnColor;
			};
			dlg.Controls.Add(btnNo);
			dlg.AcceptButton = btnYes;
			dlg.CancelButton = btnNo;
			break;
		}
		case MessageBoxButtons.OKCancel:
		{
			int btnGroupW = btnW * 2 + btnGap;
			int btnStartX = (dlg.ClientSize.Width - btnGroupW) / 2;
			Button button = new Button();
			button.Text = "确定";
			button.DialogResult = DialogResult.OK;
			button.Location = new Point(btnStartX, btnY);
			button.Size = new Size(btnW, btnH);
			button.BackColor = accentColor;
			button.ForeColor = btnAccentFg;
			button.FlatStyle = FlatStyle.Flat;
			button.FlatAppearance.BorderSize = 0;
			button.Font = msgFont;
			Button btnOK = button;
			btnOK.FlatAppearance.BorderSize = 0;
			btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
			btnOK.MouseEnter += delegate
			{
				btnOK.BackColor = accentHover;
			};
			btnOK.MouseLeave += delegate
			{
				btnOK.BackColor = accentColor;
			};
			dlg.Controls.Add(btnOK);
			Button button2 = new Button();
			button2.Text = "取消";
			button2.DialogResult = DialogResult.Cancel;
			button2.Location = new Point(btnStartX + btnW + btnGap, btnY);
			button2.Size = new Size(btnW, btnH);
			button2.BackColor = btnColor;
			button2.ForeColor = btnFg;
			button2.FlatStyle = FlatStyle.Flat;
			button2.FlatAppearance.BorderSize = 0;
			button2.Font = msgFont;
			Button btnCancel = button2;
			btnCancel.FlatAppearance.BorderColor = btnBorderColor;
			btnCancel.FlatAppearance.BorderSize = 1;
			btnCancel.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
			btnCancel.MouseEnter += delegate
			{
				btnCancel.BackColor = btnHoverColor;
			};
			btnCancel.MouseLeave += delegate
			{
				btnCancel.BackColor = btnColor;
			};
			dlg.Controls.Add(btnCancel);
			dlg.AcceptButton = btnOK;
			dlg.CancelButton = btnCancel;
			break;
		}
		}
		if (isDark)
		{
			int dm = 1;
			try
			{
				DwmSetWindowAttribute(dlg.Handle, 20, ref dm, 4);
			}
			catch
			{
			}
			try
			{
				DwmSetWindowAttribute(dlg.Handle, 19, ref dm, 4);
			}
			catch
			{
			}
		}
		Rectangle screen = Screen.PrimaryScreen.WorkingArea;
		int winTotalW = dlg.Width;
		int winTotalH = dlg.Height;
		int centerX = screen.X + (screen.Width - winTotalW) / 2;
		int centerY = screen.Y + (screen.Height - winTotalH) / 2;
		if (owner != null && owner is Form ownerForm)
		{
			centerX = ownerForm.Left + (ownerForm.Width - winTotalW) / 2;
			centerY = ownerForm.Top + (ownerForm.Height - winTotalH) / 2;
		}
		if (centerX < screen.X)
		{
			centerX = screen.X;
		}
		if (centerY < screen.Y)
		{
			centerY = screen.Y;
		}
		if (centerX + winTotalW > screen.X + screen.Width)
		{
			centerX = screen.X + screen.Width - winTotalW;
		}
		if (centerY + winTotalH > screen.Y + screen.Height)
		{
			centerY = screen.Y + screen.Height - winTotalH;
		}
		dlg.Location = new Point(centerX, centerY);
		// 入场动效：淡入 + 轻微上移（ease-out），尊重 ReduceMotion 无障碍设置
		dlg.Shown += (s, e) =>
		{
			try
			{
				if (AnimationSettings.ReduceMotion)
				{
					dlg.Opacity = 1;
					return;
				}
			}
			catch
			{
				dlg.Opacity = 1;
				return;
			}
			Point target = dlg.Location;
			dlg.Location = new Point(target.X, target.Y + 10);
			dlg.Opacity = 0;
			int steps = 11;
			int cur = 0;
			System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
			fadeTimer.Tick += (s2, e2) =>
			{
				cur++;
				double k = cur / (double)steps;
				double ease = 1 - (1 - k) * (1 - k);
				dlg.Opacity = ease;
				dlg.Location = new Point(target.X, target.Y + (int)(10 * (1 - ease)));
				if (cur >= steps)
				{
					dlg.Opacity = 1;
					dlg.Location = target;
					fadeTimer.Stop();
					fadeTimer.Dispose();
				}
			};
			fadeTimer.Start();
		};
		if (owner != null)
		{
			return dlg.ShowDialog(owner);
		}
		return dlg.ShowDialog();
	}

	public static DialogResult Show(string text, string caption)
	{
		return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}

	public static DialogResult Show(string text)
	{
		return Show(null, text, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}
}
