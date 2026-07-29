using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain
{
	private void StartUpdater(string downloadUrl, string md5 = "")
	{
		Program.StartUpdater(downloadUrl, md5);
		Application.Exit();
	}

	private async void CheckForUpdate()
	{
		try
		{
			UpdateConfig config = null;
			for (int i = 0; i < AppConstants.UpdateMirrors.Length; i++)
			{
				config = await System.Threading.Tasks.Task.Run(() => Program.FetchUpdateConfig(AppConstants.UpdateMirrors[i], 10)).ConfigureAwait(continueOnCapturedContext: true);
				if (config != null)
				{
					break;
				}
			}
			if (config == null)
			{
				DarkMessageBox.Show(this, "检查更新失败：无法连接到更新服务器。", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			if (config.VersionCode > AppConstants.CurrentVersionCode)
			{
				string msg = Program.BuildUpdateMessage(config) + "\n\n" + (config.IsForceUpdate ? "请立即更新后继续使用。" : "是否立即更新？");
				if (config.IsForceUpdate)
				{
					DarkMessageBox.Show(this, msg, "更新", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					StartUpdater(config.DownloadUrl, config.Md5Checksum);
				}
				else if (DarkMessageBox.Show(this, msg, "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
				{
					StartUpdater(config.DownloadUrl, config.Md5Checksum);
				}
			}
			else
			{
				DarkMessageBox.Show(this, "当前已是最新版本。\n\n版本：" + AppConstants.CurrentVersion, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show(this, "检查更新失败：" + ex.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	public bool ShowDisclaimerBeforeStart()
	{
		if (!base.IsHandleCreated)
		{
			CreateHandle();
		}
		using (Graphics g = CreateGraphics())
		{
			dpiScale = g.DpiX / 96f;
		}
		config.Initialize(dpiScale);
		DarkMessageBox.DpiScale = dpiScale;
		LoadConfig();
		if (disclaimerAgreed && skipDisclaimerPrompt)
		{
			return true;
		}
		return ShowDisclaimerDialog();
	}

	private bool ShowDisclaimerDialog()
	{
		bool dialogResult = false;
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Color bgColor = pal.PanelBg;
		Color textColor = pal.InputText;
		Color subTextColor = pal.Muted;
		Color accentColor = pal.Neon;
		Color btnEnabledBg = accentColor;
		Color btnDisabledBg = (isDark ? Color.FromArgb(50, 54, 66) : Color.FromArgb(225, 228, 234));
		Color btnEnabledText = pal.PrimaryText;
		Color btnDisabledText = pal.Muted;
		Color contentBg = pal.PanelBg;
		Color hintColor = pal.Muted;
		Color successColor = theme.SuccessColor;
		Color warningColor = theme.WarnColor;
		int padX = SX(36);
		int contentW = SX(688);
		Form dlg = new Form();
		CheckBox cbAgree;
		bool canAgree;
		bool hasScrolledToBottom;
		int countdownSeconds;
		try
		{
			dlg.Text = "免责声明";
		dlg.StartPosition = FormStartPosition.CenterScreen;
		dlg.MaximizeBox = false;
		dlg.MinimizeBox = false;
		dlg.ShowInTaskbar = true;
		dlg.TopMost = false;
		dlg.Font = GetFont(SF(9f));
		dlg.Icon = this.Icon;
		dlg.ClientSize = new Size(SX(760), SY(790));
			var ctx = NeonChrome.Apply(dlg, pal, "免责声明", dpiScale);
			int ox = ctx.Margin, oy = ctx.Margin + ctx.TitleHeight;
			Point At(int x, int yy) => new Point(x - ox, yy);
			int y = SY(20);
			Label lblTitle = new Label
			{
				Text = "\ud83d\udee1 免责声明",
				Font = GetFont(SF(14f), FontStyle.Bold),
				Location = At(0, y),
				Size = new Size(ctx.Body.Width, SY(40)),
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = textColor,
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(lblTitle);
			y += SY(48);
			Label lblSubtitle = new Label
			{
				Text = "使用本软件前请仔细阅读以下条款",
				Font = GetFont(SF(9f)),
				Location = At(0, y),
				Size = new Size(ctx.Body.Width, SY(20)),
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = subTextColor,
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(lblSubtitle);
			y += SY(36);
			Blend blender = new Blend();
			Panel dividerTop = new Panel
			{
				Location = At(padX, y),
				Size = new Size(contentW, SY(3)),
				BackColor = Color.Transparent
			};
			dividerTop.Paint += delegate(object s, PaintEventArgs pe)
			{
				using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new Rectangle(0, 0, dividerTop.Width, dividerTop.Height), bgColor, accentColor, LinearGradientMode.Horizontal);
				blender.Positions = new float[3] { 0f, 0.5f, 1f };
				blender.Factors = new float[3] { 0f, 1f, 0f };
				linearGradientBrush.Blend = blender;
				pe.Graphics.FillRectangle(linearGradientBrush, 0, 0, dividerTop.Width, dividerTop.Height);
			};
			ctx.Body.Controls.Add(dividerTop);
			y += SY(23);
			string disclaimerText = "第一条  软件性质\n\n本软件仅为「流媒体链接技术检测工具」，仅提供链接连通性、媒体编码、网络延迟检测功能。软件本身不生产、不存储、不提供任何 IPTV 直播源、影视播放地址、电视节目资源。\n\n第二条  责任归属\n\n所有待检测流媒体链接、频道地址均由使用者自行导入、自行获取。用户访问、检测第三方流媒体地址产生的一切著作权纠纷、行政处罚、法律责任，全部由使用者独立承担，与软件开发者无关。\n\n第三条  禁止行为\n\n严禁使用本软件从事以下行为：\n    1. 窃取、破解运营商专网 IPTV 组播信号\n    2. 爬取、售卖、分发无版权直播源\n    3. 搭建商用非法视听、直播服务\n    4. 绕过版权保护收看付费影视、有线电视节目\n\n第四条  合规使用\n\n使用者应当严格遵守《中华人民共和国网络安全法》《中华人民共和国著作权法》《互联网视听节目服务管理规定》等法律法规，仅检测自身拥有合法授权的流媒体链接。\n\n第五条  免责条款\n\n本程序按现状免费提供，不提供任何明示或隐含担保。因使用本软件造成 IP 封禁、网络限制、设备故障等损失，开发者不承担任何赔偿责任。";
			Panel contentPanel = new Panel
			{
				Location = At(padX, y),
				Size = new Size(contentW, SY(400)),
				BackColor = contentBg,
				BorderStyle = BorderStyle.None
			};
			contentPanel.Region = new Region(GetRoundedPath(new Rectangle(0, 0, contentPanel.Width, contentPanel.Height), SX(8)));
			Color contentBorderColor = pal.Border;
			contentPanel.Paint += delegate(object s, PaintEventArgs pe)
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, contentPanel.Width - 1, contentPanel.Height - 1), SX(8));
				using Pen pen = new Pen(contentBorderColor, 2f);
				pe.Graphics.DrawPath(pen, path);
			};
			RichTextBox txtDisclaimer = new RichTextBox
			{
				Text = disclaimerText,
				Multiline = true,
				ReadOnly = true,
				ScrollBars = RichTextBoxScrollBars.Vertical,
				Location = new Point(SX(12), SY(12)),
				Size = new Size(contentW - SX(24), SY(376)),
				Font = GetFont(SF(9f)),
				BackColor = contentBg,
				ForeColor = textColor,
				BorderStyle = BorderStyle.None,
				WordWrap = true,
				DetectUrls = false,
				SelectionTabs = new int[1] { SX(20) }
			};
			txtDisclaimer.SelectAll();
			txtDisclaimer.SelectionAlignment = HorizontalAlignment.Center;
			txtDisclaimer.DeselectAll();
			ApplyDisclaimerFormatting(txtDisclaimer, accentColor);
			contentPanel.Controls.Add(txtDisclaimer);
			ctx.Body.Controls.Add(contentPanel);
			txtDisclaimer.SelectionStart = 0;
			txtDisclaimer.ScrollToCaret();
			y += SY(420);
			Label lblHint = new Label
			{
				Text = "⇩ 请向下滚动阅读全部条款",
				Font = GetFont(SF(8.5f)),
				Location = At(0, y),
				Size = new Size(ctx.Body.Width, SY(22)),
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = hintColor,
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(lblHint);
			y += SY(32);
			cbAgree = new CheckBox
			{
				Text = "我已仔细阅读并同意以上全部条款",
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft,
				CheckAlign = ContentAlignment.MiddleLeft,
				ForeColor = textColor,
				BackColor = Color.Transparent,
				Font = GetFont(SF(9.5f)),
				Enabled = false
			};
			ctx.Body.Controls.Add(cbAgree);
			cbAgree.Location = At((dlg.ClientSize.Width - cbAgree.Width) / 2, y);
			y += SY(36);
			Button btnEnter = new Button
			{
				Text = "进入软件",
				Location = At(padX, y),
				Size = new Size(contentW, SY(40)),
				Font = GetFont(SF(10f), FontStyle.Regular),
				FlatStyle = FlatStyle.Flat,
				Enabled = false,
				BackColor = btnDisabledBg,
				ForeColor = btnDisabledText,
				Cursor = Cursors.Default,
				UseVisualStyleBackColor = false
			};
			btnEnter.FlatAppearance.BorderSize = 0;
			btnEnter.Region = new Region(GetRoundedPath(new Rectangle(0, 0, btnEnter.Width, btnEnter.Height), SX(6)));
			bool btnHover = false;
			bool btnPressed = false;
			bool btnClicked = false;
			btnEnter.MouseEnter += delegate
			{
				if (btnEnter.Enabled)
				{
					btnHover = true;
					btnEnter.Invalidate();
				}
			};
			btnEnter.MouseLeave += delegate
			{
				btnHover = false;
				btnPressed = false;
				btnEnter.Invalidate();
			};
			btnEnter.MouseDown += delegate
			{
				if (btnEnter.Enabled)
				{
					btnPressed = true;
					btnEnter.Invalidate();
				}
			};
			btnEnter.MouseUp += delegate
			{
				btnPressed = false;
				btnEnter.Invalidate();
			};
			btnEnter.MouseClick += delegate
			{
				if (btnEnter.Enabled)
				{
					btnClicked = true;
					btnEnter.Invalidate();
					System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer
					{
						Interval = 150
					};
					animTimer.Tick += delegate
					{
						animTimer.Stop();
						animTimer.Dispose();
						btnClicked = false;
						btnEnter.Invalidate();
					};
					animTimer.Start();
				}
			};
			btnEnter.Paint += delegate(object s, PaintEventArgs pe)
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				Color color = ((btnEnter.Parent != null) ? btnEnter.Parent.BackColor : bgColor);
				using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btnEnter.Width, btnEnter.Height), SX(6)))
				{
					using SolidBrush brush = new SolidBrush(color);
					pe.Graphics.FillPath(brush, path);
				}
				int num = ((btnPressed || btnClicked) ? 2 : 0);
				Rectangle rect = new Rectangle(0, num, btnEnter.Width - 1, btnEnter.Height - 1);
				Color backColor = btnEnter.BackColor;
				Color color2 = ((!btnEnter.Enabled) ? backColor : (btnClicked ? Color.White : (btnPressed ? (isDark ? Color.FromArgb(Math.Max(0, backColor.R - 30), Math.Max(0, backColor.G - 30), Math.Max(0, backColor.B - 30)) : Color.FromArgb(Math.Max(0, backColor.R - 25), Math.Max(0, backColor.G - 25), Math.Max(0, backColor.B - 25))) : ((!btnHover) ? backColor : (isDark ? Color.FromArgb(Math.Min(255, backColor.R + 35), Math.Min(255, backColor.G + 35), Math.Min(255, backColor.B + 35)) : Color.FromArgb(Math.Min(255, backColor.R + 18), Math.Min(255, backColor.G + 18), Math.Min(255, backColor.B + 18)))))));
				using (GraphicsPath path2 = GetRoundedPath(rect, SX(6)))
				{
					using SolidBrush brush2 = new SolidBrush(color2);
					pe.Graphics.FillPath(brush2, path2);
				}
				if (btnHover && btnEnter.Enabled)
				{
					using GraphicsPath path3 = GetRoundedPath(new Rectangle(2, 2 + num, btnEnter.Width - 5, btnEnter.Height - 5), SX(4));
					using Pen pen = new Pen(Color.FromArgb(40, Color.White), 1.5f);
					pe.Graphics.DrawPath(pen, path3);
				}
				TextRenderer.DrawText(pe.Graphics, btnEnter.Text, btnEnter.Font, new Rectangle(0, num, btnEnter.Width, btnEnter.Height), btnEnter.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
			};
			btnEnter.Resize += delegate
			{
				btnEnter.Region?.Dispose();
				btnEnter.Region = new Region(GetRoundedPath(new Rectangle(0, 0, btnEnter.Width, btnEnter.Height), SX(6)));
				btnEnter.Invalidate();
			};
			ctx.Body.Controls.Add(btnEnter);
			canAgree = false;
			hasScrolledToBottom = false;
			bool timerStarted = false;
			bool scrollDetectionActive = false;
			int initialScrollPos = 0;
			countdownSeconds = 10;
			System.Windows.Forms.Timer countdownTimer = new System.Windows.Forms.Timer
			{
				Interval = 1000
			};
			countdownTimer.Tick += delegate
			{
				countdownSeconds--;
				if (countdownSeconds <= 0)
				{
					countdownSeconds = 0;
					countdownTimer.Stop();
					if (hasScrolledToBottom)
					{
						lblHint.Text = "✓ 阅读时间已满足，请勾选同意条款后进入软件";
						lblHint.ForeColor = successColor;
					}
					else
					{
						lblHint.Text = "⏳ 阅读时间已满足，请继续滚动至底部";
						lblHint.ForeColor = warningColor;
					}
					UpdateAgreeState();
				}
				else if (hasScrolledToBottom)
				{
					lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请稍候";
				}
				else
				{
					lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请滚动至底部";
				}
			};
			txtDisclaimer.VScroll += delegate
			{
				if (scrollDetectionActive)
				{
					SCROLLINFO lpsi = new SCROLLINFO
					{
						cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
						fMask = 7u
					};
					GetScrollInfo(txtDisclaimer.Handle, 1, ref lpsi);
					int nPos = lpsi.nPos;
					if (!timerStarted && nPos - initialScrollPos > 30)
					{
						timerStarted = true;
						countdownTimer.Start();
						lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请滚动至底部";
						lblHint.ForeColor = warningColor;
					}
					if (lpsi.nPos + (int)lpsi.nPage >= lpsi.nMax - 2 && !hasScrolledToBottom)
					{
						hasScrolledToBottom = true;
						UpdateAgreeState();
						if (timerStarted && countdownSeconds > 0)
						{
							lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请稍候";
						}
						else if (countdownSeconds <= 0)
						{
							lblHint.Text = "✓ 阅读时间已满足，请勾选同意条款后进入软件";
							lblHint.ForeColor = successColor;
						}
					}
				}
			};
			cbAgree.CheckedChanged += delegate
			{
				bool flag = cbAgree.Checked && canAgree;
				btnEnter.Enabled = flag;
				btnEnter.BackColor = (flag ? btnEnabledBg : btnDisabledBg);
				btnEnter.ForeColor = (flag ? btnEnabledText : btnDisabledText);
				btnEnter.Cursor = (flag ? Cursors.Hand : Cursors.No);
				btnEnter.Invalidate();
			};
			btnEnter.Click += delegate
			{
				dialogResult = true;
				disclaimerAgreed = true;
				countdownTimer.Stop();
				SaveConfig();
				dlg.DialogResult = DialogResult.OK;
				dlg.Close();
			};
			dlg.FormClosing += delegate
			{
				countdownTimer.Stop();
				if (!dialogResult)
				{
					dlg.DialogResult = DialogResult.Cancel;
				}
			};
			dlg.Shown += delegate
			{
				txtDisclaimer.SelectionStart = 0;
				txtDisclaimer.ScrollToCaret();
				cbAgree.Location = At((dlg.ClientSize.Width - cbAgree.Width) / 2, cbAgree.Location.Y);
				System.Windows.Forms.Timer initTimer = new System.Windows.Forms.Timer
				{
					Interval = 200
				};
				initTimer.Tick += delegate
				{
					initTimer.Stop();
					initTimer.Dispose();
					SCROLLINFO lpsi = new SCROLLINFO
					{
						cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
						fMask = 4u
					};
					GetScrollInfo(txtDisclaimer.Handle, 1, ref lpsi);
					initialScrollPos = lpsi.nPos;
					scrollDetectionActive = true;
				};
				initTimer.Start();
			};
			dlg.ShowDialog(this);
			return dialogResult;
		}
		finally
		{
			if (dlg != null)
			{
				((IDisposable)dlg).Dispose();
			}
		}
		void UpdateAgreeState()
		{
			canAgree = hasScrolledToBottom && countdownSeconds <= 0;
			cbAgree.Enabled = canAgree;
			if (!canAgree && cbAgree.Checked)
			{
				cbAgree.Checked = false;
			}
		}
	}

	private void ApplyDisclaimerFormatting(RichTextBox rtb, Color accentColor)
	{
		string[] array = new string[5] { "第一条", "第二条", "第三条", "第四条", "第五条" };
		foreach (string title in array)
		{
			int startIndex = rtb.Text.IndexOf(title);
			if (startIndex >= 0)
			{
				int lineEnd = rtb.Text.IndexOf('\n', startIndex);
				if (lineEnd < 0)
				{
					lineEnd = rtb.Text.Length;
				}
				int lineLength = lineEnd - startIndex;
				while (lineLength > 0 && char.IsWhiteSpace(rtb.Text[startIndex + lineLength - 1]))
				{
					lineLength--;
				}
				rtb.Select(startIndex, lineLength);
				rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
				rtb.SelectionColor = accentColor;
				rtb.SelectionAlignment = HorizontalAlignment.Center;
				rtb.DeselectAll();
			}
		}
	}
}
