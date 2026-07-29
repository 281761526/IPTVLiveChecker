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

	public static float DpiScale { get; set; } = 1f;

	public static bool IsDarkColor(Color color)
	{
		return DrawingUtils.IsDarkColor(color);
	}

	public static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
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
		}
		Font msgFont = new Font("Microsoft YaHei UI", 10.5f);
		using Form dlg = new Form();
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
		switch (icon)
		{
		case MessageBoxIcon.Asterisk:
			picIcon.Image = SystemIcons.Information.ToBitmap();
			break;
		case MessageBoxIcon.Exclamation:
			picIcon.Image = SystemIcons.Warning.ToBitmap();
			break;
		case MessageBoxIcon.Hand:
			picIcon.Image = SystemIcons.Error.ToBitmap();
			break;
		case MessageBoxIcon.Question:
			picIcon.Image = SystemIcons.Question.ToBitmap();
			break;
		default:
			picIcon.Image = SystemIcons.Information.ToBitmap();
			break;
		}
		dlg.Controls.Add(picIcon);
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
			button5.ForeColor = Color.White;
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
			button3.ForeColor = Color.White;
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
			button.ForeColor = Color.White;
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
