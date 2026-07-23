using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IPTVLiveChecker
{
    public class AppTheme
    {
        public string Name { get; set; }
        public Color Primary { get; set; }
        public Color PrimaryDark { get; set; }
        public Color Accent { get; set; }
        public Color Bg { get; set; }
        public Color BgAlt { get; set; }
        public Color Surface { get; set; }
        public Color Border { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color HeaderBg { get; set; }
        public Color SelectRow { get; set; }
        public Color SelectRowText { get; set; }
        public Color StatusBarBg { get; set; }
        public Color NavBg { get; set; }
        public Color TipBg { get; set; }
        public Color PlayBtnBg { get; set; }
        public Color PlayBtnText { get; set; }
        public Color CopyBtnBg { get; set; }
        public Color CopyBtnText { get; set; }
        public Color StatusTagBg { get; set; }
        public Color StatusTagBorder { get; set; }
        public Color LinkTextColor { get; set; }
        public Color SuccessColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color WarnColor { get; set; }
        public Color InfoColor { get; set; }

        public static bool IsSystemDarkTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && (int)value == 0) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static AppTheme GetAutoTheme()
        {
            return IsSystemDarkTheme() ? Dark : Light;
        }

        public static AppTheme Light = new AppTheme
        {
            Name = "浅色",
            Primary = Color.FromArgb(155, 89, 182),
            PrimaryDark = Color.FromArgb(142, 68, 173),
            Accent = Color.FromArgb(255, 107, 129),
            Bg = Color.White,
            BgAlt = Color.White,
            Surface = Color.White,
            Border = Color.FromArgb(230, 230, 238),
            TextPrimary = Color.FromArgb(55, 55, 65),
            TextSecondary = Color.FromArgb(150, 150, 160),
            HeaderBg = Color.FromArgb(248, 248, 252),
            SelectRow = Color.FromArgb(243, 232, 252),
            SelectRowText = Color.FromArgb(55, 55, 65),
            StatusBarBg = Color.FromArgb(189, 190, 200),
            NavBg = Color.White,
            TipBg = Color.FromArgb(245, 248, 252),
            PlayBtnBg = Color.FromArgb(82, 196, 130),
            PlayBtnText = Color.White,
            CopyBtnBg = Color.FromArgb(72, 170, 255),
            CopyBtnText = Color.White,
            StatusTagBg = Color.FromArgb(230, 248, 235),
            StatusTagBorder = Color.FromArgb(46, 184, 113),
            LinkTextColor = Color.FromArgb(100, 100, 115),
            SuccessColor = Color.FromArgb(46, 184, 113),
            ErrorColor = Color.FromArgb(230, 70, 70),
            WarnColor = Color.FromArgb(240, 170, 40),
            InfoColor = Color.FromArgb(52, 152, 219)
        };

        public static AppTheme Dark = new AppTheme
        {
            Name = "深色",
            Primary = Color.FromArgb(160, 110, 225),
            PrimaryDark = Color.FromArgb(180, 130, 240),
            Accent = Color.FromArgb(255, 95, 145),
            Bg = Color.FromArgb(30, 30, 36),
            BgAlt = Color.FromArgb(38, 38, 44),
            Surface = Color.FromArgb(44, 44, 52),
            Border = Color.FromArgb(68, 68, 78),
            TextPrimary = Color.FromArgb(225, 225, 232),
            TextSecondary = Color.FromArgb(155, 155, 168),
            HeaderBg = Color.FromArgb(50, 50, 58),
            SelectRow = Color.FromArgb(75, 52, 105),
            SelectRowText = Color.FromArgb(235, 235, 240),
            StatusBarBg = Color.FromArgb(55, 55, 65),
            NavBg = Color.FromArgb(32, 32, 38),
            TipBg = Color.FromArgb(48, 55, 75),
            PlayBtnBg = Color.FromArgb(70, 130, 240),
            PlayBtnText = Color.White,
            CopyBtnBg = Color.FromArgb(40, 170, 100),
            CopyBtnText = Color.White,
            StatusTagBg = Color.FromArgb(35, 80, 50),
            StatusTagBorder = Color.FromArgb(60, 200, 120),
            LinkTextColor = Color.FromArgb(170, 170, 185),
            SuccessColor = Color.FromArgb(70, 200, 110),
            ErrorColor = Color.FromArgb(235, 80, 80),
            WarnColor = Color.FromArgb(235, 175, 45),
            InfoColor = Color.FromArgb(70, 165, 230)
        };
    }

    // 自定义 TabControl，解决标签头右侧白色留白问题，支持主题切换
    public class DarkTabControl : TabControl
    {
        private Color _headerBg = Color.FromArgb(35, 35, 35);
        private Color _tabBg = Color.FromArgb(35, 35, 35);
        private Color _tabSelectedBg = Color.FromArgb(50, 50, 50);
        private Color _tabHoverBg = Color.FromArgb(60, 60, 60);
        private Color _tabText = Color.FromArgb(180, 180, 180);
        private Color _tabTextSelected = Color.White;
        private int _hoverIndex = -1;

        public int[] TabWidths { get; set; }
        private int _tabHeight = 0;
        public int TabHeight
        {
            get { return _tabHeight; }
            set
            {
                _tabHeight = value;
                if (_tabHeight > 0 && this.IsHandleCreated)
                {
                    this.ItemSize = new Size(this.ItemSize.Width, _tabHeight + 2);
                }
            }
        }
        public int TabXOffset { get; set; } = 0;
        public int TabSpacing { get; set; } = 0;

        public DarkTabControl()
        {
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.SizeMode = TabSizeMode.Fixed;
            this.Padding = new Point(10, 4);
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_tabHeight > 0)
            {
                this.ItemSize = new Size(this.ItemSize.Width, _tabHeight + 2);
            }
        }

        private Rectangle GetCustomTabRect(int index)
        {
            if (TabHeight <= 0)
            {
                TabHeight = this.GetTabRect(0).Height;
            }

            int x = TabXOffset;
            for (int i = 0; i < index; i++)
            {
                if (TabWidths != null && i < TabWidths.Length && TabWidths[i] > 0)
                {
                    x += TabWidths[i] + TabSpacing;
                }
                else
                {
                    x += this.GetTabRect(i).Width + TabSpacing;
                }
            }

            int width;
            if (TabWidths != null && index < TabWidths.Length && TabWidths[index] > 0)
            {
                width = TabWidths[index];
            }
            else
            {
                width = this.GetTabRect(index).Width;
            }

            return new Rectangle(x, 0, width, TabHeight);
        }

        // 根据主题设置配色
        public void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                _headerBg = Color.FromArgb(35, 35, 35);
                _tabBg = Color.FromArgb(35, 35, 35);
                _tabSelectedBg = Color.FromArgb(50, 50, 50);
                _tabHoverBg = Color.FromArgb(60, 60, 60);
                _tabText = Color.FromArgb(180, 180, 180);
                _tabTextSelected = Color.White;
            }
            else
            {
                _headerBg = Color.FromArgb(248, 248, 252);
                _tabBg = Color.FromArgb(248, 248, 252);
                _tabSelectedBg = Color.FromArgb(243, 232, 252);
                _tabHoverBg = Color.FromArgb(240, 240, 245);
                _tabText = Color.FromArgb(100, 100, 115);
                _tabTextSelected = Color.FromArgb(55, 55, 65);
            }
            this.Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 填充整个控件背景（包括标签头区域）
            using (SolidBrush br = new SolidBrush(_headerBg))
                e.Graphics.FillRectangle(br, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 填充整个标签头区域背景（含右侧空白）
            if (this.TabPages.Count > 0)
            {
                int headerH = (TabHeight > 0 ? TabHeight : this.GetTabRect(0).Height) + 4;
                using (SolidBrush br = new SolidBrush(_headerBg))
                    e.Graphics.FillRectangle(br, 0, 0, this.Width, headerH);
            }

            // 绘制每个标签
            for (int i = 0; i < this.TabPages.Count; i++)
            {
                Rectangle tabRect = GetCustomTabRect(i);
                bool isSelected = (i == this.SelectedIndex);
                bool isHover = (i == _hoverIndex);
                Color tabBg;
                if (isSelected) tabBg = _tabSelectedBg;
                else if (isHover) tabBg = _tabHoverBg;
                else tabBg = _tabBg;
                using (SolidBrush br = new SolidBrush(tabBg))
                    e.Graphics.FillRectangle(br, tabRect);
                Color tabFg = isSelected ? _tabTextSelected : _tabText;
                using (SolidBrush br = new SolidBrush(tabFg))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(this.TabPages[i].Text, this.Font, br, tabRect, sf);
                }
            }

            // 绘制 TabPage 内容区域背景
            if (this.TabPages.Count > 0 && this.SelectedIndex >= 0)
            {
                Rectangle displayRect = this.DisplayRectangle;
                using (SolidBrush br = new SolidBrush(_headerBg))
                    e.Graphics.FillRectangle(br, displayRect);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHover = -1;
            for (int i = 0; i < this.TabPages.Count; i++)
            {
                if (GetCustomTabRect(i).Contains(e.Location))
                {
                    newHover = i;
                    break;
                }
            }
            if (newHover != _hoverIndex)
            {
                _hoverIndex = newHover;
                this.Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                this.Invalidate();
            }
        }
    }

    // 自定义深色 MessageBox，替代系统白色 MessageBox
    // 支持深色/浅色主题切换，自动适配DPI缩放，可自定义弹窗大小、按钮样式、圆角等
    public static class DarkMessageBox
    {
        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        /// <param name="rect">矩形区域</param>
        /// <param name="radius">圆角半径（建议设置为按钮高度的一半，如6）</param>
        public static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Windows API：设置窗口属性（深色标题栏）
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2 = 19;

        /// <summary>主题提供器 - 返回当前是否为深色主题</summary>
        public static Func<bool> IsDarkProvider { get; set; } = () => false;
        /// <summary>DPI缩放因子 - 用于自适应不同屏幕分辨率，默认1.0（96DPI）</summary>
        public static float DpiScale { get; set; } = 1f;

        /// <summary>
        /// 应用深色标题栏效果（Windows 10 1809+）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="darkMode">是否启用深色模式（1=深色，0=浅色）</param>
        public static void ApplyDarkTitleBar(IntPtr hwnd, int darkMode)
        {
            int dm = darkMode;
            try { DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dm, 4); } catch { }
            try { DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref dm, 4); } catch { }
        }

        /// <summary>
        /// 显示消息框（无所有者窗口）
        /// </summary>
        /// <param name="text">消息内容</param>
        /// <param name="caption">窗口标题</param>
        /// <param name="buttons">按钮类型（OK/YesNo/OKCancel）</param>
        /// <param name="icon">图标类型（Information/Warning/Error/Question）</param>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return Show(null, text, caption, buttons, icon);
        }

        /// <summary>
        /// 显示消息框（有所有者窗口，居中于所有者）
        /// </summary>
        /// <param name="owner">所有者窗口</param>
        /// <param name="text">消息内容</param>
        /// <param name="caption">窗口标题</param>
        /// <param name="buttons">按钮类型（OK/YesNo/OKCancel）</param>
        /// <param name="icon">图标类型（Information/Warning/Error/Question）</param>
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            // ========== 颜色配置 ==========
            bool isDark = IsDarkProvider();
            Color bgColor = isDark ? Color.FromArgb(40, 40, 50) : Color.White;           // 窗口背景色（深色：深蓝灰 / 浅色：纯白）
            Color textColor = isDark ? Color.FromArgb(230, 230, 240) : Color.FromArgb(40, 40, 40); // 文字颜色（深色：浅灰白 / 浅色：深灰）
            Color accentColor = Color.FromArgb(66, 133, 244);                             // 强调色（蓝色，用于主按钮）
            Color btnColor = isDark ? Color.FromArgb(60, 60, 75) : Color.FromArgb(240, 240, 245); // 次要按钮背景色
            Color btnHoverColor = isDark ? Color.FromArgb(80, 80, 100) : Color.FromArgb(220, 220, 230); // 按钮悬停色
            Color btnBorderColor = isDark ? Color.FromArgb(90, 90, 110) : Color.FromArgb(200, 200, 205); // 按钮边框色
            Color btnFg = isDark ? Color.White : Color.FromArgb(50, 50, 50);               // 次要按钮文字色

            Font msgFont = new Font("Microsoft YaHei UI", 10.5f);  // 消息文字字体（10.5pt）

            using (Form dlg = new Form())
            {
                // ========== 弹窗基础属性 ==========
                dlg.Text = caption;                                    // 窗口标题
                dlg.StartPosition = FormStartPosition.Manual;           // 手动定位（后续计算居中位置）
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;      // 固定对话框样式，禁止调整大小
                dlg.MaximizeBox = false;                               // 禁用最大化按钮
                dlg.MinimizeBox = false;                               // 禁用最小化按钮
                dlg.BackColor = bgColor;                               // 窗口背景色
                dlg.ForeColor = textColor;                             // 默认文字颜色
                dlg.Font = msgFont;                                    // 默认字体
                dlg.ShowInTaskbar = false;                             // 不在任务栏显示
                dlg.TopMost = true;                                    // 置顶显示

                // ========== 尺寸参数配置（均已乘以DpiScale，自动适配高DPI） ==========
                int paddingH = (int)(24 * DarkMessageBox.DpiScale);     // 水平内边距（24px * DPI缩放）
                int paddingV = (int)(20 * DarkMessageBox.DpiScale);     // 垂直内边距（20px * DPI缩放）
                int iconSize = (int)(40 * DarkMessageBox.DpiScale);     // 图标大小（40x40px * DPI缩放）
                int iconGap = (int)(16 * DarkMessageBox.DpiScale);      // 图标与文字间距（16px * DPI缩放）
                int btnGap = (int)(16 * DarkMessageBox.DpiScale);       // 按钮之间间距（16px * DPI缩放）
                int btnW = (int)(85 * DarkMessageBox.DpiScale);         // 按钮宽度（85px * DPI缩放）
                int btnH = (int)(34 * DarkMessageBox.DpiScale);         // 按钮高度（34px * DPI缩放）
                int btnPadding = (int)(16 * DarkMessageBox.DpiScale);   // 按钮区域与内容间距（16px * DPI缩放）
                int minWidth = (int)(320 * DarkMessageBox.DpiScale);    // 弹窗最小宽度（320px * DPI缩放）

                // ========== 消息内容标签 ==========
                // [位置] 图标右侧，水平内边距 + 图标宽 + 图标间距
                // [大小] 根据文字内容自动计算，最小宽度 = minWidth - 2*paddingH - iconSize - iconGap
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

                // 计算实际文本尺寸和弹窗总大小
                int textW = Math.Max(lblText.PreferredWidth, minWidth - paddingH * 2 - iconSize - iconGap);
                int textH = lblText.PreferredHeight;
                int contentW = paddingH + iconSize + iconGap + textW + paddingH;       // 总宽度 = 左内边距 + 图标 + 间距 + 文字 + 右内边距
                int contentH = paddingV + Math.Max(iconSize, textH) + btnPadding + btnH + paddingV; // 总高度 = 上内边距 + 图标/文字高度 + 按钮间距 + 按钮 + 下内边距

                dlg.ClientSize = new Size(Math.Max(contentW, minWidth), contentH);     // 设置弹窗大小（不小于最小宽度）

                // 计算各元素位置
                int iconX = paddingH;                            // 图标X = 左内边距
                int iconY = paddingV;                            // 图标Y = 上内边距
                int textX = paddingH + iconSize + iconGap;        // 文字X = 左内边距 + 图标宽 + 图标间距
                int textY = paddingV;                            // 文字Y = 上内边距
                int btnY = contentH - paddingV - btnH;            // 按钮Y = 总高度 - 下内边距 - 按钮高度

                lblText.Location = new Point(textX, textY);
                lblText.Size = new Size(textW, textH);
                dlg.Controls.Add(lblText);

                // ========== 图标图片框 ==========
                // [位置] 左上角，水平/垂直内边距处
                // [大小] iconSize x iconSize
                PictureBox picIcon = new PictureBox
                {
                    Size = new Size(iconSize, iconSize),
                    Location = new Point(iconX, iconY),
                    BackColor = bgColor,
                    SizeMode = PictureBoxSizeMode.AutoSize
                };
                // 根据图标类型设置对应系统图标
                switch (icon)
                {
                    case MessageBoxIcon.Information:
                        picIcon.Image = SystemIcons.Information.ToBitmap();
                        break;
                    case MessageBoxIcon.Warning:
                        picIcon.Image = SystemIcons.Warning.ToBitmap();
                        break;
                    case MessageBoxIcon.Error:
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

                // ========== 按钮创建（根据按钮类型） ==========
                // 所有按钮统一样式：圆角6px，悬停高亮，无边框

                // --- OK按钮 ---
                if (buttons == MessageBoxButtons.OK)
                {
                    int btnX = (dlg.ClientSize.Width - btnW) / 2;  // 水平居中
                    Button btnOK = new Button
                    {
                        Text = "确定",
                        DialogResult = DialogResult.OK,
                        Location = new Point(btnX, btnY),
                        Size = new Size(btnW, btnH),
                        BackColor = accentColor,           // 蓝色主按钮
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 },
                        Font = msgFont
                    };
                    btnOK.FlatAppearance.BorderSize = 0;
                    btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));  // 圆角6px
                    btnOK.MouseEnter += (s, ev) => btnOK.BackColor = Color.FromArgb(86, 153, 254);         // 悬停高亮
                    btnOK.MouseLeave += (s, ev) => btnOK.BackColor = accentColor;
                    dlg.Controls.Add(btnOK);
                    dlg.AcceptButton = btnOK;              // Enter键触发
                }
                // --- Yes/No按钮 ---
                else if (buttons == MessageBoxButtons.YesNo)
                {
                    int btnGroupW = btnW * 2 + btnGap;     // 按钮组总宽度 = 两个按钮 + 间距
                    int btnStartX = (dlg.ClientSize.Width - btnGroupW) / 2;  // 按钮组水平居中

                    Button btnYes = new Button
                    {
                        Text = "是",
                        DialogResult = DialogResult.Yes,
                        Location = new Point(btnStartX, btnY),
                        Size = new Size(btnW, btnH),
                        BackColor = accentColor,           // 蓝色主按钮
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 },
                        Font = msgFont
                    };
                    btnYes.FlatAppearance.BorderSize = 0;
                    btnYes.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
                    btnYes.MouseEnter += (s, ev) => btnYes.BackColor = Color.FromArgb(86, 153, 254);
                    btnYes.MouseLeave += (s, ev) => btnYes.BackColor = accentColor;
                    dlg.Controls.Add(btnYes);

                    Button btnNo = new Button
                    {
                        Text = "否",
                        DialogResult = DialogResult.No,
                        Location = new Point(btnStartX + btnW + btnGap, btnY),
                        Size = new Size(btnW, btnH),
                        BackColor = btnColor,              // 灰色次要按钮
                        ForeColor = btnFg,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 },
                        Font = msgFont
                    };
                    btnNo.FlatAppearance.BorderColor = btnBorderColor;
                    btnNo.FlatAppearance.BorderSize = 1;
                    btnNo.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
                    btnNo.MouseEnter += (s, ev) => btnNo.BackColor = btnHoverColor;
                    btnNo.MouseLeave += (s, ev) => btnNo.BackColor = btnColor;
                    dlg.Controls.Add(btnNo);
                    dlg.AcceptButton = btnYes;             // Enter键触发"是"
                    dlg.CancelButton = btnNo;              // Escape键触发"否"
                }
                // --- OK/Cancel按钮 ---
                else if (buttons == MessageBoxButtons.OKCancel)
                {
                    int btnGroupW = btnW * 2 + btnGap;
                    int btnStartX = (dlg.ClientSize.Width - btnGroupW) / 2;

                    Button btnOK = new Button
                    {
                        Text = "确定",
                        DialogResult = DialogResult.OK,
                        Location = new Point(btnStartX, btnY),
                        Size = new Size(btnW, btnH),
                        BackColor = accentColor,
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 },
                        Font = msgFont
                    };
                    btnOK.FlatAppearance.BorderSize = 0;
                    btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
                    btnOK.MouseEnter += (s, ev) => btnOK.BackColor = Color.FromArgb(86, 153, 254);
                    btnOK.MouseLeave += (s, ev) => btnOK.BackColor = accentColor;
                    dlg.Controls.Add(btnOK);

                    Button btnCancel = new Button
                    {
                        Text = "取消",
                        DialogResult = DialogResult.Cancel,
                        Location = new Point(btnStartX + btnW + btnGap, btnY),
                        Size = new Size(btnW, btnH),
                        BackColor = btnColor,
                        ForeColor = btnFg,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 0 },
                        Font = msgFont
                    };
                    btnCancel.FlatAppearance.BorderColor = btnBorderColor;
                    btnCancel.FlatAppearance.BorderSize = 1;
                    btnCancel.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), 6));
                    btnCancel.MouseEnter += (s, ev) => btnCancel.BackColor = btnHoverColor;
                    btnCancel.MouseLeave += (s, ev) => btnCancel.BackColor = btnColor;
                    dlg.Controls.Add(btnCancel);
                    dlg.AcceptButton = btnOK;              // Enter键触发"确定"
                    dlg.CancelButton = btnCancel;          // Escape键触发"取消"
                }

                // ========== 深色标题栏适配（Windows 10+） ==========
                if (isDark)
                {
                    int dm = 1;
                    try { DwmSetWindowAttribute(dlg.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dm, 4); } catch { }
                    try { DwmSetWindowAttribute(dlg.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref dm, 4); } catch { }
                }

                // ========== 弹窗位置计算（居中显示） ==========
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int winTotalW = dlg.Width;
                int winTotalH = dlg.Height;
                int centerX = screen.X + (screen.Width - winTotalW) / 2;       // 屏幕居中
                int centerY = screen.Y + (screen.Height - winTotalH) / 2;
                if (owner != null)                                              // 如果有所有者窗口，居中于所有者
                {
                    Form ownerForm = owner as Form;
                    if (ownerForm != null)
                    {
                        centerX = ownerForm.Left + (ownerForm.Width - winTotalW) / 2;
                        centerY = ownerForm.Top + (ownerForm.Height - winTotalH) / 2;
                    }
                }
                // 确保弹窗不超出屏幕边界
                if (centerX < screen.X) centerX = screen.X;
                if (centerY < screen.Y) centerY = screen.Y;
                if (centerX + winTotalW > screen.X + screen.Width) centerX = screen.X + screen.Width - winTotalW;
                if (centerY + winTotalH > screen.Y + screen.Height) centerY = screen.Y + screen.Height - winTotalH;
                dlg.Location = new Point(centerX, centerY);

                // 显示弹窗并等待用户操作
                DialogResult result;
                if (owner != null)
                    result = dlg.ShowDialog(owner);
                else
                    result = dlg.ShowDialog();

                return result;
            }
        }

        /// <summary>
        /// 快捷方法：显示只有确定按钮的消息框（无图标）
        /// </summary>
        public static DialogResult Show(string text, string caption)
        {
            return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 快捷方法：显示只有确定按钮的消息框（无标题，无图标）
        /// </summary>
        public static DialogResult Show(string text)
        {
            return Show(null, text, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class RoundedLabel : Label
    {
        private int _cornerRadius = 8;

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; UpdateRegion(); }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = CreateRoundedRectPath(ClientRectangle, _cornerRadius))
            {
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.Left | TextFormatFlags.Top);
            }
        }

        private void UpdateRegion()
        {
            if (ClientRectangle.Width > 0 && ClientRectangle.Height > 0)
            {
                using (var path = CreateRoundedRectPath(ClientRectangle, _cornerRadius))
                {
                    Region = new Region(path);
                }
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseAllFigures();
            return path;
        }
    }

    public partial class IPTVLiveCheckerMain : Form
    {
        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private DataGridView dgvData;
        private Label lblDetected;
        private Label lblAvailable;
        private Label lblPercent;
        private Label lblStreamInfo;
        private Label lblProgressText;
        private int progressBarWidth;
        private Panel emptyStatePanel;
        private Label emptyLabel;
        private Button btnNavDetect;
        private Panel actionSepRef;
        private ComboBox cboGroup;
        private Panel cboGroupHost;
        private Label lblGroupFilter;
        private TextBox txtSearchBox;
        private HttpClient httpClient;
        private Dictionary<string, string> ipLocationCache = new Dictionary<string, string>();
        private HashSet<string> ipLocationFailed = new HashSet<string>();
        private Dictionary<string, string> domainIpCache = new Dictionary<string, string>();
        private HashSet<string> domainIpFailed = new HashSet<string>();
        private CancellationTokenSource cts;
        private Process _runningPlayer;
        private Process previewProcess;
        private System.Windows.Forms.Timer previewResizeTimer;
        private bool isDetecting = false;
        private bool isPaused = false;
        private Button btnStartDetect;
        private Button btnStopDetect;
        private Button btnExport;
        private Button btnNavSearch;
        private Button btnNavSettings;
        private Button btnNavAbout;
        private Color navBtnHoverBg;
        private string webViewPendingUrl = "";
        private Panel webViewNavPanel = null;
        private ComboBox webViewCboEngine = null;
        private TextBox webViewTxtUrl = null;
        private Label webViewStatusUrl = null;
        private Button btnScanSource;
        private Button btnParseLink;
        private Panel tipBox;
        private bool hasSearchPlatformData = false;
        private bool autoParseLink = false;
        private int detectConcurrency = 10;
        private string detectEngine = "HTTP";
        private string customPlayerPath = "";
        private string ffplayPath = "";
        private string ffprobePath = "";
        private string ffmpegPath = "";
        private string mediainfoPath = "";
        private int timeoutSeconds = 5;
        private bool autoClearInvalid = false;
        private bool persistList = true;
        private bool watchSearchWindow = false;
        private bool showSearchButton = false;
        private bool autoExtractIpPort = false;
        private string loginDataPath = "";
        private List<string> iptvHistoryIps = new List<string>();
        private Dictionary<string, Dictionary<string, string>> savedLogins = new Dictionary<string, Dictionary<string, string>>();
        private int totalCount = 0;
        private int detectedCount = 0;
        private int availableCount = 0;
        private List<ChannelInfo> allChannels = new List<ChannelInfo>();
        private AppTheme theme = AppTheme.GetAutoTheme();

        private void AddChannelToList(string content, string baseUrl, DateTime parseTime = default(DateTime))
        {
            if (parseTime == default(DateTime)) parseTime = DateTime.Now;
            try
            {
                if (baseUrl.Contains("/ZHGXTV/"))
                {
                    ParseZhgxTv(content, baseUrl, parseTime);
                }
                else if (baseUrl.Contains("/iptv/live/1000.json"))
                {
                    ParseKutvJson(content, baseUrl, parseTime);
                }
                else if (baseUrl.Contains("/newlive/live/hls/"))
                {
                    ParseHuashiM3u8(content, baseUrl, parseTime);
                }
                else if (content.Contains("json") || content.StartsWith("{"))
                {
                    var jsonMatches = System.Text.RegularExpressions.Regex.Matches(content, "\"(name|title|channel)\":\\s*\"([^\"]+)\"");
                    var urlMatches = System.Text.RegularExpressions.Regex.Matches(content, "\"(url|link|src)\":\\s*\"([^\"]+)\"");
                    for (int i = 0; i < jsonMatches.Count && i < urlMatches.Count; i++)
                    {
                        string name = jsonMatches[i].Groups[2].Value;
                        string url = urlMatches[i].Groups[2].Value;
                        if (url.StartsWith("/"))
                            url = baseUrl.Replace(baseUrl.Split('/')[3], "") + url.TrimStart('/');
                        allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                    }
                }
                else if (content.Contains(".m3u8"))
                {
                    var m3u8Matches = System.Text.RegularExpressions.Regex.Matches(content, @"^#EXTINF:\d+,\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                    var urlMatches = System.Text.RegularExpressions.Regex.Matches(content, @"^(http[^\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                    for (int i = 0; i < m3u8Matches.Count && i < urlMatches.Count; i++)
                    {
                        string name = m3u8Matches[i].Groups[1].Value;
                        string url = urlMatches[i].Groups[1].Value;
                        if (!url.StartsWith("http"))
                            url = baseUrl + "/" + url;
                        allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                    }
                }
                else if (content.Contains(".txt"))
                {
                    var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("http"))
                        {
                            string name = "直播源";
                            int idx = line.IndexOf(",");
                            if (idx > 0)
                            {
                                name = line.Substring(0, idx);
                            }
                            allChannels.Add(new ChannelInfo { Name = name, Url = line, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                        }
                    }
                }
            }
            catch { }
        }

        private void ParseZhgxTv(string content, string baseUrl, DateTime parseTime)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                var parts = trimmedLine.Split(new[] { ',' }, 2);
                if (parts.Length >= 2)
                {
                    string name = CleanText(parts[0].Trim());
                    string url = parts[1].Trim();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
                    {
                        allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                    }
                }
            }
        }

        private void ParseKutvJson(string content, string baseUrl, DateTime parseTime)
        {
            try
            {
                var ipPortMatch = System.Text.RegularExpressions.Regex.Match(baseUrl, @"http://([^/]+)");
                string baseHttp = ipPortMatch.Success ? ipPortMatch.Value : baseUrl;

                var nameMatches = System.Text.RegularExpressions.Regex.Matches(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                var urlMatches = System.Text.RegularExpressions.Regex.Matches(content, "\"url\"\\s*:\\s*\"([^\"]+)\"");

                for (int i = 0; i < nameMatches.Count && i < urlMatches.Count; i++)
                {
                    string name = CleanText(nameMatches[i].Groups[1].Value);
                    string relUrl = urlMatches[i].Groups[1].Value;
                    if (string.IsNullOrEmpty(relUrl)) continue;

                    string fullUrl = relUrl.StartsWith("http") ? relUrl : $"{baseHttp}{relUrl}";
                    allChannels.Add(new ChannelInfo { Name = name, Url = fullUrl, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                }
            }
            catch { }
        }

        private void ParseHuashiM3u8(string content, string baseUrl, DateTime parseTime)
        {
            if (content.Contains("#EXTM3U"))
            {
                var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string name = "";
                foreach (var line in lines)
                {
                    if (line.StartsWith("#EXTINF:"))
                    {
                        int commaIdx = line.IndexOf(',');
                        name = commaIdx > 0 ? CleanText(line.Substring(commaIdx + 1).Trim()) : "华视频道";
                    }
                    else if (line.StartsWith("http"))
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            allChannels.Add(new ChannelInfo { Name = name, Url = line.Trim(), Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
                            name = "";
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(content) && content.Length < 500)
            {
                allChannels.Add(new ChannelInfo { Name = "华视频道", Url = baseUrl, Group = "解析结果", Status = "未检测", ParseDateTime = parseTime });
            }
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, @"[\x00-\x1F\x7F]", "").Trim();
        }
        private string themePreference = "浅色";
        private static string customFontFamily = "Microsoft YaHei";
        
        private static Font GetFont(float size)
        {
            return new Font(customFontFamily, size);
        }
        
        private static Font GetFont(float size, FontStyle style)
        {
            return new Font(customFontFamily, size, style);
        }
        
        private string configPath = Path.Combine(Application.StartupPath, "config.ini");
        private string channelListPath = Path.Combine(Application.StartupPath, "channellist.txt");
        private bool disclaimerAgreed = false;
        private bool skipDisclaimerPrompt = false;
        
        private void SaveConfig()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Settings]");
            sb.AppendLine($"CustomFontFamily={customFontFamily}");
            sb.AppendLine($"DetectEngine={detectEngine}");
            sb.AppendLine($"DetectConcurrency={detectConcurrency}");
            sb.AppendLine($"TimeoutSeconds={timeoutSeconds}");
            sb.AppendLine($"AutoClearInvalid={autoClearInvalid}");
            sb.AppendLine($"PersistList={persistList}");
            sb.AppendLine($"CustomPlayerPath={customPlayerPath}");
            sb.AppendLine($"WatchSearchWindow={watchSearchWindow}");
            sb.AppendLine($"ShowSearchButton={showSearchButton}");
            sb.AppendLine($"ThemePreference={themePreference}");
            sb.AppendLine($"AutoExtractIpPort={autoExtractIpPort}");
            sb.AppendLine($"AutoParseLink={autoParseLink}");
            sb.AppendLine($"IptvHistoryIps={string.Join("|", iptvHistoryIps)}");

            sb.AppendLine($"DisclaimerAgreed={disclaimerAgreed}");

            sb.AppendLine($"SkipDisclaimerPrompt={skipDisclaimerPrompt}");
            File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 保存直播源列表到文件（格式：名称,链接,分组,状态,分辨率,位置,速度）
        /// </summary>
        private void SaveChannelList()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ch in allChannels)
                {
                    string name = ch.Name?.Replace(",", "，") ?? "";
                    string url = ch.Url ?? "";
                    string group = ch.Group?.Replace(",", "，") ?? "";
                    string status = ch.Status ?? "";
                    string resolution = ch.Resolution ?? "";
                    string location = ch.Location ?? "";
                    string speed = ch.Speed ?? "";
                    sb.AppendLine($"{name},{url},{group},{status},{resolution},{location},{speed}");
                }
                File.WriteAllText(channelListPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// 从文件加载直播源列表（只加载数据，不更新UI）
        /// </summary>
        private void LoadChannelList()
        {
            if (!File.Exists(channelListPath)) return;
            try
            {
                string[] lines = File.ReadAllLines(channelListPath, Encoding.UTF8);
                allChannels.Clear();
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length < 2) continue;
                    string url = parts[1];
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    allChannels.Add(new ChannelInfo
                    {
                        Name = parts[0],
                        Url = url,
                        Group = parts.Length > 2 ? parts[2] : "",
                        Status = parts.Length > 3 ? parts[3] : "未检测",
                        Resolution = parts.Length > 4 ? parts[4] : "",
                        Location = parts.Length > 5 ? parts[5] : "",
                        Speed = parts.Length > 6 ? parts[6] : "",
                        Visible = true
                    });
                }
                totalCount = allChannels.Count;
            }
            catch { }
        }
        
        private void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                SaveConfig();
                return;
            }
            try
            {
                string[] lines = File.ReadAllLines(configPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                    if (line.StartsWith("[")) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    switch (key)
                    {
                        case "CustomFontFamily":
                            customFontFamily = value;
                            break;
                        case "DetectEngine":
                            detectEngine = value;
                            break;
                        case "DetectConcurrency":
                            int.TryParse(value, out detectConcurrency);
                            break;
                        case "TimeoutSeconds":
                            int.TryParse(value, out timeoutSeconds);
                            break;
                        case "AutoClearInvalid":
                            bool.TryParse(value, out autoClearInvalid);
                            break;
                        case "PersistList":
                            bool.TryParse(value, out persistList);
                            break;
                        case "CustomPlayerPath":
                            customPlayerPath = value;
                            break;
                        case "WatchSearchWindow":
                            bool.TryParse(value, out watchSearchWindow);
                            break;
                        case "ShowSearchButton":
                            bool.TryParse(value, out showSearchButton);
                            break;
                        case "ThemePreference":
                            themePreference = value;
                            break;
                        case "AutoExtractIpPort":
                            bool.TryParse(value, out autoExtractIpPort);
                            break;
                        case "AutoParseLink":
                            bool.TryParse(value, out autoParseLink);
                            break;
                        case "IptvHistoryIps":
                            if (!string.IsNullOrEmpty(value))
                            {
                                iptvHistoryIps = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            }
                            break;
                        case "DisclaimerAgreed":
                            bool.TryParse(value, out disclaimerAgreed);
                            break;
                        case "SkipDisclaimerPrompt":
                            bool.TryParse(value, out skipDisclaimerPrompt);
                            break;
                    }
                }
            }
            catch (IOException)
            {
                try { SaveConfig(); } catch { }
            }
            catch { }
        }
        
        private string sortedColumn = null;
        private SortOrder sortDirection = SortOrder.Ascending;
        private Panel outerWrap;
        private ContextMenuStrip dataGridViewContextMenu;
        private Panel mainArea;
        private Panel actionArea;
        private Panel statusBarContainer;
        private Panel statusBarRef;
        private Panel searchPanelRef;
        private Panel searchBoxHostRef;
        private Button btnSearchRef;
        private Panel gridContainerRef;
        private Panel titleBarPanel;
        private Panel bottomBarRef;
        private Button btnThemeToggle;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private PictureBox titleIconRef;
        private string currentView = "检测";
        private bool _applyingTheme = false;
        private float dpiScale = 1f;
        private int SX(int x) { return (int)(x * dpiScale); }
        private int SY(int y) { return (int)(y * dpiScale); }
        private float SF(float f) { return f * dpiScale; }

        private class ChannelInfo
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Location { get; set; }
            public string Resolution { get; set; }
            public string Speed { get; set; }
            public string Group { get; set; }
            public string Status { get; set; }
            public bool Visible { get; set; } = true;
            public DateTime ParseDateTime { get; set; } = DateTime.MinValue;
        }

        /// <summary>
        /// 区域配置基类 - 包含字体、布局和配色的通用结构
        /// 每个功能区域继承此类，实现独立的样式控制
        /// </summary>
        private class RegionConfig
        {
            /// <summary>字体配置 - 控制该区域内所有文本的字体类型和大小</summary>
            public FontConfig Font = new FontConfig();
            /// <summary>布局配置 - 控制该区域内组件的大小、位置和间距</summary>
            public LayoutConfig Layout = new LayoutConfig();
            /// <summary>配色配置 - 控制该区域内组件的颜色设置</summary>
            public ColorConfig Color = new ColorConfig();
        }

        /// <summary>
        /// 字体配置类 - 各区域的字体设置
        /// 每个字体类型控制特定组件的文本样式，可独立调整字号和字体
        /// </summary>
        private class FontConfig
        {
            /// <summary>图标字体 - 控制导航栏图标、按钮图标等图形符号的字体大小</summary>
            public Font Icon;
            /// <summary>普通文字字体 - 控制正文内容、列表项、状态栏文字等通用文本</summary>
            public Font Text;
            /// <summary>标题字体 - 控制窗口标题、页面标题、分组标题等重要标题文本</summary>
            public Font Title;
            /// <summary>按钮字体 - 控制普通按钮上的文字大小</summary>
            public Font Button;
            /// <summary>标签字体 - 控制表单标签、说明文字、分组标签等标签文本</summary>
            public Font Label;
            /// <summary>输入框字体 - 控制文本框、下拉框等输入控件内的文字大小</summary>
            public Font Input;
            /// <summary>提示字体 - 控制占位符文字、错误提示、辅助说明等次要文本</summary>
            public Font Hint;
            /// <summary>内容字体 - 控制数据表格单元格、详情内容等主要数据显示文本</summary>
            public Font Content;
            /// <summary>表头字体 - 控制数据表格表头、弹窗标题栏等标题行文本</summary>
            public Font Header;
            /// <summary>药丸标签字体 - 控制状态标签（检测中、已检测等）上的文字大小</summary>
            public Font Pill;
            /// <summary>URL字体 - 控制链接地址的显示字体，通常使用等宽字体便于阅读</summary>
            public Font Url;
            /// <summary>激活状态字体 - 控制选中/激活状态的文字（如导航栏选中项）</summary>
            public Font Active;
            /// <summary>正常状态字体 - 控制未选中/普通状态的文字（如导航栏未选中项）</summary>
            public Font Normal;
            /// <summary>大按钮字体 - 控制主要操作按钮（如开始检测）上的文字大小</summary>
            public Font Btn;

            public void Initialize(float dpiScale, FontDefaults defaults)
            {
                Icon = defaults.Icon ?? new Font("Segoe UI Symbol", 16f * dpiScale);
                Text = defaults.Text ?? new Font(customFontFamily, 9f * dpiScale);
                Title = defaults.Title ?? new Font(customFontFamily, 11f * dpiScale, FontStyle.Bold);
                Button = defaults.Button ?? new Font(customFontFamily, 8.5f * dpiScale);
                Label = defaults.Label ?? new Font(customFontFamily, 9f * dpiScale);
                Input = defaults.Input ?? new Font(customFontFamily, 8.5f * dpiScale);
                Hint = defaults.Hint ?? new Font(customFontFamily, 8.5f * dpiScale);
                Content = defaults.Content ?? new Font(customFontFamily, 9f * dpiScale);
                Header = defaults.Header ?? new Font(customFontFamily, 9f * dpiScale);
                Pill = defaults.Pill ?? new Font(customFontFamily, 6.7f * dpiScale);
                Url = defaults.Url ?? new Font("Consolas", 6.7f * dpiScale);
                Active = defaults.Active ?? new Font(customFontFamily, 8.5f * dpiScale, FontStyle.Bold);
                Normal = defaults.Normal ?? new Font(customFontFamily, 8.5f * dpiScale);
                Btn = defaults.Btn ?? new Font(customFontFamily, 11f * dpiScale);
            }
        }

        /// <summary>
        /// 字体默认值配置 - 用于各区域初始化
        /// </summary>
        private class FontDefaults
        {
            public Font Icon;
            public Font Text;
            public Font Title;
            public Font Button;
            public Font Label;
            public Font Input;
            public Font Hint;
            public Font Content;
            public Font Header;
            public Font Pill;
            public Font Url;
            public Font Active;
            public Font Normal;
            public Font Btn;
        }

        /// <summary>
        /// 布局配置类 - 各区域的大小和位置设置
        /// 每个布局参数控制特定组件的尺寸、间距或位置，可独立调整布局效果
        /// </summary>
        private class LayoutConfig
        {
            /// <summary>宽度 - 控制区域或控件的整体宽度</summary>
            public int Width;
            /// <summary>高度 - 控制区域或控件的整体高度</summary>
            public int Height;
            /// <summary>最小宽度 - 控制窗口或控件的最小宽度限制</summary>
            public int MinWidth;
            /// <summary>最小高度 - 控制窗口或控件的最小高度限制</summary>
            public int MinHeight;
            /// <summary>内边距 - 控制区域内部内容与边界的距离</summary>
            public int Padding;
            /// <summary>外边距 - 控制区域与其他元素之间的距离</summary>
            public int Margin;
            /// <summary>间距 - 控制区域内元素之间的通用间距</summary>
            public int Gap;
            /// <summary>左偏移 - 控制元素相对于父容器左侧的位置</summary>
            public int Left;
            /// <summary>上偏移 - 控制元素相对于父容器顶部的位置</summary>
            public int Top;
            /// <summary>右偏移 - 控制元素相对于父容器右侧的位置</summary>
            public int Right;
            /// <summary>下偏移 - 控制元素相对于父容器底部的位置</summary>
            public int Bottom;
            /// <summary>图标大小 - 控制图标或图形元素的尺寸（通常为正方形）</summary>
            public int IconSize;
            /// <summary>图标间距 - 控制图标与相邻元素（如文字）之间的距离</summary>
            public int IconGap;
            /// <summary>标题栏高度 - 控制标题栏区域的高度</summary>
            public int TitleHeight;
            /// <summary>标题间距 - 控制标题元素之间的垂直间距</summary>
            public int TitleGap;
            /// <summary>按钮高度 - 控制按钮控件的高度</summary>
            public int BtnHeight;
            /// <summary>按钮宽度 - 控制按钮控件的宽度</summary>
            public int BtnWidth;
            /// <summary>按钮间距 - 控制多个按钮之间的间距</summary>
            public int BtnGap;
            /// <summary>标签宽度 - 控制表单标签或说明文字的宽度</summary>
            public int LabelWidth;
            /// <summary>标签间距 - 控制标签与关联控件之间的距离</summary>
            public int LabelGap;
            /// <summary>输入框高度 - 控制文本框、下拉框等输入控件的高度</summary>
            public int InputHeight;
            /// <summary>圆角半径 - 控制按钮、输入框、弹窗等控件的圆角大小</summary>
            public int CornerRadius;
            /// <summary>行高度 - 控制数据表格行或列表项的高度</summary>
            public int RowHeight;
            /// <summary>表头高度 - 控制数据表格表头行的高度</summary>
            public int HeaderHeight;
            /// <summary>分割线宽度 - 控制分隔元素的线条宽度</summary>
            public int DividerWidth;

            public void Initialize(LayoutDefaults defaults)
            {
                Width = defaults.Width;
                Height = defaults.Height;
                MinWidth = defaults.MinWidth;
                MinHeight = defaults.MinHeight;
                Padding = defaults.Padding;
                Margin = defaults.Margin;
                Gap = defaults.Gap;
                Left = defaults.Left;
                Top = defaults.Top;
                Right = defaults.Right;
                Bottom = defaults.Bottom;
                IconSize = defaults.IconSize;
                IconGap = defaults.IconGap;
                TitleHeight = defaults.TitleHeight;
                TitleGap = defaults.TitleGap;
                BtnHeight = defaults.BtnHeight;
                BtnWidth = defaults.BtnWidth;
                BtnGap = defaults.BtnGap;
                LabelWidth = defaults.LabelWidth;
                LabelGap = defaults.LabelGap;
                InputHeight = defaults.InputHeight;
                CornerRadius = defaults.CornerRadius;
                RowHeight = defaults.RowHeight;
                HeaderHeight = defaults.HeaderHeight;
                DividerWidth = defaults.DividerWidth;
            }
        }

        /// <summary>
        /// 布局默认值配置 - 用于各区域初始化
        /// </summary>
        private class LayoutDefaults
        {
            public int Width = 0;
            public int Height = 0;
            public int MinWidth = 0;
            public int MinHeight = 0;
            public int Padding = 0;
            public int Margin = 0;
            public int Gap = 0;
            public int Left = 0;
            public int Top = 0;
            public int Right = 0;
            public int Bottom = 0;
            public int IconSize = 0;
            public int IconGap = 0;
            public int TitleHeight = 0;
            public int TitleGap = 0;
            public int BtnHeight = 0;
            public int BtnWidth = 0;
            public int BtnGap = 0;
            public int LabelWidth = 0;
            public int LabelGap = 0;
            public int InputHeight = 0;
            public int CornerRadius = 0;
            public int RowHeight = 0;
            public int HeaderHeight = 0;
            public int DividerWidth = 0;
        }

        /// <summary>
        /// 配色配置类 - 各区域的颜色设置
        /// 包含背景色、前景色、边框色、按钮颜色、状态颜色等，支持完整的主题配色
        /// 通过修改这些颜色值，可以自定义界面外观，实现主题切换效果
        /// </summary>
        private class ColorConfig
        {
            /// <summary>背景色 - 控件或区域的主背景色，如窗口背景、面板背景</summary>
            public Color Background;
            /// <summary>浅色背景 - 用于次要区域或悬浮效果，比主背景色稍浅</summary>
            public Color BackgroundLight;
            /// <summary>深色背景 - 用于强调或阴影效果，比主背景色更深</summary>
            public Color BackgroundDark;
            /// <summary>前景色 - 主要文字颜色，用于标题、正文等重要文本</summary>
            public Color Foreground;
            /// <summary>次要前景色 - 次要文字或辅助信息颜色，如副标题、提示文字</summary>
            public Color ForegroundSecondary;
            /// <summary>禁用前景色 - 禁用状态的文字颜色，通常为灰色</summary>
            public Color ForegroundDisabled;
            /// <summary>边框色 - 控件边框颜色，用于分隔不同区域或控件</summary>
            public Color Border;
            /// <summary>浅色边框 - 更细或更浅的边框，用于次要分隔</summary>
            public Color BorderLight;
            /// <summary>标题背景色 - 弹窗标题栏背景色</summary>
            public Color Title;
            /// <summary>主色调 - 品牌色，用于主要按钮、选中状态、强调元素</summary>
            public Color Primary;
            /// <summary>主色调悬停 - 鼠标悬停在主色调元素上时的颜色</summary>
            public Color PrimaryHover;
            /// <summary>主色调激活 - 点击主色调元素时的颜色</summary>
            public Color PrimaryActive;
            /// <summary>强调色 - 用于特殊强调或高亮元素，如错误提示、警告</summary>
            public Color Accent;
            /// <summary>强调色悬停 - 鼠标悬停在强调色元素上时的颜色</summary>
            public Color AccentHover;
            /// <summary>按钮背景色 - 按钮的默认背景色</summary>
            public Color Button;
            /// <summary>按钮悬停色 - 鼠标悬停时的按钮背景色</summary>
            public Color ButtonHover;
            /// <summary>按钮激活色 - 点击时的按钮背景色</summary>
            public Color ButtonActive;
            /// <summary>按钮文字色 - 按钮上文字的颜色</summary>
            public Color ButtonText;
            /// <summary>按钮禁用文字色 - 禁用状态的按钮文字颜色</summary>
            public Color ButtonTextDisabled;
            /// <summary>标签颜色 - 标签文字颜色，如表单标签、分组标签</summary>
            public Color Label;
            /// <summary>次要标签颜色 - 次要标签文字颜色，如辅助说明标签</summary>
            public Color LabelSecondary;
            /// <summary>输入框背景色 - 文本框、下拉框等输入控件的背景色</summary>
            public Color Input;
            /// <summary>输入框聚焦色 - 输入框获得焦点时的边框色，用于提示当前焦点位置</summary>
            public Color InputFocus;
            /// <summary>输入框文字色 - 输入框内文字颜色</summary>
            public Color InputText;
            /// <summary>输入框占位符色 - 输入框占位符提示文字颜色</summary>
            public Color InputPlaceholder;
            /// <summary>成功状态色 - 成功提示、验证通过等状态颜色（通常为绿色）</summary>
            public Color Success;
            /// <summary>警告状态色 - 警告提示、需要注意等状态颜色（通常为橙色）</summary>
            public Color Warning;
            /// <summary>错误状态色 - 错误提示、验证失败等状态颜色（通常为红色）</summary>
            public Color Error;
            /// <summary>信息状态色 - 普通信息提示颜色（通常为蓝色）</summary>
            public Color Info;
            /// <summary>药丸背景色 - 药丸标签的背景色，如"检测中"、"已检测"标签</summary>
            public Color Pill;
            /// <summary>药丸文字色 - 药丸标签上的文字颜色</summary>
            public Color PillText;
            /// <summary>选中状态色 - 选中项的背景色，如导航栏选中项、列表选中项</summary>
            public Color Selected;
            /// <summary>选中悬停色 - 选中项悬停时的背景色</summary>
            public Color SelectedHover;
            /// <summary>滚动条背景色 - 滚动条轨道颜色</summary>
            public Color ScrollBar;
            /// <summary>滚动条悬停色 - 滚动条悬停时的颜色</summary>
            public Color ScrollBarHover;
            /// <summary>滚动条滑块色 - 滚动条拖动块颜色</summary>
            public Color ScrollBarThumb;
            /// <summary>分割线颜色 - 分隔控件的线条颜色</summary>
            public Color Divider;
            /// <summary>表头背景色 - 数据表格表头的背景色</summary>
            public Color Header;
            /// <summary>表头文字色 - 数据表格表头的文字颜色</summary>
            public Color HeaderText;
            /// <summary>行背景色 - 数据表格行的背景色</summary>
            public Color Row;
            /// <summary>行悬停色 - 鼠标悬停时行的背景色</summary>
            public Color RowHover;
            /// <summary>交替行背景色 - 数据表格交替行的背景色，用于区分相邻行</summary>
            public Color RowAlternate;

            /// <summary>
            /// 初始化配色配置
            /// </summary>
            /// <param name="defaults">配色默认值配置</param>
            public void Initialize(ColorDefaults defaults)
            {
                Background = defaults.Background;
                BackgroundLight = defaults.BackgroundLight;
                BackgroundDark = defaults.BackgroundDark;
                Foreground = defaults.Foreground;
                ForegroundSecondary = defaults.ForegroundSecondary;
                ForegroundDisabled = defaults.ForegroundDisabled;
                Border = defaults.Border;
                BorderLight = defaults.BorderLight;
                Title = defaults.Title;
                Primary = defaults.Primary;
                PrimaryHover = defaults.PrimaryHover;
                PrimaryActive = defaults.PrimaryActive;
                Accent = defaults.Accent;
                AccentHover = defaults.AccentHover;
                Button = defaults.Button;
                ButtonHover = defaults.ButtonHover;
                ButtonActive = defaults.ButtonActive;
                ButtonText = defaults.ButtonText;
                ButtonTextDisabled = defaults.ButtonTextDisabled;
                Label = defaults.Label;
                LabelSecondary = defaults.LabelSecondary;
                Input = defaults.Input;
                InputFocus = defaults.InputFocus;
                InputText = defaults.InputText;
                InputPlaceholder = defaults.InputPlaceholder;
                Success = defaults.Success;
                Warning = defaults.Warning;
                Error = defaults.Error;
                Info = defaults.Info;
                Pill = defaults.Pill;
                PillText = defaults.PillText;
                Selected = defaults.Selected;
                SelectedHover = defaults.SelectedHover;
                ScrollBar = defaults.ScrollBar;
                ScrollBarHover = defaults.ScrollBarHover;
                ScrollBarThumb = defaults.ScrollBarThumb;
                Divider = defaults.Divider;
                Header = defaults.Header;
                HeaderText = defaults.HeaderText;
                Row = defaults.Row;
                RowHover = defaults.RowHover;
                RowAlternate = defaults.RowAlternate;
            }
        }

        /// <summary>
        /// 配色默认值配置 - 用于各区域初始化时指定自定义颜色
        /// 未指定的参数默认为Color.Empty，表示使用系统默认颜色
        /// </summary>
        private class ColorDefaults
        {
            public Color Background = Color.Empty;
            public Color BackgroundLight = Color.Empty;
            public Color BackgroundDark = Color.Empty;
            public Color Foreground = Color.Empty;
            public Color ForegroundSecondary = Color.Empty;
            public Color ForegroundDisabled = Color.Empty;
            public Color Border = Color.Empty;
            public Color BorderLight = Color.Empty;
            public Color Title = Color.Empty;
            public Color Primary = Color.Empty;
            public Color PrimaryHover = Color.Empty;
            public Color PrimaryActive = Color.Empty;
            public Color Accent = Color.Empty;
            public Color AccentHover = Color.Empty;
            public Color Button = Color.Empty;
            public Color ButtonHover = Color.Empty;
            public Color ButtonActive = Color.Empty;
            public Color ButtonText = Color.Empty;
            public Color ButtonTextDisabled = Color.Empty;
            public Color Label = Color.Empty;
            public Color LabelSecondary = Color.Empty;
            public Color Input = Color.Empty;
            public Color InputFocus = Color.Empty;
            public Color InputText = Color.Empty;
            public Color InputPlaceholder = Color.Empty;
            public Color Success = Color.Empty;
            public Color Warning = Color.Empty;
            public Color Error = Color.Empty;
            public Color Info = Color.Empty;
            public Color Pill = Color.Empty;
            public Color PillText = Color.Empty;
            public Color Selected = Color.Empty;
            public Color SelectedHover = Color.Empty;
            public Color ScrollBar = Color.Empty;
            public Color ScrollBarHover = Color.Empty;
            public Color ScrollBarThumb = Color.Empty;
            public Color Divider = Color.Empty;
            public Color Header = Color.Empty;
            public Color HeaderText = Color.Empty;
            public Color Row = Color.Empty;
            public Color RowHover = Color.Empty;
            public Color RowAlternate = Color.Empty;
        }

        /// <summary>
        /// 全局配置类 - 统一管理应用程序中所有区域的字体、布局和配色设置
        /// 按功能区域分组，每个区域包含独立的字体(Font)、布局(Layout)和配色(Color)配置
        /// 通过修改各区域配置，可以独立调整每个组件的样式而不影响其他区域
        /// </summary>
        private class AppConfig
        {
            #region 主窗口配置 - 控制整个应用窗口的基础属性
            /// <summary>主窗口配置 - 控制窗口整体尺寸、最小尺寸限制、背景色、边框色等
            /// Font控制：Text - 窗口内通用文字字体
            /// Layout控制：Width/Height(窗口大小)、MinWidth/MinHeight(最小尺寸)、Gap(分隔间距)
            /// Color控制：Background(窗口背景)、BackgroundLight(浅色背景)、Foreground(文字颜色)、Border(边框色)
            /// </summary>
            public RegionConfig Window = new RegionConfig();
            #endregion

            #region 标题栏配置 - 控制窗口顶部标题栏区域
            /// <summary>标题栏配置 - 控制标题栏高度、窗口图标、标题文字、窗口控制按钮(最小化/最大化/关闭)
            /// Font控制：Title - 标题文字字体、Icon - 窗口图标字体
            /// Layout控制：Height(标题栏高度)、Left(图标左偏移)、IconSize(图标大小)、IconGap(图标与文字间距)、BtnWidth(控制按钮宽度)
            /// Color控制：Background(标题栏背景)、Foreground(标题文字)、Button(控制按钮背景)、ButtonHover(按钮悬停)、ButtonText(按钮文字)
            /// </summary>
            public RegionConfig TitleBar = new RegionConfig();
            #endregion

            #region 导航栏配置 - 控制左侧垂直导航区域
            /// <summary>导航栏配置 - 控制左侧垂直导航栏宽度、图标大小、导航文字、选中/悬停状态
            /// Font控制：Icon - 导航图标字体、Text - 导航文字字体、Active - 选中状态文字、Normal - 正常状态文字
            /// Layout控制：Width(导航栏宽度)、IconSize(图标大小)、Gap(导航项间距)、Top(顶部偏移)、IconGap(图标与文字间距)
            /// Color控制：Background(导航栏背景)、Selected(选中项背景)、SelectedHover(选中悬停)、Foreground(导航文字)、Primary(选中边框)、Border(分隔线)
            /// </summary>
            public RegionConfig Navigation = new RegionConfig();
            #endregion

            #region 搜索面板配置 - 控制顶部搜索和分组选择区域
            /// <summary>搜索面板配置 - 控制顶部搜索栏高度、搜索标签、输入框、分组下拉框、搜索按钮
            /// Font控制：Label - "搜一搜"/"分组"标签字体、Input - 输入框字体、Text - 按钮文字字体
            /// Layout控制：Height(搜索栏高度)、Left(左侧偏移)、Padding(内边距)、Gap/BtnWidth/BtnHeight(按钮尺寸)、CornerRadius(圆角)
            /// Color控制：Background(搜索栏背景)、Input(输入框背景)、InputFocus(聚焦边框)、InputText(输入文字)、InputPlaceholder(占位符)、Label(标签颜色)、Button/ButtonHover/ButtonText(按钮样式)
            /// </summary>
            public RegionConfig SearchPanel = new RegionConfig();
            #endregion

            #region 左侧操作区配置 - 控制数据表格左侧的操作按钮区域
            /// <summary>左侧操作区配置 - 控制"选择文件"、"开始检测"、"导出"、"直播源生成器"等操作按钮
            /// Font控制：Title - 区域标题字体、Button - 按钮文字字体、Content - 说明文字、Label - 分组标签字体
            /// Layout控制：Width(区域宽度)、Padding(内边距)、BtnHeight(按钮高度)、BtnGap(按钮间距)、Gap/Top/IconGap(布局参数)
            /// Color控制：Background(区域背景)、Button/ButtonHover/ButtonActive/ButtonText(按钮样式)、Border(边框)
            /// </summary>
            public RegionConfig ActionArea = new RegionConfig();
            #endregion

            #region 数据表格配置 - 控制主数据显示区域
            /// <summary>数据表格配置 - 控制数据表格表头、行、列、分割线、滚动条
            /// Font控制：Content - 单元格内容字体、Header - 表头字体、Pill - 状态标签字体、Button - 操作按钮字体、Url - URL链接字体
            /// Layout控制：HeaderHeight(表头高度)、RowHeight(行高度)、Padding(内边距)、DividerWidth(分割线宽度)
            /// Color控制：Background(表格背景)、Header/HeaderText(表头样式)、Row/RowHover/RowAlternate(行样式)、Foreground/ForegroundSecondary(文字)、Divider(分割线)、ScrollBar相关(滚动条)
            /// </summary>
            public RegionConfig DataGrid = new RegionConfig();
            #endregion

            #region 状态栏配置 - 控制底部状态信息区域
            /// <summary>状态栏配置 - 控制底部状态栏高度、检测数量、运行状态、主题切换按钮
            /// Font控制：Text - 状态文字字体
            /// Layout控制：Height(状态栏高度)、Padding(内边距)、Gap(元素间距)、BtnHeight(按钮高度)、IconSize(图标大小)、CornerRadius(圆角)
            /// Color控制：Background(状态栏背景)、Foreground/ForegroundSecondary(文字)、Button/ButtonHover/ButtonText(按钮样式)、Border(边框)
            /// </summary>
            public RegionConfig StatusBar = new RegionConfig();
            #endregion

            #region 药丸标签配置 - 控制状态标签样式
            /// <summary>药丸标签配置 - 控制"检测中"、"已检测"、"不可用"等状态标签的外观
            /// Font控制：Pill - 药丸文字字体
            /// Layout控制：Height(药丸高度)、CornerRadius(圆角，通常设为高度一半)、Padding(内边距)
            /// Color控制：Pill(药丸背景)、PillText(药丸文字)、Success/Warning/Error/Info(不同状态颜色)
            /// </summary>
            public RegionConfig Pill = new RegionConfig();
            #endregion

            #region 操作按钮配置 - 控制表格内操作按钮样式
            /// <summary>操作按钮配置 - 控制数据表格内每行的"复制"和"播放"小按钮
            /// Font控制：Button - 按钮文字字体
            /// Layout控制：Height/Width(按钮尺寸)、CornerRadius(圆角)、Gap(按钮间距)
            /// Color控制：Button/ButtonHover/ButtonActive(按钮背景)、ButtonText(按钮文字)、Border(边框)
            /// </summary>
            public RegionConfig DataGridButton = new RegionConfig();
            #endregion

            #region 弹窗配置 - 控制各类弹窗窗口
            /// <summary>弹窗配置 - 控制直播源生成器窗口、设置窗口、关于窗口等所有弹窗的样式
            /// Font控制：Title - 弹窗标题、Text - 正文、Input - 输入框、Hint - 提示文字、Btn - 按钮、Url - URL显示
            /// Layout控制：Width/Height(弹窗大小)、CornerRadius(圆角)、TitleHeight(标题栏高度)、Padding(内边距)、BtnWidth/BtnHeight/BtnGap(按钮)、Bottom(底部偏移)
            /// Color控制：Background/BackgroundLight(背景)、Foreground/ForegroundSecondary(文字)、Border(边框)、Input相关(输入框)、Button相关(按钮)、Success/Warning/Error(状态色)
            /// </summary>
            public RegionConfig Dialog = new RegionConfig();
            #endregion

            #region 步骤指示器配置 - 控制步骤进度显示
            /// <summary>步骤指示器配置 - 控制直播源生成器等向导式界面的步骤进度显示(如Step1/Step2/Step3)
            /// Font控制：Text - 步骤文字字体
            /// Layout控制：Height(指示器高度)、IconSize(步骤圆圈大小)、Gap(步骤间距)、IconGap(圆圈与文字间距)
            /// Color控制：Background(背景)、Primary(当前步骤颜色)、Border(边框)、Foreground/ForegroundSecondary(文字)
            /// </summary>
            public RegionConfig StepIndicator = new RegionConfig();
            #endregion

            #region Toast提示配置 - 控制消息提示框
            /// <summary>Toast提示配置 - 控制右下角弹出的消息提示框(如复制成功、导入成功等)
            /// Font控制：Text - 提示文字字体
            /// Layout控制：Width/Height(提示框大小)、CornerRadius(圆角)、Right/Bottom(位置)、IconSize(图标)、IconGap(图标间距)、Gap(显示时长)
            /// Color控制：Background(背景)、Foreground(文字)、Border(边框)、Success/Warning/Error/Info(不同类型颜色)
            /// </summary>
            public RegionConfig Toast = new RegionConfig();
            #endregion

            #region 空状态配置 - 控制无数据时的显示
            /// <summary>空状态配置 - 控制数据表格为空时显示的提示图标和文字
            /// Font控制：Text - 提示文字字体
            /// Layout控制：Width/Height(容器大小)、IconSize(图标大小)、IconGap(图标与文字间距)
            /// Color控制：Background(背景)、Foreground(文字)、Error(红色X图标颜色)
            /// </summary>
            public RegionConfig EmptyState = new RegionConfig();
            #endregion

            #region 右键菜单配置 - 控制上下文菜单样式
            /// <summary>右键菜单配置 - 控制数据表格右键弹出菜单的样式
            /// Font控制：Text - 菜单项文字字体
            /// Layout控制：BtnHeight(菜单项高度)、Padding(内边距)、IconSize(图标大小)、IconGap(图标与文字间距)
            /// Color控制：Background/BackgroundLight(背景)、Foreground(文字)、Selected/SelectedHover(选中状态)、Border(边框)、Error(危险操作颜色)
            /// </summary>
            public RegionConfig ContextMenu = new RegionConfig();
            #endregion

            #region ToggleSwitch控件配置 - 控制开关控件样式
            /// <summary>ToggleSwitch控件配置 - 控制主题切换等开关控件的样式
            /// Font控制：Text - 开关内部文字字体
            /// Layout控制：Width/Height(开关大小)、IconSize(内部滑块大小)
            /// Color控制：Background(关闭状态背景)、Primary(开启状态背景)、Foreground(滑块颜色)、Border(边框)
            /// </summary>
            public RegionConfig ToggleSwitch = new RegionConfig();
            #endregion

            /// <summary>
            /// 初始化所有区域配置
            /// 为每个区域设置默认的字体、布局和配色参数
            /// 修改这里的参数可以调整对应区域的外观
            /// </summary>
            /// <param name="dpiScale">DPI缩放因子，用于适配不同分辨率</param>
            public void Initialize(float dpiScale)
            {
                // ==================== 主窗口配置 ====================
                // 控制：窗口整体尺寸、最小尺寸、背景色
                // 调整参数：MinWidth/MinHeight控制窗口最小大小
                Window.Layout.Initialize(new LayoutDefaults
                {
                    Width = 0, Height = 0, MinWidth = 1280, MinHeight = 800,
                    Gap = 1
                });
                Window.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = GetFont(11f * dpiScale)
                });
                Window.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(30, 30, 30),
                    BackgroundLight = Color.FromArgb(40, 40, 40),
                    Foreground = Color.FromArgb(240, 240, 240),
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 标题栏配置 ====================
                // 控制：窗口顶部标题栏高度、图标、窗口控制按钮
                // 调整参数：Height控制标题栏高度，IconSize控制窗口图标大小
                TitleBar.Layout.Initialize(new LayoutDefaults
                {
                    Height = 32, Left = 12, IconSize = 18, IconGap = 8,
                    BtnWidth = 40
                });
                TitleBar.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(35, 35, 35),
                    Foreground = Color.FromArgb(240, 240, 240),
                    Button = Color.FromArgb(35, 35, 35),
                    ButtonHover = Color.FromArgb(50, 50, 50),
                    ButtonText = Color.FromArgb(200, 200, 200)
                });

                // ==================== 导航栏配置 ====================
                // 控制：左侧垂直导航栏宽度、图标大小、文字样式、选中状态
                // 调整参数：Width控制导航栏宽度，IconSize控制图标大小，Gap控制图标间距
                Navigation.Layout.Initialize(new LayoutDefaults
                {
                    Width = 48, IconSize = 32, Gap = 70, Top = 6,
                    IconGap = 4
                });
                Navigation.Font.Initialize(dpiScale, new FontDefaults
                {
                    Icon = new Font("Segoe UI Symbol", 16f * dpiScale),
                    Text = GetFont(9f * dpiScale),
                    Active = GetFont(9f * dpiScale, FontStyle.Bold),
                    Normal = GetFont(9f * dpiScale)
                });
                Navigation.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(35, 35, 35),
                    Selected = Color.FromArgb(50, 50, 50),
                    SelectedHover = Color.FromArgb(60, 60, 60),
                    Foreground = Color.FromArgb(180, 180, 180),
                    Primary = Color.FromArgb(138, 43, 226),
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 搜索面板配置 ====================
                // 控制：顶部搜索区域高度、输入框、分组选择框、搜索按钮
                // 调整参数：Height控制面板高度，BtnWidth/BtnHeight控制按钮尺寸，CornerRadius控制圆角
                SearchPanel.Layout.Initialize(new LayoutDefaults
                {
                    Height = 46, Left = 12, Padding = 26,
                    Gap = 98, IconGap = 298,
                    BtnWidth = 130, BtnHeight = 26,
                    CornerRadius = 6
                });
                SearchPanel.Font.Initialize(dpiScale, new FontDefaults
                {
                    Label = GetFont(10f * dpiScale),
                    Input = GetFont(8.5f * dpiScale),
                    Text = GetFont(9.5f * dpiScale)
                });
                SearchPanel.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(40, 40, 40),
                    Input = Color.FromArgb(50, 50, 50),
                    InputFocus = Color.FromArgb(138, 43, 226),
                    InputText = Color.FromArgb(240, 240, 240),
                    InputPlaceholder = Color.FromArgb(120, 120, 120),
                    Label = Color.FromArgb(200, 200, 200),
                    Button = Color.FromArgb(138, 43, 226),
                    ButtonHover = Color.FromArgb(158, 63, 246),
                    ButtonText = Color.White,
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 左侧操作区配置 ====================
                // 控制：数据表格左侧的操作按钮列
                // 调整参数：Width控制列宽度，BtnHeight控制按钮高度，BtnGap控制按钮间距
                ActionArea.Layout.Initialize(new LayoutDefaults
                {
                    Width = 180, Padding = 10, BtnHeight = 36,
                    BtnGap = 8, Gap = 130, Top = 8,
                    IconGap = 30
                });
                ActionArea.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = GetFont(11f * dpiScale, FontStyle.Bold),
                    Button = GetFont(8.5f * dpiScale),
                    Content = GetFont(9.5f * dpiScale),
                    Label = GetFont(9.5f * dpiScale, FontStyle.Bold)
                });
                ActionArea.Color.Initialize(new ColorDefaults
                {
                    Background = Color.Transparent,
                    Button = Color.FromArgb(50, 50, 50),
                    ButtonHover = Color.FromArgb(70, 70, 70),
                    ButtonActive = Color.FromArgb(90, 90, 90),
                    ButtonText = Color.FromArgb(220, 220, 220),
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 数据表格配置 ====================
                // 控制：主数据显示表格的表头、行、列、分割线
                // 调整参数：HeaderHeight控制表头高度，RowHeight控制行高度，DividerWidth控制分割线宽度
                DataGrid.Layout.Initialize(new LayoutDefaults
                {
                    HeaderHeight = 36, RowHeight = 30, Padding = 10,
                    DividerWidth = 1
                });
                DataGrid.Font.Initialize(dpiScale, new FontDefaults
                {
                    Content = GetFont(6.7f * dpiScale),
                    Header = GetFont(9f * dpiScale),
                    Pill = GetFont(6.7f * dpiScale),
                    Button = GetFont(6.7f * dpiScale),
                    Url = new Font("Consolas", 6.7f * dpiScale)
                });
                DataGrid.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(30, 30, 30),
                    Header = Color.FromArgb(45, 45, 45),
                    HeaderText = Color.FromArgb(200, 200, 200),
                    Row = Color.FromArgb(30, 30, 30),
                    RowHover = Color.FromArgb(45, 45, 45),
                    RowAlternate = Color.FromArgb(35, 35, 35),
                    Foreground = Color.FromArgb(240, 240, 240),
                    ForegroundSecondary = Color.FromArgb(160, 160, 160),
                    Divider = Color.FromArgb(50, 50, 50),
                    ScrollBar = Color.FromArgb(35, 35, 35),
                    ScrollBarThumb = Color.FromArgb(60, 60, 60),
                    ScrollBarHover = Color.FromArgb(80, 80, 80)
                });

                // ==================== 状态栏配置 ====================
                // 控制：底部状态栏高度、文字、模式切换按钮
                // 调整参数：Height控制状态栏高度，Gap控制元素间距
                StatusBar.Layout.Initialize(new LayoutDefaults
                {
                    Height = 26, Padding = 12, Gap = 10,
                    BtnHeight = 38, IconSize = 6,
                    CornerRadius = 3
                });
                StatusBar.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = GetFont(9.5f * dpiScale)
                });
                StatusBar.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(35, 35, 35),
                    Foreground = Color.FromArgb(180, 180, 180),
                    ForegroundSecondary = Color.FromArgb(140, 140, 140),
                    Button = Color.FromArgb(50, 50, 50),
                    ButtonHover = Color.FromArgb(70, 70, 70),
                    ButtonText = Color.FromArgb(200, 200, 200),
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 药丸标签配置 ====================
                // 控制：状态标签的外观（检测中、已检测、不可用等）
                // 调整参数：Height控制药丸高度，CornerRadius控制圆角（通常设为高度一半），Padding控制内边距
                Pill.Layout.Initialize(new LayoutDefaults
                {
                    Height = 26, CornerRadius = 13, Padding = 12
                });
                Pill.Font.Initialize(dpiScale, new FontDefaults
                {
                    Pill = GetFont(6.7f * dpiScale)
                });
                Pill.Color.Initialize(new ColorDefaults
                {
                    Pill = Color.FromArgb(60, 60, 60),
                    PillText = Color.FromArgb(200, 200, 200),
                    Success = Color.FromArgb(76, 175, 80),
                    Warning = Color.FromArgb(255, 152, 0),
                    Error = Color.FromArgb(244, 67, 54),
                    Info = Color.FromArgb(33, 150, 243)
                });

                // ==================== 操作按钮配置 ====================
                // 控制：数据表格内的小按钮（复制、播放）
                // 调整参数：Height/Width控制按钮尺寸，CornerRadius控制圆角，Gap控制按钮间距
                DataGridButton.Layout.Initialize(new LayoutDefaults
                {
                    Height = 22, Width = 60, CornerRadius = 4, Gap = 4
                });
                DataGridButton.Font.Initialize(dpiScale, new FontDefaults
                {
                    Button = GetFont(6.7f * dpiScale)
                });
                DataGridButton.Color.Initialize(new ColorDefaults
                {
                    Button = Color.FromArgb(50, 50, 50),
                    ButtonHover = Color.FromArgb(70, 70, 70),
                    ButtonActive = Color.FromArgb(90, 90, 90),
                    ButtonText = Color.FromArgb(200, 200, 200),
                    Border = Color.FromArgb(60, 60, 60)
                });

                // ==================== 弹窗配置 ====================
                // 控制：直播源生成器窗口、设置窗口、关于窗口等弹窗
                // 调整参数：Width/Height控制弹窗大小，TitleHeight控制标题栏高度，CornerRadius控制圆角
                Dialog.Layout.Initialize(new LayoutDefaults
                {
                    Width = 900, Height = 750, CornerRadius = 12,
                    TitleHeight = 56, Padding = 32,
                    BtnWidth = 150, BtnHeight = 42,
                    BtnGap = 16, Bottom = 28
                });
                Dialog.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = GetFont(15f * dpiScale, FontStyle.Bold),
                    Text = GetFont(11f * dpiScale),
                    Input = GetFont(11f * dpiScale),
                    Hint = GetFont(11f * dpiScale),
                    Btn = GetFont(11f * dpiScale),
                    Url = new Font("Consolas", 11f * dpiScale)
                });
                Dialog.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(35, 35, 35),
                    BackgroundLight = Color.FromArgb(45, 45, 45),
                    Foreground = Color.FromArgb(240, 240, 240),
                    ForegroundSecondary = Color.FromArgb(160, 160, 160),
                    Border = Color.FromArgb(60, 60, 60),
                    Title = Color.FromArgb(35, 35, 35),
                    HeaderText = Color.FromArgb(240, 240, 240),
                    Input = Color.FromArgb(50, 50, 50),
                    InputFocus = Color.FromArgb(138, 43, 226),
                    InputText = Color.FromArgb(240, 240, 240),
                    InputPlaceholder = Color.FromArgb(120, 120, 120),
                    Button = Color.FromArgb(138, 43, 226),
                    ButtonHover = Color.FromArgb(158, 63, 246),
                    ButtonActive = Color.FromArgb(118, 23, 206),
                    ButtonText = Color.White,
                    Success = Color.FromArgb(76, 175, 80),
                    Warning = Color.FromArgb(255, 152, 0),
                    Error = Color.FromArgb(244, 67, 54)
                });

                // ==================== 步骤指示器配置 ====================
                // 控制：直播源生成器等向导式界面的步骤进度显示
                // 调整参数：Height控制高度，IconSize控制步骤图标大小
                StepIndicator.Layout.Initialize(new LayoutDefaults
                {
                    Height = 105, IconSize = 14, Gap = 2, IconGap = 8
                });
                StepIndicator.Color.Initialize(new ColorDefaults
                {
                    Background = Color.Transparent,
                    Primary = Color.FromArgb(138, 43, 226),
                    Border = Color.FromArgb(60, 60, 60),
                    Foreground = Color.FromArgb(240, 240, 240),
                    ForegroundSecondary = Color.FromArgb(160, 160, 160)
                });

                // ==================== Toast提示配置 ====================
                // 控制：右下角弹出的消息提示框
                // 调整参数：Width/Height控制提示框大小，CornerRadius控制圆角，Right/Bottom控制位置
                Toast.Layout.Initialize(new LayoutDefaults
                {
                    Width = 280, Height = 50, CornerRadius = 8,
                    Right = 20, Bottom = 60, IconSize = 24, IconGap = 12,
                    Gap = 2000
                });
                Toast.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(45, 45, 45),
                    Foreground = Color.FromArgb(240, 240, 240),
                    Border = Color.FromArgb(60, 60, 60),
                    Success = Color.FromArgb(76, 175, 80),
                    Warning = Color.FromArgb(255, 152, 0),
                    Error = Color.FromArgb(244, 67, 54),
                    Info = Color.FromArgb(33, 150, 243)
                });

                // ==================== 空状态配置 ====================
                // 控制：数据表格为空时的提示显示
                // 调整参数：Width/Height控制容器大小，IconSize控制图标大小，IconGap控制图标与文字间距
                EmptyState.Layout.Initialize(new LayoutDefaults
                {
                    Width = 200, Height = 140, IconSize = 64, IconGap = 16
                });
                EmptyState.Color.Initialize(new ColorDefaults
                {
                    Background = Color.Transparent,
                    Foreground = Color.FromArgb(160, 160, 160),
                    Error = Color.FromArgb(244, 67, 54)
                });

                // ==================== 右键菜单配置 ====================
                // 控制：数据表格右键弹出菜单的样式
                // 调整参数：BtnHeight控制菜单项高度，IconSize控制图标大小，IconGap控制图标与文字间距
                ContextMenu.Layout.Initialize(new LayoutDefaults
                {
                    BtnHeight = 28, Padding = 4, IconSize = 16, IconGap = 8
                });
                ContextMenu.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = GetFont(9f * dpiScale)
                });
                ContextMenu.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(45, 45, 45),
                    BackgroundLight = Color.FromArgb(55, 55, 55),
                    Foreground = Color.FromArgb(240, 240, 240),
                    Selected = Color.FromArgb(60, 60, 60),
                    SelectedHover = Color.FromArgb(70, 70, 70),
                    Border = Color.FromArgb(60, 60, 60),
                    Error = Color.FromArgb(244, 67, 54)
                });

                // ==================== ToggleSwitch控件配置 ====================
                // 控制：主题切换等开关控件的样式
                // 调整参数：Width/Height控制开关大小，IconSize控制内部滑块大小
                ToggleSwitch.Layout.Initialize(new LayoutDefaults
                {
                    Width = 70, Height = 24, IconSize = 18
                });
                ToggleSwitch.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = GetFont(8.5f * dpiScale)
                });
                ToggleSwitch.Color.Initialize(new ColorDefaults
                {
                    Background = Color.FromArgb(60, 60, 60),
                    Primary = Color.FromArgb(138, 43, 226),
                    Foreground = Color.White,
                    Border = Color.FromArgb(80, 80, 80)
                });
            }
        }
        /// <summary>全局配置实例，所有组件共享此配置</summary>
        private AppConfig config = new AppConfig();

        private Color ColorPurple => theme.Primary;
        private Color ColorPurpleDark => theme.PrimaryDark;
        private Color ColorPink => theme.Accent;
        private Color ColorGreen => Color.FromArgb(76, 175, 80);
        private Color ColorOrange => Color.FromArgb(255, 152, 0);
        private Color ColorStatusBar => theme.StatusBarBg;
        private Color ColorNavSelected => theme.Primary;
        private Color ColorNavNormal => theme.TextSecondary;
        private Color ColorBorder => theme.Border;

        public IPTVLiveCheckerMain()
        {
            InitializeComponent();
            // 设置 DarkMessageBox 的主题提供者，使其能获取当前主题状态
            DarkMessageBox.IsDarkProvider = () => theme == AppTheme.Dark;
            // 设置 DarkMessageBox 的 DPI 缩放因子
            DarkMessageBox.DpiScale = dpiScale;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.KeyPreview = true;
            this.AllowDrop = true;
            this.DragEnter += IPTVLiveCheckerMain_DragEnter;
            this.DragDrop += IPTVLiveCheckerMain_DragDrop;
            this.KeyDown += IPTVLiveCheckerMain_KeyDown;
            var handler = new System.Net.Http.HttpClientHandler
            {
                MaxConnectionsPerServer = 32,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                UseCookies = false,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(120);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (themePreference == "跟随系统")
                {
                    theme = AppTheme.GetAutoTheme();
                    ApplyTheme();
                }
            };
            FindFFplay();
        }

        private void IPTVLiveCheckerMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private async void IPTVLiveCheckerMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            foreach (string filePath in files)
            {
                try
                {
                    string ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".m3u" || ext == ".m3u8" || ext == ".txt" || ext == ".json")
                    {
                        string content = File.ReadAllText(filePath, Encoding.UTF8);
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        if (ext == ".json")
                        {
                            await ParseTvboxSubscriptionFromContent(content);
                        }
                        else
                        {
                            await ParseM3uContent(content, filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show($"导入文件失败: {ex.Message}", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task ParseTvboxSubscriptionFromContent(string jsonContent)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<Dictionary<string, object>>(jsonContent);

                if (json == null || !json.ContainsKey("lives"))
                {
                    DarkMessageBox.Show("订阅源格式不正确，未找到lives数组", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var livesArray = json["lives"] as List<object>;
                if (livesArray == null || livesArray.Count == 0)
                {
                    DarkMessageBox.Show("订阅源中没有直播源", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int addedCount = 0;
                int duplicateCount = 0;
                var existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));

                foreach (var liveObj in livesArray)
                {
                    var live = liveObj as Dictionary<string, object>;
                    if (live == null) continue;

                    string name = live.ContainsKey("name") ? live["name"]?.ToString()?.Trim() ?? "" : "";
                    string url = live.ContainsKey("url") ? live["url"]?.ToString()?.Trim() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;
                    if (existingUrls.Contains(url.ToLowerInvariant())) { duplicateCount++; continue; }

                    var channel = new ChannelInfo
                    {
                        Name = name,
                        Url = url,
                        Group = live.ContainsKey("group") ? live["group"]?.ToString()?.Trim() ?? "未分组" : "未分组",
                        Status = "未检测",
                        Location = ExtractLocationFromUrl(url)
                    };
                    allChannels.Add(channel);
                    existingUrls.Add(url.ToLowerInvariant());
                    addedCount++;
                }

                RefreshGrid();
                totalCount = allChannels.Count;
                UpdateStatusBar();
                UpdateEmptyState();
                DarkMessageBox.Show($"成功导入 {addedCount} 个直播源，{duplicateCount} 个重复", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show($"解析JSON失败: {ex.Message}", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ParseM3uContent(string content, string fileName)
        {
            try
            {
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int addedCount = 0;
                int duplicateCount = 0;
                var existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));
                string currentGroup = "未分组";
                string currentName = "";

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#EXTGRP:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentGroup = trimmed.Substring(8).Trim();
                    }
                    else if (trimmed.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        int colonIdx = trimmed.IndexOf(':');
                        int commaIdx = trimmed.IndexOf(',');
                        if (commaIdx > colonIdx)
                        {
                            currentName = trimmed.Substring(commaIdx + 1).Trim();
                        }
                        else
                        {
                            currentName = "";
                        }
                    }
                    else if (!trimmed.StartsWith("#") && Uri.IsWellFormedUriString(trimmed, UriKind.Absolute))
                    {
                        string url = trimmed;
                        string name = string.IsNullOrWhiteSpace(currentName) ? Path.GetFileNameWithoutExtension(fileName) : currentName;

                        if (existingUrls.Contains(url.ToLowerInvariant())) { duplicateCount++; continue; }

                        var channel = new ChannelInfo
                        {
                            Name = name,
                            Url = url,
                            Group = currentGroup,
                            Status = "未检测",
                            Location = ExtractLocationFromUrl(url)
                        };
                        allChannels.Add(channel);
                        existingUrls.Add(url.ToLowerInvariant());
                        addedCount++;
                        currentName = "";
                    }
                }

                RefreshGrid();
                totalCount = allChannels.Count;
                UpdateStatusBar();
                UpdateEmptyState();
                DarkMessageBox.Show($"成功导入 {addedCount} 个直播源，{duplicateCount} 个重复", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show($"解析M3U文件失败: {ex.Message}", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 在系统PATH、应用目录和常见位置查找ffplay.exe、ffprobe.exe、ffmpeg.exe和mediainfo.exe
        /// </summary>
        private void FindFFplay()
        {
            ffplayPath = "";
            ffprobePath = "";
            ffmpegPath = "";
            mediainfoPath = "";
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? new string[0];
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string[] extraDirs = {
                appDir,
                Path.Combine(appDir, "ffmpeg", "bin"),
                Path.Combine(appDir, "bin"),
                Path.Combine(appDir, "mediainfo"),
                @"C:\ffmpeg\bin",
                @"C:\Program Files\ffmpeg\bin",
                @"C:\Program Files\MediaInfo",
                @"C:\Program Files (x86)\MediaInfo",
                @"C:\msys64\ucrt64\bin",
                @"C:\msys64\mingw64\bin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "mediainfo"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "mediainfo")
            };
            var allDirs = paths.Concat(extraDirs).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct();
            foreach (string dir in allDirs)
            {
                try
                {
                    string d = dir.Trim();
                    if (string.IsNullOrEmpty(ffplayPath))
                    {
                        string fp = Path.Combine(d, "ffplay.exe");
                        if (File.Exists(fp)) ffplayPath = fp;
                    }
                    if (string.IsNullOrEmpty(ffprobePath))
                    {
                        string fp2 = Path.Combine(d, "ffprobe.exe");
                        if (File.Exists(fp2)) ffprobePath = fp2;
                    }
                    if (string.IsNullOrEmpty(ffmpegPath))
                    {
                        string fp3 = Path.Combine(d, "ffmpeg.exe");
                        if (File.Exists(fp3)) ffmpegPath = fp3;
                    }
                    if (string.IsNullOrEmpty(mediainfoPath))
                    {
                        string fp4 = Path.Combine(d, "mediainfo.exe");
                        if (File.Exists(fp4)) mediainfoPath = fp4;
                    }
                    if (!string.IsNullOrEmpty(ffplayPath) && !string.IsNullOrEmpty(ffprobePath) && !string.IsNullOrEmpty(ffmpegPath) && !string.IsNullOrEmpty(mediainfoPath)) break;
                }
                catch { }
            }
        }

        private bool FFComponentsReady()
        {
            return !string.IsNullOrEmpty(ffplayPath) && File.Exists(ffplayPath)
                && !string.IsNullOrEmpty(ffprobePath) && File.Exists(ffprobePath)
                && !string.IsNullOrEmpty(ffmpegPath) && File.Exists(ffmpegPath);
        }

        private bool MediaInfoReady()
        {
            return !string.IsNullOrEmpty(mediainfoPath) && File.Exists(mediainfoPath);
        }

        private async Task CheckAndDownloadComponentsAsync()
        {
            FindFFplay();
            bool hasFfmpeg = FFComponentsReady();
            bool hasMediaInfo = MediaInfoReady();

            if (!hasFfmpeg)
            {
                string message = $"当前检测模式：极速HTTP检测\n\n" +
                    $"此模式仅检测链接可用性，无法获取视频分辨率。\n\n" +
                    $"若要启用完整功能（获取分辨率、内置播放），\n" +
                    $"需要下载安装 FFmpeg 组件。\n\n" +
                    $"是否下载安装？";

                var dr = DarkMessageBox.Show(this, message, "检测模式", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes)
                {
                    bool ok = await DownloadFFmpegAsync(this);
                    FindFFplay();
                    if (ok && FFComponentsReady())
                    {
                        detectEngine = "FFMPEG";
                        DarkMessageBox.Show(this, "🎉 FFmpeg 组件安装成功！\n\n已自动切换到【完整检测模式】\n\n• 将获取视频分辨率信息\n• 支持内置播放器播放", "安装成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        DarkMessageBox.Show(this, "⚠ FFmpeg 安装失败\n\n将继续使用【极速HTTP检测】模式\n\n• 无法获取分辨率\n• 默认播放器不可用\n\n可稍后手动下载 ffmpeg 并解压到程序目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            if (!hasMediaInfo && detectEngine == "FFMPEG")
            {
                string message = $"检测到 MediaInfo 组件缺失\n\n" +
                    $"MediaInfo 功能说明：\n" +
                    $"• 提高分辨率检测精度\n" +
                    $"• 支持更多视频格式解析\n" +
                    $"• 补充 FFmpeg 无法识别的编码\n\n" +
                    $"是否下载安装 MediaInfo 组件？";

                var dr = DarkMessageBox.Show(this, message, "组件提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes)
                {
                    bool ok = await DownloadMediaInfoAsync(this);
                    FindFFplay();
                    if (!ok && !MediaInfoReady())
                    {
                        DarkMessageBox.Show(this, "⚠ MediaInfo 安装失败\n\n分辨率检测将使用 FFmpeg 方案\n\n可稍后手动下载 mediainfo 并解压到程序目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// 创建下载进度对话框
        /// 用于显示FFmpeg/MediaInfo组件下载进度和日志信息
        /// </summary>
        /// <returns>下载对话框窗体，Tag属性包含(lblStatus, progressBar, txtLog)三元组，供外部更新UI</returns>
        private Form CreateDownloadForm()
        {
            // ========== 颜色配置（根据主题自动切换） ==========
            bool isDarkFF = IsDarkColor(theme.Bg);
            Color ffBg = isDarkFF ? Color.FromArgb(45, 45, 55) : Color.White;           // 窗口背景色（深色：深蓝灰 / 浅色：纯白）
            Color ffText = isDarkFF ? Color.FromArgb(220, 220, 230) : Color.FromArgb(50, 55, 65); // 标题文字颜色
            Color ffStatus = isDarkFF ? Color.FromArgb(160, 160, 175) : Color.FromArgb(100, 105, 115); // 状态文字颜色（比标题略暗）
            Color ffLogBg = isDarkFF ? Color.FromArgb(30, 30, 38) : Color.FromArgb(248, 249, 250); // 日志区域背景色
            Color ffLogFg = isDarkFF ? Color.FromArgb(180, 180, 190) : Color.FromArgb(50, 55, 65); // 日志文字颜色

            // ========== 主窗口配置 ==========
            // [位置] 屏幕居中 [大小] 580x280（已乘以DPI缩放）[样式] 固定对话框
            Form dlg = new Form
            {
                Text = "正在安装 FFmpeg 组件",          // 窗口标题（调用方会根据下载内容修改）
                Size = new Size(SX(580), SY(280)),       // 窗口大小（宽580px，高280px，已适配DPI）
                StartPosition = FormStartPosition.CenterScreen,    // 居中显示
                FormBorderStyle = FormBorderStyle.FixedDialog,     // 固定对话框样式，禁止调整大小
                MaximizeBox = false,                               // 禁用最大化按钮
                MinimizeBox = false,                               // 禁用最小化按钮
                BackColor = ffBg,                                  // 窗口背景色
                ShowInTaskbar = false,                             // 不在任务栏显示
                TopMost = true                                     // 置顶显示
            };
            SetFormDarkModeTitleBar(dlg, isDarkFF);      // 应用深色标题栏

            // ========== 标题标签 ==========
            // [位置] (20, 18) [大小] 530x30 [字体] YaHei 11pt 加粗
            Label lblTitle = new Label
            {
                Text = "⏳ 正在下载 FFmpeg 组件（播放和检测功能必需）",
                Font = GetFont(SF(11f), FontStyle.Bold),
                ForeColor = ffText,
                Location = new Point(SX(20), SY(18)),
                Size = new Size(SX(530), SY(30)),
                TextAlign = ContentAlignment.TopLeft
            };
            dlg.Controls.Add(lblTitle);

            // ========== 状态标签 ==========
            // [位置] (20, 55) [大小] 530x22 [字体] YaHei 9.5pt [颜色] ffStatus（次要文字颜色）
            Label lblStatus = new Label
            {
                Text = "正在准备下载...",
                Font = GetFont(SF(9.5f)),
                ForeColor = ffStatus,
                Location = new Point(SX(20), SY(55)),
                Size = new Size(SX(530), SY(22)),
                TextAlign = ContentAlignment.TopLeft
            };
            dlg.Controls.Add(lblStatus);

            // ========== 进度条 ==========
            // [位置] (20, 85) [大小] 525x24 [样式] 分段块式 [范围] 0-100
            ProgressBar progressBar = new ProgressBar
            {
                Location = new Point(SX(20), SY(85)),
                Width = SX(525),
                Height = SY(24),
                Style = ProgressBarStyle.Blocks,      // 分段块式进度条
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            dlg.Controls.Add(progressBar);

            // ========== 日志文本框 ==========
            // [位置] (20, 120) [大小] 525x100 [字体] Consolas 8.5pt（等宽字体，便于日志阅读）
            // [属性] 多行、只读、垂直滚动条
            TextBox txtLog = new TextBox
            {
                Location = new Point(SX(20), SY(120)),
                Width = SX(525),
                Height = SY(100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", SF(8.5f)),
                BackColor = ffLogBg,
                ForeColor = ffLogFg,
                BorderStyle = BorderStyle.FixedSingle
            };
            dlg.Controls.Add(txtLog);

            // 将三个控件打包存入Tag，供外部更新UI使用
            dlg.Tag = new Tuple<Label, ProgressBar, TextBox>(lblStatus, progressBar, txtLog);
            return dlg;
        }

        private async Task<bool> DownloadFFmpegAsync(IWin32Window owner)
        {
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string tempDir = Path.Combine(Path.GetTempPath(), "wtv_ffmpeg_dl_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string zipPath = Path.Combine(tempDir, "ffmpeg.zip");
            string extractDir = Path.Combine(tempDir, "extract");

            bool downloadSuccess = false;
            try
            {
                Directory.CreateDirectory(tempDir);

                Form dlg = CreateDownloadForm();
                var tuple = (Tuple<Label, ProgressBar, TextBox>)dlg.Tag;
                Label lblStatus = tuple.Item1;
                ProgressBar progressBar = tuple.Item2;
                TextBox txtLog = tuple.Item3;

                void Log(string msg)
                {
                    if (txtLog.IsDisposed) return;
                    if (txtLog.InvokeRequired)
                        txtLog.BeginInvoke(new Action(() => { txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n"); }));
                    else
                        txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
                }
                void SetStatus(string msg)
                {
                    if (lblStatus.IsDisposed) return;
                    if (lblStatus.InvokeRequired)
                        lblStatus.BeginInvoke(new Action(() => { lblStatus.Text = msg; }));
                    else
                        lblStatus.Text = msg;
                }
                void SetProgress(int pct)
                {
                    if (progressBar.IsDisposed) return;
                    if (progressBar.InvokeRequired)
                        progressBar.BeginInvoke(new Action(() => { progressBar.Value = Math.Max(0, Math.Min(100, pct)); }));
                    else
                        progressBar.Value = Math.Max(0, Math.Min(100, pct));
                }

                dlg.Shown += async (s, e) =>
                {
                    try
                    {
                        string[] urls = new string[]
                        {
                            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
                            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip",
                        };
                        string downloadedFile = null;
                        Exception lastErr = null;
                        foreach (string dlUrl in urls)
                        {
                            try
                            {
                                SetStatus("正在连接下载服务器：" + new Uri(dlUrl).Host);
                                Log("尝试下载：" + dlUrl);
                                SetProgress(2);

                                using (var client = new System.Net.WebClient())
                                {
                                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                                    client.DownloadProgressChanged += (cs, ce) =>
                                    {
                                        if (ce.TotalBytesToReceive > 0)
                                        {
                                            int pct = (int)Math.Min(90, 2 + (ce.BytesReceived * 88L / ce.TotalBytesToReceive));
                                            SetProgress(pct);
                                            SetStatus($"正在下载... {(ce.BytesReceived / 1024.0 / 1024.0):F1}MB / {(ce.TotalBytesToReceive / 1024.0 / 1024.0):F1}MB");
                                        }
                                        else
                                        {
                                            SetStatus($"正在下载... {(ce.BytesReceived / 1024.0 / 1024.0):F1}MB");
                                        }
                                    };
                                    await client.DownloadFileTaskAsync(new Uri(dlUrl), zipPath);
                                }
                                if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 1024 * 1024)
                                {
                                    downloadedFile = zipPath;
                                    Log("下载完成：" + (new FileInfo(zipPath).Length / 1024.0 / 1024.0).ToString("F1") + "MB");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                lastErr = ex;
                                Log("下载失败：" + ex.Message);
                                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                            }
                        }
                        if (downloadedFile == null)
                        {
                            SetStatus("下载失败，正在尝试备用方式...");
                            Log("主下载地址均失败，尝试PowerShell方式...");
                            try
                            {
                                downloadedFile = await DownloadViaPowerShell(zipPath, Log, SetStatus, SetProgress);
                            }
                            catch (Exception pex) { Log("PowerShell下载失败: " + pex.Message); }
                        }

                        if (downloadedFile == null || !File.Exists(downloadedFile))
                        {
                            Log("所有下载方式均失败");
                            DarkMessageBox.Show(dlg, "FFmpeg 自动下载失败，请手动下载 ffmpeg 并将 ffmpeg.exe、ffplay.exe、ffprobe.exe 放到程序根目录。\n下载地址：https://www.gyan.dev/ffmpeg/builds/", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dlg.DialogResult = DialogResult.Cancel;
                            dlg.Close();
                            return;
                        }

                        SetStatus("正在解压...");
                        SetProgress(92);
                        Log("开始解压文件...");
                        Directory.CreateDirectory(extractDir);
                        try { await ExtractZipAsync(downloadedFile, extractDir, Log); }
                        catch (Exception zex) { Log("解压失败: " + zex.Message); }

                        SetStatus("正在查找组件...");
                        SetProgress(97);
                        Log("查找 ffmpeg.exe/ffplay.exe/ffprobe.exe...");
                        string fp = FindFileInDir(extractDir, "ffplay.exe");
                        string fpr = FindFileInDir(extractDir, "ffprobe.exe");
                        string ffm = FindFileInDir(extractDir, "ffmpeg.exe");
                        if (string.IsNullOrEmpty(fp) || string.IsNullOrEmpty(fpr) || string.IsNullOrEmpty(ffm))
                        {
                            Log("未在解压目录找到所有组件！");
                            try
                            {
                                string allFiles = string.Join(", ", Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories).Select(f => Path.GetFileName(f)).Take(20));
                                Log("找到的exe：" + allFiles);
                            }
                            catch { }
                            DarkMessageBox.Show(dlg, "已下载但未能在压缩包中找到所需组件，请手动将 ffmpeg.exe、ffplay.exe、ffprobe.exe 复制到程序目录：\n" + appDir, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dlg.DialogResult = DialogResult.Cancel;
                            dlg.Close();
                            return;
                        }

                        File.Copy(fp, Path.Combine(appDir, "ffplay.exe"), true);
                        File.Copy(fpr, Path.Combine(appDir, "ffprobe.exe"), true);
                        File.Copy(ffm, Path.Combine(appDir, "ffmpeg.exe"), true);
                        Log("已复制组件到：" + appDir);

                        FindFFplay();
                        SetProgress(100);
                        SetStatus("✅ 安装完成！");
                        Log("FFmpeg 组件安装成功！");
                        downloadSuccess = true;
                        await Task.Delay(500);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        Log("安装异常：" + ex.Message);
                        try
                        {
                            DarkMessageBox.Show(dlg, "FFmpeg 安装过程出错：\n" + ex.Message + "\n\n请手动从 https://www.gyan.dev/ffmpeg/builds/ 下载并解压到程序目录。", "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch { }
                        dlg.DialogResult = DialogResult.Cancel;
                        dlg.Close();
                    }
                };

                dlg.ShowDialog(owner);
                return downloadSuccess && FFComponentsReady();
            }
            catch
            {
                return false;
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private async Task<bool> DownloadMediaInfoAsync(IWin32Window owner)
        {
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string tempDir = Path.Combine(Path.GetTempPath(), "wtv_mediainfo_dl_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string zipPath = Path.Combine(tempDir, "mediainfo.zip");
            string extractDir = Path.Combine(tempDir, "extract");

            bool downloadSuccess = false;
            try
            {
                Directory.CreateDirectory(tempDir);

                Form dlg = CreateDownloadForm();
                dlg.Text = "正在安装 MediaInfo 组件";
                var tuple = (Tuple<Label, ProgressBar, TextBox>)dlg.Tag;
                Label lblStatus = tuple.Item1;
                ProgressBar progressBar = tuple.Item2;
                TextBox txtLog = tuple.Item3;

                void Log(string msg)
                {
                    if (txtLog.IsDisposed) return;
                    if (txtLog.InvokeRequired)
                        txtLog.BeginInvoke(new Action(() => { txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n"); }));
                    else
                        txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
                }
                void SetStatus(string msg)
                {
                    if (lblStatus.IsDisposed) return;
                    if (lblStatus.InvokeRequired)
                        lblStatus.BeginInvoke(new Action(() => { lblStatus.Text = msg; }));
                    else
                        lblStatus.Text = msg;
                }
                void SetProgress(int pct)
                {
                    if (progressBar.IsDisposed) return;
                    if (progressBar.InvokeRequired)
                        progressBar.BeginInvoke(new Action(() => { progressBar.Value = Math.Max(0, Math.Min(100, pct)); }));
                    else
                        progressBar.Value = Math.Max(0, Math.Min(100, pct));
                }

                dlg.Shown += async (s, e) =>
                {
                    try
                    {
                        bool is64Bit = Environment.Is64BitOperatingSystem;
                        string arch = is64Bit ? "x64" : "i386";
                        string version = "26.05";
                        Log($"系统架构：{(is64Bit ? "64位" : "32位")}，选择 MediaInfo {arch} 版本");

                        string[] urls = new string[]
                        {
                            $"https://mediaarea.net/download/binary/mediainfo/{version}/MediaInfo_CLI_{version}_Windows_{arch}.zip",
                            $"https://github.com/MediaArea/MediaInfo/releases/download/v{version}/MediaInfo_CLI_{version}_Windows_{arch}.zip",
                        };
                        string downloadedFile = null;
                        Exception lastErr = null;
                        foreach (string dlUrl in urls)
                        {
                            try
                            {
                                SetStatus("正在连接下载服务器：" + new Uri(dlUrl).Host);
                                Log("尝试下载：" + dlUrl);
                                SetProgress(2);

                                using (var client = new System.Net.WebClient())
                                {
                                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                                    client.DownloadProgressChanged += (cs, ce) =>
                                    {
                                        if (ce.TotalBytesToReceive > 0)
                                        {
                                            int pct = (int)Math.Min(90, 2 + (ce.BytesReceived * 88L / ce.TotalBytesToReceive));
                                            SetProgress(pct);
                                            SetStatus($"正在下载... {(ce.BytesReceived / 1024.0 / 1024.0):F1}MB / {(ce.TotalBytesToReceive / 1024.0 / 1024.0):F1}MB");
                                        }
                                        else
                                        {
                                            SetStatus($"正在下载... {(ce.BytesReceived / 1024.0 / 1024.0):F1}MB");
                                        }
                                    };
                                    await client.DownloadFileTaskAsync(new Uri(dlUrl), zipPath);
                                }
                                if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 100 * 1024)
                                {
                                    downloadedFile = zipPath;
                                    Log("下载完成：" + (new FileInfo(zipPath).Length / 1024.0 / 1024.0).ToString("F1") + "MB");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                lastErr = ex;
                                Log("下载失败：" + ex.Message);
                                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                            }
                        }
                        if (downloadedFile == null)
                        {
                            SetStatus("下载失败，正在尝试备用方式...");
                            Log("主下载地址均失败，尝试PowerShell方式...");
                            try
                            {
                                string psUrl = $"https://mediaarea.net/download/binary/mediainfo/{version}/MediaInfo_CLI_{version}_Windows_{arch}.zip";
                                downloadedFile = await DownloadViaPowerShellWithUrl(zipPath, psUrl, Log, SetStatus, SetProgress);
                            }
                            catch (Exception pex) { Log("PowerShell下载失败: " + pex.Message); }
                        }

                        if (downloadedFile == null || !File.Exists(downloadedFile))
                        {
                            Log("所有下载方式均失败");
                            DarkMessageBox.Show(dlg, "MediaInfo 自动下载失败，请手动下载 MediaInfo CLI 并将 mediainfo.exe 放到程序根目录。\n下载地址：https://mediaarea.net/zh-CN/MediaInfo/Download/Windows", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dlg.DialogResult = DialogResult.Cancel;
                            dlg.Close();
                            return;
                        }

                        SetStatus("正在解压...");
                        SetProgress(92);
                        Log("开始解压文件...");
                        Directory.CreateDirectory(extractDir);
                        try { await ExtractZipAsync(downloadedFile, extractDir, Log); }
                        catch (Exception zex) { Log("解压失败: " + zex.Message); }

                        SetStatus("正在查找组件...");
                        SetProgress(97);
                        Log("查找 mediainfo.exe...");
                        string mi = FindFileInDir(extractDir, "mediainfo.exe");
                        if (string.IsNullOrEmpty(mi))
                        {
                            Log("未在解压目录找到mediainfo.exe！");
                            try
                            {
                                string allFiles = string.Join(", ", Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories).Select(f => Path.GetFileName(f)).Take(20));
                                Log("找到的exe：" + allFiles);
                            }
                            catch { }
                            DarkMessageBox.Show(dlg, "已下载但未能在压缩包中找到 mediainfo.exe，请手动将 mediainfo.exe 复制到程序目录：\n" + appDir, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dlg.DialogResult = DialogResult.Cancel;
                            dlg.Close();
                            return;
                        }

                        string miDir = Path.GetDirectoryName(mi);
                        File.Copy(mi, Path.Combine(appDir, "mediainfo.exe"), true);
                        Log("已复制 mediainfo.exe 到：" + appDir);

                        int dllCount = 0;
                        try
                        {
                            string[] dllFiles = Directory.GetFiles(miDir, "*.dll", SearchOption.TopDirectoryOnly);
                            foreach (string dllFile in dllFiles)
                            {
                                string dllName = Path.GetFileName(dllFile);
                                File.Copy(dllFile, Path.Combine(appDir, dllName), true);
                                dllCount++;
                                Log("已复制依赖：" + dllName);
                            }
                        }
                        catch (Exception dllEx) { Log("复制DLL时出错: " + dllEx.Message); }
                        Log($"共复制 {dllCount} 个依赖 DLL 文件");

                        FindFFplay();
                        SetProgress(100);
                        SetStatus("✅ 安装完成！");
                        Log("MediaInfo 组件安装成功！");
                        downloadSuccess = true;
                        await Task.Delay(500);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        Log("安装异常：" + ex.Message);
                        try
                        {
                            DarkMessageBox.Show(dlg, "MediaInfo 安装过程出错：\n" + ex.Message + "\n\n请手动从 https://mediaarea.net/zh-CN/MediaInfo/Download/Windows 下载并解压到程序目录。", "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch { }
                        dlg.DialogResult = DialogResult.Cancel;
                        dlg.Close();
                    }
                };

                dlg.ShowDialog(owner);
                return downloadSuccess && MediaInfoReady();
            }
            catch
            {
                return false;
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private async Task<string> DownloadViaPowerShell(string destPath, Action<string> log, Action<string> setStatus, Action<int> setProgress)
        {
            try
            {
                setStatus("通过 PowerShell 下载...");
                log("尝试通过 PowerShell Invoke-WebRequest 下载...");
                setProgress(5);
                string psUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
                string script = $"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '{psUrl}' -OutFile '{destPath}' -UseBasicParsing";
                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "'") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    proc.Start();
                    string err = await proc.StandardError.ReadToEndAsync();
                    await Task.Run(() => proc.WaitForExit());
                    if (!string.IsNullOrEmpty(err)) log("PS日志: " + err.Substring(0, Math.Min(300, err.Length)));
                }
                if (File.Exists(destPath) && new FileInfo(destPath).Length > 1024 * 1024)
                    return destPath;
            }
            catch (Exception ex) { log("PowerShell下载失败: " + ex.Message); }
            return null;
        }

        private async Task<string> DownloadViaPowerShellWithUrl(string destPath, string url, Action<string> log, Action<string> setStatus, Action<int> setProgress)
        {
            try
            {
                setStatus("通过 PowerShell 下载...");
                log("尝试通过 PowerShell Invoke-WebRequest 下载...");
                setProgress(5);
                string script = $"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '{url}' -OutFile '{destPath}' -UseBasicParsing";
                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "'") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    proc.Start();
                    string err = await proc.StandardError.ReadToEndAsync();
                    await Task.Run(() => proc.WaitForExit());
                    if (!string.IsNullOrEmpty(err)) log("PS日志: " + err.Substring(0, Math.Min(300, err.Length)));
                }
                if (File.Exists(destPath) && new FileInfo(destPath).Length > 100 * 1024)
                    return destPath;
            }
            catch (Exception ex) { log("PowerShell下载失败: " + ex.Message); }
            return null;
        }

        private async Task ExtractZipAsync(string zipPath, string destDir, Action<string> log)
        {
            try
            {
                using (var proc = new Process())
                {
                    string script = $"Expand-Archive -Path '{zipPath}' -DestinationPath '{destDir}' -Force";
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    proc.Start();
                    await Task.Run(() => proc.WaitForExit());
                }
                log("解压完成");
            }
            catch (Exception ex)
            {
                log("PowerShell解压失败：" + ex.Message + "，尝试备用方式...");
                try
                {
                    log("尝试使用.NET内置解压...");
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                    string psScript2 = $"Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory('{zipPath}', '{destDir}')";
                    using (var proc2 = new Process())
                    {
                        proc2.StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript2 + "\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        proc2.Start();
                        proc2.WaitForExit();
                    }
                    log(".NET ZipFile解压完成");
                }
                catch (Exception ex2)
                {
                    log(".NET解压也失败：" + ex2.Message);
                    throw;
                }
            }
        }

        private string FindFileInDir(string rootDir, string fileName)
        {
            try
            {
                var files = Directory.GetFiles(rootDir, fileName, SearchOption.AllDirectories);
                return files.FirstOrDefault();
            }
            catch { return null; }
        }

        private string ExtractLocationFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return "";
                string host = "";
                Uri uri;
                try { uri = new Uri(url); host = uri.Host; } catch { return ""; }

                if (IPAddress.TryParse(host, out IPAddress ip))
                {
                    byte[] b = ip.GetAddressBytes();
                    if (b.Length == 4)
                    {
                        if (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 169 && b[1] == 254)
                            return "内网";
                        return "";
                    }
                    return "IPv6";
                }
                string lowerHost = host.ToLower();
                var parts = lowerHost.Split('.');
                if (parts.Length >= 2)
                {
                    string tld = parts[parts.Length - 1];
                    string sld = parts[parts.Length - 2];
                    string domain = parts.Length >= 3 ? parts[parts.Length - 3] : "";
                    string fullDomain = string.Join(".", parts.Skip(Math.Max(0, parts.Length - 2)));

                    var provinceMap = new Dictionary<string, string>
                    {
                        {"beijing","北京"},{"bj","北京"},
                        {"shanghai","上海"},{"sh","上海"},
                        {"guangzhou","广州"},{"gz","广州"},{"gd","广东"},
                        {"shenzhen","深圳"},{"sz","深圳"},
                        {"hangzhou","杭州"},{"hz","杭州"},{"zj","浙江"},
                        {"nanjing","南京"},{"nj","南京"},{"js","江苏"},
                        {"chengdu","成都"},{"cd","成都"},{"sc","四川"},
                        {"wuhan","武汉"},{"wh","武汉"},{"hb","湖北"},
                        {"xian","西安"},{"xa","西安"},{"sn","陕西"},
                        {"chongqing","重庆"},{"cq","重庆"},
                        {"tianjin","天津"},{"tj","天津"},
                        {"shenyang","沈阳"},{"sy","沈阳"},{"ln","辽宁"},
                        {"qingdao","青岛"},{"qd","青岛"},{"sd","山东"},
                        {"zhengzhou","郑州"},{"zz","郑州"},{"hn","河南"},
                        {"changsha","长沙"},{"cs","长沙"},
                        {"hefei","合肥"},{"hf","合肥"},{"ah","安徽"},
                        {"fuzhou","福州"},{"fz","福州"},{"fj","福建"},
                        {"xiamen","厦门"},{"xm","厦门"},
                        {"kunming","昆明"},{"km","昆明"},{"yn","云南"},
                        {"guiyang","贵阳"},{"gy","贵阳"},{"gz2","贵州"},
                        {"nanning","南宁"},{"nn","南宁"},{"gx","广西"},
                        {"haikou","海口"},{"hk","海口"},{"hi","海南"},
                        {"harbin","哈尔滨"},{"heb","哈尔滨"},{"hlj","黑龙江"},
                        {"changchun","长春"},{"cc","长春"},{"jl","吉林"},
                        {"huhehot","呼和浩特"},{"nm","内蒙古"},
                        {"wulumuqi","乌鲁木齐"},{"xj","新疆"},
                        {"nmg","内蒙古"},{"neimenggu","内蒙古"},{"cnmg","内蒙古"},
                        {"lasa","拉萨"},{"xz","西藏"},
                        {"lanzhou","兰州"},{"gs","甘肃"},
                        {"yinchuan","银川"},{"nx","宁夏"},
                        {"xining","西宁"},{"qh","青海"},
                        {"nanchang","南昌"},{"jx","江西"},
                        {"taiyuan","太原"},{"ty","太原"},{"sx","山西"},
                        {"shijiazhuang","石家庄"},{"sjz","石家庄"},{"he","河北"},
                    };
                    var ispMap = new Dictionary<string, string>
                    {
                        {"cmcc","移动"},{"mobile","移动"},{"chinamobile","移动"},{"migu","移动"},
                        {"unicom","联通"},{"chinaunicom","联通"},{"cu","联通"},{"wo","联通"},
                        {"telecom","电信"},{"chinatelecom","电信"},{"ct","电信"},{"tianyi","电信"},{"189","电信"},
                        {"cernet","教育网"},{"edu","教育网"},
                        {"aliyun","阿里云"},{"ali","阿里云"},{"alibaba","阿里云"},
                        {"tencent","腾讯云"},{"tenc","腾讯云"},{"qcloud","腾讯云"},{"wechat","腾讯云"},
                        {"baidu","百度云"},{"bce","百度云"},
                        {"huawei","华为云"},{"hwcloud","华为云"},
                        {"aws","AWS"},{"cloudfront","AWS"},{"amazon","AWS"},
                        {"cloudflare","Cloudflare"},{"cf","Cloudflare"},
                        {"google","Google"},{"gstatic","Google"},
                        {"akamai","Akamai"},{"akamaized","Akamai"},
                        {"cdn","CDN"},{"cache","CDN"},{"ks3","CDN"},{"qiniucdn","CDN"},{"cdnbye","CDN"},
                    };
                    var cctvMap = new Dictionary<string, string>
                    {
                        {"cctv","CCTV"},{"cntv","CCTV"},{"cctvnews","CCTV"},{"cctv5","CCTV"},
                        {"cmg","央视"},{"chinacert","央视"},{"cnr","央广"},{"cri","国际台"},
                        {"wasu","华数"},{"wasu","华数"},
                        {"hunan","湖南"},{"mgtv","芒果TV"},{"hunantv","湖南"},
                        {"zhejiang","浙江卫视"},{"zjstv","浙江"},
                        {"jiangsu","江苏卫视"},{"jstv","江苏"},
                        {"dongfang","东方"},{"dragon","东方"},
                        {"beijingtv","北京卫视"},{"brtn","北京"},
                        {"shmedia","上海台"},{"smg","上海"},
                        {"satv","深圳卫视"},{"sztv","深圳"},
                        {"guangdong","广东卫视"},{"gdtv","广东"},
                        {"scs","四川卫视"},{"sctv","四川"},
                        {"hbtv","湖北卫视"},
                        {"sdtv","山东卫视"},
                        {"hntv","河南卫视"},
                        {"ahtv","安徽卫视"},
                        {"fjrtv","福建卫视"},{"fjtv","东南"},
                    };

                    foreach (var kv in provinceMap)
                    {
                        if (sld == kv.Key || domain == kv.Key || lowerHost.Contains(kv.Key))
                            return kv.Value;
                    }
                    foreach (var kv in cctvMap)
                    {
                        if (lowerHost.Contains(kv.Key))
                            return kv.Value;
                    }
                    foreach (var kv in ispMap)
                    {
                        if (sld == kv.Key || domain == kv.Key || lowerHost.Contains(kv.Key))
                            return kv.Value;
                    }

                    if (tld == "cn" || tld == "com.cn" || tld == "net.cn" || tld == "org.cn" || tld == "gov.cn")
                        return "国内";
                    if (tld == "tv" || tld == "live" || tld == "me" || tld == "cc" || tld == "io" || tld == "top" || tld == "xyz")
                        return "海外";
                    if (tld == "com" || tld == "net" || tld == "org")
                    {
                        return host.Length > 18 ? host.Substring(0, 15) + "..." : host;
                    }
                }
                return host.Length > 18 ? host.Substring(0, 15) + "..." : host;
            }
            catch { return ""; }
        }

        private string GuessIpLocation(byte a, byte b)
        {
            if (a == 36 || a == 39 || a == 42 || a == 43 || (a == 49 && b >= 64 && b <= 95) || a == 58 || a == 59 || a == 60 || a == 61 || a == 101 || a == 103 || a == 106 || a == 110 || a == 111 || a == 112 || a == 113 || a == 114 || a == 115 || a == 116 || a == 117 || a == 118 || a == 119 || a == 120 || a == 121 || a == 122 || a == 123 || a == 124 || a == 125 || a == 126 || (a >= 1 && a <= 22))
                return "国内";
            if (a == 8 || a == 9) return "北美";
            if (a >= 23 && a <= 33) return "北美";
            if (a >= 64 && a <= 77) return "北美";
            if (a >= 96 && a <= 100) return "北美";
            if (a >= 128 && a <= 191)
            {
                if (b >= 0 && b <= 99) return "北美";
                if (b >= 100 && b <= 159) return "欧洲";
                if (b >= 160 && b <= 199) return "北美";
                if (b >= 200 && b <= 255) return "其他";
            }
            return "";
        }

        private async Task<string> QueryIpLocationAsync(string ip, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(ip)) return "";
            lock (ipLocationCache)
            {
                if (ipLocationCache.ContainsKey(ip)) return ipLocationCache[ip];
                if (ipLocationFailed.Contains(ip)) return "";
            }
            if (!IPAddress.TryParse(ip, out IPAddress addr)) return "";
            byte[] b = addr.GetAddressBytes();
            bool isV6 = (b.Length != 4);

            if (!isV6)
            {
                if (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 169 && b[1] == 254)
                {
                    string lan = "内网";
                    lock (ipLocationCache) { ipLocationCache[ip] = lan; }
                    return lan;
                }
            }
            else
            {
                if (IPAddress.IsLoopback(addr) || addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal)
                {
                    string lan = "内网";
                    lock (ipLocationCache) { ipLocationCache[ip] = lan; }
                    return lan;
                }
            }

            string result = "";
            try
            {
                result = await QueryIpApiComAsync(ip, token);
            }
            catch { }
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = await QueryPing0CcAsync(ip, token);
                }
                catch { }
            }
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = await QueryIpWhoIsAsync(ip, token);
                }
                catch { }
            }
            if (string.IsNullOrEmpty(result))
            {
                if (!isV6)
                {
                    result = GuessIpLocation(b[0], b[1]);
                    if (string.IsNullOrEmpty(result)) result = "海外";
                }
                else
                {
                    result = "海外";
                }
            }
            lock (ipLocationCache)
            {
                if (!string.IsNullOrEmpty(result))
                    ipLocationCache[ip] = result;
                else
                    ipLocationFailed.Add(ip);
            }
            return result;
        }

        private string ExtractJsonField(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            if (m.Success) return m.Groups[1].Value;
            return "";
        }

        private async Task<string> QueryIpApiComAsync(string ip, CancellationToken token)
        {
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(3000).Token))
                {
                    string url = $"http://ip-api.com/json/{ip}?lang=zh-CN&fields=status,message,country,regionName,city,isp";
                    using (var resp = await httpClient.GetAsync(url, cts.Token))
                    {
                        if (!resp.IsSuccessStatusCode) return "";
                        string body = await resp.Content.ReadAsStringAsync();
                        if (!body.TrimStart().StartsWith("{")) return "";
                        string status = ExtractJsonField(body, "status");
                        if (status != "success") return "";
                        string country = ExtractJsonField(body, "country");
                        string region = ExtractJsonField(body, "regionName");
                        string city = ExtractJsonField(body, "city");
                        string isp = ExtractJsonField(body, "isp");
                        if (string.IsNullOrEmpty(country)) return "";
                        string loc = "";
                        if (country.Contains("中国"))
                        {
                            loc = region;
                            if (!string.IsNullOrEmpty(city) && city != region) loc += city;
                        }
                        else
                        {
                            loc = country;
                            if (!string.IsNullOrEmpty(city)) loc += city;
                        }
                        if (!string.IsNullOrEmpty(isp))
                        {
                            string shortIsp = ShortenIsp(isp);
                            if (!string.IsNullOrEmpty(shortIsp)) loc += " " + shortIsp;
                        }
                        return loc.Trim();
                    }
                }
            }
            catch { return ""; }
        }

        private async Task<string> QueryIpWhoIsAsync(string ip, CancellationToken token)
        {
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(3000).Token))
                {
                    string url = $"https://ipwho.is/{ip}?lang=zh-CN&fields=success,country,region,city,connection";
                    using (var resp = await httpClient.GetAsync(url, cts.Token))
                    {
                        if (!resp.IsSuccessStatusCode) return "";
                        string body = await resp.Content.ReadAsStringAsync();
                        if (!body.TrimStart().StartsWith("{")) return "";
                        var successM = Regex.Match(body, "\"success\"\\s*:\\s*(true|false)");
                        if (!successM.Success || successM.Groups[1].Value != "true") return "";
                        string country = ExtractJsonField(body, "country");
                        string region = ExtractJsonField(body, "region");
                        string city = ExtractJsonField(body, "city");
                        string isp = "";
                        var connM = Regex.Match(body, "\"connection\"\\s*:\\s*\\{[^}]*\"isp\"\\s*:\\s*\"([^\"]*)\"");
                        if (connM.Success) isp = connM.Groups[1].Value;
                        var orgM = Regex.Match(body, "\"connection\"\\s*:\\s*\\{[^}]*\"org\"\\s*:\\s*\"([^\"]*)\"");
                        if (string.IsNullOrEmpty(isp) && orgM.Success) isp = orgM.Groups[1].Value;
                        if (string.IsNullOrEmpty(country)) return "";
                        string loc = "";
                        if (country.Contains("中国"))
                        {
                            loc = region;
                            if (!string.IsNullOrEmpty(city) && city != region) loc += city;
                        }
                        else
                        {
                            loc = country;
                            if (!string.IsNullOrEmpty(city)) loc += city;
                        }
                        if (!string.IsNullOrEmpty(isp))
                        {
                            string shortIsp = ShortenIsp(isp);
                            if (!string.IsNullOrEmpty(shortIsp)) loc += " " + shortIsp;
                        }
                        return loc.Trim();
                    }
                }
            }
            catch { return ""; }
        }

        private string ShortenIsp(string isp)
        {
            if (string.IsNullOrEmpty(isp)) return "";
            var rules = new Dictionary<string, string>
            {
                {"电信", "电信"},{"联通", "联通"},{"移动", "移动"},
                {"China Telecom", "电信"},{"China Unicom", "联通"},{"China Mobile", "移动"},
                {"CHINANET", "电信"},{"UNICOM", "联通"},{"CMNET", "移动"},
                {"阿里云", "阿里云"},{"腾讯云", "腾讯云"},{"华为云", "华为云"},
                {"Alibaba", "阿里云"},{"Tencent", "腾讯云"},{"Huawei", "华为云"},
                {"Amazon", "AWS"},{"Cloudflare", "CF"},{"Google", "Google"},
                {"教育网", "教育网"},{"CERNET", "教育网"},{"广电", "广电"},
                {"铁通", "铁通"},{"长城", "长城宽带"},{"鹏博士", "鹏博士"},
            };
            foreach (var kv in rules)
            {
                if (isp.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return "";
        }

        private async Task<string> ResolveDomainToIpAsync(string host, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(host)) return "";
            lock (domainIpCache)
            {
                if (domainIpCache.ContainsKey(host)) return domainIpCache[host];
                if (domainIpFailed.Contains(host)) return "";
            }
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(3000).Token))
                {
                    var ips = await Dns.GetHostAddressesAsync(host);
                    foreach (var ip in ips)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            byte[] b = ip.GetAddressBytes();
                            if (!(b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127))
                            {
                                string ipStr = ip.ToString();
                                lock (domainIpCache) { domainIpCache[host] = ipStr; }
                                return ipStr;
                            }
                        }
                        // IPv6功能已移除
                    } // foreach循环结束
                } // using结束
            }
            catch { }
            lock (domainIpCache) { domainIpFailed.Add(host); }
            return "";
        }

        private async Task<string> QueryDomainLocationAsync(string host, CancellationToken token)
        {
            try
            {
                string domainLoc = ExtractLocationFromUrl("http://" + host + "/");
                if (!string.IsNullOrWhiteSpace(domainLoc) && domainLoc != "国内" && domainLoc != "海外")
                    return domainLoc;
                string ip = await ResolveDomainToIpAsync(host, token);
                if (!string.IsNullOrEmpty(ip))
                {
                    string ipLoc = await QueryIpLocationAsync(ip, token);
                    if (!string.IsNullOrEmpty(ipLoc)) return ipLoc;
                }
                return domainLoc;
            }
            catch { return ""; }
        }

        private async Task<string> QueryPing0CcAsync(string ip, CancellationToken token)
        {
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(4000).Token))
                {
                    string url = $"https://ping0.cc/ip/{ip}";
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    req.Headers.Add("Accept", "text/html,application/xhtml+xml");
                    req.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                    using (var resp = await httpClient.SendAsync(req, cts.Token))
                    {
                        if (!resp.IsSuccessStatusCode) return "";
                        string html = await resp.Content.ReadAsStringAsync();
                        int idx = html.IndexOf("IP 位置");
                        if (idx < 0) return "";
                        string snippet = html.Substring(idx, Math.Min(500, html.Length - idx));
                        string clean = Regex.Replace(snippet, "<[^>]+>", "");
                        clean = System.Net.WebUtility.HtmlDecode(clean);
                        clean = clean.Replace("IP 位置", "").Replace("错误提交", "").Trim();
                        int flagEnd = clean.IndexOfAny(new char[] { '中', '美', '日', '韩', '英', '德', '法', '俄', '新', '马', '泰', '越', '印', '菲', '加', '澳', '香', '台', '澳' });
                        if (flagEnd > 0) clean = clean.Substring(flagEnd);
                        else
                        {
                            int m = Regex.Match(clean, @"[\u4e00-\u9fff]").Index;
                            if (m > 0) clean = clean.Substring(m);
                        }
                        clean = Regex.Replace(clean, @"\s+", " ").Trim();
                        int end = clean.IndexOf("ASN");
                        if (end > 0) clean = clean.Substring(0, end).Trim();
                        if (string.IsNullOrWhiteSpace(clean)) return "";
                        if (clean.Length > 30) clean = clean.Substring(0, 30);
                        return SimplifyLocation(clean);
                    }
                }
            }
            catch { return ""; }
        }

        private string SimplifyLocation(string loc)
        {
            if (string.IsNullOrEmpty(loc)) return "";
            loc = loc.Trim();
            if (loc.StartsWith("中国 ")) loc = loc.Substring(3).TrimStart();
            else if (loc.StartsWith("中国")) loc = loc.Substring(2).TrimStart();
            var ispRules = new Dictionary<string, string>
            {
                {"中国移动", "移动"},{"中国联通", "联通"},{"中国电信", "电信"},
                {"CHINA MOBILE", "移动"},{"CHINA UNICOM", "联通"},{"CHINA TELECOM", "电信"},
                {"China Mobile", "移动"},{"China Unicom", "联通"},{"China Telecom", "电信"},
                {"China Mobile Communications", "移动"},
                {"China United Network", "联通"},
                {"China Telecom Group", "电信"},
                {"CT", "电信"},{"CU", "联通"},{"CM", "移动"},
            };
            foreach (var kv in ispRules)
            {
                int idx = loc.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    loc = loc.Substring(0, idx) + " " + kv.Value;
                    break;
                }
            }
            loc = loc.Replace("省", "").Replace("市", "").Replace("自治区", "").Replace("特别行政区", "");
            string[] knownIsp = { "移动","联通","电信","教育网","阿里云","腾讯云","华为云","百度云","Cloudflare","Google","AWS","Akamai","CDN" };
            string isp = "";
            foreach (var k in knownIsp)
            {
                int ki = loc.LastIndexOf(k);
                if (ki >= 0)
                {
                    isp = k;
                    loc = loc.Substring(0, ki).Trim();
                    break;
                }
            }
            loc = Regex.Replace(loc, @"[A-Za-z]+", "");
            loc = Regex.Replace(loc, @"\d+", "");
            loc = loc.Replace(",", "").Trim();
            while (loc.Contains("  ")) loc = loc.Replace("  ", " ");
            loc = loc.Trim();
            if (!string.IsNullOrEmpty(isp) && !loc.Contains(isp)) loc += " " + isp;
            if (string.IsNullOrWhiteSpace(loc)) return "";
            return loc.Trim();
        }

        private async Task<string> TryGetResolutionWithFfprobe(string url, CancellationToken token)
        {
            try
            {
                string fp = ffprobePath;
                if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
                {
                    string dir = Path.GetDirectoryName(ffplayPath);
                    string candidate = Path.Combine(dir, "ffprobe.exe");
                    if (File.Exists(candidate)) fp = candidate;
                }
                if (string.IsNullOrEmpty(fp)) return "";
                using (var cts = new CancellationTokenSource(30000))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fp,
                        Arguments = $"-v error -fflags +fastseek+genpts+nobuffer -avioflags direct -rtbufsize 64000 -analyzeduration 10M -probesize 10M -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"{url}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return "";
                        bool wasCanceled = false;
                        var waitTask = Task.Run(() => proc.WaitForExit(30000));
                        try
                        {
                            await Task.WhenAny(waitTask, Task.Delay(31000, linked.Token));
                        }
                        catch (OperationCanceledException)
                        {
                            wasCanceled = true;
                        }
                        if (wasCanceled || !proc.HasExited)
                        {
                            try { proc.Kill(); } catch { }
                            try { await waitTask; } catch { }
                            return "";
                        }
                        string output = await proc.StandardOutput.ReadToEndAsync();
                        await proc.StandardError.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            var lines = output.Trim().Split('\n', '\r');
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                var wh = line.Trim().Split(',');
                                if (wh.Length >= 2 && int.TryParse(wh[0].Trim(), out int w) && int.TryParse(wh[1].Trim(), out int h) && w > 0 && h > 0)
                                    return $"{w}x{h}";
                            }
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private async Task<string> TryGetResolutionWithFfmpeg(string url, CancellationToken token)
        {
            try
            {
                string fp = ffmpegPath;
                if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
                {
                    string dir = Path.GetDirectoryName(ffplayPath);
                    string candidate = Path.Combine(dir, "ffmpeg.exe");
                    if (File.Exists(candidate)) fp = candidate;
                }
                if (string.IsNullOrEmpty(fp)) return "";
                using (var cts = new CancellationTokenSource(15000))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fp,
                        Arguments = $"-analyzeduration 10M -probesize 10M -i \"{url}\" -hide_banner",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return "";
                        bool wasCanceled = false;
                        var waitTask = Task.Run(() => proc.WaitForExit(15000));
                        try
                        {
                            await Task.WhenAny(waitTask, Task.Delay(16000, linked.Token));
                        }
                        catch (OperationCanceledException)
                        {
                            wasCanceled = true;
                        }
                        if (wasCanceled || !proc.HasExited)
                        {
                            try { proc.Kill(); } catch { }
                            try { await waitTask; } catch { }
                            return "";
                        }
                        string output = await proc.StandardOutput.ReadToEndAsync();
                        string error = await proc.StandardError.ReadToEndAsync();
                        string allText = output + "\n" + error;

                        string[] patterns = new string[]
                        {
                            @"(\d{2,5})x(\d{2,5})",
                            @"(\d{2,5})\s*[x×]\s*(\d{2,5})",
                            @"Stream.*Video.*?(\d{2,5})[x×](\d{2,5})",
                            @"Video:.*?(\d{2,5})x(\d{2,5})",
                            @"width\s*[=:]\s*(\d{2,5}).*?height\s*[=:]\s*(\d{2,5})",
                        };

                        foreach (string pattern in patterns)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(allText, pattern, RegexOptions.IgnoreCase);
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int w) && int.TryParse(match.Groups[2].Value, out int h) && w > 0 && h > 0 && w < 8000 && h < 8000)
                                return $"{w}x{h}";
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private async Task<string> TryGetResolutionWithMediainfo(string url, CancellationToken token)
        {
            Process proc = null;
            try
            {
                string fp = mediainfoPath;
                if (string.IsNullOrEmpty(fp)) return "";
                using (var cts = new CancellationTokenSource(15000))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fp,
                        Arguments = $"--Full \"{url}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    proc = Process.Start(psi);
                    if (proc == null) return "";
                    bool wasCanceled = false;
                    var waitTask = Task.Run(() => proc.WaitForExit(15000));
                    try
                    {
                        await Task.WhenAny(waitTask, Task.Delay(16000, linked.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        wasCanceled = true;
                    }
                    if (wasCanceled || !proc.HasExited)
                    {
                        try { proc.Kill(); } catch { }
                        try { await waitTask; } catch { }
                        return "";
                    }
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    string error = await proc.StandardError.ReadToEndAsync();
                    string allText = output + "\n" + error;
                    if (!string.IsNullOrWhiteSpace(allText))
                    {
                        // 优先从 Video 段解析 Width/Height
                        var wMatch = System.Text.RegularExpressions.Regex.Match(allText, @"Width\s*:\s*(\d{2,5})");
                        var hMatch = System.Text.RegularExpressions.Regex.Match(allText, @"Height\s*:\s*(\d{2,5})");
                        if (wMatch.Success && hMatch.Success)
                        {
                            int w = int.Parse(wMatch.Groups[1].Value);
                            int h = int.Parse(hMatch.Groups[1].Value);
                            if (w > 0 && h > 0 && w < 8000 && h < 8000)
                                return $"{w}x{h}";
                        }
                        // 备用：从完整输出中匹配 NxN 格式
                        var lines = allText.Trim().Split('\n', '\r');
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var match = System.Text.RegularExpressions.Regex.Match(line.Trim(), @"(\d{2,5})\s*[x×]\s*(\d{2,5})");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int w2) && int.TryParse(match.Groups[2].Value, out int h2) && w2 > 0 && h2 > 0 && w2 < 8000 && h2 < 8000)
                                return $"{w2}x{h2}";
                        }
                    }
                }
            }
            catch (InvalidOperationException) { }
            catch (IOException) { }
            catch (Exception) { }
            finally
            {
                if (proc != null)
                {
                    try { if (!proc.HasExited) proc.Kill(); } catch { }
                    proc.Dispose();
                }
            }
            return "";
        }

        private async Task<string> GetResolutionWithFallback(string url, CancellationToken token)
        {
            string resolution = "";

            resolution = await TryGetResolutionWithFfprobe(url, token);
            if (!string.IsNullOrEmpty(resolution)) return resolution;

            resolution = await TryGetResolutionWithFfmpeg(url, token);
            if (!string.IsNullOrEmpty(resolution)) return resolution;

            resolution = await TryGetResolutionWithMediainfo(url, token);
            if (!string.IsNullOrEmpty(resolution)) return resolution;

            return "";
        }

        private void GetFullStreamInfoWithFfprobeSync(string url)
        {
            try
            {
                string fp = ffprobePath;
                if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
                {
                    string dir = Path.GetDirectoryName(ffplayPath);
                    string candidate = Path.Combine(dir, "ffprobe.exe");
                    if (File.Exists(candidate)) fp = candidate;
                }
                if (string.IsNullOrEmpty(fp)) return;

                var psi = new ProcessStartInfo
                {
                    FileName = fp,
                    Arguments = $"-v quiet -print_format json -show_streams -show_format \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc == null) return;
                    if (!proc.WaitForExit(15000))
                    {
                        proc.Kill();
                        return;
                    }
                    string output = proc.StandardOutput.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        ParseFfprobeJson(output);
                    }
                }
            }
            catch { }
        }

        private async Task GetFullStreamInfoWithFfprobe(string url)
        {
            try
            {
                string fp = ffprobePath;
                if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
                {
                    string dir = Path.GetDirectoryName(ffplayPath);
                    string candidate = Path.Combine(dir, "ffprobe.exe");
                    if (File.Exists(candidate)) fp = candidate;
                }
                if (string.IsNullOrEmpty(fp)) return;

                using (var cts = new CancellationTokenSource(20000))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fp,
                        Arguments = $"-v quiet -print_format json -show_streams -show_format \"{url}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return;
                        var waitTask = Task.Run(() => proc.WaitForExit(20000));
                        await Task.WhenAny(waitTask, Task.Delay(21000));
                        if (!proc.HasExited)
                        {
                            try { proc.Kill(); } catch { }
                            try { await waitTask; } catch { }
                            return;
                        }
                        string output = await proc.StandardOutput.ReadToEndAsync();
                        await proc.StandardError.ReadToEndAsync();

                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            ParseFfprobeJson(output);
                        }
                    }
                }
            }
            catch { }
        }

        private void ParseFfprobeJson(string json)
        {
            try
            {
                _currentFormat = GetJsonValue(json, "format_long_name") ?? GetJsonValue(json, "format_name") ?? "";

                string bitrateStr = GetJsonValue(json, "bit_rate");
                if (!string.IsNullOrEmpty(bitrateStr) && double.TryParse(bitrateStr, out double bitRate))
                    _currentBitrate = $"{bitRate / 1000:F1} kb/s";

                string durationStr = GetJsonValue(json, "duration");
                if (!string.IsNullOrEmpty(durationStr) && double.TryParse(durationStr, out double dur))
                {
                    int hours = (int)(dur / 3600);
                    int minutes = (int)((dur % 3600) / 60);
                    double seconds = dur % 60;
                    _currentDuration = $"{hours:00}:{minutes:00}:{seconds:00.00}";
                }

                var videoMatch = System.Text.RegularExpressions.Regex.Match(json, @"{""codec_type"":\s*""video""[^}]*}");
                if (videoMatch.Success)
                {
                    string videoJson = videoMatch.Value;
                    string codecName = GetJsonValue(videoJson, "codec_name") ?? "";
                    string codecLongName = GetJsonValue(videoJson, "codec_long_name") ?? "";
                    string profile = GetJsonValue(videoJson, "profile") ?? "";
                    string width = GetJsonValue(videoJson, "width") ?? "";
                    string height = GetJsonValue(videoJson, "height") ?? "";
                    string sar = GetJsonValue(videoJson, "sample_aspect_ratio") ?? "";
                    string dar = GetJsonValue(videoJson, "display_aspect_ratio") ?? "";
                    string pixFmt = GetJsonValue(videoJson, "pix_fmt") ?? "";
                    string fps = GetJsonValue(videoJson, "r_frame_rate") ?? "";
                    string level = GetJsonValue(videoJson, "level") ?? "";
                    string colorSpace = GetJsonValue(videoJson, "color_space") ?? "";
                    string colorRange = GetJsonValue(videoJson, "color_range") ?? "";
                    string colorPrimaries = GetJsonValue(videoJson, "color_primaries") ?? "";
                    string colorTransfer = GetJsonValue(videoJson, "color_transfer") ?? "";

                    if (!string.IsNullOrEmpty(codecName))
                    {
                        _currentCodec = codecName.ToUpper();
                        if (!string.IsNullOrEmpty(codecLongName))
                            _currentCodec = codecLongName;
                        if (!string.IsNullOrEmpty(profile))
                            _currentCodec += $" ({profile})";
                    }
                    if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height) && int.TryParse(width, out int w) && int.TryParse(height, out int h) && w > 0 && h > 0)
                        _currentResolution = $"{w}x{h}";
                    if (!string.IsNullOrEmpty(sar))
                        _currentSar = sar;
                    if (!string.IsNullOrEmpty(dar))
                        _currentDar = dar;
                    if (!string.IsNullOrEmpty(fps))
                    {
                        var fpsParts = fps.Split('/');
                        if (fpsParts.Length == 2 && int.TryParse(fpsParts[0], out int fpsNum) && int.TryParse(fpsParts[1], out int fpsDen) && fpsNum > 0 && fpsDen > 0)
                            _currentFps = $"{(double)fpsNum / fpsDen:F2} FPS";
                    }

                    _currentPixFmt = pixFmt;
                    _currentLevel = level;
                    _currentColorSpace = colorSpace;
                    _currentColorRange = colorRange;
                    _currentColorPrimaries = colorPrimaries;
                    _currentColorTransfer = colorTransfer;
                }

                var audioMatch = System.Text.RegularExpressions.Regex.Match(json, @"{""codec_type"":\s*""audio""[^}]*}");
                if (audioMatch.Success)
                {
                    string audioJson = audioMatch.Value;
                    string codecName = GetJsonValue(audioJson, "codec_name") ?? "";
                    string codecLongName = GetJsonValue(audioJson, "codec_long_name") ?? "";
                    string sampleRate = GetJsonValue(audioJson, "sample_rate") ?? "";
                    string channels = GetJsonValue(audioJson, "channels") ?? "";
                    string channelLayout = GetJsonValue(audioJson, "channel_layout") ?? "";
                    string sampleFmt = GetJsonValue(audioJson, "sample_fmt") ?? "";
                    string bitsPerSample = GetJsonValue(audioJson, "bits_per_sample") ?? "";

                    if (!string.IsNullOrEmpty(codecName))
                    {
                        if (!string.IsNullOrEmpty(_currentCodec))
                            _currentCodec += $" + {codecName.ToUpper()}";
                        else
                            _currentCodec = codecName.ToUpper();
                        if (!string.IsNullOrEmpty(codecLongName) && !_currentCodec.Contains(codecLongName))
                            _currentCodec = (_currentCodec + $" ({codecLongName})").Replace(" + ", " + ");
                    }
                    if (!string.IsNullOrEmpty(sampleRate) && int.TryParse(sampleRate, out int sr))
                        _currentAudioSampleRate = $"{sr} Hz";
                    if (!string.IsNullOrEmpty(channels) && int.TryParse(channels, out int ch))
                    {
                        if (!string.IsNullOrEmpty(channelLayout))
                            _currentAudioChannels = channelLayout;
                        else
                            _currentAudioChannels = $"{ch}声道";
                    }
                    if (!string.IsNullOrEmpty(sampleFmt))
                        _currentAudioBitdepth = sampleFmt;
                    if (!string.IsNullOrEmpty(bitsPerSample) && int.TryParse(bitsPerSample, out int bps))
                        _currentAudioBitdepth = $"{bps} bits";
                }
            }
            catch { }
        }

        private string GetJsonValue(string json, string key)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, $"\"{key}\"\\s*:\\s*(\"[^\"]*\"|\\d+)");
                if (match.Success)
                {
                    string value = match.Groups[1].Value;
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                        return value.Substring(1, value.Length - 2);
                    return value;
                }
            }
            catch { }
            return null;
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            d = Math.Min(d, Math.Min(rect.Width, rect.Height));
            if (d < 4) d = 4;
            GraphicsPath path = new GraphicsPath();
            int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void StyleRoundButton(Button btn, int radius = 8, Color? borderColor = null, int borderWidth = 0, string colorRole = "primary", Action<Graphics, int, int> customDraw = null)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Empty;
            btn.UseVisualStyleBackColor = false;
            btn.Tag = "sr:" + colorRole;
            
            btn.Region?.Dispose();
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
                btn.Region = new Region(path);
            
            Color bc = borderColor ?? Color.Empty;
            int bw = borderWidth;
            bool isHover = false, isPressed = false, isClicked = false;
            int pressOffset = 2;
            int animDuration = 150;
            btn.MouseEnter += (s, e) => { isHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHover = false; isPressed = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { isPressed = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { isPressed = false; btn.Invalidate(); };
            btn.MouseClick += (s, e) =>
            {
                isClicked = true;
                btn.Invalidate();
                System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer { Interval = animDuration };
                animTimer.Tick += (timerS, timerE) =>
                {
                    animTimer.Stop();
                    animTimer.Dispose();
                    isClicked = false;
                    btn.Invalidate();
                };
                animTimer.Start();
            };
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color parentBg = btn.Parent != null ? btn.Parent.BackColor : Color.White;
                using (SolidBrush clear = new SolidBrush(parentBg))
                    e.Graphics.FillRectangle(clear, 0, 0, btn.Width, btn.Height);
                int offsetY = isPressed || isClicked ? pressOffset : 0;
                int offsetX = isPressed || isClicked ? 1 : 0;
                Rectangle rect = new Rectangle(bw / 2 + offsetX, bw / 2 + offsetY, btn.Width - 1 - bw, btn.Height - 1 - bw);
                Color normalBg = btn.BackColor;
                bool isDark = IsDarkColor(normalBg);
                int hoverDelta = isDark ? 35 : 18;
                int pressedDelta = isDark ? 30 : 25;
                int clickDelta = isDark ? 45 : 40;
                Color hoverBg = Color.FromArgb(Math.Min(255, normalBg.R + hoverDelta), Math.Min(255, normalBg.G + hoverDelta), Math.Min(255, normalBg.B + hoverDelta));
                Color pressedBg = Color.FromArgb(Math.Max(0, normalBg.R - pressedDelta), Math.Max(0, normalBg.G - pressedDelta), Math.Max(0, normalBg.B - pressedDelta));
                Color clickBg = Color.FromArgb(Math.Max(0, normalBg.R - clickDelta), Math.Max(0, normalBg.G - clickDelta), Math.Max(0, normalBg.B - clickDelta));
                Color bg;
                if (btn.Enabled)
                {
                    if (isClicked) bg = clickBg;
                    else if (isPressed) bg = pressedBg;
                    else if (isHover) bg = hoverBg;
                    else bg = normalBg;
                }
                else
                {
                    bg = Color.FromArgb(
                        (int)(normalBg.R * 0.6),
                        (int)(normalBg.G * 0.6),
                        (int)(normalBg.B * 0.6));
                }
                int innerR = Math.Max(1, radius - bw);
                using (GraphicsPath path = GetRoundedPath(rect, innerR))
                using (SolidBrush br = new SolidBrush(bg))
                    e.Graphics.FillPath(br, path);
                if (bw > 0 && bc != Color.Empty)
                {
                    Rectangle borderRect = new Rectangle(bw / 2 + offsetX, bw / 2 + offsetY, btn.Width - 1 - bw, btn.Height - 1 - bw);
                    using (GraphicsPath bp = GetRoundedPath(borderRect, innerR))
                    using (Pen pen = new Pen(bc, bw))
                    {
                        pen.Alignment = PenAlignment.Center;
                        e.Graphics.DrawPath(pen, bp);
                    }
                }
                if (isHover)
                {
                    Rectangle glowRect = new Rectangle(2 + offsetX, 2 + offsetY, btn.Width - 5 - bw, btn.Height - 5 - bw);
                    using (GraphicsPath glowPath = GetRoundedPath(glowRect, innerR - 1))
                    using (Pen glowPen = new Pen(Color.FromArgb(40, Color.White), 1.5f))
                        e.Graphics.DrawPath(glowPen, glowPath);
                }
                if (customDraw != null)
                {
                    customDraw(e.Graphics, offsetX, offsetY);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(offsetX, offsetY, btn.Width, btn.Height), btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            };
            btn.Resize += (s, e) =>
            {
                btn.Region?.Dispose();
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
                    btn.Region = new Region(path);
                btn.Invalidate();
            };
            btn.BackColorChanged += (s, e) => btn.Invalidate();
            btn.ParentChanged += (s, e) =>
            {
                if (btn.Parent != null)
                {
                    btn.Parent.BackColorChanged += (ps, pe) => btn.Invalidate();
                }
            };
            btn.Invalidate();
        }

        public static void StyleOutlineButton(Button btn, int radius = 19, Color? borderColor = null, Color? textColor = null)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Empty;
            btn.UseVisualStyleBackColor = false;

            Color bc = borderColor ?? Color.FromArgb(200, 200, 210);
            Color tc = textColor ?? Color.FromArgb(60, 60, 70);

            btn.Region?.Dispose();
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
                btn.Region = new Region(path);

            bool isHover = false;
            bool isPressed = false;
            float animProgress = 0f;
            int animSpeed = 8;
            System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer { Interval = 16 };

            Color hoverBg = bc;
            Color hoverText = IsDarkColor(bc) ? Color.White : Color.White;

            animTimer.Tick += (s, e) =>
            {
                float target = isHover ? 1f : 0f;
                if (Math.Abs(animProgress - target) < 0.01f)
                {
                    animProgress = target;
                    animTimer.Stop();
                }
                else
                {
                    animProgress += (target - animProgress) * animSpeed / 100f;
                }
                btn.Invalidate();
            };

            btn.MouseEnter += (s, e) =>
            {
                isHover = true;
                animTimer.Start();
            };
            btn.MouseLeave += (s, e) =>
            {
                isHover = false;
                isPressed = false;
                animTimer.Start();
            };
            btn.MouseDown += (s, e) =>
            {
                isPressed = true;
                btn.Invalidate();
            };
            btn.MouseUp += (s, e) =>
            {
                isPressed = false;
                btn.Invalidate();
            };

            btn.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Color parentBg = btn.Parent != null ? btn.Parent.BackColor : Color.White;
                using (SolidBrush clearBrush = new SolidBrush(parentBg))
                    g.FillRectangle(clearBrush, 0, 0, btn.Width, btn.Height);

                int pressOffset = isPressed ? 1 : 0;
                Rectangle rect = new Rectangle(0, pressOffset, btn.Width - 1, btn.Height - 1 - pressOffset);

                Color currentBg = LerpColorStatic(parentBg, hoverBg, animProgress);
                Color currentText = LerpColorStatic(tc, hoverText, animProgress);

                using (GraphicsPath path = GetRoundedPath(rect, radius))
                {
                    if (animProgress > 0.01f)
                    {
                        using (SolidBrush fillBrush = new SolidBrush(currentBg))
                        {
                            g.FillPath(fillBrush, path);
                        }
                    }

                    using (Pen borderPen = new Pen(bc, 1.5f))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                using (SolidBrush textBrush = new SolidBrush(currentText))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(btn.Text, btn.Font, textBrush, new RectangleF(0, pressOffset, btn.Width, btn.Height - pressOffset), sf);
                }
            };

            btn.Resize += (s, e) =>
            {
                btn.Region?.Dispose();
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
                    btn.Region = new Region(path);
                btn.Invalidate();
            };

            btn.BackColorChanged += (s, e) => btn.Invalidate();
            btn.ParentChanged += (s, e) =>
            {
                if (btn.Parent != null)
                {
                    btn.Parent.BackColorChanged += (ps, pe) => btn.Invalidate();
                }
            };
            btn.Invalidate();
        }

        private static Color LerpColorStatic(Color c1, Color c2, float t)
        {
            return Color.FromArgb(
                (int)(c1.A + (c2.A - c1.A) * t),
                (int)(c1.R + (c2.R - c1.R) * t),
                (int)(c1.G + (c2.G - c1.G) * t),
                (int)(c1.B + (c2.B - c1.B) * t)
            );
        }

        private static bool IsDarkColor(Color color)
        {
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance < 0.5;
        }

        private static void StyleRoundTextBox(TextBox txt, int radius = 6, Color? borderColor = null, int borderWidth = 1)
        {
            txt.BorderStyle = BorderStyle.None;
            Color bc = borderColor ?? Color.FromArgb(200, 200, 200);
            int bw = borderWidth;
            Panel host = txt.Parent as Panel;
            if (host == null) return;
            host.Tag = bc;
            host.Paint += (s, e) =>
            {
                if (!txt.Visible || txt.IsDisposed) return;
                Rectangle rect = new Rectangle(txt.Left - bw, txt.Top - bw, txt.Width + bw * 2, txt.Height + bw * 2);
                Color borderCol = (host.Tag is Color) ? (Color)host.Tag : bc;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(rect, radius))
                using (SolidBrush br = new SolidBrush(txt.BackColor))
                    e.Graphics.FillPath(br, path);
                using (GraphicsPath path = GetRoundedPath(rect, radius))
                using (Pen pen = new Pen(borderCol, bw))
                    e.Graphics.DrawPath(pen, path);
            };
            txt.Resize += (s, e) => host.Invalidate();
            txt.LocationChanged += (s, e) => host.Invalidate();
            txt.BackColorChanged += (s, e) => host.Invalidate();
            host.Invalidate();
        }

        private static void MakeRounded(Control ctrl, int radius = 6)
        {
            ctrl.Region?.Dispose();
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius))
                ctrl.Region = new Region(path);
            ctrl.Resize += (s, e) =>
            {
                ctrl.Region?.Dispose();
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius))
                    ctrl.Region = new Region(path);
                ctrl.Invalidate();
            };
        }

        /// <summary>
        /// 显示设置对话框
        /// 窗口大小: 860 x 860
        /// 布局方式: 使用Panel作为容器，每个设置项一行，带鼠标滑过高亮效果
        /// 修改说明: 直接修改下方布局常量即可调整整体布局
        /// </summary>
        private void ShowSettingsDialog()
        {
            bool isDark = theme.Name == "深色";
            Color bgColor = isDark ? Color.FromArgb(28, 32, 42) : Color.White;
            Color textColor = isDark ? Color.FromArgb(220, 225, 235) : Color.FromArgb(35, 40, 50);
            Color subTextColor = isDark ? Color.FromArgb(160, 168, 185) : Color.FromArgb(100, 110, 125);
            Color accentColor = Color.FromArgb(64, 158, 255);
            Color borderColor = isDark ? Color.FromArgb(50, 55, 70) : Color.FromArgb(230, 232, 238);

            Color engineCardBg = isDark ? Color.FromArgb(30, 45, 60) : Color.FromArgb(235, 245, 255);
            Color engineCardBorder = isDark ? Color.FromArgb(45, 70, 100) : Color.FromArgb(180, 210, 240);

            Color perfCardBg = isDark ? Color.FromArgb(55, 45, 35) : Color.FromArgb(255, 245, 235);
            Color perfCardBorder = isDark ? Color.FromArgb(100, 70, 45) : Color.FromArgb(240, 200, 160);

            Color funcCardBg = isDark ? Color.FromArgb(35, 55, 45) : Color.FromArgb(240, 255, 245);
            Color funcCardBorder = isDark ? Color.FromArgb(55, 90, 70) : Color.FromArgb(180, 230, 200);

            Color customCardBg = isDark ? Color.FromArgb(50, 40, 60) : Color.FromArgb(250, 245, 255);
            Color customCardBorder = isDark ? Color.FromArgb(90, 65, 110) : Color.FromArgb(220, 190, 240);

            // ========== 屏幕分辨率自适应 ==========
            // 根据屏幕工作区动态计算窗口大小，确保内容完整显示
            Rectangle screenWorkArea = Screen.GetWorkingArea(this);
            int screenWidth = screenWorkArea.Width;
            int screenHeight = screenWorkArea.Height;

            // 卡片内容所需高度（DPI缩放后）
            int scrollTopPad = SY(20);        // 滚动容器顶部内边距
            int scrollBottomPad = SY(8);      // 滚动容器底部内边距（减少留白）
            int scrollRightPad = SX(20);      // 滚动容器右侧内边距（补偿滚动条宽度，使卡片左右留白对称）
            int cardStartY = SY(15);          // 第一个卡片的起始Y坐标
            int engineCardH = SY(105);        // 检测引擎卡片
            int perfCardH = SY(125);          // 性能设置卡片（2行布局）
            int funcCardH = SY(270);          // 功能开关卡片（增加了搜索功能开关）
            int customCardH = SY(130);        // 个性化卡片（减少底部留白）
            int cardGap = SY(12);             // 卡片间距
            int btnPanelH = SY(65);           // 底部按钮面板（减少高度）
            // 默认功能开关卡片隐藏时的内容高度（3个卡片，2个间距）
            int contentTotalH = cardStartY + engineCardH + perfCardH + customCardH + cardGap * 2 + scrollBottomPad;
            int requiredHeight = scrollTopPad + contentTotalH + btnPanelH;

            // 窗口大小：取所需高度和屏幕95%的较小值，但至少能容纳内容
            int windowWidth = Math.Min(SX(820), (int)(screenWidth * 0.9));
            int windowHeight = Math.Min(Math.Max(requiredHeight, SY(450)), (int)(screenHeight * 0.95));

            Form dlg = new Form
            {
                Text = "设置",
                Size = new Size(windowWidth, windowHeight),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = bgColor
            };
            SetFormDarkModeTitleBar(dlg, isDark);

            int cardHMargin = SX(30);            // 卡片左右边距
            int cardWidth = windowWidth - cardHMargin * 2;
            int cardX = cardHMargin;
            int cardY = cardStartY;

            // ========== 内容滚动容器 ==========
            // 当窗口高度不足时，自动显示滚动条
            Panel scrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = bgColor,
                Padding = new Padding(0, scrollTopPad, scrollRightPad, scrollBottomPad)
            };

            Panel CreateCard(Color bg, Color border)
            {
                return new Panel
                {
                    Size = new Size(cardWidth, 0),
                    Location = new Point(cardX, cardY),
                    BackColor = bg,
                    BorderStyle = BorderStyle.None
                };
            }

            void PaintCardBorder(Panel panel, Color border)
            {
                panel.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(border, 2))
                    {
                        Rectangle rect = new Rectangle(1, 1, panel.Width - 3, panel.Height - 3);
                        using (GraphicsPath path = GetRoundedPath(rect, 8))
                        {
                            pe.Graphics.DrawPath(pen, path);
                        }
                    }
                };
            }

            // ========== 检测引擎卡片 ==========
            Panel engineCard = CreateCard(engineCardBg, engineCardBorder);
            PaintCardBorder(engineCard, engineCardBorder);

            Label engineTitle = new Label
            {
                Text = "⚙️ 检测引擎",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = isDark ? Color.FromArgb(100, 180, 255) : Color.FromArgb(30, 100, 180),
                Size = new Size(cardWidth - SY(30), 28),
                Location = new Point(SY(15), SY(12))
            };
            engineCard.Controls.Add(engineTitle);

            RadioButton rbHttp = new RadioButton
            {
                Text = "HTTP",
                Font = GetFont(10),
                ForeColor = textColor,
                BackColor = engineCardBg,
                Checked = detectEngine == "HTTP",
                Location = new Point(SX(40), SY(48))
            };
            engineCard.Controls.Add(rbHttp);

            RadioButton rbFfmpeg = new RadioButton
            {
                Text = "FFMPEG",
                Font = GetFont(10),
                ForeColor = textColor,
                BackColor = engineCardBg,
                Checked = detectEngine == "FFMPEG",
                Location = new Point(cardWidth - SX(120), SY(48))
            };
            engineCard.Controls.Add(rbFfmpeg);

            Label engineTip = new Label
            {
                Text = "提示：HTTP模式不支持分辨率检测",
                Font = GetFont(8.5f),
                ForeColor = isDark ? Color.FromArgb(200, 100, 100) : Color.FromArgb(200, 80, 80),
                Size = new Size(cardWidth - SY(30), 22),
                Location = new Point(SY(15), SY(78))
            };
            engineCard.Controls.Add(engineTip);

            engineCard.Size = new Size(cardWidth, engineCardH);
            scrollContainer.Controls.Add(engineCard);
            cardY += engineCardH + cardGap;

            // ========== 性能设置卡片 ==========
            Panel perfCard = CreateCard(perfCardBg, perfCardBorder);
            PaintCardBorder(perfCard, perfCardBorder);

            Label perfTitle = new Label
            {
                Text = "🚀 性能设置",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = isDark ? Color.FromArgb(255, 160, 80) : Color.FromArgb(200, 100, 30),
                Size = new Size(cardWidth - SY(30), 28),
                Location = new Point(SY(15), SY(12))
            };
            perfCard.Controls.Add(perfTitle);

            // 性能设置颜色：提示信息使用橙色高亮
            Color perfTipColor = isDark ? Color.FromArgb(255, 180, 100) : Color.FromArgb(200, 100, 30);

            Label concurrencyLabel = new Label
            {
                Text = "并发检测数量",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(120), 24),
                Location = new Point(SY(15), SY(48))
            };
            perfCard.Controls.Add(concurrencyLabel);

            Label concurrencyTip = new Label
            {
                Text = "（范围：1-20，过大可能导致检测不准确）",
                Font = GetFont(8.5f),
                ForeColor = perfTipColor,
                AutoSize = true,
                Location = new Point(SY(140), SY(50))
            };
            perfCard.Controls.Add(concurrencyTip);

            TextBox txtConcurrency = new TextBox
            {
                Text = detectConcurrency.ToString(),
                Font = GetFont(9.5f),
                ForeColor = textColor,
                BackColor = bgColor,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Right,
                Size = new Size(SY(80), SY(28)),
                Location = new Point(cardWidth - SY(110), SY(46))
            };
            perfCard.Controls.Add(txtConcurrency);

            Label timeoutLabel = new Label
            {
                Text = "超时时间（秒）",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(120), 24),
                Location = new Point(SY(15), SY(88))
            };
            perfCard.Controls.Add(timeoutLabel);

            Label timeoutTip = new Label
            {
                Text = "（范围：1-60秒）",
                Font = GetFont(8.5f),
                ForeColor = perfTipColor,
                AutoSize = true,
                Location = new Point(SY(140), SY(90))
            };
            perfCard.Controls.Add(timeoutTip);

            TextBox txtTimeout = new TextBox
            {
                Text = timeoutSeconds.ToString(),
                Font = GetFont(9.5f),
                ForeColor = textColor,
                BackColor = bgColor,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Right,
                Size = new Size(SY(80), SY(28)),
                Location = new Point(cardWidth - SY(110), SY(86))
            };
            perfCard.Controls.Add(txtTimeout);

            perfCard.Size = new Size(cardWidth, perfCardH);
            scrollContainer.Controls.Add(perfCard);
            cardY += perfCardH + cardGap;

            // ========== 功能开关卡片 ==========
            Panel funcCard = CreateCard(funcCardBg, funcCardBorder);
            PaintCardBorder(funcCard, funcCardBorder);

            Label funcTitle = new Label
            {
                Text = "🎯 功能开关",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = isDark ? Color.FromArgb(120, 220, 150) : Color.FromArgb(40, 160, 80),
                Size = new Size(cardWidth - SY(30), 28),
                Location = new Point(SY(15), SY(12))
            };
            funcCard.Controls.Add(funcTitle);

            Label autoClearLabel = new Label
            {
                Text = "自动清除无效源",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(180), 24),
                Location = new Point(SY(15), SY(48))
            };
            funcCard.Controls.Add(autoClearLabel);

            ToggleSwitch toggleAutoClear = new ToggleSwitch { Checked = autoClearInvalid, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(47)) };
            funcCard.Controls.Add(toggleAutoClear);

            Label persistLabel = new Label
            {
                Text = "检测列表持久化",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(180), 24),
                Location = new Point(SY(15), SY(85))
            };
            funcCard.Controls.Add(persistLabel);

            ToggleSwitch togglePersist = new ToggleSwitch { Checked = persistList, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(84)) };
            funcCard.Controls.Add(togglePersist);

            Label watchLabel = new Label
            {
                Text = "关闭搜索提示框",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(180), 24),
                Location = new Point(SY(15), SY(122))
            };
            funcCard.Controls.Add(watchLabel);

            ToggleSwitch toggleWatch = new ToggleSwitch { Checked = watchSearchWindow, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(121)) };
            funcCard.Controls.Add(toggleWatch);

            Label autoParseLabel = new Label
            {
                Text = "自动解析链接",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(180), 24),
                Location = new Point(SY(15), SY(159))
            };
            funcCard.Controls.Add(autoParseLabel);

            ToggleSwitch toggleAutoParse = new ToggleSwitch { Checked = autoParseLink, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(158)) };
            funcCard.Controls.Add(toggleAutoParse);

            Label searchBtnLabel = new Label
            {
                Text = "搜索功能",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(180), 24),
                Location = new Point(SY(15), SY(196))
            };
            funcCard.Controls.Add(searchBtnLabel);

            ToggleSwitch toggleSearchBtn = new ToggleSwitch { Checked = showSearchButton, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(195)) };
            funcCard.Controls.Add(toggleSearchBtn);

            // 下次不再提示免责声明（隐藏在高级功能中）
            Label skipDisclaimerLabel = new Label
            {
                Text = "下次启动不再提示免责声明",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(240), 24),
                Location = new Point(SY(15), SY(233))
            };
            funcCard.Controls.Add(skipDisclaimerLabel);

            ToggleSwitch toggleSkipDisclaimer = new ToggleSwitch { Checked = skipDisclaimerPrompt, Size = new Size(SY(80), SY(30)), Location = new Point(cardWidth - SY(110), SY(232)) };
            funcCard.Controls.Add(toggleSkipDisclaimer);

            funcCard.Size = new Size(cardWidth, funcCardH);
            scrollContainer.Controls.Add(funcCard);
            cardY += funcCardH + cardGap;

            // ========== 个性化设置卡片 ==========
            Panel customCard = CreateCard(customCardBg, customCardBorder);
            PaintCardBorder(customCard, customCardBorder);

            Label customTitle = new Label
            {
                Text = "🎨 个性化",
                Font = GetFont(11, FontStyle.Bold),
                ForeColor = isDark ? Color.FromArgb(200, 150, 255) : Color.FromArgb(120, 60, 180),
                Size = new Size(cardWidth - SY(30), 28),
                Location = new Point(SY(15), SY(12))
            };
            customCard.Controls.Add(customTitle);

            Label fontLabel = new Label
            {
                Text = "字体设置",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(120), 24),
                Location = new Point(SY(15), SY(48))
            };
            customCard.Controls.Add(fontLabel);

            int cmbFontW = SX(220);
            ComboBox cmbFont = new ComboBox
            {
                Font = GetFont(9),
                ForeColor = textColor,
                BackColor = bgColor,
                Size = new Size(cmbFontW, SY(28)),
                Location = new Point(cardWidth - SX(90) - SX(12) - cmbFontW, SY(46)),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (FontFamily fontFamily in FontFamily.Families)
            {
                cmbFont.Items.Add(fontFamily.Name);
            }
            if (cmbFont.Items.Contains(customFontFamily))
            {
                cmbFont.SelectedItem = customFontFamily;
            }
            else if (cmbFont.Items.Count > 0)
            {
                cmbFont.SelectedIndex = 0;
            }
            customCard.Controls.Add(cmbFont);

            Button btnFontApply = new Button
            {
                Text = "应用",
                Font = GetFont(9),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(SY(70), SY(28)),
                Location = new Point(cardWidth - SY(90), SY(46)),
                Cursor = Cursors.Hand
            };
            btnFontApply.FlatAppearance.BorderSize = 0;
            btnFontApply.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, SY(70), SY(28)), 6));
            btnFontApply.Click += (s, e) =>
            {
                if (cmbFont.SelectedItem != null)
                {
                    string newFont = cmbFont.SelectedItem.ToString();
                    if (newFont != customFontFamily)
                    {
                        customFontFamily = newFont;
                        RefreshFontsImmediately();
                        RefreshControlFonts(dlg.Controls);
                        dlg.Invalidate();
                        SaveConfig();
                    }
                }
            };
            customCard.Controls.Add(btnFontApply);

            Label playerLabel = new Label
            {
                Text = "第三方播放器",
                Font = GetFont(9.5f),
                ForeColor = textColor,
                Size = new Size(SY(100), 24),
                Location = new Point(SY(15), SY(85))
            };
            customCard.Controls.Add(playerLabel);

            int btnBrowseW = SX(75);
            int btnRightMargin = SX(20);
            int inputBtnGap = SX(12);
            int playerInputW = cardWidth - SX(130) - btnRightMargin - btnBrowseW - inputBtnGap;
            TextBox txtPlayerPath = new TextBox
            {
                Text = customPlayerPath,
                Font = GetFont(9),
                ForeColor = textColor,
                BackColor = bgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(playerInputW, SY(28)),
                Location = new Point(cardWidth - btnRightMargin - btnBrowseW - inputBtnGap - playerInputW, SY(83))
            };
            customCard.Controls.Add(txtPlayerPath);

            Button btnBrowsePlayer = new Button
            {
                Text = "浏览...",
                Font = GetFont(9),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(btnBrowseW, SY(28)),
                Location = new Point(cardWidth - btnRightMargin - btnBrowseW, SY(83)),
                Cursor = Cursors.Hand
            };
            btnBrowsePlayer.FlatAppearance.BorderSize = 0;
            btnBrowsePlayer.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnBrowseW, SY(28)), 6));
            btnBrowsePlayer.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "可执行文件|*.exe|所有文件|*.*";
                    ofd.Title = "选择第三方播放器";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtPlayerPath.Text = ofd.FileName;
                    }
                }
            };
            customCard.Controls.Add(btnBrowsePlayer);

            customCard.Size = new Size(cardWidth, customCardH);
            scrollContainer.Controls.Add(customCard);
            cardY += customCardH + cardGap;

            // 设置滚动容器的最小滚动区域大小，确保所有内容都能滚动到
            scrollContainer.AutoScrollMinSize = new Size(cardWidth, contentTotalH);

            // ========== 底部按钮面板 ==========
            Panel btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = btnPanelH,
                BackColor = bgColor,
                Padding = new Padding(0, SY(12), 0, SY(12))
            };
            
            // 先添加按钮面板（Dock=Bottom），再添加滚动容器（Dock=Fill）
            // 这样 Dock=Bottom 的按钮面板会先占底部空间，然后 scrollContainer 填充剩余空间
            dlg.Controls.Add(btnPanel);
            dlg.Controls.Add(scrollContainer);

            Button btnReset = new Button
            {
                Text = "恢复默认",
                Font = GetFont(10),
                ForeColor = textColor,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(SY(110), SY(35)),
                Cursor = Cursors.Hand
            };
            StyleOutlineButton(btnReset, 17, borderColor, textColor);
            btnReset.Click += (s, e) =>
            {
                if (DarkMessageBox.Show("确定要恢复所有设置为默认值吗？", "恢复默认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    detectEngine = "HTTP";
                    detectConcurrency = 10;
                    timeoutSeconds = 5;
                    autoClearInvalid = false;
                    persistList = true;
                    autoParseLink = false;
                    customPlayerPath = "";
                    watchSearchWindow = false;
                    customFontFamily = "Microsoft YaHei";
                    themePreference = "跟随系统";
                    theme = AppTheme.GetAutoTheme();

                    rbHttp.Checked = true;
                    rbFfmpeg.Checked = false;
                    txtConcurrency.Text = "10";
                    txtTimeout.Text = "5";
                    toggleAutoClear.Checked = false;
                    togglePersist.Checked = true;
                    toggleAutoParse.Checked = false;
                    toggleWatch.Checked = false;
                    txtPlayerPath.Text = "";
                    if (cmbFont.Items.Contains("Microsoft YaHei"))
                    {
                        cmbFont.SelectedItem = "Microsoft YaHei";
                    }

                    SaveConfig();
                    ApplyTheme();
                    RefreshFontsImmediately();
                    RefreshControlFonts(dlg.Controls);
                    dlg.Invalidate();
                }
            };

            Button btnOK = new Button
            {
                Text = "确定",
                Font = GetFont(10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(SY(110), SY(35)),
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, SY(110), SY(35)), 6));
            btnOK.Click += (s, e) =>
            {
                detectEngine = rbHttp.Checked ? "HTTP" : "FFMPEG";

                int concurrency;
                if (int.TryParse(txtConcurrency.Text, out concurrency))
                {
                    detectConcurrency = Math.Max(1, Math.Min(20, concurrency));
                }

                int timeout;
                if (int.TryParse(txtTimeout.Text, out timeout))
                {
                    timeoutSeconds = Math.Max(1, Math.Min(60, timeout));
                }

                autoClearInvalid = toggleAutoClear.Checked;
                        skipDisclaimerPrompt = toggleSkipDisclaimer.Checked;
                persistList = togglePersist.Checked;
                customPlayerPath = txtPlayerPath.Text;
                watchSearchWindow = toggleWatch.Checked;
                autoParseLink = toggleAutoParse.Checked;
                showSearchButton = toggleSearchBtn.Checked;

                if (btnNavSearch != null)
                {
                    btnNavSearch.Visible = showSearchButton;
                    RefreshNavButtonSizes();
                }

                SaveConfig();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            Button btnCancel = new Button
            {
                Text = "取消",
                Font = GetFont(10),
                ForeColor = textColor,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(SY(110), SY(35)),
                Cursor = Cursors.Hand
            };
            StyleOutlineButton(btnCancel, 17, borderColor, textColor);
            btnCancel.Click += (s, e) => dlg.Close();

            // 动态更新卡片布局（与关于窗口公众号推广布局逻辑一致）
            Action UpdateCardsLayout = () =>
            {
                int curY = cardStartY;

                engineCard.Location = new Point(cardX, curY);
                curY += engineCardH + cardGap;

                perfCard.Location = new Point(cardX, curY);
                curY += perfCardH + cardGap;

                if (funcCard.Visible)
                {
                    funcCard.Location = new Point(cardX, curY);
                    curY += funcCardH + cardGap;
                }

                customCard.Location = new Point(cardX, curY);
                curY += customCardH + cardGap;

                int newContentH = curY - cardStartY + scrollBottomPad;
                scrollContainer.AutoScrollMinSize = new Size(cardWidth, newContentH);

                // 动态调整窗口高度
                int newRequiredHeight = scrollTopPad + newContentH + btnPanelH;
                int newWindowHeight = Math.Min(Math.Max(newRequiredHeight, SY(450)), (int)(screenHeight * 0.95));
                if (dlg.ClientSize.Height != newWindowHeight)
                {
                    dlg.ClientSize = new Size(windowWidth, newWindowHeight);
                }
            };

            // ========== 高级功能按钮（彩蛋）==========
            // 与关于窗口公众号推广彩蛋逻辑一致：覆盖在恢复默认按钮上
            Button btnAdvanced = new Button
            {
                Text = "高级功能",
                Font = GetFont(10),
                ForeColor = accentColor,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(SX(110), SY(35)),
                Visible = false,
                Cursor = Cursors.Hand
            };
            btnAdvanced.FlatAppearance.BorderSize = 0;
            btnAdvanced.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, SX(110), SY(35)), 17));
            btnAdvanced.Click += (s, e) =>
            {
                funcCard.Visible = !funcCard.Visible;
                UpdateCardsLayout();
            };
            btnPanel.Controls.Add(btnAdvanced);

            btnPanel.Controls.Add(btnReset);
            btnPanel.Controls.Add(btnOK);
            btnPanel.Controls.Add(btnCancel);

            // 彩蛋：鼠标悬停恢复默认按钮3秒后显示高级功能按钮，逻辑与关于窗口公众号推广彩蛋一致
            using (System.Windows.Forms.Timer advEggTimer = new System.Windows.Forms.Timer { Interval = 3000 })
            using (System.Windows.Forms.Timer advHideTimer = new System.Windows.Forms.Timer { Interval = 1000 })
            {
                advEggTimer.Tick += (s, e) =>
                {
                    advEggTimer.Stop();
                    btnAdvanced.Visible = true;
                    btnAdvanced.Refresh();
                };

                advHideTimer.Tick += (s, e) =>
                {
                    advHideTimer.Stop();
                    btnAdvanced.Visible = false;
                };

                // 鼠标悬停恢复默认按钮触发彩蛋
                Action<Control> resetWireUpWithEgg = null;
                resetWireUpWithEgg = (ctrl) =>
                {
                    ctrl.MouseEnter += (s, e) => { advEggTimer.Start(); advHideTimer.Stop(); };
                    ctrl.MouseLeave += (s, e) => { advEggTimer.Stop(); if (btnAdvanced.Visible) advHideTimer.Start(); };
                    foreach (Control child in ctrl.Controls)
                        resetWireUpWithEgg(child);
                };
                resetWireUpWithEgg(btnReset);

                // 鼠标进入高级功能按钮时停止隐藏计时器
                Action<Control> advWireUpWithHide = null;
                advWireUpWithHide = (ctrl) =>
                {
                    ctrl.MouseEnter += (s, e) => advHideTimer.Stop();
                    ctrl.MouseLeave += (s, e) => { if (btnAdvanced.Visible) advHideTimer.Start(); };
                    foreach (Control child in ctrl.Controls)
                        advWireUpWithHide(child);
                };
                advWireUpWithHide(btnAdvanced);

                // 初始化功能开关卡片为隐藏状态
                funcCard.Visible = false;
                UpdateCardsLayout();

                btnPanel.Paint += (s, pe) =>
                {
                    int contentRightX = cardX + cardWidth;
                    btnReset.Location = new Point(cardX, SY(15));
                    btnAdvanced.Location = new Point(cardX, SY(15)); // 覆盖在恢复默认按钮上
                    btnOK.Location = new Point(contentRightX - SX(250), SY(15));
                    btnCancel.Location = new Point(contentRightX - SX(115), SY(15));
                };

                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAboutDialog()
        {
            bool isDark = theme != null && theme.Name == "深色";
            
            Color bgColor = isDark ? Color.FromArgb(28, 32, 42) : Color.White;
            Color textColor = isDark ? Color.FromArgb(220, 225, 235) : Color.FromArgb(35, 40, 50);
            Color subTextColor = isDark ? Color.FromArgb(160, 168, 185) : Color.FromArgb(100, 110, 125);
            Color accentColor = Color.FromArgb(64, 158, 255);
            
            Color featureCardBg = isDark ? Color.FromArgb(38, 42, 55) : Color.FromArgb(245, 245, 248);
            Color promoCardBg = isDark ? Color.FromArgb(30, 50, 40) : Color.FromArgb(240, 250, 240);
            Color feedbackCardBg = isDark ? Color.FromArgb(30, 40, 55) : Color.FromArgb(240, 245, 255);
            
            Color featureCardBorder = isDark ? Color.FromArgb(50, 55, 70) : Color.FromArgb(230, 232, 238);
            Color promoCardBorder = isDark ? Color.FromArgb(40, 80, 55) : Color.FromArgb(180, 220, 180);
            Color feedbackCardBorder = isDark ? Color.FromArgb(40, 55, 80) : Color.FromArgb(180, 200, 230);
            
            Color btnColor = isDark ? Color.FromArgb(50, 55, 70) : Color.FromArgb(240, 242, 247);
            Color btnHoverColor = isDark ? Color.FromArgb(60, 68, 85) : Color.FromArgb(225, 228, 235);
            Color btnBorderColor = isDark ? Color.FromArgb(70, 78, 95) : Color.FromArgb(210, 215, 225);
            
            string version = "v1.0-beta";
            
            // ========== 窗口基础尺寸参数 ==========
            int dlgW = SX(860);                // 窗口总宽度（SX() 为水平方向 DPI 缩放函数）
            int dlgH = SY(400);                // 窗口初始高度（SY() 为垂直方向 DPI 缩放函数）
            int cx = SX(10);                   // 内容区域左边距
            int cw = SX(840);                  // 内容区域宽度（窗口宽度 - 两侧边距）
            int cardRadius = SX(8);            // 卡片圆角半径
            int cardGap = SY(10);              // 卡片之间的垂直间距
            
            using (Form dlg = new Form())
            {
                dlg.Text = "关于";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = bgColor;
                dlg.ForeColor = textColor;
                dlg.Font = GetFont(SF(9f));
                dlg.ShowInTaskbar = false;
                dlg.TopMost = false;
                dlg.ClientSize = new Size(dlgW, dlgH);
                
                try
                {
                    System.Reflection.PropertyInfo pi = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    pi?.SetValue(dlg, true, null);
                }
                catch { }
                
                dlg.HandleCreated += (s, e) =>
                {
                    if (isDark)
                    {
                        try { int dm = 1; DarkMessageBox.ApplyDarkTitleBar(dlg.Handle, dm); } catch { }
                    }
                };
                
                // ========== 顶部标题区域参数 ==========
                int y = SY(16);                  // 当前布局Y坐标起始位置（顶部边距）
                int topCardH = SY(88);           // 顶部标题卡片高度
                
                Panel topCard = new Panel
                {
                    Location = new Point(cx, y),
                    Size = new Size(cw, topCardH),
                    BackColor = Color.Transparent
                };
                dlg.Controls.Add(topCard);
                
                // 文字字体大小参数
                Font topTitleFont = GetFont(SF(15f), FontStyle.Bold);  // 主标题字体（SF() 为字体大小 DPI 缩放）
                Font verFont = GetFont(SF(9.5f));                       // 版本号字体
                
                // 图标和文字布局参数
                int iconSize = SX(56);          // 应用图标尺寸（正方形）
                var topTitleSize = TextRenderer.MeasureText("IPTV 直播源检测工具", topTitleFont);
                var versionSize = TextRenderer.MeasureText("版本 " + version, verFont);
                int gap1 = SX(18);              // 图标与文字之间的水平间距
                int totalW = iconSize + gap1 + topTitleSize.Width;
                int startX = (cw - totalW) / 2; // 内容居中起始X坐标
                
                // 垂直居中计算参数
                int contentH = iconSize;                                        // 图标和文字区域的最大高度
                int contentY = (topCardH - contentH) / 2;                       // 内容垂直居中偏移
                int iconY = contentY + (contentH - iconSize) / 2;               // 图标垂直位置
                int titleY = contentY + (contentH - topTitleSize.Height) / 2;   // 标题垂直居中位置
                
                Panel iconPanel = new Panel
                {
                    Location = new Point(startX, iconY),
                    Size = new Size(iconSize, iconSize),
                    BackColor = Color.Transparent
                };
                iconPanel.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var brush = new SolidBrush(accentColor))
                        {
                            using (var path = CreateRoundedRectPath(new Rectangle(0, 0, iconSize - 1, iconSize - 1), SX(12)))
                                g.FillPath(brush, path);
                        }
                        int mx = SX(12), my = SX(14), mw = SX(32), mh = SY(22);
                        using (var wpen = new Pen(Color.White, 2f))
                        {
                            g.DrawRectangle(wpen, mx, my, mw, mh);
                            g.DrawLine(wpen, mx + SX(6), my + mh + SX(7), mx + mw - SX(6), my + mh + SX(7));
                            g.DrawLine(wpen, mx + mw / 2, my + mh, mx + mw / 2, my + mh + SX(7));
                        }
                    }
                };
                topCard.Controls.Add(iconPanel);
                
                int textStartX = startX + iconSize + gap1;
                Label lblTitle = new Label
                {
                    Text = "IPTV 直播源检测工具",
                    Font = topTitleFont,
                    Location = new Point(textStartX, titleY),
                    AutoSize = true,
                    ForeColor = textColor,
                    BackColor = Color.Transparent
                };
                topCard.Controls.Add(lblTitle);
                
                Font verFontSmall = GetFont(SF(6f));
                var versionSizeSmall = TextRenderer.MeasureText("版本 " + version, verFontSmall);
                Label lblVersion = new Label
                {
                    Text = "版本 " + version,
                    Font = verFontSmall,
                    Location = new Point(textStartX + topTitleSize.Width - versionSizeSmall.Width, titleY + topTitleSize.Height + SY(2)),
                    AutoSize = true,
                    ForeColor = accentColor,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleRight
                };
                topCard.Controls.Add(lblVersion);
                

                
                // ========== 功能概述区域参数 ==========
                y += topCardH + cardGap;           // 更新Y坐标到下一个卡片位置
                int featCardH = SY(150);          // 功能概述卡片高度
                
                Panel featCard = new Panel
                {
                    Location = new Point(cx, y),
                    Size = new Size(cw, featCardH),
                    BackColor = featureCardBg
                };
                featCard.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var path = CreateRoundedRectPath(new Rectangle(0, 0, featCard.Width - 1, featCard.Height - 1), cardRadius))
                        {
                            using (var pen = new Pen(featureCardBorder, 1f))
                                g.DrawPath(pen, path);
                        }
                    }
                };
                dlg.Controls.Add(featCard);
                
                Label lblFeatTitle = new Label
                {
                    Text = "功能概述",
                    Font = GetFont(SF(10.5f), FontStyle.Bold), // 功能卡片标题字体大小
                    Location = new Point(SX(16), SY(14)),      // 标题位置（左缩进16，上缩进14）
                    AutoSize = true,
                    ForeColor = textColor,
                    BackColor = Color.Transparent
                };
                featCard.Controls.Add(lblFeatTitle);
                
                string[][] features = new string[][]
                {
                    new[] { "🔍", "批量检测 IPTV 直播源可用性" },
                    new[] { "📺", "自动识别视频分辨率和编码格式" },
                    new[] { "🎬", "支持 ffprobe/ffmpeg/MediaInfo" },
                    new[] { "📋", "内置链接解析、搜索、分组管理" },
                    new[] { "📤", "支持合并导出、源生成器批量生成" },
                    new[] { "🌍", "支持 IP 归属地、响应速度测试" }
                };
                
                // 功能列表网格布局参数
                int colCount = 2;                // 列数（2列布局）
                int itemH = SY(30);             // 每个功能项的高度
                int startYFeat = SY(42);        // 功能列表起始Y坐标（标题下方）
                int colW = (cw - SX(32)) / colCount; // 每列宽度（总宽度-左右边距后均分）
                
                for (int i = 0; i < features.Length; i++)
                {
                    int col = i % colCount;       // 当前列索引
                    int row = i / colCount;       // 当前行索引
                    int itemX = SX(16) + col * colW; // 项X坐标（左边距+列偏移）
                    int itemY = startYFeat + row * itemH; // 项Y坐标（起始Y+行偏移）
                    
                    Panel itemPanel = new Panel
                    {
                        Location = new Point(itemX, itemY),
                        Size = new Size(colW - SX(8), itemH - SY(4)), // 项尺寸（减去内边距）
                        BackColor = Color.Transparent
                    };
                    itemPanel.MouseEnter += (s, e) =>
                    {
                        itemPanel.BackColor = isDark ? Color.FromArgb(48, 54, 70) : Color.FromArgb(235, 238, 245);
                    };
                    itemPanel.MouseLeave += (s, e) =>
                    {
                        itemPanel.BackColor = Color.Transparent;
                    };
                    featCard.Controls.Add(itemPanel);
                    
                    Label lblIcon = new Label
                    {
                        Text = features[i][0],
                        Font = GetFont(SF(10f)),       // 图标字体大小
                        Location = new Point(SX(4), SY(3)),
                        Size = new Size(SX(24), SY(22)), // 图标区域尺寸
                        ForeColor = textColor,
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    itemPanel.Controls.Add(lblIcon);
                    
                    Label lblDesc = new Label
                    {
                        Text = features[i][1],
                        Font = GetFont(SF(9f)),        // 功能描述字体大小
                        Location = new Point(SX(32), SY(2)),
                        Size = new Size(colW - SX(42), SY(22)), // 描述文本区域尺寸
                        ForeColor = subTextColor,
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    itemPanel.Controls.Add(lblDesc);
                }
                
                // ========== 公众号推广卡片参数（彩蛋功能）==========
                y += featCardH + cardGap;           // 更新Y坐标
                int promoCardH = SY(220);          // 推广卡片高度（包含标题栏和图片）
                
                Panel promoCard = new Panel
                {
                    Location = new Point(cx, y),
                    Size = new Size(cw, promoCardH),
                    BackColor = promoCardBg,
                    Cursor = Cursors.Hand,
                    Visible = false                // 默认隐藏，通过彩蛋触发显示
                };
                promoCard.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var path = CreateRoundedRectPath(new Rectangle(0, 0, promoCard.Width - 1, promoCard.Height - 1), cardRadius))
                        {
                            using (var pen = new Pen(promoCardBorder, 1f))
                                g.DrawPath(pen, path);
                        }
                    }
                };
                dlg.Controls.Add(promoCard);
                
                // 推广卡片字体参数
                Font promoLeftFont = GetFont(SF(12f), FontStyle.Bold);   // 左侧标题字体（加粗）
                Font promoMidFont = GetFont(SF(10f));                     // 中间描述字体
                Font promoRightFont = GetFont(SF(9.5f), FontStyle.Italic); // 右侧提示字体（斜体）
                
                // 推广卡片文本内容
                string promoLeftText = "🎯 关注公众号";
                string promoMidText = "微信搜一搜「文娱茶话会」";
                string promoRightText = "点击复制";
                
                // 测量各文本尺寸
                var promoLeftSize = TextRenderer.MeasureText(promoLeftText, promoLeftFont);
                var promoMidSize = TextRenderer.MeasureText(promoMidText, promoMidFont);
                var promoRightSize = TextRenderer.MeasureText(promoRightText, promoRightFont);
                
                // 文字栏布局参数（单行横向排列）
                int textBarHeight = SY(32);        // 文字栏高度
                int textBarPadding = SY(14);       // 文字栏左右内边距
                int textBarY = SY(2);              // 文字栏顶部Y坐标
                
                // 左侧：关注公众号
                Label lblPromoLeft = new Label
                {
                    Text = promoLeftText,
                    Font = promoLeftFont,
                    Size = promoLeftSize,
                    ForeColor = isDark ? Color.FromArgb(120, 220, 140) : Color.FromArgb(40, 140, 70),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                lblPromoLeft.Location = new Point(textBarPadding, textBarY + (textBarHeight - promoLeftSize.Height) / 2);
                promoCard.Controls.Add(lblPromoLeft);
                
                // 中间：微信搜一搜「文娱茶话会」
                Label lblPromoMid = new Label
                {
                    Text = promoMidText,
                    Font = promoMidFont,
                    Size = promoMidSize,
                    ForeColor = textColor,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblPromoMid.Location = new Point((cw - promoMidSize.Width) / 2, textBarY + (textBarHeight - promoMidSize.Height) / 2);
                promoCard.Controls.Add(lblPromoMid);
                
                // 右侧：点击复制
                Label lblPromoRight = new Label
                {
                    Text = promoRightText,
                    Font = promoRightFont,
                    Size = promoRightSize,
                    ForeColor = isDark ? Color.FromArgb(100, 200, 120) : Color.FromArgb(60, 160, 90),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleRight
                };
                lblPromoRight.Location = new Point(cw - textBarPadding - promoRightSize.Width, textBarY + (textBarHeight - promoRightSize.Height) / 2);
                promoCard.Controls.Add(lblPromoRight);
                
                // 推广卡片图片区域参数（图片填满下方区域）
                int imgAreaTopPad = SY(2);           // 图片区域顶部内边距
                int imgAreaBottomPad = SY(8);       // 图片区域底部内边距
                int imgAreaLeftPad = SX(40);         // 图片区域左侧内边距
                int imgAreaRightPad = SX(40);        // 图片区域右侧内边距
                int imgAreaY = textBarY + textBarHeight + imgAreaTopPad; // 图片区域起始Y坐标
                int promoImgTargetW = cw - imgAreaLeftPad - imgAreaRightPad; // 图片目标宽度（卡片宽度减去左右内边距）
                int borderSize = SX(2);              // 图片边框宽度
                
                Bitmap promoImg = LoadWechatPromoImage(promoImgTargetW);
                
                int promoImgW, promoImgH;
                if (promoImg != null)
                {
                    promoImgW = promoImg.Width;
                    promoImgH = promoImg.Height;
                }
                else
                {
                    promoImgW = promoImgTargetW;
                    promoImgH = (int)(promoImgTargetW * 219.0 / 600.0);
                }
                
                int promoImgX = (cw - promoImgW) / 2;
                int promoImgY = imgAreaY;
                
                Color greenBorderColor = isDark ? Color.FromArgb(60, 160, 90) : Color.FromArgb(80, 180, 110);
                
                // 图片容器面板：直接在 Paint 事件中绘制图片，避免 PictureBox 的各种问题
                Panel imgPanel = new Panel
                {
                    Location = new Point(promoImgX - borderSize, promoImgY - borderSize),
                    Size = new Size(promoImgW + borderSize * 2, promoImgH + borderSize * 2),
                    BackColor = Color.White
                };
                imgPanel.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    
                    // 1. 绘制白色背景（圆角）
                    using (var bgPath = CreateRoundedRectPath(new Rectangle(0, 0, imgPanel.Width - 1, imgPanel.Height - 1), SX(8)))
                    {
                        using (var bgBrush = new SolidBrush(Color.White))
                        {
                            g.FillPath(bgBrush, bgPath);
                        }
                    }
                    
                    // 2. 绘制图片（居中）
                    if (promoImg != null)
                    {
                        int imgDrawX = borderSize;
                        int imgDrawY = borderSize;
                        int imgDrawW = promoImgW;
                        int imgDrawH = promoImgH;
                        g.DrawImage(promoImg, imgDrawX, imgDrawY, imgDrawW, imgDrawH);
                    }
                    
                    // 3. 绘制绿色圆角边框
                    using (var borderPath = CreateRoundedRectPath(new Rectangle(1, 1, imgPanel.Width - 3, imgPanel.Height - 3), SX(8)))
                    {
                        using (var borderPen = new Pen(greenBorderColor, borderSize))
                        {
                            g.DrawPath(borderPen, borderPath);
                        }
                    }
                };
                promoCard.Controls.Add(imgPanel);
                imgPanel.BringToFront();
                
                Color promoNormalBg = promoCardBg;
                Color promoHoverBg = isDark ? Color.FromArgb(35, 60, 48) : Color.FromArgb(225, 242, 228);
                Color promoPressBg = isDark ? Color.FromArgb(42, 70, 55) : Color.FromArgb(210, 235, 218);
                bool promoIsHover = false;
                
                Action<Control> promoWireUp = null;
                promoWireUp = (ctrl) =>
                {
                    ctrl.MouseEnter += (s, e) =>
                    {
                        promoIsHover = true;
                        promoCard.BackColor = promoHoverBg;
                        promoCard.Cursor = Cursors.Hand;
                    };
                    ctrl.MouseLeave += (s, e) =>
                    {
                        promoIsHover = false;
                        promoCard.BackColor = promoNormalBg;
                        promoCard.Cursor = Cursors.Default;
                    };
                    ctrl.MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            promoCard.BackColor = promoPressBg;
                    };
                    ctrl.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            promoCard.BackColor = promoIsHover ? promoHoverBg : promoNormalBg;
                    };
                    ctrl.Click += async (s, e) =>
                    {
                        try
                        {
                            Clipboard.SetText("文娱茶话会");
                            promoCard.BackColor = promoPressBg;
                            await Task.Delay(100);
                            promoCard.BackColor = promoIsHover ? promoHoverBg : promoNormalBg;
                        }
                        catch { }
                    };
                    foreach (Control child in ctrl.Controls)
                        promoWireUp(child);
                };
                promoWireUp(promoCard);
                
                // ========== 反馈卡片参数 ==========
                y += promoCardH + cardGap;           // 更新Y坐标
                int fbCardH = SY(110);             // 反馈卡片高度（包含邮箱和TG两个子卡片）
                
                Panel fbCard = new Panel
                {
                    Location = new Point(cx, y),
                    Size = new Size(cw, fbCardH),
                    BackColor = feedbackCardBg
                };
                fbCard.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var path = CreateRoundedRectPath(new Rectangle(0, 0, fbCard.Width - 1, fbCard.Height - 1), cardRadius))
                        {
                            using (var pen = new Pen(feedbackCardBorder, 1f))
                                g.DrawPath(pen, path);
                        }
                    }
                };
                dlg.Controls.Add(fbCard);
                
                Label lblBugTitle = new Label
                {
                    Text = "问题反馈 & 交流",
                    Font = GetFont(SF(10.5f), FontStyle.Bold), // 反馈卡片标题字体
                    Location = new Point(SX(16), SY(14)),      // 标题位置
                    AutoSize = true,
                    ForeColor = textColor,
                    BackColor = Color.Transparent
                };
                fbCard.Controls.Add(lblBugTitle);
                
                // 子卡片（邮箱/TG）布局参数
                int infoCardW = (cw - SX(32) - SX(12)) / 2; // 每个子卡片宽度（总宽-边距-间隔后均分）
                int infoCardH = SY(54);                     // 每个子卡片高度
                int infoCardY = SY(44);                     // 子卡片起始Y坐标（标题下方）
                
                Label lblEmail = null;
                Panel emailCard = new Panel
                {
                    Location = new Point(SX(16), infoCardY),
                    Size = new Size(infoCardW, infoCardH),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                emailCard.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var path = CreateRoundedRectPath(new Rectangle(0, 0, emailCard.Width - 1, emailCard.Height - 1), SX(6)))
                        {
                            using (var pen = new Pen(feedbackCardBorder, 1f))
                                g.DrawPath(pen, path);
                        }
                    }
                };
                
                Color emailNormalBg = Color.Transparent;
                Color emailHoverBg = isDark ? Color.FromArgb(40, 52, 75) : Color.FromArgb(225, 233, 248);
                Color emailPressBg = isDark ? Color.FromArgb(50, 65, 90) : Color.FromArgb(210, 222, 245);
                bool emailIsHover = false;
                
                Action<Control> emailWireUp = null;
                emailWireUp = (ctrl) =>
                {
                    ctrl.MouseEnter += (s, e) =>
                    {
                        emailIsHover = true;
                        emailCard.BackColor = emailHoverBg;
                        emailCard.Cursor = Cursors.Hand;
                        if (lblEmail != null) lblEmail.Font = GetFont(SF(8.5f), FontStyle.Underline);
                    };
                    ctrl.MouseLeave += (s, e) =>
                    {
                        emailIsHover = false;
                        emailCard.BackColor = emailNormalBg;
                        emailCard.Cursor = Cursors.Default;
                        if (lblEmail != null) lblEmail.Font = GetFont(SF(8.5f));
                    };
                    ctrl.MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            emailCard.BackColor = emailPressBg;
                    };
                    ctrl.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            emailCard.BackColor = emailIsHover ? emailHoverBg : emailNormalBg;
                    };
                    ctrl.Click += async (s, e) =>
                    {
                        try
                        {
                            Clipboard.SetText("xiaomiren0510@gmail.com");
                            emailCard.BackColor = emailPressBg;
                            await Task.Delay(80);
                            emailCard.BackColor = emailIsHover ? emailHoverBg : emailNormalBg;
                            System.Diagnostics.Process.Start("mailto:xiaomiren0510@gmail.com?subject=IPTV直播源检测工具 - BUG反馈");
                        }
                        catch { }
                    };
                    foreach (Control child in ctrl.Controls)
                        emailWireUp(child);
                };
                fbCard.Controls.Add(emailCard);
                
                Label lblEmailIcon = new Label
                {
                    Text = "📧",
                    Font = GetFont(SF(14f)),           // 邮箱图标字体大小
                    Location = new Point(SX(12), SY(13)),
                    Size = new Size(SX(28), SY(28)),   // 图标区域尺寸
                    ForeColor = textColor,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                emailCard.Controls.Add(lblEmailIcon);
                
                Label lblEmailTitle = new Label
                {
                    Text = "邮箱反馈",
                    Font = GetFont(SF(8.5f), FontStyle.Bold), // 邮箱标题字体
                    Location = new Point(SX(46), SY(8)),      // 标题位置（图标右侧）
                    AutoSize = true,
                    ForeColor = subTextColor,
                    BackColor = Color.Transparent
                };
                emailCard.Controls.Add(lblEmailTitle);
                
                lblEmail = new Label
                {
                    Text = "xiaomiren0510@gmail.com",
                    Font = GetFont(SF(8.5f)),          // 邮箱地址字体大小
                    Location = new Point(SX(46), SY(26)), // 邮箱地址位置（标题下方）
                    AutoSize = true,
                    ForeColor = accentColor,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                emailCard.Controls.Add(lblEmail);
                emailWireUp(emailCard);
                
                // TG/GitHub 卡片（彩蛋：鼠标悬停3秒显示TG频道）
                Label lblTgChannel = null;
                Panel tgCard = new Panel
                {
                    Location = new Point(SX(16) + infoCardW + SX(12), infoCardY), // 右侧子卡片位置
                    Size = new Size(infoCardW, infoCardH),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                tgCard.Paint += (s, e) =>
                {
                    using (var g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var path = CreateRoundedRectPath(new Rectangle(0, 0, tgCard.Width - 1, tgCard.Height - 1), SX(6)))
                        {
                            using (var pen = new Pen(feedbackCardBorder, 1f))
                                g.DrawPath(pen, path);
                        }
                    }
                };
                
                Color tgNormalBg = Color.Transparent;
                Color tgHoverBg = isDark ? Color.FromArgb(40, 52, 75) : Color.FromArgb(225, 233, 248);
                Color tgPressBg = isDark ? Color.FromArgb(50, 65, 90) : Color.FromArgb(210, 222, 245);
                bool tgIsHover = false;
                bool isTgRevealed = false;
                
                Action<Control> tgWireUp = null;
                tgWireUp = (ctrl) =>
                {
                    ctrl.MouseEnter += (s, e) =>
                    {
                        tgIsHover = true;
                        tgCard.BackColor = tgHoverBg;
                        tgCard.Cursor = Cursors.Hand;
                        if (lblTgChannel != null) lblTgChannel.Font = GetFont(SF(8.5f), FontStyle.Underline);
                    };
                    ctrl.MouseLeave += (s, e) =>
                    {
                        tgIsHover = false;
                        tgCard.BackColor = tgNormalBg;
                        tgCard.Cursor = Cursors.Default;
                        if (lblTgChannel != null) lblTgChannel.Font = GetFont(SF(8.5f));
                    };
                    ctrl.MouseDown += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            tgCard.BackColor = tgPressBg;
                    };
                    ctrl.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                            tgCard.BackColor = tgIsHover ? tgHoverBg : tgNormalBg;
                    };
                    ctrl.Click += async (s, e) =>
                    {
                        try
                        {
                            tgCard.BackColor = tgPressBg;
                            await Task.Delay(80);
                            tgCard.BackColor = tgIsHover ? tgHoverBg : tgNormalBg;
                            if (isTgRevealed)
                                System.Diagnostics.Process.Start("https://t.me/+jTncKg0Vbrg5YjI1");
                            else
                                System.Diagnostics.Process.Start("https://github.com/281761526/IPTVLiveChecker");
                        }
                        catch { }
                    };
                    foreach (Control child in ctrl.Controls)
                        tgWireUp(child);
                };
                fbCard.Controls.Add(tgCard);
                
                Label lblTgIcon = new Label
                {
                    Text = "💻",                         // 默认显示 GitHub 图标（彩蛋切换为📢）
                    Font = GetFont(SF(14f)),           // TG/GitHub 图标字体大小
                    Location = new Point(SX(12), SY(13)),
                    Size = new Size(SX(28), SY(28)),   // 图标区域尺寸
                    ForeColor = textColor,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tgCard.Controls.Add(lblTgIcon);
                
                Label lblTgTitle = new Label
                {
                    Text = "GitHub",                     // 默认显示 GitHub（彩蛋切换为"TG 频道"）
                    Font = GetFont(SF(8.5f), FontStyle.Bold), // 标题字体
                    Location = new Point(SX(46), SY(8)),      // 标题位置
                    AutoSize = true,
                    ForeColor = subTextColor,
                    BackColor = Color.Transparent
                };
                tgCard.Controls.Add(lblTgTitle);
                
                lblTgChannel = new Label
                {
                    Text = "github.com/281761526/IPTVLiveChecker", // 默认显示 GitHub 地址
                    Font = GetFont(SF(8.5f)),                       // 链接字体大小
                    Location = new Point(SX(46), SY(26)),           // 链接位置
                    AutoSize = true,
                    ForeColor = accentColor,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                tgCard.Controls.Add(lblTgChannel);
                tgWireUp(tgCard);
                
                
                // 免责声明入口
                Label lblDisclaimerLink = new Label
                {
                    Text = "免责声明",
                    Font = GetFont(SF(8.5f), FontStyle.Underline),
                    Location = new Point(SX(16), SY(14)),
                    AutoSize = true,
                    ForeColor = accentColor,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                lblDisclaimerLink.MouseEnter += (s, e) => { lblDisclaimerLink.Font = GetFont(SF(8.5f), FontStyle.Underline | FontStyle.Bold); };
                lblDisclaimerLink.MouseLeave += (s, e) => { lblDisclaimerLink.Font = GetFont(SF(8.5f), FontStyle.Underline); };
                lblDisclaimerLink.Click += (s, e) => { dlg.Close(); ShowDisclaimerDialog(); };
                fbCard.Controls.Add(lblDisclaimerLink);
                
                Font authorFont = GetFont(SF(8f));
                var authorSize = TextRenderer.MeasureText("— Designed by 半步沧桑 —", authorFont);
                int authorH = authorSize.Height + SY(4);
                Label lblAuthor = new Label
                {
                    Text = "— Designed by 半步沧桑 —",
                    Font = authorFont,
                    AutoSize = true,
                    ForeColor = isDark ? Color.FromArgb(110, 120, 135) : Color.FromArgb(170, 180, 195),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                dlg.Controls.Add(lblAuthor);
                
                void UpdateLayout()
                {
                    dlg.SuspendLayout();
                    int promoOffset = promoCard.Visible ? promoCardH + cardGap : 0;
                    promoCard.Location = new Point(cx, SY(16) + topCardH + cardGap + featCardH + cardGap);
                    
                    fbCard.Location = new Point(cx, SY(16) + topCardH + cardGap + featCardH + cardGap + promoOffset);
                    
                    int authorY = SY(16) + topCardH + cardGap + featCardH + cardGap + promoOffset + fbCardH + cardGap;
                    lblAuthor.Location = new Point((dlgW - authorSize.Width) / 2, authorY);
                    
                    int totalH = authorY + authorH + SY(14);
                    dlg.ClientSize = new Size(dlgW, totalH);
                    dlg.ResumeLayout();
                    dlg.Invalidate();
                    dlg.Update();
                }
                
                UpdateLayout();
                
                using (System.Windows.Forms.Timer promoEggTimer = new System.Windows.Forms.Timer { Interval = 3000 })
                using (System.Windows.Forms.Timer promoHideTimer = new System.Windows.Forms.Timer { Interval = 1000 })
                using (System.Windows.Forms.Timer tgEggTimer = new System.Windows.Forms.Timer { Interval = 3000 })
                {
                    promoEggTimer.Tick += (s, e) =>
                    {
                        promoEggTimer.Stop();
                        promoCard.Visible = true;
                        promoCard.Refresh();
                        UpdateLayout();
                    };
                    
                    promoHideTimer.Tick += (s, e) =>
                    {
                        promoHideTimer.Stop();
                        promoCard.Visible = false;
                        UpdateLayout();
                    };
                    
                    tgEggTimer.Tick += (s, e) =>
                    {
                        tgEggTimer.Stop();
                        isTgRevealed = true;
                        lblTgIcon.Text = "📢";
                        lblTgTitle.Text = "TG 频道";
                        lblTgChannel.Text = "t.me/+jTncKg0Vbrg5YjI1";
                    };
                    
                    Action<Control> emailWireUpWithEgg = null;
                    emailWireUpWithEgg = (ctrl) =>
                    {
                        ctrl.MouseEnter += (s, e) => { promoEggTimer.Start(); promoHideTimer.Stop(); };
                        ctrl.MouseLeave += (s, e) => { promoEggTimer.Stop(); if (promoCard.Visible) promoHideTimer.Start(); };
                        foreach (Control child in ctrl.Controls)
                            emailWireUpWithEgg(child);
                    };
                    emailWireUpWithEgg(emailCard);
                    
                    Action<Control> promoWireUpWithHide = null;
                    promoWireUpWithHide = (ctrl) =>
                    {
                        ctrl.MouseEnter += (s, e) => promoHideTimer.Stop();
                        ctrl.MouseLeave += (s, e) => { if (promoCard.Visible) promoHideTimer.Start(); };
                        foreach (Control child in ctrl.Controls)
                            promoWireUpWithHide(child);
                    };
                    promoWireUpWithHide(promoCard);
                    
                    Action<Control> tgWireUpWithEgg = null;
                    tgWireUpWithEgg = (ctrl) =>
                    {
                        ctrl.MouseEnter += (s, e) => { if (!isTgRevealed) tgEggTimer.Start(); };
                        ctrl.MouseLeave += (s, e) => tgEggTimer.Stop();
                        foreach (Control child in ctrl.Controls)
                            tgWireUpWithEgg(child);
                    };
                    tgWireUpWithEgg(tgCard);
                    
                    dlg.ShowDialog(this);
                }
            }
        }

        /// <summary>
        /// 调用独立Updater升级器更新
        /// </summary>
        private void StartUpdater(string downloadUrl, string md5 = "")
        {
            string mainExe = Application.ExecutablePath;
            string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");

            if (!File.Exists(updaterPath))
            {
                DarkMessageBox.Show(this, "找不到 Updater.exe 文件，无法启动自动更新。\n请确保 Updater.exe 与主程序在同一目录。", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string args = $"\"{mainExe}\" \"{downloadUrl}\" \"{md5}\"";
                var proc = new System.Diagnostics.ProcessStartInfo(updaterPath, args);
                proc.UseShellExecute = false;
                proc.StandardOutputEncoding = System.Text.Encoding.UTF8;
                proc.StandardErrorEncoding = System.Text.Encoding.UTF8;
                System.Diagnostics.Process.Start(proc);
                Application.Exit();
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, "启动更新程序失败：" + ex.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 检查软件更新
        /// </summary>
        private async void CheckForUpdate()
        {
            string updateUrl = "https://raw.githubusercontent.com/281761526/IPTVLiveChecker/master/update.json";
            string currentVersion = AppVersion.Version;

            try
            {
                using (var http = new System.Net.Http.HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(10);
                    http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                    string json = await http.GetStringAsync(updateUrl).ConfigureAwait(true);

                    var serializer = new JavaScriptSerializer();
                    var jsonObj = serializer.Deserialize<Dictionary<string, object>>(json);

                    if (jsonObj == null)
                    {
                        DarkMessageBox.Show(this, "更新信息格式错误", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    int localVersionCode = AppVersion.VersionCode;
                    string latestVersion = jsonObj.ContainsKey("latestVersion") ? jsonObj["latestVersion"]?.ToString() ?? "" : "";
                    string downloadUrl = jsonObj.ContainsKey("downloadUrl") ? jsonObj["downloadUrl"]?.ToString() ?? "" : "";
                    string md5Checksum = jsonObj.ContainsKey("md5Checksum") ? jsonObj["md5Checksum"]?.ToString() ?? "" : "";
                    int remoteVersionCode = 0;
                    if (jsonObj.ContainsKey("versionCode"))
                        int.TryParse(jsonObj["versionCode"]?.ToString(), out remoteVersionCode);

                    bool isForceUpdate = false;
                    if (jsonObj.ContainsKey("isForceUpdate"))
                        bool.TryParse(jsonObj["isForceUpdate"]?.ToString(), out isForceUpdate);

                    string changelog = "";
                    if (jsonObj.ContainsKey("changelog") && jsonObj["changelog"] is System.Collections.ArrayList logList)
                    {
                        foreach (var item in logList)
                            changelog += "\u2022 " + item?.ToString() + "\n";
                    }

                    bool hasUpdate = remoteVersionCode > localVersionCode;
                    if (hasUpdate)
                    {
                        string updateNotice = jsonObj.ContainsKey("updateNotice") ? jsonObj["updateNotice"]?.ToString() ?? "" : "";
                        string msg = $"发现新版本：{latestVersion}\n\n" +
                                     $"当前版本：{currentVersion}\n\n" +
                                     $"更新日志：\n{changelog}\n" +
                                     (string.IsNullOrEmpty(updateNotice) ? "" : $"说明：{updateNotice}\n\n") +
                                     (isForceUpdate ? "请立即更新后继续使用。" : "是否立即更新？");

                        if (isForceUpdate)
                        {
                            DarkMessageBox.Show(this, msg, "更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            StartUpdater(downloadUrl, md5Checksum);
                        }
                        else
                        {
                            var result = DarkMessageBox.Show(this, msg, "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (result == DialogResult.Yes)
                                StartUpdater(downloadUrl, md5Checksum);
                        }
                    }
                    else
                    {
                        DarkMessageBox.Show(this, $"当前已是最新版本。\n\n版本：{currentVersion}", "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show(this, "检查更新失败：" + ex.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        
        /// <summary>
        /// 启动前显示免责声明对话框（在主窗口显示之前调用）
        /// </summary>
        /// <returns>用户是否同意条款</returns>
        public bool ShowDisclaimerBeforeStart()
        {
            if (!this.IsHandleCreated)
                this.CreateHandle();

            using (Graphics g = this.CreateGraphics())
                dpiScale = g.DpiX / 96f;

            config.Initialize(dpiScale);
            DarkMessageBox.DpiScale = dpiScale;

            LoadConfig();

            if (disclaimerAgreed && skipDisclaimerPrompt)
                return true;

            return ShowDisclaimerDialog();
        }

        /// <summary>
        /// 显示首次启动免责声明弹窗（强制同意方可进入）
        /// </summary>
        /// <returns>用户是否同意条款</returns>
        private bool ShowDisclaimerDialog()
        {
            // ========== 局部返回值：仅在用户点击"进入软件"按钮时置为 true ==========
            bool dialogResult = false;

            // ==================== 主题颜色配置（深色/浅色自动适配） ====================
            bool isDark = theme != null && theme.Name == "深色";

            // 窗口背景色：深色为深灰蓝，浅色为近白
            Color bgColor = isDark ? Color.FromArgb(24, 27, 36) : Color.FromArgb(250, 251, 253);
            // 主文字颜色：深色为浅灰白，浅色为深灰
            Color textColor = isDark ? Color.FromArgb(220, 226, 238) : Color.FromArgb(28, 32, 44);
            // 副标题文字颜色：比主文字略暗，用于提示性文字
            Color subTextColor = isDark ? Color.FromArgb(148, 156, 172) : Color.FromArgb(120, 128, 145);
            // 强调色：用于按钮高亮、链接等，深色为亮蓝，浅色为标准蓝
            Color accentColor = isDark ? Color.FromArgb(82, 160, 250) : Color.FromArgb(46, 135, 245);
            // 按钮启用时的背景色（与强调色相同）
            Color btnEnabledBg = accentColor;
            // 按钮禁用时的背景色：深色为暗灰，浅色为浅灰
            Color btnDisabledBg = isDark ? Color.FromArgb(50, 54, 66) : Color.FromArgb(225, 228, 234);
            // 按钮启用时的文字颜色：白色
            Color btnEnabledText = Color.White;
            // 按钮禁用时的文字颜色：深色为中灰，浅色为浅灰
            Color btnDisabledText = isDark ? Color.FromArgb(115, 118, 132) : Color.FromArgb(170, 175, 185);
            // 分割线颜色：深色为深灰，浅色为浅灰
            Color dividerColor = isDark ? Color.FromArgb(48, 52, 64) : Color.FromArgb(230, 233, 238);
            // 内容区域背景色（免责声明文本框所在面板）
            Color contentBg = isDark ? Color.FromArgb(32, 36, 46) : Color.FromArgb(255, 255, 255);
            // 提示文字颜色：用于倒计时提示等
            Color hintColor = isDark ? Color.FromArgb(120, 128, 145) : Color.FromArgb(140, 148, 165);
            // 成功提示颜色：倒计时结束后提示文字变绿
            Color successColor = isDark ? Color.FromArgb(56, 196, 106) : Color.FromArgb(36, 176, 86);
            // 警告提示颜色：倒计时进行中提示文字变橙
            Color warningColor = isDark ? Color.FromArgb(252, 176, 54) : Color.FromArgb(235, 145, 18);

            // ==================== 窗口布局尺寸参数（SX/SY为DPI自适应缩放） ====================
            int padX = SX(36);       // 左右内边距：控件与窗口边缘的水平间距
            int contentW = SX(688);   // 内容区域宽度：标题、分割线、内容面板、按钮的统一宽度

            using (Form dlg = new Form())
            {
                // ==================== 窗口基本属性 ====================
                dlg.Text = "免责声明";                              // 窗口标题栏文字
                dlg.StartPosition = FormStartPosition.CenterScreen; // 窗口居中显示
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;  // 固定对话框边框（不可调整大小）
                dlg.MaximizeBox = false;                             // 禁用最大化按钮
                dlg.MinimizeBox = false;                             // 禁用最小化按钮
                dlg.ControlBox = true;                               // 显示右上角关闭按钮
                dlg.ShowInTaskbar = true;                            // 在任务栏显示窗口图标
                dlg.TopMost = false;                                 // 非置顶（允许切换到其他窗口）
                dlg.BackColor = bgColor;                             // 窗口背景色
                dlg.ForeColor = textColor;                           // 窗口默认文字颜色
                dlg.Font = GetFont(SF(9f));                          // 窗口默认字体（9pt，DPI自适应）
                dlg.ClientSize = new Size(SX(760), SY(640));        // 窗口客户区大小：宽760×高640（DPI缩放后，内容面板高度减少，窗口高度相应减小）

                // ==================== 布局游标 y：从上往下依次排列各控件 ====================
                int y = SY(36);  // 起始Y坐标：距窗口顶部36px（DPI缩放后）

                // ==================== 标题标签"免责声明" ====================
                Label lblTitle = new Label
                {
                    Text = "免责声明",                                    // 标题文字
                    Font = GetFont(SF(14f), FontStyle.Bold),             // 字体：14pt加粗（DPI自适应）
                    Location = new Point(0, y-30),                     // 位置：水平居中（宽度=窗口宽度），垂直偏移-30微调
                    Size = new Size(dlg.ClientSize.Width, SY(40)),       // 大小：宽=窗口宽度，高=40px（增大高度避免文字截断）
                    TextAlign = ContentAlignment.MiddleCenter,           // 文字居中对齐
                    ForeColor = textColor,                               // 文字颜色：主文字色
                    BackColor = Color.Transparent                        // 背景透明
                };
                dlg.Controls.Add(lblTitle);

                // y下移32px，为副标题留出空间
                y += SY(32);

                // ==================== 副标题标签"使用本软件前请仔细阅读以下条款" ====================
                Label lblSubtitle = new Label
                {
                    Text = "使用本软件前请仔细阅读以下条款",               // 副标题文字
                    Font = GetFont(SF(9f)),                              // 字体：9pt常规（DPI自适应）
                    Location = new Point(0, y-10),                          // 位置：水平居中，Y=当前游标-10微调
                    Size = new Size(dlg.ClientSize.Width, SY(20)),       // 大小：宽=窗口宽度，高=20px
                    TextAlign = ContentAlignment.MiddleCenter,            // 文字居中对齐
                    ForeColor = subTextColor,                            // 文字颜色：副文字色（略暗）
                    BackColor = Color.Transparent                        // 背景透明
                };
                dlg.Controls.Add(lblSubtitle);

                // y下移24px，为分割线留出空间
                y += SY(24);

                // ==================== 顶部分割线 ====================
                Panel dividerTop = new Panel
                {
                    Location = new Point(padX, y),           // 位置：左边距=padX，Y=当前游标
                    Size = new Size(contentW, 1),            // 大小：宽=内容宽度，高=1px
                    BackColor = dividerColor                 // 背景色：分割线颜色
                };
                dlg.Controls.Add(dividerTop);
                // y下移18px，为内容面板留出间距
                y += SY(18);

                // ==================== 免责声明正文内容 ====================
                string disclaimerText =
@"第一条  软件性质

本软件仅为「流媒体链接技术检测工具」，仅提供链接连通性、媒体编码、网络延迟检测功能。软件本身不生产、不存储、不提供任何 IPTV 直播源、影视播放地址、电视节目资源。

第二条  责任归属

所有待检测流媒体链接、频道地址均由使用者自行导入、自行获取。用户访问、检测第三方流媒体地址产生的一切著作权纠纷、行政处罚、法律责任，全部由使用者独立承担，与软件开发者无关。

第三条  禁止行为

严禁使用本软件从事以下行为：
    1. 窃取、破解运营商专网 IPTV 组播信号
    2. 爬取、售卖、分发无版权直播源
    3. 搭建商用非法视听、直播服务
    4. 绕过版权保护收看付费影视、有线电视节目

第四条  合规使用

使用者应当严格遵守《中华人民共和国网络安全法》《中华人民共和国著作权法》《互联网视听节目服务管理规定》等法律法规，仅检测自身拥有合法授权的流媒体链接。

第五条  免责条款

本程序按现状免费提供，不提供任何明示或隐含担保。因使用本软件造成 IP 封禁、网络限制、设备故障等损失，开发者不承担任何赔偿责任。";

                // ==================== 内容面板（包裹RichTextBox，带边框） ====================
                Panel contentPanel = new Panel
                {
                    Location = new Point(padX, y),               // 位置：左边距=padX，Y=当前游标
                    Size = new Size(contentW, SY(400)),           // 大小：宽=内容宽度，高=400px（段落间距减少，高度相应减小）
                    BackColor = contentBg,                        // 背景色：内容区域背景色
                    BorderStyle = BorderStyle.FixedSingle         // 边框：单线边框
                };

                // ==================== 免责声明文本框（RichTextBox，支持滚动） ====================
                RichTextBox txtDisclaimer = new RichTextBox
                {
                    Text = disclaimerText,                                    // 文本内容
                    Multiline = true,                                         // 多行模式
                    ReadOnly = true,                                          // 只读
                    ScrollBars = RichTextBoxScrollBars.Vertical,              // 垂直滚动条
                    Location = new Point(SX(12), SY(12)),                     // 位置：距面板左上角各12px内边距（增大2px更美观）
                    Size = new Size(contentW - SX(24), SY(376)),             // 大小：宽=内容宽度-24px内边距，高=376px（配合面板高度调整）
                    Font = GetFont(SF(9f)),                                   // 字体：9pt（DPI自适应，增大0.5pt更清晰）
                    BackColor = contentBg,                                    // 背景色：与面板一致
                    ForeColor = textColor,                                    // 文字颜色：主文字色
                    BorderStyle = BorderStyle.None,                           // 无边框（由面板提供边框）
                    WordWrap = true,                                          // 自动换行
                    DetectUrls = false,                                       // 不自动检测URL
                    SelectionTabs = new int[] { SX(20) }                      // 设置缩进：条款项缩进20px
                };
                // 设置文本居中对齐（标题和列表项居中，更美观）
                txtDisclaimer.SelectAll();
                txtDisclaimer.SelectionAlignment = HorizontalAlignment.Center;
                txtDisclaimer.DeselectAll();

                // 手动设置条款标题加粗（"第一条"、"第二条"等标题更醒目）
                ApplyDisclaimerFormatting(txtDisclaimer, accentColor);

                contentPanel.Controls.Add(txtDisclaimer);
                dlg.Controls.Add(contentPanel);

                // 在控件添加到父容器后，设置滚动位置到顶部（确保从第一条开始显示）
                txtDisclaimer.SelectionStart = 0;
                txtDisclaimer.ScrollToCaret();

                // y下移418px（内容面板高度400+间距18），为提示文字留出空间
                y += SY(418);

                // ==================== 提示文字标签（倒计时/状态提示） ====================
                Label lblHint = new Label
                {
                    Text = "请滚动阅读至底部并等待倒计时结束",                  // 初始提示文字
                    Font = GetFont(SF(8.5f)),                                 // 字体：8.5pt（DPI自适应）
                    Location = new Point(0, y),                               // 位置：水平居中，Y=当前游标
                    Size = new Size(dlg.ClientSize.Width, SY(20)),            // 大小：宽=窗口宽度，高=20px
                    TextAlign = ContentAlignment.MiddleCenter,                // 文字居中对齐
                    ForeColor = hintColor,                                    // 文字颜色：提示色
                    BackColor = Color.Transparent                             // 背景透明
                };
                dlg.Controls.Add(lblHint);

                // y下移26px，为复选框留出空间
                y += SY(26);

                // ==================== 同意条款复选框 ====================
                CheckBox cbAgree = new CheckBox
                {
                    Text = "我已仔细阅读并同意以上全部条款",                    // 复选框文字
                    AutoSize = true,                                          // 自动大小（根据文字内容）
                    TextAlign = ContentAlignment.MiddleLeft,                  // 文字左中对齐
                    CheckAlign = ContentAlignment.MiddleLeft,                 // 勾选框左中对齐
                    ForeColor = textColor,                                    // 文字颜色：主文字色
                    BackColor = Color.Transparent,                            // 背景透明
                    Font = GetFont(SF(9.5f)),                                 // 字体：9.5pt（DPI自适应）
                    Enabled = false                                           // 初始禁用（需满足条件后启用）
                };
                dlg.Controls.Add(cbAgree);
                // 复选框水平居中：X = (窗口宽度 - 复选框宽度) / 2
                cbAgree.Location = new Point((dlg.ClientSize.Width - cbAgree.Width) / 2, y);

                // y下移32px，为按钮留出空间
                y += SY(32);

                // ==================== "进入软件"按钮 ====================
                Button btnEnter = new Button
                {
                    Text = "进入软件",                                        // 按钮文字
                    Location = new Point(padX, y),                           // 位置：左边距=padX，Y=当前游标
                    Size = new Size(contentW, SY(40)),                       // 大小：宽=内容宽度，高=40px（DPI缩放后）
                    Font = GetFont(SF(10f), FontStyle.Regular),              // 字体：10pt常规（DPI自适应）
                    FlatStyle = FlatStyle.Flat,                              // 扁平样式
                    Enabled = false,                                         // 初始禁用（需勾选同意后启用）
                    BackColor = btnDisabledBg,                               // 背景色：禁用态颜色
                    ForeColor = btnDisabledText,                             // 文字颜色：禁用态颜色
                    Cursor = Cursors.No,                                     // 鼠标光标：禁止样式
                    UseVisualStyleBackColor = false                          // 禁用系统视觉样式（使用自定义颜色）
                };
                btnEnter.FlatAppearance.BorderSize = 0;                      // 扁平样式边框宽度：0（无边框）
                // 按钮鼠标悬停时的背景色：深色为亮蓝，浅色为深蓝
                btnEnter.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(68, 145, 238) : Color.FromArgb(36, 118, 225);
                dlg.Controls.Add(btnEnter);

                // ==================== 倒计时与交互状态控制 ====================
                bool canAgree = false;              // 是否允许勾选同意（需同时满足：滚动到底+倒计时结束）
                bool hasScrolledToBottom = false;    // 用户是否已滚动到文本底部
                bool timerStarted = false;           // 倒计时是否已启动（首次滚动时启动）
                int countdownSeconds = 8;            // 倒计时秒数：用户需阅读8秒后方可勾选同意

                // 倒计时定时器：每秒触发一次
                System.Windows.Forms.Timer countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                countdownTimer.Tick += (s, e) =>
                {
                    countdownSeconds--;
                    if (countdownSeconds <= 0)
                    {
                        // 倒计时结束：更新提示文字为成功状态
                        countdownSeconds = 0;
                        countdownTimer.Stop();
                        lblHint.Text = "✓ 阅读时间已满足，请勾选同意条款后进入软件";
                        lblHint.ForeColor = successColor;
                        UpdateAgreeState();
                    }
                    else
                    {
                        // 倒计时进行中：更新提示文字显示剩余秒数
                        lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请滚动至底部";
                    }
                };

                // 文本框滚动事件：首次滚动启动倒计时，滚动到底部时更新状态
                txtDisclaimer.VScroll += (s, e) =>
                {
                    // 首次滚动时启动倒计时，提示文字变为警告色
                    if (!timerStarted)
                    {
                        timerStarted = true;
                        countdownTimer.Start();
                        lblHint.Text = $"⏳ 阅读倒计时 {countdownSeconds} 秒 · 请滚动至底部";
                        lblHint.ForeColor = warningColor;
                    }

                    // 通过Win32 API获取滚动条位置，判断是否滚动到底部
                    var scrollInfo = new SCROLLINFO();
                    scrollInfo.cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO));
                    scrollInfo.fMask = 7;  // SIF_RANGE | SIF_PAGE | SIF_POS
                    GetScrollInfo(txtDisclaimer.Handle, 1, ref scrollInfo);

                    // 判断是否到达底部：当前位置 + 可见页高度 >= 最大滚动范围 - 2（容差）
                    bool atBottom = scrollInfo.nPos + (int)scrollInfo.nPage >= scrollInfo.nMax - 2;
                    if (atBottom && !hasScrolledToBottom)
                    {
                        hasScrolledToBottom = true;
                        UpdateAgreeState();
                    }
                };

                // 更新同意复选框的可用状态
                void UpdateAgreeState()
                {
                    // 仅当"已滚动到底"且"倒计时结束"时才允许勾选
                    canAgree = hasScrolledToBottom && countdownSeconds <= 0;
                    cbAgree.Enabled = canAgree;
                    // 如果条件不满足但已勾选，则取消勾选
                    if (!canAgree && cbAgree.Checked)
                        cbAgree.Checked = false;
                }

                // 复选框状态变化事件：控制"进入软件"按钮的启用/禁用
                cbAgree.CheckedChanged += (s, e) =>
                {
                    bool ready = cbAgree.Checked && canAgree;
                    btnEnter.Enabled = ready;
                    btnEnter.BackColor = ready ? btnEnabledBg : btnDisabledBg;
                    btnEnter.ForeColor = ready ? btnEnabledText : btnDisabledText;
                    btnEnter.Cursor = ready ? Cursors.Hand : Cursors.No;
                };

                // "进入软件"按钮点击事件：用户同意条款，保存配置并关闭窗口
                btnEnter.Click += (s, e) =>
                {
                    dialogResult = true;           // 设置返回值为true
                    disclaimerAgreed = true;       // 保存到实例字段
                    countdownTimer.Stop();         // 停止倒计时
                    SaveConfig();                  // 持久化保存同意状态
                    dlg.DialogResult = DialogResult.OK;  // 设置对话框返回值为OK
                    dlg.Close();                   // 关闭窗口
                };

                // 窗口关闭事件：停止倒计时，根据返回值设置对话框结果
                dlg.FormClosing += (s, e) =>
                {
                    countdownTimer.Stop();
                    if (!dialogResult)
                    {
                        dlg.DialogResult = DialogResult.Cancel;  // 未同意则返回Cancel
                    }
                };

                // 在对话框显示后立即设置滚动位置到顶部（确保从第一条开始显示）
                dlg.Shown += (s, e) =>
                {
                    txtDisclaimer.SelectionStart = 0;
                    txtDisclaimer.ScrollToCaret();
                };

                dlg.ShowDialog(this);

                return dialogResult;
            }
        }

        /// <summary>
        /// 为免责声明文本框应用格式化：条款标题加粗并使用强调色
        /// </summary>
        /// <param name="rtb">RichTextBox控件</param>
        /// <param name="accentColor">强调色（用于标题高亮）</param>
        private void ApplyDisclaimerFormatting(RichTextBox rtb, Color accentColor)
        {
            string[] titles = { "第一条", "第二条", "第三条", "第四条", "第五条" };

            foreach (string title in titles)
            {
                int startIndex = rtb.Text.IndexOf(title);
                if (startIndex >= 0)
                {
                    rtb.Select(startIndex, title.Length);
                    rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                    rtb.SelectionColor = accentColor;
                    rtb.SelectionAlignment = HorizontalAlignment.Center;
                    rtb.DeselectAll();
                }
            }
        }

        private void RefreshFontsImmediately()
        {
            this.Font = GetFont(SF(10.5f));
            
            if (dgvData != null)
            {
                dgvData.Font = GetFont(SF(6.7f));
                dgvData.ColumnHeadersDefaultCellStyle.Font = GetFont(SF(9f));
                dgvData.RowsDefaultCellStyle.Font = GetFont(SF(6.7f));
                dgvData.AlternatingRowsDefaultCellStyle.Font = GetFont(SF(6.7f));
                if (dgvData.Columns["colUrl"] != null)
                {
                    dgvData.Columns["colUrl"].DefaultCellStyle.Font = GetFont(SF(6.7f));
                }
            }
            
            RefreshControlFonts(this.Controls);
            RefreshContextMenuFonts();
            config.Initialize(dpiScale);
            
            RefreshNavButtonSizes();
            RefreshComponentSizes();
            
            this.Invalidate();
        }
        
        private void RefreshNavButtonSizes()
        {
            if (titleBarPanel == null || btnNavDetect == null) return;
            
            Font navFont = btnNavDetect.Font;
            int requiredBtnWidth = 0;
            
            using (Graphics g = Graphics.FromHwnd(this.Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                foreach (Button btn in new[] { btnNavDetect, btnNavSearch, btnNavSettings, btnNavAbout })
                {
                    if (btn != null)
                    {
                        SizeF textSize = g.MeasureString(btn.Text, btn.Font);
                        requiredBtnWidth = Math.Max(requiredBtnWidth, (int)textSize.Width);
                    }
                }
            }
            
            requiredBtnWidth += 24;
            int requiredBtnHeight = (int)(navFont.Height * 1.4);
            int maxBtnHeight = (int)(titleBarPanel.Height * 0.9);
            requiredBtnHeight = Math.Min(requiredBtnHeight, maxBtnHeight);
            
            int navBtnY = (titleBarPanel.Height - requiredBtnHeight) / 2;
            int navBtnRadius = 4;
            int navBtnGap = 1;
            
            int startX = SX(42);
            
            Action<Button> updateBtn = (btn) =>
            {
                if (btn != null)
                {
                    btn.Width = requiredBtnWidth;
                    btn.Height = requiredBtnHeight;
                    btn.Top = navBtnY;
                    btn.Region?.Dispose();
                    using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), navBtnRadius))
                        btn.Region = new Region(path);
                    btn.Invalidate();
                }
            };
            
            btnNavDetect.Left = startX;
            updateBtn(btnNavDetect);
            
            int currentX = btnNavDetect.Right + navBtnGap;
            
            if (btnNavSearch.Visible)
            {
                btnNavSearch.Left = currentX;
                updateBtn(btnNavSearch);
                currentX = btnNavSearch.Right + navBtnGap;
            }
            
            btnNavSettings.Left = currentX;
            updateBtn(btnNavSettings);
            
            btnNavAbout.Left = btnNavSettings.Right + navBtnGap;
            updateBtn(btnNavAbout);
        }
        
        private void RefreshComponentSizes()
        {
            if (actionArea != null)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    int btnW = SX(126);
                    int leftX = SX(12);
                    int ay = SY(14);
                    
                    foreach (Control ctrl in actionArea.Controls)
                    {
                        if (ctrl is Button btn && !string.IsNullOrEmpty(btn.Text))
                        {
                            SizeF textSize = g.MeasureString(btn.Text, btn.Font);
                            int requiredBtnHeight = (int)(textSize.Height * 1.5) + 6;
                            requiredBtnHeight = Math.Max(requiredBtnHeight, SY(28));
                            
                            btn.Width = btnW;
                            btn.Height = requiredBtnHeight;
                            btn.Left = leftX;
                            btn.Top = ay;
                            
                            ay += requiredBtnHeight + SY(10);
                            
                            btn.Region?.Dispose();
                            using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), 8))
                                btn.Region = new Region(path);
                            btn.Invalidate();
                        }
                        else if (ctrl is Panel && ctrl == tipBox)
                        {
                            ctrl.Top = ay;
                            ay += ctrl.Height + SY(10);
                        }
                    }
                }
            }
            
            if (searchPanelRef != null)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    float baseFontSize = SF(8.5f);
                    Font baseFont = GetFont(baseFontSize);
                    SizeF textSize = g.MeasureString("搜 索 :", baseFont);
                    int requiredHeight = (int)(textSize.Height * 1.6) + 8;
                    requiredHeight = Math.Max(requiredHeight, SY(32));
                    
                    searchPanelRef.Height = requiredHeight;
                    
                    foreach (Control ctrl in searchPanelRef.Controls)
                    {
                        if (ctrl is Label lbl && (lbl.Text == "搜 索 :" || lbl.Text == "分组:"))
                        {
                            lbl.Height = (int)textSize.Height + 4;
                            lbl.Top = (searchPanelRef.Height - lbl.Height) / 2;
                        }
                        else if (ctrl is Panel p && p.Width == 110)
                        {
                            p.Size = new Size(110, (int)textSize.Height + 6);
                            p.Top = (searchPanelRef.Height - p.Height) / 2;
                        }
                    }
                    
                    if (searchBoxHostRef != null)
                    {
                        searchBoxHostRef.Size = new Size(searchPanelRef.Width - SX(300), (int)textSize.Height + 6);
                        searchBoxHostRef.Top = (searchPanelRef.Height - searchBoxHostRef.Height) / 2;
                        
                        if (txtSearchBox != null)
                        {
                            txtSearchBox.Top = (searchBoxHostRef.Height - txtSearchBox.Height) / 2;
                        }
                    }
                }
            }
            
            if (statusBarRef != null)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    Font statusFont = GetFont(SF(9.5f));
                    SizeF textSize = g.MeasureString("已检测: 0/0", statusFont);
                    int requiredHeight = (int)(textSize.Height * 1.6) + 4;
                    requiredHeight = Math.Max(requiredHeight, SY(24));
                    
                    statusBarRef.Height = requiredHeight;
                }
                LayoutStatusBar(statusBarRef);
                UpdateStatusBarRegion();
            }
            
            if (dgvData != null)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    Font rowFont = GetFont(SF(6.7f));
                    SizeF textSize = g.MeasureString("测试文字", rowFont);
                    int requiredRowHeight = (int)(textSize.Height * 1.4) + 4;
                    requiredRowHeight = Math.Max(requiredRowHeight, SY(28));
                    
                    dgvData.RowTemplate.Height = requiredRowHeight;
                    
                    Font headerFont = GetFont(SF(9f));
                    SizeF headerSize = g.MeasureString("名称", headerFont);
                    int requiredHeaderHeight = (int)(headerSize.Height * 1.4) + 4;
                    requiredHeaderHeight = Math.Max(requiredHeaderHeight, SY(30));
                    
                    dgvData.ColumnHeadersHeight = requiredHeaderHeight;
                }
            }
            
            if (tipBox != null && tipBox.Visible)
            {
                UpdateTipBoxSize();
            }
            
            if (emptyStatePanel != null && emptyStatePanel.Visible)
            {
                CenterEmptyState();
            }
        }
        
        private void RefreshContextMenuFonts()
        {
            Font menuFont = GetFont(SF(9f));
            
            if (dataGridViewContextMenu != null)
            {
                dataGridViewContextMenu.Font = menuFont;
                foreach (ToolStripItem item in dataGridViewContextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                    {
                        RefreshDropDownMenuFonts(menuItem.DropDownItems, menuFont);
                    }
                }
            }
            
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.ContextMenuStrip != null)
                {
                    ctrl.ContextMenuStrip.Font = menuFont;
                }
            }
            
            if (_toastPanel != null)
            {
                foreach (Control ctrl in _toastPanel.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.Font = lbl.Text == "✓" ? GetFont(SF(11f), FontStyle.Bold) : GetFont(SF(9f), FontStyle.Bold);
                    }
                }
            }
        }
        
        private void RefreshDropDownMenuFonts(ToolStripItemCollection items, Font font)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.Font = font;
                    if (menuItem.HasDropDownItems)
                    {
                        RefreshDropDownMenuFonts(menuItem.DropDownItems, font);
                    }
                }
            }
        }
        
        private void RefreshControlFonts(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl.Tag != null && ctrl.Tag.ToString() == "noFontRefresh")
                {
                    RefreshControlFonts(ctrl.Controls);
                    continue;
                }
                
                float size = ctrl.Font.SizeInPoints;
                FontStyle style = ctrl.Font.Style;
                
                if (ctrl is Label || ctrl is Button || ctrl is TextBox || ctrl is ComboBox ||
                    ctrl is RadioButton || ctrl is CheckBox || ctrl is GroupBox ||
                    ctrl is Panel || ctrl is TabControl || ctrl is ListBox ||
                    ctrl is ToggleSwitch)
                {
                    ctrl.Font = GetFont(size, style);
                }
                
                RefreshControlFonts(ctrl.Controls);
            }
        }

        public class ToggleSwitch : Control
        {
            private bool _checked;
            private bool _targetChecked;
            private float _animProgress = 1f;
            private System.Windows.Forms.Timer _animTimer;

            public event EventHandler<ToggleChangingEventArgs> ToggleChanging;

            public bool Checked
            {
                get { return _checked; }
                set
                {
                    if (_checked != value)
                    {
                        _targetChecked = value;
                        // 初始化阶段（控件句柄未创建）直接设置状态，不播放动画
                        // 避免Timer无法触发导致首次显示时动画异常
                        if (!this.IsHandleCreated)
                        {
                            _checked = value;
                            _animProgress = 1f;
                            OnCheckedChanged(EventArgs.Empty);
                            return;
                        }
                        if (_animTimer == null)
                        {
                            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
                            _animTimer.Tick += (s, e) => { UpdateAnimation(); };
                        }
                        _animProgress = 0f;
                        _animTimer.Start();
                        OnCheckedChanged(EventArgs.Empty);
                    }
                }
            }

            public string OnText { get; set; } = "开";
            public string OffText { get; set; } = "关";
            public Color OnColor { get; set; } = Color.FromArgb(46, 169, 92);
            public Color OffColor { get; set; } = Color.FromArgb(205, 205, 210);
            public event EventHandler CheckedChanged;
            protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }

            public ToggleSwitch()
            {
                this.Size = new Size(110, 36);
                this.DoubleBuffered = true;
                this.Cursor = Cursors.Hand;
                this.Font = GetFont(11.5f);
                this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                this.BackColor = Color.Transparent;
                _targetChecked = _checked;
                this.HandleCreated += (s, e) => { this.Invalidate(); };
                this.VisibleChanged += (s, e) => { if (this.Visible) this.Invalidate(); };
                this.ParentChanged += ToggleSwitch_ParentChanged;
            }

            private void ToggleSwitch_ParentChanged(object sender, EventArgs e)
            {
                this.Invalidate();
            }

            protected override void OnVisibleChanged(EventArgs e)
            {
                base.OnVisibleChanged(e);
                if (this.Visible)
                    this.Invalidate();
            }

            protected override void OnLocationChanged(EventArgs e)
            {
                base.OnLocationChanged(e);
                this.Invalidate();
            }

            private void UpdateAnimation()
            {
                _animProgress += 0.15f;
                if (_animProgress >= 1f)
                {
                    _animProgress = 1f;
                    _checked = _targetChecked;
                    if (_animTimer != null) _animTimer.Stop();
                }
                this.Invalidate();
            }

            private Color LerpColor(Color c1, Color c2, float t)
            {
                return Color.FromArgb(
                    (int)(c1.A + (c2.A - c1.A) * t),
                    (int)(c1.R + (c2.R - c1.R) * t),
                    (int)(c1.G + (c2.G - c1.G) * t),
                    (int)(c1.B + (c2.B - c1.B) * t)
                );
            }

            private Color GetRealBackColor()
            {
                Control ctrl = this.Parent;
                while (ctrl != null)
                {
                    if (ctrl.BackColor != Color.Transparent)
                        return ctrl.BackColor;
                    ctrl = ctrl.Parent;
                }
                return Color.White;
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
            }

            protected override void OnClick(EventArgs e)
            {
                var args = new ToggleChangingEventArgs(!_checked);
                ToggleChanging?.Invoke(this, args);
                if (!args.Cancel)
                    Checked = !_checked;
                base.OnClick(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                Color bgColor = GetRealBackColor();
                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                    g.FillRectangle(bgBrush, this.ClientRectangle);

                int w = this.Width, h = this.Height;
                int pillH = Math.Min(h - 4, 32);
                int pillY = (h - pillH) / 2;
                int pillR = pillH / 2;
                Rectangle pillRect = new Rectangle(0, pillY, w - 1, pillH - 1);

                float t = _animProgress;
                bool isTurningOn = _targetChecked && !_checked;
                bool isTurningOff = !_targetChecked && _checked;

                Color pillColor = isTurningOn ? LerpColor(OffColor, OnColor, t) :
                                  isTurningOff ? LerpColor(OnColor, OffColor, t) :
                                  (_checked ? OnColor : OffColor);

                using (SolidBrush br = new SolidBrush(pillColor))
                using (GraphicsPath path = GetRoundedPath(pillRect, pillR))
                {
                    g.FillPath(br, path);
                }

                int dotMargin = 3;
                int dotSize = pillH - dotMargin * 2;
                int dotY = pillY + dotMargin;
                int dotXOff = dotMargin;
                int dotXOn = w - dotSize - dotMargin - 1;
                int dotX = (int)(isTurningOn ? dotXOff + (dotXOn - dotXOff) * t :
                                 isTurningOff ? dotXOn + (dotXOff - dotXOn) * t :
                                 (_checked ? dotXOn : dotXOff));

                using (SolidBrush dotBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                }
            }
        }

        public class ToggleChangingEventArgs : EventArgs
        {
            public bool NewValue { get; }
            public bool Cancel { get; set; }
            public ToggleChangingEventArgs(bool newValue) { NewValue = newValue; }
        }

        private void IPTVLiveCheckerMain_Load(object sender, EventArgs e)
        {
            if (persistList) LoadChannelList();
            try
            {
                this.Icon = LoadIconFromResources();
            }
            catch { this.Icon = GenerateAppIcon(512); }
            BuildUI();
            // 使用 Func<Task> 避免 async void 异常传播问题
            var initTask = new Func<Task>(async () =>
            {
                await Task.Delay(300);
            });
            this.BeginInvoke(new Action(() => { _ = initTask(); }));
        }

        /// <summary>
        /// 创建自定义标题栏
        /// 包含窗口图标、导航按钮（解析、搜索、设置、关于）和窗口控制按钮（主题切换、最小化、最大化、关闭）
        /// 所有尺寸已适配DPI缩放，自动根据主题切换颜色
        /// </summary>
        private void CreateTitleBar()
        {
            // ========== 标题栏面板 ==========
            // [位置] 顶部Dock填充 [高度] 40px [背景] 使用主题背景色
            titleBarPanel = new Panel
            {
                Dock = DockStyle.Top,           // 顶部停靠
                Height = SY(40),               // 标题栏高度（40px * DPI缩放）
                BackColor = theme.Bg            // 背景色跟随主题
            };

            // ========== 窗口图标（左侧） ==========
            // [位置] (12, 9) [大小] 22x22 [绘制] 自定义电视图标（使用主题主色）
            PictureBox titleIcon = new PictureBox
            {
                Size = new Size(SX(22), SY(22)),
                Location = new Point(SX(12), SY(9)),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            titleIconRef = titleIcon;
            // 自定义绘制电视图标
            titleIcon.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush tvBrush = new SolidBrush(theme.Primary))
                {
                    // 绘制电视主体（圆角矩形）
                    using (GraphicsPath tvPath = RoundedRectPath(new Rectangle(SX(2), SY(4), SX(18), SY(13)), SX(3)))
                        g.FillPath(tvBrush, tvPath);
                    // 绘制屏幕（内部矩形）
                    using (SolidBrush screenBrush = new SolidBrush(theme.Bg))
                        g.FillRectangle(screenBrush, new Rectangle(SX(4), SY(6), SX(14), SY(9)));
                    // 绘制底座
                    g.FillRectangle(tvBrush, SX(7), SY(17), SX(7), SY(2));
                    g.FillRectangle(tvBrush, SX(5), SY(19), SX(12), SY(2));
                }
            };
            titleBarPanel.Controls.Add(titleIcon);

            // ========== 窗口控制按钮配置（右上角） ==========
            // 控制按钮：主题切换、最小化、最大化、关闭，每个按钮40x40px，圆角8px
            int btnSize = SY(40);                              // 控制按钮尺寸（40px * DPI缩放）
            Color titleBtnBg = theme.Bg;                      // 控制按钮背景色
            Color titleBtnFg = theme.TextSecondary;           // 控制按钮文字颜色
            Color titleBtnHover = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235); // 控制按钮悬停色
            Color closeBtnHover = Color.FromArgb(232, 17, 35); // 关闭按钮悬停色（红色）
            Color closeBtnFg = Color.White;                   // 关闭按钮文字颜色

            // ========== 导航按钮配置（左侧图标右侧） ==========
            // 导航按钮：解析(P)、搜索(F)、设置(S)、关于(A)，带悬停高亮效果
            int navBtnWidth = SX(75);                          // 导航按钮宽度（75px * DPI缩放）
            int navBtnHeight = (int)(titleBarPanel.Height * 0.6); // 导航按钮高度 = 标题栏高度的60%
            int navBtnY = (titleBarPanel.Height - navBtnHeight) / 2; // 导航按钮垂直居中
            int navBtnGap = 1;                                 // 导航按钮之间间距
            int navBtnRadius = 4;                              // 导航按钮圆角半径（4px）

            Color navBtnText = IsDarkColor(theme.Bg) ? Color.White : Color.Black;             // 导航按钮文字颜色
            navBtnHoverBg = IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230); // 导航按钮悬停背景色

            // ========== 导航按钮通用绘制方法 ==========
            // 实现导航按钮的自定义绘制逻辑：悬停时显示背景高亮，文字居中显示
            void PaintNavButton(object sender, PaintEventArgs e)
            {
                Button btn = (Button)sender;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // 清除背景（使用父容器背景色填充，实现透明效果）
                Color parentBg = btn.Parent != null ? btn.Parent.BackColor : Color.White;
                using (SolidBrush clearBrush = new SolidBrush(parentBg))
                    g.FillRectangle(clearBrush, 0, 0, btn.Width, btn.Height);

                // 判断是否悬停（鼠标在按钮区域内）
                bool isHover = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));

                // 绘制背景（悬停时显示圆角矩形高亮）
                if (isHover)
                {
                    Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                    using (GraphicsPath path = RoundedRectPath(rect, navBtnRadius))
                    using (SolidBrush bgBrush = new SolidBrush(navBtnHoverBg))
                        g.FillPath(bgBrush, path);
                }

                // 绘制文字（带8px左右内边距，防止文字超出圆角区域）
                Rectangle textRect = new Rectangle(8, 0, btn.Width - 16, btn.Height);
                using (SolidBrush textBrush = new SolidBrush(btn.ForeColor))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(btn.Text, btn.Font, textBrush, textRect, sf);
                }
            }

            // ========== 导航按钮通用事件绑定方法 ==========
            // 设置导航按钮样式为无边框扁平按钮，绑定鼠标事件实现悬停效果和圆角自适应
            void AttachNavButtonEvents(Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.Empty;
                btn.FlatAppearance.MouseDownBackColor = Color.Empty;

                // 设置圆角Region（裁剪按钮区域为圆角矩形）
                btn.Region?.Dispose();
                using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), navBtnRadius))
                    btn.Region = new Region(path);

                // 绑定绘制和鼠标事件
                btn.Paint += PaintNavButton;                          // 自定义绘制
                btn.MouseEnter += (s, e) => btn.Invalidate();        // 鼠标进入时重绘
                btn.MouseLeave += (s, e) => btn.Invalidate();        // 鼠标离开时重绘
                btn.Resize += (s, e) =>
                {
                    // 按钮大小变化时重新设置圆角Region
                    btn.Region?.Dispose();
                    using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), navBtnRadius))
                        btn.Region = new Region(path);
                    btn.Invalidate();
                };
            }

            // ========== 导航按钮：解析 (P) ==========
            // [位置] (42, navBtnY) [大小] (navBtnWidth+20) x navBtnHeight [字体] YaHei 9pt [快捷键] P
            btnNavDetect = new Button
            {
                Text = "解析 (P)",
                Size = new Size(navBtnWidth + 20, navBtnHeight),
                Location = new Point(SX(42), navBtnY),
                BackColor = Color.Transparent,
                ForeColor = navBtnText,
                Font = GetFont(SF(9f), FontStyle.Regular),
                Cursor = Cursors.Hand,
                Tag = "nav:解析",
                TabStop = false
            };
            AttachNavButtonEvents(btnNavDetect);
            btnNavDetect.Click += (s, e) => ShowIptvParserDialog();
            titleBarPanel.Controls.Add(btnNavDetect);

            // ========== 导航按钮：搜索 (F) ==========
            // [位置] 在解析按钮右侧，间距navBtnGap [大小] 同解析按钮 [字体] YaHei 9pt [快捷键] F
            btnNavSearch = new Button
            {
                Text = "搜索 (F)",
                Size = new Size(navBtnWidth + 20, navBtnHeight),
                Location = new Point(SX(42) + navBtnWidth + 20 + navBtnGap, navBtnY),
                BackColor = Color.Transparent,
                ForeColor = navBtnText,
                Font = GetFont(SF(9f), FontStyle.Regular),
                Cursor = Cursors.Hand,
                Tag = "nav:搜索",
                TabStop = false,
                Visible = showSearchButton
            };
            AttachNavButtonEvents(btnNavSearch);
            btnNavSearch.Click += (s, e) =>
            {
                if (watchSearchWindow)
                {
                    string searchRule = "title=\"IPTV\" || title=\"直播\"";
                    string baseUrl = "https://fofa.info/result?qbase64=";
                    string url = baseUrl + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(searchRule));
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch { }
                }
                else
                {
                    ShowSearchEngineDialog();
                }
            };
            titleBarPanel.Controls.Add(btnNavSearch);

            // ========== 导航按钮：设置 (S) ==========
            // [位置] 在搜索按钮右侧，间距navBtnGap [大小] 同解析按钮 [字体] YaHei 9pt [快捷键] S
            btnNavSettings = new Button
            {
                Text = "设置 (S)",
                Size = new Size(navBtnWidth + 20, navBtnHeight),
                Location = new Point(SX(42) + (navBtnWidth + 20) * 2 + navBtnGap * 2, navBtnY ),
                BackColor = Color.Transparent,
                ForeColor = navBtnText,
                Font = GetFont(SF(9f), FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            AttachNavButtonEvents(btnNavSettings);
            btnNavSettings.Click += (s, e) => ShowSettingsDialog();
            titleBarPanel.Controls.Add(btnNavSettings);

            // ========== 导航按钮：关于 (A) ==========
            // [位置] 在设置按钮右侧，间距navBtnGap [大小] 同解析按钮 [字体] YaHei 9pt [快捷键] A
            btnNavAbout = new Button
            {
                Text = "关于 (A)",
                Size = new Size(navBtnWidth + 20, navBtnHeight),
                Location = new Point(SX(42) + (navBtnWidth + 20) * 3 + navBtnGap * 3, navBtnY),
                BackColor = Color.Transparent,
                ForeColor = navBtnText,
                Font = GetFont(SF(9f), FontStyle.Regular),
                Cursor = Cursors.Hand,
                Tag = "nav:关于",
                TabStop = false
            };
            AttachNavButtonEvents(btnNavAbout);
            btnNavAbout.Click += (s, e) => ShowAboutDialog();
            titleBarPanel.Controls.Add(btnNavAbout);

            // ========== 标题栏布局更新方法 ==========
            // 窗口大小变化时，重新计算导航按钮和控制按钮的位置
            // 导航按钮固定在左侧，控制按钮右对齐
            void UpdateTitleAndNav()
            {
                RefreshNavButtonSizes();
                
                int w = titleBarPanel.ClientSize.Width;
                int btnGap = navBtnGap;
                if (btnNavDetect != null && btnNavSearch != null && btnNavSearch.Visible)
                    btnGap = btnNavSearch.Left - btnNavDetect.Right;
                
                int currentX = SX(42);
                if (btnNavDetect != null)
                {
                    btnNavDetect.Left = currentX;
                    currentX = btnNavDetect.Right + btnGap;
                }
                if (btnNavSearch != null && btnNavSearch.Visible)
                {
                    btnNavSearch.Left = currentX;
                    currentX = btnNavSearch.Right + btnGap;
                }
                if (btnNavSettings != null)
                {
                    btnNavSettings.Left = currentX;
                    currentX = btnNavSettings.Right + btnGap;
                }
                if (btnNavAbout != null)
                {
                    btnNavAbout.Left = currentX;
                }
                
                int totalBtnsWidth = btnSize * 4;
                int startX = w - totalBtnsWidth;
                btnThemeToggle.Left = startX;
                btnMin.Left = startX + btnSize;
                btnMax.Left = startX + btnSize * 2;
                btnClose.Left = startX + btnSize * 3;
            }

            // ========== 窗口控制按钮工厂方法 ==========
            // 创建统一风格的窗口控制按钮（主题切换、最小化、最大化、关闭）
            // [大小] 40x40px [圆角] 8px [样式] 扁平无边框
            Button CreateTitleButton()
            {
                Button b = new Button
                {
                    Size = new Size(btnSize, btnSize),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = titleBtnBg,
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor = titleBtnHover;
                b.FlatAppearance.CheckedBackColor = titleBtnHover;
                // 设置圆角Region（8px圆角，使按钮看起来圆润）
                using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btnSize, btnSize), 8))
                    b.Region = new Region(path);
                // 按钮大小变化时重新设置圆角Region
                b.Resize += (s, e) =>
                {
                    Button btn = (Button)s;
                    btn.Region?.Dispose();
                    using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), 8))
                        btn.Region = new Region(path);
                };
                return b;
            }

            // ========== 控制按钮：主题切换 ==========
            // [位置] 右上角最左侧 [图标] 深色显示太阳，浅色显示月亮 [功能] 切换深色/浅色主题
            btnThemeToggle = CreateTitleButton();
            btnThemeToggle.Tag = "theme";
            btnThemeToggle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isDark = theme.Name == "深色";
                bool isHover = btnThemeToggle.ClientRectangle.Contains(btnThemeToggle.PointToClient(Cursor.Position));
                Color iconColor = isHover ? theme.TextPrimary : theme.TextSecondary;
                int baseSize = 40;
                int cx = baseSize / 2;
                int cy = baseSize / 2;
                bool currentIsDark = IsDarkColor(theme.Bg);
                if (currentIsDark)
                {
                    // 深色主题显示太阳图标（圆形+8条射线）
                    using (Pen pen = new Pen(iconColor, 1.6f))
                    using (SolidBrush br = new SolidBrush(iconColor))
                    {
                        int r = 7;
                        e.Graphics.FillEllipse(br, cx - r, cy - r, r * 2, r * 2);
                        for (int i = 0; i < 8; i++)
                        {
                            double angle = i * Math.PI / 4;
                            int x1 = cx + (int)((r + 2) * Math.Cos(angle));
                            int y1 = cy + (int)((r + 2) * Math.Sin(angle));
                            int x2 = cx + (int)((r + 5) * Math.Cos(angle));
                            int y2 = cy + (int)((r + 5) * Math.Sin(angle));
                            e.Graphics.DrawLine(pen, x1, y1, x2, y2);
                        }
                    }
                }
                else
                {
                    // 浅色主题显示月亮图标（两个重叠圆）
                    using (SolidBrush br = new SolidBrush(iconColor))
                    {
                        int r = 8;
                        e.Graphics.FillEllipse(br, cx - r + 2, cy - r, r * 2, r * 2);
                        using (SolidBrush bgBr = new SolidBrush(titleBarPanel.BackColor))
                            e.Graphics.FillEllipse(bgBr, cx - r + 6, cy - r - 2, r * 2, r * 2);
                    }
                }
            };
            btnThemeToggle.Click += (s, e) =>
            {
                string nextTheme = themePreference == "浅色" ? "深色" : "浅色";
                themePreference = nextTheme;
                AppTheme newTheme = nextTheme == "深色" ? AppTheme.Dark : AppTheme.Light;
                SetTheme(newTheme);
            };
            titleBarPanel.Controls.Add(btnThemeToggle);

            // ========== 控制按钮：最小化 ==========
            // [位置] 主题按钮右侧 [图标] 水平线 [功能] 将窗口最小化到任务栏
            btnMin = CreateTitleButton();
            btnMin.Paint += (s, e) =>
            {
                bool isHover = btnMin.ClientRectangle.Contains(btnMin.PointToClient(Cursor.Position));
                Color ic = isHover ? theme.TextPrimary : theme.TextSecondary;
                int baseSize = 40;
                // 绘制水平线图标（居中偏下）
                using (Pen pen = new Pen(ic, 1.5f))
                    e.Graphics.DrawLine(pen, baseSize / 2 - 8, baseSize / 2 + 6, baseSize / 2 + 8, baseSize / 2 + 6);
            };
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            titleBarPanel.Controls.Add(btnMin);

            // ========== 控制按钮：最大化/还原 ==========
            // [位置] 最小化按钮右侧 [图标] 最大化时显示还原图标，还原时显示最大化图标 [功能] 切换窗口最大化/还原状态
            btnMax = CreateTitleButton();
            btnMax.Paint += (s, e) =>
            {
                bool isHover = btnMax.ClientRectangle.Contains(btnMax.PointToClient(Cursor.Position));
                Color ic = isHover ? theme.TextPrimary : theme.TextSecondary;
                bool isMaximized = this.WindowState == FormWindowState.Maximized;
                int baseSize = 40;
                using (Pen pen = new Pen(ic, 1.5f))
                {
                    if (isMaximized)
                    {
                        // 最大化状态：显示还原图标（两个小矩形）
                        e.Graphics.DrawRectangle(pen, baseSize / 2 - 7, baseSize / 2 - 5, 9, 9);
                        e.Graphics.DrawRectangle(pen, baseSize / 2 - 4, baseSize / 2 - 8, 9, 9);
                    }
                    else
                    {
                        // 还原状态：显示最大化图标（一个大矩形）
                        e.Graphics.DrawRectangle(pen, baseSize / 2 - 7, baseSize / 2 - 7, 14, 14);
                    }
                }
            };
            btnMax.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            titleBarPanel.Controls.Add(btnMax);

            // ========== 控制按钮：关闭 ==========
            // [位置] 最大化按钮右侧 [图标] X形 [功能] 关闭应用程序
            // [特殊效果] 悬停时显示红色背景，按下时颜色加深
            btnClose = CreateTitleButton();
            btnClose.FlatAppearance.MouseOverBackColor = closeBtnHover;
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
            btnClose.Paint += (s, e) =>
            {
                bool isHover = btnClose.ClientRectangle.Contains(btnClose.PointToClient(Cursor.Position));
                bool isDown = MouseButtons == MouseButtons.Left && isHover;
                int baseSize = 40;
                // 根据状态确定背景色（按下>悬停>正常）
                Color bgColor = isDown ? Color.FromArgb(200, 15, 30) : (isHover ? closeBtnHover : btnClose.BackColor);
                // 绘制圆角背景
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, baseSize, baseSize), 8))
                using (SolidBrush bgBr = new SolidBrush(bgColor))
                    e.Graphics.FillPath(bgBr, path);
                // 根据状态确定图标颜色（悬停时白色，否则为次要文字色）
                Color ic = isHover ? closeBtnFg : theme.TextSecondary;
                int offset = isDown ? 1 : 0; // 按下时图标轻微偏移
                // 绘制X形图标（两条对角线）
                using (Pen pen = new Pen(ic, 1.6f))
                {
                    e.Graphics.DrawLine(pen, baseSize / 2 - 7 + offset, baseSize / 2 - 7 + offset, baseSize / 2 + 7 + offset, baseSize / 2 + 7 + offset);
                    e.Graphics.DrawLine(pen, baseSize / 2 + 7 + offset, baseSize / 2 - 7 + offset, baseSize / 2 - 7 + offset, baseSize / 2 + 7 + offset);
                }
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();
            btnClose.MouseLeave += (s, e) => btnClose.Invalidate();
            btnClose.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) btnClose.Invalidate(); };
            btnClose.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) btnClose.Invalidate(); };
            btnClose.Click += (s, e) => this.Close();
            titleBarPanel.Controls.Add(btnClose);

            // ========== 标题栏事件绑定 ==========
            // 窗口大小变化时更新布局
            titleBarPanel.Resize += (s, e) => UpdateTitleAndNav();

            // 标题栏鼠标拖拽（实现自定义标题栏拖动）
            titleBarPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            // 图标区域鼠标拖拽（同标题栏）
            titleIcon.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

            // 标题栏双击（切换最大化/还原）
            titleBarPanel.DoubleClick += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
                btnMax.Text = this.WindowState == FormWindowState.Maximized ? "❐" : "☐";
            };
        }

        private void CreateBottomBar()
        {
            bottomBarRef = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = theme.Bg
            };
            bottomBarRef.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            bottomBarRef.DoubleClick += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
            };
        }

        private void g_drawSunIcon(Graphics g, Rectangle rect, Pen pen, Color fillColor)
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            int r = rect.Width / 2 - 2;
            using (SolidBrush br = new SolidBrush(fillColor))
                g.FillEllipse(br, cx - r + 1, cy - r + 1, r * 2 - 2, r * 2 - 2);
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                int x1 = cx + (int)((r + 2) * Math.Cos(angle));
                int y1 = cy + (int)((r + 2) * Math.Sin(angle));
                int x2 = cx + (int)((r + 4) * Math.Cos(angle));
                int y2 = cy + (int)((r + 4) * Math.Sin(angle));
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void g_drawMoonIcon(Graphics g, Rectangle rect, Pen pen, Color fillColor)
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            int r = rect.Width / 2 - 1;
            using (SolidBrush br = new SolidBrush(fillColor))
            {
                g.FillEllipse(br, cx - r, cy - r, r * 2, r * 2);
                using (SolidBrush bgBr = new SolidBrush(titleBarPanel != null ? titleBarPanel.BackColor : Color.White))
                {
                    g.FillEllipse(bgBr, cx - r + 4, cy - r - 1, r * 2, r * 2);
                }
            }
        }

        private byte[] MakeSimpleIcon(byte[] pngData, int w, int h)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write((ushort)1);
                bw.Write((byte)(w >= 256 ? 0 : w));
                bw.Write((byte)(h >= 256 ? 0 : h));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write((uint)pngData.Length);
                bw.Write((uint)(6 + 16));
                bw.Write(pngData);
                return ms.ToArray();
            }
        }

        private void SaveMultiSizeIcon(string path)
        {
            int[] sizes = { 16, 32, 48, 64, 128, 256, 512 };
            var bitmaps = new List<Bitmap>();
            try
            {
                using (Bitmap master = GenerateAppIconBitmap(512))
                {
                    foreach (int s in sizes)
                    {
                        Bitmap resized = new Bitmap(s, s);
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.Clear(Color.Transparent);
                            g.DrawImage(master, 0, 0, s, s);
                        }
                        bitmaps.Add(resized);
                    }
                }
                SaveIcon(path, bitmaps);
            }
            finally
            {
                foreach (var bmp in bitmaps) bmp.Dispose();
            }
        }

        private Icon GenerateAppIcon(int size = 512)
        {
            using (Bitmap bmp = GenerateAppIconBitmap(size))
            {
                IntPtr hIcon = bmp.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                Icon result = (Icon)icon.Clone();
                icon.Dispose();
                DestroyIcon(hIcon);
                return result;
            }
        }

        private Icon LoadIconFromResources()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            string iconResourceName = resourceNames.FirstOrDefault(r => r.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(iconResourceName))
            {
                using (Stream stream = assembly.GetManifestResourceStream(iconResourceName))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
            }
            return GenerateAppIcon(512);
        }

        private Bitmap LoadWechatPromoImage(int maxWidth)
        {
            try
            {
                // 从内嵌的Base64字符串加载图片，无需外部文件
                byte[] imageBytes = Convert.FromBase64String(IPTVLiveChecker.Resources.WechatPromoResource.Base64Data);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Bitmap original = new Bitmap(ms);
                    int targetW = original.Width <= maxWidth ? original.Width : maxWidth;
                    int targetH = (int)((double)original.Height * targetW / original.Width);
                    Bitmap copy = new Bitmap(targetW, targetH);
                    using (Graphics g = Graphics.FromImage(copy))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(original, 0, 0, targetW, targetH);
                    }
                    original.Dispose();
                    return copy;
                }
            }
            catch { }

            return null;
        }

        private Bitmap GenerateAppIconBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                float scale = size / 512f;
                int margin = (int)(30 * scale);
                int cardSize = size - margin * 2;
                int cornerR = (int)(90 * scale);
                int innerR = (int)(82 * scale);

                using (GraphicsPath cardPath = GetRoundedPath(new Rectangle(margin, margin, cardSize, cardSize), cornerR))
                {
                    using (LinearGradientBrush cardBrush = new LinearGradientBrush(
                        new Rectangle(margin, margin, cardSize, cardSize),
                        Color.FromArgb(155, 105, 220),
                        Color.FromArgb(115, 65, 185),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(cardBrush, cardPath);
                    }
                    using (GraphicsPath innerGlow = GetRoundedPath(new Rectangle(margin + (int)(8 * scale), margin + (int)(8 * scale), cardSize - (int)(16 * scale), cardSize - (int)(16 * scale)), innerR))
                    using (LinearGradientBrush glow = new LinearGradientBrush(
                        new Rectangle(margin, margin, cardSize, cardSize / 2),
                        Color.FromArgb(60, 255, 255, 255),
                        Color.FromArgb(5, 255, 255, 255),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(glow, innerGlow);
                    }
                }

                int tvW = (int)(310 * scale), tvH = (int)(230 * scale);
                int tvX = (size - tvW) / 2;
                int tvY = (int)(120 * scale);
                int tvR = (int)(28 * scale);

                using (GraphicsPath tvBody = GetRoundedPath(new Rectangle(tvX, tvY, tvW, tvH), tvR))
                {
                    using (SolidBrush tvBrush = new SolidBrush(Color.FromArgb(255, 250, 252)))
                        g.FillPath(tvBrush, tvBody);
                    using (Pen tvPen = new Pen(Color.FromArgb(80, 50, 130), Math.Max(2f, 3f * scale)))
                        g.DrawPath(tvPen, tvBody);
                }

                int screenPad = (int)(22 * scale);
                Rectangle screenRect = new Rectangle(tvX + screenPad, tvY + screenPad, tvW - screenPad * 2, tvH - screenPad * 2 - (int)(20 * scale));
                int screenR = (int)(14 * scale);
                using (GraphicsPath screenPath = GetRoundedPath(screenRect, screenR))
                {
                    using (LinearGradientBrush screenGrad = new LinearGradientBrush(screenRect,
                        Color.FromArgb(55, 30, 95),
                        Color.FromArgb(85, 45, 140),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(screenGrad, screenPath);
                    }
                }

                int waveCenterX = screenRect.Left + screenRect.Width / 2;
                int waveCenterY = screenRect.Top + screenRect.Height / 2;
                float waveW1 = 3f * scale, waveW2 = 2.5f * scale, waveW3 = 2f * scale;
                using (Pen wavePen1 = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1.5f, waveW1)))
                using (Pen wavePen2 = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1.2f, waveW2)))
                using (Pen wavePen3 = new Pen(Color.FromArgb(70, 255, 255, 255), Math.Max(1f, waveW3)))
                {
                    int arc1 = (int)(80 * scale), arc2 = (int)(60 * scale), arc3 = (int)(40 * scale);
                    int ah1 = (int)(35 * scale), ah2 = (int)(26 * scale), ah3 = (int)(17 * scale);
                    g.DrawArc(wavePen3, waveCenterX - arc1 * 2, waveCenterY - ah1, arc1 * 4, ah1 * 2, 200, 140);
                    g.DrawArc(wavePen2, waveCenterX - arc2 * 2, waveCenterY - ah2, arc2 * 4, ah2 * 2, 200, 140);
                    g.DrawArc(wavePen1, waveCenterX - arc3 * 2, waveCenterY - ah3, arc3 * 4, ah3 * 2, 200, 140);
                }

                int playOffX = (int)(-12 * scale), playOffY = (int)(4 * scale);
                int pH = (int)(52 * scale);
                Point[] playTriangle = new Point[]
                {
                    new Point(waveCenterX + playOffX, waveCenterY - pH/2 + playOffY),
                    new Point(waveCenterX + playOffX, waveCenterY + pH/2 + playOffY),
                    new Point(waveCenterX + pH/2 + (int)(8*scale), waveCenterY + playOffY)
                };
                using (SolidBrush playBrush = new SolidBrush(Color.FromArgb(245, 245, 250)))
                    g.FillPolygon(playBrush, playTriangle);

                int standW = (int)(70 * scale), standH = (int)(20 * scale);
                int standX = (size - standW) / 2;
                int standY = tvY + tvH - (int)(5 * scale);
                using (GraphicsPath standPath = GetRoundedPath(new Rectangle(standX, standY, standW, standH), (int)(6 * scale)))
                using (SolidBrush standBrush = new SolidBrush(Color.FromArgb(235, 230, 245)))
                    g.FillPath(standBrush, standPath);

                int baseW = (int)(160 * scale), baseH = (int)(16 * scale);
                int baseX = (size - baseW) / 2;
                int baseY = standY + standH - (int)(4 * scale);
                using (GraphicsPath basePath = GetRoundedPath(new Rectangle(baseX, baseY, baseW, baseH), (int)(8 * scale)))
                using (SolidBrush baseBrush = new SolidBrush(Color.FromArgb(220, 215, 235)))
                    g.FillPath(baseBrush, basePath);

                float wtvFontSize = Math.Max(10f, 38f * scale);
                float proFontSize = Math.Max(6f, 14f * scale);
                using (Font wtvFont = new Font("Arial Black", wtvFontSize, FontStyle.Bold))
                {
                    string wtv = "WTV";
                    SizeF wtvSize = g.MeasureString(wtv, wtvFont);
                    using (SolidBrush wtvBrush = new SolidBrush(Color.White))
                        g.DrawString(wtv, wtvFont, wtvBrush, (size - wtvSize.Width) / 2, tvY + tvH + (int)(22 * scale));
                }
                using (Font proFont = new Font("Arial", proFontSize, FontStyle.Bold))
                {
                    string proLabel = "工具箱 PRO";
                    if (size < 64) proLabel = "PRO";
                    else if (size < 128) proLabel = "工具箱";
                    SizeF proSize = g.MeasureString(proLabel, proFont);
                    using (SolidBrush proBrush = new SolidBrush(Color.FromArgb(220, 210, 240)))
                        g.DrawString(proLabel, proFont, proBrush, (size - proSize.Width) / 2, tvY + tvH + (int)(72 * scale));
                }
            }
            return bmp;
        }

        private void SaveIcon(string path, List<Bitmap> bitmaps)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                ushort imageCount = (ushort)bitmaps.Count;
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write(imageCount);

                int offset = 6 + 16 * imageCount;
                List<byte[]> pngDatas = new List<byte[]>();
                foreach (Bitmap bmp in bitmaps)
                {
                    byte[] pngData;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        pngData = ms.ToArray();
                    }
                    pngDatas.Add(pngData);

                    bw.Write((byte)(bmp.Width >= 256 ? 0 : bmp.Width));
                    bw.Write((byte)(bmp.Height >= 256 ? 0 : bmp.Height));
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write((uint)pngData.Length);
                    bw.Write((uint)offset);
                    offset += pngData.Length;
                }
                foreach (var data in pngDatas)
                {
                    bw.Write(data);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(WndEnumProc lpEnumFunc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool WndEnumProc(IntPtr hWnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SafeGetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr(hWnd, nIndex);
            }
            else
            {
                return (IntPtr)GetWindowLong(hWnd, nIndex);
            }
        }

        private static IntPtr SafeSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }
            else
            {
                return (IntPtr)SetWindowLong(hWnd, nIndex, (int)dwNewLong);
            }
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // 手动居中弹窗（CenterParent 在无边框父窗口下失效）
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
            if (centerX < screen.X) centerX = screen.X;
            if (centerY < screen.Y) centerY = screen.Y;
            if (centerX + form.Width > screen.X + screen.Width) centerX = screen.X + screen.Width - form.Width;
            if (centerY + form.Height > screen.Y + screen.Height) centerY = screen.Y + screen.Height - form.Height;
            form.Location = new Point(centerX, centerY);
        }

        private static void SetFormDarkModeTitleBar(Form form, bool isDark)
        {
            if (form == null) return;
            int darkMode = isDark ? 1 : 0;
            try
            {
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref darkMode, 4);
            }
            catch { }
        }

        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_SETREDRAW = 0x000B;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;
        private const int HTCLIENT = 1;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;

        private IntPtr _mouseHook = IntPtr.Zero;
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseHookProc;
        private const int WH_MOUSE_LL = 14;
        private const int WM_RBUTTONUP = 0x0205;

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
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);


        [DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        private static extern int GetScrollInfo(IntPtr hWnd, int nBar, ref SCROLLINFO lpsi);

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
        private void StartMouseHook()
        {
            if (_mouseHook != IntPtr.Zero) return;
            _mouseHookProc = MouseHookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle(curModule.ModuleName), 0);
            }
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
            if (nCode >= 0 && wParam == (IntPtr)WM_RBUTTONUP)
            {
                POINT pt = (POINT)Marshal.PtrToStructure(lParam, typeof(POINT));
                IntPtr hWnd = WindowFromPoint(new Point(pt.x, pt.y));
                if (hWnd != IntPtr.Zero)
                {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    Process playerProc = null;
                    if (_runningPlayer != null && !_runningPlayer.HasExited && (uint)_runningPlayer.Id == pid)
                        playerProc = _runningPlayer;
                    else if (previewProcess != null && !previewProcess.HasExited && (uint)previewProcess.Id == pid)
                        playerProc = previewProcess;

                    if (playerProc != null)
                    {
                        this.BeginInvoke(new Action(() => ShowPlayerContextMenu(new Point(pt.x, pt.y))));
                    }
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private void ShowPlayerContextMenu(Point screenPoint)
        {
            try
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Font = GetFont(SF(9f));
                menu.Renderer = new RoundedMenuRenderer(IsDarkColor(theme.Bg));
                menu.BackColor = Color.Transparent;
                menu.ForeColor = theme.TextPrimary;

                var showInfoItem = new ToolStripMenuItem("显示流媒体信息");
                showInfoItem.Checked = _showStreamInfoOverlay;
                showInfoItem.Click += (s, e) =>
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
                menu.Items.Add(showInfoItem);

                menu.Show(screenPoint);
            }
            catch { }
        }

        private string _currentCodec = "";
        private string _currentResolution = "";
        private string _currentFps = "";
        private string _currentBitrate = "";
        private string _currentChannelName = "";
        private string _currentAudioChannels = "";
        private string _currentAudioSampleRate = "";
        private string _currentDelay = "";
        private string _currentFrameCount = "";
        private string _currentTime = "";
        private string _currentSpeed = "";
        private string _currentBuffer = "";
        private string _currentSar = "";
        private string _currentDar = "";
        private string _currentAudioBitdepth = "";
        private string _currentSize = "";
        private string _currentPixFmt = "";
        private string _currentLevel = "";
        private string _currentColorSpace = "";
        private string _currentColorRange = "";
        private string _currentColorPrimaries = "";
        private string _currentColorTransfer = "";
        private string _currentFormat = "";
        private string _currentDuration = "";
        private int _droppedFrames = 0;
        private int _totalFrames = 0;
        private string _currentDecodedFrames = "";
        private string _currentDisplayedFrames = "";
        private System.Threading.CancellationTokenSource _ffplayOutputCts;
        private bool _showStreamInfoOverlay = false;
        private System.Windows.Forms.Timer _streamInfoOverlayTimer;
        private Form _streamInfoOverlayForm;
        private Label _streamInfoLabel;

        private void StartStreamInfoOverlay()
        {
            try
            {
                StopStreamInfoOverlay();

                _streamInfoOverlayForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    TopMost = true,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Color.Black,
                    TransparencyKey = Color.Black,
                    Visible = false
                };

                _streamInfoLabel = new Label
                {
                    Font = GetFont(SF(7f)),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(180, 0, 0, 0),
                    AutoSize = true,
                    Padding = new Padding(6),
                    TextAlign = ContentAlignment.TopLeft,
                    UseCompatibleTextRendering = true,
                    Location = new Point(0, 0)
                };

                _streamInfoOverlayForm.Controls.Add(_streamInfoLabel);
                _streamInfoOverlayForm.Show();

                _streamInfoOverlayForm.KeyPreview = true;
                _streamInfoOverlayForm.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        _showStreamInfoOverlay = false;
                        StopStreamInfoOverlay();
                    }
                };

                _streamInfoOverlayTimer = new System.Windows.Forms.Timer { Interval = 100 };
                _streamInfoOverlayTimer.Tick += (s, e) => UpdateStreamInfoOverlay();
                _streamInfoOverlayTimer.Start();
            }
            catch { }
        }

        private void StopStreamInfoOverlay()
        {
            try
            {
                if (_streamInfoOverlayTimer != null)
                {
                    _streamInfoOverlayTimer.Stop();
                    _streamInfoOverlayTimer.Dispose();
                    _streamInfoOverlayTimer = null;
                }
            }
            catch { }

            try
            {
                if (_streamInfoOverlayForm != null && !_streamInfoOverlayForm.IsDisposed)
                {
                    _streamInfoOverlayForm.Close();
                    _streamInfoOverlayForm.Dispose();
                    _streamInfoOverlayForm = null;
                }
            }
            catch { }
        }

        private void UpdateStreamInfoOverlay()
        {
            try
            {
                if (!_showStreamInfoOverlay)
                {
                    StopStreamInfoOverlay();
                    return;
                }

                if (_streamInfoOverlayForm == null || _streamInfoOverlayForm.IsDisposed || _streamInfoLabel == null || _streamInfoLabel.IsDisposed)
                {
                    StopStreamInfoOverlay();
                    return;
                }

                IntPtr targetHwnd = FindPlayerWindow();
                if (targetHwnd == IntPtr.Zero)
                {
                    _streamInfoOverlayForm.Visible = false;
                    return;
                }

                RECT rect;
                if (!GetWindowRect(targetHwnd, out rect))
                {
                    _streamInfoOverlayForm.Visible = false;
                    return;
                }

                StringBuilder info = new StringBuilder();
                if (!string.IsNullOrEmpty(_currentChannelName)) info.Append($"名称: {_currentChannelName}\n");
                if (!string.IsNullOrEmpty(_currentFormat)) info.Append($"格式: {_currentFormat}\n");
                if (!string.IsNullOrEmpty(_currentCodec)) info.Append($"编码: {_currentCodec}\n");
                if (!string.IsNullOrEmpty(_currentResolution)) info.Append($"分辨率: {_currentResolution}\n");
                if (!string.IsNullOrEmpty(_currentSar)) info.Append($"SAR: {_currentSar}\n");
                if (!string.IsNullOrEmpty(_currentDar)) info.Append($"DAR: {_currentDar}\n");
                if (!string.IsNullOrEmpty(_currentFps)) info.Append($"帧率: {_currentFps}\n");
                if (!string.IsNullOrEmpty(_currentPixFmt)) info.Append($"像素格式: {_currentPixFmt}\n");
                if (!string.IsNullOrEmpty(_currentLevel)) info.Append($"级别: {_currentLevel}\n");
                if (!string.IsNullOrEmpty(_currentColorSpace)) info.Append($"色彩空间: {_currentColorSpace}\n");
                if (!string.IsNullOrEmpty(_currentColorPrimaries)) info.Append($"色基: {_currentColorPrimaries}\n");
                if (!string.IsNullOrEmpty(_currentColorTransfer)) info.Append($"传递函数: {_currentColorTransfer}\n");
                if (!string.IsNullOrEmpty(_currentAudioChannels)) info.Append($"声道: {_currentAudioChannels}\n");
                if (!string.IsNullOrEmpty(_currentAudioSampleRate)) info.Append($"采样率: {_currentAudioSampleRate}\n");
                if (!string.IsNullOrEmpty(_currentAudioBitdepth)) info.Append($"位深: {_currentAudioBitdepth}\n");
                if (!string.IsNullOrEmpty(_currentBitrate)) info.Append($"码率: {_currentBitrate}\n");
                if (!string.IsNullOrEmpty(_currentDuration)) info.Append($"时长: {_currentDuration}\n");
                if (!string.IsNullOrEmpty(_currentDelay)) info.Append($"延时: {_currentDelay}\n");
                if (!string.IsNullOrEmpty(_currentTime)) info.Append($"时间: {_currentTime}\n");
                if (!string.IsNullOrEmpty(_currentSpeed)) info.Append($"速度: {_currentSpeed}\n");
                if (!string.IsNullOrEmpty(_currentFrameCount)) info.Append($"帧计数: {_currentFrameCount}\n");
                if (!string.IsNullOrEmpty(_currentDecodedFrames)) info.Append($"{_currentDecodedFrames}\n");
                if (!string.IsNullOrEmpty(_currentDisplayedFrames)) info.Append($"{_currentDisplayedFrames}\n");
                if (!string.IsNullOrEmpty(_currentBuffer)) info.Append($"缓冲: {_currentBuffer}\n");
                if (_droppedFrames > 0) info.Append($"丢帧: {_droppedFrames}/{_totalFrames}");

                _streamInfoLabel.Text = info.ToString();
                _streamInfoLabel.Location = new Point(0, 0);

                _streamInfoOverlayForm.Location = new Point(rect.Left + 26, rect.Top + 60);
                _streamInfoOverlayForm.Size = _streamInfoLabel.Size;
                _streamInfoOverlayForm.Visible = true;
            }
            catch
            {
                StopStreamInfoOverlay();
            }
        }

        private IntPtr FindPlayerWindow()
        {
            IntPtr targetHwnd = IntPtr.Zero;

            if (_runningPlayer != null && !_runningPlayer.HasExited)
            {
                uint targetPid = (uint)_runningPlayer.Id;
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pid != targetPid) return true;
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);
                    string clsName = className.ToString().ToLower();
                    StringBuilder windowTitle = new StringBuilder(512);
                    GetWindowText(hWnd, windowTitle, windowTitle.Capacity);
                    string title = windowTitle.ToString().ToLower();
                    if (clsName.Contains("sdl") || clsName.Contains("ffplay") || 
                        clsName.Contains("potplayer") || clsName.Contains("vlc") || 
                        clsName.Contains("mpv") || clsName.Contains("wxwidgets") ||
                        title.Contains("potplayer") || title.Contains("vlc") || title.Contains("mpv"))
                    {
                        targetHwnd = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            else if (previewProcess != null && !previewProcess.HasExited)
            {
                uint targetPid = (uint)previewProcess.Id;
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pid != targetPid) return true;
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);
                    string clsName = className.ToString().ToLower();
                    StringBuilder windowTitle = new StringBuilder(512);
                    GetWindowText(hWnd, windowTitle, windowTitle.Capacity);
                    string title = windowTitle.ToString().ToLower();
                    if (clsName.Contains("sdl") || clsName.Contains("ffplay") || 
                        clsName.Contains("potplayer") || clsName.Contains("vlc") || 
                        clsName.Contains("mpv") || clsName.Contains("wxwidgets") ||
                        title.Contains("potplayer") || title.Contains("vlc") || title.Contains("mpv"))
                    {
                        targetHwnd = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            return targetHwnd;
        }

        /// <summary>
        /// 构建主界面UI（完整的界面初始化入口）
        /// 包含窗口自适应、主题初始化、DPI缩放配置、整体布局结构搭建
        /// 布局结构：外层边框容器 → 标题栏(Dock=Top) → 主内容区(Dock=Fill)
        /// 主内容区：左侧导航栏 → 中间操作区 → 右侧数据表格区
        /// </summary>
        private void BuildUI()
        {
            // ========== 主题初始化 ==========
            if (themePreference == "深色") theme = AppTheme.Dark;
            else if (themePreference == "跟随系统") theme = AppTheme.GetAutoTheme();
            else theme = AppTheme.Light;

            // ========== 窗口基础配置 ==========
            this.Text = "";                                    // 清空默认标题（使用自定义标题栏）
            this.AutoScaleMode = AutoScaleMode.None;           // 禁用系统自动缩放（使用自定义DPI缩放）
            // 获取当前DPI缩放比例（96dpi为100%缩放基准）
            using (Graphics g = this.CreateGraphics())
                dpiScale = g.DpiX / 96f;
            config.Initialize(dpiScale);
            DarkMessageBox.DpiScale = dpiScale;

            // ========== 窗口大小自适应（根据屏幕分辨率） ==========
            // 窗口大小 = 屏幕工作区的88%，最小1280x800，确保在不同分辨率下都能完整显示
            int screenW = Screen.PrimaryScreen.WorkingArea.Width;
            int screenH = Screen.PrimaryScreen.WorkingArea.Height;
            int winW = Math.Max(1280, (int)(screenW * 0.88)); // 窗口宽度
            int winH = Math.Max(800, (int)(screenH * 0.88));  // 窗口高度
            this.Size = new Size(winW, winH);
            this.StartPosition = FormStartPosition.Manual;     // 手动定位（居中显示）
            this.Location = new Point(
                (screenW - winW) / 2,                          // 水平居中
                (screenH - winH) / 2                           // 垂直居中
            );
            this.Font = GetFont(SF(11f));                      // 全局字体（YaHei 11pt * DPI缩放）
            this.MinimumSize = new Size(SX(900), SY(600));     // 窗口最小尺寸（900x600 * DPI缩放）
            this.BackColor = theme.Border;                     // 窗口背景色（边框色）

            // ========== 外层边框容器 ==========
            // [作用] 实现细边框效果，padding=1px使内容与窗口边缘有1px间隔
            outerWrap = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.Border,
                Padding = new Padding(1)
            };
            this.Controls.Add(outerWrap);

            // ========== 自定义标题栏 ==========
            CreateTitleBar();

            // ================================================================
            //  WinForms Dock布局规则: Dock=Left时,最后Add的控件在最左边
            //  目标顺序(从左到右): navPanel → navSep → actionArea → actionSep → mainArea(Fill)
            // ================================================================

            // ========== 右侧主数据区(Dock=Fill,最先Add) ==========
            // [位置] 填充剩余空间 [背景] 主题次要背景色 [功能] 包含搜索栏和数据表格
            mainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.BgAlt
            };
            mainArea.Resize += (s, e) =>
            {
                UpdateScrollBarTheme(mainArea); // 窗口大小变化时更新滚动条主题
            };

            // ========== 数据表格容器(Dock=Fill,在mainArea内最先Add) ==========
            // [位置] 填充mainArea剩余空间 [背景] 主题次要背景色 [功能] 包含DataGridView和空状态面板
            gridContainerRef = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.BgAlt
            };

            // ========== 数据表格(DataGridView) ==========
            // [位置] 填充gridContainerRef [字体] YaHei 6.7pt * DPI缩放 [行高] 36px * DPI缩放
            // [功能] 显示频道列表，支持双击播放、编辑名称、排序、批量操作
            dgvData = new DataGridView();
            dgvData.Dock = DockStyle.Fill;
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // 列自动填充
            dgvData.BackgroundColor = theme.BgAlt;                            // 背景色
            dgvData.RowHeadersVisible = false;                                 // 隐藏行头
            dgvData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;   // 整行选择
            dgvData.ReadOnly = false;                                          // 允许编辑
            dgvData.AllowUserToAddRows = false;                                // 禁止用户添加行
            dgvData.AllowUserToDeleteRows = false;                             // 禁止用户删除行
            dgvData.AllowUserToResizeColumns = false;                          // 禁止用户调整列宽
            dgvData.AllowUserToResizeRows = false;                             // 禁止用户调整行高
            dgvData.AllowUserToOrderColumns = false;                           // 禁止用户排序列
            dgvData.EditMode = DataGridViewEditMode.EditOnF2;                  // 按F2编辑
            dgvData.Font = GetFont(SF(6.7f));                                  // 单元格字体（6.7pt * DPI缩放）
            dgvData.RowTemplate.Height = SY(42);                               // 行高（42px * DPI缩放）
            // 绑定事件
            dgvData.CellDoubleClick += DgvData_CellDoubleClick;                // 双击播放
            dgvData.CellEndEdit += DgvData_CellEndEdit;                        // 编辑结束
            // 快捷键：Ctrl+A全选，Ctrl+Shift+C复制所有链接
            dgvData.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    SelectAllRows();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.Shift && e.KeyCode == Keys.C)
                {
                    CopyAllLinks();
                    e.SuppressKeyPress = true;
                }
            };
            // 样式配置
            dgvData.EnableHeadersVisualStyles = false;                         // 禁用系统默认表头样式
            dgvData.GridColor = theme.Border;                                  // 网格线颜色
            dgvData.BorderStyle = BorderStyle.None;                            // 无边框
            dgvData.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical; // 仅显示垂直分隔线
            dgvData.ColumnHeadersVisible = true;                               // 显示表头
            dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; // 固定表头高度
            dgvData.ColumnHeadersHeight = SY(36);                              // 表头高度（36px * DPI缩放）
            dgvData.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; // 表头无边框
            dgvData.DefaultCellStyle.SelectionBackColor = theme.SelectRow;    // 选中行背景色
            dgvData.DefaultCellStyle.SelectionForeColor = theme.SelectRowText; // 选中行文字色
            dgvData.RowTemplate.Height = SY(36);                               // 行高（36px * DPI缩放）

            // ========== 表头样式 ==========
            // [背景] 主题表头背景色 [文字] 次要文字色 [字体] YaHei 9pt * DPI缩放 [对齐] 左对齐
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.BackColor = theme.HeaderBg;        // 表头背景色
            headerStyle.ForeColor = theme.TextSecondary;   // 表头文字色
            headerStyle.Font = GetFont(SF(9f));            // 表头字体（9pt * DPI缩放）
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft; // 左对齐
            headerStyle.Padding = new Padding(SX(10), 0, 0, 0); // 左内边距（10px * DPI缩放）
            headerStyle.SelectionBackColor = theme.HeaderBg;    // 选中时背景色不变
            headerStyle.SelectionForeColor = theme.TextSecondary; // 选中时文字色不变
            dgvData.ColumnHeadersDefaultCellStyle = headerStyle;

            // ========== 行样式 ==========
            // [背景] 主题表面色 [文字] 主文字色 [字体] YaHei 6.7pt * DPI缩放 [内边距] 左10px右6px
            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
            rowStyle.BackColor = theme.Surface;           // 行背景色
            rowStyle.ForeColor = theme.TextPrimary;       // 行文字色
            rowStyle.SelectionBackColor = theme.SelectRow; // 选中行背景色
            rowStyle.SelectionForeColor = theme.SelectRowText; // 选中行文字色
            rowStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0)); // 内边距（左10px,右6px * DPI缩放）
            rowStyle.Font = GetFont(SF(6.7f));            // 行字体（6.7pt * DPI缩放）
            dgvData.RowsDefaultCellStyle = rowStyle;
            // 交替行样式（与普通行相同，无斑马纹效果）
            DataGridViewCellStyle altStyle = new DataGridViewCellStyle();
            altStyle.BackColor = theme.Surface;
            altStyle.ForeColor = theme.TextPrimary;
            altStyle.SelectionBackColor = theme.SelectRow;
            altStyle.SelectionForeColor = theme.SelectRowText;
            altStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0));
            altStyle.Font = GetFont(SF(6.7f));
            dgvData.AlternatingRowsDefaultCellStyle = altStyle;

            // ========== 列定义（8列） ==========
            // [名称] 可编辑 [链接] 只读/Consolas字体 [归属地] 只读 [分辨率] 只读
            // [响应速度] 药丸样式/只读 [分组] 只读 [状态] 药丸样式/只读 [操作] 自绘双按钮/只读
            dgvData.Columns.Add("colName", "名称");       // 频道名称（可编辑）
            dgvData.Columns.Add("colUrl", "链接");        // 播放链接（只读，Consolas字体）
            dgvData.Columns.Add("colLocation", "归属地"); // 归属地信息
            dgvData.Columns.Add("colResolution", "分辨率"); // 视频分辨率
            dgvData.Columns.Add("colSpeed", "响应速度");   // 响应速度（药丸样式）
            dgvData.Columns.Add("colGroup", "分组");      // 分组信息
            dgvData.Columns.Add("colStatus", "状态");      // 状态（药丸样式）
            dgvData.Columns.Add("colAction", "操作");      // 操作列（自绘播放/复制按钮）

            // ========== 表格事件绑定 ==========
            dgvData.CellClick += DgvData_CellClick;                          // 单元格点击（操作按钮）
            dgvData.ColumnHeaderMouseClick += DgvData_ColumnHeaderMouseClick; // 表头点击（排序）
            dgvData.CellPainting += DgvData_CellPainting;                    // 单元格自绘（药丸、按钮）
            dgvData.CellFormatting += DgvData_CellFormatting;                // 单元格格式化
            dgvData.CellMouseMove += DgvData_CellMouseMove;                  // 鼠标移动（悬停按钮）
            dgvData.CellMouseDown += DgvData_CellMouseDown;                  // 鼠标按下
            dgvData.CellMouseUp += DgvData_CellMouseUp;                      // 鼠标释放
            dgvData.ShowCellToolTips = true;                                 // 显示单元格工具提示
            // 工具提示：当单元格内容过长时显示完整内容
            dgvData.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    string colName = dgvData.Columns[e.ColumnIndex].Name;
                    if (colName == "colName" || colName == "colUrl" || colName == "colLocation" || colName == "colResolution" || colName == "colGroup")
                    {
                        var val = dgvData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(val)) e.ToolTipText = val;
                    }
                }
            };
            // 鼠标离开表格时重置悬停状态
            dgvData.MouseLeave += (s, e) => { if (_hoverRow != -1) { _hoverRow = -1; _hoverBtn = -1; dgvData.Invalidate(); } };

            // ========== 列宽权重配置（FillWeight） ==========
            // 权重总和 = 323，链接列占最大比重（160/323 ≈ 50%）
            dgvData.Columns["colName"].FillWeight = 15;       // 名称列权重
            dgvData.Columns["colUrl"].FillWeight = 160;      // 链接列权重（最宽）
            dgvData.Columns["colLocation"].FillWeight = 35;   // 归属地列权重
            dgvData.Columns["colResolution"].FillWeight = 22; // 分辨率列权重
            dgvData.Columns["colSpeed"].FillWeight = 25;      // 响应速度列权重
            dgvData.Columns["colGroup"].FillWeight = 18;      // 分组列权重
            dgvData.Columns["colStatus"].FillWeight = 20;     // 状态列权重
            dgvData.Columns["colAction"].FillWeight = 28;     // 操作列权重

            // ========== 列最小宽度配置（确保内容完整显示） ==========
            // [链接列] 最小600px（防止URL被截断）[操作列] 最小20px（双按钮空间）
            dgvData.Columns["colName"].MinimumWidth = SX(12);       // 名称列最小宽度
            dgvData.Columns["colUrl"].MinimumWidth = SX(600);      // 链接列最小宽度（确保URL完整）
            dgvData.Columns["colLocation"].MinimumWidth = SX(20);   // 归属地列最小宽度
            dgvData.Columns["colResolution"].MinimumWidth = SX(15); // 分辨率列最小宽度
            dgvData.Columns["colSpeed"].MinimumWidth = SX(20);      // 响应速度列最小宽度
            dgvData.Columns["colGroup"].MinimumWidth = SX(18);      // 分组列最小宽度
            dgvData.Columns["colStatus"].MinimumWidth = SX(18);     // 状态列最小宽度
            dgvData.Columns["colAction"].MinimumWidth = SX(20);     // 操作列最小宽度（双按钮）

            // ========== 列对齐方式配置 ==========
            // [默认] 居中对齐 [名称/链接/归属地/分辨率/分组] 左对齐 [响应速度/状态/操作] 居中对齐
            foreach (DataGridViewColumn col in dgvData.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Programmatic; // 编程式排序
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; // 默认居中
            }
            // 文本列左对齐
            dgvData.Columns["colName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colUrl"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colLocation"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colResolution"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colGroup"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            // 药丸（响应速度、状态）和按钮（操作）保持居中
            dgvData.Columns["colSpeed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns["colStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns["colAction"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // ========== 列只读配置 ==========
            // [名称] 可编辑（允许用户修改频道名称）[其他] 只读（数据由程序生成）
            dgvData.Columns["colName"].ReadOnly = false;          // 名称列可编辑
            dgvData.Columns["colUrl"].ReadOnly = true;            // 链接列只读
            dgvData.Columns["colUrl"].DefaultCellStyle.Font = new Font("Consolas", SF(6.7f)); // 链接列使用等宽字体
            dgvData.Columns["colLocation"].ReadOnly = true;       // 归属地列只读
            dgvData.Columns["colResolution"].ReadOnly = true;     // 分辨率列只读
            dgvData.Columns["colSpeed"].ReadOnly = true;          // 响应速度列只读
            dgvData.Columns["colGroup"].ReadOnly = true;          // 分组列只读
            dgvData.Columns["colStatus"].ReadOnly = true;         // 状态列只读
            dgvData.Columns["colAction"].ReadOnly = true;         // 操作列只读（自绘按钮）

            // 默认按名称升序排序
            sortedColumn = "colName";
            sortDirection = SortOrder.Ascending;

            gridContainerRef.Controls.Add(dgvData);

            // ========== 空状态面板（无数据时显示） ==========
            // [位置] 居中显示 [大小] 140x110px * DPI缩放 [内容] 图标 + "无效站"提示文字
            emptyStatePanel = new Panel
            {
                BackColor = Color.Transparent,  // 透明背景
                Size = new Size(SX(140), SY(110)) // 面板大小（140x110px * DPI缩放）
            };

            // 空状态图标（自定义绘制电视关闭图标）
            PictureBox emptyIconBox = new PictureBox
            {
                Size = new Size(SX(56), SY(56)),      // 图标大小（56x56px * DPI缩放）
                Location = new Point(SX(42), SY(0)),   // 图标位置（水平居中）
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            emptyIconBox.Paint += EmptyIcon_Paint; // 绑定自定义绘制事件

            // 空状态文字提示
            emptyLabel = new Label
            {
                Text = "无效站",                        // 提示文字
                Font = GetFont(SF(11f)),               // 字体（11pt * DPI缩放）
                ForeColor = Color.FromArgb(180, 180, 180), // 灰色文字
                AutoSize = true,                        // 自动调整大小
                TextAlign = ContentAlignment.MiddleCenter // 居中对齐
            };

            emptyStatePanel.Controls.Add(emptyIconBox);
            emptyStatePanel.Controls.Add(emptyLabel);
            gridContainerRef.Controls.Add(emptyStatePanel);
            emptyStatePanel.BringToFront(); // 置于最上层

            // 容器大小变化时重新居中空状态面板
            gridContainerRef.Resize += (s, e) => CenterEmptyState();

            // ========== 搜索栏(Dock=Top,在gridContainer上方Add) ==========
            // [位置] 顶部停靠 [高度] 38px * DPI缩放 [背景] 主题次要背景色
            // [内容] 搜索标签 + 搜索框 + 分组筛选标签 + 分组下拉框
            searchPanelRef = new Panel
            {
                Dock = DockStyle.Top,           // 顶部停靠
                Height = SY(38),                // 高度（38px * DPI缩放）
                BackColor = theme.BgAlt         // 背景色
            };

            // 搜索标签（"搜 索 :"）
            Label lblSearch = new Label
            {
                Text = "搜 索 :",
                Font = GetFont(SF(8.5f)),     // 字体（8.5pt * DPI缩放）
                ForeColor = theme.TextPrimary,
                Location = new Point(0, SY(0)),
                AutoSize = true
            };
            lblSearch.Height = SY(26);
            lblSearch.Top = (SY(38) - SY(26)) / 2; // 垂直居中
            searchPanelRef.Controls.Add(lblSearch);

            // ========== 分组筛选下拉框（右侧） ==========
            // [容器] 110x26px * DPI缩放 [控件] DarkComboBox自绘圆角
            cboGroupHost = new Panel
            {
                BackColor = theme.BgAlt,
                Visible = false,               // 默认隐藏（有分组时显示）
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(0, (SY(38) - SY(26)) / 2), // 垂直居中
                Size = new Size(110, SY(26))   // 容器大小（110x26px）
            };
            // 自定义深色下拉框（圆角6px）
            DarkComboBox darkCbo = new DarkComboBox
            {
                Font = GetFont(SF(8.5f)),               // 字体（8.5pt * DPI缩放）
                Dock = DockStyle.Fill,
                BackColor = theme.Surface,              // 背景色
                ForeColor = theme.TextPrimary,          // 文字色
                BorderColor = theme.Border,             // 边框色
                FocusBorderColor = theme.Primary,       // 聚焦时边框色
                ItemBackColor = theme.Surface,          // 选项背景色
                ItemSelectedBackColor = theme.BgAlt,    // 选中选项背景色
                ItemHoverBackColor = Color.FromArgb(Math.Min(255, theme.Surface.R + 10), Math.Min(255, theme.Surface.G + 10), Math.Min(255, theme.Surface.B + 10)), // 悬停选项背景色
                CornerRadius = 6,                       // 圆角半径（6px）
                ItemHeight = SY(22)                     // 选项高度（22px * DPI缩放）
            };
            cboGroup = darkCbo;
            cboGroup.Items.Add("全部");                // 默认选项
            cboGroup.SelectedIndex = 0;                // 默认选中"全部"
            cboGroup.SelectedIndexChanged += CboGroup_SelectedIndexChanged; // 绑定分组筛选事件
            cboGroupHost.Controls.Add(cboGroup);
            searchPanelRef.Controls.Add(cboGroupHost);

            // 分组筛选标签（"分组:"）
            Label lblGroup = new Label
            {
                Text = "分组:",
                Font = GetFont(SF(8.5f)),     // 字体（8.5pt * DPI缩放）
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Visible = false,               // 默认隐藏（有分组时显示）
                TextAlign = ContentAlignment.MiddleRight
            };
            lblGroup.Height = SY(26);
            lblGroup.Top = (SY(38) - SY(26)) / 2; // 垂直居中
            lblGroupFilter = lblGroup;
            searchPanelRef.Controls.Add(lblGroup);

            // ========== 搜索框圆角容器 ==========
            // [位置] (98, 垂直居中) [大小] 40x26px [背景] 主题表面色
            // [内部] TextBox（无边框）+ 圆角边框自绘
            Panel searchBoxHost = new Panel
            {
                Location = new Point(98, (SY(38) - SY(26)) / 2), // 位于搜索标签右侧
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Size = new Size(40, SY(26)),                     // 初始大小（40x26px）
                BackColor = theme.Surface                        // 背景色
            };
            searchBoxHostRef = searchBoxHost;
            searchPanelRef.Controls.Add(searchBoxHost);

            // 搜索输入框（无边框，嵌入圆角容器内）
            TextBox txtSearch = new TextBox
            {
                Font = GetFont(SF(8f)),              // 字体（8pt * DPI缩放）
                BorderStyle = BorderStyle.None,       // 无边框（由容器绘制圆角边框）
                Location = new Point(18, SY(2)),      // 内部偏移（左18px,上2px）
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, // 左右自适应
                Width = searchBoxHost.Width - 20,    // 宽度（容器宽度-20px）
                Height = SY(18),                     // 高度（18px * DPI缩放）
                Text = "输入搜索内容，按下回车键搜索", // 占位提示文字
                ForeColor = theme.TextSecondary,     // 占位文字颜色（次要文字色）
                BackColor = theme.Surface            // 背景色
            };
            txtSearchBox = txtSearch;
            searchBoxHost.Controls.Add(txtSearch);

            // 移除下拉框的系统默认主题样式（使其使用自定义绘制）
            cboGroup.HandleCreated += (s, e) =>
            {
                SetWindowTheme(cboGroup.Handle, "", "");
            };

            // ========== 搜索框焦点处理与圆角边框绘制 ==========
            bool searchFocus = false; // 搜索框聚焦状态标志

            // 搜索框获取焦点
            txtSearch.GotFocus += (s, e) =>
            {
                searchFocus = true;
                searchBoxHost.Invalidate(); // 触发重绘（更新边框颜色）
                // 清空占位提示文字，改为主文字色
                if (txtSearch.Text == "输入搜索内容，按下回车键搜索")
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = theme.TextPrimary;
                }
            };

            // 搜索框失去焦点
            txtSearch.LostFocus += (s, e) =>
            {
                searchFocus = false;
                searchBoxHost.Invalidate(); // 触发重绘（恢复边框颜色）
                // 恢复占位提示文字，改为次要文字色
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = "输入搜索内容，按下回车键搜索";
                    txtSearch.ForeColor = theme.TextSecondary;
                }
            };

            // 搜索框圆角边框绘制（聚焦时边框变粗变亮）
            searchBoxHost.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // 抗锯齿
                Rectangle rect = new Rectangle(0, 0, searchBoxHost.Width - 1, searchBoxHost.Height - 1);
                // 聚焦时使用主题主色（边框1.5px），未聚焦时使用边框色（边框1px）
                Color bc = searchFocus ? theme.Primary : theme.Border;
                using (GraphicsPath sp = GetRoundedPath(rect, 6)) // 圆角半径6px
                {
                    using (SolidBrush br = new SolidBrush(theme.Surface))
                        e.Graphics.FillPath(br, sp); // 填充背景
                    using (Pen pen = new Pen(bc, searchFocus ? 1.5f : 1f))
                        e.Graphics.DrawPath(pen, sp); // 绘制边框
                }
            };

            // 更新搜索框Region（已废弃，圆角效果通过Paint事件实现）
            void UpdateSearchBoxRegion()
            {
                // 不再使用Region裁剪，避免裁掉子控件TextBox
                // 圆角效果通过Paint事件绘制边框实现
            }
            UpdateSearchBoxRegion();

            // 更新分组下拉框Region（圆角裁剪）
            void UpdateCboGroupRegion()
            {
                if (cboGroupHost.Width > 0 && cboGroupHost.Height > 0)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, cboGroupHost.Width - 1, cboGroupHost.Height - 1), 6))
                    {
                        cboGroupHost.Region = new Region(path); // 裁剪为圆角矩形
                    }
                }
            }
            UpdateCboGroupRegion();

            searchPanelRef.Resize += (s, e) =>
            {
                int rightAreaWidth = cboGroupHost.Visible ? 328 : 15;
                int leftMargin = 98;
                searchBoxHost.Left = leftMargin;
                searchBoxHost.Width = Math.Max(100, searchPanelRef.ClientSize.Width - leftMargin - rightAreaWidth);
                txtSearch.Width = searchBoxHost.Width - 20;
                if (cboGroupHost.Visible)
                {
                    lblGroup.Left = searchPanelRef.ClientSize.Width - 298;
                    cboGroupHost.Left = searchPanelRef.ClientSize.Width - 158;
                    cboGroupHost.Width = 130;
                    cboGroupHost.Top = searchBoxHost.Top;
                }
                UpdateSearchBoxRegion();
                UpdateCboGroupRegion();
                searchBoxHost.Invalidate();
                cboGroupHost.Invalidate();
            };

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchChannels(txtSearch.Text);
                    e.SuppressKeyPress = true;
                }
            };

            ContextMenuStrip txtMenu = new ContextMenuStrip
            {
                Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg))),
                Font = GetFont(SF(10f)),
                ShowImageMargin = true,
                BackColor = theme.Surface
            };
            ToolStripMenuItem miCut = new ToolStripMenuItem("剪切", null, (s, e) => txtSearch.Cut()) { ShortcutKeyDisplayString = "Ctrl+X" };
            ToolStripMenuItem miCopy = new ToolStripMenuItem("复制", null, (s, e) => txtSearch.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" };
            ToolStripMenuItem miPaste = new ToolStripMenuItem("粘贴", null, (s, e) =>
            {
                if (Clipboard.ContainsText())
                {
                    if (txtSearch.Text == "输入搜索内容，按下回车键搜索")
                    {
                        txtSearch.Text = "";
                        txtSearch.ForeColor = theme.TextPrimary;
                    }
                    txtSearch.Paste();
                }
            })
            { ShortcutKeyDisplayString = "Ctrl+V" };
            ToolStripMenuItem miClear = new ToolStripMenuItem("清空", null, (s, e) =>
            {
                txtSearch.Clear();
                txtSearch.ForeColor = theme.TextSecondary;
            });
            txtMenu.Items.AddRange(new ToolStripItem[] { miCut, miCopy, miPaste, new ToolStripSeparator(), miClear });
            txtMenu.Opening += (s, e) =>
            {
                bool hasSel = txtSearch.SelectionLength > 0;
                bool canRead = !string.IsNullOrEmpty(txtSearch.Text) && txtSearch.Text != "输入搜索内容，按下回车键搜索";
                miCut.Enabled = hasSel && !txtSearch.ReadOnly;
                miCopy.Enabled = hasSel;
                miPaste.Enabled = Clipboard.ContainsText() && !txtSearch.ReadOnly;
                miClear.Enabled = canRead;
            };
            txtSearch.ContextMenuStrip = txtMenu;

            // 搜索栏底部分隔线
            Panel searchSep = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ColorBorder
            };
            searchPanelRef.Controls.Add(searchSep);

            // ---- 状态栏(Dock=Top,在searchPanelRef上方Add) 药丸形状 ----
            statusBarContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = SY(32),
                BackColor = theme.Bg,
                Padding = new Padding(SX(12), SY(4), SX(12), SY(4))
            };

            statusBarRef = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 210)
            };
            statusBarRef.Paint += (s, e) =>
            {
                if (progressBarWidth > 0)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 180, 80)))
                    {
                        e.Graphics.FillRectangle(brush, 0, 0, progressBarWidth, statusBarRef.ClientSize.Height);
                    }
                }
            };

            progressBarWidth = 0;

            lblDetected = new Label
            {
                Text = "已检测: 0/0",
                Font = GetFont(SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblDetected);

            lblAvailable = new Label
            {
                Text = "可用: 0",
                Font = GetFont(SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblAvailable);

            lblProgressText = new Label
            {
                Text = "检测进度:",
                Font = GetFont(SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblProgressText);

            lblPercent = new Label
            {
                Text = "0.00%",
                Font = GetFont(SF(10.5f), FontStyle.Bold),
                ForeColor = theme.Primary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblPercent);

            lblStreamInfo = new Label
            {
                Text = "",
                Font = GetFont(SF(8.5f)),
                ForeColor = theme.TextSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Visible = false
            };
            statusBarRef.Controls.Add(lblStreamInfo);

            statusBarRef.Resize += (s, e) =>
            {
                LayoutStatusBar(statusBarRef);
                UpdateStatusBarRegion();
            };
            statusBarContainer.Controls.Add(statusBarRef);
            LayoutStatusBar(statusBarRef);

            // 按Dock顺序添加到mainArea(从下到上:gridContainer -> statusBarContainer -> searchPanel)
            mainArea.Controls.Add(gridContainerRef);
            mainArea.Controls.Add(statusBarContainer);
            mainArea.Controls.Add(searchPanelRef);

            // ==================== 中间:操作按钮区(Dock=Left,第二个Add) ====================
            actionArea = new Panel
            {
                Dock = DockStyle.Left,
                Width = SX(150),
                BackColor = theme.BgAlt,
                AutoScroll = true
            };

            int ay = SY(14);
            int btnW = SX(126);
            int leftX = SX(12);

            // 1. 选择m3u/txt按钮
            Button btnSelectFile = new Button
            {
                Text = "📄选择m3u/txt",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(32)),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Surface,
                ForeColor = theme.Primary,
                Font = GetFont(SF(8.5f)),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft
            };
            btnSelectFile.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int iconX = 16;
                int iconY = (btnSelectFile.Height - 12) / 2;
                using (Pen pen = new Pen(theme.Primary, 1.5f))
                {
                    g.DrawRectangle(pen, iconX, iconY, 12, 10);
                    g.DrawLine(pen, iconX, iconY + 3, iconX + 12, iconY + 3);
                }
                using (SolidBrush brush = new SolidBrush(theme.Primary))
                {
                    g.FillRectangle(brush, iconX + 2, iconY + 1, 3, 2);
                    g.FillRectangle(brush, iconX + 7, iconY + 1, 3, 2);
                }
            };
            btnSelectFile.FlatAppearance.MouseOverBackColor = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(248, 242, 255);
            btnSelectFile.FlatAppearance.BorderColor = theme.Primary;
            btnSelectFile.FlatAppearance.BorderSize = 1;
            btnSelectFile.Click += BtnSelectFile_Click;
            actionArea.Controls.Add(btnSelectFile);
            StyleRoundButton(btnSelectFile, 8, theme.Primary, 1, "border");
            ay += SY(32) + SY(10);

            // 2. 开始检测按钮
            btnStartDetect = new Button
            {
                Text = "⚡️开始检测",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(36)),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.InfoColor,
                ForeColor = Color.White,
                Font = GetFont(SF(9.5f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            btnStartDetect.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int iconSize = 14;
                int iconY = (btnStartDetect.Height - iconSize) / 2;
                int iconX = 20;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(iconX, iconY, iconSize, iconSize);
                    using (SolidBrush brush = new SolidBrush(Color.White))
                        g.FillPath(brush, path);
                }
                using (SolidBrush playBrush = new SolidBrush(theme.InfoColor))
                {
                    PointF[] playPoints = new PointF[]
                    {
                        new PointF(iconX + 5, iconY + 3.5f),
                        new PointF(iconX + 5, iconY + iconSize - 3.5f),
                        new PointF(iconX + iconSize - 3, iconY + iconSize / 2f)
                    };
                    g.FillPolygon(playBrush, playPoints);
                }
            };
            btnStartDetect.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, theme.InfoColor.R + 20),
                Math.Min(255, theme.InfoColor.G + 20),
                Math.Min(255, theme.InfoColor.B + 20));
            btnStartDetect.FlatAppearance.BorderColor = theme.InfoColor;
            btnStartDetect.FlatAppearance.BorderSize = 0;
            btnStartDetect.Click += BtnStartDetect_Click;
            actionArea.Controls.Add(btnStartDetect);
            StyleRoundButton(btnStartDetect, 8, theme.InfoColor, 1, "info");
            ay += SY(36) + SY(6);

            btnStopDetect = new Button
            {
                Text = "⏹ 停止检测",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(32)),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.ErrorColor,
                ForeColor = Color.White,
                Font = GetFont(SF(8.5f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Enabled = false,
                Visible = false
            };
            btnStopDetect.EnabledChanged += (s, e) => btnStopDetect.Invalidate();
            btnStopDetect.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, theme.ErrorColor.R + 20),
                Math.Min(255, theme.ErrorColor.G + 20),
                Math.Min(255, theme.ErrorColor.B + 20));
            btnStopDetect.FlatAppearance.BorderSize = 0;
            btnStopDetect.Click += (s, e) =>
            {
                var result = DarkMessageBox.Show("确定要停止检测吗？已检测的数据将被保留。", "停止检测", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    cts?.Cancel();
                    isDetecting = false;
                    isPaused = false;
                    btnStartDetect.Text = "⏺ 开始检测";
                    btnStartDetect.BackColor = theme.InfoColor;
                    btnStartDetect.ForeColor = Color.White;
                    btnStopDetect.Enabled = false;
                    if (btnScanSource != null) btnScanSource.Enabled = true;
                }
            };
            actionArea.Controls.Add(btnStopDetect);
            StyleRoundButton(btnStopDetect, 8, theme.ErrorColor, 0, "error");
            ay += SY(32) + SY(10);

            // 4. 导出按钮
            Color exportBtnColor = Color.FromArgb(0xFF, 0x00, 0xFF);
            btnExport = new Button
            {
                Text = "📤合并导出",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = exportBtnColor,
                ForeColor = Color.White,
                Font = GetFont(SF(9f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, exportBtnColor.R + 30), Math.Min(255, exportBtnColor.G + 30), Math.Min(255, exportBtnColor.B + 30));
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            actionArea.Controls.Add(btnExport);
            StyleRoundButton(btnExport, 8, null, 0, "export");
            ay += SY(34) + SY(10);

            // 5. 直播源生成器按钮
            btnScanSource = new Button
            {
                Text = "📡源生成器",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.SuccessColor,
                ForeColor = Color.White,
                Font = GetFont(SF(9f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnScanSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, theme.SuccessColor.R + 20),
                Math.Min(255, theme.SuccessColor.G + 20),
                Math.Min(255, theme.SuccessColor.B + 20));
            btnScanSource.FlatAppearance.BorderSize = 0;
            btnScanSource.Click += (s, e) => ShowScanSourceDialog();
            actionArea.Controls.Add(btnScanSource);
            StyleRoundButton(btnScanSource, 8, theme.SuccessColor, 0, "success");
            ay += SY(34) + SY(6);

            btnParseLink = new Button
            {
                Text = "🔗 解析链接",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(147, 51, 234),
                ForeColor = Color.White,
                Font = GetFont(SF(9f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            btnParseLink.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, 147 + 20),
                Math.Min(255, 51 + 20),
                Math.Min(255, 234 + 20));
            btnParseLink.FlatAppearance.BorderSize = 0;
            bool parseIsRunning = false;
            bool parseIsPaused = false;
            int parseSuccessCount = 0;
            int parseTotalCount = 0;
            System.Threading.CancellationTokenSource parseCts = null;
            btnParseLink.Click += async (s, e) =>
            {
                if (!parseIsRunning && !parseIsPaused)
                {
                    var pendingChannels = allChannels.Where(c => c.Group == "解析待处理" && c.Status == "待解析").ToList();
                    if (pendingChannels.Count == 0)
                    {
                        DarkMessageBox.Show("没有待解析的链接", "解析链接", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    parseIsRunning = true;
                    parseIsPaused = false;
                    parseSuccessCount = 0;
                    parseTotalCount = pendingChannels.Count;
                    parseCts = new System.Threading.CancellationTokenSource();
                    btnParseLink.Text = "⏸ 暂停";
                    btnParseLink.BackColor = Color.FromArgb(251, 146, 60);

                    DateTime parseTime = DateTime.Now;
                    var failedChannels = new List<ChannelInfo>();
                    foreach (var channel in pendingChannels)
                    {
                        if (parseIsPaused)
                        {
                            while (parseIsPaused && !parseCts.Token.IsCancellationRequested)
                            {
                                await System.Threading.Tasks.Task.Delay(100);
                            }
                        }
                        if (parseCts.Token.IsCancellationRequested) break;
                        if (channel.Status != "待解析") continue;

                        bool parsedSuccess = false;
                        try
                        {
                            if (!Uri.IsWellFormedUriString(channel.Url, UriKind.Absolute))
                            {
                                continue;
                            }

                            using (var ctsParse = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
                            {
                                var resp = await httpClient.GetAsync(channel.Url, ctsParse.Token);
                                if (resp.IsSuccessStatusCode)
                                {
                                    string content = await resp.Content.ReadAsStringAsync();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        parseSuccessCount++;
                                        AddChannelToList(content, channel.Url, parseTime);
                                        channel.Status = "已解析";
                                        channel.ParseDateTime = parseTime;
                                        parsedSuccess = true;
                                    }
                                }
                            }
                        }
                        catch { }
                        if (!parsedSuccess)
                        {
                            failedChannels.Add(channel);
                        }
                        btnParseLink.Text = $"⏸ 暂停 ({parseSuccessCount}/{parseTotalCount})";
                    }
                    foreach (var failed in failedChannels)
                    {
                        allChannels.Remove(failed);
                    }

                    parseIsRunning = false;
                    parseIsPaused = false;
                    if (parseCts != null) parseCts.Dispose();
                    parseCts = null;
                    btnParseLink.Text = "🔗 解析链接";
                    btnParseLink.BackColor = Color.FromArgb(147, 51, 234);
                    RefreshGrid();
                    UpdateEmptyState();
                    UpdateActionButtonsVisibility();
                }
                else if (parseIsRunning && !parseIsPaused)
                {
                    parseIsPaused = true;
                    btnParseLink.Text = "⏹ 停止";
                    btnParseLink.BackColor = Color.FromArgb(239, 68, 68);
                }
                else if (parseIsRunning && parseIsPaused)
                {
                    parseIsRunning = false;
                    parseIsPaused = false;
                    if (parseCts != null)
                    {
                        parseCts.Cancel();
                        parseCts.Dispose();
                    }
                    parseCts = null;
                    btnParseLink.Text = "🔗 解析链接";
                    btnParseLink.BackColor = Color.FromArgb(147, 51, 234);
                    RefreshGrid();
                    UpdateEmptyState();
                    UpdateActionButtonsVisibility();
                }
            };
            actionArea.Controls.Add(btnParseLink);
            StyleRoundButton(btnParseLink, 8, Color.FromArgb(147, 51, 234), 0, "parse");
            ay += SY(34) + SY(26); // 按钮高度 34 + 间距 26 = 60，使 tipBox 位于 Y=160

            // 6. 提示框
            int tipW = btnW;
            int tipRadius = 8; // 提示框圆角半径
            string tipText = "1. 列表位置，点击右键发现更多功能\r\n2. 双击名称，重命名，双击链接，修复直播源。\r\n3. 打开设置发现更多功能。";
            Font tipContentFont = GetFont(9f);
            SizeF tipTextSize;
            using (Graphics g = CreateGraphics())
            {
                tipTextSize = g.MeasureString(tipText, tipContentFont, tipW - 24);
            }
            int tipContentHeight = (int)Math.Ceiling(tipTextSize.Height);
            int tipBoxHeight = 10 + 22 + 6 + tipContentHeight + 10;

            tipBox = new Panel
            {
                Location = new Point(leftX, ay),
                Size = new Size(tipW, tipBoxHeight),
                BackColor = Color.Transparent,
                Visible = false
            };
            Label tipTitle = new Label
            {
                Text = "提示",
                Font = GetFont(9.5f, FontStyle.Bold),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                Location = new Point(SX(12), SY(10)),
                BackColor = Color.Transparent
            };
            tipBox.Controls.Add(tipTitle);
            Label tipContent = new Label
            {
                Text = tipText,
                Font = tipContentFont,
                ForeColor = theme.TextSecondary,
                AutoSize = false,
                Location = new Point(12, 32 + 6),
                Size = new Size(tipW - 24, tipContentHeight),
                BackColor = Color.Transparent
            };
            tipBox.Controls.Add(tipContent);
            tipBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, tipBox.Width - 1, tipBox.Height - 1);
                using (GraphicsPath path = RoundedRectPath(rect, tipRadius))
                {
                    // 绘制背景
                    using (SolidBrush bgBrush = new SolidBrush(theme.TipBg))
                        e.Graphics.FillPath(bgBrush, path);
                    // 绘制边框
                    using (Pen pen = new Pen(theme.Border))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            tipBox.Resize += (s, e) =>
            {
                Label contentLbl = null;
                foreach (Control c in tipBox.Controls)
                {
                    if (c is Label && c != tipTitle)
                    {
                        contentLbl = (Label)c;
                        break;
                    }
                }
                if (contentLbl == null) return;
                using (Graphics g = tipBox.CreateGraphics())
                {
                    SizeF sz = g.MeasureString(contentLbl.Text, contentLbl.Font, tipBox.Width - 24);
                    int newContentH = (int)Math.Ceiling(sz.Height);
                    int newBoxH = 10 + 22 + 6 + newContentH + 10;
                    if (tipBox.Height != newBoxH)
                    {
                        tipBox.Height = newBoxH;
                        contentLbl.Height = newContentH;
                        UpdateActionButtonsVisibility();
                    }
                }
            };
            actionArea.Controls.Add(tipBox);
            ay += tipBoxHeight + SY(10);

            UpdateActionButtonsVisibility();

            // 在actionArea和mainArea之间添加分隔线
            actionSepRef = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = ColorBorder
            };

            outerWrap.Controls.Add(mainArea);
            outerWrap.Controls.Add(actionSepRef);
            outerWrap.Controls.Add(actionArea);
            CreateBottomBar();
            outerWrap.Controls.Add(bottomBarRef);
            outerWrap.Controls.Add(titleBarPanel);

            // 右键菜单
            dataGridViewContextMenu = new ContextMenuStrip();
            dataGridViewContextMenu.Font = GetFont(SF(9f));
            dataGridViewContextMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg)));
            dataGridViewContextMenu.ShowImageMargin = true;
            dataGridViewContextMenu.ShowCheckMargin = false;
            dataGridViewContextMenu.ShowItemToolTips = false;

            Image icoPaste = CreateMenuIcon("paste", Color.DodgerBlue);
            Image icoDetect = CreateMenuIcon("detect", Color.OrangeRed);
            Image icoSort = CreateMenuIcon("sort", Color.SeaGreen);
            Image icoPlay = CreateMenuIcon("play", Color.FromArgb(138, 78, 203));
            Image icoCopy = CreateMenuIcon("copy", Color.DodgerBlue);
            Image icoCopyAll = CreateMenuIcon("copyAll", Color.SeaGreen);
            Image icoSelectAll = CreateMenuIcon("selectAll", Color.SteelBlue);
            Image icoRename = CreateMenuIcon("rename", Color.SaddleBrown);
            Image icoDelete = CreateMenuIcon("delete", Color.Crimson);
            Image icoInfo = CreateMenuIcon("info", Color.SteelBlue);
            Image icoClearInv = CreateMenuIcon("clearInv", Color.Gray);
            Image icoClearAll = CreateMenuIcon("clearAll", Color.FromArgb(180, 50, 50));
            Image icoFixUrl = CreateMenuIcon("fix", Color.MediumSeaGreen);
            Image icoFixAll = CreateMenuIcon("fixAll", Color.DarkCyan);

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("从剪贴板粘贴链接", icoPaste, (s, e) => PasteFromClipboard());
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
            dataGridViewContextMenu.Items.Add(pasteItem);

            // 检测模式子菜单
            ToolStripMenuItem detectMenuItem = new ToolStripMenuItem("检测模式", icoDetect);
            ToolStripMenuItem modeNormal = new ToolStripMenuItem("普通模式(逐个检测)");
            ToolStripMenuItem modeFast = new ToolStripMenuItem("极速模式(5并发)");
            ToolStripMenuItem modeConcurrent = new ToolStripMenuItem("并发模式(10并发)");
            modeNormal.Click += (s, e) => { detectConcurrency = 1; modeNormal.Checked = true; modeFast.Checked = false; modeConcurrent.Checked = false; };
            modeFast.Click += (s, e) => { detectConcurrency = 5; modeNormal.Checked = false; modeFast.Checked = true; modeConcurrent.Checked = false; };
            modeConcurrent.Click += (s, e) => { detectConcurrency = 10; modeNormal.Checked = false; modeFast.Checked = false; modeConcurrent.Checked = true; };
            modeConcurrent.Checked = true;
            detectMenuItem.DropDownItems.Add(modeNormal);
            detectMenuItem.DropDownItems.Add(modeFast);
            detectMenuItem.DropDownItems.Add(modeConcurrent);
            dataGridViewContextMenu.Items.Add(detectMenuItem);

            // 排序子菜单
            ToolStripMenuItem sortMenuItem = new ToolStripMenuItem("排序", icoSort);
            sortMenuItem.DropDownItems.Add("按名称排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按延迟排序", null, (s, e) => { allChannels.Sort((a, b) => { int ta = ParseSpeed(a.Speed), tb = ParseSpeed(b.Speed); return ta.CompareTo(tb); }); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按状态排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Status, b.Status, StringComparison.Ordinal)); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按分组排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Group, b.Group, StringComparison.Ordinal)); RefreshGrid(); });
            dataGridViewContextMenu.Items.Add(sortMenuItem);

            // 播放子菜单
            ToolStripMenuItem playMenuItem = new ToolStripMenuItem("播放", icoPlay);
            playMenuItem.DropDownItems.Add("第三方播放器", null, (s, e) => { if (dgvData.SelectedRows.Count > 0) { string u = dgvData.SelectedRows[0].Cells[1].Value?.ToString(); if (!string.IsNullOrWhiteSpace(u)) PlayChannelCustom(u); } });
            playMenuItem.DropDownItems.Add(new ToolStripSeparator());
            playMenuItem.DropDownItems.Add("设置第三方播放器路径...", null, (s, e) => SetCustomPlayerPath());
            dataGridViewContextMenu.Items.Add(playMenuItem);

            dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem copyItem = new ToolStripMenuItem("复制链接", icoCopy, (s, e) => CopyLink());
            copyItem.ShortcutKeyDisplayString = "Ctrl+C";
            dataGridViewContextMenu.Items.Add(copyItem);

            ToolStripMenuItem copyAllItem = new ToolStripMenuItem("复制所有链接", icoCopyAll, (s, e) => CopyAllLinks());
            copyAllItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
            dataGridViewContextMenu.Items.Add(copyAllItem);

            ToolStripMenuItem selectAllItem = new ToolStripMenuItem("全选", icoSelectAll, (s, e) => SelectAllRows());
            selectAllItem.ShortcutKeyDisplayString = "Ctrl+A";
            dataGridViewContextMenu.Items.Add(selectAllItem);

            // 修复直播源子菜单
            ToolStripMenuItem fixUrlMenuItem = new ToolStripMenuItem("修复直播源", icoFixUrl);
            ToolStripMenuItem fixSingleItem = new ToolStripMenuItem("修复当前直播源", null, (s, e) =>
            {
                if (dgvData.SelectedRows.Count > 0)
                {
                    string u = dgvData.SelectedRows[0].Cells[1].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(u))
                        ShowReplaceUrlDialog(u);
                }
                else
                {
                    DarkMessageBox.Show("请先选中一条直播源！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            fixUrlMenuItem.DropDownItems.Add(fixSingleItem);

            ToolStripMenuItem fixAllItem = new ToolStripMenuItem("一键全部修复", null, (s, e) => ReplaceAllUrls());
            fixUrlMenuItem.DropDownItems.Add(fixAllItem);
            dataGridViewContextMenu.Items.Add(fixUrlMenuItem);

            dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem renameItem = new ToolStripMenuItem("重命名", icoRename, (s, e) => BeginRenameSelected());
            renameItem.ShortcutKeyDisplayString = "F2";
            dataGridViewContextMenu.Items.Add(renameItem);

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除此行", icoDelete, (s, e) => DeleteRow());
            deleteItem.ShortcutKeyDisplayString = "Del";
            dataGridViewContextMenu.Items.Add(deleteItem);

            ToolStripMenuItem detailItem = new ToolStripMenuItem("查看详情", icoInfo, (s, e) => ViewDetails());
            detailItem.ShortcutKeyDisplayString = "Enter";
            dataGridViewContextMenu.Items.Add(detailItem);

            dataGridViewContextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem clearInvalidItem = new ToolStripMenuItem("清空无效链接", icoClearInv, (s, e) => ClearInvalidLinks());
            dataGridViewContextMenu.Items.Add(clearInvalidItem);

            ToolStripMenuItem clearAllItem = new ToolStripMenuItem("清空所有列表", icoClearAll, (s, e) => ClearAllLinks());
            dataGridViewContextMenu.Items.Add(clearAllItem);

            dgvData.ContextMenuStrip = dataGridViewContextMenu;

            // 窗口首次显示后调整右侧标签位置并刷新列宽
            this.Shown += (s, e) =>
            {
                CenterEmptyState();
                int rightAreaWidth = cboGroupHost != null && cboGroupHost.Visible ? 328 : 15;
                int leftMargin = 98;
                searchBoxHost.Left = leftMargin;
                searchBoxHost.Width = searchPanelRef.ClientSize.Width - leftMargin - rightAreaWidth;
                txtSearch.Width = searchBoxHost.Width - 20;
                if (cboGroupHost.Visible)
                {
                    lblGroup.Left = searchPanelRef.ClientSize.Width - 298;
                    cboGroupHost.Left = searchPanelRef.ClientSize.Width - 158;
                    cboGroupHost.Width = 130;
                    cboGroupHost.Top = searchBoxHost.Top;
                }
                UpdateSearchBoxRegion();
                UpdateCboGroupRegion();
                UpdateStatusBarRegion();
                searchPanelRef.Invalidate();
                // 延迟强制刷新DataGridView列布局，解决首次打开列宽计算不正确的问题
                var refreshTask = new Func<Task>(async () =>
                {
                    try
                    {
                        await Task.Delay(100);
                        dgvData.PerformLayout();
                        dgvData.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UI刷新异常: {ex.Message}");
                    }
                });
                this.BeginInvoke(new Action(() => { _ = refreshTask(); }));
            };

            this.FormClosing += (s, e) =>
            {
                try { StopMouseHook(); } catch { }
                try { _ffplayOutputCts?.Cancel(); } catch { }
                try { _ffplayOutputCts?.Dispose(); } catch { }
                _ffplayOutputCts = null;
                KillRunningPlayer();
                StopPreview();
                if (persistList) SaveChannelList();
                SaveConfig();
                httpClient?.Dispose();
                CleanupCaches();
            };

            void CleanupCaches()
            {
                const int maxCacheSize = 1000;
                if (ipLocationCache.Count > maxCacheSize)
                {
                    var keys = ipLocationCache.Keys.Take(ipLocationCache.Count - maxCacheSize).ToList();
                    foreach (var key in keys) ipLocationCache.Remove(key);
                }
                if (domainIpCache.Count > maxCacheSize)
                {
                    var keys = domainIpCache.Keys.Take(domainIpCache.Count - maxCacheSize).ToList();
                    foreach (var key in keys) domainIpCache.Remove(key);
                }
                if (ipLocationFailed.Count > maxCacheSize)
                {
                    var keys = ipLocationFailed.Take(ipLocationFailed.Count - maxCacheSize).ToList();
                    foreach (var key in keys) ipLocationFailed.Remove(key);
                }
                if (domainIpFailed.Count > maxCacheSize)
                {
                    var keys = domainIpFailed.Take(domainIpFailed.Count - maxCacheSize).ToList();
                    foreach (var key in keys) domainIpFailed.Remove(key);
                }
            }

            if (persistList && allChannels.Count > 0)
            {
                UpdateGroupFilter();
                RefreshGrid();
                UpdateEmptyState();
                UpdateActionButtonsVisibility();
            }

            UpdateStatusBar();
            ApplyTheme();
            RefreshNavButtonSizes();

            if (btnParseLink != null) btnParseLink.Visible = false;
        }

        /// <summary>
        /// 绘制空状态图标（红色X）
        /// </summary>
        private void EmptyIcon_Paint(object sender, PaintEventArgs pe)
        {
            Graphics g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = pe.ClipRectangle.Width;
            int h = pe.ClipRectangle.Height;
            int size = Math.Min(w, h) - 8;
            int x = (w - size) / 2;
            int y = (h - size) / 2;
            int r = size / 2;

            using (Pen xPen = new Pen(Color.FromArgb(220, 80, 80), 4f))
            {
                xPen.StartCap = LineCap.Round;
                xPen.EndCap = LineCap.Round;
                int pad = size / 4;
                g.DrawLine(xPen, x + pad, y + pad, x + size - pad, y + size - pad);
                g.DrawLine(xPen, x + size - pad, y + pad, x + pad, y + size - pad);
            }
        }

        /// <summary>
        /// 选中导航项（切换视图）
        /// </summary>
        private void SelectNavItem(string name)
        {
            Color textColor = IsDarkColor(theme.Bg) ? Color.White : Color.Black;

            if (btnNavDetect != null)
            {
                btnNavDetect.ForeColor = textColor;
                btnNavDetect.Font = GetFont(SF(9f), FontStyle.Regular);
                btnNavDetect.Invalidate();
            }

            if (btnNavSearch != null)
            {
                btnNavSearch.ForeColor = textColor;
                btnNavSearch.Font = GetFont(SF(9f), FontStyle.Regular);
                btnNavSearch.Invalidate();
            }

            if (btnNavSettings != null)
            {
                btnNavSettings.ForeColor = textColor;
                btnNavSettings.Font = GetFont(SF(9f), FontStyle.Regular);
                btnNavSettings.Invalidate();
            }

            currentView = name;
            SwitchView(name);
        }

        private void IPTVLiveCheckerMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.P:
                        e.Handled = true;
                        btnNavDetect?.PerformClick();
                        break;
                    case Keys.F:
                        e.Handled = true;
                        btnNavSearch?.PerformClick();
                        break;
                    case Keys.S:
                        e.Handled = true;
                        btnNavSettings?.PerformClick();
                        break;
                }
            }
        }

        private void SwitchView(string name)
        {
            bool isDetect = (name == "检测");

            if (statusBarRef != null) statusBarRef.Visible = isDetect;
            if (searchPanelRef != null) searchPanelRef.Visible = isDetect;
            if (gridContainerRef != null) gridContainerRef.Visible = isDetect;

            if (actionArea != null) actionArea.Visible = isDetect;
            if (actionSepRef != null) actionSepRef.Visible = isDetect;

            if (actionArea != null)
            {
                foreach (Control c in actionArea.Controls)
                {
                    if (c == btnStartDetect || c == btnStopDetect || c == btnExport)
                        continue;
                    c.Visible = isDetect;
                }
            }

            mainArea.PerformLayout();
            UpdateScrollBarTheme(mainArea);
        }

        private void LayoutFillPanel(Panel p)
        {
            if (p == null || mainArea == null) return;
            Rectangle r = mainArea.ClientRectangle;
            p.Bounds = r;
            p.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        }

        private void ForceCreateChildHandles(Control parent)
        {
            if (parent == null) return;
            // 强制创建当前控件的句柄
            var handle = parent.Handle;
            foreach (Control c in parent.Controls)
            {
                ForceCreateChildHandles(c);
            }
        }

        private void UpdateScrollBarTheme(Control parent)
        {
            if (parent == null) return;
            bool isDark = (theme == AppTheme.Dark);
            string themeName = isDark ? "DarkMode_Explorer" : null;
            int darkMode = isDark ? 1 : 0;
            foreach (Control c in parent.Controls)
            {
                if (c is ScrollableControl || c is Panel || c is DataGridView || c is TextBox || c is TabControl || c is DarkTabControl || c is ListBox)
                {
                    try { SetWindowTheme(c.Handle, themeName, null); } catch { }
                    try
                    {
                        DwmSetWindowAttribute(c.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);
                        DwmSetWindowAttribute(c.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref darkMode, 4);
                    }
                    catch { }
                }
                if (c is DataGridView dgv)
                {
                    try
                    {
                        foreach (Control child in dgv.Controls)
                        {
                            if (child is VScrollBar || child is HScrollBar)
                            {
                                SetWindowTheme(child.Handle, themeName, null);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref darkMode, 4);
                            }
                        }
                    }
                    catch { }
                }
                if (c is TextBox tb)
                {
                    try
                    {
                        foreach (Control child in tb.Controls)
                        {
                            if (child is VScrollBar || child is HScrollBar)
                            {
                                SetWindowTheme(child.Handle, themeName, null);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref darkMode, 4);
                            }
                        }
                    }
                    catch { }
                }
                if (c is ListBox lb)
                {
                    try
                    {
                        foreach (Control child in lb.Controls)
                        {
                            if (child is VScrollBar || child is HScrollBar)
                            {
                                SetWindowTheme(child.Handle, themeName, null);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, 4);
                                DwmSetWindowAttribute(child.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2, ref darkMode, 4);
                            }
                        }
                    }
                    catch { }
                }
                if (c.HasChildren) UpdateScrollBarTheme(c);
            }
        }

        /// <summary>
        /// 空状态居中
        /// </summary>
        private void CenterEmptyState()
        {
            if (emptyStatePanel != null && emptyLabel != null && dgvData?.Parent != null)
            {
                int w = dgvData.Parent.ClientSize.Width;
                int h = dgvData.Parent.ClientSize.Height;
                int pw = SX(140), ph = SY(110);
                emptyStatePanel.Location = new Point((w - pw) / 2, (h - ph) / 2);
                emptyStatePanel.Size = new Size(pw, ph);

                foreach (Control c in emptyStatePanel.Controls)
                {
                    if (c is PictureBox)
                        c.Location = new Point((pw - c.Width) / 2, 0);
                    else if (c is Label lbl)
                        lbl.Location = new Point((pw - lbl.Width) / 2, SY(66));
                }
            }
        }

        /// <summary>
        /// 更新空状态显示
        /// </summary>
        private void UpdateEmptyState()
        {
            if (emptyStatePanel != null && dgvData != null)
            {
                emptyStatePanel.Visible = (dgvData.Rows.Count == 0);
                if (emptyStatePanel.Visible) emptyStatePanel.BringToFront();
            }
        }

        /// <summary>
        /// 根据数据列表状态更新左侧按钮显示/隐藏
        /// </summary>
        private void UpdateActionButtonsVisibility()
        {
            bool hasData = allChannels != null && allChannels.Count > 0;
            bool hasPendingParse = allChannels != null && allChannels.Any(c => c.Group == "解析待处理" && c.Status == "待解析");
            bool canShowParseLink = hasPendingParse && hasSearchPlatformData && !autoParseLink;
            if (btnStartDetect != null) btnStartDetect.Visible = hasData;
            if (btnStopDetect != null) btnStopDetect.Visible = hasData;
            if (btnExport != null) btnExport.Visible = hasData;
            if (btnParseLink != null) btnParseLink.Visible = canShowParseLink;

            int ay = SY(14) + SY(32) + SY(10);
            if (hasData)
            {
                if (btnStartDetect != null) btnStartDetect.Location = new Point(SX(12), ay);
                ay += SY(36) + SY(6);
                if (btnStopDetect != null) btnStopDetect.Location = new Point(SX(12), ay);
                ay += SY(32) + SY(10);
                if (btnExport != null) btnExport.Location = new Point(SX(12), ay);
                ay += SY(34) + SY(10);
            }
            if (btnScanSource != null) btnScanSource.Location = new Point(SX(12), ay);
            ay += SY(34) + SY(6);
            // 解析链接按钮：无论是否可见都占用空间
            // 布局说明：按钮高度 SY(34)，与 tipBox 间距 SY(26)，确保 tipBox 在 Y=160 位置
            if (btnParseLink != null)
            {
                btnParseLink.Visible = canShowParseLink;
                btnParseLink.Location = new Point(SX(12), ay);
                ay += SY(34) + SY(26); // 按钮高度 34 + 间距 26 = 60，使 tipBox 位于 Y=160
            }

            // 提示栏跟随按钮位置自动滑动补正
            if (tipBox != null)
            {
                tipBox.Location = new Point(SX(12), ay); // tipBox 位于 Y=160（在解析链接按钮下方）
            }
        }

        private void UpdateTipBoxSize()
        {
            if (tipBox == null) return;
            string tipText = "1. 列表位置，点击右键发现更多功能\r\n2. 双击名称，重命名，双击链接，修复直播源。\r\n3. 打开设置发现更多功能。";
            Font tipContentFont = GetFont(9f);
            int tipW = tipBox.Width;
            SizeF tipTextSize;
            using (Graphics g = CreateGraphics())
            {
                tipTextSize = g.MeasureString(tipText, tipContentFont, tipW - 24);
            }
            int tipContentHeight = (int)Math.Ceiling(tipTextSize.Height);
            int tipBoxHeight = 10 + 22 + 6 + tipContentHeight + 10;
            tipBox.Size = new Size(tipW, tipBoxHeight);
            foreach (Control ctrl in tipBox.Controls)
            {
                if (ctrl is Label label && !ctrl.Text.Equals("提示"))
                {
                    label.Size = new Size(tipW - 24, tipContentHeight);
                }
            }
        }

        /// <summary>
        /// 单元格格式化：URL链接色、响应速度分级配色
        /// </summary>
        private void DgvData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dgvData.Columns[e.ColumnIndex].Name;
            if (colName == "colUrl")
            {
                e.CellStyle.ForeColor = theme.LinkTextColor;
            }
            else if (colName == "colSpeed")
            {
                string speedText = e.Value?.ToString() ?? "";
                Color speedColor;
                if (string.IsNullOrWhiteSpace(speedText) || speedText == "超时" || speedText == "-1")
                {
                    speedColor = Color.FromArgb(150, 153, 160);
                }
                else
                {
                    int ms = ParseSpeed(speedText);
                    if (ms < 1000)
                        speedColor = Color.FromArgb(39, 174, 96);
                    else if (ms < 3500)
                        speedColor = Color.FromArgb(230, 126, 34);
                    else
                        speedColor = Color.FromArgb(231, 76, 60);
                }
                e.CellStyle.ForeColor = speedColor;
                e.CellStyle.SelectionForeColor = speedColor;
            }
        }

        private void DgvData_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            string colName = dgvData.Columns[e.ColumnIndex].Name;
            if (colName == "colAction") return;
            if (sortedColumn == colName)
            {
                sortDirection = sortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sortedColumn = colName;
                sortDirection = SortOrder.Ascending;
            }
            SortChannels();
            dgvData.Invalidate();
        }

        private Color LightenColor(Color color, int percent)
        {
            float factor = 1 + percent / 100f;
            int r = Math.Min(255, (int)(color.R * factor));
            int g = Math.Min(255, (int)(color.G * factor));
            int b = Math.Min(255, (int)(color.B * factor));
            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// 显示华视美达扫描配置对话框，返回 (scanCount, threadCount)，取消返回 null
        /// 使用 Show() + TaskCompletionSource 模拟模态效果，避免 ShowDialog() 与 WebView2 消息循环冲突
        /// </summary>
        private async Task<Tuple<int, int>> ShowScanConfigDialogAsync()
        {
            // 如果不在 UI 线程，转发到 UI 线程执行
            if (InvokeRequired || !IsHandleCreated)
            {
                try
                {
                    var result = Invoke(new Func<Task<Tuple<int, int>>>(ShowScanConfigDialogAsync));
                    return await (Task<Tuple<int, int>>)result;
                }
                catch (InvalidOperationException)
                {
                    // 如果 Invoke 失败（句柄已销毁），返回 null
                    return null;
                }
            }

            int scanCount = 100;
            int threadCount = 8;

            var tcs = new TaskCompletionSource<DialogResult>();

            using (var scanDlg = new Form())
            {
                bool isDarkScan = IsDarkColor(theme.Bg);
                scanDlg.Text = "华视美达扫描配置";
                scanDlg.Size = new Size(SX(420), SY(290));
                scanDlg.StartPosition = FormStartPosition.Manual;
                scanDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                scanDlg.MaximizeBox = false;
                scanDlg.MinimizeBox = false;
                scanDlg.BackColor = isDarkScan ? Color.FromArgb(45, 45, 55) : Color.White;
                scanDlg.ForeColor = isDarkScan ? Color.FromArgb(220, 220, 230) : Color.Black;
                SetFormDarkModeTitleBar(scanDlg, isDarkScan);
                CenterForm(scanDlg, this);

                int labelW = SX(130);
                int inputX = SX(150);
                int inputW = SX(120);
                int rowH = SY(45);
                int startY = SY(30);

                Label lblCount = new Label { Text = "扫描CID数量:", Location = new Point(SX(25), startY), Size = new Size(labelW, SY(28)), Font = GetFont(SF(11)), ForeColor = scanDlg.ForeColor, BackColor = scanDlg.BackColor };
                scanDlg.Controls.Add(lblCount);

                TextBox txtCount = new TextBox { Text = "100", Location = new Point(inputX, startY + SY(2)), Size = new Size(inputW, SY(26)), Font = GetFont(SF(11)), BackColor = isDarkScan ? Color.FromArgb(30, 30, 38) : Color.White, ForeColor = isDarkScan ? Color.FromArgb(220, 220, 230) : Color.Black };
                scanDlg.Controls.Add(txtCount);

                Label lblThread = new Label { Text = "并发线程数:", Location = new Point(SX(25), startY + rowH), Size = new Size(labelW, SY(28)), Font = GetFont(SF(11)), ForeColor = scanDlg.ForeColor, BackColor = scanDlg.BackColor };
                scanDlg.Controls.Add(lblThread);

                TextBox txtThread = new TextBox { Text = "8", Location = new Point(inputX, startY + rowH + SY(2)), Size = new Size(inputW, SY(26)), Font = GetFont(SF(11)), BackColor = isDarkScan ? Color.FromArgb(30, 30, 38) : Color.White, ForeColor = isDarkScan ? Color.FromArgb(220, 220, 230) : Color.Black };
                scanDlg.Controls.Add(txtThread);

                int btnW = SX(100), btnH = SY(38), btnGap = SX(30);
                int btnGroupW = btnW * 2 + btnGap;
                int btnStartX = (scanDlg.ClientSize.Width - btnGroupW) / 2;
                int btnY = SY(195);

                Button btnOK = new Button { Text = "确定", Location = new Point(btnStartX, btnY), Size = new Size(btnW, btnH), Font = GetFont(SF(11)), BackColor = isDarkScan ? Color.FromArgb(55, 55, 70) : Color.FromArgb(200, 200, 210), ForeColor = isDarkScan ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
                btnOK.FlatAppearance.BorderColor = isDarkScan ? Color.FromArgb(80, 80, 100) : Color.Gray;
                btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), SX(6)));
                btnOK.Click += (s, e) => { tcs.SetResult(DialogResult.OK); scanDlg.Close(); };
                scanDlg.Controls.Add(btnOK);

                Button btnCancel = new Button { Text = "取消", Location = new Point(btnStartX + btnW + btnGap, btnY), Size = new Size(btnW, btnH), Font = GetFont(SF(11)), BackColor = isDarkScan ? Color.FromArgb(55, 55, 70) : Color.FromArgb(200, 200, 210), ForeColor = isDarkScan ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
                btnCancel.FlatAppearance.BorderColor = isDarkScan ? Color.FromArgb(80, 80, 100) : Color.Gray;
                btnCancel.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnW, btnH), SX(6)));
                btnCancel.Click += (s, e) => { tcs.SetResult(DialogResult.Cancel); scanDlg.Close(); };
                scanDlg.Controls.Add(btnCancel);

                scanDlg.FormClosing += (s, e) => { if (!tcs.Task.IsCompleted) tcs.SetResult(DialogResult.Cancel); };

                scanDlg.Show(this);
                var result = await tcs.Task;

                if (result == DialogResult.OK)
                {
                    int.TryParse(txtCount.Text, out scanCount);
                    int.TryParse(txtThread.Text, out threadCount);
                    if (scanCount < 1) scanCount = 1;
                    if (scanCount > 500) scanCount = 500;
                    if (threadCount < 1) threadCount = 1;
                    if (threadCount > 20) threadCount = 20;
                    return Tuple.Create(scanCount, threadCount);
                }
            }
            return null;
        }

        private void SortChannels()
        {
            if (string.IsNullOrEmpty(sortedColumn)) return;
            int asc = sortDirection == SortOrder.Ascending ? 1 : -1;
            Comparison<ChannelInfo> cmp = null;
            switch (sortedColumn)
            {
                case "colName": cmp = (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal) * asc; break;
                case "colUrl": cmp = (a, b) => string.Compare(a.Url, b.Url, StringComparison.Ordinal) * asc; break;
                case "colLocation": cmp = (a, b) => string.Compare(a.Location, b.Location, StringComparison.Ordinal) * asc; break;
                case "colResolution": cmp = (a, b) => string.Compare(a.Resolution, b.Resolution, StringComparison.Ordinal) * asc; break;
                case "colSpeed":
                    cmp = (a, b) =>
                    {
                        int ta = ParseSpeed(a.Speed), tb = ParseSpeed(b.Speed);
                        return ta.CompareTo(tb) * asc;
                    };
                    break;
                case "colGroup": cmp = (a, b) => string.Compare(a.Group, b.Group, StringComparison.Ordinal) * asc; break;
                case "colStatus": cmp = (a, b) => string.Compare(a.Status, b.Status, StringComparison.Ordinal) * asc; break;
            }
            if (cmp != null)
            {
                allChannels.Sort(cmp);
                RefreshGrid();
            }
        }

        private GraphicsPath RoundedRectPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawStatusTag(Graphics g, Rectangle bounds, string text, Color bg, Color border, Color foreColor)
        {
            int tagH = SY(22);
            int tagPad = SX(10);
            using (Font tagFont = GetFont(SF(6.7f)))
            {
                Size textSize = TextRenderer.MeasureText(text, tagFont);
                int tagW = textSize.Width + tagPad * 2;
                int tagX = bounds.X + (bounds.Width - tagW) / 2;
                int tagY = bounds.Y + (bounds.Height - tagH) / 2;
                Rectangle tagRect = new Rectangle(tagX, tagY, tagW, tagH);

                using (GraphicsPath path = RoundedRectPath(tagRect, SX(11)))
                {
                    using (SolidBrush bgBrush = new SolidBrush(bg))
                        g.FillPath(bgBrush, path);
                    using (Pen pen = new Pen(border, 1))
                        g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, text, tagFont, tagRect, foreColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        private void DrawRoundedButton(Graphics g, Rectangle rect, string text, Color bg, Color foreColor)
        {
            using (GraphicsPath path = RoundedRectPath(rect, SX(4)))
            {
                using (SolidBrush bgBrush = new SolidBrush(bg))
                    g.FillPath(bgBrush, path);
            }
            using (Font btnFont = GetFont(SF(6.7f)))
            {
                TextRenderer.DrawText(g, text, btnFont, rect, foreColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        private Image CreateMenuIcon(string iconType, Color color)
        {
            int s = 16;
            Bitmap bmp = new Bitmap(s, s);
            try
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush bg = new SolidBrush(color))
                    using (GraphicsPath rounded = RoundedRectPath(new Rectangle(1, 1, s - 2, s - 2), 3))
                    using (Pen white = new Pen(Color.White, 1.5f))
                    using (SolidBrush whiteB = new SolidBrush(Color.White))
                    {
                        g.FillPath(bg, rounded);
                        switch (iconType)
                        {
                            case "paste":
                                g.DrawRectangle(white, 4, 3, 8, 10);
                                g.FillRectangle(whiteB, 6, 2, 4, 2);
                                g.FillRectangle(bg, 6, 3, 4, 2);
                                break;
                            case "detect":
                                g.DrawString("⚡", new Font("Segoe UI Symbol", 9f), whiteB, 0, 0);
                                break;
                            case "sort":
                                g.DrawLine(white, 8, 3, 8, 13);
                                g.DrawLine(white, 5, 6, 8, 3);
                                g.DrawLine(white, 11, 6, 8, 3);
                                g.DrawLine(white, 5, 10, 8, 13);
                                g.DrawLine(white, 11, 10, 8, 13);
                                break;
                            case "play":
                                Point[] tri = { new Point(5, 3), new Point(13, 8), new Point(5, 13) };
                                g.FillPolygon(whiteB, tri);
                                break;
                            case "copy":
                                g.DrawRectangle(white, 4, 4, 6, 7);
                                g.DrawRectangle(white, 7, 6, 6, 7);
                                g.FillRectangle(bg, 7, 6, 2, 1);
                                g.FillRectangle(bg, 4, 10, 3, 1);
                                break;
                            case "rename":
                                g.DrawLine(white, 4, 12, 10, 6);
                                g.DrawLine(white, 10, 6, 12, 8);
                                g.DrawLine(white, 4, 12, 6, 12);
                                break;
                            case "delete":
                                g.DrawLine(white, 4, 4, 12, 12);
                                g.DrawLine(white, 12, 4, 4, 12);
                                break;
                            case "info":
                                g.FillEllipse(whiteB, 7, 3, 2, 2);
                                g.DrawLine(white, 8, 6, 8, 12);
                                break;
                            case "clearInv":
                                g.DrawLine(white, 3, 13, 13, 3);
                                g.DrawEllipse(white, 3, 3, 10, 10);
                                break;
                            case "clearAll":
                                g.DrawLine(white, 4, 5, 12, 5);
                                g.DrawLine(white, 5, 5, 5, 12);
                                g.DrawLine(white, 11, 5, 11, 12);
                                g.DrawLine(white, 5, 12, 11, 12);
                                g.DrawLine(white, 7, 3, 9, 3);
                                g.DrawLine(white, 7, 3, 7, 5);
                                g.DrawLine(white, 9, 3, 9, 5);
                                g.DrawLine(white, 3, 5, 13, 5);
                                break;
                            case "selectAll":
                                g.DrawRectangle(white, 3, 3, 5, 5);
                                g.DrawRectangle(white, 9, 3, 5, 5);
                                g.DrawRectangle(white, 3, 9, 5, 5);
                                g.DrawRectangle(white, 9, 9, 5, 5);
                                break;
                            case "copyAll":
                                g.DrawRectangle(white, 3, 3, 5, 6);
                                g.DrawRectangle(white, 9, 3, 5, 6);
                                g.DrawRectangle(white, 3, 9, 5, 6);
                                g.DrawRectangle(white, 9, 9, 5, 6);
                                break;
                            case "fix":
                                g.DrawString("🔧", new Font("Segoe UI Symbol", 9f), whiteB, 0, 0);
                                break;
                            case "fixAll":
                                g.DrawString("🔧", new Font("Segoe UI Symbol", 7f), whiteB, 0, 1);
                                g.DrawString("↻", new Font("Segoe UI Symbol", 7f), whiteB, 7, 1);
                                break;
                            case "sub":
                                Point[] arrow = { new Point(6, 4), new Point(11, 8), new Point(6, 12) };
                                g.FillPolygon(whiteB, arrow);
                                break;
                        }
                    }
                }
                Image result = (Image)bmp.Clone();
                bmp.Dispose();
                return result;
            }
            catch
            {
                bmp?.Dispose();
                throw;
            }
        }

        private class MenuColorTable : ProfessionalColorTable
        {
            private readonly bool _isDark;

            public MenuColorTable(bool isDark)
            {
                _isDark = isDark;
            }

            public override Color MenuBorder => _isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 205);
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => _isDark ? Color.FromArgb(70, 70, 85) : Color.FromArgb(230, 225, 245);
            public override Color MenuItemSelectedGradientBegin => _isDark ? Color.FromArgb(70, 70, 85) : Color.FromArgb(230, 225, 245);
            public override Color MenuItemSelectedGradientEnd => _isDark ? Color.FromArgb(70, 70, 85) : Color.FromArgb(230, 225, 245);
            public override Color MenuStripGradientBegin => _isDark ? Color.FromArgb(40, 40, 50) : Color.White;
            public override Color MenuStripGradientEnd => _isDark ? Color.FromArgb(40, 40, 50) : Color.White;
            public override Color ToolStripBorder => _isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 205);
            public override Color ToolStripDropDownBackground => _isDark ? Color.FromArgb(45, 45, 55) : Color.White;
            public override Color ImageMarginGradientBegin => _isDark ? Color.FromArgb(45, 45, 55) : Color.White;
            public override Color ImageMarginGradientMiddle => _isDark ? Color.FromArgb(45, 45, 55) : Color.White;
            public override Color ImageMarginGradientEnd => _isDark ? Color.FromArgb(45, 45, 55) : Color.White;
            public override Color SeparatorDark => _isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(220, 220, 225);
            public override Color SeparatorLight => Color.Transparent;
        }

        private class RoundedMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly bool _isDark;
            private readonly int _radius = 8;

            public RoundedMenuRenderer(bool isDark) : base(new MenuColorTable(isDark))
            {
                _isDark = isDark;
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (GraphicsPath path = GetRoundedRectangle(e.AffectedBounds, _radius))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(_isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 205)))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (GraphicsPath path = GetRoundedRectangle(e.AffectedBounds, _radius))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (SolidBrush brush = new SolidBrush(_isDark ? Color.FromArgb(45, 45, 55) : Color.White))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected)
                {
                    e.Item.BackColor = Color.Transparent;
                    return;
                }

                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                using (GraphicsPath path = GetRoundedRectangle(rect, _radius))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (SolidBrush brush = new SolidBrush(_isDark ? Color.FromArgb(70, 70, 85) : Color.FromArgb(230, 225, 245)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }

            private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.X + rect.Width - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.X + rect.Width - radius * 2, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseAllFigures();
                return path;
            }
        }

        /// <summary>
        /// 自绘单元格：表头（浅灰底+紫色排序箭头）、状态标签、双按钮（复制+播放）
        /// </summary>
        private void DgvData_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                e.PaintBackground(e.ClipBounds, false);
                string colName = dgvData.Columns[e.ColumnIndex].Name;
                bool isSorted = (colName == sortedColumn);

                using (SolidBrush bgBrush = new SolidBrush(theme.HeaderBg))
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);

                using (Pen pen = new Pen(theme.Border, 1))
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                // 列标题右侧竖线分割线
                if (e.ColumnIndex < dgvData.Columns.Count - 1)
                {
                    using (Pen sepPen = new Pen(theme.Border, 1))
                        e.Graphics.DrawLine(sepPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                }

                string headerText = e.FormattedValue?.ToString() ?? "";
                using (Font headerFont = dgvData.ColumnHeadersDefaultCellStyle.Font ?? GetFont(SF(9f)))
                {
                    int arrowW = isSorted ? SX(16) : 0;
                    int textPad = SX(12);
                    Rectangle textRect = new Rectangle(
                        e.CellBounds.X + textPad,
                        e.CellBounds.Y,
                        e.CellBounds.Width - textPad - arrowW - 4,
                        e.CellBounds.Height);

                    TextFormatFlags tff = TextFormatFlags.VerticalCenter;
                    if (colName == "colName" || colName == "colUrl")
                        tff |= TextFormatFlags.Left;
                    else
                        tff |= TextFormatFlags.HorizontalCenter;

                    Color headerColor = isSorted ? theme.Primary : theme.TextPrimary;
                    TextRenderer.DrawText(e.Graphics, headerText, headerFont, textRect, headerColor, tff);

                    if (isSorted && colName != "colAction")
                    {
                        string arrow = sortDirection == SortOrder.Ascending ? "▲" : "▼";
                        using (Font arrowFont = new Font(dgvData.Font.FontFamily, SF(7f), FontStyle.Bold))
                        {
                            Size arrowSize = TextRenderer.MeasureText(arrow, arrowFont);
                            int arrowX = textRect.Right + 2;
                            int arrowY = e.CellBounds.Y + (e.CellBounds.Height - arrowSize.Height) / 2;
                            TextRenderer.DrawText(e.Graphics, arrow, arrowFont, new Point(arrowX, arrowY), theme.Primary);
                        }
                    }
                }
                e.Handled = true;
            }
            else if (e.RowIndex >= 0)
            {
                string colName = dgvData.Columns[e.ColumnIndex].Name;
                Color rowSepColor = theme.Name == "深色" ? Color.FromArgb(75, 75, 90) : Color.FromArgb(220, 220, 230);

                // 所有单元格右侧竖线分割线
                if (e.ColumnIndex < dgvData.Columns.Count - 1)
                {
                    using (Pen sepPen = new Pen(rowSepColor, 1))
                        e.Graphics.DrawLine(sepPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
                }

                if (colName == "colStatus")
                {
                    e.PaintBackground(e.ClipBounds, false);
                    int r = e.RowIndex;
                    bool isSelected = dgvData.Rows[r].Selected;
                    bool isHover = _hoverRow == r;
                    if (isSelected)
                    {
                        using (SolidBrush selBrush = new SolidBrush(theme.SelectRow))
                            e.Graphics.FillRectangle(selBrush, e.CellBounds);
                    }
                    else if (isHover)
                    {
                        Color hoverColor = theme.Name == "深色"
                            ? Color.FromArgb(65, 60, 80)
                            : Color.FromArgb(245, 240, 252);
                        using (SolidBrush hoverBrush = new SolidBrush(hoverColor))
                            e.Graphics.FillRectangle(hoverBrush, e.CellBounds);
                    }
                    string status = e.Value?.ToString() ?? "";
                    if (status == "可用")
                    {
                        DrawStatusTag(e.Graphics, e.CellBounds, status, theme.StatusTagBg, theme.StatusTagBorder, theme.SuccessColor);
                    }
                    else if (status == "不可用")
                    {
                        Color bg = theme.Name == "深色" ? Color.FromArgb(80, 40, 40) : Color.FromArgb(255, 235, 235);
                        DrawStatusTag(e.Graphics, e.CellBounds, status, bg, theme.ErrorColor, theme.ErrorColor);
                    }
                    else if (status == "检测中")
                    {
                        Color bg = theme.Name == "深色" ? Color.FromArgb(80, 65, 30) : Color.FromArgb(255, 248, 230);
                        DrawStatusTag(e.Graphics, e.CellBounds, status, bg, theme.WarnColor, theme.WarnColor);
                    }
                    else
                    {
                        TextRenderer.DrawText(e.Graphics, status, dgvData.Font, e.CellBounds, theme.TextSecondary,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    }

                    // 行底部分隔线
                    Color sepColor2 = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247);
                    using (Pen pen2 = new Pen(sepColor2, 1))
                        e.Graphics.DrawLine(pen2, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                    e.Handled = true;
                }
                else if (colName == "colAction")
                {
                    e.PaintBackground(e.ClipBounds, false);
                    int r = e.RowIndex;
                    bool isSelected = dgvData.Rows[r].Selected;
                    bool isHover = _hoverRow == r;
                    if (isSelected)
                    {
                        using (SolidBrush selBrush = new SolidBrush(theme.SelectRow))
                            e.Graphics.FillRectangle(selBrush, e.CellBounds);
                    }
                    else if (isHover)
                    {
                        Color hoverColor = theme.Name == "深色"
                            ? Color.FromArgb(65, 60, 80)
                            : Color.FromArgb(245, 240, 252);
                        using (SolidBrush hoverBrush = new SolidBrush(hoverColor))
                            e.Graphics.FillRectangle(hoverBrush, e.CellBounds);
                    }
                    int cellW = e.CellBounds.Width;
                    int cellH = e.CellBounds.Height;
                    int btnH = SY(26);
                    int btnW = SX(46);
                    int gap = SX(14);
                    int totalW = btnW * 2 + gap;
                    int startX = e.CellBounds.X + (cellW - totalW) / 2;
                    int startY = e.CellBounds.Y + (cellH - btnH) / 2;

                    Rectangle copyRect = new Rectangle(startX, startY, btnW, btnH);
                    Rectangle playRect = new Rectangle(startX + btnW + gap, startY, btnW, btnH);

                    Color copyBg = theme.CopyBtnBg;
                    Color copyFg = theme.CopyBtnText;
                    Color playBg = theme.PlayBtnBg;
                    Color playFg = theme.PlayBtnText;

                    if (_pressRow == r && _pressBtn == 0)
                    {
                        copyBg = Color.FromArgb(Math.Min(255, theme.CopyBtnBg.R + 30), Math.Min(255, theme.CopyBtnBg.G + 30), Math.Min(255, theme.CopyBtnBg.B + 30));
                        copyRect.Offset(0, 1);
                    }
                    else if (_hoverRow == r && _hoverBtn == 0)
                    {
                        copyBg = Color.FromArgb(Math.Min(255, theme.CopyBtnBg.R + 18), Math.Min(255, theme.CopyBtnBg.G + 18), Math.Min(255, theme.CopyBtnBg.B + 18));
                    }

                    if (_pressRow == r && _pressBtn == 1)
                    {
                        playBg = Color.FromArgb(Math.Min(255, theme.PlayBtnBg.R + 30), Math.Min(255, theme.PlayBtnBg.G + 30), Math.Min(255, theme.PlayBtnBg.B + 30));
                        playRect.Offset(0, 1);
                    }
                    else if (_hoverRow == r && _hoverBtn == 1)
                    {
                        playBg = Color.FromArgb(Math.Min(255, theme.PlayBtnBg.R + 18), Math.Min(255, theme.PlayBtnBg.G + 18), Math.Min(255, theme.PlayBtnBg.B + 18));
                    }

                    if (_clickRow == r && _clickBtn == 0)
                    {
                        copyBg = Color.FromArgb(Math.Max(0, theme.CopyBtnBg.R - 30), Math.Max(0, theme.CopyBtnBg.G - 30), Math.Max(0, theme.CopyBtnBg.B - 30));
                        copyRect.Offset(0, 1);
                    }
                    if (_clickRow == r && _clickBtn == 1)
                    {
                        playBg = Color.FromArgb(Math.Max(0, theme.PlayBtnBg.R - 30), Math.Max(0, theme.PlayBtnBg.G - 30), Math.Max(0, theme.PlayBtnBg.B - 30));
                        playRect.Offset(0, 1);
                    }

                    DrawRoundedButton(e.Graphics, copyRect, "复制", copyBg, copyFg);
                    DrawRoundedButton(e.Graphics, playRect, "播放", playBg, playFg);

                    // 行底部分隔线（横跨整行）
                    Rectangle firstCell = dgvData.GetCellDisplayRectangle(0, e.RowIndex, false);
                    Color sepColor = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247);
                    using (Pen pen = new Pen(sepColor, 1))
                        e.Graphics.DrawLine(pen, firstCell.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                    e.Handled = true;
                }
                else
                {
                    e.PaintBackground(e.ClipBounds, false);
                    int r = e.RowIndex;
                    bool isSelected = dgvData.Rows[r].Selected;
                    bool isHover = _hoverRow == r;
                    if (isSelected)
                    {
                        using (SolidBrush selBrush = new SolidBrush(theme.SelectRow))
                            e.Graphics.FillRectangle(selBrush, e.CellBounds);
                    }
                    else if (isHover)
                    {
                        Color hoverColor = theme.Name == "深色"
                            ? Color.FromArgb(65, 60, 80)
                            : Color.FromArgb(245, 240, 252);
                        using (SolidBrush hoverBrush = new SolidBrush(hoverColor))
                            e.Graphics.FillRectangle(hoverBrush, e.CellBounds);
                    }
                    string cellText = e.FormattedValue?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(cellText))
                    {
                        Color textColor = isSelected ? theme.SelectRowText : e.CellStyle.ForeColor;
                        Font baseFont = e.CellStyle.Font ?? dgvData.Font;
                        int padding = SX(10);
                        Rectangle textRect = new Rectangle(
                            e.CellBounds.X + padding,
                            e.CellBounds.Y,
                            e.CellBounds.Width - padding * 2,
                            e.CellBounds.Height);

                        TextFormatFlags tff = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                        if (colName == "colName" || colName == "colUrl" || colName == "colLocation" ||
                            colName == "colResolution" || colName == "colGroup")
                            tff |= TextFormatFlags.Left;
                        else
                            tff |= TextFormatFlags.HorizontalCenter;

                        Size textSize = TextRenderer.MeasureText(cellText, baseFont);
                        if (textSize.Width > textRect.Width)
                        {
                            float ratio = (float)textRect.Width / textSize.Width;
                            float newSize = Math.Max(baseFont.Size * ratio, SF(6f));
                            using (Font scaledFont = new Font(baseFont.FontFamily, newSize, baseFont.Style))
                            {
                                TextRenderer.DrawText(e.Graphics, cellText, scaledFont, textRect, textColor, tff);
                            }
                        }
                        else
                        {
                            TextRenderer.DrawText(e.Graphics, cellText, baseFont, textRect, textColor, tff);
                        }
                    }

                    Color sepColor2 = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247);
                    using (Pen pen2 = new Pen(sepColor2, 1))
                        e.Graphics.DrawLine(pen2, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 单元格点击：处理复制/播放双按钮
        /// </summary>
        private void DgvData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            dgvData.ClearSelection();
            dgvData.Rows[e.RowIndex].Selected = true;

            if (e.ColumnIndex == 7)
            {
                Rectangle cellRect = dgvData.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point mousePos = dgvData.PointToClient(Cursor.Position);
                int relX = mousePos.X - cellRect.X;
                int relY = mousePos.Y - cellRect.Y;

                int cellW = cellRect.Width;
                int cellH = cellRect.Height;
                int btnH = SY(26);
                int btnW = SX(46);
                int gap = SX(14);
                int totalW = btnW * 2 + gap;
                int startX = (cellW - totalW) / 2;
                int startY = (cellH - btnH) / 2;

                Rectangle copyBtnRect = new Rectangle(startX, startY, btnW, btnH);
                Rectangle playBtnRect = new Rectangle(startX + btnW + gap, startY, btnW, btnH);

                string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(url)) return;

                if (copyBtnRect.Contains(relX, relY))
                {
                    StartButtonPress(e.RowIndex, 0);
                    CopyTextToClipboard(url, cellRect.Y + cellRect.Height / 2);
                }
                else if (playBtnRect.Contains(relX, relY))
                {
                    StartButtonPress(e.RowIndex, 1);
                    bool hasCustom = !string.IsNullOrWhiteSpace(customPlayerPath) && File.Exists(customPlayerPath);
                    if (hasCustom)
                    {
                        PlayChannelCustom(url);
                    }
                    else
                    {
                        StartPreview(url);
                    }
                }
            }
        }

        private int GetActionBtnIndex(int rowIndex, int x, int y)
        {
            if (rowIndex < 0) return -1;
            var cellRect = dgvData.GetCellDisplayRectangle(7, rowIndex, false);
            if (cellRect.Width <= 0) return -1;
            int relX = x - cellRect.X;
            int relY = y - cellRect.Y;
            int cellW = cellRect.Width;
            int cellH = cellRect.Height;
            int btnH = SY(26);
            int btnW = SX(46);
            int gap = SX(14);
            int totalW = btnW * 2 + gap;
            int startX = (cellW - totalW) / 2;
            int startY = (cellH - btnH) / 2;
            Rectangle copyRect = new Rectangle(startX, startY, btnW, btnH);
            Rectangle playRect = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
            if (copyRect.Contains(relX, relY)) return 0;
            if (playRect.Contains(relX, relY)) return 1;
            return -1;
        }

        private void StartButtonPress(int row, int btn)
        {
            _clickRow = row;
            _clickBtn = btn;
            dgvData.InvalidateCell(7, row);
            if (_btnFlashTimer == null)
            {
                _btnFlashTimer = new System.Windows.Forms.Timer { Interval = 150 };
                _btnFlashTimer.Tick += (s, args) =>
                {
                    int curRow = _clickRow;
                    _clickRow = -1;
                    _clickBtn = -1;
                    _btnFlashTimer.Stop();
                    if (curRow >= 0) dgvData.InvalidateCell(7, curRow);
                };
            }
            _btnFlashTimer.Stop();
            _btnFlashTimer.Start();
        }

        private void DgvData_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 7 && e.RowIndex >= 0)
            {
                var cellRect = dgvData.GetCellDisplayRectangle(7, e.RowIndex, false);
                int btnIdx = GetActionBtnIndex(e.RowIndex, cellRect.X + e.X, cellRect.Y + e.Y);
                int oldHoverRow = _hoverRow;
                int oldHoverBtn = _hoverBtn;
                _hoverRow = btnIdx >= 0 ? e.RowIndex : -1;
                _hoverBtn = btnIdx;
                dgvData.Cursor = btnIdx >= 0 ? Cursors.Hand : Cursors.Default;
                if (oldHoverRow != _hoverRow || oldHoverBtn != _hoverBtn)
                {
                    if (oldHoverRow >= 0) dgvData.InvalidateRow(oldHoverRow);
                    if (_hoverRow >= 0) dgvData.InvalidateRow(_hoverRow);
                }
            }
            else if (e.RowIndex >= 0)
            {
                int oldHoverRow = _hoverRow;
                _hoverRow = e.RowIndex;
                _hoverBtn = -1;
                dgvData.Cursor = Cursors.Default;
                if (oldHoverRow != _hoverRow)
                {
                    if (oldHoverRow >= 0) dgvData.InvalidateRow(oldHoverRow);
                    if (_hoverRow >= 0) dgvData.InvalidateRow(_hoverRow);
                }
            }
            else
            {
                if (_hoverRow != -1)
                {
                    int oldRow = _hoverRow;
                    _hoverRow = -1;
                    _hoverBtn = -1;
                    dgvData.Cursor = Cursors.Default;
                    dgvData.InvalidateRow(oldRow);
                }
            }
        }

        private void DgvData_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 7 && e.RowIndex >= 0)
            {
                var cellRect = dgvData.GetCellDisplayRectangle(7, e.RowIndex, false);
                int btnIdx = GetActionBtnIndex(e.RowIndex, cellRect.X + e.X, cellRect.Y + e.Y);
                if (btnIdx >= 0)
                {
                    _pressRow = e.RowIndex;
                    _pressBtn = btnIdx;
                    dgvData.InvalidateCell(7, e.RowIndex);
                }
            }
        }

        private void DgvData_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_pressRow != -1)
            {
                int oldRow = _pressRow;
                _pressRow = -1;
                _pressBtn = -1;
                dgvData.InvalidateCell(7, oldRow);
            }
        }

        private Panel _toastPanel;
        private System.Windows.Forms.Timer _toastTimer;
        private int _hoverRow = -1;
        private int _hoverBtn = -1;
        private int _pressRow = -1;
        private int _pressBtn = -1;
        private System.Windows.Forms.Timer _btnFlashTimer;
        private int _clickRow = -1;
        private int _clickBtn = -1;

        private void CopyTextToClipboard(string text, int? targetY = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                Clipboard.Clear();
                Clipboard.SetDataObject(text, true, 5, 100);
                ShowCopyToast(text, targetY);
            }
            catch
            {
                try
                {
                    Thread.Sleep(50);
                    Clipboard.SetDataObject(text, true);
                    ShowCopyToast(text, targetY);
                }
                catch
                {
                    DarkMessageBox.Show("复制失败，剪贴板被占用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ShowCopyToast(string url, int? targetY = null)
        {
            bool isDark = theme.Name == "深色";
            Color toastBg = isDark ? Color.FromArgb(60, 63, 70) : Color.FromArgb(245, 245, 245);
            Color toastBorder = isDark ? Color.FromArgb(80, 80, 90) : Color.FromArgb(200, 200, 200);
            Color toastText = isDark ? Color.White : Color.FromArgb(30, 30, 30);

            if (_toastPanel == null)
            {
                _toastPanel = new Panel
                {
                    Size = new Size(SX(260), SY(50)),
                    BackColor = toastBg,
                    Visible = false
                };
                _toastPanel.Paint += (s, pe) =>
                {
                    Graphics g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = _toastPanel.ClientRectangle;
                    using (GraphicsPath path = GetRoundedPath(rect, SX(12)))
                    {
                        _toastPanel.Region = new Region(path);
                        using (SolidBrush br = new SolidBrush(isDark ? Color.FromArgb(60, 63, 70) : Color.FromArgb(245, 245, 245)))
                            g.FillPath(br, path);
                        using (Pen pen = new Pen(isDark ? Color.FromArgb(80, 80, 90) : Color.FromArgb(200, 200, 200), 1))
                            g.DrawPath(pen, path);
                    }
                };

                Label lblIcon = new Label
                {
                    Text = "✓",
                    Font = GetFont(SF(11f), FontStyle.Bold),
                    ForeColor = Color.FromArgb(46, 189, 96),
                    Location = new Point(SX(18), SY(12)),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _toastPanel.Controls.Add(lblIcon);

                Label lblMsg = new Label
                {
                    Text = "复制成功",
                    Font = GetFont(SF(9f), FontStyle.Bold),
                    ForeColor = toastText,
                    Location = new Point(SX(46), SY(10)),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _toastPanel.Controls.Add(lblMsg);

                dgvData.Controls.Add(_toastPanel);
                _toastPanel.BringToFront();

                _toastTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    _toastPanel.Visible = false;
                };
            }
            else
            {
                _toastPanel.BackColor = toastBg;
                foreach (Control ctrl in _toastPanel.Controls)
                {
                    if (ctrl is Label lbl)
                    {
                        lbl.ForeColor = lbl.Text == "✓" ? Color.FromArgb(46, 189, 96) : toastText;
                        lbl.Font = lbl.Text == "✓" ? GetFont(SF(11f), FontStyle.Bold) : GetFont(SF(9f), FontStyle.Bold);
                    }
                }
                _toastPanel.Invalidate();
            }

            int yPos;
            if (targetY.HasValue)
            {
                yPos = targetY.Value - _toastPanel.Height / 2;
                if (yPos < 4) yPos = 4;
                if (yPos > dgvData.ClientSize.Height - _toastPanel.Height - 4)
                    yPos = dgvData.ClientSize.Height - _toastPanel.Height - 4;
            }
            else
            {
                yPos = dgvData.ClientSize.Height / 2 - _toastPanel.Height / 2;
            }

            int xPos = (dgvData.ClientSize.Width - _toastPanel.Width) / 2;
            if (xPos < 4) xPos = 4;

            _toastPanel.Location = new Point(xPos, yPos);
            _toastPanel.Visible = true;
            _toastPanel.BringToFront();
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        /// <summary>
        /// 双击名称列进入编辑重命名
        /// </summary>
        private void DgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                dgvData.BeginEdit(true);
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    ShowReplaceUrlDialog(url);
                }
            }
        }

        /// <summary>
        /// 单元格编辑完成（重命名后同步到数据源）
        /// </summary>
        private void DgvData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString();
                string newName = dgvData.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
                var ch = allChannels.FirstOrDefault(c => c.Url == url);
                if (ch != null && !string.IsNullOrWhiteSpace(newName))
                    ch.Name = newName;
            }
        }

        /// <summary>
        /// 从URL中提取协议、主机、端口、路径
        /// </summary>
        private (string protocol, string host, string port, string path) ParseUrl(string url)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"^(https?|rtmp|rtsp)://([^:/]+)(?::(\d+))?(.*)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return (
                        match.Groups[1].Value.ToLower(),
                        match.Groups[2].Value,
                        match.Groups[3].Value,
                        match.Groups[4].Value
                    );
                }
            }
            catch { }
            return ("", "", "", "");
        }

        /// <summary>
        /// 显示替换IP+端口对话框（单条直播源修复）
        /// </summary>
        private void ShowReplaceUrlDialog(string originalUrl)
        {
            var (protocol, host, port, path) = ParseUrl(originalUrl);
            if (string.IsNullOrEmpty(protocol))
            {
                DarkMessageBox.Show("无法解析此链接格式！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form dlg = new Form())
            {
                dlg.Text = "直播源修复";
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = GetFont(SF(10f));
                dlg.Width = SX(480);
                dlg.Height = SY(320);
                CenterForm(dlg, this);
                dlg.BackColor = theme.Bg;
                SetFormDarkModeTitleBar(dlg, IsDarkColor(theme.Bg));

                Label lblTitle = new Label
                {
                    Text = "将可用IP+端口或网址替换为新地址",
                    Font = GetFont(SF(10f), FontStyle.Bold),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(20), SY(15)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblTitle);

                Label lblOriginal = new Label
                {
                    Text = "原始地址：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextSecondary,
                    Location = new Point(SX(20), SY(50)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblOriginal);

                TextBox txtOriginal = new TextBox
                {
                    Text = originalUrl,
                    ReadOnly = true,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(20), SY(70)),
                    Size = new Size(SX(430), SY(28)),
                    BackColor = theme.BgAlt,
                    ForeColor = theme.TextSecondary
                };
                dlg.Controls.Add(txtOriginal);

                Label lblHost = new Label
                {
                    Text = "新IP/域名：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(20), SY(110)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblHost);

                TextBox txtHost = new TextBox
                {
                    Text = host,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(110), SY(108)),
                    Size = new Size(SX(200), SY(28)),
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.Surface
                };
                dlg.Controls.Add(txtHost);

                Label lblPort = new Label
                {
                    Text = "新端口：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(310), SY(110)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblPort);

                TextBox txtPort = new TextBox
                {
                    Text = port,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(380), SY(108)),
                    Size = new Size(SX(70), SY(28)),
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.Surface
                };
                dlg.Controls.Add(txtPort);

                Label lblPreview = new Label
                {
                    Text = "预览：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextSecondary,
                    Location = new Point(SX(20), SY(150)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblPreview);

                TextBox txtPreview = new TextBox
                {
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(20), SY(170)),
                    Size = new Size(SX(430), SY(28)),
                    ReadOnly = true,
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.BgAlt
                };
                dlg.Controls.Add(txtPreview);

                void UpdatePreview()
                {
                    string newHost = txtHost.Text.Trim();
                    string newPort = txtPort.Text.Trim();
                    string newUrl = protocol + "://" + newHost;
                    if (!string.IsNullOrEmpty(newPort))
                        newUrl += ":" + newPort;
                    newUrl += path;
                    txtPreview.Text = newUrl;
                }

                void AutoParseIpPort(string input)
                {
                    input = input.Trim();
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"^([^:]+):(\d+)$");
                    if (match.Success)
                    {
                        txtHost.Text = match.Groups[1].Value.Trim();
                        txtPort.Text = match.Groups[2].Value.Trim();
                        UpdatePreview();
                    }
                }

                txtHost.TextChanged += (s, e) =>
                {
                    AutoParseIpPort(txtHost.Text);
                    UpdatePreview();
                };
                txtPort.TextChanged += (s, e) => UpdatePreview();
                UpdatePreview();

                Button btnOK = new Button
                {
                    Text = "确定",
                    Font = GetFont(SF(8f)),
                    DialogResult = DialogResult.OK,
                    Location = new Point(SX(270), SY(220)),
                    Size = new Size(SX(85), SY(32))
                };
                StyleOutlineButton(btnOK, 19, theme.Border, theme.TextPrimary);
                dlg.Controls.Add(btnOK);

                Button btnCancel = new Button
                {
                    Text = "取消",
                    Font = GetFont(SF(8f)),
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(SX(365), SY(220)),
                    Size = new Size(SX(85), SY(32))
                };
                StyleOutlineButton(btnCancel, 19, theme.Border, theme.TextPrimary);
                dlg.Controls.Add(btnCancel);

                dlg.AcceptButton = btnOK;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string newHost = txtHost.Text.Trim();
                    string newPort = txtPort.Text.Trim();
                    if (string.IsNullOrEmpty(newHost))
                    {
                        DarkMessageBox.Show("IP/域名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string newUrl = protocol + "://" + newHost;
                    if (!string.IsNullOrEmpty(newPort))
                        newUrl += ":" + newPort;
                    newUrl += path;

                    var ch = allChannels.FirstOrDefault(c => c.Url == originalUrl);
                    if (ch != null)
                    {
                        ch.Url = newUrl;
                        ch.Status = "未检测";
                        ch.Speed = "";
                        RefreshGrid();
                        DarkMessageBox.Show("直播源地址已替换成功！\n\n新地址：\n" + newUrl, "替换成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        /// <summary>
        /// 一键全部替换IP+端口（批量修复直播源）
        /// </summary>
        private void ReplaceAllUrls()
        {
            if (allChannels.Count == 0)
            {
                DarkMessageBox.Show("没有可修复的直播源！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var firstCh = allChannels[0];
            var (protocol, host, port, path) = ParseUrl(firstCh.Url);
            if (string.IsNullOrEmpty(protocol))
            {
                DarkMessageBox.Show("无法解析直播源格式！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form dlg = new Form())
            {
                dlg.Text = "一键全部替换";
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = GetFont(SF(10f));
                dlg.Width = SX(480);
                dlg.Height = SY(360);
                CenterForm(dlg, this);
                dlg.BackColor = theme.Bg;
                SetFormDarkModeTitleBar(dlg, IsDarkColor(theme.Bg));

                Label lblTitle = new Label
                {
                    Text = "一键替换所有直播源的IP+端口",
                    Font = GetFont(SF(10f), FontStyle.Bold),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(20), SY(15)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblTitle);

                Label lblTip = new Label
                {
                    Text = $"共 {allChannels.Count} 条直播源，将替换所有链接的IP和端口\n（保留路径部分不变）",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = Color.Red,
                    Location = new Point(SX(20), SY(45)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblTip);

                Label lblOriginal = new Label
                {
                    Text = "原始地址（示例）：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextSecondary,
                    Location = new Point(SX(20), SY(80)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblOriginal);

                TextBox txtOriginal = new TextBox
                {
                    Text = firstCh.Url,
                    ReadOnly = true,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(20), SY(100)),
                    Size = new Size(SX(430), SY(28)),
                    BackColor = theme.BgAlt,
                    ForeColor = theme.TextSecondary
                };
                dlg.Controls.Add(txtOriginal);

                Label lblHost = new Label
                {
                    Text = "新IP/域名：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(20), SY(140)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblHost);

                TextBox txtHost = new TextBox
                {
                    Text = host,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(110), SY(138)),
                    Size = new Size(SX(200), SY(28)),
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.Surface
                };
                dlg.Controls.Add(txtHost);

                Label lblPort = new Label
                {
                    Text = "新端口：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(SX(310), SY(140)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblPort);

                TextBox txtPort = new TextBox
                {
                    Text = port,
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(380), SY(138)),
                    Size = new Size(SX(70), SY(28)),
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.Surface
                };
                dlg.Controls.Add(txtPort);

                Label lblPreview = new Label
                {
                    Text = "替换后示例：",
                    Font = GetFont(SF(7.5f)),
                    ForeColor = theme.TextSecondary,
                    Location = new Point(SX(20), SY(180)),
                    AutoSize = true
                };
                dlg.Controls.Add(lblPreview);

                TextBox txtPreview = new TextBox
                {
                    Font = GetFont(SF(7f)),
                    Location = new Point(SX(20), SY(200)),
                    Size = new Size(SX(430), SY(28)),
                    ReadOnly = true,
                    ForeColor = theme.TextPrimary,
                    BackColor = theme.BgAlt
                };
                dlg.Controls.Add(txtPreview);

                void UpdatePreview()
                {
                    string newHost = txtHost.Text.Trim();
                    string newPort = txtPort.Text.Trim();
                    string newUrl = protocol + "://" + newHost;
                    if (!string.IsNullOrEmpty(newPort))
                        newUrl += ":" + newPort;
                    newUrl += path;
                    txtPreview.Text = newUrl;
                }

                void AutoParseIpPort(string input)
                {
                    input = input.Trim();
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"^([^:]+):(\d+)$");
                    if (match.Success)
                    {
                        txtHost.Text = match.Groups[1].Value.Trim();
                        txtPort.Text = match.Groups[2].Value.Trim();
                        UpdatePreview();
                    }
                }

                txtHost.TextChanged += (s, e) =>
                {
                    AutoParseIpPort(txtHost.Text);
                    UpdatePreview();
                };
                txtPort.TextChanged += (s, e) => UpdatePreview();
                UpdatePreview();

                Button btnOK = new Button
                {
                    Text = "确定",
                    Font = GetFont(SF(8.5f)),
                    DialogResult = DialogResult.OK,
                    Location = new Point(SX(270), SY(250)),
                    Size = new Size(SX(85), SY(32))
                };
                StyleOutlineButton(btnOK, 19, theme.Border, theme.TextPrimary);
                dlg.Controls.Add(btnOK);

                Button btnCancel = new Button
                {
                    Text = "取消",
                    Font = GetFont(SF(8.5f)),
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(SX(365), SY(250)),
                    Size = new Size(SX(85), SY(32))
                };
                StyleOutlineButton(btnCancel, 19, theme.Border, theme.TextPrimary);
                dlg.Controls.Add(btnCancel);

                dlg.AcceptButton = btnOK;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string newHost = txtHost.Text.Trim();
                    string newPort = txtPort.Text.Trim();
                    if (string.IsNullOrEmpty(newHost))
                    {
                        DarkMessageBox.Show("IP/域名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int replaced = 0;
                    int failed = 0;
                    foreach (var ch in allChannels)
                    {
                        var (p, h, pt, path2) = ParseUrl(ch.Url);
                        if (string.IsNullOrEmpty(p))
                        {
                            failed++;
                            continue;
                        }
                        string newUrl = p + "://" + newHost;
                        if (!string.IsNullOrEmpty(newPort))
                            newUrl += ":" + newPort;
                        newUrl += path2;
                        ch.Url = newUrl;
                        ch.Status = "未检测";
                        ch.Speed = "";
                        replaced++;
                    }

                    RefreshGrid();
                    string msg = $"批量替换完成！\n成功替换: {replaced} 条";
                    if (failed > 0)
                        msg += $"\n跳过(格式不支持): {failed} 条";
                    DarkMessageBox.Show(msg, "替换完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 右键菜单重命名：选中行进入编辑
        /// </summary>
        private void BeginRenameSelected()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                int idx = dgvData.SelectedRows[0].Index;
                dgvData.CurrentCell = dgvData.Rows[idx].Cells[0];
                dgvData.BeginEdit(true);
            }
            else DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 解析延迟字符串为毫秒数（排序用）
        /// </summary>
        private int ParseSpeed(string speed)
        {
            if (string.IsNullOrWhiteSpace(speed)) return int.MaxValue;
            if (speed == "超时") return int.MaxValue - 1;
            string num = new string(speed.TakeWhile(c => char.IsDigit(c)).ToArray());
            if (int.TryParse(num, out int ms)) return ms;
            return int.MaxValue;
        }

        /// <summary>
        /// 弹出播放方式选择菜单
        /// </summary>
        private void ShowPlayMenu(string url)
        {
            ContextMenuStrip playMenu = new ContextMenuStrip();
            playMenu.Font = GetFont(SF(9f));
            playMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg)));
            playMenu.BackColor = theme.Surface;
            playMenu.ForeColor = theme.TextPrimary;
            playMenu.Items.Add("系统默认播放器", null, (s, ev) => PlayChannelDefault(url));
            bool hasFFplay = !string.IsNullOrWhiteSpace(ffplayPath) && File.Exists(ffplayPath);
            var ffplayItem = new ToolStripMenuItem(hasFFplay ? $"FFplay播放 ({ffplayPath})" : "FFplay播放(未找到ffplay)");
            ffplayItem.Enabled = hasFFplay;
            ffplayItem.Click += (s, ev) => PlayChannelFFplay(url);
            playMenu.Items.Add(ffplayItem);
            bool hasCustom = !string.IsNullOrWhiteSpace(customPlayerPath) && File.Exists(customPlayerPath);
            var customItem = new ToolStripMenuItem(hasCustom ? $"第三方播放器 ({Path.GetFileName(customPlayerPath)})" : "第三方播放器(未设置)");
            customItem.Enabled = hasCustom;
            customItem.Click += (s, ev) => PlayChannelCustom(url);
            playMenu.Items.Add(customItem);
            playMenu.Items.Add(new ToolStripSeparator());
            playMenu.Items.Add("设置第三方播放器路径...", null, (s, ev) => SetCustomPlayerPath());
            if (hasFFplay)
            {
                playMenu.Items.Add(new ToolStripSeparator());
                var autoItem = new ToolStripMenuItem("自动(优先FFplay)");
                autoItem.Click += (s, ev) => { try { PlayChannelFFplay(url); } catch { PlayChannelDefault(url); } };
                playMenu.Items.Add(autoItem);
            }
            Point p = dgvData.PointToClient(Cursor.Position);
            playMenu.Show(dgvData, p);
        }

        /// <summary>
        /// 系统默认播放器播放
        /// </summary>
        private void PlayChannelDefault(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show($"无法使用系统播放器打开链接：\n{ex.Message}", "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// FFplay播放
        /// </summary>
        private void KillRunningPlayer()
        {
            try
            {
                _ffplayOutputCts?.Cancel();
                System.Threading.Thread.Sleep(100);
            }
            catch { }

            try
            {
                if (_runningPlayer != null && !_runningPlayer.HasExited)
                {
                    _runningPlayer.Kill();
                    _runningPlayer.WaitForExit(2000);
                }
            }
            catch { }
            finally
            {
                try { _runningPlayer?.Dispose(); } catch { }
                _runningPlayer = null;
                try { _ffplayOutputCts?.Dispose(); } catch { }
                _ffplayOutputCts = null;
                if (lblStreamInfo != null) lblStreamInfo.Visible = false;
                StopStreamInfoOverlay();
                _showStreamInfoOverlay = false;
            }
        }

        private IntPtr embeddedPreviewHwnd = IntPtr.Zero;

        private void StartPreview(string url)
        {
            if (string.IsNullOrWhiteSpace(ffplayPath) || !File.Exists(ffplayPath))
            {
                DarkMessageBox.Show("未找到 ffplay.exe，无法预览。请确保 FFmpeg 组件已安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StopPreview();

            try
            {
                int playWidth = 800;
                int playHeight = 600;

                string ffplayArgs = BuildFfplayArguments(url);
                ffplayArgs = ffplayArgs.Replace("\"" + url + "\"", $"\"{url}\"") + $" -x {playWidth} -y {playHeight}";

                var psi = new ProcessStartInfo
                {
                    FileName = ffplayPath,
                    Arguments = ffplayArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                previewProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                previewProcess.Exited += (s, e) =>
                {
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            embeddedPreviewHwnd = IntPtr.Zero;
                            previewProcess = null;
                        }));
                    }
                    catch { }
                };

                previewProcess.Start();

                StartMouseHook();
                _ = ReadFfplayOutputAsync(previewProcess);

                System.Threading.Tasks.Task.Run(() =>
                {
                    int attempts = 0;
                    while (attempts < 20)
                    {
                        if (previewProcess == null || previewProcess.HasExited)
                            return;

                        IntPtr targetHwnd = IntPtr.Zero;
                        uint targetPid = (uint)previewProcess.Id;

                        EnumWindows((hWnd, lParam) =>
                        {
                            if (!IsWindowVisible(hWnd)) return true;

                            uint pid;
                            GetWindowThreadProcessId(hWnd, out pid);
                            if (pid != targetPid) return true;

                            System.Text.StringBuilder className = new System.Text.StringBuilder(256);
                            GetClassName(hWnd, className, className.Capacity);
                            string clsName = className.ToString().ToLower();

                            if (clsName.Contains("sdl") || clsName.Contains("ffplay"))
                            {
                                targetHwnd = hWnd;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (targetHwnd != IntPtr.Zero)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                const int GWL_STYLE = -16;
                                const int WS_CAPTION = 0x00C00000;
                                const int WS_THICKFRAME = 0x00040000;
                                const int WS_MINIMIZEBOX = 0x00020000;
                                const int WS_MAXIMIZEBOX = 0x00010000;
                                const int WS_SYSMENU = 0x00080000;
                                const int WS_EX_TOPMOST = 0x00000008;
                                const int WS_EX_APPWINDOW = 0x00040000;
                                const int GWL_EXSTYLE = -20;

                                IntPtr stylePtr = SafeGetWindowLongPtr(targetHwnd, GWL_STYLE);
                                int style = (int)stylePtr;
                                style |= WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU;
                                SafeSetWindowLongPtr(targetHwnd, GWL_STYLE, (IntPtr)style);

                                IntPtr exStylePtr = SafeGetWindowLongPtr(targetHwnd, GWL_EXSTYLE);
                                int exStyle = (int)exStylePtr;
                                exStyle |= WS_EX_TOPMOST | WS_EX_APPWINDOW;
                                SafeSetWindowLongPtr(targetHwnd, GWL_EXSTYLE, (IntPtr)exStyle);

                                SetWindowPos(targetHwnd, (IntPtr)(-1), 0, 0, playWidth, playHeight, 0x0004 | 0x0020);

                                embeddedPreviewHwnd = targetHwnd;

                                ShowWindow(targetHwnd, 3);
                            }));
                            return;
                        }

                        attempts++;
                        System.Threading.Thread.Sleep(250);
                    }
                });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show("预览失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopPreview()
        {
            try
            {
                if (previewResizeTimer != null)
                {
                    previewResizeTimer.Stop();
                    previewResizeTimer.Dispose();
                    previewResizeTimer = null;
                }
                if (embeddedPreviewHwnd != IntPtr.Zero)
                {
                    SetParent(embeddedPreviewHwnd, IntPtr.Zero);
                    embeddedPreviewHwnd = IntPtr.Zero;
                }
                if (previewProcess != null && !previewProcess.HasExited)
                {
                    previewProcess.Kill();
                    previewProcess.WaitForExit(1000);
                }
            }
            catch { }
            finally
            {
                try { previewProcess?.Dispose(); } catch { }
                previewProcess = null;
                embeddedPreviewHwnd = IntPtr.Zero;
                try { previewResizeTimer?.Dispose(); } catch { }
                previewResizeTimer = null;
                try { _ffplayOutputCts?.Cancel(); } catch { }
                try { _ffplayOutputCts?.Dispose(); } catch { }
                _ffplayOutputCts = null;
                StopStreamInfoOverlay();
                _showStreamInfoOverlay = false;
            }
        }

        private void PlayChannelFFplay(string url)
        {
            try
            {
                KillRunningPlayer();

                if (dgvData.SelectedRows.Count > 0)
                {
                    var row = dgvData.SelectedRows[0];
                    _currentChannelName = row.Cells[0].Value?.ToString() ?? "";
                }

                if (string.IsNullOrWhiteSpace(_currentResolution) && dgvData.SelectedRows.Count > 0)
                {
                    var row = dgvData.SelectedRows[0];
                    string backupResolution = row.Cells[3].Value?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(backupResolution) && backupResolution != "0x0" && backupResolution != "未检测")
                    {
                        _currentResolution = backupResolution;
                    }
                }

                if (string.IsNullOrWhiteSpace(ffplayPath) || !File.Exists(ffplayPath))
                {
                    FindFFplay();
                }
                string playerPath = ffplayPath;
                if (string.IsNullOrWhiteSpace(playerPath) || !File.Exists(playerPath))
                {
                    playerPath = customPlayerPath;
                }
                if (string.IsNullOrWhiteSpace(playerPath) || !File.Exists(playerPath))
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "播放器程序|*.exe|所有文件|*.*";
                        ofd.Title = "未找到FFplay，请选择播放器exe文件（ffplay.exe/vlc.exe/potplayer/mpv.exe等）";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            string selected = ofd.FileName;
                            string fname = Path.GetFileName(selected).ToLower();
                            if (fname == "ffplay.exe")
                                ffplayPath = selected;
                            else
                                customPlayerPath = selected;
                            playerPath = selected;
                        }
                        else
                        {
                            PlayChannelDefault(url);
                            return;
                        }
                    }
                }
                bool isFfplay = Path.GetFileName(playerPath).ToLower() == "ffplay.exe";
                _runningPlayer = new Process();
                string ffplayArgs = BuildFfplayArguments(url);
                _runningPlayer.StartInfo = new ProcessStartInfo
                {
                    FileName = playerPath,
                    Arguments = isFfplay ? ffplayArgs : $"\"{url}\"",
                    UseShellExecute = false,
                    CreateNoWindow = !isFfplay,
                    WindowStyle = isFfplay ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = isFfplay,
                    RedirectStandardError = isFfplay
                };
                _runningPlayer.EnableRaisingEvents = true;
                _runningPlayer.Exited += (s, e) =>
                {
                    try
                    {
                        if (_runningPlayer == s) _runningPlayer = null;
                        ((Process)s)?.Dispose();
                    }
                    catch { }
                };
                _runningPlayer.Start();

                StartMouseHook();

                if (isFfplay)
                {
                    _ = ReadFfplayOutputAsync(_runningPlayer);
                }
            }
            catch
            {
                try { _runningPlayer = null; PlayChannelDefault(url); } catch { }
            }
        }

        private string BuildFfplayArguments(string url)
        {
            System.Text.StringBuilder args = new System.Text.StringBuilder();
            args.Append("-autoexit ");
            args.Append("-stats ");

            // 实时播放优化参数 - 减少缓冲，降低延迟
            args.Append("-fflags +fastseek+genpts+nobuffer ");
            args.Append("-flags +low_delay ");
            args.Append("-framedrop ");
            args.Append("-avioflags direct ");
            args.Append("-rtbufsize 64000 ");
            args.Append("-sync ext ");
            args.Append("-probesize 500000 ");
            args.Append("-analyzeduration 500000 ");
            args.Append("-max_delay 0 ");

            if (url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0 || url.IndexOf("/hls/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                args.Append("-allowed_extensions ALL ");
            }

            if (url.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
            {
                args.Append("-rtsp_transport tcp ");
            }

            if (url.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase))
            {
                // genpts已在fflags中
            }

            args.Append($"\"{url}\"");
            return args.ToString();
        }

        private async System.Threading.Tasks.Task ReadFfplayOutputAsync(Process proc)
        {
            if (proc == null) return;
            if (!proc.StartInfo.RedirectStandardError) return;

            StreamReader reader = null;
            try
            {
                reader = proc.StandardError;
            }
            catch { return; }

            if (reader == null) return;

            _ffplayOutputCts = new System.Threading.CancellationTokenSource();
            var token = _ffplayOutputCts.Token;

            try
            {
                string line;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        line = await reader.ReadLineAsync();
                        if (line == null) break;
                        if (!string.IsNullOrWhiteSpace(line) && line.Length > 5)
                        {
                            if (line.Contains("Video:") || line.Contains("Audio:") || 
                                line.Contains("frame=") || line.Contains("fps=") || 
                                line.Contains("time=") || line.Contains("bitrate=") ||
                                line.Contains("speed=") || line.Contains("KB queue:") ||
                                line.Contains("dropped") || line.Contains("size="))
                            {
                                ParseFfplayStats(line);
                            }
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch { }
        }

        private void ParseFfplayStats(string line)
        {
            try
            {
                string codec = "";
                string resolution = "";
                string fps = "";
                string bitrate = "";
                string audioChannels = "";
                string audioSampleRate = "";
                string delay = "";
                string frameCount = "";
                string currentTime = "";
                string speed = "";
                string buffer = "";

                if (line.Contains("Video:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Video:\s*([^\s,]+)");
                    if (match.Success) codec = match.Groups[1].Value.ToUpper();

                    // 先排除 [0x0] 等流索引标识，再匹配实际分辨率
                    string videoPart = line;
                    int videoIdx = videoPart.IndexOf("Video:");
                    if (videoIdx >= 0) videoPart = videoPart.Substring(videoIdx);
                    // 移除 [0x0] 格式的流索引
                    videoPart = System.Text.RegularExpressions.Regex.Replace(videoPart, @"\[\d+x\d+\]", "");
                    match = System.Text.RegularExpressions.Regex.Match(videoPart, @"(\d{2,5})x(\d{2,5})");
                    if (match.Success) resolution = $"{match.Groups[1].Value}x{match.Groups[2].Value}";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+(?:\.\d+)?)\s*fps");
                    if (match.Success) fps = $"{match.Groups[1].Value} FPS";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"delay\s*=\s*([\d.]+)");
                    if (match.Success) delay = $"{match.Groups[1].Value}s";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"SAR\s*(\d+:\d+)");
                    if (match.Success) _currentSar = match.Groups[1].Value;

                    match = System.Text.RegularExpressions.Regex.Match(line, @"DAR\s*(\d+:\d+)");
                    if (match.Success) _currentDar = match.Groups[1].Value;
                }

                if (line.Contains("Audio:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Audio:\s*([^\s,]+)");
                    if (match.Success && !string.IsNullOrEmpty(codec))
                        codec += $" + {match.Groups[1].Value.ToUpper()}";
                    else if (match.Success)
                        codec = match.Groups[1].Value.ToUpper();

                    match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*Hz");
                    if (match.Success) audioSampleRate = $"{match.Groups[1].Value} Hz";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"(mono|stereo|surround|5\.1)");
                    if (match.Success) audioChannels = match.Groups[1].Value;

                    if (string.IsNullOrEmpty(audioChannels))
                    {
                        match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*channels?");
                        if (match.Success) audioChannels = $"{match.Groups[1].Value}声道";
                    }

                    match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*bps");
                    if (match.Success) _currentAudioBitdepth = $"{match.Groups[1].Value} bps";
                }

                if (line.Contains("frame="))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"frame=\s*(\d+)");
                    if (match.Success) frameCount = match.Groups[1].Value;

                    match = System.Text.RegularExpressions.Regex.Match(line, @"fps=\s*([\d.]+)");
                    if (match.Success) fps = $"{match.Groups[1].Value} FPS";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"time=\s*([\d:.]+)");
                    if (match.Success) currentTime = match.Groups[1].Value;

                    match = System.Text.RegularExpressions.Regex.Match(line, @"bitrate=\s*([\d.]+)");
                    if (match.Success) bitrate = $"{match.Groups[1].Value} kb/s";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"speed=\s*([\d.]+)x?");
                    if (match.Success) speed = $"{match.Groups[1].Value}x";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"size=\s*(\d+)");
                    if (match.Success) _currentSize = $"{match.Groups[1].Value} bytes";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"decoded=\s*(\d+)");
                    if (match.Success) _currentDecodedFrames = $"已解码: {match.Groups[1].Value}";

                    match = System.Text.RegularExpressions.Regex.Match(line, @"displayed=\s*(\d+)");
                    if (match.Success) _currentDisplayedFrames = $"已显示: {match.Groups[1].Value}";
                }

                if (line.Contains("KB queue:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"KB queue:\s*(\d+)");
                    if (match.Success) buffer = $"{match.Groups[1].Value} KB";
                }

                if (line.Contains("dropped"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"dropped\s*=\s*(\d+)");
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[1].Value, out _droppedFrames);
                    }
                    match = System.Text.RegularExpressions.Regex.Match(line, @"total\s*=\s*(\d+)");
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[1].Value, out _totalFrames);
                    }
                }

                UpdateStreamInfoDisplay(codec, resolution, fps, bitrate, audioChannels, audioSampleRate, delay, frameCount, currentTime, speed, buffer);
            }
            catch { }
        }

        private void UpdateStreamInfoDisplay(string codec, string resolution, string fps, string bitrate, string audioChannels = "", string audioSampleRate = "", string delay = "", string frameCount = "", string currentTime = "", string speed = "", string buffer = "")
        {
            if (!string.IsNullOrEmpty(codec)) _currentCodec = codec;
            if (!string.IsNullOrEmpty(resolution)) _currentResolution = resolution;
            if (!string.IsNullOrEmpty(fps)) _currentFps = fps;
            if (!string.IsNullOrEmpty(bitrate)) _currentBitrate = bitrate;
            if (!string.IsNullOrEmpty(audioChannels)) _currentAudioChannels = audioChannels;
            if (!string.IsNullOrEmpty(audioSampleRate)) _currentAudioSampleRate = audioSampleRate;
            if (!string.IsNullOrEmpty(delay)) _currentDelay = delay;
            if (!string.IsNullOrEmpty(frameCount)) _currentFrameCount = frameCount;
            if (!string.IsNullOrEmpty(currentTime)) _currentTime = currentTime;
            if (!string.IsNullOrEmpty(speed)) _currentSpeed = speed;
            if (!string.IsNullOrEmpty(buffer)) _currentBuffer = buffer;

            if (lblStreamInfo != null)
            {
                lblStreamInfo.Visible = false;
            }

            if (_showStreamInfoOverlay && _streamInfoLabel != null && !_streamInfoLabel.IsDisposed)
            {
                UpdateStreamInfoOverlay();
            }
        }

        /// <summary>
        /// 第三方播放器播放
        /// </summary>
        private void PlayChannelCustom(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customPlayerPath) || !File.Exists(customPlayerPath))
                {
                    DarkMessageBox.Show("未设置第三方播放器路径或文件不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetCustomPlayerPath();
                    return;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = customPlayerPath,
                    Arguments = $"\"{url}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show($"第三方播放器播放失败：\n{ex.Message}", "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 设置第三方播放器路径
        /// </summary>
        private void SetCustomPlayerPath()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "可执行文件|*.exe|所有文件|*.*";
                ofd.Title = "选择播放器exe文件（如vlc.exe、mpv.exe、potplayer等）";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    customPlayerPath = ofd.FileName;
                    DarkMessageBox.Show($"已设置第三方播放器：\n{customPlayerPath}", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 分组筛选改变
        /// </summary>
        private void CboGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshGrid();
            UpdateEmptyState();
        }

        /// <summary>
        /// 根据allChannels和筛选条件刷新表格
        /// </summary>
        private void RefreshGrid()
        {
            SendMessage(dgvData.Handle, WM_SETREDRAW, 0, 0);
            dgvData.SuspendLayout();
            try
            {
                string selectedGroup = cboGroup?.SelectedItem?.ToString() ?? "全部";
                string searchText = GetSearchText();

                List<ChannelInfo> filteredChannels = new List<ChannelInfo>();
                foreach (var ch in allChannels)
                {
                    string chGroup = string.IsNullOrWhiteSpace(ch.Group) ? "未分组" : ch.Group;
                    bool matchGroup = selectedGroup == "全部" || chGroup == selectedGroup;
                    bool matchSearch = string.IsNullOrWhiteSpace(searchText) ||
                        ch.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        MatchPinyinAbbreviation(ch.Name, searchText);
                    if (matchGroup && matchSearch)
                    {
                        filteredChannels.Add(ch);
                    }
                }

                if (dgvData.Rows.Count != filteredChannels.Count)
                {
                    dgvData.Rows.Clear();
                    if (filteredChannels.Count > 0)
                        dgvData.Rows.Add(filteredChannels.Count);
                }

                for (int i = 0; i < filteredChannels.Count; i++)
                {
                    var ch = filteredChannels[i];
                    var row = dgvData.Rows[i];
                    row.Cells[0].Value = ch.Name;
                    row.Cells[1].Value = ch.Url;
                    row.Cells[2].Value = ch.Location;
                    row.Cells[3].Value = ch.Resolution;
                    row.Cells[4].Value = ch.Speed;
                    row.Cells[5].Value = string.IsNullOrWhiteSpace(ch.Group) ? "未分组" : ch.Group;
                    row.Cells[6].Value = ch.Status;
                    row.Cells[7].Value = "";
                }
            }
            finally
            {
                dgvData.ResumeLayout();
                SendMessage(dgvData.Handle, WM_SETREDRAW, 1, 0);
                dgvData.Invalidate();
                UpdateActionButtonsVisibility();
            }
        }

        /// <summary>
        /// 获取搜索框文本（忽略占位符）
        /// </summary>
        private string GetSearchText()
        {
            if (txtSearchBox == null) return "";
            if (txtSearchBox.Text == "输入搜索内容，按下回车键搜索") return "";
            return txtSearchBox.Text;
        }

        /// <summary>
        /// 拼音缩写匹配
        /// </summary>
        private bool MatchPinyinAbbreviation(string name, string keyword)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(keyword))
                return false;

            string abbreviation = GetPinyinAbbreviation(name);
            return abbreviation.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 获取中文名称的拼音首字母缩写
        /// </summary>
        private string GetPinyinAbbreviation(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if (IsChineseChar(c))
                {
                    sb.Append(GetPinyinFirstLetter(c));
                }
                else if (char.IsLetter(c))
                {
                    sb.Append(char.ToLower(c));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 判断是否为中文字符
        /// </summary>
        private bool IsChineseChar(char c)
        {
            return c >= '\u4e00' && c <= '\u9fff';
        }

        /// <summary>
        /// 获取中文字符的拼音首字母
        /// </summary>
        private char GetPinyinFirstLetter(char c)
        {
            if (!IsChineseChar(c))
                return char.ToLower(c);

            string[] pinyinTable = { "啊", "芭", "擦", "搭", "蛾", "发", "噶", "哈", "击", "喀", "垃", "妈", "拿", "哦", "啪", "期", "然", "撒", "塌", "挖", "昔", "压", "匝" };
            char[] letters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'w', 'x', 'y', 'z' };

            for (int i = 0; i < pinyinTable.Length; i++)
            {
                if (c <= pinyinTable[i][0])
                {
                    return letters[i];
                }
            }
            return 'z';
        }

        /// <summary>
        /// 更新分组下拉框选项
        /// </summary>
        private void UpdateGroupFilter()
        {
            var groups = allChannels.Select(c => string.IsNullOrWhiteSpace(c.Group) ? "未分组" : c.Group)
                .Distinct().OrderBy(g => g).ToList();
            cboGroup.Items.Clear();
            cboGroup.Items.Add("全部");
            foreach (var g in groups) cboGroup.Items.Add(g);
            cboGroup.SelectedIndex = 0;
            cboGroup.Visible = allChannels.Count > 0;
            cboGroupHost.Visible = allChannels.Count > 0;
            if (lblGroupFilter != null) lblGroupFilter.Visible = allChannels.Count > 0;
            if (allChannels.Count > 0 && searchPanelRef != null)
            {
                int rightAreaWidth = 328;
                int leftMargin = 98;
                if (searchBoxHostRef != null)
                {
                    searchBoxHostRef.Left = leftMargin;
                    searchBoxHostRef.Width = searchPanelRef.ClientSize.Width - leftMargin - rightAreaWidth;
                    if (txtSearchBox != null) txtSearchBox.Width = searchBoxHostRef.Width - 20;
                    searchBoxHostRef.Invalidate();
                }
                if (lblGroupFilter != null) lblGroupFilter.Left = searchPanelRef.ClientSize.Width - 298;
                cboGroupHost.Left = searchPanelRef.ClientSize.Width - 158;
                cboGroupHost.Width = 130;
                if (searchBoxHostRef != null) cboGroupHost.Top = searchBoxHostRef.Top;
                cboGroupHost.Invalidate();
            }
        }

        /// <summary>
        /// 重新计算统计数据
        /// </summary>
        private void RecalcStats()
        {
            detectedCount = allChannels.Count(c => c.Status != "未检测" && c.Status != "检测中");
            availableCount = allChannels.Count(c => c.Status == "可用");
        }

        /// <summary>
        /// 选择文件
        /// </summary>
        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "m3u/txt文件|*.m3u;*.txt|m3u文件|*.m3u|txt文件|*.txt|所有文件|*.*";
                ofd.Title = "选择m3u或txt文件";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    int beforeCount = allChannels.Count;
                    int newCount = 0;
                    int dupCount = 0;
                    HashSet<string> existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));
                    foreach (string file in ofd.FileNames)
                    {
                        var result = ImportFromFile(file, existingUrls);
                        newCount += result.newItems;
                        dupCount += result.dupItems;
                    }
                    totalCount = allChannels.Count;
                    UpdateGroupFilter();
                    RefreshGrid();
                    UpdateStatusBar();
                    UpdateEmptyState();
                    UpdateActionButtonsVisibility();
                    string msg = $"成功导入 {newCount} 个频道";
                    if (beforeCount > 0) msg += $"（追加到列表，总计 {totalCount} 个）";
                    if (dupCount > 0) msg += $"\n跳过重复链接 {dupCount} 个";
                    DarkMessageBox.Show(msg, "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 开始/暂停/继续检测
        /// </summary>
        private async void BtnStartDetect_Click(object sender, EventArgs e)
        {
            if (isDetecting && !isPaused)
            {
                isPaused = true;
                btnStartDetect.Text = "▶ 继续检测";
                btnStartDetect.BackColor = ColorGreen;
                btnStartDetect.ForeColor = Color.White;
                return;
            }
            if (isDetecting && isPaused)
            {
                isPaused = false;
                btnStartDetect.Text = "⏸ 暂停检测";
                btnStartDetect.BackColor = ColorOrange;
                btnStartDetect.ForeColor = Color.White;
                return;
            }
            if (allChannels.Count == 0)
            {
                DarkMessageBox.Show("请先导入频道数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await CheckAndDownloadComponentsAsync();

            isDetecting = true;
            isPaused = false;
            btnStartDetect.Text = "⏸ 暂停检测";
            btnStartDetect.BackColor = ColorOrange;
            btnStartDetect.ForeColor = Color.White;
            if (btnScanSource != null) btnScanSource.Enabled = false;

            await StartDetection();

            isDetecting = false;
            isPaused = false;
            btnStartDetect.Text = "⏺ 开始检测";
            btnStartDetect.BackColor = theme.InfoColor;
            btnStartDetect.ForeColor = Color.White;
            if (btnScanSource != null) btnScanSource.Enabled = true;
        }

        /// <summary>
        /// 导出
        /// </summary>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (allChannels.Count == 0)
            {
                DarkMessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "m3u文件(标准)|*.m3u|m3u文件(按名称分组)|*.m3u|txt文件(标准)|*.txt|txt文件(合并频道)|*.txt";
                sfd.Title = "选择导出格式";
                sfd.FileName = "channels";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    int filterIdx = sfd.FilterIndex;
                    if (filterIdx == 1)
                        ExportToM3u(sfd.FileName);
                    else if (filterIdx == 2)
                        ExportToM3uMergeGroup(sfd.FileName);
                    else if (filterIdx == 3)
                        ExportToTxt(sfd.FileName, false);
                    else
                        ExportToTxtMergeUrl(sfd.FileName);
                }
            }
        }

        /// <summary>
        /// 更新状态栏药丸形状Region
        /// </summary>
        private void UpdateStatusBarRegion()
        {
            if (statusBarRef == null || statusBarRef.Width <= 0 || statusBarRef.Height <= 0) return;
            int r = statusBarRef.Height / 2;
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, statusBarRef.Width - 1, statusBarRef.Height - 1), r))
            {
                statusBarRef.Region = new Region(path);
            }
        }

        /// <summary>
        /// 三等分布局状态栏标签
        /// </summary>
        private void LayoutStatusBar(Panel statusBar)
        {
            if (lblDetected == null || lblAvailable == null || lblProgressText == null || lblPercent == null) return;
            int w = statusBar.ClientSize.Width;
            int h = statusBar.ClientSize.Height;
            if (w <= 0) return;

            int padLeft = SX(20);
            int padRight = SX(20);
            int gap = SX(40);
            int padY = (h - lblDetected.Height) / 2;
            if (padY < 0) padY = 0;

            lblDetected.Location = new Point(padLeft, padY);
            lblAvailable.Location = new Point(lblDetected.Right + gap, padY);

            if (lblStreamInfo != null && lblStreamInfo.Visible && !string.IsNullOrEmpty(lblStreamInfo.Text))
            {
                lblStreamInfo.Location = new Point(lblAvailable.Right + gap, padY);
            }

            int progTotalW = lblProgressText.Width + SX(6) + lblPercent.Width;
            int progX;
            if (lblProgressText.Text.Contains("华视美达"))
            {
                progX = (w - progTotalW) / 2;
            }
            else
            {
                progX = w - padRight - progTotalW;
            }
            lblProgressText.Location = new Point(progX, padY);
            lblPercent.Location = new Point(progX + lblProgressText.Width + SX(6), padY);

            statusBarRef.Invalidate();
        }

        private void ApplyTheme()
        {
            this.SuspendLayout();
            try
            {
                this.BackColor = theme.Border;
                if (outerWrap != null)
                {
                    outerWrap.BackColor = theme.Border;
                }
                if (titleBarPanel != null)
                {
                    titleBarPanel.BackColor = theme.Bg;
                    Color titleBtnHover = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235);
                    Color titleBtnFg = theme.TextSecondary;
                    if (btnThemeToggle != null)
                    {
                        btnThemeToggle.BackColor = theme.Bg;
                        btnThemeToggle.FlatAppearance.MouseOverBackColor = titleBtnHover;
                    }
                    if (btnMin != null) { btnMin.BackColor = theme.Bg; btnMin.FlatAppearance.MouseOverBackColor = titleBtnHover; }
                    if (btnMax != null) { btnMax.BackColor = theme.Bg; btnMax.FlatAppearance.MouseOverBackColor = titleBtnHover; }
                    if (btnClose != null) { btnClose.BackColor = theme.Bg; btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35); btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30); }
                    if (btnThemeToggle != null) { btnThemeToggle.Invalidate(); }

                    Color navBtnText = IsDarkColor(theme.Bg) ? Color.White : Color.Black;
                    navBtnHoverBg = IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
                    if (btnNavDetect != null) { btnNavDetect.ForeColor = navBtnText; }
                    if (btnNavSearch != null) { btnNavSearch.ForeColor = navBtnText; }
                    if (btnNavSettings != null) { btnNavSettings.ForeColor = navBtnText; }
                    if (btnNavAbout != null) { btnNavAbout.ForeColor = navBtnText; }
                }
                if (bottomBarRef != null)
                {
                    bottomBarRef.BackColor = theme.Bg;
                }
                if (mainArea != null)
                {
                    mainArea.BackColor = theme.BgAlt;
                    foreach (Control c in mainArea.Controls) { ApplyThemeToControl(c); }
                }
                if (actionArea != null)
                {
                    actionArea.BackColor = theme.BgAlt;
                    foreach (Control c in actionArea.Controls) { ApplyThemeToControl(c); }
                    if (tipBox != null) tipBox.Invalidate();
                }
                if (actionSepRef != null) actionSepRef.BackColor = theme.Border;
                if (dgvData != null)
                {
                    dgvData.BackgroundColor = theme.BgAlt;
                    dgvData.GridColor = theme.Border;
                    dgvData.ColumnHeadersDefaultCellStyle.BackColor = theme.HeaderBg;
                    dgvData.ColumnHeadersDefaultCellStyle.ForeColor = theme.TextPrimary;
                    dgvData.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.Primary;
                    dgvData.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
                    dgvData.RowsDefaultCellStyle.BackColor = theme.Surface;
                    dgvData.RowsDefaultCellStyle.ForeColor = theme.TextPrimary;
                    dgvData.RowsDefaultCellStyle.SelectionBackColor = theme.SelectRow;
                    dgvData.RowsDefaultCellStyle.SelectionForeColor = theme.SelectRowText;
                    dgvData.AlternatingRowsDefaultCellStyle.BackColor = theme.Surface;
                    dgvData.AlternatingRowsDefaultCellStyle.ForeColor = theme.TextPrimary;
                    dgvData.AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.SelectRow;
                    dgvData.AlternatingRowsDefaultCellStyle.SelectionForeColor = theme.SelectRowText;
                    dgvData.DefaultCellStyle.SelectionBackColor = theme.SelectRow;
                    dgvData.DefaultCellStyle.SelectionForeColor = theme.SelectRowText;
                }
                if (statusBarContainer != null)
                {
                    statusBarContainer.BackColor = theme.Bg;
                }
                if (statusBarRef != null)
                {
                    statusBarRef.BackColor = theme.StatusBarBg;
                    foreach (Control c in statusBarRef.Controls) { ApplyThemeToControl(c); }
                    UpdateStatusBarRegion();
                }
                if (searchPanelRef != null)
                {
                    searchPanelRef.BackColor = theme.BgAlt;
                    foreach (Control c in searchPanelRef.Controls) { ApplyThemeToControl(c); }
                    if (searchBoxHostRef != null) searchBoxHostRef.BackColor = theme.Surface;
                    if (cboGroupHost != null) cboGroupHost.BackColor = theme.BgAlt;
                }
                if (gridContainerRef != null)
                {
                    gridContainerRef.BackColor = theme.BgAlt;
                }
                if (cboGroup != null)
                {
                    cboGroup.BackColor = theme.Surface;
                    cboGroup.ForeColor = theme.TextPrimary;
                    if (cboGroup is DarkComboBox dcbo)
                    {
                        dcbo.BorderColor = theme.Border;
                        dcbo.FocusBorderColor = theme.Primary;
                        dcbo.ItemBackColor = theme.Surface;
                        dcbo.ItemSelectedBackColor = theme.BgAlt;
                        dcbo.ItemHoverBackColor = Color.FromArgb(Math.Min(255, theme.Surface.R + 10), Math.Min(255, theme.Surface.G + 10), Math.Min(255, theme.Surface.B + 10));
                    }
                }
                if (emptyLabel != null) emptyLabel.ForeColor = theme.TextSecondary;

                if (dataGridViewContextMenu != null)
                {
                    dataGridViewContextMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg)));
                    dataGridViewContextMenu.BackColor = theme.Surface;
                    dataGridViewContextMenu.ForeColor = theme.TextPrimary;
                    foreach (ToolStripItem item in dataGridViewContextMenu.Items)
                    {
                        item.ForeColor = theme.TextPrimary;
                        if (item is ToolStripMenuItem mi && mi.DropDownItems.Count > 0)
                        {
                            mi.DropDown.ForeColor = theme.TextPrimary;
                            mi.DropDown.BackColor = theme.Surface;
                        }
                    }
                }

                if (txtSearchBox != null && txtSearchBox.ContextMenuStrip != null)
                {
                    var cms = txtSearchBox.ContextMenuStrip;
                    cms.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg)));
                    cms.BackColor = theme.Surface;
                    cms.ForeColor = theme.TextPrimary;
                    foreach (ToolStripItem item in cms.Items)
                    {
                        item.ForeColor = theme.TextPrimary;
                    }
                }

                LayoutStatusBar(statusBarRef);
                RestoreLabelColors();
                SelectNavItem(currentView);
                UpdateScrollBarTheme(mainArea);
                UpdateActionButtonsVisibility();
            }
            finally
            {
                this.ResumeLayout(false);
                this.Invalidate(true);
            }
        }

        private void ApplyThemeToControl(Control ctrl)
        {
            if (ctrl == null) return;
            if (ctrl is Panel p)
            {
                if (p.Parent == statusBarRef)
                {
                    if (p.Height <= 1 || p.Width <= 1) p.BackColor = theme.Border;
                }
                else if (p.Parent == actionArea && (p.Name != null && p.Name.Contains("sep")))
                {
                    p.BackColor = theme.Border;
                }
                else if (p.Parent == searchPanelRef && (p.Height <= 1 || p.Width <= 1))
                {
                    p.BackColor = theme.Border;
                }
                if (p.Controls.Count == 1 && p.Controls[0] is TextBox)
                {
                    p.Tag = theme.Border;
                    p.Invalidate();
                }
                foreach (Control child in p.Controls) ApplyThemeToControl(child);
                return;
            }
            if (ctrl is Label lbl)
            {
                if (lbl == lblPercent)
                    lbl.ForeColor = theme.Primary;
                else if (lbl.Parent == statusBarRef)
                    lbl.ForeColor = theme.TextPrimary;
                else if (lbl.Parent == searchPanelRef)
                    lbl.ForeColor = theme.TextPrimary;
            }
            if (ctrl is Button btn)
            {
                if (btn.FlatStyle != FlatStyle.Flat)
                {
                    if (btn.BackColor == Color.FromArgb(148, 95, 205) || btn.BackColor.R > 180 && btn.BackColor.G < 150)
                        btn.BackColor = theme.Primary;
                    else if (btn.BackColor == Color.FromArgb(255, 85, 140))
                        btn.BackColor = theme.Accent;
                    else if (btn.FlatAppearance.BorderColor == Color.FromArgb(148, 95, 205))
                    {
                        btn.BackColor = theme.BgAlt;
                        btn.ForeColor = theme.PrimaryDark;
                    }
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Min(255, btn.BackColor.R + 15), Math.Min(255, btn.BackColor.G + 15), Math.Min(255, btn.BackColor.B + 15));
                }
                else if (btn.Tag is string tag && tag.StartsWith("sr:"))
                {
                    string role = tag.Substring(3);
                    switch (role)
                    {
                        case "primary":
                            btn.BackColor = theme.Primary;
                            break;
                        case "accent":
                            btn.BackColor = theme.Accent;
                            break;
                        case "export":
                            btn.BackColor = Color.FromArgb(0xFF, 0x00, 0xFF);
                            btn.ForeColor = Color.White;
                            break;
                        case "border":
                            btn.BackColor = theme.BgAlt;
                            btn.ForeColor = theme.PrimaryDark;
                            break;
                        case "surface":
                            btn.BackColor = theme.Surface;
                            btn.ForeColor = theme.TextPrimary;
                            break;
                        case "info":
                            btn.BackColor = theme.InfoColor;
                            btn.ForeColor = Color.White;
                            break;
                        case "error":
                            btn.BackColor = theme.ErrorColor;
                            btn.ForeColor = Color.White;
                            break;
                        case "success":
                            btn.BackColor = theme.SuccessColor;
                            btn.ForeColor = Color.White;
                            break;
                        case "dynamic":
                            Color cur = btn.BackColor;
                            int dPrim = Math.Abs(cur.R - ColorPurple.R) + Math.Abs(cur.G - ColorPurple.G) + Math.Abs(cur.B - ColorPurple.B);
                            int dAcc = Math.Abs(cur.R - ColorPink.R) + Math.Abs(cur.G - ColorPink.G) + Math.Abs(cur.B - ColorPink.B);
                            btn.BackColor = dPrim <= dAcc ? theme.Primary : theme.Accent;
                            break;
                        case "parse":
                            btn.BackColor = theme.Primary;
                            btn.ForeColor = Color.White;
                            break;
                    }
                    btn.Invalidate();
                }
            }
            if (ctrl is TextBox txt)
            {
                txt.BackColor = theme.Surface;
                txt.ForeColor = theme.TextPrimary;
            }
            if (ctrl is ComboBox cbo)
            {
                cbo.BackColor = theme.Surface;
                cbo.ForeColor = theme.TextPrimary;
                if (cbo is DarkComboBox dcbo)
                {
                    dcbo.BorderColor = theme.Border;
                    dcbo.FocusBorderColor = theme.Primary;
                    dcbo.ItemBackColor = theme.Surface;
                    dcbo.ItemSelectedBackColor = theme.BgAlt;
                    dcbo.ItemHoverBackColor = Color.FromArgb(Math.Min(255, theme.Surface.R + 10), Math.Min(255, theme.Surface.G + 10), Math.Min(255, theme.Surface.B + 10));
                }
            }
            foreach (Control child in ctrl.Controls) ApplyThemeToControl(child);
        }

        private void SetTheme(AppTheme newTheme)
        {
            if (_applyingTheme) return;
            if (theme == newTheme) return;

            _applyingTheme = true;
            try
            {
                theme = newTheme;
                ApplyTheme();
            }
            finally
            {
                _applyingTheme = false;
            }
        }

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatusBar()
        {
            if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
            {
                lblDetected.Text = $"已检测: {detectedCount}/{totalCount}";
                lblAvailable.Text = $"可用: {availableCount}";
                double pct = totalCount > 0 ? (double)detectedCount / totalCount * 100 : 0;
                lblPercent.Text = $"{pct:F2}%";

                if (totalCount > 0)
                {
                    progressBarWidth = (int)(statusBarRef.ClientSize.Width * pct / 100);
                }
                else
                {
                    progressBarWidth = 0;
                }

                statusBarRef.PerformLayout();
                LayoutStatusBar(statusBarRef);

                if (progressBarWidth > 0)
                {
                    UpdateLabelColorsBasedOnProgress();
                }
                else
                {
                    RestoreLabelColors();
                }

                statusBarRef.Refresh();
            }
        }

        private void UpdateLabelColorsBasedOnProgress()
        {
            if (theme == null) return;
            if (lblDetected != null)
            {
                lblDetected.ForeColor = lblDetected.Location.X + lblDetected.Width / 2 < progressBarWidth ? Color.White : theme.TextPrimary;
            }
            if (lblAvailable != null)
            {
                lblAvailable.ForeColor = lblAvailable.Location.X + lblAvailable.Width / 2 < progressBarWidth ? Color.White : theme.TextPrimary;
            }
            if (lblProgressText != null)
            {
                lblProgressText.ForeColor = lblProgressText.Location.X + lblProgressText.Width / 2 < progressBarWidth ? Color.White : theme.TextPrimary;
            }
            if (lblPercent != null)
            {
                lblPercent.ForeColor = lblPercent.Location.X + lblPercent.Width / 2 < progressBarWidth ? Color.White : theme.Primary;
            }
            if (lblStreamInfo != null && lblStreamInfo.Visible)
            {
                lblStreamInfo.ForeColor = lblStreamInfo.Location.X + lblStreamInfo.Width / 2 < progressBarWidth ? Color.White : theme.TextSecondary;
            }
        }

        private void RestoreLabelColors()
        {
            if (theme == null) return;
            if (lblDetected != null) lblDetected.ForeColor = theme.TextPrimary;
            if (lblAvailable != null) lblAvailable.ForeColor = theme.TextPrimary;
            if (lblProgressText != null) lblProgressText.ForeColor = theme.TextPrimary;
            if (lblPercent != null) lblPercent.ForeColor = theme.Primary;
            if (lblStreamInfo != null) lblStreamInfo.ForeColor = theme.TextSecondary;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void SearchChannels(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword == "输入搜索内容，按下回车键搜索")
            {
                foreach (var ch in allChannels) ch.Visible = true;
                RefreshGrid();
                return;
            }
            RefreshGrid();
            if (dgvData.Rows.Count == 0)
                DarkMessageBox.Show($"未找到包含 \"{keyword}\" 的频道", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 从文件导入
        /// </summary>
        private (int newItems, int dupItems) ImportFromFile(string filePath, HashSet<string> existingUrls = null)
        {
            int newItems = 0;
            int dupItems = 0;
            if (existingUrls == null) existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));
            try
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                string currentName = "", currentGroup = "";
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        int gi = line.IndexOf("group-title=\"", StringComparison.OrdinalIgnoreCase);
                        if (gi >= 0)
                        {
                            int eq = line.IndexOf('"', gi + 13);
                            if (eq > gi) currentGroup = line.Substring(gi + 13, eq - gi - 13);
                            else currentGroup = "";
                        }
                        else
                        {
                            currentGroup = "";
                        }
                        int ci = line.LastIndexOf(',');
                        if (ci >= 0)
                        {
                            currentName = line.Substring(ci + 1).Trim();
                        }
                    }
                    else if (line.StartsWith("#")) continue;
                    else
                    {
                        bool added = false;
                        string urlToAdd = null;
                        string nameToAdd = null;
                        string groupToAdd = null;

                        if (line.Contains(","))
                        {
                            int commaIdx = line.IndexOf(',');
                            string n = line.Substring(0, commaIdx).Trim().Trim('"', ' ', '`');
                            string u = line.Substring(commaIdx + 1).Trim().Trim('"', ' ', '`');
                            if (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                u.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                u.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.IsNullOrWhiteSpace(n)) n = "未命名频道";
                                if (u.Contains("#"))
                                {
                                    string[] urls = u.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string singleUrl in urls)
                                    {
                                        string trimmedUrl = singleUrl.Trim();
                                        if (string.IsNullOrWhiteSpace(trimmedUrl)) continue;
                                        string urlKey = trimmedUrl.ToLowerInvariant();
                                        if (existingUrls.Contains(urlKey))
                                        {
                                            dupItems++;
                                        }
                                        else
                                        {
                                            allChannels.Add(new ChannelInfo
                                            {
                                                Name = n,
                                                Url = trimmedUrl,
                                                Location = "",
                                                Resolution = "",
                                                Speed = "",
                                                Group = currentGroup,
                                                Status = "未检测",
                                                Visible = true
                                            });
                                            existingUrls.Add(urlKey);
                                            newItems++;
                                        }
                                    }
                                    currentName = "";
                                    continue;
                                }
                                urlToAdd = u;
                                nameToAdd = n;
                                groupToAdd = currentGroup;
                                currentName = "";
                                added = true;
                            }
                        }

                        if (!added && (line.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                             line.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                             line.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (string.IsNullOrWhiteSpace(currentName)) currentName = "未命名频道";
                            urlToAdd = line;
                            nameToAdd = currentName;
                            groupToAdd = currentGroup;
                            currentName = "";
                            added = true;
                        }

                        if (!added && line.Contains("|"))
                        {
                            string[] p = line.Split('|');
                            if (p.Length > 1 && !string.IsNullOrWhiteSpace(p[1]))
                            {
                                urlToAdd = p[1].Trim();
                                nameToAdd = p[0].Trim();
                                groupToAdd = p.Length > 5 ? p[5].Trim() : currentGroup;
                                added = true;
                            }
                        }

                        if (added && urlToAdd != null)
                        {
                            string urlKey = urlToAdd.ToLowerInvariant();
                            if (existingUrls.Contains(urlKey))
                            {
                                dupItems++;
                            }
                            else
                            {
                                allChannels.Add(new ChannelInfo
                                {
                                    Name = nameToAdd,
                                    Url = urlToAdd,
                                    Location = "",
                                    Resolution = "",
                                    Speed = "",
                                    Group = groupToAdd ?? "",
                                    Status = "未检测",
                                    Visible = true
                                });
                                existingUrls.Add(urlKey);
                                newItems++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show($"导入文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return (newItems, dupItems);
        }

        /// <summary>
        /// 从剪贴板粘贴
        /// </summary>
        private void PasteFromClipboard()
        {
            try
            {
                string text = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    DarkMessageBox.Show("剪贴板为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int added = 0;
                int dupCount = 0;
                HashSet<string> existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));
                string pendingName = null;
                string pendingGroup = "";

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        string info = line.Substring(8);
                        int commaIdx = info.LastIndexOf(',');
                        string attrs = commaIdx >= 0 ? info.Substring(0, commaIdx) : info;
                        string chName = commaIdx >= 0 ? info.Substring(commaIdx + 1).Trim().Trim('"', ' ', '`') : "";

                        var grpMatch = System.Text.RegularExpressions.Regex.Match(attrs, @"group-title\s*=\s*""([^""]*)""");
                        string grp = grpMatch.Success ? grpMatch.Groups[1].Value.Trim() : "";

                        var tvgNameMatch = System.Text.RegularExpressions.Regex.Match(attrs, @"tvg-name\s*=\s*""([^""]*)""");
                        if (tvgNameMatch.Success && !string.IsNullOrWhiteSpace(tvgNameMatch.Groups[1].Value))
                        {
                            string tn = tvgNameMatch.Groups[1].Value.Trim().Trim('"', ' ', '`');
                            if (!string.IsNullOrWhiteSpace(tn) && string.IsNullOrWhiteSpace(chName))
                                chName = tn;
                        }

                        if (string.IsNullOrWhiteSpace(chName)) chName = "粘贴链接";
                        pendingName = chName;
                        pendingGroup = grp;
                        continue;
                    }

                    if (line.StartsWith("#")) continue;

                    bool pasted = false;
                    bool isUrl = line.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
                    bool hasBtUrl = System.Text.RegularExpressions.Regex.IsMatch(line, @"`https?://");
                    string btUrlOnly = null;
                    if (!isUrl && hasBtUrl)
                    {
                        var btm = System.Text.RegularExpressions.Regex.Match(line, @"`([^`]+)`");
                        if (btm.Success)
                        {
                            string bu = btm.Groups[1].Value.Trim();
                            if (bu.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                bu.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                bu.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                            {
                                btUrlOnly = bu;
                                isUrl = true;
                            }
                        }
                    }

                    if (isUrl && pendingName != null)
                    {
                        string u = btUrlOnly ?? line.Trim('"', ' ', '`');
                        string urlKey = u.ToLowerInvariant();
                        if (existingUrls.Contains(urlKey))
                        {
                            dupCount++;
                        }
                        else
                        {
                            allChannels.Add(new ChannelInfo
                            {
                                Name = pendingName,
                                Url = u,
                                Group = pendingGroup,
                                Status = "未检测",
                                Visible = true
                            });
                            existingUrls.Add(urlKey);
                            added++;
                        }
                        pasted = true;
                        pendingName = null;
                        pendingGroup = "";
                    }

                    if (!pasted && line.Contains(","))
                    {
                        int commaIdx = line.IndexOf(',');
                        string n = line.Substring(0, commaIdx).Trim().Trim('"', ' ', '`');
                        string afterComma = line.Substring(commaIdx + 1);
                        string u = afterComma.Trim().Trim('"', ' ', '`');
                        bool uValid = u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                      u.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                      u.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
                        if (!uValid)
                        {
                            var btMatch = System.Text.RegularExpressions.Regex.Match(afterComma, @"`([^`]+)`");
                            if (btMatch.Success)
                            {
                                string btUrl = btMatch.Groups[1].Value.Trim().Trim('"', ' ', '`');
                                if (btUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                    btUrl.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                    btUrl.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                                {
                                    u = btUrl;
                                    uValid = true;
                                }
                            }
                        }
                        if (!uValid)
                        {
                            var urlMatch = System.Text.RegularExpressions.Regex.Match(afterComma, @"(https?://[^\s`""<>]+)");
                            if (urlMatch.Success)
                            {
                                u = urlMatch.Groups[1].Value.Trim();
                                uValid = true;
                            }
                        }
                        if (uValid)
                        {
                            if (string.IsNullOrWhiteSpace(n)) n = "粘贴链接";
                            if (u.Contains("#"))
                            {
                                string[] urls = u.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string singleUrl in urls)
                                {
                                    string trimmedUrl = singleUrl.Trim();
                                    if (string.IsNullOrWhiteSpace(trimmedUrl)) continue;
                                    string singleUrlKey = trimmedUrl.ToLowerInvariant();
                                    if (existingUrls.Contains(singleUrlKey))
                                        dupCount++;
                                    else
                                    {
                                        allChannels.Add(new ChannelInfo { Name = n, Url = trimmedUrl, Status = "未检测", Visible = true });
                                        existingUrls.Add(singleUrlKey);
                                        added++;
                                    }
                                }
                                pasted = true;
                                continue;
                            }
                            string urlKey = u.ToLowerInvariant();
                            if (existingUrls.Contains(urlKey))
                            {
                                dupCount++;
                            }
                            else
                            {
                                allChannels.Add(new ChannelInfo { Name = n, Url = u, Status = "未检测", Visible = true });
                                existingUrls.Add(urlKey);
                                added++;
                            }
                            pasted = true;
                        }
                    }

                    if (!pasted && isUrl)
                    {
                        string cleanedUrl = btUrlOnly ?? line.Trim('"', ' ', '`');
                        if (btUrlOnly == null)
                        {
                            var btUrlMatch = System.Text.RegularExpressions.Regex.Match(line, @"`([^`]+)`");
                            if (btUrlMatch.Success)
                            {
                                string bu = btUrlMatch.Groups[1].Value.Trim();
                                if (bu.StartsWith("http", StringComparison.OrdinalIgnoreCase)) cleanedUrl = bu;
                            }
                        }
                        string urlKey = cleanedUrl.ToLowerInvariant();
                        if (existingUrls.Contains(urlKey))
                        {
                            dupCount++;
                        }
                        else
                        {
                            allChannels.Add(new ChannelInfo { Name = "粘贴链接", Url = cleanedUrl, Status = "未检测", Visible = true });
                            existingUrls.Add(urlKey);
                            added++;
                        }
                        pasted = true;
                    }

                    if (!pasted && line.Contains("|"))
                    {
                        string[] p = line.Split('|');
                        if (p.Length > 1 && !string.IsNullOrWhiteSpace(p[1]))
                        {
                            string urlKey = p[1].Trim().ToLowerInvariant();
                            if (existingUrls.Contains(urlKey))
                            {
                                dupCount++;
                            }
                            else
                            {
                                allChannels.Add(new ChannelInfo
                                {
                                    Name = p[0].Trim(),
                                    Url = p[1].Trim(),
                                    Location = p.Length > 2 ? p[2].Trim() : "",
                                    Resolution = p.Length > 3 ? p[3].Trim() : "",
                                    Speed = p.Length > 4 ? p[4].Trim() : "",
                                    Group = p.Length > 5 ? p[5].Trim() : "",
                                    Status = "未检测",
                                    Visible = true
                                });
                                existingUrls.Add(urlKey);
                                added++;
                            }
                        }
                    }
                }
                totalCount = allChannels.Count;
                UpdateGroupFilter();
                RefreshGrid();
                UpdateStatusBar();
                UpdateEmptyState();
                if (added > 0)
                {
                    string msg = $"成功粘贴 {added} 条链接";
                    if (dupCount > 0) msg += $"\n跳过重复链接 {dupCount} 条";
                    DarkMessageBox.Show(msg, "粘贴成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { DarkMessageBox.Show($"粘贴失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// 检测单条直播链接
        /// 返回是否可用
        /// </summary>
        private async Task<bool> DetectSingleChannel(ChannelInfo ch, int timeout, CancellationToken token)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = false;
            string speed = "";
            string resolution = "";
            string location = ch.Location;
            System.Threading.Tasks.Task<string> ipLocTask = null;

            string ipHost = "";
            string domainHost = "";
            try
            {
                Uri u0 = new Uri(ch.Url);
                string h0 = u0.Host;
                if (IPAddress.TryParse(h0, out IPAddress tip) && tip.GetAddressBytes().Length == 4)
                    ipHost = h0;
                else
                    domainHost = h0;
            }
            catch { }
            if (!string.IsNullOrWhiteSpace(location))
            {
                ipLocTask = null;
            }
            else if (!string.IsNullOrEmpty(ipHost))
            {
                ipLocTask = QueryIpLocationAsync(ipHost, token);
            }
            else if (!string.IsNullOrEmpty(domainHost))
            {
                ipLocTask = QueryDomainLocationAsync(domainHost, token);
            }

            try
            {
                if (ch.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
                    using (var ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token))
                    {
                        var resp = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                        sw.Stop();
                        int statusCode = (int)resp.StatusCode;
                        ok = (statusCode >= 200 && statusCode < 400);
                        if (!ok && statusCode == 416)
                        {
                            request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
                            resp = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                            sw.Stop();
                            statusCode = (int)resp.StatusCode;
                            ok = (statusCode >= 200 && statusCode < 400);
                        }
                        if (!ok && (statusCode == 403 || statusCode == 405))
                        {
                            ok = true;
                        }
                        if (ok)
                        {
                            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                            bool isM3u8 = contentType.Contains("mpegurl") || contentType.Contains("x-mpegurl") ||
                                ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;

                            if (isM3u8)
                            {
                                try
                                {
                                    var buf = new byte[16384];
                                    using (var stream = await resp.Content.ReadAsStreamAsync())
                                    {
                                        int totalRead = 0;
                                        while (totalRead < buf.Length)
                                        {
                                            int n = await stream.ReadAsync(buf, totalRead, buf.Length - totalRead, linkedCts.Token);
                                            if (n <= 0) break;
                                            totalRead += n;
                                            if (totalRead > 2048 && !Encoding.UTF8.GetString(buf, 0, totalRead).Contains("#EXT-X-STREAM-INF"))
                                            {
                                                if (Encoding.UTF8.GetString(buf, 0, totalRead).Contains("#EXTINF")) break;
                                            }
                                        }
                                        string snippet = Encoding.UTF8.GetString(buf, 0, totalRead);
                                        if (snippet.Contains("#EXTM3U") || snippet.Contains("#EXTINF") ||
                                            snippet.Contains("#EXT-X-") || snippet.Contains(".ts") ||
                                            contentType.Contains("mpegurl")) ok = true;
                                        else if (!contentType.Contains("mpegurl") && statusCode == 200) ok = true;
                                        var resMatches = Regex.Matches(snippet, @"RESOLUTION=(\d+)x(\d+)");
                                        int maxW = 0, maxH = 0;
                                        foreach (Match m in resMatches)
                                        {
                                            if (int.TryParse(m.Groups[1].Value, out int w) && int.TryParse(m.Groups[2].Value, out int h))
                                            {
                                                if (w * h > maxW * maxH) { maxW = w; maxH = h; }
                                            }
                                        }
                                        if (maxW > 0 && maxH > 0) resolution = $"{maxW}x{maxH}";
                                    }
                                }
                                catch { ok = statusCode >= 200 && statusCode < 400; }
                            }
                            else if (contentType.Contains("video") || contentType.Contains("flv") || contentType.Contains("mp4") || contentType.Contains("octet-stream") || contentType.Contains("audio"))
                            {
                                try
                                {
                                    var buf = new byte[8192];
                                    using (var stream = await resp.Content.ReadAsStreamAsync())
                                    {
                                        int n = await stream.ReadAsync(buf, 0, buf.Length, linkedCts.Token);
                                        string sig = Encoding.ASCII.GetString(buf, 0, Math.Min(n, 16));
                                        if (sig.StartsWith("FLV"))
                                        {
                                            ok = true;
                                            if (n > 11)
                                            {
                                                int width = 0, height = 0;
                                                for (int i = 11; i < n - 18; i++)
                                                {
                                                    if (buf[i] == 0x09 && i + 15 < n)
                                                    {
                                                        width = (buf[i + 13] << 8) | buf[i + 14];
                                                        height = (buf[i + 14] << 8) | buf[i + 15];
                                                        break;
                                                    }
                                                }
                                                if (width > 0 && height > 0) resolution = $"{width}x{height}";
                                            }
                                        }
                                        else if (n > 11 && buf[4] == 0x66 && buf[5] == 0x74 && buf[6] == 0x79 && buf[7] == 0x70)
                                        {
                                            ok = true;
                                        }
                                        else
                                        {
                                            ok = true;
                                        }
                                    }
                                }
                                catch { ok = true; }
                            }
                            else if (ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                ok = true;
                            }
                            speed = $"{sw.ElapsedMilliseconds}ms";
                            if (string.IsNullOrEmpty(location))
                                location = ExtractLocationFromUrl(ch.Url);
                        }
                        resp.Dispose();
                    }

                    if (ok && string.IsNullOrEmpty(resolution))
                    {
                        resolution = await GetResolutionWithFallback(ch.Url, token);
                    }

                    if (ok && string.IsNullOrEmpty(resolution))
                        resolution = "直播";
                }
                else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                         ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                {
                    ok = true;
                    sw.Stop();
                    speed = $"{sw.ElapsedMilliseconds}ms";
                    resolution = "直播";
                    if (string.IsNullOrEmpty(location))
                        location = ExtractLocationFromUrl(ch.Url);
                }
                else
                {
                    ok = true;
                    sw.Stop();
                    speed = $"{sw.ElapsedMilliseconds}ms";
                }
            }
            catch
            {
                sw.Stop();
                ok = false;
                speed = "超时";
            }

            if (ipLocTask != null)
            {
                try
                {
                    string ipLoc = await ipLocTask;
                    if (!string.IsNullOrEmpty(ipLoc)) location = ipLoc;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(location))
                location = ExtractLocationFromUrl(ch.Url);

            ch.Status = ok ? "可用" : "不可用";
            ch.Speed = speed;
            ch.Resolution = resolution;
            ch.Location = location;
            return ok;
        }

        /// <summary>
        /// 真实HTTP异步检测（并发检测，UI节流更新）
        /// </summary>
        private async System.Threading.Tasks.Task StartDetection()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;

            if (btnStopDetect != null) btnStopDetect.Enabled = true;

            detectedCount = 0; availableCount = 0;
            totalCount = allChannels.Count;
            Parallel.ForEach(allChannels, ch =>
            {
                ch.Status = "未检测";
                ch.Speed = "";
                if (string.IsNullOrWhiteSpace(ch.Location))
                    ch.Location = ExtractLocationFromUrl(ch.Url);
                if (string.IsNullOrWhiteSpace(ch.Resolution))
                    ch.Resolution = "";
            });

            if (lblProgressText != null && !lblProgressText.IsDisposed)
            {
                lblProgressText.Text = "检测进度:";
            }
            RefreshGrid();
            UpdateStatusBar();

            int concurrency = Math.Min(detectConcurrency, allChannels.Count);
            int uiUpdateCounter = 0;
            var startTime = DateTime.Now;

            int uiRefreshNeeded = 0;
            int lastRefreshPercent = -1;
            System.Windows.Forms.Timer uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 500 };
            uiRefreshTimer.Tick += (s, e) =>
            {
                if (Interlocked.Exchange(ref uiRefreshNeeded, 0) == 1)
                {
                    int currentPercent = totalCount > 0 ? (int)((double)detectedCount / totalCount * 100) : 0;
                    if (currentPercent != lastRefreshPercent || detectedCount == totalCount)
                    {
                        lastRefreshPercent = currentPercent;
                        RefreshGrid();
                    }
                    UpdateStatusBar();
                }
            };
            uiRefreshTimer.Start();

            try
            {
            using (var sem = new SemaphoreSlim(concurrency, concurrency))
            {
                var tasks = allChannels.Select(async ch =>
                    {
                        await sem.WaitAsync(token);
                        System.Threading.Tasks.Task<string> ipLocTask = null;
                        try
                        {
                            while (isPaused && !token.IsCancellationRequested)
                            {
                                await System.Threading.Tasks.Task.Delay(100);
                            }
                            token.ThrowIfCancellationRequested();

                            ch.Status = "检测中";
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            bool ok = false;
                            string speed = "";
                            string resolution = "";
                            string location = ch.Location;

                            string ipHost = "";
                            string domainHost = "";
                            try
                            {
                                Uri u0 = new Uri(ch.Url);
                                string h0 = u0.Host;
                                if (IPAddress.TryParse(h0, out IPAddress tip) && tip.GetAddressBytes().Length == 4)
                                    ipHost = h0;
                                else
                                    domainHost = h0;
                            }
                            catch { }
                            if (string.IsNullOrWhiteSpace(location))
                            {
                                if (!string.IsNullOrEmpty(ipHost))
                                {
                                    ipLocTask = QueryIpLocationAsync(ipHost, token);
                                }
                                else if (!string.IsNullOrEmpty(domainHost))
                                {
                                    ipLocTask = QueryDomainLocationAsync(domainHost, token);
                                }
                            }

                            if (ch.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    using (var ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token))
                                    {
                                        bool isUrlM3u8 = ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;
                                        bool isUrlFlv = ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0;
                                        bool isUrlMp4 = ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0;
                                        bool isUrlTs = ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0;

                                        var request = new HttpRequestMessage(HttpMethod.Head, ch.Url);
                                        var resp = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                                        sw.Stop();
                                        int statusCode = (int)resp.StatusCode;
                                        ok = (statusCode >= 200 && statusCode < 400);

                                        if (!ok && statusCode == 405)
                                        {
                                            request = new HttpRequestMessage(isUrlM3u8 ? HttpMethod.Get : HttpMethod.Head, ch.Url);
                                            var resp2 = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                                            statusCode = (int)resp2.StatusCode;
                                            ok = (statusCode >= 200 && statusCode < 400);
                                            resp = resp2;
                                        }
                                        if (!ok && (statusCode == 403 || statusCode == 416))
                                        {
                                            ok = true;
                                        }
                                        if (ok)
                                        {
                                            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                                            bool isContentTypeVideo = contentType.Contains("video") || contentType.Contains("flv") ||
                                                contentType.Contains("mp4") || contentType.Contains("octet-stream") || contentType.Contains("audio") ||
                                                contentType.Contains("mpegurl") || contentType.Contains("x-mpegurl");

                                            if (isUrlM3u8 || isContentTypeVideo)
                                            {
                                                if (isUrlM3u8)
                                                {
                                                    try
                                                    {
                                                        var buf = new byte[1024];
                                                        using (var stream = await resp.Content.ReadAsStreamAsync())
                                                        {
                                                            int n = await stream.ReadAsync(buf, 0, buf.Length, linkedCts.Token);
                                                            string snippet = n > 0 ? Encoding.UTF8.GetString(buf, 0, n) : "";
                                                            if (!string.IsNullOrEmpty(snippet) && !(snippet.Contains("#EXTM3U") || snippet.Contains("#EXTINF") || 
                                                                snippet.Contains("#EXT-X-") || snippet.Contains(".ts")))
                                                            {
                                                                if (statusCode < 200 || statusCode >= 400) ok = false;
                                                            }
                                                            if (ok)
                                                            {
                                                                var resMatches = Regex.Matches(snippet, @"RESOLUTION=(\d+)x(\d+)");
                                                                int maxW = 0, maxH = 0;
                                                                foreach (Match m in resMatches)
                                                                {
                                                                    if (int.TryParse(m.Groups[1].Value, out int w) && int.TryParse(m.Groups[2].Value, out int h))
                                                                    {
                                                                        if (w * h > maxW * maxH) { maxW = w; maxH = h; }
                                                                    }
                                                                }
                                                                if (maxW > 0 && maxH > 0) resolution = $"{maxW}x{maxH}";
                                                            }
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                            else if (isUrlFlv || isUrlMp4 || isUrlTs)
                                            {
                                                ok = true;
                                            }
                                            speed = $"{sw.ElapsedMilliseconds}ms";
                                            if (string.IsNullOrEmpty(location))
                                                location = ExtractLocationFromUrl(ch.Url);
                                        }
                                    }
                                }
                                catch (HttpRequestException) { ok = false; }
                                catch (OperationCanceledException) { ok = false; }
                                catch { ok = false; }
                            }
                            else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                     ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                            {
                                ok = true;
                                sw.Stop();
                                speed = $"{sw.ElapsedMilliseconds}ms";
                                resolution = "直播";
                                if (string.IsNullOrEmpty(location))
                                    location = ExtractLocationFromUrl(ch.Url);
                            }
                            else
                            {
                                ok = true;
                                sw.Stop();
                                speed = $"{sw.ElapsedMilliseconds}ms";
                            }

                            if (ok && string.IsNullOrEmpty(resolution) && detectEngine == "FFMPEG")
                            {
                                try
                                {
                                    resolution = await GetResolutionWithFallback(ch.Url, token);
                                }
                                catch { }
                            }

                            if (ok && string.IsNullOrEmpty(resolution))
                                resolution = "直播";

                            if (ipLocTask != null && ok)
                            {
                                try
                                {
                                    string ipLoc = await ipLocTask;
                                    if (!string.IsNullOrEmpty(ipLoc)) location = ipLoc;
                                }
                                catch { }
                            }
                            if (string.IsNullOrEmpty(location))
                                location = ExtractLocationFromUrl(ch.Url);

                        ch.Status = ok ? "可用" : "不可用";
                        ch.Speed = speed;
                        ch.Resolution = resolution;
                        ch.Location = location;
                        Interlocked.Increment(ref detectedCount);
                        if (ok) Interlocked.Increment(ref availableCount);

                        Interlocked.Increment(ref uiUpdateCounter);
                        Interlocked.Exchange(ref uiRefreshNeeded, 1);
                    }
                    finally
                    {
                        sem.Release();
                    }
                });

                try
                {
                    await System.Threading.Tasks.Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) { }
            }

            var failedChannels = allChannels.Where(c => c.Status == "不可用").ToList();
            if (failedChannels.Count > 0 && !token.IsCancellationRequested)
            {
                int fallbackConcurrency = Math.Max(1, Math.Min(5, concurrency / 2));
                int fallbackTimeout = timeoutSeconds * 2;

                using (var sem = new SemaphoreSlim(fallbackConcurrency, fallbackConcurrency))
                {
                    var fallbackTasks = failedChannels.Select(async ch =>
                    {
                        await sem.WaitAsync(token);
                        try
                        {
                            while (isPaused && !token.IsCancellationRequested)
                            {
                                await System.Threading.Tasks.Task.Delay(100);
                            }
                            token.ThrowIfCancellationRequested();

                            ch.Status = "复检中";
                            Interlocked.Exchange(ref uiRefreshNeeded, 1);
                            System.Threading.Tasks.Task<string> ipLocTask = null;
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            bool ok = false;
                            string speed = "";
                            string resolution = "";
                            string location = ch.Location;

                            string ipHost = "";
                            string domainHost = "";
                            try
                            {
                                Uri u0 = new Uri(ch.Url);
                                string h0 = u0.Host;
                                if (IPAddress.TryParse(h0, out IPAddress tip) && tip.GetAddressBytes().Length == 4)
                                    ipHost = h0;
                                else
                                    domainHost = h0;
                            }
                            catch { }
                            if (!string.IsNullOrWhiteSpace(location))
                            {
                                ipLocTask = null;
                            }
                            else if (!string.IsNullOrEmpty(ipHost))
                            {
                                ipLocTask = QueryIpLocationAsync(ipHost, token);
                            }
                            else if (!string.IsNullOrEmpty(domainHost))
                            {
                                ipLocTask = QueryDomainLocationAsync(domainHost, token);
                            }

                            try
                            {
                                if (ch.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        var request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
                                        using (var ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(fallbackTimeout)))
                                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token))
                                        {
                                            var resp = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                                        sw.Stop();
                                        int statusCode = (int)resp.StatusCode;
                                        ok = (statusCode >= 200 && statusCode < 400);
                                        if (!ok && statusCode == 416)
                                        {
                                            request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
                                            resp = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                                            sw.Stop();
                                            statusCode = (int)resp.StatusCode;
                                            ok = (statusCode >= 200 && statusCode < 400);
                                        }
                                        if (!ok && (statusCode == 403 || statusCode == 405))
                                        {
                                            ok = true;
                                        }
                                        if (ok)
                                        {
                                            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                                            bool isM3u8 = contentType.Contains("mpegurl") || contentType.Contains("x-mpegurl") ||
                                                ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;

                                            if (isM3u8)
                                            {
                                                try
                                                {
                                                    var buf = new byte[16384];
                                                    using (var stream = await resp.Content.ReadAsStreamAsync())
                                                    {
                                                        int totalRead = 0;
                                                        while (totalRead < buf.Length)
                                                        {
                                                            int n = await stream.ReadAsync(buf, totalRead, buf.Length - totalRead, linkedCts.Token);
                                                            if (n <= 0) break;
                                                            totalRead += n;
                                                            if (totalRead > 2048 && !Encoding.UTF8.GetString(buf, 0, totalRead).Contains("#EXT-X-STREAM-INF"))
                                                            {
                                                                if (Encoding.UTF8.GetString(buf, 0, totalRead).Contains("#EXTINF")) break;
                                                            }
                                                        }
                                                        string snippet = Encoding.UTF8.GetString(buf, 0, totalRead);
                                                        bool hasM3u8Signature = snippet.Contains("#EXTM3U") || snippet.Contains("#EXTINF") ||
                                                            snippet.Contains("#EXT-X-") || snippet.Contains(".ts") ||
                                                            contentType.Contains("mpegurl");
                                                        if (!hasM3u8Signature && !contentType.Contains("mpegurl") && statusCode != 200)
                                                        {
                                                            ok = false;
                                                        }
                                                        var resMatches = Regex.Matches(snippet, @"RESOLUTION=(\d+)x(\d+)");
                                                        int maxW = 0, maxH = 0;
                                                        foreach (Match m in resMatches)
                                                        {
                                                            if (int.TryParse(m.Groups[1].Value, out int w) && int.TryParse(m.Groups[2].Value, out int h))
                                                            {
                                                                if (w * h > maxW * maxH) { maxW = w; maxH = h; }
                                                            }
                                                        }
                                                        if (maxW > 0 && maxH > 0) resolution = $"{maxW}x{maxH}";
                                                    }
                                                }
                                                catch { ok = statusCode >= 200 && statusCode < 400; }
                                            }
                                            else if (contentType.Contains("video") || contentType.Contains("flv") || contentType.Contains("mp4") || contentType.Contains("octet-stream") || contentType.Contains("audio"))
                                            {
                                                try
                                                {
                                                    var buf = new byte[8192];
                                                    using (var stream = await resp.Content.ReadAsStreamAsync())
                                                    {
                                                        int n = await stream.ReadAsync(buf, 0, buf.Length, linkedCts.Token);
                                                        string sig = Encoding.ASCII.GetString(buf, 0, Math.Min(n, 16));
                                                        if (sig.StartsWith("FLV"))
                                                        {
                                                            ok = true;
                                                            if (n > 11)
                                                            {
                                                                int width = 0, height = 0;
                                                                for (int i = 11; i < n - 18; i++)
                                                                {
                                                                    if (buf[i] == 0x09 && i + 15 < n)
                                                                    {
                                                                        width = (buf[i + 13] << 8) | buf[i + 14];
                                                                        height = (buf[i + 14] << 8) | buf[i + 15];
                                                                        break;
                                                                    }
                                                                }
                                                                if (width > 0 && height > 0) resolution = $"{width}x{height}";
                                                            }
                                                        }
                                                        else if (n > 11 && buf[4] == 0x66 && buf[5] == 0x74 && buf[6] == 0x79 && buf[7] == 0x70)
                                                        {
                                                            ok = true;
                                                        }
                                                        else
                                                        {
                                                            ok = true;
                                                        }
                                                    }
                                                }
                                                catch { ok = true; }
                                            }
                                            else if (ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                ok = true;
                                            }
                                            speed = $"{sw.ElapsedMilliseconds}ms";
                                            if (string.IsNullOrEmpty(location))
                                                location = ExtractLocationFromUrl(ch.Url);
                                        }
                                            resp.Dispose();
                                        }
                                    }
                                    catch (HttpRequestException) { ok = false; }
                                    catch (OperationCanceledException) { ok = false; }
                                    catch { ok = false; }

                                    if (ok && string.IsNullOrEmpty(resolution) && detectEngine == "FFMPEG")
                                    {
                                        try { resolution = await GetResolutionWithFallback(ch.Url, token); } catch { }
                                    }

                                    if (ok && string.IsNullOrEmpty(resolution))
                                        resolution = "直播";
                                }
                                else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                                         ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                                {
                                    ok = true;
                                    sw.Stop();
                                    speed = $"{sw.ElapsedMilliseconds}ms";
                                    resolution = "直播";
                                    if (string.IsNullOrEmpty(location))
                                        location = ExtractLocationFromUrl(ch.Url);
                                }
                                else
                                {
                                    ok = true;
                                    sw.Stop();
                                    speed = $"{sw.ElapsedMilliseconds}ms";
                                }
                            }
                            catch
                            {
                                sw.Stop();
                                ok = false;
                                speed = "超时";
                            }

                            if (ipLocTask != null)
                            {
                                try
                                {
                                    string ipLoc = await ipLocTask;
                                    if (!string.IsNullOrEmpty(ipLoc)) location = ipLoc;
                                }
                                catch { }
                            }
                            if (string.IsNullOrEmpty(location))
                                location = ExtractLocationFromUrl(ch.Url);

                            ch.Status = ok ? "可用" : "不可用";
                            ch.Speed = speed;
                            ch.Resolution = resolution;
                            ch.Location = location;
                            if (ok) Interlocked.Increment(ref availableCount);
                            Interlocked.Exchange(ref uiRefreshNeeded, 1);
                        }
                        catch { }
                        finally
                        {
                            sem.Release();
                        }
                    });

                    try
                    {
                        await System.Threading.Tasks.Task.WhenAll(fallbackTasks);
                    }
                    catch (OperationCanceledException) { }
                }
            }
            }
            finally
            {
                uiRefreshTimer.Stop();
                uiRefreshTimer.Dispose();
                if (btnStopDetect != null) btnStopDetect.Enabled = false;
            }
            if (lblProgressText != null && !lblProgressText.IsDisposed)
            {
                lblProgressText.Text = "检测完成";
            }
            RefreshGrid();
            UpdateStatusBar();
            UpdateEmptyState();
            if (!token.IsCancellationRequested)
            {
                int failedCount = allChannels.Count(c => c.Status == "不可用");
                string msg = $"检测完成！\n已检测: {detectedCount}/{totalCount}\n可用: {availableCount}";
                if (failedCount > 0)
                    msg += $"\n不可用: {failedCount}（已进行二次复检）";
                DarkMessageBox.Show(msg, "检测完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void WebView2InitCompletedHandler(object sender, EventArgs e)
        {
            try
            {
                var webView2 = sender;
                var type = webView2.GetType();
                var coreProp = type.GetProperty("CoreWebView2");
                if (coreProp != null)
                {
                    var core = coreProp.GetValue(webView2);
                    if (core != null)
                    {
                        var settingsProp = core.GetType().GetProperty("Settings");
                        if (settingsProp != null)
                        {
                            var settings = settingsProp.GetValue(core);
                            settings.GetType().GetProperty("UserAgent")?.SetValue(settings, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        }
                        if (!string.IsNullOrEmpty(webViewPendingUrl))
                        {
                            var navMethod = core.GetType().GetMethod("Navigate", new[] { typeof(string) });
                            navMethod?.Invoke(core, new object[] { webViewPendingUrl });
                            webViewPendingUrl = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show("WebView2初始化失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void WebView2NavCompletedHandler(object sender, EventArgs e)
        {
            try
            {
                var webView2 = sender;
                var type = webView2.GetType();
                var coreProp = type.GetProperty("CoreWebView2");
                if (coreProp != null)
                {
                    var core = coreProp.GetValue(webView2);
                    if (core != null)
                    {
                        var execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new[] { typeof(string) });
                        if (execMethod == null) return;

                        // 1. 检测页面背景色并调整工具栏
                        try
                        {
                            var bgTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] {
                                "(() => { " +
                                "let bg = window.getComputedStyle(document.body).backgroundColor; " +
                                "if (!bg || bg === 'transparent') { bg = window.getComputedStyle(document.documentElement).backgroundColor; } " +
                                "if (!bg || bg === 'transparent') { bg = '#1a1a2e'; } " +
                                "return bg; " +
                                "})()"
                            });
                            string jsResult = await bgTask;
                            jsResult = jsResult?.Trim('"');
                            if (!string.IsNullOrEmpty(jsResult) && jsResult.StartsWith("#"))
                            {
                                try
                                {
                                    Color pageBg = ColorTranslator.FromHtml(jsResult);
                                    AdjustToolbarColors(webViewNavPanel, webViewCboEngine, webViewTxtUrl, pageBg);
                                }
                                catch { }
                            }
                        }
                        catch { }

                        // 2. 注入登录信息记录脚本
                        try
                        {
                            string loginJs =
                                "(function() { " +
                                "  if (window._loginFormHooked) return; " +
                                "  window._loginFormHooked = true; " +
                                "  document.addEventListener('submit', function(e) { " +
                                "    try { " +
                                "      var form = e.target; " +
                                "      var pwd = form.querySelector('input[type=\"password\"]'); " +
                                "      if (!pwd || !pwd.value) return; " +
                                "      var userInput = form.querySelector('input[type=\"text\"], input[type=\"email\"], input[name*=\"user\"], input[name*=\"account\"], input[name*=\"email\"], input[name*=\"login\"]'); " +
                                "      if (!userInput) { var allInputs = form.querySelectorAll('input'); for (var i=0; i<allInputs.length; i++) { if (allInputs[i] !== pwd && allInputs[i].type !== 'hidden' && allInputs[i].type !== 'password') { userInput = allInputs[i]; break; } } } " +
                                "      if (userInput && userInput.value) { " +
                                "        window.chrome.webview.postMessage(JSON.stringify({type:'login', url:location.origin, username:userInput.value, password:pwd.value})); " +
                                "      } " +
                                "    } catch(ex) {} " +
                                "  }, true); " +
                                "  document.addEventListener('keydown', function(e) { " +
                                "    if (e.key === 'Enter') { " +
                                "      var el = e.target; " +
                                "      if (el && el.type === 'password' && el.value) { " +
                                "        var form = el.closest('form'); " +
                                "        if (form) { var userInput = form.querySelector('input[type=\"text\"], input[type=\"email\"], input[name*=\"user\"], input[name*=\"account\"], input[name*=\"email\"]'); " +
                                "          if (!userInput) { var allInputs = form.querySelectorAll('input'); for (var i=0; i<allInputs.length; i++) { if (allInputs[i] !== el && allInputs[i].type !== 'hidden' && allInputs[i].type !== 'password') { userInput = allInputs[i]; break; } } } " +
                                "          if (userInput && userInput.value) { " +
                                "            window.chrome.webview.postMessage(JSON.stringify({type:'login', url:location.origin, username:userInput.value, password:el.value})); " +
                                "          } " +
                                "        } " +
                                "      } " +
                                "    } " +
                                "  }, true); " +
                                "})();";
                            await (System.Threading.Tasks.Task)execMethod.Invoke(core, new object[] { loginJs });
                        }
                        catch { }

                        // 更新状态栏URL
                        try
                        {
                            var sourceProp = type.GetProperty("Source");
                            string url = sourceProp?.GetValue(webView2)?.ToString() ?? "";
                            if (webViewStatusUrl != null)
                            {
                                if (webViewStatusUrl.InvokeRequired)
                                {
                                    webViewStatusUrl.BeginInvoke(new Action(() =>
                                    {
                                        webViewStatusUrl.Text = url.Length > 50 ? url.Substring(0, 50) + "..." : url;
                                    }));
                                }
                                else
                                {
                                    webViewStatusUrl.Text = url.Length > 50 ? url.Substring(0, 50) + "..." : url;
                                }
                            }
                        }
                        catch { }

                        // 3. 自动提取IP+端口
                        if (autoExtractIpPort)
                        {
                            try
                            {
                                string extractJs =
                                    "(function() { " +
                                    "  var allText = ''; " +
                                    "  allText += document.body.innerText || ''; " +
                                    "  allText += ' ' + document.documentElement.outerHTML || ''; " +
                                    "  try { " +
                                    "    var iframes = document.querySelectorAll('iframe'); " +
                                    "    for (var k=0; k<iframes.length; k++) { " +
                                    "      try { if (iframes[k].contentDocument) { allText += ' ' + iframes[k].contentDocument.body.innerText; } } catch(e) {} " +
                                    "    } " +
                                    "  } catch(e) {} " +
                                    "  var valid = {}; " +
                                    "  var ipv4Regex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b/g; " +
                                    "  var matches = allText.match(ipv4Regex) || []; " +
                                    "  for (var i=0; i<matches.length; i++) { valid[matches[i]] = true; } " +
                                    "  var urlIpRegex = /(?:http|https):\\/\\/(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?::(\\d{1,5}))?(?:\\/|\\?|$)/gi; " +
                                    "  var urlMatches = allText.match(urlIpRegex) || []; " +
                                    "  for (var i=0; i<urlMatches.length; i++) { " +
                                    "    var urlMatch = urlMatches[i].replace(/^https?:\\/\\//i, ''); " +
                                    "    var portMatch = urlMatch.match(/:(\\d{1,5})$/); " +
                                    "    var ip = urlMatch.replace(/:\\d{1,5}$/, ''); " +
                                    "    if (portMatch) { valid[ip + ':' + portMatch[1]] = true; } else { valid[ip] = true; } " +
                                    "  } " +
                                    "  var ipWithPortRegex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):(\\d{1,5})\\b/g; " +
                                    "  var portMatches = allText.match(ipWithPortRegex) || []; " +
                                    "  for (var i=0; i<portMatches.length; i++) { valid[portMatches[i]] = true; } " +
                                    "  var ipList = Object.keys(valid); " +
                                    "  ipList = ipList.filter(function(ip) { " +
                                    "    var parts = ip.split(':')[0].split('.'); " +
                                    "    if (parts.length !== 4) return false; " +
                                    "    for (var j=0; j<4; j++) { var n = parseInt(parts[j]); if (isNaN(n) || n<0 || n>255) return false; } " +
                                    "    return true; " +
                                    "  }); " +
                                    "  return JSON.stringify(ipList); " +
                                    "})()";
                                var ipTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] { extractJs });
                                string ipResult = await ipTask;
                                if (!string.IsNullOrEmpty(ipResult))
                                {
                                    var ips = new List<string>();
                                    try
                                    {
                                        ipResult = ipResult.Trim();
                                        var allMatches = System.Text.RegularExpressions.Regex.Matches(ipResult, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})");
                                        foreach (System.Text.RegularExpressions.Match m in allMatches)
                                        {
                                            string ip = m.Groups[1].Value;
                                            string port = m.Groups[2].Value;
                                            var parts = ip.Split('.');
                                            if (parts.Length != 4) continue;
                                            bool isValid = true;
                                            int[] ipParts = new int[4];
                                            for (int i = 0; i < 4; i++)
                                            {
                                                if (!int.TryParse(parts[i], out ipParts[i]) || ipParts[i] < 0 || ipParts[i] > 255)
                                                {
                                                    isValid = false;
                                                    break;
                                                }
                                            }
                                            if (!isValid) continue;
                                            if (ipParts[0] == 10) continue;
                                            if (ipParts[0] == 172 && ipParts[1] >= 16 && ipParts[1] <= 31) continue;
                                            if (ipParts[0] == 192 && ipParts[1] == 168) continue;
                                            if (ipParts[0] == 127) continue;
                                            if (ipParts[0] == 0 && ipParts[1] == 0 && ipParts[2] == 0 && ipParts[3] == 0) continue;
                                            if (ipParts[0] == 255 && ipParts[1] == 255 && ipParts[2] == 255 && ipParts[3] == 255) continue;
                                            if (ipParts[0] == 169 && ipParts[1] == 254) continue;
                                            if (!int.TryParse(port, out int portNum) || portNum < 1 || portNum > 65535)
                                                continue;
                                            string fullIp = ip + ":" + port;
                                            if (!ips.Contains(fullIp))
                                                ips.Add(fullIp);
                                        }
                                    }
                                    catch { }
                                    if (ips != null && ips.Count > 0)
                                    {
                                        string ipFile = System.IO.Path.Combine(Application.StartupPath, "extracted_ips.txt");
                                        using (var sw = new System.IO.StreamWriter(ipFile, true, Encoding.UTF8))
                                        {
                                            var sourceProp = type.GetProperty("Source");
                                            string currentSrc = sourceProp?.GetValue(webView2)?.ToString() ?? "";
                                            sw.WriteLine($"# 提取时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} 来源: {currentSrc} 共{ips.Count}条");
                                            foreach (var ip in ips)
                                            {
                                                sw.WriteLine(ip);
                                            }
                                        }
                                        if (webViewCboEngine != null && webViewCboEngine.InvokeRequired)
                                        {
                                            webViewCboEngine.BeginInvoke(new Action(() =>
                                            {
                                                DarkMessageBox.Show($"已提取 {ips.Count} 条IP地址\n保存到: extracted_ips.txt", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }));
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private void WebView2WebMessageReceivedHandler(object sender, EventArgs e)
        {
            try
            {
                // 通过反射获取WebMessageReceivedEventArgs的Message属性
                var argsType = e.GetType();
                var msgProp = argsType.GetProperty("Message") ?? argsType.GetProperty("TryGetWebMessageAsString");
                string message = null;

                if (msgProp != null && msgProp.PropertyType == typeof(string))
                {
                    message = msgProp.GetValue(e) as string;
                }
                else
                {
                    // CoreWebView2WebMessageReceivedEventArgs 有 TryGetWebMessageAsString 方法
                    var tryGetMethod = argsType.GetMethod("TryGetWebMessageAsString");
                    if (tryGetMethod != null)
                    {
                        message = tryGetMethod.Invoke(e, null) as string;
                    }
                }

                if (string.IsNullOrEmpty(message)) return;

                // 手动解析JSON
                var data = new Dictionary<string, string>();
                var matches = System.Text.RegularExpressions.Regex.Matches(message, "\"(\\w+)\":\"([^\"]*)\"");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    data[m.Groups[1].Value] = m.Groups[2].Value;
                }
                if (data != null && data.ContainsKey("type") && data["type"] == "login")
                {
                    string url = data.ContainsKey("url") ? data["url"] : "";
                    string username = data.ContainsKey("username") ? data["username"] : "";
                    string password = data.ContainsKey("password") ? data["password"] : "";

                    if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(username))
                    {
                        // 保存到文件
                        loginDataPath = System.IO.Path.Combine(Application.StartupPath, "login_data.txt");
                        bool exists = System.IO.File.Exists(loginDataPath);
                        bool hasExisting = false;

                        // 检查是否已存在相同的登录信息
                        if (exists)
                        {
                            var lines = System.IO.File.ReadAllLines(loginDataPath, Encoding.UTF8);
                            foreach (var line in lines)
                            {
                                if (line.Contains(url) && line.Contains(username))
                                {
                                    hasExisting = true;
                                    break;
                                }
                            }
                        }

                        if (!hasExisting)
                        {
                            using (var sw = new System.IO.StreamWriter(loginDataPath, true, Encoding.UTF8))
                            {
                                if (!exists) sw.WriteLine("# WebView2登录信息记录");
                                sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {url} | 用户名: {username} | 密码: {password}");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 解析下载直播源：从当前网页提取IP，按规则生成M3U8 URL，解析后添加到列表
        /// </summary>
        private async System.Threading.Tasks.Task ParseAndDownloadLiveSources(object webView2, Type webView2Type, string ruleName)
        {
            try
            {
                // 1. 从网页提取IP+端口
                string extractJs =
                    "(function() { " +
                    "  var html = document.documentElement.outerHTML; " +
                    "  var matches = html.match(/(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?::(\\d{2,5}))?/g) || []; " +
                    "  var valid = []; " +
                    "  for (var i=0; i<matches.length && valid.length<300; i++) { " +
                    "    var parts = matches[i].split('.'); " +
                    "    var ok = true; " +
                    "    for (var j=0; j<4; j++) { var n = parseInt(parts[j]); if (n<0||n>255) { ok=false; break; } } " +
                    "    if (ok) { if (valid.indexOf(matches[i]) === -1) valid.push(matches[i]); } " +
                    "  } " +
                    "  return JSON.stringify(valid); " +
                    "})()";

                var coreProp = webView2Type.GetProperty("CoreWebView2");
                if (coreProp == null) return;
                var core = coreProp.GetValue(webView2);
                if (core == null) return;

                var execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new[] { typeof(string) });
                if (execMethod == null) return;

                var ipTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] { extractJs });
                string ipResult = await ipTask;

                // 解析IP列表
                var ipMatches = System.Text.RegularExpressions.Regex.Matches(ipResult, "\"([^\"]+)\"");
                var ipList = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in ipMatches)
                {
                    string val = m.Groups[1].Value;
                    if (System.Text.RegularExpressions.Regex.IsMatch(val, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?$"))
                        ipList.Add(val);
                }

                if (ipList.Count == 0)
                {
                    DarkMessageBox.Show("未在当前页面找到IP地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                hasSearchPlatformData = true;

                int addedCount = 0;
                DateTime parseTime = DateTime.Now;

                if (!autoParseLink)
                {
                    foreach (string ipPort in ipList)
                    {
                        var parts = ipPort.Split(':');
                        if (parts.Length != 2) continue;

                        string ip = parts[0];
                        string port = parts[1];
                        string rootHttp = $"http://{ip}:{port}";

                        if (ruleName == "智慧光迅")
                        {
                            string url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                            bool exists = allChannels.Any(c => c.Url == url);
                            if (!exists)
                            {
                                allChannels.Add(new ChannelInfo { Name = ipPort, Url = url, Group = "解析待处理", Status = "待解析", ParseDateTime = parseTime });
                                addedCount++;
                            }
                        }
                        else if (ruleName == "华视美达")
                        {
                            var scanConfig = await ShowScanConfigDialogAsync();
                            if (scanConfig == null) continue;
                            int scanCount = scanConfig.Item1;
                            int threadCount = scanConfig.Item2;

                            if (lblProgressText != null)
                            {
                                lblProgressText.Text = $"华视美达扫描进度:";
                                lblProgressText.Refresh();
                            }
                            if (lblPercent != null)
                            {
                                lblPercent.Text = "0%";
                                lblPercent.Refresh();
                            }
                            if (statusBarRef != null)
                                LayoutStatusBar(statusBarRef);
                            this.Refresh();

                            var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<string, string>>();
                            var cidList = Enumerable.Range(1, scanCount).ToList();
                            int processedCount = 0;

                            await Task.Run(() =>
                            {
                                using (var httpClient = new System.Net.Http.HttpClient())
                                {
                                    httpClient.Timeout = TimeSpan.FromSeconds(2.5);
                                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                                    Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, cid =>
                                    {
                                        string url = $"{rootHttp}/newlive/live/hls/{cid}/live.m3u8";
                                        try
                                        {
                                            var headTask = httpClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                                            var headResp = headTask.Result;
                                            if (headResp.IsSuccessStatusCode)
                                            {
                                                var getTask = httpClient.GetAsync(url);
                                                var getResp = getTask.Result;
                                                if (getResp.IsSuccessStatusCode)
                                                {
                                                    var contentTask = getResp.Content.ReadAsStringAsync();
                                                    string content = contentTask.Result;
                                                    if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                    {
                                                        validResults.Add(Tuple.Create(url, content));
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                        catch { }

                                        try
                                        {
                                            var getTask = httpClient.GetAsync(url);
                                            var getResp = getTask.Result;
                                            if (getResp.IsSuccessStatusCode)
                                            {
                                                var contentTask = getResp.Content.ReadAsStringAsync();
                                                string content = contentTask.Result;
                                                if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                {
                                                    validResults.Add(Tuple.Create(url, content));
                                                }
                                            }
                                        }
                                        catch { }

                                        int current = System.Threading.Interlocked.Increment(ref processedCount);
                                        int pct = (int)(current * 100.0 / scanCount);
                                        if (lblPercent != null && !lblPercent.IsDisposed)
                                        {
                                            try
                                            {
                                                lblPercent.Invoke(new Action(() =>
                                                {
                                                    if (lblPercent != null && !lblPercent.IsDisposed)
                                                        lblPercent.Text = $"{pct}%";
                                                    if (statusBarRef != null && !statusBarRef.IsDisposed)
                                                    {
                                                        progressBarWidth = (int)(statusBarRef.ClientSize.Width * pct / 100);
                                                        if (progressBarWidth > 0)
                                                            UpdateLabelColorsBasedOnProgress();
                                                        else
                                                            RestoreLabelColors();
                                                        statusBarRef.Refresh();
                                                    }
                                                }));
                                            }
                                            catch { }
                                        }
                                    });
                                }
                            });

                            if (lblProgressText != null && !lblProgressText.IsDisposed)
                            {
                                lblProgressText.Text = $"华视美达扫描完成:";
                            }
                            if (lblPercent != null && !lblPercent.IsDisposed)
                            {
                                lblPercent.Text = $"找到{validResults.Count}个";
                            }
                            if (statusBarRef != null)
                                LayoutStatusBar(statusBarRef);
                            this.Refresh();

                            foreach (var result in validResults)
                            {
                                bool exists = allChannels.Any(c => c.Url == result.Item1);
                                if (!exists)
                                {
                                    string[] urlParts = result.Item1.Split('/');
                                    string cid = urlParts.Length > 1 ? urlParts[urlParts.Length - 2] : "";
                                    allChannels.Add(new ChannelInfo { Name = $"{ipPort}_CID{cid}", Url = result.Item1, Group = "解析待处理", Status = "待解析", ParseDateTime = parseTime });
                                    addedCount++;
                                }
                            }

                            if (lblProgressText != null && !lblProgressText.IsDisposed)
                            {
                                lblProgressText.Text = "检测进度:";
                            }
                            if (lblPercent != null && !lblPercent.IsDisposed)
                            {
                                lblPercent.Text = "0%";
                            }
                            if (statusBarRef != null)
                                LayoutStatusBar(statusBarRef);
                        }
                        else
                        {
                            string url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                            bool exists = allChannels.Any(c => c.Url == url);
                            if (!exists)
                            {
                                allChannels.Add(new ChannelInfo { Name = ipPort, Url = url, Group = "解析待处理", Status = "待解析", ParseDateTime = parseTime });
                                addedCount++;
                            }
                        }
                    }
                }
                else
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(8);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                        foreach (string ipPort in ipList)
                        {
                            var parts = ipPort.Split(':');
                            if (parts.Length != 2) continue;

                            string ip = parts[0];
                            string port = parts[1];
                            string rootHttp = $"http://{ip}:{port}";

                            if (ruleName == "智慧光迅")
                            {
                                string url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                                try
                                {
                                    var resp = await httpClient.GetAsync(url);
                                    if (resp.IsSuccessStatusCode)
                                    {
                                        string content = await resp.Content.ReadAsStringAsync();
                                        if (!string.IsNullOrEmpty(content))
                                        {
                                            ParseZhgxTv(content, url, parseTime);
                                            addedCount++;
                                        }
                                    }
                                }
                                catch { }
                            }
                            else if (ruleName == "华视美达")
                            {
                                var scanConfig = await ShowScanConfigDialogAsync();
                                if (scanConfig == null) continue;
                                int scanCount = scanConfig.Item1;
                                int threadCount = scanConfig.Item2;

                                var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<string, string>>();
                                var cidList = Enumerable.Range(1, scanCount).ToList();

                                await Task.Run(() =>
                                {
                                    Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, cid =>
                                    {
                                        string url = $"{rootHttp}/newlive/live/hls/{cid}/live.m3u8";
                                        try
                                        {
                                            var headTask = httpClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                                            var headResp = headTask.Result;
                                            if (headResp.IsSuccessStatusCode)
                                            {
                                                var getTask = httpClient.GetAsync(url);
                                                var getResp = getTask.Result;
                                                if (getResp.IsSuccessStatusCode)
                                                {
                                                    var contentTask = getResp.Content.ReadAsStringAsync();
                                                    string content = contentTask.Result;
                                                    if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                    {
                                                        validResults.Add(Tuple.Create(url, content));
                                                    }
                                                }
                                                return;
                                            }
                                        }
                                        catch { }

                                        try
                                        {
                                            var getTask = httpClient.GetAsync(url);
                                            var getResp = getTask.Result;
                                            if (getResp.IsSuccessStatusCode)
                                            {
                                                var contentTask = getResp.Content.ReadAsStringAsync();
                                                string content = contentTask.Result;
                                                if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                {
                                                    validResults.Add(Tuple.Create(url, content));
                                                }
                                            }
                                        }
                                        catch { }
                                    });
                                });

                                foreach (var result in validResults)
                                {
                                    bool exists = allChannels.Any(c => c.Url == result.Item1);
                                    if (!exists)
                                    {
                                        string[] urlParts = result.Item1.Split('/');
                                        string cid = urlParts.Length > 1 ? urlParts[urlParts.Length - 2] : "";
                                        allChannels.Add(new ChannelInfo { Name = $"{ipPort}_CID{cid}", Url = result.Item1, Group = "解析待处理", Status = "待解析", ParseDateTime = parseTime });
                                        addedCount++;
                                    }
                                }
                            }
                            else
                            {
                                string url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                                try
                                {
                                    var resp = await httpClient.GetAsync(url);
                                    if (resp.IsSuccessStatusCode)
                                    {
                                        string content = await resp.Content.ReadAsStringAsync();
                                        if (!string.IsNullOrEmpty(content))
                                        {
                                            ParseKutvJson(content, url, parseTime);
                                            addedCount++;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                if (addedCount > 0)
                {
                    totalCount = allChannels.Count;
                    RefreshGrid();
                    UpdateEmptyState();
                    UpdateActionButtonsVisibility();
                    SaveChannelList();

                    // 更新状态栏显示解析结果
                    if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
                    {
                        lblDetected.Text = $"已检测: 0/{totalCount}";
                        lblAvailable.Text = $"可用: 0";
                        lblPercent.Text = "0.00%";
                        progressBarWidth = 0;
                        RestoreLabelColors();
                        statusBarRef.PerformLayout();
                        LayoutStatusBar(statusBarRef);
                        statusBarRef.Refresh();
                    }

                    if (!autoParseLink)
                    {
                        DarkMessageBox.Show($"已提取 {addedCount} 条链接到待解析列表\n请点击\"解析链接\"按钮进行解析", "提取完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        DarkMessageBox.Show($"解析完成！\n成功: {addedCount} 个IP\n请点击\"开始检测\"按钮验证链接有效性", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    DarkMessageBox.Show($"未解析到有效直播源\n共检测 {ipList.Count} 个IP，全部失败", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                DarkMessageBox.Show("解析下载出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToM3u(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("#EXTM3U");
                    foreach (var ch in allChannels)
                    {
                        sw.WriteLine($"#EXTINF:-1 group-title=\"{ch.Group}\",{ch.Name}");
                        sw.WriteLine(ch.Url);
                    }
                }
                DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { DarkMessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ExportToTxtMergeUrl(string filePath)
        {
            try
            {
                Dictionary<string, List<string>> merged = new Dictionary<string, List<string>>();
                Dictionary<string, string> groupMap = new Dictionary<string, string>();
                foreach (var ch in allChannels)
                {
                    string n = ch.Name?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(n) || string.IsNullOrWhiteSpace(ch.Url)) continue;
                    if (!merged.ContainsKey(n))
                    {
                        merged[n] = new List<string>();
                        groupMap[n] = ch.Group ?? "";
                    }
                    if (!merged[n].Contains(ch.Url))
                        merged[n].Add(ch.Url);
                }
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var kv in merged)
                    {
                        string urls = string.Join("#", kv.Value);
                        sw.WriteLine($"{kv.Key},{urls}");
                    }
                }
                DarkMessageBox.Show($"成功导出 {merged.Count} 条数据（已合并相同频道，共 {allChannels.Count} 个源）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { DarkMessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ExportToM3uMergeGroup(string filePath)
        {
            try
            {
                Dictionary<string, List<ChannelInfo>> merged = new Dictionary<string, List<ChannelInfo>>();
                foreach (var ch in allChannels)
                {
                    string n = ch.Name?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(n) || string.IsNullOrWhiteSpace(ch.Url)) continue;
                    if (!merged.ContainsKey(n))
                        merged[n] = new List<ChannelInfo>();
                    merged[n].Add(ch);
                }
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    sw.WriteLine("#EXTM3U");
                    foreach (var kv in merged)
                    {
                        string group = kv.Value.FirstOrDefault()?.Group ?? "";
                        foreach (var ch in kv.Value)
                        {
                            sw.WriteLine($"#EXTINF:-1 tvg-name=\"{ch.Name}\" group-title=\"{group}\",{ch.Name}");
                            sw.WriteLine(ch.Url);
                        }
                    }
                }
                DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据（{merged.Count} 个频道，按名称分组）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { DarkMessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ExportToTxt(string filePath, bool merge)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    if (merge)
                    {
                        Dictionary<string, ChannelInfo> unique = new Dictionary<string, ChannelInfo>();
                        foreach (var ch in allChannels)
                        {
                            string n = ch.Name?.Trim() ?? "";
                            if (!unique.ContainsKey(n)) unique[n] = ch;
                            else if (ch.Status == "可用" && unique[n].Status != "可用")
                                unique[n] = ch;
                        }
                        foreach (var kv in unique)
                        {
                            var r = kv.Value;
                            sw.WriteLine($"{r.Name}|{r.Url}|{r.Location}|{r.Resolution}|{r.Speed}|{r.Group}|{r.Status}");
                        }
                        DarkMessageBox.Show($"成功导出 {unique.Count} 条数据（已合并相同频道）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        foreach (var ch in allChannels)
                            sw.WriteLine($"{ch.Name}|{ch.Url}|{ch.Location}|{ch.Resolution}|{ch.Speed}|{ch.Group}|{ch.Status}");
                        DarkMessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex) { DarkMessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void CopyLink()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                string name = dgvData.SelectedRows[0].Cells[0].Value?.ToString() ?? "";
                string url = dgvData.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string text = $"{name}, {url}";
                    CopyTextToClipboard(text);
                }
            }
            else DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void CopyAllLinks()
        {
            if (allChannels.Count == 0)
            {
                DarkMessageBox.Show("列表为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var lines = allChannels
                .Where(c => !string.IsNullOrWhiteSpace(c.Url))
                .Select(c => $"{c.Name}, {c.Url}");
            string text = string.Join(Environment.NewLine, lines);
            CopyTextToClipboard(text);
        }

        private void SelectAllRows()
        {
            if (dgvData.Rows.Count == 0) return;
            dgvData.ClearSelection();
            foreach (DataGridViewRow row in dgvData.Rows)
                row.Selected = true;
            dgvData.Invalidate();
        }

        private void DeleteRow()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                if (DarkMessageBox.Show("确定删除选中行？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in dgvData.SelectedRows.Cast<DataGridViewRow>().ToList())
                    {
                        string url = row.Cells[1].Value?.ToString();
                        var ch = allChannels.FirstOrDefault(c => c.Url == url);
                        if (ch != null) allChannels.Remove(ch);
                    }
                    RefreshGrid();
                    UpdateGroupFilter();
                    totalCount = allChannels.Count;
                    RecalcStats();
                    UpdateStatusBar(); UpdateEmptyState(); UpdateActionButtonsVisibility();
                }
            }
            else DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ViewDetails()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                var r = dgvData.SelectedRows[0];
                DarkMessageBox.Show($"名称: {r.Cells[0].Value}\n链接: {r.Cells[1].Value}\n归属地: {r.Cells[2].Value}\n分辨率: {r.Cells[3].Value}\n响应速度: {r.Cells[4].Value}\n分组: {r.Cells[5].Value}\n状态: {r.Cells[6].Value}",
                    "频道详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ClearInvalidLinks()
        {
            int before = allChannels.Count;
            allChannels.RemoveAll(c => c.Status == "不可用");
            int removed = before - allChannels.Count;
            RefreshGrid();
            UpdateGroupFilter();
            totalCount = allChannels.Count;
            RecalcStats();
            UpdateStatusBar(); UpdateEmptyState(); UpdateActionButtonsVisibility();
        }

        private void ClearAllLinks()
        {
            if (isDetecting)
            {
                DarkMessageBox.Show("检测正在进行中，请先停止检测后再清空列表。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvData.Rows.Count == 0)
            {
                DarkMessageBox.Show("列表为空，无需清空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            allChannels.Clear();
            dgvData.Rows.Clear();
            totalCount = 0; detectedCount = 0; availableCount = 0;
            UpdateGroupFilter();
            UpdateStatusBar(); UpdateEmptyState(); UpdateActionButtonsVisibility();
        }

        /// <summary>
        /// 检测当前网络是否支持IPv6（系统支持且至少有一个活动的IPv6地址）
        /// </summary>
        private bool IsIPv6Supported()
        {
            try
            {
                if (!Socket.OSSupportsIPv6) return false;
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    IPInterfaceProperties props = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                base.WndProc(ref m);
                Screen screen = Screen.FromControl(this);
                System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 16, screen.WorkingArea.Left);
                System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 20, screen.WorkingArea.Top);
                System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 8, screen.WorkingArea.Width);
                System.Runtime.InteropServices.Marshal.WriteInt32(m.LParam, 12, screen.WorkingArea.Height);
                return;
            }
            if (m.Msg == WM_NCHITTEST && this.WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)(m.LParam.ToInt32() >> 16);
                    Point pt = this.PointToClient(new Point(x, y));
                    int border = 6;
                    bool left = pt.X <= border;
                    bool right = pt.X >= this.ClientSize.Width - border;
                    bool top = pt.Y <= border;
                    bool bottom = pt.Y >= this.ClientSize.Height - border;
                    if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }
            if (m.Msg == WM_DPICHANGED)
            {
                int newDpiX = (int)(m.WParam.ToInt64() & 0xFFFF);
                float newScale = newDpiX / 96f;
                if (Math.Abs(newScale - dpiScale) > 0.01f)
                {
                    dpiScale = newScale;
                    DarkMessageBox.DpiScale = dpiScale;
                    config.Initialize(dpiScale);
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke(new Action(() => {
                            this.SuspendLayout();
                            this.Controls.Clear();
                            BuildUI();
                            this.ResumeLayout(true);
                        }));
                    }
                }
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        enum ScanSegType { Number, CctvChannel, PayChannel, WsChannel, MovieChannel, Resolution }

        class ScanSegInfo
        {
            public ScanSegType Type;
            public int GlobalStart;
            public int GlobalEnd;
            public int PathStart;
            public string OriginalText;
            public string Label;
            public List<string> Candidates = new List<string>();
        }

        static readonly Dictionary<string, string[]> CctvChannelMap = new Dictionary<string, string[]>
        {
            { "cctv", new string[]{
                "cctv1","cctv2","cctv3","cctv4","cctv5","cctv5p","cctv6","cctv7","cctv8",
                "cctv9","cctv10","cctv11","cctv12","cctv13","cctv14","cctv15","cctv16","cctv17",
                "cctv4k","cctv8k"
            }},
        };

        static readonly List<KeyValuePair<string, string>> PayChannelList = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("cwjd","重温经典"),
            new KeyValuePair<string,string>("dyjc","CCTV第一剧场"),
            new KeyValuePair<string,string>("fyjc","CCTV风云剧场"),
            new KeyValuePair<string,string>("hjjc","CCTV怀旧剧场"),
            new KeyValuePair<string,string>("gfjs","CCTV兵器科技"),
            new KeyValuePair<string,string>("nxss","CCTV女性时尚"),
            new KeyValuePair<string,string>("sjdl","CCTV世界地理"),
            new KeyValuePair<string,string>("wsjk","CCTV卫生健康"),
            new KeyValuePair<string,string>("ysjp","CCTV央视文化精品"),
            new KeyValuePair<string,string>("fyyy","CCTV风云音乐"),
            new KeyValuePair<string,string>("ystq","CCTV央视台球"),
            new KeyValuePair<string,string>("fyzq","CCTV风云足球"),
            new KeyValuePair<string,string>("gefwq","CCTV高尔夫网球"),
            new KeyValuePair<string,string>("jbty","劲爆体育"),
        };

        static readonly List<KeyValuePair<string, string>> WsChannelList = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("jsws","江苏卫视"),
            new KeyValuePair<string,string>("dfws","东方卫视"),
            new KeyValuePair<string,string>("zjws","浙江卫视"),
            new KeyValuePair<string,string>("sdws","山东卫视"),
            new KeyValuePair<string,string>("hnws","河南卫视"),
            new KeyValuePair<string,string>("hbws","湖北卫视"),
            new KeyValuePair<string,string>("hunantv","湖南卫视"),
            new KeyValuePair<string,string>("hunanws","湖南卫视"),
            new KeyValuePair<string,string>("gdws","广东卫视"),
            new KeyValuePair<string,string>("szws","深圳卫视"),
            new KeyValuePair<string,string>("bjws","北京卫视"),
            new KeyValuePair<string,string>("tjws","天津卫视"),
            new KeyValuePair<string,string>("ahws","安徽卫视"),
            new KeyValuePair<string,string>("jxws","江西卫视"),
            new KeyValuePair<string,string>("lnws","辽宁卫视"),
            new KeyValuePair<string,string>("jlws","吉林卫视"),
            new KeyValuePair<string,string>("hljws","黑龙江卫视"),
            new KeyValuePair<string,string>("hebeiws","河北卫视"),
            new KeyValuePair<string,string>("hebs","河北卫视"),
            new KeyValuePair<string,string>("sxws","山西卫视"),
            new KeyValuePair<string,string>("sxxws","陕西卫视"),
            new KeyValuePair<string,string>("gsws","甘肃卫视"),
            new KeyValuePair<string,string>("qhws","青海卫视"),
            new KeyValuePair<string,string>("scws","四川卫视"),
            new KeyValuePair<string,string>("ynws","云南卫视"),
            new KeyValuePair<string,string>("gzws","贵州卫视"),
            new KeyValuePair<string,string>("gxws","广西卫视"),
            new KeyValuePair<string,string>("nmgws","内蒙古卫视"),
            new KeyValuePair<string,string>("nmg","内蒙古卫视"),
            new KeyValuePair<string,string>("xjws","新疆卫视"),
            new KeyValuePair<string,string>("xzws","西藏卫视"),
            new KeyValuePair<string,string>("nxws","宁夏卫视"),
            new KeyValuePair<string,string>("hnws2","海南卫视"),
            new KeyValuePair<string,string>("lyws","旅游卫视"),
            new KeyValuePair<string,string>("cqws","重庆卫视"),
            new KeyValuePair<string,string>("fjws","福建卫视"),
            new KeyValuePair<string,string>("dnws","东南卫视"),
        };

        static readonly List<KeyValuePair<string, string>> MovieChannelList = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("vlcl","成龙影院"),
            new KeyValuePair<string,string>("vlzxc","周星驰影院"),
            new KeyValuePair<string,string>("vllzy","林正英影院"),
            new KeyValuePair<string,string>("sscy","邵氏楚原专场"),
            new KeyValuePair<string,string>("ssgf","邵氏功夫电影"),
            new KeyValuePair<string,string>("ssnx","邵氏女侠"),
            new KeyValuePair<string,string>("ssqa","邵氏奇案"),
            new KeyValuePair<string,string>("sswx","邵氏武侠电影"),
            new KeyValuePair<string,string>("ssxj","邵氏喜剧电影"),
            new KeyValuePair<string,string>("sszc","邵氏张彻专场"),
        };

        static readonly string[] ResolutionList = new string[] { "4k", "2160p", "1080p", "720p", "540p", "480p", "360p", "sd", "hd" };

        private void ShowScanSourceDialog()
        {
            // ==================== 主题颜色配置（深色/浅色自动适配） ====================
            bool isDark = theme.Name == "深色";
            // [主绿色] 按钮、选中状态、成功提示的主要颜色：深色偏亮绿，浅色标准绿
            Color GreenMain = isDark ? Color.FromArgb(70, 200, 110) : Color.FromArgb(46, 189, 96);
            // [深绿色] 按钮悬停状态颜色：比主绿色略深，提供视觉反馈
            Color GreenDark = isDark ? Color.FromArgb(55, 180, 95) : Color.FromArgb(39, 174, 86);
            // [灰色文字] 辅助文字、占位符、非选中状态文字颜色
            Color GrayText = isDark ? theme.TextSecondary : Color.FromArgb(153, 153, 153);
            // [分割线颜色] 标题栏下方分割线、面板分隔线颜色
            Color GrayLine = isDark ? theme.Border : Color.FromArgb(230, 232, 238);
            // [边框颜色] 输入框、面板的边框颜色（未聚焦时）
            Color GrayBorder = isDark ? theme.Border : Color.FromArgb(200, 203, 210);
            // [深色文字] 主要文字、标签、按钮文字颜色（浅色主题下为深色）
            Color DarkText = isDark ? theme.TextPrimary : Color.FromArgb(51, 51, 51);
            // [红色高亮] 必填项星号、错误提示、警告文字颜色
            Color RedHighlight = isDark ? theme.ErrorColor : Color.FromArgb(231, 76, 60);
            // [浅色按钮背景] 数字面板加减按钮、次要按钮的背景色
            Color LightBtnBg = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(248, 249, 250);
            // [输入框背景] 文本框、数字面板的背景色（浅色主题使用浅灰，与窗口背景区分）
            Color InputBg = isDark ? theme.Surface : Color.FromArgb(245, 245, 247);
            // [输入框聚焦边框] 输入框获得焦点时的边框高亮颜色（与主绿色一致）
            Color InputFocusBorder = GreenMain;
            // [面板背景] 整个对话框、所有面板的背景色
            Color PanelBg = isDark ? theme.Bg : Color.White;
            // [步骤指示器线条颜色] 步骤指示器中未完成步骤的灰色线条
            Color StepLineGray = isDark ? Color.FromArgb(80, 80, 92) : Color.FromArgb(210, 213, 220);
            // [数字面板悬停] 数字面板加减按钮的鼠标悬停背景色
            Color NumPadHover = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(235, 236, 240);
            // [数字面板按下] 数字面板加减按钮的鼠标按下背景色
            Color NumPadDown = isDark ? Color.FromArgb(75, 75, 85) : Color.FromArgb(225, 226, 232);
            // [关闭按钮悬停] 标题栏关闭按钮的鼠标悬停背景色
            Color CloseHover = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(245, 245, 245);
            // [关闭按钮按下] 标题栏关闭按钮的鼠标按下背景色
            Color CloseDown = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(230, 230, 230);

            // ====== 窗口尺寸配置 ======
            // [窗口宽度] 固定900px，足够容纳完整URL和操作按钮，DPI自适应缩放
            int DLG_W = SX(900);
            // [窗口高度] 580px，包含：标题栏(52px) + 分割线(1px) + 步骤指示器(80px) + 内容区(447px)
            // 增大高度以容纳步骤2的所有控件，确保按钮和信息栏完整显示
            int DLG_H = SY(620);
            
            // ====== 布局间距配置 ======
            // [内容边距] 左右两侧留白，避免控件贴边，增强视觉舒适度
            int CONTENT_PAD = SX(32);
            // [控件间距] 控件之间的垂直间距，保持视觉平衡，避免拥挤
            int CONTROL_GAP = SY(12);
            // [分组间距] 逻辑分组（如标签+输入框、提示栏）之间的间距，比控件间距略大
            int GROUP_GAP = SY(16);
            // [顶部留白] 步骤内容区与步骤指示器之间的间距
            int TOP_PADDING = SY(20);
            
            // ====== 控件尺寸配置 ======
            // [输入框高度] 文本框和数字面板的统一高度（增大至44px，更清晰易读）
            int INPUT_HEIGHT = SY(44);
            // [按钮高度] 操作按钮（下一步、取消）的统一高度
            int BTN_HEIGHT = SY(38);
            // [提示栏高度] 智能提示、范围提示等信息栏高度（容纳2行文字+上下内边距）
            int HINT_HEIGHT = SY(68);
            // [标题栏高度] 对话框顶部标题栏高度（包含标题文字和关闭按钮）
            int TITLE_BAR_H = SY(52);
            // [步骤指示器高度] 步骤进度条区域高度（圆形指示器+标签文字）
            int STEP_INDICATOR_H = SY(80);
            
            // ====== 字体配置 ======
            // [基础字体] 整个对话框的默认字体（10pt）
            Font BASE_FONT = GetFont(9f);
            // [标题字体] 对话框标题"直播源生成器"（14pt加粗）
            Font TITLE_FONT = GetFont(14f, FontStyle.Bold);
            // [标签字体] 字段标签（如"直播源地址"、"起始数字"）（10.5pt）
            Font LABEL_FONT = GetFont(10.5f);
            // [提示字体] 提示信息（如"智能识别"、"最大生成范围"）（10pt）
            Font HINT_FONT = GetFont(10f);
            // [URL字体] URL显示和输入框使用的等宽字体（Consolas 9.5pt，确保数字对齐）
            Font URL_FONT = new Font("Consolas", SF(9.5f));
            // [URL选中字体] URL中选中部分的高亮字体（Consolas 10.5pt，加粗下划线）
            Font URL_SEL_FONT = new Font("Consolas", SF(10.5f), FontStyle.Bold | FontStyle.Underline);
            // [URL加粗字体] URL中强调部分的字体（Consolas 10.5pt加粗）
            Font URL_BOLD_FONT = new Font("Consolas", SF(10.5f), FontStyle.Bold);
            // [按钮字体] 操作按钮的字体（10.5pt加粗，提高可读性）
            Font BTN_FONT = GetFont(10.5f, FontStyle.Bold);
            // [数字面板字体] 数字面板加减按钮的字体（14pt加粗）
            Font NUMPAD_BTN_FONT = GetFont(14f, FontStyle.Bold);
            // [数字输入字体] 数字面板输入框的字体（12pt，居中显示）
            Font NUM_INPUT_FONT = GetFont(12f);

            Form dlg = new Form
            {
                Text = "直播源生成器",
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = PanelBg,
                ClientSize = new Size(DLG_W, DLG_H),
                Font = BASE_FONT,
                KeyPreview = true
            };
            CenterForm(dlg, this);

            string CleanUrlToken(string token)
            {
                if (string.IsNullOrWhiteSpace(token)) return "";
                string t = token.Trim().Trim().Trim('"', ' ', '`', '\t');
                int btStart = t.IndexOf('`');
                if (btStart >= 0)
                {
                    int btEnd = t.IndexOf('`', btStart + 1);
                    if (btEnd > btStart)
                        t = t.Substring(btStart + 1, btEnd - btStart - 1);
                }
                return t.Trim().Trim('"', ' ', '`');
            }

            string ExtractUrlFromText(string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return text;

                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (text.Contains("#EXTINF"))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string tl = lines[i].Trim();
                        if (tl.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                        {
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                string ul = CleanUrlToken(lines[j]);
                                if (ul.StartsWith("#")) continue;
                                if (IsUrl(ul)) return ul;
                            }
                        }
                    }
                }

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    int commaIdx = line.IndexOf(',');
                    if (commaIdx > 0)
                    {
                        string afterComma = line.Substring(commaIdx + 1);
                        string cleaned = CleanUrlToken(afterComma);
                        if (IsUrl(cleaned)) return cleaned;

                        var btMatch = System.Text.RegularExpressions.Regex.Match(afterComma, @"`([^`]+)`");
                        if (btMatch.Success && IsUrl(btMatch.Groups[1].Value.Trim()))
                            return btMatch.Groups[1].Value.Trim();
                    }

                    string wholeLine = CleanUrlToken(line);
                    if (IsUrl(wholeLine)) return wholeLine;
                }

                foreach (string rawLine in lines)
                {
                    string url = System.Text.RegularExpressions.Regex.Match(rawLine, @"(https?://[^\s`""<>]+)").Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(url) && IsUrl(url.Trim())) return url.Trim();
                }

                return text;
            }

            bool IsUrl(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                return s.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                       s.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) ||
                       s.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
            }

            List<ChannelInfo> ParseChannelList(string text)
            {
                var result = new List<ChannelInfo>();
                if (string.IsNullOrWhiteSpace(text)) return result;
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string pendingName = null;
                string pendingGroup = "";

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        string info = line.Substring(8);
                        int ci = info.LastIndexOf(',');
                        string attrs = ci >= 0 ? info.Substring(0, ci) : info;
                        string chName = ci >= 0 ? info.Substring(ci + 1).Trim().Trim('"', ' ', '`') : "";
                        var gm = System.Text.RegularExpressions.Regex.Match(attrs, @"group-title\s*=\s*""([^""]*)""");
                        string grp = gm.Success ? gm.Groups[1].Value.Trim() : "";
                        var tnm = System.Text.RegularExpressions.Regex.Match(attrs, @"tvg-name\s*=\s*""([^""]*)""");
                        if (tnm.Success && !string.IsNullOrWhiteSpace(tnm.Groups[1].Value))
                        {
                            string tn = tnm.Groups[1].Value.Trim().Trim('"', ' ', '`');
                            if (!string.IsNullOrWhiteSpace(tn) && string.IsNullOrWhiteSpace(chName)) chName = tn;
                        }
                        if (string.IsNullOrWhiteSpace(chName)) chName = "生成器导入";
                        pendingName = chName;
                        pendingGroup = grp;
                        continue;
                    }

                    if (line.StartsWith("#")) continue;

                    string name = null;
                    string url = null;

                    int commaIdx = line.LastIndexOf(',');
                    if (commaIdx > 0 && commaIdx < line.Length - 1)
                    {
                        string before = line.Substring(0, commaIdx).Trim().Trim('"', ' ', '`', '\t', '，');
                        string after = line.Substring(commaIdx + 1);
                        string cleaned = CleanUrlToken(after);
                        if (IsUrl(cleaned) && !string.IsNullOrWhiteSpace(before))
                        {
                            name = before;
                            url = cleaned;
                        }
                    }

                    if (url == null)
                    {
                        int tabIdx = line.IndexOf('\t');
                        if (tabIdx > 0)
                        {
                            string before = line.Substring(0, tabIdx).Trim().Trim('"', ' ', '`');
                            string after = line.Substring(tabIdx + 1);
                            string cleaned = CleanUrlToken(after);
                            if (IsUrl(cleaned) && !string.IsNullOrWhiteSpace(before))
                            {
                                name = before;
                                url = cleaned;
                            }
                        }
                    }

                    if (url == null)
                    {
                        string whole = CleanUrlToken(line);
                        if (IsUrl(whole))
                        {
                            url = whole;
                            name = pendingName ?? $"源{result.Count + 1}";
                            pendingName = null;
                        }
                    }

                    if (url != null && IsUrl(url))
                    {
                        if (string.IsNullOrWhiteSpace(name)) name = pendingName ?? $"源{result.Count + 1}";
                        result.Add(new ChannelInfo { Name = name, Url = url, Group = string.IsNullOrEmpty(pendingGroup) ? "生成器导入" : pendingGroup, Status = "未检测", Visible = true });
                        pendingName = null;
                        pendingGroup = "";
                    }
                }
                return result;
            }

            ContextMenuStrip CreateInputContextMenu(TextBox targetTb)
            {
                ContextMenuStrip cms = new ContextMenuStrip
                {
                    Font = GetFont(9.5f),
                    Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg))),
                    BackColor = theme.Surface,
                    ForeColor = theme.TextPrimary
                };
                ToolStripMenuItem miCut = new ToolStripMenuItem("剪切(T)", null, (s, e) => targetTb.Cut()) { ShortcutKeyDisplayString = "Ctrl+X" };
                ToolStripMenuItem miCopy = new ToolStripMenuItem("复制(C)", null, (s, e) => targetTb.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" };
                ToolStripMenuItem miPaste = new ToolStripMenuItem("粘贴(P)", null, (s, e) => targetTb.Paste()) { ShortcutKeyDisplayString = "Ctrl+V" };
                ToolStripMenuItem miSelectAll = new ToolStripMenuItem("全选(A)", null, (s, e) => targetTb.SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" };
                ToolStripMenuItem miClear = new ToolStripMenuItem("清空(L)", null, (s, e) => { targetTb.Clear(); targetTb.Focus(); });
                cms.Items.AddRange(new ToolStripItem[] { miCut, miCopy, miPaste, miSelectAll, new ToolStripSeparator(), miClear });
                cms.Opening += (s, e) =>
                {
                    bool hasText = targetTb.SelectionLength > 0;
                    bool canPaste = Clipboard.ContainsText();
                    miCut.Enabled = hasText && !targetTb.ReadOnly;
                    miCopy.Enabled = hasText;
                    miPaste.Enabled = canPaste && !targetTb.ReadOnly;
                    miSelectAll.Enabled = targetTb.TextLength > 0;
                };
                return cms;
            }

            void StyleGreenButton(Button btn)
            {
                // [按钮样式] 绿色主按钮统一样式配置
                btn.FlatStyle = FlatStyle.Flat;           // 扁平样式，无边框凸起效果
                btn.FlatAppearance.BorderSize = 0;        // 边框宽度为0
                btn.FlatAppearance.MouseOverBackColor = GreenDark;      // 鼠标悬停时背景色变为深绿色
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(34, 160, 76);  // 鼠标按下时背景色更深
                btn.BackColor = GreenMain;                // 默认背景色为主绿色
                btn.ForeColor = Color.White;              // 文字颜色为白色
                btn.Font = BTN_FONT;                      // 使用按钮字体（10.5pt加粗）
                btn.Cursor = Cursors.Hand;                // 鼠标变为手型
                StyleRoundButton(btn, SX(8));             // 设置圆角半径为8px（DPI自适应）
            }

            // ====== 标题栏 ======
            // [标题栏面板] 对话框顶部标题区域，包含标题和关闭按钮，可拖动
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,     // 顶部停靠，自动填充宽度
                Height = TITLE_BAR_H,     // 标题栏高度52px
                BackColor = PanelBg       // 使用面板背景色
            };
            
            // [标题标签] 对话框标题"🔍 直播源生成器"
            Label lblTitle = new Label
            {
                Text = "🔍 直播源生成器",                // 标题文字，带放大镜图标
                Font = TITLE_FONT,                      // 使用标题字体（14pt加粗）
                ForeColor = DarkText,                   // 深色文字，确保对比度
                Location = new Point(CONTENT_PAD, (TITLE_BAR_H - SY(22)) / 2),  // 垂直居中，水平左对齐
                AutoSize = true                         // 自动调整大小以适应文字
            };
            titleBar.Controls.Add(lblTitle);

            // [关闭按钮] 标题栏右侧的关闭按钮（✕）
            Button btnClose = new Button
            {
                Text = "✕",                                  // 关闭图标
                FlatStyle = FlatStyle.Flat,                  // 扁平样式
                Size = new Size(SX(40), TITLE_BAR_H),        // 宽度40px，高度与标题栏一致
                Location = new Point(DLG_W - SX(40), 0),     // 位于窗口最右侧
                ForeColor = GrayText,                        // 灰色文字，不显眼
                BackColor = PanelBg,                         // 与标题栏背景一致
                Font = GetFont(11f),                         // 11pt字体，图标清晰
                Cursor = Cursors.Hand                        // 鼠标变为手型
            };
            btnClose.FlatAppearance.BorderSize = 0;           // 无边框
            btnClose.FlatAppearance.MouseOverBackColor = CloseHover;  // 悬停时背景色变深
            btnClose.FlatAppearance.MouseDownBackColor = CloseDown;   // 按下时背景色更深
            btnClose.Click += (s, e) => dlg.Close();         // 点击关闭对话框
            titleBar.Controls.Add(btnClose);

            void MakeDraggable(Control c)
            {
                c.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    { ReleaseCapture(); SendMessage(dlg.Handle, 0xA1, 0x2, 0); }
                };
            }
            MakeDraggable(titleBar);
            MakeDraggable(lblTitle);

            dlg.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) dlg.Close(); };

            Panel sepTitle = new Panel { Dock = DockStyle.Top, Height = SX(1), BackColor = GrayLine };

            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg
            };

            int currentStep = 1;
            string step1Url = "";
            List<ScanSegInfo> segs = null;
            string segBaseUrl = "";
            int selectedSegIndex = -1;
            long fromVal = 0, toVal = 0;
            bool segPadZero = false;
            int segPadWidth = 0;
            bool isCustomRangeMode = false;
            long customRangeStart = 0, customRangeEnd = 0;
            int customPadWidth = 0;
            bool customPadZero = false;
            int customReplacePos = 0;
            int customReplaceLen = 0;
            string customUrlTemplate = "";
            int[] subSegStart = null;
            int[] subSegLen = null;
            int selectedResSegIndex = -1;
            bool multiResEnabled = false;

            // ====== 步骤指示器 ======
            // [步骤指示器面板] 显示当前向导进度，包含3个步骤：输入源地址 → 选择字段 → 设置范围
            Panel stepIndicator = new Panel
            {
                Dock = DockStyle.Top,     // 顶部停靠
                Height = STEP_INDICATOR_H,  // 高度80px，容纳圆形指示器和标签文字
                BackColor = PanelBg       // 使用面板背景色
            };
            
            // [步骤标签数组] 三个步骤的显示文字
            string[] stepLabelsArr = { "输入源地址", "选择字段", "设置范围" };
            // [步骤圆半径] 步骤指示器中圆形的半径（12px，DPI自适应）
            int stepCircleR = SX(12);

            stepIndicator.Paint += (s, pe) =>
            {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                int w = stepIndicator.ClientSize.Width;
                int h = stepIndicator.Height;

                float circleD = stepCircleR * 2;
                float circleY = SY(10);
                float lineY = circleY + stepCircleR;
                float sectionW = w / 3f;
                float[] circleCX = new float[3];
                for (int i = 0; i < 3; i++)
                    circleCX[i] = sectionW * i + sectionW / 2f;

                float maxLabelWidth = sectionW - SX(40);
                float fontSize = 10.5f;
                Font stepLblFont = null;
                Font stepLblFontBold = null;
                SizeF lblSize;
                do
                {
                    stepLblFont?.Dispose();
                    stepLblFontBold?.Dispose();
                    stepLblFont = GetFont(fontSize);
                    stepLblFontBold = GetFont(fontSize, FontStyle.Bold);
                    lblSize = g.MeasureString("输入源地址", stepLblFontBold);
                    fontSize -= 0.5f;
                } while (lblSize.Width > maxLabelWidth && fontSize >= 8f);

                float circleFontSize = fontSize * 0.9f;
                if (circleFontSize < 7.5f) circleFontSize = 7.5f;
                Font numFont = GetFont(circleFontSize, FontStyle.Bold);
                Font checkFont = GetFont(circleFontSize + 1f, FontStyle.Bold);

                float lblGap = h - circleY - circleD - lblSize.Height;
                if (lblGap < SY(8)) lblGap = SY(8);

                using (Pen linePen = new Pen(GrayLine, 2f))
                using (Pen greenPen = new Pen(GreenMain, 2f))
                using (Brush greenBrush = new SolidBrush(GreenMain))
                using (Brush whiteBrush = new SolidBrush(PanelBg))
                using (Brush grayBrush = new SolidBrush(GrayText))
                using (numFont)
                using (checkFont)
                using (stepLblFont)
                using (stepLblFontBold)
                using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    for (int seg = 0; seg < 2; seg++)
                    {
                        Pen p = (currentStep >= seg + 2) ? greenPen : linePen;
                        float x1 = circleCX[seg] + stepCircleR + SX(10);
                        float x2 = circleCX[seg + 1] - stepCircleR - SX(10);
                        g.DrawLine(p, x1, lineY, x2, lineY);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = circleCX[i];
                        RectangleF circleRect = new RectangleF(cx - stepCircleR, circleY, circleD, circleD);
                        bool isCompleted = i < currentStep - 1;
                        bool isCurrent = i == currentStep - 1;

                        if (isCompleted)
                        {
                            g.FillEllipse(greenBrush, circleRect);
                            g.DrawEllipse(greenPen, circleRect);
                            RectangleF strRect = new RectangleF(cx - stepCircleR, circleY + 1, circleD, circleD);
                            g.DrawString("✓", checkFont, whiteBrush, strRect, sfCenter);
                        }
                        else if (isCurrent)
                        {
                            g.FillEllipse(greenBrush, circleRect);
                            g.DrawEllipse(greenPen, circleRect);
                            g.DrawString((i + 1).ToString(), numFont, whiteBrush, circleRect, sfCenter);
                        }
                        else
                        {
                            g.FillEllipse(whiteBrush, circleRect);
                            using (Pen grayPen = new Pen(StepLineGray, 2f))
                                g.DrawEllipse(grayPen, circleRect);
                            g.DrawString((i + 1).ToString(), numFont, grayBrush, circleRect, sfCenter);
                        }

                        Brush lblBrush = isCurrent || isCompleted ? (Brush)new SolidBrush(GreenMain) : grayBrush;
                        Font lblF = isCurrent ? stepLblFontBold : stepLblFont;
                        SizeF labelSize = g.MeasureString(stepLabelsArr[i], lblF);
                        float lblX = cx - labelSize.Width / 2;
                        float lblY = circleY + circleD + lblGap;
                        g.DrawString(stepLabelsArr[i], lblF, lblBrush, lblX, lblY);
                    }
                }
            };

            Panel stepContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg
            };

            contentHost.Controls.Add(stepContainer);
            
            Panel stepIndicatorTopGap = new Panel
            {
                Dock = DockStyle.Top,
                Height = SY(20),
                BackColor = PanelBg
            };
            contentHost.Controls.Add(stepIndicatorTopGap);
            
            contentHost.Controls.Add(stepIndicator);

            Panel step1Panel = new Panel { Dock = DockStyle.Fill, BackColor = PanelBg };
            Panel step2Panel = new Panel { Dock = DockStyle.Fill, BackColor = PanelBg, Visible = false };
            Panel step3Panel = new Panel { Dock = DockStyle.Fill, BackColor = PanelBg, Visible = false };

            stepContainer.Controls.Add(step2Panel);
            stepContainer.Controls.Add(step3Panel);
            stepContainer.Controls.Add(step1Panel);

            // ====== 步骤1：输入源地址 ======
            // [布局结构] 标签(28px) → 间距(12px) → 输入框(44px) → 间距(12px) → 提示栏(68px)
            
            // ------ 标签 ------
            int step1Top = TOP_PADDING;  // 步骤1内容起始位置（距步骤指示器底部20px）
            
            // [步骤1标签] "直播源地址"，带必填星号
            Label lblStep1Hint = new Label
            {
                Text = "直播源地址",
                Font = LABEL_FONT,    // 标签字体10.5pt
                ForeColor = DarkText, // 深色文字
                Location = new Point(CONTENT_PAD, step1Top),  // 左对齐，距左侧边距32px
                AutoSize = true,      // 自动调整大小
                BackColor = PanelBg   // 透明背景
            };
            int hint1W = TextRenderer.MeasureText(lblStep1Hint.Text, lblStep1Hint.Font).Width;
            int hint1H = TextRenderer.MeasureText(lblStep1Hint.Text, lblStep1Hint.Font).Height;
            
            // [必填星号] 红色星号，标识该字段为必填项
            Label lblStep1Star = new Label
            {
                Text = "*",
                Font = LABEL_FONT,      // 与标签字体一致
                ForeColor = RedHighlight,  // 红色高亮
                Location = new Point(CONTENT_PAD + hint1W + SX(3), step1Top),  // 紧跟标签右侧，间距3px
                AutoSize = true,
                BackColor = PanelBg
            };
            step1Panel.Controls.Add(lblStep1Hint);
            step1Panel.Controls.Add(lblStep1Star);

            // ------ 输入框 ------
            // 输入框顶部位置 = 标签高度 + 控件间距
            int step1InputTop = step1Top + hint1H + CONTROL_GAP;  
            
            // [URL输入框] 用于输入直播源地址，支持标准URL或自定义范围格式
            TextBox txtStep1Url = new TextBox
            {
                Location = new Point(CONTENT_PAD, step1InputTop),  // 左对齐，距左侧边距32px
                Width = DLG_W - CONTENT_PAD * 2,                   // 宽度 = 窗口宽度 - 左右边距
                Height = INPUT_HEIGHT,                              // 输入框高度44px
                Font = URL_FONT,                                    // 等宽字体9.5pt（Consolas）
                BorderStyle = BorderStyle.None,                     // 无边框（自定义绘制圆角边框）
                BackColor = InputBg,                                // 输入框背景色（浅灰/深色Surface）
                Padding = new Padding(SX(8), SX(2), SX(8), SX(2))   // 内边距，防止文字被边框截断
            };
            // 设置圆角路径（半径6px）
            txtStep1Url.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, txtStep1Url.Width, txtStep1Url.Height), SX(6)));
            // 自定义绘制边框（聚焦时绿色高亮，未聚焦时灰色）
            txtStep1Url.Paint += (s, pe) =>
            {
                Color borderColor = txtStep1Url.Focused ? InputFocusBorder : Color.FromArgb(100, 100, 100);  // 深灰色边框，浅色主题下更醒目
                using (Pen p = new Pen(borderColor, 2.5f))
                {
                    pe.Graphics.DrawPath(p, CreateRoundedRectPath(new Rectangle(0, 0, txtStep1Url.Width - 1, txtStep1Url.Height - 1), SX(6)));
                }
            };
            // 设置右键菜单（剪切/复制/粘贴/全选/清空）
            txtStep1Url.ContextMenuStrip = CreateInputContextMenu(txtStep1Url);
            
            // [占位符颜色] 提示文字颜色，加深以提高可读性（深色主题下稍亮，浅色主题下稍深）
            Color phColor = isDark ? Color.FromArgb(120, 125, 135) : Color.FromArgb(130, 133, 140);
            bool phStep1Active = true;
            // 设置占位符文字（提示用户支持的输入格式）
            txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选生成段";
            txtStep1Url.ForeColor = phColor;
            // 获取焦点时清除占位符，恢复正常文字颜色
            txtStep1Url.GotFocus += (s, e) =>
            {
                if (phStep1Active) { phStep1Active = false; txtStep1Url.Text = ""; txtStep1Url.ForeColor = DarkText; }
                txtStep1Url.Invalidate();
            };
            // 失去焦点时如果输入框为空，恢复占位符
            txtStep1Url.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtStep1Url.Text))
                { phStep1Active = true; txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选生成段"; txtStep1Url.ForeColor = phColor; }
            };
            step1Panel.Controls.Add(txtStep1Url);

            // ------ 智能提示栏 ------
            // 提示栏顶部位置 = 输入框顶部 + 输入框高度 + 控件间距
            int step1HintTop = step1InputTop + INPUT_HEIGHT + CONTROL_GAP;  
            
            // [智能提示面板] 显示使用说明和格式示例
            Panel pnlSmartHint = new Panel
            {
                Location = new Point(CONTENT_PAD, step1HintTop),  // 左对齐，距左侧边距32px
                Size = new Size(DLG_W - CONTENT_PAD * 2, HINT_HEIGHT),  // 宽度 = 窗口宽度 - 左右边距，高度68px
                BackColor = theme.StatusTagBg,                    // 使用主题的提示背景色（浅绿色）
                BorderStyle = BorderStyle.None                    // 无边框（自定义绘制圆角边框）
            };
            // 设置圆角路径（半径6px）
            pnlSmartHint.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, pnlSmartHint.Width, pnlSmartHint.Height), SX(6)));
            
            // [智能提示标签] 提示文字，带💡图标
            Label lblStep1SmartHint = new Label
            {
                Text = "💡 智能识别：输入标准URL进入向导模式；输入带 [起始-结束] 的地址直接生成\n（如 http://example.com/[1-100].m3u8）",
                Font = HINT_FONT,      // 提示字体10pt
                ForeColor = theme.SuccessColor,  // 成功提示颜色（绿色）
                Location = new Point(SX(16), SY(8)),  // 提示栏内边距（左16px，上8px）
                AutoSize = false,      // 固定大小，支持换行
                Size = new Size(DLG_W - CONTENT_PAD * 2 - SX(32), HINT_HEIGHT - SY(16)),  // 减去左右内边距
                BackColor = theme.StatusTagBg  // 透明背景
            };
            pnlSmartHint.Controls.Add(lblStep1SmartHint);
            // 自定义绘制边框（使用主题的提示边框色）
            pnlSmartHint.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(theme.StatusTagBorder, 1f))
                {
                    pe.Graphics.DrawPath(p, CreateRoundedRectPath(new Rectangle(0, 0, pnlSmartHint.Width - 1, pnlSmartHint.Height - 1), SX(6)));
                }
            };
            step1Panel.Controls.Add(pnlSmartHint);

            // ====== 步骤2：选择字段 ======
            // [布局结构] 标签(28px) → 间距(12px) → URL显示区(310px)
            
            // ------ 标签 ------
            int step2Top = TOP_PADDING;  // 步骤2内容起始位置（距步骤指示器底部20px）
            
            // [步骤2标签] "请选择要生成的字符段"，带必填星号
            Label lblStep2Hint = new Label
            {
                Text = "请选择要生成的字符段",
                Font = LABEL_FONT,    // 标签字体10.5pt
                ForeColor = DarkText, // 深色文字
                Location = new Point(CONTENT_PAD, step2Top),
                AutoSize = true,
                BackColor = PanelBg
            };
            int hint2W = TextRenderer.MeasureText(lblStep2Hint.Text, lblStep2Hint.Font).Width;
            int hint2H = TextRenderer.MeasureText(lblStep2Hint.Text, lblStep2Hint.Font).Height;
            
            // [必填星号] 红色星号，标识该字段为必填项
            Label lblStep2Star = new Label
            {
                Text = "*",
                Font = LABEL_FONT,
                ForeColor = RedHighlight,
                Location = new Point(CONTENT_PAD + hint2W + SX(3), step2Top),
                AutoSize = true,
                BackColor = PanelBg
            };
            step2Panel.Controls.Add(lblStep2Hint);
            step2Panel.Controls.Add(lblStep2Star);

            // ------ URL显示容器 ------
            // URL显示区顶部位置 = 标签高度 + 控件间距
            int step2ContentTop = step2Top + hint2H + CONTROL_GAP;  
            
            // [URL显示面板] 用于展示解析后的URL片段，用户可选择要生成的字符段
            Panel segListContainer = new Panel
            {
                Location = new Point(CONTENT_PAD, step2ContentTop),
                Width = DLG_W - CONTENT_PAD * 2,
                Height = SY(310),        // 高度310px，可容纳多个URL片段
                BackColor = PanelBg,
                AutoScroll = true        // 支持垂直滚动
            };
            step2Panel.Controls.Add(segListContainer);

            // ====== 步骤3：设置范围 ======
            // [布局结构] 标签(28px) → 间距(12px) → 数字面板(44px) → 间距(24px) → 提示栏(72px)
            
            // ------ 数字面板配置 ------
            int numPanelW = SX(180);   // 数字面板宽度180px（包含加减按钮和输入框）
            int step3Top = TOP_PADDING;     // 步骤3内容起始位置
            
            // 计算两个数字面板的位置，使其在窗口中水平居中分布
            int panelTotalW = numPanelW * 2 + SX(180);  // 两个面板 + 中间间距180px
            int pFromX = (DLG_W - panelTotalW) / 2;     // 起始面板X位置（水平居中）
            int pToX = pFromX + numPanelW + SX(180);    // 结束面板X位置（距起始面板180px）
            
            // ------ 标签 ------
            // [起始数字标签] "起始数字"，居中对齐
            Label lblStep3From = new Label
            {
                Text = "起始数字",
                Font = LABEL_FONT,    // 标签字体10.5pt
                ForeColor = DarkText,
                Location = new Point(pFromX, step3Top),
                AutoSize = false,
                Size = new Size(numPanelW, SY(28)),     // 宽度与数字面板一致，高度28px
                TextAlign = ContentAlignment.MiddleCenter,  // 文字居中对齐
                BackColor = PanelBg
            };
            step3Panel.Controls.Add(lblStep3From);
            
            // [结束数字标签] "结束数字"，居中对齐
            Label lblStep3To = new Label
            {
                Text = "结束数字",
                Font = LABEL_FONT,
                ForeColor = DarkText,
                Location = new Point(pToX, step3Top),
                AutoSize = false,
                Size = new Size(numPanelW, SY(28)),     // 宽度与数字面板一致，高度28px
                TextAlign = ContentAlignment.MiddleCenter,  // 文字居中对齐
                BackColor = PanelBg
            };
            step3Panel.Controls.Add(lblStep3To);

            TextBox txtFrom = null, txtTo = null;
            Panel pnlTextOptions = null;
            CheckedListBox clstTextCandidates = null;
            List<string> selectedTextValues = null;

            // ====== 数字面板创建方法 ======
            // [创建数字面板] 创建一个包含减号按钮、输入框、加号按钮的数字选择面板
            // 参数: x - 面板X位置, initialVal - 初始数值, outTextBox - 返回的输入框引用
            Panel CreateNumPanel(int x, long initialVal, out TextBox outTextBox)
            {
                // [数字面板容器] 包含加减按钮和输入框的面板
                Panel p = new Panel
                {
                    Location = new Point(x, 0),
                    Width = numPanelW,
                    Height = INPUT_HEIGHT,
                    BackColor = InputBg,
                    BorderStyle = BorderStyle.None
                };
                // 设置圆角路径（半径6px）
                p.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, numPanelW, INPUT_HEIGHT), SX(6)));
                // 自定义绘制边框（深灰色，圆角）
                p.Paint += (s, pe) =>
                {
                    Color borderColor = isDark ? Color.FromArgb(100, 105, 115) : Color.FromArgb(130, 135, 145);
                    using (Pen pen = new Pen(borderColor, 2.5f))
                    {
                        pe.Graphics.DrawPath(pen, CreateRoundedRectPath(new Rectangle(0, 0, numPanelW - 1, INPUT_HEIGHT - 1), SX(6)));
                    }
                };
                
                int btnW = INPUT_HEIGHT;  // 加减按钮宽度 = 输入框高度（正方形）
                
                // [减号按钮] 点击减少数字
                Button btnMinus = new Button
                {
                    Text = "−",
                    Size = new Size(btnW, INPUT_HEIGHT - 2),
                    Location = new Point(0, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = LightBtnBg,
                    ForeColor = DarkText,
                    Font = NUMPAD_BTN_FONT,  // 14pt加粗字体
                    Cursor = Cursors.Hand
                };
                btnMinus.FlatAppearance.BorderSize = 0;
                btnMinus.FlatAppearance.MouseOverBackColor = NumPadHover;   // 悬停时背景色变深
                btnMinus.FlatAppearance.MouseDownBackColor = NumPadDown;   // 按下时背景色更深
                
                // [数字输入框] 显示和输入当前数值
                TextBox tb = new TextBox
                {
                    Text = initialVal.ToString(),
                    Location = new Point(btnW + 2, (INPUT_HEIGHT - 24) / 2),  // 垂直居中
                    Width = numPanelW - btnW * 2 - 6,                         // 宽度 = 面板宽度 - 两个按钮宽度
                    Height = 24,
                    BorderStyle = BorderStyle.None,
                    Font = NUM_INPUT_FONT,     // 12pt字体，居中显示
                    ForeColor = DarkText,
                    BackColor = InputBg,
                    TextAlign = HorizontalAlignment.Center
                };
                // 设置右键菜单（剪切/复制/粘贴/全选/清空）
                tb.ContextMenuStrip = CreateInputContextMenu(tb);
                
                // [加号按钮] 点击增加数字
                Button btnPlus = new Button
                {
                    Text = "+",
                    Size = new Size(btnW, INPUT_HEIGHT - 2),
                    Location = new Point(numPanelW - btnW - SX(1), 0),  // 右侧对齐
                    FlatStyle = FlatStyle.Flat,
                    BackColor = LightBtnBg,
                    ForeColor = DarkText,
                    Font = NUMPAD_BTN_FONT,  // 14pt加粗字体
                    Cursor = Cursors.Hand
                };
                btnPlus.FlatAppearance.BorderSize = 0;
                btnPlus.FlatAppearance.MouseOverBackColor = NumPadHover;   // 悬停时背景色变深
                btnPlus.FlatAppearance.MouseDownBackColor = NumPadDown;   // 按下时背景色更深

                // 减号按钮点击事件：数字减1（最小值为0）
                btnMinus.Click += (s, e) =>
                {
                    long v;
                    if (long.TryParse(tb.Text, out v) && v > 0) { v--; tb.Text = v.ToString(); }
                    else { tb.Text = "0"; }
                };
                
                // 加号按钮点击事件：数字加1（最大值为9999999999）
                btnPlus.Click += (s, e) =>
                {
                    long v;
                    if (long.TryParse(tb.Text, out v) && v < 9999999999L) { v++; tb.Text = v.ToString(); }
                    else if (!long.TryParse(tb.Text, out v)) { tb.Text = "0"; }
                };
                
                // 输入框按键事件：只允许输入数字
                tb.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                        e.Handled = true;
                };

                p.Controls.Add(btnMinus);
                p.Controls.Add(tb);
                p.Controls.Add(btnPlus);
                outTextBox = tb;
                return p;
            }

            // ------ 数字面板 ------
            int step3PanelTop = step3Top + SY(28) + CONTROL_GAP;  // 标签高度28px + 控件间距12px
            
            Panel pFrom = CreateNumPanel(pFromX, fromVal, out txtFrom);
            pFrom.Location = new Point(pFromX, step3PanelTop);
            step3Panel.Controls.Add(pFrom);
            
            Panel pTo = CreateNumPanel(pToX, toVal, out txtTo);
            pTo.Location = new Point(pToX, step3PanelTop);
            step3Panel.Controls.Add(pTo);

            // ------ 范围提示栏 ------
            int step3HintTop = step3PanelTop + INPUT_HEIGHT + SY(24);  // 面板高度44px + 控件间距24px，增大间距
            
            Panel pnlRangeHint = new Panel
            {
                Location = new Point(CONTENT_PAD, step3HintTop),
                Size = new Size(DLG_W - CONTENT_PAD * 2, SY(72)),  // 提示栏高度72px
                BackColor = theme.TipBg,
                BorderStyle = BorderStyle.None
            };
            pnlRangeHint.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, pnlRangeHint.Width, pnlRangeHint.Height), SX(6)));
            Label lblStep3RangeHint = new Label
            {
                Text = "⚠ 最大生成范围为10000，范围过大可能导致检测时间过长",
                Font = GetFont(9.5f),      // 提示字体9.5pt
                ForeColor = theme.WarnColor,
                Location = new Point(SX(16), SY(10)),  // 提示栏内边距
                AutoSize = false,
                Size = new Size(DLG_W - CONTENT_PAD * 2 - SX(32), SY(52)),
                BackColor = theme.TipBg,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRangeHint.Controls.Add(lblStep3RangeHint);
            pnlRangeHint.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(theme.WarnColor, 1.5f))
                {
                    pe.Graphics.DrawPath(p, CreateRoundedRectPath(new Rectangle(0, 0, pnlRangeHint.Width - 1, pnlRangeHint.Height - 1), SX(6)));
                }
            };
            step3Panel.Controls.Add(pnlRangeHint);

            Label lblStep3Preview = new Label
            {
                Text = "",
                Font = URL_FONT,
                ForeColor = GreenMain,
                Location = new Point(CONTENT_PAD, SY(176)),
                AutoSize = false,
                Size = new Size(DLG_W - CONTENT_PAD * 2, 60),
                BackColor = theme.StatusTagBg,
                Visible = false,
                Padding = new Padding(SX(10), SY(8), SX(10), SY(8))
            };
            step3Panel.Controls.Add(lblStep3Preview);

            pnlTextOptions = new Panel
            {
                Location = new Point(CONTENT_PAD, 22),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 210),
                BackColor = Color.Transparent,
                Visible = false
            };
            Label lblTextOptTitle = new Label
            {
                Text = "选择要替换的选项：",
                Font = GetFont(11f),
                ForeColor = DarkText,
                Location = new Point(0, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlTextOptions.Controls.Add(lblTextOptTitle);
            clstTextCandidates = new CheckedListBox
            {
                Location = new Point(0, SY(30)),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 140),
                Font = GetFont(10f),
                BackColor = InputBg,
                ForeColor = DarkText,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };
            pnlTextOptions.Controls.Add(clstTextCandidates);
            FlowLayoutPanel pnlTextBtns = new FlowLayoutPanel
            {
                Location = new Point(0, SY(176)),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 32),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            Button btnTextCheckAll = new Button
            {
                Text = "全选",
                Size = new Size(SX(70), SY(30)),
                FlatStyle = FlatStyle.Flat,
                BackColor = LightBtnBg,
                ForeColor = DarkText,
                Font = GetFont(9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnTextCheckAll.FlatAppearance.BorderSize = 1;
            btnTextCheckAll.FlatAppearance.BorderColor = GrayBorder;
            btnTextCheckAll.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(235, 236, 240);
            btnTextCheckAll.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(225, 226, 232);
            btnTextCheckAll.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 70, 30), 6));
            btnTextCheckAll.Click += (s, e) =>
            {
                for (int i = 0; i < clstTextCandidates.Items.Count; i++)
                    clstTextCandidates.SetItemChecked(i, true);
            };
            Button btnTextUncheckAll = new Button
            {
                Text = "全不选",
                Size = new Size(SX(70), SY(30)),
                FlatStyle = FlatStyle.Flat,
                BackColor = LightBtnBg,
                ForeColor = DarkText,
                Font = GetFont(9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnTextUncheckAll.FlatAppearance.BorderSize = 1;
            btnTextUncheckAll.FlatAppearance.BorderColor = GrayBorder;
            btnTextUncheckAll.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(235, 236, 240);
            btnTextUncheckAll.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(225, 226, 232);
            btnTextUncheckAll.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 70, 30), 6));
            btnTextUncheckAll.Click += (s, e) =>
            {
                for (int i = 0; i < clstTextCandidates.Items.Count; i++)
                    clstTextCandidates.SetItemChecked(i, false);
            };
            pnlTextBtns.Controls.Add(btnTextCheckAll);
            pnlTextBtns.Controls.Add(btnTextUncheckAll);
            pnlTextOptions.Controls.Add(pnlTextBtns);
            step3Panel.Controls.Add(pnlTextOptions);

            // ====== 底部按钮栏 ======
            // [高度] 68px，包含按钮(38px) + 上下间距各15px
            
            Panel sepBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = GrayLine };

            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = SY(68),
                BackColor = PanelBg,
                Padding = new Padding(CONTENT_PAD, 0, CONTENT_PAD, 0)
            };

            int btnBottomY = (bottomBar.Height - BTN_HEIGHT) / 2;  // 垂直居中
            
            Button btnPrev = new Button
            {
                Text = "← 上一步 (B)",
                Size = new Size(SX(130), BTN_HEIGHT),
                Location = new Point(CONTENT_PAD, btnBottomY),
                Visible = false
            };
            StyleGreenButton(btnPrev);
            bottomBar.Controls.Add(btnPrev);

            Button btnAction = new Button
            {
                Text = "下一步 (N) →",
                Size = new Size(SX(130), BTN_HEIGHT),
                Location = new Point(DLG_W - CONTENT_PAD - SX(130), btnBottomY)
            };
            StyleGreenButton(btnAction);
            bottomBar.Controls.Add(btnAction);

            void UpdateStepUI()
            {
                step1Panel.Dock = DockStyle.None; step1Panel.Visible = false;
                step2Panel.Dock = DockStyle.None; step2Panel.Visible = false;
                step3Panel.Dock = DockStyle.None; step3Panel.Visible = false;

                Panel active;
                if (currentStep == 1) active = step1Panel;
                else if (currentStep == 2) active = step2Panel;
                else active = step3Panel;

                active.Dock = DockStyle.Fill;
                active.Visible = true;
                active.BringToFront();

                stepIndicator.Invalidate();
                btnPrev.Visible = currentStep > 1;
                if (isCustomRangeMode)
                    btnAction.Text = "开始生成";
                else
                    btnAction.Text = (currentStep == 3) ? "开始生成" : "下一步 (N) →";
            }

            void BuildUrlPreview(string url, int preSelectSeg = -1, int preSubStart = 0, int preSubLen = 0, int preGlobalPos = -1, int preGlobalLen = 0)
            {
                url = url.Trim().Trim('"', ' ', '`', '\t');
                int qPos = url.IndexOf('?');
                if (qPos >= 0) url = url.Substring(0, qPos);
                segBaseUrl = url;
                segListContainer.Controls.Clear();
                selectedSegIndex = -1;
                multiResEnabled = false;
                selectedResSegIndex = -1;

                int pathStart = url.IndexOf("://");
                if (pathStart >= 0)
                    pathStart = url.IndexOf('/', pathStart + 3);
                if (pathStart < 0) pathStart = 0;
                string pathPart = pathStart > 0 ? url.Substring(pathStart) : url;
                string prefixPart = pathStart > 0 ? url.Substring(0, pathStart) : "";

                var payDict = new Dictionary<string, string>();
                foreach (var kv in PayChannelList) payDict[kv.Key] = kv.Value;
                var wsDict = new Dictionary<string, string>();
                foreach (var kv in WsChannelList) wsDict[kv.Key] = kv.Value;
                var movieDict = new Dictionary<string, string>();
                foreach (var kv in MovieChannelList) movieDict[kv.Key] = kv.Value;
                var cctvCandidates = CctvChannelMap["cctv"];
                var cctvSet = new HashSet<string>(cctvCandidates);
                var payKeys = new List<string>(payDict.Keys);
                var wsKeys = new List<string>(wsDict.Keys);
                var movieKeys = new List<string>(movieDict.Keys);

                segs = new List<ScanSegInfo>();

                Regex numRegex = new Regex(@"[/_-](\d+)(?=[/._?-]|$)");
                foreach (Match m in numRegex.Matches(pathPart))
                {
                    string num = m.Groups[1].Value;
                    segs.Add(new ScanSegInfo
                    {
                        Type = ScanSegType.Number,
                        PathStart = m.Groups[1].Index,
                        GlobalStart = pathStart + m.Groups[1].Index,
                        GlobalEnd = pathStart + m.Groups[1].Index + m.Groups[1].Length,
                        OriginalText = num,
                        Label = "🔢 数字段: " + num,
                        Candidates = null
                    });
                }

                Regex resRegex = new Regex(@"[/_-]((?:2160|1080|720|540|480|360)p|4k|2k|hd|sd)(?=[/._?-]|$)", RegexOptions.IgnoreCase);
                foreach (Match m in resRegex.Matches(pathPart))
                {
                    string res = m.Groups[1].Value.ToLower();
                    segs.Add(new ScanSegInfo
                    {
                        Type = ScanSegType.Resolution,
                        PathStart = m.Groups[1].Index,
                        GlobalStart = pathStart + m.Groups[1].Index,
                        GlobalEnd = pathStart + m.Groups[1].Index + m.Groups[1].Length,
                        OriginalText = res,
                        Label = "📐 分辨率: " + res,
                        Candidates = new List<string>(ResolutionList)
                    });
                }

                var coveredRanges = new List<Tuple<int, int>>();
                foreach (var seg in segs)
                    coveredRanges.Add(Tuple.Create(seg.PathStart, seg.PathStart + seg.OriginalText.Length));

                int fileExtPos = -1;
                var extMatch = Regex.Match(pathPart, @"\.(m3u8|flv|ts|mp4)(?=[/._?-]|$)", RegexOptions.IgnoreCase);
                if (extMatch.Success) fileExtPos = extMatch.Index;

                Regex tokenRegex = new Regex(@"[a-z0-9]+", RegexOptions.IgnoreCase);
                foreach (Match tm in tokenRegex.Matches(pathPart))
                {
                    int tokStart = tm.Index;
                    int tokLen = tm.Length;
                    string tok = tm.Value.ToLower();

                    bool overlaps = false;
                    foreach (var range in coveredRanges)
                    {
                        if (tokStart < range.Item2 && tokStart + tokLen > range.Item1)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (overlaps) continue;

                    if (fileExtPos >= 0 && tokStart >= fileExtPos) continue;
                    if (tok.Length < 2) continue;
                    if (Regex.IsMatch(tok, @"^\d+$")) continue;

                    ScanSegType segType = ScanSegType.Number;
                    string segLabel = null;
                    List<string> segCandidates = null;
                    bool found = false;

                    if (Regex.IsMatch(tok, @"^cctv\d+[a-z0-9]*$") && cctvSet.Contains(tok))
                    {
                        segType = ScanSegType.CctvChannel;
                        segLabel = "📺 CCTV频道: " + tok;
                        segCandidates = new List<string>(cctvCandidates);
                        found = true;
                    }
                    else if (payDict.ContainsKey(tok))
                    {
                        segType = ScanSegType.PayChannel;
                        segLabel = "📺 付费频道: " + tok;
                        segCandidates = payKeys;
                        found = true;
                    }
                    else if (wsDict.ContainsKey(tok))
                    {
                        segType = ScanSegType.WsChannel;
                        segLabel = "📡 卫视频道: " + tok;
                        segCandidates = wsKeys;
                        found = true;
                    }
                    else if (movieDict.ContainsKey(tok))
                    {
                        segType = ScanSegType.MovieChannel;
                        segLabel = "🎬 影视频道: " + tok;
                        segCandidates = movieKeys;
                        found = true;
                    }
                    else if (fileExtPos >= 0 && tokStart + tokLen == fileExtPos &&
                             Regex.IsMatch(tok, @"^[a-z]{2,}\d*[a-z]*$"))
                    {
                        if (cctvSet.Contains(tok))
                        {
                            segType = ScanSegType.CctvChannel;
                            segLabel = "📺 CCTV频道: " + tok;
                            segCandidates = new List<string>(cctvCandidates);
                            found = true;
                        }
                        else if (payDict.ContainsKey(tok))
                        {
                            segType = ScanSegType.PayChannel;
                            segLabel = "📺 付费频道: " + tok;
                            segCandidates = payKeys;
                            found = true;
                        }
                        else if (wsDict.ContainsKey(tok))
                        {
                            segType = ScanSegType.WsChannel;
                            segLabel = "📡 卫视频道: " + tok;
                            segCandidates = wsKeys;
                            found = true;
                        }
                        else if (movieDict.ContainsKey(tok))
                        {
                            segType = ScanSegType.MovieChannel;
                            segLabel = "🎬 影视频道: " + tok;
                            segCandidates = movieKeys;
                            found = true;
                        }
                    }

                    if (found)
                    {
                        segs.Add(new ScanSegInfo
                        {
                            Type = segType,
                            PathStart = tokStart,
                            GlobalStart = pathStart + tokStart,
                            GlobalEnd = pathStart + tokStart + tokLen,
                            OriginalText = tok,
                            Label = segLabel,
                            Candidates = segCandidates
                        });
                    }
                }

                segs.Sort((a, b) => a.PathStart.CompareTo(b.PathStart));

                bool hasResolution = false;
                int resSegIdx = -1;
                for (int i = 0; i < segs.Count; i++)
                {
                    if (segs[i].Type == ScanSegType.Resolution)
                    {
                        hasResolution = true;
                        resSegIdx = i;
                        break;
                    }
                }

                if (segs.Count == 0)
                {
                    Label noMatch = new Label
                    {
                        Text = "❌ 未找到可生成的字段，请检查URL格式（支持数字段如/123/、频道名如cctv1、分辨率如1080p等）",
                        ForeColor = RedHighlight,
                        Font = GetFont(10.5f),
                        Location = new Point(0, SY(8)),
                        AutoSize = true,
                        BackColor = Color.Transparent
                    };
                    segListContainer.Controls.Add(noMatch);
                    return;
                }

                subSegStart = new int[segs.Count];
                subSegLen = new int[segs.Count];
                for (int i = 0; i < segs.Count; i++)
                {
                    if (segs[i].Type == ScanSegType.Number)
                    {
                        subSegStart[i] = 0;
                        subSegLen[i] = segs[i].OriginalText.Length;
                    }
                    else
                    {
                        subSegStart[i] = 0;
                        subSegLen[i] = 0;
                    }
                }

                int itemY = 8;
                int itemH = SY(44);  // 增大行高以容纳所有控件
                int radioSize = 18;
                Color rowBgNormal = PanelBg;
                Color radioBorderColor = GrayBorder;
                Color bracketColor = isDark ? theme.TextSecondary : Color.FromArgb(150, 153, 160);
                Color bracketActiveColor = GreenMain;
                Color subSelBg = isDark ? Color.FromArgb(30, 90, 50) : Color.FromArgb(195, 240, 205);
                Color dimFg = isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(120, 125, 135);

                Panel[] radioCircles = new Panel[segs.Count];
                Panel[] rowPanels = new Panel[segs.Count];
                Label[] segPrefixLbl = new Label[segs.Count];
                Label[] segBracketL = new Label[segs.Count];
                Label[] segSelTextLbl = new Label[segs.Count];
                Label[] segBracketR = new Label[segs.Count];
                Label[] segSuffixLbl = new Label[segs.Count];
                Label[] segTypeTagLbl = new Label[segs.Count];
                bool[] segSelected = new bool[segs.Count];

                Panel adjPanel = null;
                Button btnLeftShrink = null, btnLeftExpand = null, btnRightShrink = null, btnRightExpand = null, btnSelectAll = null;
                Label lblSelInfo = null;

                void UpdateSegDisplay(int segIdx)
                {
                    if (segIdx < 0 || segIdx >= segs.Count) return;
                    var seg = segs[segIdx];
                    bool isSel = segIdx == selectedSegIndex;

                    Label bl = segBracketL[segIdx];
                    Label preL = segPrefixLbl[segIdx];
                    Label selL = segSelTextLbl[segIdx];
                    Label sufL = segSuffixLbl[segIdx];
                    Label br = segBracketR[segIdx];
                    Panel rad = radioCircles[segIdx];
                    Label tagL = segTypeTagLbl[segIdx];

                    if (seg.Type == ScanSegType.Number)
                    {
                        int ss = subSegStart[segIdx];
                        int sl = subSegLen[segIdx];
                        string num = seg.OriginalText;
                        int numLen = num.Length;

                        if (isSel)
                        {
                            if (bl != null) { bl.Visible = true; bl.ForeColor = bracketActiveColor; bl.Font = URL_BOLD_FONT; }
                            if (br != null) { br.Visible = true; br.ForeColor = bracketActiveColor; br.Font = URL_BOLD_FONT; }
                            string prefix = (ss > 0) ? num.Substring(0, ss) : "";
                            string selDigits = num.Substring(ss, sl);
                            string suffix = (ss + sl < numLen) ? num.Substring(ss + sl) : "";
                            if (preL != null) { preL.Text = prefix; preL.Visible = prefix.Length > 0; preL.ForeColor = dimFg; preL.Font = URL_FONT; }
                            if (selL != null) { selL.Text = selDigits; selL.Visible = true; selL.ForeColor = bracketActiveColor; selL.Font = URL_BOLD_FONT; selL.BackColor = subSelBg; }
                            if (sufL != null) { sufL.Text = suffix; sufL.Visible = suffix.Length > 0; sufL.ForeColor = dimFg; sufL.Font = URL_FONT; }
                        }
                        else
                        {
                            if (bl != null) { bl.Visible = true; bl.ForeColor = bracketColor; bl.Font = URL_FONT; }
                            if (br != null) { br.Visible = true; br.ForeColor = bracketColor; br.Font = URL_FONT; }
                            if (preL != null) preL.Visible = false;
                            if (selL != null) { selL.Text = num; selL.Visible = true; selL.ForeColor = DarkText; selL.Font = URL_FONT; selL.BackColor = Color.Transparent; }
                            if (sufL != null) sufL.Visible = false;
                        }
                    }
                    else
                    {
                        if (bl != null) bl.Visible = false;
                        if (br != null) br.Visible = false;
                        if (preL != null) preL.Visible = false;
                        if (sufL != null) sufL.Visible = false;
                        if (selL != null)
                        {
                            selL.Text = seg.OriginalText;
                            selL.Visible = true;
                            if (isSel)
                            {
                                selL.ForeColor = bracketActiveColor;
                                selL.Font = URL_BOLD_FONT;
                                selL.BackColor = subSelBg;
                            }
                            else
                            {
                                selL.ForeColor = DarkText;
                                selL.Font = URL_FONT;
                                selL.BackColor = Color.Transparent;
                            }
                        }
                    }

                    if (tagL != null)
                    {
                        tagL.ForeColor = isSel ? bracketActiveColor : GrayText;
                        tagL.Font = isSel ? URL_BOLD_FONT : URL_FONT;
                    }

                    if (rad != null) rad.Invalidate();
                }

                void UpdateAdjButtons()
                {
                    if (selectedSegIndex < 0 || adjPanel == null)
                    {
                        if (adjPanel != null) adjPanel.Visible = false;
                        return;
                    }
                    var curSeg = segs[selectedSegIndex];
                    if (curSeg.Type != ScanSegType.Number)
                    {
                        adjPanel.Visible = false;
                        return;
                    }
                    adjPanel.Visible = true;
                    int numLen = curSeg.OriginalText.Length;
                    int ss = subSegStart[selectedSegIndex];
                    int sl = subSegLen[selectedSegIndex];
                    btnLeftShrink.Enabled = sl > 1;
                    btnLeftExpand.Enabled = ss > 0;
                    btnRightShrink.Enabled = sl > 1;
                    btnRightExpand.Enabled = ss + sl < numLen;
                    string selDigits = curSeg.OriginalText.Substring(ss, sl);
                    string fullNum = curSeg.OriginalText;
                    if (ss == 0 && sl == numLen)
                        lblSelInfo.Text = string.Format("已选中整段 {{{0}}}（{1}位），若需框选部分位数请用按钮调整大括号", selDigits, numLen);
                    else
                        lblSelInfo.Text = string.Format("已框选：{0}{{{1}}}{2}", fullNum.Substring(0, ss), selDigits, (ss + sl < numLen ? fullNum.Substring(ss + sl) : ""));
                }

                void SelectSegment(int segIdx)
                {
                    for (int k = 0; k < segs.Count; k++)
                    {
                        segSelected[k] = false;
                        if (segs[k].Type == ScanSegType.Number)
                        {
                            subSegStart[k] = 0;
                            subSegLen[k] = segs[k].OriginalText.Length;
                        }
                        UpdateSegDisplay(k);
                    }
                    segSelected[segIdx] = true;
                    selectedSegIndex = segIdx;
                    if (segs[segIdx].Type == ScanSegType.Number)
                    {
                        subSegStart[segIdx] = 0;
                        subSegLen[segIdx] = segs[segIdx].OriginalText.Length;
                    }
                    UpdateSegDisplay(segIdx);
                    UpdateAdjButtons();
                }

                for (int i = 0; i < segs.Count; i++)
                {
                    var seg = segs[i];
                    int segStartInPath = seg.PathStart;
                    int segLen = seg.OriginalText.Length;

                    string beforeText = pathPart.Substring(0, segStartInPath);
                    string afterText = pathPart.Substring(segStartInPath + segLen);
                    string fullBefore = prefixPart + beforeText;

                    Panel rowPanel = new Panel
                    {
                        Location = new Point(0, itemY),
                        Width = segListContainer.Width - 20,
                        Height = itemH,
                        BackColor = rowBgNormal,
                        Cursor = Cursors.Hand
                    };

                    int idxI = i;

                    FlowLayoutPanel rowFlow = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.Transparent,
                        WrapContents = false,
                        Margin = new Padding(4),
                        Padding = new Padding(4, 0, 4, 0),
                        FlowDirection = FlowDirection.LeftToRight
                    };

                    Label lblBefore = new Label
                    {
                        Text = fullBefore,
                        Font = URL_FONT,
                        ForeColor = DarkText,
                        AutoSize = true,
                        BackColor = Color.Transparent,
                        Cursor = Cursors.Hand,
                        Tag = i,
                        Margin = new Padding(0)
                    };
                    rowFlow.Controls.Add(lblBefore);

                    Panel radioCircle = new Panel
                    {
                        Size = new Size(radioSize, radioSize),
                        BackColor = Color.Transparent,
                        Tag = i,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(4, 0, 4, 0)
                    };
                    radioCircle.Paint += (s, pe) =>
                    {
                        Graphics g = pe.Graphics;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        bool isSel = segSelected[idxI];
                        Rectangle rect = new Rectangle(0, 0, radioSize - 1, radioSize - 1);
                        if (isSel)
                        {
                            using (SolidBrush b = new SolidBrush(bracketActiveColor))
                                g.FillEllipse(b, rect);
                            using (Pen p = new Pen(bracketActiveColor))
                                g.DrawEllipse(p, rect);
                            int innerD = 6;
                            int innerX = (radioSize - innerD) / 2;
                            using (SolidBrush wb = new SolidBrush(PanelBg))
                                g.FillEllipse(wb, new Rectangle(innerX, innerX, innerD, innerD));
                        }
                        else
                        {
                            using (Pen p = new Pen(radioBorderColor, 1.5f))
                                g.DrawEllipse(p, rect);
                        }
                    };
                    rowFlow.Controls.Add(radioCircle);
                    radioCircles[i] = radioCircle;

                    if (seg.Type == ScanSegType.Number)
                    {
                        Label lblPre = new Label
                        {
                            Text = "",
                            Font = URL_FONT,
                            ForeColor = dimFg,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Visible = false,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblPre);
                        segPrefixLbl[i] = lblPre;

                        Label lblBL = new Label
                        {
                            Text = "{",
                            Font = URL_FONT,
                            ForeColor = bracketColor,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Cursor = Cursors.Hand,
                            Tag = i,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblBL);
                        segBracketL[i] = lblBL;

                        Label lblNum = new Label
                        {
                            Text = seg.OriginalText,
                            Font = URL_FONT,
                            ForeColor = DarkText,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Cursor = Cursors.Hand,
                            Tag = i,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblNum);
                        segSelTextLbl[i] = lblNum;

                        Label lblBR = new Label
                        {
                            Text = "}",
                            Font = URL_FONT,
                            ForeColor = bracketColor,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Cursor = Cursors.Hand,
                            Tag = i,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblBR);
                        segBracketR[i] = lblBR;

                        Label lblSuf = new Label
                        {
                            Text = "",
                            Font = URL_FONT,
                            ForeColor = dimFg,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Visible = false,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblSuf);
                        segSuffixLbl[i] = lblSuf;
                    }
                    else
                    {
                        segPrefixLbl[i] = null;
                        segBracketL[i] = null;
                        segBracketR[i] = null;
                        segSuffixLbl[i] = null;

                        Label lblText = new Label
                        {
                            Text = seg.OriginalText,
                            Font = URL_FONT,
                            ForeColor = DarkText,
                            AutoSize = true,
                            BackColor = Color.Transparent,
                            Cursor = Cursors.Hand,
                            Tag = i,
                            Margin = new Padding(0)
                        };
                        rowFlow.Controls.Add(lblText);
                        segSelTextLbl[i] = lblText;
                    }

                    Label lblAfter = new Label
                    {
                        Text = afterText,
                        Font = URL_FONT,
                        ForeColor = DarkText,
                        AutoSize = true,
                        BackColor = Color.Transparent,
                        Margin = new Padding(0)
                    };
                    rowFlow.Controls.Add(lblAfter);

                    EventHandler rowClickHandler = (s, e) => { SelectSegment(idxI); };
                    lblBefore.Click += rowClickHandler;
                    lblAfter.Click += rowClickHandler;
                    if (segSelTextLbl[i] != null) segSelTextLbl[i].Click += rowClickHandler;
                    if (segBracketL[i] != null) segBracketL[i].Click += rowClickHandler;
                    if (segBracketR[i] != null) segBracketR[i].Click += rowClickHandler;
                    radioCircle.Click += rowClickHandler;
                    rowPanel.Click += rowClickHandler;
                    rowFlow.Click += rowClickHandler;

                    rowPanel.Controls.Add(rowFlow);
                    rowPanels[i] = rowPanel;
                    segListContainer.Controls.Add(rowPanel);
                    itemY += itemH + CONTROL_GAP;  // 使用标准控件间距
                }

                adjPanel = new Panel
                {
                    Location = new Point(0, itemY),
                    Width = segListContainer.Width - 20,
                    Height = SY(50),
                    BackColor = Color.Transparent,
                    Visible = false
                };

                int btnH = SY(34);
                int btnY = (SY(50) - btnH) / 2;
                btnLeftExpand = new Button { Text = "◀ {", Size = new Size(SX(54), btnH), Location = new Point(0, btnY), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = GetFont(9f), Cursor = Cursors.Hand };
                btnLeftShrink = new Button { Text = "{ ▶", Size = new Size(SX(54), btnH), Location = new Point(SX(58), btnY), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = GetFont(9f), Cursor = Cursors.Hand };
                btnRightShrink = new Button { Text = "} ◀", Size = new Size(SX(54), btnH), Location = new Point(SX(116), btnY), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = GetFont(9f), Cursor = Cursors.Hand };
                btnRightExpand = new Button { Text = "} ▶", Size = new Size(SX(54), btnH), Location = new Point(SX(174), btnY), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = GetFont(9f), Cursor = Cursors.Hand };
                btnSelectAll = new Button { Text = "全选本段", Size = new Size(SX(80), btnH), Location = new Point(SX(236), btnY), FlatStyle = FlatStyle.Flat, BackColor = bracketActiveColor, ForeColor = Color.White, Font = GetFont(9f), Cursor = Cursors.Hand };

                void StyleBtn(Button b)
                {
                    b.FlatAppearance.BorderSize = 1;
                    b.FlatAppearance.BorderColor = GrayBorder;
                    b.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(235, 236, 240);
                    b.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(220, 222, 230);
                }
                StyleBtn(btnLeftShrink); StyleBtn(btnLeftExpand); StyleBtn(btnRightShrink); StyleBtn(btnRightExpand);
                btnSelectAll.FlatAppearance.BorderSize = 0;
                btnSelectAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 140, 66);
                btnSelectAll.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 120, 56);

                btnLeftShrink.Click += (s, e) =>
                {
                    if (selectedSegIndex < 0) return;
                    if (segs[selectedSegIndex].Type != ScanSegType.Number) return;
                    if (subSegLen[selectedSegIndex] > 1)
                    {
                        subSegStart[selectedSegIndex]++;
                        subSegLen[selectedSegIndex]--;
                        UpdateSegDisplay(selectedSegIndex);
                        UpdateAdjButtons();
                    }
                };
                btnLeftExpand.Click += (s, e) =>
                {
                    if (selectedSegIndex < 0) return;
                    if (segs[selectedSegIndex].Type != ScanSegType.Number) return;
                    if (subSegStart[selectedSegIndex] > 0)
                    {
                        subSegStart[selectedSegIndex]--;
                        subSegLen[selectedSegIndex]++;
                        UpdateSegDisplay(selectedSegIndex);
                        UpdateAdjButtons();
                    }
                };
                btnRightShrink.Click += (s, e) =>
                {
                    if (selectedSegIndex < 0) return;
                    if (segs[selectedSegIndex].Type != ScanSegType.Number) return;
                    if (subSegLen[selectedSegIndex] > 1)
                    {
                        subSegLen[selectedSegIndex]--;
                        UpdateSegDisplay(selectedSegIndex);
                        UpdateAdjButtons();
                    }
                };
                btnRightExpand.Click += (s, e) =>
                {
                    if (selectedSegIndex < 0) return;
                    if (segs[selectedSegIndex].Type != ScanSegType.Number) return;
                    int numLen = segs[selectedSegIndex].OriginalText.Length;
                    if (subSegStart[selectedSegIndex] + subSegLen[selectedSegIndex] < numLen)
                    {
                        subSegLen[selectedSegIndex]++;
                        UpdateSegDisplay(selectedSegIndex);
                        UpdateAdjButtons();
                    }
                };
                btnSelectAll.Click += (s, e) =>
                {
                    if (selectedSegIndex < 0) return;
                    if (segs[selectedSegIndex].Type != ScanSegType.Number) return;
                    subSegStart[selectedSegIndex] = 0;
                    subSegLen[selectedSegIndex] = segs[selectedSegIndex].OriginalText.Length;
                    UpdateSegDisplay(selectedSegIndex);
                    UpdateAdjButtons();
                };

                adjPanel.Controls.Add(btnLeftShrink);
                adjPanel.Controls.Add(btnLeftExpand);
                adjPanel.Controls.Add(btnRightShrink);
                adjPanel.Controls.Add(btnRightExpand);
                adjPanel.Controls.Add(btnSelectAll);

                lblSelInfo = new Label
                {
                    Text = "",
                    Font = GetFont(8.5f),
                    ForeColor = theme.TextSecondary,
                    AutoSize = false,
                    Location = new Point(SX(320), (SY(50) - SY(22)) / 2),
                    Size = new Size(adjPanel.Width - SX(320), SY(22)),
                    BackColor = Color.Transparent
                };
                adjPanel.Controls.Add(lblSelInfo);

                segListContainer.Controls.Add(adjPanel);
                itemY = adjPanel.Bottom;

                Label lblHint = new Label
                {
                    Text = "💡 点击单选按钮选择要生成的字段，绿色●为当前选中。数字段可用 ◀{ {▶ }◀ }▶ 按钮调整大括号框选部分位数（长数字可选子范围），含前导零将保持补零。频道/分辨率段将提供候选列表供选择。",
                    Font = GetFont(8.5f),
                    ForeColor = theme.SuccessColor,
                    Location = new Point(0, itemY + SY(20)),
                    AutoSize = false,
                    Size = new Size(segListContainer.Width - 20, SY(56)),
                    BackColor = theme.StatusTagBg,
                    Padding = new Padding(SX(10), SY(8), SX(10), SY(8)),
                    TextAlign = ContentAlignment.TopLeft
                };
                segListContainer.Controls.Add(lblHint);
                itemY = lblHint.Bottom + CONTROL_GAP;  // 使用标准控件间距

                CheckBox chkMultiRes = new CheckBox
                {
                    Text = "📐 同时生成多个分辨率（1080p/720p/540p/480p/360p）",
                    Font = GetFont(9.5f),
                    ForeColor = hasResolution ? DarkText : GrayText,
                    Location = new Point(0, itemY),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Enabled = hasResolution,
                    Checked = false,
                    Cursor = hasResolution ? Cursors.Hand : Cursors.Default
                };
                chkMultiRes.CheckedChanged += (s, e) =>
                {
                    multiResEnabled = chkMultiRes.Checked;
                    selectedResSegIndex = multiResEnabled ? resSegIdx : -1;
                };
                segListContainer.Controls.Add(chkMultiRes);

                if (segs.Count > 0)
                {
                    int initSeg = 0;
                    if (preGlobalPos >= 0 && preGlobalLen > 0)
                    {
                        for (int si = 0; si < segs.Count; si++)
                        {
                            if (segs[si].Type == ScanSegType.Number &&
                                preGlobalPos >= segs[si].GlobalStart &&
                                preGlobalPos + preGlobalLen <= segs[si].GlobalEnd)
                            {
                                initSeg = si;
                                break;
                            }
                        }
                    }
                    else if (preSelectSeg >= 0 && preSelectSeg < segs.Count)
                    {
                        initSeg = preSelectSeg;
                    }
                    SelectSegment(initSeg);
                    if (preGlobalPos >= 0 && preGlobalLen > 0 && segs[initSeg].Type == ScanSegType.Number &&
                        preGlobalPos >= segs[initSeg].GlobalStart &&
                        preGlobalPos + preGlobalLen <= segs[initSeg].GlobalEnd)
                    {
                        int ss = preGlobalPos - segs[initSeg].GlobalStart;
                        int sl = preGlobalLen;
                        int numLen = segs[initSeg].OriginalText.Length;
                        if (ss >= 0 && ss + sl <= numLen)
                        {
                            subSegStart[initSeg] = ss;
                            subSegLen[initSeg] = sl;
                            UpdateSegDisplay(initSeg);
                            UpdateAdjButtons();
                        }
                    }
                    else if (preSelectSeg >= 0 && preSelectSeg < segs.Count && preSubLen > 0 && segs[preSelectSeg].Type == ScanSegType.Number)
                    {
                        int numLen = segs[preSelectSeg].OriginalText.Length;
                        if (preSubStart >= 0 && preSubStart + preSubLen <= numLen)
                        {
                            subSegStart[preSelectSeg] = preSubStart;
                            subSegLen[preSelectSeg] = preSubLen;
                            UpdateSegDisplay(preSelectSeg);
                            UpdateAdjButtons();
                        }
                    }
                }
            }

            bool ValidateCustomRangeUrl(string url, out string error, out long start, out long end, out int padW, out bool padZero, out int replacePos, out int replaceLen, out string template)
            {
                error = "";
                start = end = 0;
                padW = 0;
                padZero = false;
                replacePos = replaceLen = 0;
                template = "";
                var rangePattern1 = new Regex(@"\[(\d+)-(\d+)\]");
                var rangePattern2 = new Regex(@"\{(\d+)-(\d+)\}");
                Match m = null;
                var mc1 = rangePattern1.Matches(url);
                var mc2 = rangePattern2.Matches(url);
                int totalMatches = mc1.Count + mc2.Count;
                if (totalMatches == 0) return false;
                if (totalMatches > 1)
                {
                    error = "每次只能配置一个变量范围（仅允许一对[数字-数字]或{数字-数字}）";
                    return false;
                }
                if (mc1.Count == 1) m = mc1[0];
                else m = mc2[0];
                string startStr = m.Groups[1].Value;
                string endStr = m.Groups[2].Value;
                start = long.Parse(startStr);
                end = long.Parse(endStr);
                if (start >= end)
                {
                    error = "范围起始值必须小于结束值";
                    return false;
                }
                if (end - start > 10000)
                {
                    error = "生成范围过大，请控制在10000以内";
                    return false;
                }
                padW = startStr.Length;
                padZero = startStr.Length > 1 && startStr.StartsWith("0");
                replacePos = m.Index;
                replaceLen = m.Length;
                template = url;
                string testUrl = url.Substring(0, m.Index) + "12345" + url.Substring(m.Index + m.Length);
                if (!Uri.IsWellFormedUriString(testUrl, UriKind.Absolute))
                {
                    error = "URL格式不正确，请检查地址";
                    return false;
                }
                return true;
            }

            bool ParseManualBracketUrl(string url, out string error, out string cleanUrl, out long bracketNum, out int replacePos, out int replaceLen)
            {
                error = "";
                cleanUrl = "";
                bracketNum = 0;
                replacePos = replaceLen = 0;
                var bracketPattern = new Regex(@"\{(\d+)\}");
                var rangePattern = new Regex(@"\{(\d+)-(\d+)\}");
                var bracketMatches = bracketPattern.Matches(url);
                var rangeMatches = rangePattern.Matches(url);
                if (bracketMatches.Count == 0 && rangeMatches.Count == 0) return false;
                if (rangeMatches.Count > 0) return false;
                if (bracketMatches.Count > 1)
                {
                    error = "每次只能框选一个数字段（仅允许一对{数字}）";
                    return false;
                }
                var m = bracketMatches[0];
                bracketNum = long.Parse(m.Groups[1].Value);
                replacePos = m.Index;
                replaceLen = m.Groups[1].Length;
                cleanUrl = url.Substring(0, m.Index) + m.Groups[1].Value + url.Substring(m.Index + m.Length);
                if (!Uri.IsWellFormedUriString(cleanUrl, UriKind.Absolute))
                {
                    error = "URL格式不正确，请检查地址";
                    return false;
                }
                return true;
            }

            string PadNumber(long num, int width, bool padZero)
            {
                string s = num.ToString();
                if (padZero && s.Length < width)
                    return s.PadLeft(width, '0');
                return s;
            }

            List<ChannelInfo> generatedChannels = null;

            string GetChannelDisplayName(string key, ScanSegType type)
            {
                if (type == ScanSegType.CctvChannel)
                {
                    if (key == "cctv4k") return "CCTV-4K";
                    if (key == "cctv8k") return "CCTV-8K";
                    if (key == "cctv5p") return "CCTV-5+";
                    string numPart = key.Substring(4);
                    return "CCTV-" + numPart;
                }
                foreach (var kv in PayChannelList) if (kv.Key == key) return kv.Value;
                foreach (var kv in WsChannelList) if (kv.Key == key) return kv.Value;
                foreach (var kv in MovieChannelList) if (kv.Key == key) return kv.Value;
                return key;
            }

            void DoGenerate()
            {
                var channels = new List<ChannelInfo>();
                if (isCustomRangeMode)
                {
                    for (long v = customRangeStart; v <= customRangeEnd; v++)
                    {
                        string url = customUrlTemplate.Substring(0, customReplacePos) + PadNumber(v, customPadWidth, customPadZero) + customUrlTemplate.Substring(customReplacePos + customReplaceLen);
                        channels.Add(new ChannelInfo { Name = "源" + (channels.Count + 1), Url = url, Group = "生成器", Status = "未检测", Visible = true });
                    }
                }
                else
                {
                    var selSeg = segs[selectedSegIndex];
                    int pathStart = segBaseUrl.IndexOf("://");
                    if (pathStart >= 0) pathStart = segBaseUrl.IndexOf('/', pathStart + 3);
                    if (pathStart < 0) pathStart = 0;
                    string prefixPart = pathStart > 0 ? segBaseUrl.Substring(0, pathStart) : "";
                    string pathPart = pathStart > 0 ? segBaseUrl.Substring(pathStart) : segBaseUrl;

                    int primStart = selSeg.PathStart;
                    int primLen = selSeg.OriginalText.Length;

                    if (selSeg.Type == ScanSegType.Number)
                    {
                        string numStr = selSeg.OriginalText;
                        int subStart = (subSegStart != null && selectedSegIndex >= 0 && selectedSegIndex < subSegStart.Length) ? subSegStart[selectedSegIndex] : 0;
                        int subLen = (subSegLen != null && selectedSegIndex >= 0 && selectedSegIndex < subSegLen.Length) ? subSegLen[selectedSegIndex] : numStr.Length;
                        if (subStart < 0) subStart = 0;
                        if (subStart + subLen > numStr.Length) subLen = numStr.Length - subStart;
                        if (subLen <= 0) { subStart = 0; subLen = numStr.Length; }
                        primStart = selSeg.PathStart + subStart;
                        primLen = subLen;

                        for (long v = fromVal; v <= toVal; v++)
                        {
                            string replText = PadNumber(v, segPadWidth, segPadZero);
                            string newPath = pathPart.Substring(0, primStart) + replText + pathPart.Substring(primStart + primLen);
                            int deltaLen = replText.Length - primLen;
                            string baseName = "源" + (channels.Count + 1);
                            AddChannelWithResVariants(channels, prefixPart, newPath, selSeg, baseName, deltaLen);
                        }
                    }
                    else
                    {
                        var textValues = selectedTextValues ?? new List<string>();
                        foreach (string val in textValues)
                        {
                            string newPath = pathPart.Substring(0, primStart) + val + pathPart.Substring(primStart + primLen);
                            int deltaLen = val.Length - primLen;
                            string baseName = GetChannelDisplayName(val, selSeg.Type);
                            AddChannelWithResVariants(channels, prefixPart, newPath, selSeg, baseName, deltaLen);
                        }
                    }
                }

                if (channels.Count > 10000)
                {
                    DarkMessageBox.Show("生成的源数量超过10000，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                generatedChannels = channels;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            }

            void AddChannelWithResVariants(List<ChannelInfo> channels, string prefixPart, string baseNewPath, ScanSegInfo selSeg, string baseName, int deltaLen)
            {
                string baseUrl = prefixPart + baseNewPath;

                if (multiResEnabled && selectedResSegIndex >= 0 && selectedResSegIndex < segs.Count && selectedSegIndex != selectedResSegIndex)
                {
                    var resSeg = segs[selectedResSegIndex];
                    int resPathStart = resSeg.PathStart;
                    int resPathLen = resSeg.OriginalText.Length;

                    if (selectedSegIndex < selectedResSegIndex)
                    {
                        resPathStart += deltaLen;
                    }

                    foreach (string res in ResolutionList)
                    {
                        string resNewPath = baseNewPath.Substring(0, resPathStart) + res + baseNewPath.Substring(resPathStart + resPathLen);
                        string resUrl = prefixPart + resNewPath;
                        string resName = baseName + "-" + res;
                        channels.Add(new ChannelInfo { Name = resName, Url = resUrl, Group = "生成器", Status = "未检测", Visible = true });
                        if (channels.Count > 10000) break;
                    }
                }
                else
                {
                    channels.Add(new ChannelInfo { Name = baseName, Url = baseUrl, Group = "生成器", Status = "未检测", Visible = true });
                }
            }

            btnAction.Click += (s, e) =>
            {
                if (currentStep == 1)
                {
                    string input = phStep1Active ? "" : txtStep1Url.Text.Trim();

                    string extracted = ExtractUrlFromText(input);
                    if (!string.IsNullOrEmpty(extracted) && extracted != input)
                    {
                        var parsedList = ParseChannelList(input);
                        if (parsedList.Count > 1)
                        {
                            var dr = DarkMessageBox.Show(
                                string.Format("检测到 {0} 条频道列表（名称+地址格式），是否直接导入到检测窗口？\n\n点击「是」直接导入全部频道\n点击「否」使用第一条URL进行生成", parsedList.Count),
                                "检测到频道列表",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question);
                            if (dr == DialogResult.Cancel) return;
                            if (dr == DialogResult.Yes)
                            {
                                generatedChannels = parsedList;
                                dlg.DialogResult = DialogResult.OK;
                                dlg.Close();
                                return;
                            }
                        }
                        input = extracted;
                        phStep1Active = false;
                        txtStep1Url.Text = input;
                        txtStep1Url.ForeColor = DarkText;
                    }

                    step1Url = input;
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        DarkMessageBox.Show("请输入直播源地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string custErr; long cs, ce; int cpw, crp, crl; bool cpz; string ctpl;
                    if (ValidateCustomRangeUrl(input, out custErr, out cs, out ce, out cpw, out cpz, out crp, out crl, out ctpl))
                    {
                        isCustomRangeMode = true;
                        customRangeStart = cs;
                        customRangeEnd = ce;
                        customPadWidth = cpw;
                        customPadZero = cpz;
                        customReplacePos = crp;
                        customReplaceLen = crl;
                        customUrlTemplate = ctpl;
                        currentStep = 3;
                        txtFrom.Text = cs.ToString();
                        txtTo.Text = ce.ToString();
                        pnlTextOptions.Visible = false;
                        lblStep3From.Visible = true;
                        lblStep3To.Visible = true;
                        pFrom.Visible = true;
                        pTo.Visible = true;
                        pnlRangeHint.Visible = true;
                        string sampleUrl = ctpl.Substring(0, crp) + PadNumber(cs, cpw, cpz) + ctpl.Substring(crp + crl);
                        lblStep3Preview.Text = string.Format("✅ 检测到自定义范围格式\n将生成 {0} 个源地址\n示例：{1}", ce - cs + 1, sampleUrl);
                        lblStep3Preview.Visible = true;
                        UpdateStepUI();
                        return;
                    }

                    string bktErr; string cleanInput; long bktNum; int bktPos; int bktLen;
                    int manualBracketGStart = -1;
                    int manualBracketGLen = 0;
                    if (ParseManualBracketUrl(input, out bktErr, out cleanInput, out bktNum, out bktPos, out bktLen))
                    {
                        input = cleanInput;
                        manualBracketGStart = bktPos;
                        manualBracketGLen = bktLen;
                    }

                    if (!string.IsNullOrEmpty(bktErr))
                    {
                        DarkMessageBox.Show(bktErr, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!Uri.IsWellFormedUriString(input, UriKind.Absolute))
                    {
                        DarkMessageBox.Show("请输入有效的直播源地址（如 http://example.com/1.m3u8）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    isCustomRangeMode = false;
                    lblStep3Preview.Visible = false;
                    BuildUrlPreview(input, -1, 0, 0, manualBracketGStart, manualBracketGLen);
                    currentStep = 2;
                    UpdateStepUI();
                }
                else if (currentStep == 2)
                {
                    if (segs == null || segs.Count == 0)
                    {
                        DarkMessageBox.Show("未找到可生成的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (selectedSegIndex < 0 || selectedSegIndex >= segs.Count)
                    {
                        DarkMessageBox.Show("请选择要生成的字段（点击RadioButton或字段文本）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var curSeg = segs[selectedSegIndex];
                    selectedTextValues = null;

                    if (curSeg.Type == ScanSegType.Number)
                    {
                        string numStr = curSeg.OriginalText;
                        int subStart = (subSegStart != null && selectedSegIndex >= 0 && selectedSegIndex < subSegStart.Length) ? subSegStart[selectedSegIndex] : 0;
                        int subLen = (subSegLen != null && selectedSegIndex >= 0 && selectedSegIndex < subSegLen.Length) ? subSegLen[selectedSegIndex] : numStr.Length;
                        if (subStart < 0) subStart = 0;
                        if (subStart + subLen > numStr.Length) subLen = numStr.Length - subStart;
                        if (subLen <= 0) { subStart = 0; subLen = numStr.Length; }
                        string selDigits = numStr.Substring(subStart, subLen);
                        segPadWidth = subLen;
                        segPadZero = selDigits.Length > 1 && selDigits.StartsWith("0");
                        long selNum;
                        if (long.TryParse(selDigits, out selNum))
                        {
                            txtFrom.Text = selNum.ToString();
                            long defaultTo = selNum + (selNum < 100 ? 10 : (selNum < 10000 ? 20 : 50));
                            txtTo.Text = defaultTo.ToString();
                        }
                        lblStep3From.Text = "起始数字";
                        lblStep3To.Text = "结束数字";
                        lblStep3From.Visible = true;
                        lblStep3To.Visible = true;
                        pFrom.Visible = true;
                        pTo.Visible = true;
                        pnlRangeHint.Visible = true;
                        pnlTextOptions.Visible = false;
                    }
                    else
                    {
                        clstTextCandidates.Items.Clear();
                        var candidates = curSeg.Candidates ?? new List<string>();
                        string origText = curSeg.OriginalText;
                        List<string> orderedCandidates = new List<string>();
                        if (candidates.Contains(origText))
                        {
                            orderedCandidates.Add(origText);
                        }
                        foreach (string c in candidates)
                        {
                            if (c != origText && !orderedCandidates.Contains(c))
                                orderedCandidates.Add(c);
                        }
                        int origIdx = -1;
                        for (int ci = 0; ci < orderedCandidates.Count; ci++)
                        {
                            string c = orderedCandidates[ci];
                            string displayName = GetChannelDisplayName(c, curSeg.Type);
                            clstTextCandidates.Items.Add(c + (displayName != c ? " (" + displayName + ")" : ""));
                            if (c == origText) origIdx = ci;
                        }
                        for (int ci = 0; ci < clstTextCandidates.Items.Count; ci++)
                        {
                            clstTextCandidates.SetItemChecked(ci, ci == origIdx || ci < 5);
                        }
                        string typeLabel = "选项";
                        if (curSeg.Type == ScanSegType.CctvChannel) typeLabel = "CCTV频道";
                        else if (curSeg.Type == ScanSegType.PayChannel) typeLabel = "付费频道";
                        else if (curSeg.Type == ScanSegType.WsChannel) typeLabel = "卫视频道";
                        else if (curSeg.Type == ScanSegType.MovieChannel) typeLabel = "影视频道";
                        else if (curSeg.Type == ScanSegType.Resolution) typeLabel = "分辨率";
                        lblTextOptTitle.Text = "选择要替换的" + typeLabel + "：";
                        lblStep3From.Visible = false;
                        lblStep3To.Visible = false;
                        pFrom.Visible = false;
                        pTo.Visible = false;
                        pnlRangeHint.Visible = false;
                        pnlTextOptions.Visible = true;
                    }

                    currentStep = 3;
                    lblStep3Preview.Visible = false;
                    UpdateStepUI();
                }
                else if (currentStep == 3)
                {
                    if (isCustomRangeMode)
                    {
                        long fv, tv;
                        if (!long.TryParse(txtFrom.Text, out fv)) fv = 0;
                        if (!long.TryParse(txtTo.Text, out tv)) tv = 0;
                        if (fv < customRangeStart || fv > customRangeEnd || tv < customRangeStart || tv > customRangeEnd || fv >= tv)
                        {
                            DarkMessageBox.Show("请输入有效的范围值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        fromVal = fv;
                        toVal = tv;
                        DoGenerate();
                        return;
                    }

                    var curSeg = segs[selectedSegIndex];
                    if (curSeg.Type == ScanSegType.Number)
                    {
                        long fv, tv;
                        if (!long.TryParse(txtFrom.Text, out fv)) fv = 0;
                        if (!long.TryParse(txtTo.Text, out tv)) tv = 0;
                        if (fv >= tv)
                        {
                            DarkMessageBox.Show("起始值必须小于结束值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        long estCount = tv - fv + 1;
                        if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
                            estCount *= ResolutionList.Length;
                        if (estCount > 10000)
                        {
                            DarkMessageBox.Show("生成范围过大，预计生成超过10000个源，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        fromVal = fv;
                        toVal = tv;
                    }
                    else
                    {
                        selectedTextValues = new List<string>();
                        var candidates = curSeg.Candidates ?? new List<string>();
                        string origText = curSeg.OriginalText;
                        List<string> orderedCandidates = new List<string>();
                        if (candidates.Contains(origText)) orderedCandidates.Add(origText);
                        foreach (string c in candidates)
                        {
                            if (c != origText && !orderedCandidates.Contains(c))
                                orderedCandidates.Add(c);
                        }
                        for (int ci = 0; ci < clstTextCandidates.Items.Count; ci++)
                        {
                            if (clstTextCandidates.GetItemChecked(ci) && ci < orderedCandidates.Count)
                                selectedTextValues.Add(orderedCandidates[ci]);
                        }
                        if (selectedTextValues.Count == 0)
                        {
                            DarkMessageBox.Show("请至少选择一个选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        long estCount = selectedTextValues.Count;
                        if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
                            estCount *= ResolutionList.Length;
                        if (estCount > 10000)
                        {
                            DarkMessageBox.Show("选择过多，预计生成超过10000个源，请减少选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    DoGenerate();
                }
            };

            void GoPrev()
            {
                if (currentStep > 1)
                {
                    if (isCustomRangeMode)
                    {
                        isCustomRangeMode = false;
                        currentStep = 1;
                    }
                    else
                    {
                        currentStep--;
                    }
                    lblStep3Preview.Visible = false;
                    UpdateStepUI();
                }
            }

            btnPrev.Click += (s, e) => { GoPrev(); };

            void BindEnter(TextBox tb)
            {
                tb.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; btnAction.PerformClick(); }
                };
            }
            BindEnter(txtStep1Url);
            BindEnter(txtFrom);
            BindEnter(txtTo);

            dlg.KeyDown += (s, e) =>
            {
                if (e.Alt || e.Control) return;
                if (e.KeyCode == Keys.N && currentStep < 3 && !isCustomRangeMode) { btnAction.PerformClick(); }
                if (e.KeyCode == Keys.B && btnPrev.Visible) { GoPrev(); }
            };

            dlg.Controls.Add(contentHost);
            dlg.Controls.Add(sepBottom);
            dlg.Controls.Add(bottomBar);
            dlg.Controls.Add(sepTitle);
            dlg.Controls.Add(titleBar);

            dlg.Shown += (s, e) =>
            {
                UpdateStepUI();
                txtStep1Url.Focus();
            };

            MakeRounded(dlg, 12);

            Form opacityForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.Black,
                Opacity = 0.35,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = this.Bounds
            };
            opacityForm.Shown += (s, e) =>
            {
                dlg.Location = new Point(
                    this.Left + (this.Width - dlg.Width) / 2,
                    this.Top + (this.Height - dlg.Height) / 2
                );
                dlg.ShowDialog(opacityForm);
                opacityForm.Close();
                if (generatedChannels != null && generatedChannels.Count > 0)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        allChannels.Clear();
                        HashSet<string> existingUrls = new HashSet<string>();
                        int newCount = 0;
                        int dupCount = 0;
                        foreach (var ch in generatedChannels)
                        {
                            string urlKey = ch.Url.ToLowerInvariant();
                            if (existingUrls.Contains(urlKey))
                            {
                                dupCount++;
                                continue;
                            }
                            if (string.IsNullOrEmpty(ch.Name) || System.Text.RegularExpressions.Regex.IsMatch(ch.Name, @"^源\d+$"))
                            {
                                ch.Name = string.Format("源{0}", allChannels.Count + 1);
                            }
                            allChannels.Add(ch);
                            existingUrls.Add(urlKey);
                            newCount++;
                        }
                        totalCount = allChannels.Count;
                        detectedCount = 0;
                        availableCount = 0;
                        UpdateGroupFilter();
                        RefreshGrid();
                        UpdateEmptyState();
                        UpdateStatusBar();
                        UpdateActionButtonsVisibility();
                    }));
                }
            };
            opacityForm.Show(this);
        }

        private enum SearchMode { Browser, WebView2 }

        private bool IsWebView2Supported()
        {
            try
            {
                if (!CheckWindowsVersionSupported())
                {
                    return false;
                }

                if (CheckEdgeBrowserInstalled())
                    return true;

                if (CheckWebView2LoaderExists())
                    return true;

                if (CheckWebView2FromRegistry())
                    return true;
            }
            catch
            {
            }
            return false;
        }

        private bool CheckWindowsVersionSupported()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
                if (key != null)
                {
                    string productName = key.GetValue("ProductName") as string;
                    string currentBuild = key.GetValue("CurrentBuild") as string;
                    key.Close();

                    if (!string.IsNullOrEmpty(productName) && productName.Contains("Windows 11"))
                    {
                        return true;
                    }

                    if (!string.IsNullOrEmpty(currentBuild))
                    {
                        if (int.TryParse(currentBuild, out int build))
                        {
                            return build >= 17763;
                        }
                    }
                }

                Version osVersion = Environment.OSVersion.Version;
                if (osVersion.Major >= 10 && osVersion.Build >= 17763)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private bool CheckWebView2FromRegistry()
        {
            string[] checkPaths = new string[]
            {
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{D20EA4E1-3957-407C-9457-4CA219C63F57}",
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{D20EA4E1-3957-407C-9457-4CA219C63F57}"
            };

            foreach (string path in checkPaths)
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(path, false);
                    if (key != null)
                    {
                        string version = key.GetValue("pv") as string;
                        key.Close();
                        if (!string.IsNullOrEmpty(version))
                            return true;
                    }
                }
                catch { }
            }

            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", false);
                if (key != null)
                {
                    string version = key.GetValue("pv") as string;
                    key.Close();
                    if (!string.IsNullOrEmpty(version))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private bool CheckEdgeBrowserInstalled()
        {
            try
            {
                RegistryKey edgeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Edge", false);
                if (edgeKey != null)
                {
                    string version = edgeKey.GetValue("Version") as string;
                    edgeKey.Close();
                    if (!string.IsNullOrEmpty(version))
                        return true;
                }
            }
            catch { }

            try
            {
                string programFilesEdge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe");
                string programFilesX86Edge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe");
                
                if (File.Exists(programFilesEdge) || File.Exists(programFilesX86Edge))
                    return true;
            }
            catch { }

            return false;
        }

        private bool CheckWebView2LoaderExists()
        {
            try
            {
                string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string syswow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                
                string[] loaderPaths = new string[]
                {
                    Path.Combine(system32, "WebView2Loader.dll"),
                    Path.Combine(syswow64, "WebView2Loader.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "EdgeWebView2", "Application", "WebView2Loader.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView2", "Application", "WebView2Loader.dll")
                };

                foreach (string path in loaderPaths)
                {
                    if (File.Exists(path))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private List<string> CheckRuntimeDependencies()
        {
            List<string> missingDependencies = new List<string>();

            try
            {
                bool x86Installed = CheckVcRuntimeInstalled("x86");
                bool x64Installed = CheckVcRuntimeInstalled("x64");

                if (!x86Installed)
                {
                    missingDependencies.Add("Microsoft Visual C++ 2015-2022 运行时 (x86)");
                }
                if (!x64Installed)
                {
                    missingDependencies.Add("Microsoft Visual C++ 2015-2022 运行时 (x64)");
                }
            }
            catch { }

            return missingDependencies;
        }

        private bool CheckVcRuntimeInstalled(string arch)
        {
            try
            {
                string[] paths = new string[]
                {
                    $@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{arch}",
                    $@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\{arch}",
                    $@"SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeAdditionalVSU_{(arch == "x64" ? "amd64" : "x86")},v14"
                };

                foreach (string path in paths)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(path, false);
                    if (key != null)
                    {
                        key.Close();
                        return true;
                    }
                }

                string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string vcDll = Path.Combine(systemDir, "msvcp140.dll");
                if (File.Exists(vcDll))
                    return true;

                string sysWow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                string vcDllX86 = Path.Combine(sysWow64, "msvcp140.dll");
                if (arch == "x86" && File.Exists(vcDllX86))
                    return true;
            }
            catch { }

            return false;
        }

        private bool InstallWebView2Runtime()
        {
            try
            {
                string downloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
                string tempFile = Path.Combine(Path.GetTempPath(), "WebView2RuntimeInstaller.exe");

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.DownloadFile(downloadUrl, tempFile);
                }

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(tempFile)
                {
                    Arguments = "/install /quiet /norestart",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch { }
            
            return InstallWebView2RuntimeViaPowerShell();
        }

        private bool InstallWebView2RuntimeViaPowerShell()
        {
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "WebView2RuntimeInstaller.exe");
                
                string downloadScript = @"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;
$url = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';
$output = '" + tempFile + @"';
Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing;
if (Test-Path $output) { Start-Process $output -ArgumentList '/install /quiet /norestart' -Wait; exit $LASTEXITCODE }
else { exit 1 }
";

                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + downloadScript.Replace("\"", "'").Replace("`n", " ") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    };
                    proc.Start();
                    proc.WaitForExit();
                    return proc.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }

        private bool InstallVcRuntime()
        {
            try
            {
                string vcUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
                string tempFile = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.DownloadFile(vcUrl, tempFile);
                }

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(tempFile)
                {
                    Arguments = "/install /quiet /norestart",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch { }

            return InstallVcRuntimeViaPowerShell();
        }

        private bool InstallVcRuntimeViaPowerShell()
        {
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");

                string script = @"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;
$url = 'https://aka.ms/vs/17/release/vc_redist.x64.exe';
$output = '" + tempFile + @"';
Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing;
if (Test-Path $output) { Start-Process $output -ArgumentList '/install /quiet /norestart' -Wait; exit $LASTEXITCODE }
else { exit 1 }
";

                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "'").Replace("`n", " ") + "\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    };
                    proc.Start();
                    proc.WaitForExit();
                    return proc.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }

        private SearchMode ShowModeSelectionDialog()
        {
            bool isDark = theme.Name == "深色";
            Color LightBlueBg = Color.FromArgb(46, 78, 126);
            Color PanelBg = isDark ? LightBlueBg : Color.White;
            Color SurfaceBg = isDark ? Color.FromArgb(36, 62, 100) : Color.White;
            Color TextPrimary = isDark ? Color.White : Color.FromArgb(51, 51, 51);
            Color TextSecondary = isDark ? Color.FromArgb(200, 220, 240) : Color.FromArgb(153, 153, 153);
            Color BorderColor = isDark ? Color.FromArgb(60, 100, 150) : Color.FromArgb(200, 203, 210);
            Color PrimaryColor = isDark ? Color.FromArgb(100, 150, 220) : theme.Primary;

            Form dlg = new Form
            {
                Text = "选择搜索模式",
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = PanelBg,
                ClientSize = new Size(SX(600), SY(420)),
                KeyPreview = true
            };
            SetFormDarkModeTitleBar(dlg, isDark);
            CenterForm(dlg, this);
            
            SearchMode result = (SearchMode)(-1); // 默认无效值，表示取消
            bool isConfirmed = false;
            
            // 处理窗口右上角关闭按钮
            dlg.FormClosing += (s, e) =>
            {
                if (!isConfirmed)
                {
                    result = (SearchMode)(-1);
                }
            };

            RadioButton rbBrowser = new RadioButton
            {
                Text = "🌐 浏览器模式",
                Font = GetFont(14f),
                ForeColor = TextPrimary,
                BackColor = PanelBg,
                Location = new Point(SX(30), SY(30)),
                Size = new Size(SX(540), SY(40)),
                Checked = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            dlg.Controls.Add(rbBrowser);

            Label lblBrowserDesc = new Label
            {
                Text = "使用系统默认浏览器打开网络空间搜索引擎",
                Font = GetFont(12f),
                ForeColor = TextSecondary,
                Location = new Point(SX(30), SY(78)),
                Size = new Size(SX(540), SY(32)),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            dlg.Controls.Add(lblBrowserDesc);

            RadioButton rbWebView2 = new RadioButton
            {
                Text = "🖥️ WebView2窗口模式",
                Font = GetFont(14f),
                ForeColor = TextPrimary,
                BackColor = PanelBg,
                Location = new Point(SX(30), SY(120)),
                Size = new Size(SX(540), SY(40)),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            dlg.Controls.Add(rbWebView2);

            Label lblWebView2Desc = new Label
            {
                Text = "在应用内窗口中使用Edge内核显示搜索页面",
                Font = GetFont(12f),
                ForeColor = TextSecondary,
                Location = new Point(SX(30), SY(168)),
                Size = new Size(SX(540), SY(32)),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            dlg.Controls.Add(lblWebView2Desc);

            // 鼠标点击交互：点击整个区域都能选中对应选项
            rbBrowser.Click += (s, e) => { rbBrowser.Checked = true; };
            lblBrowserDesc.Click += (s, e) => { rbBrowser.Checked = true; };
            rbWebView2.Click += (s, e) => { rbWebView2.Checked = true; };
            lblWebView2Desc.Click += (s, e) => { rbWebView2.Checked = true; };

            Label lblStatus = new Label
            {
                Text = "",
                Font = GetFont(12f),
                ForeColor = isDark ? Color.FromArgb(100, 255, 100) : Color.Green,
                Location = new Point(SX(30), SY(210)),
                Size = new Size(SX(540), SY(36)),
                AutoSize = false
            };
            dlg.Controls.Add(lblStatus);

            bool webView2Supported = IsWebView2Supported();
            if (!webView2Supported)
            {
                lblStatus.Text = "⚠️ 系统未安装WebView2运行库，选择此模式将自动下载安装";
                lblStatus.ForeColor = isDark ? Color.FromArgb(255, 200, 50) : Color.Orange;
            }
            else
            {
                lblStatus.Text = "✓ WebView2运行库已安装";
                lblStatus.ForeColor = isDark ? Color.FromArgb(100, 255, 100) : Color.Green;
            }

            int btnSpacing = (dlg.ClientSize.Width - 120 - 120) / 3;
            Button btnOK = new Button
            {
                Text = "确定",
                Size = new Size(SX(120), SY(36)),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                Font = GetFont(10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(btnSpacing, SY(340))
            };
            btnOK.FlatAppearance.BorderSize = 0;
            StyleRoundButton(btnOK, 6, null, 0, "dynamic");
            dlg.Controls.Add(btnOK);

            Button btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(SX(120), SY(36)),
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(30, 50, 80) : Color.White,
                ForeColor = TextPrimary,
                Font = GetFont(10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(btnSpacing + SX(120) + btnSpacing, SY(340))
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.FlatAppearance.BorderSize = 1;
            StyleRoundButton(btnCancel, 6, BorderColor, 1, "border");
            dlg.Controls.Add(btnCancel);

            btnOK.Click += (s, e) =>
            {
                isConfirmed = true;
                if (rbBrowser.Checked)
                {
                    result = SearchMode.Browser;
                }
                else
                {
                    result = SearchMode.WebView2;
                }
                dlg.Close();
            };
            btnCancel.Click += (s, e) =>
            {
                isConfirmed = false;
                result = (SearchMode)(-1);
                dlg.Close();
            };

            dlg.ShowDialog(this);
            return isConfirmed ? result : (SearchMode)(-1);
        }

        private void ShowSearchEngineDialog()
        {
            SearchMode mode;
            
            if (watchSearchWindow)
            {
                mode = SearchMode.WebView2;
            }
            else
            {
                mode = ShowModeSelectionDialog();
                
                if ((int)mode == -1)
                {
                    return;
                }
            }
            
            if (mode == SearchMode.WebView2)
            {
                if (!CheckWindowsVersionSupported())
                {
                    DarkMessageBox.Show("您的系统版本过低，WebView2需要Windows 10 1809或更高版本。将自动使用浏览器模式打开。", "系统版本不支持", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    mode = SearchMode.Browser;
                }
                else
                {
                    List<string> missingDeps = CheckRuntimeDependencies();
                    if (missingDeps.Count > 0)
                    {
                        string depList = string.Join("\n", missingDeps);
                        DialogResult confirm = DarkMessageBox.Show($"检测到缺少以下运行库依赖：\n{depList}\n\n是否自动下载安装？", "缺少运行库", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm == DialogResult.Yes)
                        {
                            Form progressForm = CreateProgressForm("正在安装运行库依赖...");
                            progressForm.Show(this);
                            Application.DoEvents();

                            bool vcInstalled = InstallVcRuntime();
                            progressForm.Close();

                            if (!vcInstalled)
                            {
                                DarkMessageBox.Show("VC++运行时安装失败，WebView2可能无法正常工作。将使用浏览器模式打开。", "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                mode = SearchMode.Browser;
                            }
                        }
                        else
                        {
                            mode = SearchMode.Browser;
                        }
                    }

                    if (mode == SearchMode.WebView2 && !IsWebView2Supported())
                    {
                        DialogResult confirm = DarkMessageBox.Show("系统未安装WebView2运行库，是否自动下载安装？", "缺少WebView2运行库", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm == DialogResult.Yes)
                        {
                            Form progressForm = CreateProgressForm("正在下载并安装WebView2运行库，请稍候...");
                            progressForm.Show(this);
                            Application.DoEvents();

                            bool installed = InstallWebView2Runtime();
                            progressForm.Close();

                            if (!installed)
                            {
                                DarkMessageBox.Show("WebView2运行库安装失败，将使用浏览器模式打开", "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                mode = SearchMode.Browser;
                            }
                        }
                        else
                        {
                            mode = SearchMode.Browser;
                        }
                    }
                }
            }

            if (mode == SearchMode.Browser)
            {
                ShowBrowserSearchDialog();
            }
            else
            {
                ShowWebView2SearchDialog();
            }
        }

        /// <summary>
        /// 创建简易进度对话框（轻量级，仅显示文本提示）
        /// 用于安装运行库、WebView2等后台操作时显示等待状态
        /// </summary>
        /// <param name="message">显示的提示文字</param>
        /// <returns>进度对话框窗体</returns>
        private Form CreateProgressForm(string message)
        {
            // ========== 颜色配置（根据主题自动切换） ==========
            bool isDark = theme.Name == "深色";
            Color progressBg = isDark ? Color.FromArgb(46, 78, 126) : Color.White;      // 窗口背景色（深色：蓝灰色 / 浅色：纯白）
            Color progressText = isDark ? Color.White : Color.FromArgb(51, 51, 51);     // 文字颜色

            // ========== 主窗口配置 ==========
            // [位置] 手动定位（由调用方负责居中）[大小] 400x120 [样式] 固定对话框
            Form progressForm = new Form
            {
                Text = "安装进度",                         // 窗口标题
                StartPosition = FormStartPosition.Manual,   // 手动定位（调用方负责居中于主窗口）
                FormBorderStyle = FormBorderStyle.FixedDialog,  // 固定对话框样式，禁止调整大小
                MaximizeBox = false,                       // 禁用最大化按钮
                MinimizeBox = false,                       // 禁用最小化按钮
                ShowInTaskbar = false,                     // 不在任务栏显示
                ClientSize = new Size(SX(400), SY(120)),    // 窗口大小（宽400px，高120px，DPI适配）
                BackColor = progressBg                     // 窗口背景色
            };
            SetFormDarkModeTitleBar(progressForm, isDark);  // 应用深色标题栏

            // ========== 提示标签 ==========
            // [位置] (20, 40) [大小] 360x24 [字体] YaHei 10pt
            Label lblProgress = new Label
            {
                Text = message,
                Font = GetFont(10f),
                ForeColor = progressText,
                Location = new Point(SX(20), SY(40)),
                Size = new Size(SX(360), SY(24))
            };
            progressForm.Controls.Add(lblProgress);

            return progressForm;
        }

        /// <summary>
        /// 显示浏览器搜索对话框（规则搜索功能）
        /// 允许用户选择搜索规则和搜索引擎，在浏览器中打开搜索结果
        /// </summary>
        private void ShowBrowserSearchDialog()
        {
            // ========== 颜色配置（根据主题自动切换） ==========
            bool isDark = theme.Name == "深色";
            Color LightBlueBg = Color.FromArgb(46, 78, 126);      // 深色主题浅蓝背景
            Color PanelBg = isDark ? LightBlueBg : Color.White;   // 面板背景色（深色：浅蓝 / 浅色：纯白）
            Color SurfaceBg = isDark ? Color.FromArgb(36, 62, 100) : Color.White; // 表面背景色（比面板略深/同色）
            Color TextPrimary = isDark ? Color.White : Color.FromArgb(51, 51, 51);      // 主文字色
            Color TextSecondary = isDark ? Color.FromArgb(200, 220, 240) : Color.FromArgb(153, 153, 153); // 次要文字色
            Color BorderColor = isDark ? Color.FromArgb(60, 100, 150) : Color.FromArgb(200, 203, 210);  // 边框色
            Color PrimaryColor = isDark ? Color.FromArgb(100, 150, 220) : theme.Primary; // 主色调（蓝色系）
            Color HoverBg = isDark ? Color.FromArgb(50, 85, 135) : Color.FromArgb(248, 249, 250); // 悬停背景色

            // 保存主窗口状态（用于关闭对话框后恢复）
            FormWindowState originalState = this.WindowState;
            Rectangle originalBounds = this.Bounds;

            // ========== 主窗口配置 ==========
            // [标题] 规则搜索 [大小] 900x550 [样式] 可调整大小 [位置] 居中于主窗口
            Form dlg = new Form
            {
                Text = "规则搜索",                    // 窗口标题
                StartPosition = FormStartPosition.Manual,  // 手动定位
                FormBorderStyle = FormBorderStyle.Sizable, // 可调整大小
                MaximizeBox = true,                   // 启用最大化按钮
                MinimizeBox = true,                   // 启用最小化按钮
                ShowInTaskbar = true,                 // 在任务栏显示
                BackColor = PanelBg,                  // 窗口背景色
                ClientSize = new Size(SX(900), SY(550)),  // 窗口大小（900x550px，DPI适配）
                KeyPreview = true                     // 预览键盘事件
            };
            SetFormDarkModeTitleBar(dlg, isDark);     // 应用深色标题栏
            CenterForm(dlg, this);                    // 居中于主窗口

            // 隐藏主窗口，关闭对话框后恢复
            this.Hide();
            dlg.Closed += (s, e) =>
            {
                this.Show();                         // 显示主窗口
                this.WindowState = originalState;    // 恢复窗口状态
                if (originalState != FormWindowState.Maximized)
                {
                    this.Bounds = originalBounds;    // 恢复窗口位置和大小
                }
                this.Activate();                     // 激活主窗口
                this.Refresh();                      // 刷新界面
            };

            // ========== 顶部栏（标题+规则选择+搜索按钮） ==========
            // [位置] 顶部Dock [高度] 60px [背景] 表面背景色
            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = SY(60),
                BackColor = SurfaceBg
            };
            dlg.Controls.Add(topBar);

            // ========== 主内容面板（搜索引擎列表） ==========
            // [位置] 填充剩余空间 [内边距] 20px [背景] 面板背景色
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg,
                Padding = new Padding(SX(20))
            };
            dlg.Controls.Add(mainPanel);

            // ========== 标题标签 ==========
            // [位置] (16, 0) [大小] 200x48 [字体] YaHei 12pt Bold [对齐] 左居中
            Label lblTitle = new Label
            {
                Text = "规则搜索",
                Font = GetFont(12f, FontStyle.Bold),  // 字体（12pt Bold * DPI缩放）
                ForeColor = TextPrimary,
                Location = new Point(SX(16), 0),
                Size = new Size(SX(200), SY(48)),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(lblTitle);

            // ========== 搜索规则下拉框 ==========
            // [位置] (220, 14) [大小] 120x32 [选项] 智慧桌面/智慧光迅/华视美达 [默认] 智慧桌面
            ComboBox cboSearchRule = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, // 只读下拉列表
                Size = new Size(SX(120), SY(32)),
                FlatStyle = FlatStyle.Flat,
                BackColor = isDark ? Color.FromArgb(30, 50, 80) : Color.White, // 背景色
                ForeColor = TextPrimary,
                Font = GetFont(10f),                        // 字体（10pt * DPI缩放）
                Cursor = Cursors.Hand,
                Location = new Point(SX(220), SY(14))
            };
            cboSearchRule.Items.AddRange(new object[] { "智慧桌面", "智慧光迅", "华视美达" });
            cboSearchRule.SelectedIndex = 0;               // 默认选中第一个选项
            // 自定义绘制边框
            cboSearchRule.Paint += (s, e) =>
            {
                ComboBox cb = (ComboBox)s;
                using (Pen pen = new Pen(BorderColor))
                    e.Graphics.DrawRectangle(pen, 0, 0, cb.Width - 1, cb.Height - 1);
            };
            topBar.Controls.Add(cboSearchRule);

            // ========== 搜索规则映射表 ==========
            // 键：规则名称，值：FOFA搜索规则
            var searchRules = new Dictionary<string, string>
            {
                {"智慧桌面", "body=\"/iptv/live/zh_cn.js\""},
                {"智慧光迅", "body=\"ZHGXTV\""},
                {"华视美达", "body=\"华视美达\""}
            };

            // ========== 搜索引擎映射表 ==========
            // 键：搜索引擎名称，值：搜索引擎URL
            var searchEngines = new Dictionary<string, string>
            {
                {"FOFA", "https://fofa.info/"},
                {"Quake", "https://quake.360.net/"},
                {"Hunter", "https://hunter.qianxin.com/"},
                {"ZoomEye", "https://www.zoomeye.org/"},
                {"Shodan", "https://www.shodan.io/"},
                {"Censys", "https://search.censys.io/"}
            };

            string selectedEngine = "FOFA"; // 默认选中FOFA

            // ========== 打开搜索按钮 ==========
            // [位置] (780, 14) [大小] 100x32 [字体] YaHei 10pt Bold [圆角] 6px [颜色] 主色调背景+白色文字
            Button btnSearch = new Button
            {
                Text = "打开搜索",
                Size = new Size(SX(100), SY(32)),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                Font = GetFont(10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(SX(780), SY(14)),
                Visible = showSearchButton
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, PrimaryColor.R + 20),
                Math.Min(255, PrimaryColor.G + 20),
                Math.Min(255, PrimaryColor.B + 20)); // 悬停时颜色变亮
            StyleRoundButton(btnSearch, 6, null, 0, "dynamic"); // 设置圆角（6px）
            btnSearch.Click += (s, e) =>
            {
                // 获取选中的规则和引擎，构建搜索URL并在浏览器中打开
                string ruleName = cboSearchRule.SelectedItem?.ToString() ?? "智慧桌面";
                string searchRule = searchRules.ContainsKey(ruleName) ? searchRules[ruleName] : searchRules["智慧桌面"];
                string baseUrl = searchEngines.ContainsKey(selectedEngine) ? searchEngines[selectedEngine] : searchEngines["FOFA"];
                string url = baseUrl + "result?qbase64=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(searchRule));
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            };
            topBar.Controls.Add(btnSearch);
            btnSearchRef = btnSearch;

            // 顶部栏底部边框绘制
            topBar.Paint += (s, e) =>
            {
                Panel p = (Panel)s;
                using (Pen pen = new Pen(BorderColor))
                    e.Graphics.DrawLine(pen, 0, SY(47), p.Width, SY(47)); // 在Y=47处绘制分隔线（DPI适配）
            };

            // ========== 搜索引擎列表面板 ==========
            // [位置] (20, 80) [大小] 860x420 [背景] 表面背景色 [边框] 固定单边框
            Panel listPanel = new Panel
            {
                Location = new Point(SX(20), SY(80)),
                Size = new Size(SX(860), SY(420)),
                BackColor = SurfaceBg,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(listPanel);

            Color hoverColor = isDark ? Color.FromArgb(50, 85, 135) : Color.FromArgb(240, 245, 250); // 列表项悬停色

            // ========== 动态生成搜索引擎列表项 ==========
            // 每个列表项包含：单选按钮(引擎名称) + URL标签 + 访问按钮
            // 列表项高度55px，间距5px（累计60px）
            int y = 10;
            foreach (var engine in searchEngines)
            {
                // 列表项容器（带悬停高亮）
                Panel itemPanel = new Panel
                {
                    Location = new Point(SX(10), y),
                    Size = new Size(SX(840), SY(55)),
                    BackColor = Color.Transparent
                };
                listPanel.Controls.Add(itemPanel);

                // 悬停效果：鼠标进入时显示高亮背景
                itemPanel.MouseEnter += (s, e) =>
                {
                    itemPanel.BackColor = hoverColor;
                };
                itemPanel.MouseLeave += (s, e) =>
                {
                    itemPanel.BackColor = Color.Transparent;
                };

                // 搜索引擎单选按钮
                RadioButton rbEngine = new RadioButton
                {
                    Text = engine.Key,
                    Size = new Size(SX(100), SY(36)),
                    Location = new Point(0, SY(7)),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = isDark ? Color.White : Color.FromArgb(51, 51, 51),
                    Font = GetFont(10f, FontStyle.Bold), // 字体（10pt Bold * DPI缩放）
                    Cursor = Cursors.Hand,
                    Checked = engine.Key == selectedEngine // 默认选中FOFA
                };
                rbEngine.CheckedChanged += (s, e) =>
                {
                    if (rbEngine.Checked)
                    {
                        selectedEngine = engine.Key; // 更新选中的搜索引擎
                    }
                };
                itemPanel.Controls.Add(rbEngine);

                // 搜索引擎URL标签
                Label lblUrl = new Label
                {
                    Text = engine.Value,
                    Size = new Size(SX(630), SY(36)),
                    Location = new Point(SX(130), SY(10)),
                    Font = GetFont(10f),              // 字体（10pt * DPI缩放）
                    ForeColor = TextSecondary,       // 次要文字色（灰色）
                    TextAlign = ContentAlignment.MiddleLeft
                };
                itemPanel.Controls.Add(lblUrl);

                // 访问按钮
                Button btnGo = new Button
                {
                    Text = "访问",
                    Size = new Size(SX(70), SY(36)),
                    Location = new Point(SX(760), SY(10)),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = PrimaryColor,         // 主色调背景
                    ForeColor = Color.White,          // 白色文字
                    Font = GetFont(9f, FontStyle.Bold), // 字体（9pt Bold * DPI缩放）
                    Cursor = Cursors.Hand
                };
                btnGo.FlatAppearance.BorderSize = 0;
                btnGo.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Min(255, PrimaryColor.R + 20),
                    Math.Min(255, PrimaryColor.G + 20),
                    Math.Min(255, PrimaryColor.B + 20)); // 悬停时颜色变亮
                StyleRoundButton(btnGo, 6, null, 0, "dynamic"); // 设置圆角（6px）
                btnGo.Click += (s, e) =>
                {
                    // 在浏览器中打开搜索引擎主页
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(engine.Value) { UseShellExecute = true });
                    }
                    catch { }
                };
                itemPanel.Controls.Add(btnGo);

                y += 60; // 下一个列表项Y坐标（55px高度 + 5px间距）
            }

            dlg.ShowDialog(this); // 显示对话框（模态）
        }

        private void ShowWebView2SearchDialog()
        {
            try
            {
                bool isDark = theme.Name == "深色";
                Color LightBlueBg = Color.FromArgb(46, 78, 126);
                Color PanelBg = isDark ? LightBlueBg : Color.White;
                Color SurfaceBg = isDark ? Color.FromArgb(36, 62, 100) : Color.White;
                Color TextPrimary = isDark ? Color.White : Color.FromArgb(51, 51, 51);
                Color TextSecondary = isDark ? Color.FromArgb(200, 220, 240) : Color.FromArgb(153, 153, 153);
                Color BorderColor = isDark ? Color.FromArgb(60, 100, 150) : Color.FromArgb(200, 203, 210);
                Color PrimaryColor = isDark ? Color.FromArgb(100, 150, 220) : theme.Primary;
                Color HoverBg = isDark ? Color.FromArgb(50, 85, 135) : Color.FromArgb(248, 249, 250);

                int NAV_BAR_H = 36;

                FormWindowState originalState = this.WindowState;
                Rectangle originalBounds = this.Bounds;

                Form dlg = new Form
                {
                    Text = "",
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MaximizeBox = true,
                    MinimizeBox = true,
                    ShowInTaskbar = true,
                    BackColor = PanelBg,
                    KeyPreview = true,
                    ClientSize = this.ClientSize,
                    Icon = null
                };
                SetFormDarkModeTitleBar(dlg, isDark);
                CenterForm(dlg, this);

                this.Hide();

                bool wasMaximized = false;
                Action SetRoundedRegion = () =>
                {
                    if (dlg.WindowState == FormWindowState.Maximized)
                    {
                        dlg.Region = null;
                        return;
                    }
                    int radius = 12;
                    using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        Rectangle rect = new Rectangle(0, 0, dlg.Width, dlg.Height);
                        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                        path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
                        path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
                        path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
                        path.CloseAllFigures();
                        dlg.Region = new Region(path);
                    }
                };
                dlg.Resize += (s, e) =>
                {
                    if (dlg.WindowState == FormWindowState.Maximized)
                    {
                        wasMaximized = true;
                    }
                    else if (dlg.WindowState == FormWindowState.Normal && wasMaximized)
                    {
                        int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                        int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                        int x = (screenWidth - dlg.Width) / 2;
                        int y = (screenHeight - dlg.Height) / 2;
                        dlg.Location = new System.Drawing.Point(x, y);
                        wasMaximized = false;
                    }
                    SetRoundedRegion();
                };
                SetRoundedRegion();

                dlg.Closed += (s, e) =>
                {
                    this.Show();
                    this.WindowState = originalState;
                    if (originalState != FormWindowState.Maximized)
                    {
                        this.Bounds = originalBounds;
                        this.CenterToScreen();
                    }
                    this.Activate();
                    this.Refresh();
                };

                Panel navPanel = new Panel
                {
                    Height = NAV_BAR_H,
                    BackColor = SurfaceBg,
                    Padding = new Padding(8, 0, 8, 0),
                    Top = -NAV_BAR_H,
                    Width = dlg.ClientSize.Width
                };
                dlg.Controls.Add(navPanel);

                int BORDER_WIDTH = 8;
                int STATUS_BAR_H = 28;

                Panel leftBorder = new Panel
                {
                    Width = BORDER_WIDTH,
                    BackColor = BorderColor,
                    Dock = DockStyle.Left
                };
                dlg.Controls.Add(leftBorder);

                Panel rightBorder = new Panel
                {
                    Width = BORDER_WIDTH,
                    BackColor = BorderColor,
                    Dock = DockStyle.Right
                };
                dlg.Controls.Add(rightBorder);

                Panel statusBar = new Panel
                {
                    Height = STATUS_BAR_H,
                    BackColor = SurfaceBg,
                    Dock = DockStyle.Bottom,
                    Padding = new Padding(8, 0, 8, 0)
                };

                Label lblStatusUrl = new Label
                {
                    Text = "",
                    Font = GetFont(9f),
                    ForeColor = TextSecondary,
                    BackColor = SurfaceBg,
                    Size = new Size(400, STATUS_BAR_H),
                    Location = new Point(8, 0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = false
                };

                Label lblStatusIp = new Label
                {
                    Text = "IP提取: 关",
                    Font = GetFont(9f),
                    ForeColor = TextSecondary,
                    BackColor = SurfaceBg,
                    Size = new Size(120, STATUS_BAR_H),
                    Location = new Point(dlg.ClientSize.Width - BORDER_WIDTH * 2 - 130, 0),
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false
                };

                Label lblStatusEngine = new Label
                {
                    Text = "搜索引擎: FOFA",
                    Font = GetFont(9f),
                    ForeColor = TextSecondary,
                    BackColor = SurfaceBg,
                    Size = new Size(150, STATUS_BAR_H),
                    Location = new Point(dlg.ClientSize.Width - BORDER_WIDTH * 2 - 280, 0),
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false
                };

                statusBar.Controls.Add(lblStatusUrl);
                statusBar.Controls.Add(lblStatusIp);
                statusBar.Controls.Add(lblStatusEngine);
                dlg.Controls.Add(statusBar);

                webViewStatusUrl = lblStatusUrl;

                System.Windows.Forms.Timer navTimer = new System.Windows.Forms.Timer();
                navTimer.Interval = 100;

                bool isNavVisible = false;
                bool isDropdownOpen = false;
                int hideCountdown = 0;

                Action ShowNav = () =>
                {
                    if (!isNavVisible)
                    {
                        navPanel.Top = 0;
                        isNavVisible = true;
                    }
                    hideCountdown = 0;
                };

                Action HideNav = () =>
                {
                    if (isNavVisible)
                    {
                        navPanel.Top = -NAV_BAR_H;
                        isNavVisible = false;
                    }
                };

                navTimer.Tick += (s, e) =>
                {
                    if (isDropdownOpen)
                    {
                        hideCountdown = 0;
                        return;
                    }

                    System.Drawing.Point mousePos = Control.MousePosition;
                    System.Drawing.Point dlgScreenPos = dlg.PointToScreen(new System.Drawing.Point(0, 0));
                    
                    System.Drawing.Point navVisiblePos = new System.Drawing.Point(dlgScreenPos.X, dlgScreenPos.Y);
                    bool mouseInNav = mousePos.X >= navVisiblePos.X && mousePos.X <= navVisiblePos.X + navPanel.Width &&
                                      mousePos.Y >= navVisiblePos.Y && mousePos.Y <= navVisiblePos.Y + NAV_BAR_H;

                    bool mouseInTopArea = mousePos.X >= dlgScreenPos.X && mousePos.X <= dlgScreenPos.X + dlg.Width &&
                                          mousePos.Y >= dlgScreenPos.Y && mousePos.Y <= dlgScreenPos.Y + 30;

                    if (mouseInNav || mouseInTopArea)
                    {
                        ShowNav();
                    }
                    else if (isNavVisible)
                    {
                        hideCountdown++;
                        if (hideCountdown >= 6)
                        {
                            HideNav();
                            hideCountdown = 0;
                        }
                    }
                };

                navTimer.Enabled = true;

                Label lblEngine = new Label
                {
                    Text = "搜索引擎",
                    Font = GetFont(11f, FontStyle.Bold),
                    ForeColor = TextSecondary,
                    BackColor = SurfaceBg,
                    Size = new Size(120, NAV_BAR_H - 4),
                    Location = new Point(10, 2),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                navPanel.Controls.Add(lblEngine);

                ComboBox cboEngine = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Size = new Size(130, NAV_BAR_H - 4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isDark ? Color.FromArgb(30, 50, 80) : Color.White,
                    ForeColor = TextPrimary,
                    Font = GetFont(11f),
                    Cursor = Cursors.Hand,
                    Location = new Point(130, 2)
                };
                cboEngine.Items.AddRange(new object[] { "FOFA", "Quake", "Hunter", "ZoomEye", "Shodan", "Censys" });
                cboEngine.SelectedIndex = 0;
                cboEngine.DropDown += (s, e) => isDropdownOpen = true;
                cboEngine.DropDownClosed += (s, e) => isDropdownOpen = false;
                navPanel.Controls.Add(cboEngine);

                ComboBox cboSearchRule = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Size = new Size(150, NAV_BAR_H - 4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isDark ? Color.FromArgb(30, 50, 80) : Color.White,
                    ForeColor = TextPrimary,
                    Font = GetFont(11f),
                    Cursor = Cursors.Hand,
                    Location = new Point(280, 2)
                };
                cboSearchRule.Items.AddRange(new object[] { "智慧桌面", "智慧光迅", "华视美达" });
                cboSearchRule.SelectedIndex = 0;
                cboSearchRule.DropDown += (s, e) => isDropdownOpen = true;
                cboSearchRule.DropDownClosed += (s, e) => isDropdownOpen = false;
                navPanel.Controls.Add(cboSearchRule);

                var searchRules = new Dictionary<string, string>
                {
                    {"智慧桌面", "body=\"/iptv/live/zh_cn.js\""},
                    {"智慧光迅", "body=\"ZHGXTV\""},
                    {"华视美达", "body=\"华视美达\""}
                };

                var parseTemplates = new Dictionary<string, string>
                {
                    {"智能KUTV", "http://{ip}:{port}/iptv/live/1000.json?key=txiptv"},
                    {"智慧光迅", "http://{ip}:{port}/ZHGXTV/Public/json/live_interface.txt"},
                    {"华视美达", "http://{ip}:{port}/newlive/live/hls/{cid}/live.m3u8"}
                };

                TextBox txtUrl = new TextBox
                {
                    Size = new Size(300, NAV_BAR_H - 4),
                    Font = GetFont(11f),
                    ForeColor = TextSecondary,
                    BackColor = isDark ? Color.FromArgb(30, 50, 80) : Color.White,
                    BorderStyle = BorderStyle.None,
                    Location = new Point(450, 2),
                    ReadOnly = true
                };
                StyleRoundTextBox(txtUrl, 4, BorderColor, 1);
                navPanel.Controls.Add(txtUrl);

                object webView2 = null;
                Type webView2Type = null;

                try
                {
                    webView2Type = Type.GetType("Microsoft.Web.WebView2.WinForms.WebView2, Microsoft.Web.WebView2.WinForms");
                    if (webView2Type == null)
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (asm.GetName().Name.Contains("WebView2"))
                            {
                                webView2Type = asm.GetType("Microsoft.Web.WebView2.WinForms.WebView2");
                                if (webView2Type != null) break;
                            }
                        }
                    }
                    if (webView2Type != null)
                    {
                        webView2 = Activator.CreateInstance(webView2Type);
                        Control wvCtrl = (Control)webView2;
                        wvCtrl.Location = new Point(BORDER_WIDTH, NAV_BAR_H);
                        wvCtrl.Size = new Size(dlg.ClientSize.Width - BORDER_WIDTH * 2, dlg.ClientSize.Height - NAV_BAR_H - STATUS_BAR_H);
                        dlg.Controls.Add(wvCtrl);
                        dlg.Controls.SetChildIndex(wvCtrl, 1);
                    }
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show("WebView2控件创建失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                dlg.FormClosing += (s, e) =>
                {
                    try
                    {
                        navTimer.Enabled = false;
                        navTimer.Dispose();
                    }
                    catch { }
                };

                // IP提取开关按钮
                Button btnExtractIp = new Button
                {
                    Text = autoExtractIpPort ? "IP提取: 开" : "IP提取: 关",
                    Size = new Size(110, NAV_BAR_H - 6),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = autoExtractIpPort ? Color.FromArgb(40, 160, 80) : (isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(230, 230, 235)),
                    ForeColor = autoExtractIpPort ? Color.White : TextSecondary,
                    Font = GetFont(10f),
                    Cursor = Cursors.Hand,
                    Location = new Point(760, 3),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btnExtractIp.FlatAppearance.BorderSize = 0;
                btnExtractIp.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 110, NAV_BAR_H - 6), 6));
                btnExtractIp.Click += async (s2, e2) =>
                {
                    autoExtractIpPort = !autoExtractIpPort;
                    btnExtractIp.Text = autoExtractIpPort ? "IP提取: 开" : "IP提取: 关";
                    btnExtractIp.BackColor = autoExtractIpPort ? Color.FromArgb(40, 160, 80) : (isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(230, 230, 235));
                    btnExtractIp.ForeColor = autoExtractIpPort ? Color.White : TextSecondary;
                    lblStatusIp.Text = autoExtractIpPort ? "IP提取: 开" : "IP提取: 关";
                    lblStatusIp.ForeColor = autoExtractIpPort ? Color.FromArgb(40, 160, 80) : TextSecondary;
                    SaveConfig();

                    // 立即提取当前页面的IP+端口
                    if (webView2 != null && webView2Type != null)
                    {
                        btnExtractIp.Enabled = false;
                        btnExtractIp.Text = "提取中...";
                        try
                        {
                            string extractJs =
                                "(function() { " +
                                "  var allText = ''; " +
                                "  allText += document.body.innerText || ''; " +
                                "  allText += ' ' + document.documentElement.outerHTML || ''; " +
                                "  try { " +
                                "    var iframes = document.querySelectorAll('iframe'); " +
                                "    for (var k=0; k<iframes.length; k++) { " +
                                "      try { if (iframes[k].contentDocument) { allText += ' ' + iframes[k].contentDocument.body.innerText; } } catch(e) {} " +
                                "    } " +
                                "  } catch(e) {} " +
                                "  var valid = {}; " +
                                "  var ipv4Regex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b/g; " +
                                "  var matches = allText.match(ipv4Regex) || []; " +
                                "  for (var i=0; i<matches.length; i++) { " +
                                "    valid[matches[i]] = true; " +
                                "  } " +
                                "  var urlIpRegex = /(?:http|https):\\/\\/(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?::(\\d{1,5}))?(?:\\/|\\?|$)/gi; " +
                                "  var urlMatches = allText.match(urlIpRegex) || []; " +
                                "  for (var i=0; i<urlMatches.length; i++) { " +
                                "    var urlMatch = urlMatches[i].replace(/^https?:\\/\\//i, ''); " +
                                "    var portMatch = urlMatch.match(/:(\\d{1,5})$/); " +
                                "    var ip = urlMatch.replace(/:\\d{1,5}$/, ''); " +
                                "    if (portMatch) { valid[ip + ':' + portMatch[1]] = true; } " +
                                "    else { valid[ip] = true; } " +
                                "  } " +
                                "  var ipWithPortRegex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):(\\d{1,5})\\b/g; " +
                                "  var portMatches = allText.match(ipWithPortRegex) || []; " +
                                "  for (var i=0; i<portMatches.length; i++) { " +
                                "    valid[portMatches[i]] = true; " +
                                "  } " +
                                "  var ipList = Object.keys(valid); " +
                                "  ipList = ipList.filter(function(ip) { " +
                                "    var parts = ip.split(':')[0].split('.'); " +
                                "    if (parts.length !== 4) return false; " +
                                "    for (var j=0; j<4; j++) { " +
                                "      var n = parseInt(parts[j]); " +
                                "      if (isNaN(n) || n < 0 || n > 255) return false; " +
                                "    } " +
                                "    return true; " +
                                "  }); " +
                                "  return JSON.stringify(ipList); " +
                                "})()";

                            var coreProp = webView2Type.GetProperty("CoreWebView2");
                            if (coreProp != null)
                            {
                                var core = coreProp.GetValue(webView2);
                                if (core != null)
                                {
                                    var execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new[] { typeof(string) });
                                    if (execMethod != null)
                                    {
                                        var testTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] { "document.body.innerText.length.toString()" });
                                        string testResult = await testTask;
                                        var htmlTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] { "(function(){var t=document.body.innerText.substring(0,500);return t.indexOf('.')>=0?'FOUND_DOT':'NO_DOT';})()" });
                                        string htmlResult = await htmlTask;

                                        var ipTask = (System.Threading.Tasks.Task<string>)execMethod.Invoke(core, new object[] { extractJs });
                                        string ipResult = await ipTask;

                                        if (!string.IsNullOrEmpty(ipResult))
                                        {
                                            var ips = new List<string>();
                                            try
                                            {
                                                ipResult = ipResult.Trim();
                                                var allMatches = System.Text.RegularExpressions.Regex.Matches(ipResult, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})");
                                                foreach (System.Text.RegularExpressions.Match m in allMatches)
                                                {
                                                    string ip = m.Groups[1].Value;
                                                    string port = m.Groups[2].Value;
                                                    var parts = ip.Split('.');
                                                    if (parts.Length != 4) continue;
                                                    bool isValid = true;
                                                    int[] ipParts = new int[4];
                                                    for (int i = 0; i < 4; i++)
                                                    {
                                                        if (!int.TryParse(parts[i], out ipParts[i]) || ipParts[i] < 0 || ipParts[i] > 255)
                                                        {
                                                            isValid = false;
                                                            break;
                                                        }
                                                    }
                                                    if (!isValid) continue;
                                                    if (ipParts[0] == 10) continue;
                                                    if (ipParts[0] == 172 && ipParts[1] >= 16 && ipParts[1] <= 31) continue;
                                                    if (ipParts[0] == 192 && ipParts[1] == 168) continue;
                                                    if (ipParts[0] == 127) continue;
                                                    if (ipParts[0] == 0 && ipParts[1] == 0 && ipParts[2] == 0 && ipParts[3] == 0) continue;
                                                    if (ipParts[0] == 255 && ipParts[1] == 255 && ipParts[2] == 255 && ipParts[3] == 255) continue;
                                                    if (ipParts[0] == 169 && ipParts[1] == 254) continue;
                                                    if (!int.TryParse(port, out int portNum) || portNum < 1 || portNum > 65535)
                                                        continue;
                                                    string fullIp = ip + ":" + port;
                                                    if (!ips.Contains(fullIp))
                                                        ips.Add(fullIp);
                                                }
                                            }
                                            catch { }

                                            if (ips.Count > 0)
                                            {
                                                string ipFile = System.IO.Path.Combine(Application.StartupPath, "extracted_ips.txt");
                                                using (var sw = new System.IO.StreamWriter(ipFile, true, Encoding.UTF8))
                                                {
                                                    var sourceProp = webView2Type.GetProperty("Source");
                                                    string currentSrc = sourceProp?.GetValue(webView2)?.ToString() ?? "";
                                                    sw.WriteLine($"# 提取时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} 来源: {currentSrc} 共{ips.Count}条");
                                                    foreach (var ip in ips)
                                                    {
                                                        sw.WriteLine(ip);
                                                    }
                                                }

                                                DateTime extractTime = DateTime.Now;
                                                string ruleName = cboSearchRule.SelectedItem?.ToString() ?? "智慧桌面";
                                                int addedCount = 0;
                                                hasSearchPlatformData = true;

                                                if (!autoParseLink)
                                                {
                                                    foreach (var ipPort in ips)
                                                    {
                                                        var parts = ipPort.Split(':');
                                                        if (parts.Length != 2) continue;

                                                        string ip = parts[0];
                                                        string port = parts[1];
                                                        string rootHttp = $"http://{ip}:{port}";

                                                        if (ruleName == "智慧光迅")
                                                        {
                                                            string url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                                                            bool exists = allChannels.Any(c => c.Url == url);
                                                            if (!exists)
                                                            {
                                                                allChannels.Add(new ChannelInfo { Name = ipPort, Url = url, Group = "解析待处理", Status = "待解析", ParseDateTime = extractTime });
                                                                addedCount++;
                                                            }
                                                        }
                                                        else if (ruleName == "华视美达")
                                                        {
                                                            var scanConfig = await ShowScanConfigDialogAsync();
                                                            if (scanConfig == null) continue;
                                                            int scanCount = scanConfig.Item1;
                                                            int threadCount = scanConfig.Item2;

                                                            this.Invoke(new Action(() => { this.Show(); this.Activate(); }));

                                                            if (lblProgressText != null && lblProgressText.IsHandleCreated)
                                                            {
                                                                lblProgressText.Invoke(new Action(() => { lblProgressText.Text = $"华视美达扫描进度:"; lblProgressText.Refresh(); }));
                                                            }
                                                            if (lblPercent != null && lblPercent.IsHandleCreated)
                                                            {
                                                                lblPercent.Invoke(new Action(() => { lblPercent.Text = "0%"; lblPercent.Refresh(); }));
                                                            }
                                                            if (statusBarRef != null && statusBarRef.IsHandleCreated)
                                                                statusBarRef.Invoke(new Action(() => { LayoutStatusBar(statusBarRef); }));

                                                            dlg.Invoke(new Action(() => { dlg.Hide(); }));

                                                            var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<string, string>>();
                                                            var cidList = Enumerable.Range(1, scanCount).ToList();
                                                            int processedCount = 0;

                                                            await Task.Run(() =>
                                                            {
                                                                using (var httpClient = new System.Net.Http.HttpClient())
                                                                {
                                                                    httpClient.Timeout = TimeSpan.FromSeconds(2.5);
                                                                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                                                                    Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, cid =>
                                                                    {
                                                                        string url = $"{rootHttp}/newlive/live/hls/{cid}/live.m3u8";
                                                                        try
                                                                        {
                                                                            var headTask = httpClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                                                                            var headResp = headTask.Result;
                                                                            if (headResp.IsSuccessStatusCode)
                                                                            {
                                                                                var getTask = httpClient.GetAsync(url);
                                                                                var getResp = getTask.Result;
                                                                                if (getResp.IsSuccessStatusCode)
                                                                                {
                                                                                    var contentTask = getResp.Content.ReadAsStringAsync();
                                                                                    string content = contentTask.Result;
                                                                                    if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                                                    {
                                                                                        validResults.Add(Tuple.Create(url, content));
                                                                                    }
                                                                                }
                                                                                return;
                                                                            }
                                                                        }
                                                                        catch { }

                                                                        try
                                                                        {
                                                                            var getTask = httpClient.GetAsync(url);
                                                                            var getResp = getTask.Result;
                                                                            if (getResp.IsSuccessStatusCode)
                                                                            {
                                                                                var contentTask = getResp.Content.ReadAsStringAsync();
                                                                                string content = contentTask.Result;
                                                                                if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                                                {
                                                                                    validResults.Add(Tuple.Create(url, content));
                                                                                }
                                                                            }
                                                                        }
                                                                        catch { }

                                                                        int current = System.Threading.Interlocked.Increment(ref processedCount);
                                                                        int pct = (int)(current * 100.0 / scanCount);
                                                                        if (lblPercent != null && !lblPercent.IsDisposed)
                                                                        {
                                                                            try
                                                                            {
                                                                            lblPercent.Invoke(new Action(() =>
                                                                            {
                                                                                if (lblPercent != null && !lblPercent.IsDisposed)
                                                                                    lblPercent.Text = $"{pct}%";
                                                                                if (statusBarRef != null && !statusBarRef.IsDisposed)
                                                                                {
                                                                                    progressBarWidth = (int)(statusBarRef.ClientSize.Width * pct / 100);
                                                                                    if (progressBarWidth > 0)
                                                                                        UpdateLabelColorsBasedOnProgress();
                                                                                    else
                                                                                        RestoreLabelColors();
                                                                                    statusBarRef.Refresh();
                                                                                }
                                                                            }));
                                                                        }
                                                                        catch { }
                                                                    }
                                                                });
                                                            }
                                                            });

                                                            if (lblProgressText != null && !lblProgressText.IsDisposed)
                                                            {
                                                                lblProgressText.Invoke(new Action(() => { lblProgressText.Text = $"华视美达扫描完成:"; }));
                                                            }
                                                            if (lblPercent != null && !lblPercent.IsDisposed)
                                                            {
                                                                lblPercent.Invoke(new Action(() => { lblPercent.Text = $"找到{validResults.Count}个"; }));
                                                            }
                                                            if (statusBarRef != null)
                                                                statusBarRef.Invoke(new Action(() => { LayoutStatusBar(statusBarRef); }));

                                                            foreach (var result in validResults)
                                                            {
                                                                bool exists = allChannels.Any(c => c.Url == result.Item1);
                                                                if (!exists)
                                                                {
                                                                    string[] urlParts = result.Item1.Split('/');
                                                                    string cid = urlParts.Length > 1 ? urlParts[urlParts.Length - 2] : "";
                                                                    allChannels.Add(new ChannelInfo { Name = $"{ipPort}_CID{cid}", Url = result.Item1, Group = "解析待处理", Status = "待解析", ParseDateTime = extractTime });
                                                                    addedCount++;
                                                                }
                                                            }

                                                            if (lblProgressText != null && !lblProgressText.IsDisposed)
                                                            {
                                                                lblProgressText.Invoke(new Action(() => { lblProgressText.Text = "检测进度:"; }));
                                                            }
                                                            if (lblPercent != null && !lblPercent.IsDisposed)
                                                            {
                                                                lblPercent.Invoke(new Action(() => { lblPercent.Text = "0%"; }));
                                                            }
                                                            if (statusBarRef != null)
                                                                statusBarRef.Invoke(new Action(() => { LayoutStatusBar(statusBarRef); }));
                                                        }
                                                        else
                                                        {
                                                            string url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                                                            bool exists = allChannels.Any(c => c.Url == url);
                                                            if (!exists)
                                                            {
                                                                allChannels.Add(new ChannelInfo { Name = ipPort, Url = url, Group = "解析待处理", Status = "待解析", ParseDateTime = extractTime });
                                                                addedCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    using (var httpClient = new System.Net.Http.HttpClient())
                                                    {
                                                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                                                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                                                        foreach (var ipPort in ips)
                                                        {
                                                            var parts = ipPort.Split(':');
                                                            if (parts.Length != 2) continue;

                                                            string ip = parts[0];
                                                            string port = parts[1];
                                                            string rootHttp = $"http://{ip}:{port}";

                                                            if (ruleName == "智慧光迅")
                                                            {
                                                                string url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                                                                try
                                                                {
                                                                    var resp = await httpClient.GetAsync(url);
                                                                    if (resp.IsSuccessStatusCode)
                                                                    {
                                                                        string content = await resp.Content.ReadAsStringAsync();
                                                                        if (!string.IsNullOrEmpty(content))
                                                                        {
                                                                            ParseZhgxTv(content, url, extractTime);
                                                                            addedCount++;
                                                                        }
                                                                    }
                                                                }
                                                                catch { }
                                                            }
                                                            else if (ruleName == "华视美达")
                                                            {
                                                                var scanConfig = await ShowScanConfigDialogAsync();
                                                                if (scanConfig == null) continue;
                                                                int scanCount = scanConfig.Item1;
                                                                int threadCount = scanConfig.Item2;

                                                                var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<string, string>>();
                                                                var cidList = Enumerable.Range(1, scanCount).ToList();

                                                                await Task.Run(() =>
                                                                {
                                                                    Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, cid =>
                                                                    {
                                                                        string url = $"{rootHttp}/newlive/live/hls/{cid}/live.m3u8";
                                                                        try
                                                                        {
                                                                            var headTask = httpClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                                                                            var headResp = headTask.Result;
                                                                            if (headResp.IsSuccessStatusCode)
                                                                            {
                                                                                var getTask = httpClient.GetAsync(url);
                                                                                var getResp = getTask.Result;
                                                                                if (getResp.IsSuccessStatusCode)
                                                                                {
                                                                                    var contentTask = getResp.Content.ReadAsStringAsync();
                                                                                    string content = contentTask.Result;
                                                                                    if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                                                    {
                                                                                        validResults.Add(Tuple.Create(url, content));
                                                                                    }
                                                                                }
                                                                                return;
                                                                            }
                                                                        }
                                                                        catch { }

                                                                        try
                                                                        {
                                                                            var getTask = httpClient.GetAsync(url);
                                                                            var getResp = getTask.Result;
                                                                            if (getResp.IsSuccessStatusCode)
                                                                            {
                                                                                var contentTask = getResp.Content.ReadAsStringAsync();
                                                                                string content = contentTask.Result;
                                                                                if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                                                {
                                                                                    validResults.Add(Tuple.Create(url, content));
                                                                                }
                                                                            }
                                                                        }
                                                                        catch { }
                                                                    });
                                                                });

                                                                foreach (var result in validResults)
                                                                {
                                                                    bool exists = allChannels.Any(c => c.Url == result.Item1);
                                                                    if (!exists)
                                                                    {
                                                                        string[] urlParts = result.Item1.Split('/');
                                                                        string cid = urlParts.Length > 1 ? urlParts[urlParts.Length - 2] : "";
                                                                        allChannels.Add(new ChannelInfo { Name = $"{ipPort}_CID{cid}", Url = result.Item1, Group = "解析待处理", Status = "待解析", ParseDateTime = extractTime });
                                                                        addedCount++;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                string url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                                                                try
                                                                {
                                                                    var resp = await httpClient.GetAsync(url);
                                                                    if (resp.IsSuccessStatusCode)
                                                                    {
                                                                        string content = await resp.Content.ReadAsStringAsync();
                                                                        if (!string.IsNullOrEmpty(content))
                                                                        {
                                                                            ParseKutvJson(content, url, extractTime);
                                                                            addedCount++;
                                                                        }
                                                                    }
                                                                }
                                                                catch { }
                                                            }
                                                        }
                                                    }
                                                }

                                                totalCount = allChannels.Count;
                                                RefreshGrid();
                                                UpdateEmptyState();
                                                UpdateActionButtonsVisibility();
                                                SaveChannelList();
                                                this.Show();
                                                dlg.Close();

                                                // 更新状态栏显示解析结果
                                                if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
                                                {
                                                    lblDetected.Text = $"已检测: 0/{totalCount}";
                                                    lblAvailable.Text = $"可用: 0";
                                                    lblPercent.Text = "0.00%";
                                                    progressBarWidth = 0;
                                                    RestoreLabelColors();
                                                    statusBarRef.PerformLayout();
                                                    LayoutStatusBar(statusBarRef);
                                                    statusBarRef.Refresh();
                                                }

                                                if (!autoParseLink)
                                                {
                                                    DarkMessageBox.Show($"已提取 {addedCount} 条链接到待解析列表\n请点击\"解析链接\"按钮进行解析", "提取完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                                else
                                                {
                                                    DarkMessageBox.Show($"解析完成！\n成功: {addedCount} 个IP\n请点击\"开始检测\"按钮验证链接有效性", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                            }
                                            else
                                            {
                                                DarkMessageBox.Show($"未在当前页面找到IP地址\n调试信息:\n文本长度: {testResult}\n包含点号: {htmlResult}\nJS返回: {ipResult?.Substring(0, Math.Min(ipResult.Length, 200))}", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                        }
                                        else
                                        {
                                            DarkMessageBox.Show($"JS执行返回为空\n调试信息:\n文本长度: {testResult}\n包含点号: {htmlResult}", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }
                                    else
                                    {
                                        DarkMessageBox.Show("ExecuteScriptAsync方法未找到", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                                else
                                {
                                    DarkMessageBox.Show("CoreWebView2为空", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                            else
                            {
                                DarkMessageBox.Show("CoreWebView2属性未找到", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            DarkMessageBox.Show("IP提取失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            btnExtractIp.Text = autoExtractIpPort ? "IP提取: 开" : "IP提取: 关";
                            btnExtractIp.Enabled = true;
                        }
                    }
                };
                navPanel.Controls.Add(btnExtractIp);

                navPanel.Paint += (s, e) =>
                {
                    Panel p = (Panel)s;
                    using (Pen pen = new Pen(BorderColor))
                        e.Graphics.DrawLine(pen, 0, NAV_BAR_H - 1, p.Width, NAV_BAR_H - 1);
                };

                Action UpdateWebViewSize = () =>
                {
                    if (webView2 != null && webView2Type != null)
                    {
                        webView2Type.GetProperty("Location")?.SetValue(webView2, new Point(BORDER_WIDTH, isNavVisible ? NAV_BAR_H : 0));
                        webView2Type.GetProperty("Size")?.SetValue(webView2, new Size(dlg.ClientSize.Width - BORDER_WIDTH * 2, dlg.ClientSize.Height - (isNavVisible ? NAV_BAR_H : 0) - STATUS_BAR_H));
                    }
                };

                dlg.SizeChanged += (s, e) =>
                {
                    navPanel.Width = dlg.ClientSize.Width;
                    UpdateWebViewSize();
                    lblStatusIp.Location = new Point(statusBar.Width - 130, 0);
                    lblStatusEngine.Location = new Point(statusBar.Width - 280, 0);
                    leftBorder.Invalidate();
                    rightBorder.Invalidate();
                    statusBar.Invalidate();
                    navPanel.Invalidate();
                };

                navTimer.Tick += (s, e) => UpdateWebViewSize();

                var searchEngines = new Dictionary<string, string>
                {
                    {"FOFA", "https://fofa.info/"},
                    {"Quake", "https://quake.360.net/"},
                    {"Hunter", "https://hunter.qianxin.com/"},
                    {"ZoomEye", "https://www.zoomeye.org/"},
                    {"Shodan", "https://www.shodan.io/"},
                    {"Censys", "https://search.censys.io/"}
                };

                string currentUrl = searchEngines["FOFA"];
                txtUrl.Text = currentUrl;
                webViewPendingUrl = currentUrl;
                webViewNavPanel = navPanel;
                webViewCboEngine = cboEngine;
                webViewTxtUrl = txtUrl;

                if (webView2 != null && webView2Type != null)
                {
                    var initCompletedEvent = webView2Type.GetEvent("CoreWebView2InitializationCompleted");
                    if (initCompletedEvent != null)
                    {
                        var handlerType = initCompletedEvent.EventHandlerType;
                        Delegate handler = Delegate.CreateDelegate(handlerType, this, typeof(IPTVLiveCheckerMain).GetMethod("WebView2InitCompletedHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                        initCompletedEvent.AddEventHandler(webView2, handler);
                    }

                    var navCompletedEvent = webView2Type.GetEvent("NavigationCompleted");
                    if (navCompletedEvent != null)
                    {
                        var handlerType = navCompletedEvent.EventHandlerType;
                        Delegate handler = Delegate.CreateDelegate(handlerType, this, typeof(IPTVLiveCheckerMain).GetMethod("WebView2NavCompletedHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                        navCompletedEvent.AddEventHandler(webView2, handler);
                    }

                    // WebMessageReceived事件 - 接收JS发送的登录信息
                    var msgEvent = webView2Type.GetEvent("WebMessageReceived");
                    if (msgEvent != null)
                    {
                        var handlerType = msgEvent.EventHandlerType;
                        Delegate handler = Delegate.CreateDelegate(handlerType, this, typeof(IPTVLiveCheckerMain).GetMethod("WebView2WebMessageReceivedHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                        msgEvent.AddEventHandler(webView2, handler);
                    }
                }

                dlg.Load += async (s, e) =>
                {
                    try
                    {
                        if (webView2 != null && webView2Type != null)
                        {
                            UpdateWebViewSize();
                            var coreProp = webView2Type.GetProperty("CoreWebView2");
                            var core = coreProp?.GetValue(webView2);
                            if (core == null)
                            {
                                var initMethod = webView2Type.GetMethod("EnsureCoreWebView2Async", Type.EmptyTypes);
                                if (initMethod != null)
                                {
                                    var initTask = (System.Threading.Tasks.Task)initMethod.Invoke(webView2, null);
                                    await initTask;
                                }
                            }
                            if (!string.IsNullOrEmpty(webViewPendingUrl))
                            {
                                webView2Type.GetProperty("Source")?.SetValue(webView2, new Uri(webViewPendingUrl));
                                webViewPendingUrl = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DarkMessageBox.Show("WebView2加载失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                Action DoSearch = () =>
                {
                    try
                    {
                        string engineName = cboEngine.SelectedItem?.ToString() ?? "FOFA";
                        if (searchEngines.TryGetValue(engineName, out string baseUrl))
                        {
                            txtUrl.Text = baseUrl;
                            if (webView2 != null && webView2Type != null)
                            {
                                var srcProp = webView2Type.GetProperty("Source");
                                if (srcProp != null)
                                {
                                    srcProp.SetValue(webView2, new Uri(baseUrl));
                                }
                                else
                                {
                                    webViewPendingUrl = baseUrl;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DarkMessageBox.Show("搜索失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                cboEngine.SelectedIndexChanged += (s, e) =>
                {
                    lblStatusEngine.Text = "搜索引擎: " + cboEngine.SelectedItem?.ToString();
                    DoSearch();
                };

                cboSearchRule.SelectedIndexChanged += async (s, e) =>
                {
                    try
                    {
                        string ruleName = cboSearchRule.SelectedItem?.ToString();
                        if (!string.IsNullOrEmpty(ruleName) && searchRules.TryGetValue(ruleName, out string searchRule))
                        {
                            if (webView2 != null && webView2Type != null)
                            {
                                string escapedRule = searchRule.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");
                                string js = 
                                    "let targetInput = null; " +
                                    "let allInputs = document.querySelectorAll('input[type=\"text\"], input[type=\"search\"], textarea'); " +
                                    
                                    "allInputs.forEach(function(input) { input.value = ''; input.dispatchEvent(new Event('input')); input.dispatchEvent(new Event('change')); }); " +
                                    
                                    "let searchSelectors = [ " +
                                    "  'input[placeholder*=\"搜索\"]', " +
                                    "  'input[placeholder*=\"Search\"]', " +
                                    "  'input[placeholder*=\"search\"]', " +
                                    "  'input[placeholder*=\"query\"]', " +
                                    "  'input[placeholder*=\"Query\"]', " +
                                    "  'input[id*=\"search\"]', " +
                                    "  'input[id*=\"Search\"]', " +
                                    "  'input[name*=\"search\"]', " +
                                    "  'input[name*=\"q\"]', " +
                                    "  'input[class*=\"search\"]', " +
                                    "  'input[class*=\"Search\"]' " +
                                    "]; " +
                                    
                                    "for(let i=0; i<searchSelectors.length; i++) { " +
                                    "  let el = document.querySelector(searchSelectors[i]); " +
                                    "  if(el && el.offsetParent !== null) { targetInput = el; break; } " +
                                    "} " +
                                    
                                    "if(!targetInput && allInputs.length > 0) { " +
                                    "  targetInput = allInputs[0]; " +
                                    "} " +
                                    
                                    "if(targetInput) { " +
                                    "  targetInput.focus(); " +
                                    "  targetInput.value = '" + escapedRule + "'; " +
                                    "  targetInput.dispatchEvent(new Event('input', { bubbles: true })); " +
                                    "  targetInput.dispatchEvent(new Event('change', { bubbles: true })); " +
                                    "  targetInput.dispatchEvent(new Event('keyup', { bubbles: true })); " +
                                    "  targetInput.dispatchEvent(new Event('keydown', { bubbles: true })); " +
                                    "}";
                                var coreProp = webView2Type.GetProperty("CoreWebView2");
                                if (coreProp != null)
                                {
                                    var core = coreProp.GetValue(webView2);
                                    if (core != null)
                                    {
                                        var execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new[] { typeof(string) });
                                        if (execMethod != null)
                                        {
                                            var task = (System.Threading.Tasks.Task)execMethod.Invoke(core, new object[] { js });
                                            await task;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DarkMessageBox.Show("自动搜索失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                this.Show();
                string errorDetails = $"WebView2窗口初始化失败，将使用浏览器模式打开。\n\n错误信息: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}\n\n内部异常: {ex.InnerException?.Message ?? "无"}";
                DarkMessageBox.Show(errorDetails, "WebView2初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowBrowserSearchDialog();
            }
        }

        private string GetSearchUrl(string baseUrl, string query)
        {
            string encodedQuery = Uri.EscapeDataString(query);
            if (baseUrl.Contains("fofa.info"))
                return $"https://fofa.info/result?qbase64={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(query))}";
            if (baseUrl.Contains("quake.360.net"))
                return $"https://quake.360.net/search?keyword={encodedQuery}";
            if (baseUrl.Contains("hunter.qianxin.com"))
                return $"https://hunter.qianxin.com/search?q={encodedQuery}";
            if (baseUrl.Contains("zoomeye.org"))
                return $"https://www.zoomeye.org/search?q={encodedQuery}";
            if (baseUrl.Contains("shodan.io"))
                return $"https://www.shodan.io/search?query={encodedQuery}";
            if (baseUrl.Contains("censys.io"))
                return $"https://search.censys.io/search?q={encodedQuery}";
            return baseUrl;
        }

        private void AdjustToolbarColors(Panel navPanel, ComboBox cboEngine, TextBox txtUrl, Color pageBg)
        {
            try
            {
                double brightness = (pageBg.R * 0.299 + pageBg.G * 0.587 + pageBg.B * 0.114) / 255;
                bool isDarkPage = brightness < 0.5;

                Color toolbarBg = Color.FromArgb(240, pageBg.R, pageBg.G, pageBg.B);
                Color textColor = isDarkPage ? Color.White : Color.FromArgb(51, 51, 51);
                Color inputBg = isDarkPage ? Color.FromArgb(30, 30, 45) : Color.White;

                navPanel.BackColor = toolbarBg;

                cboEngine.ForeColor = textColor;
                cboEngine.BackColor = inputBg;

                txtUrl.ForeColor = isDarkPage ? Color.FromArgb(180, 180, 200) : Color.FromArgb(100, 100, 100);
                txtUrl.BackColor = inputBg;

                navPanel.Invalidate();
            }
            catch { }
        }

        private async void LoadAndParseIpPorts()
        {
            string ipFile = System.IO.Path.Combine(Application.StartupPath, "extracted_ips.txt");
            if (!System.IO.File.Exists(ipFile))
            {
                DarkMessageBox.Show("未找到提取的IP文件: extracted_ips.txt\n请先使用搜索平台提取IP+端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var platforms = new List<string> { "智慧光迅", "华视美达", "智慧桌面" };
            
            using (var dlg = new Form())
            {
                bool isDarkPlat = IsDarkColor(theme.Bg);
                dlg.Text = "选择平台规则";
                dlg.Size = new Size(680, 260);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = isDarkPlat ? Color.FromArgb(45, 45, 55) : Color.White;
                dlg.ForeColor = isDarkPlat ? Color.FromArgb(220, 220, 230) : Color.Black;
                SetFormDarkModeTitleBar(dlg, isDarkPlat);
                CenterForm(dlg, this);

                ListBox lstPlatforms = new ListBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(615, 120),
                    Font = GetFont(11),
                    SelectionMode = SelectionMode.One,
                    BackColor = isDarkPlat ? Color.FromArgb(30, 30, 38) : Color.White,
                    ForeColor = isDarkPlat ? Color.FromArgb(220, 220, 230) : Color.Black
                };
                lstPlatforms.Items.AddRange(new object[] {
                    "智慧光迅 - /ZHGXTV/Public/json/live_interface.txt",
                    "华视美达 - /newlive/live/hls/{cid}/live.m3u8",
                    "智慧桌面 - /iptv/live/1000.json?key=txiptv"
                });
                lstPlatforms.SelectedIndex = 0;
                dlg.Controls.Add(lstPlatforms);

                Button btnOK = new Button { Text = "确定", Location = new Point(100, 140), Size = new Size(100, 38), Font = GetFont(11), BackColor = isDarkPlat ? Color.FromArgb(55, 55, 70) : Color.FromArgb(200, 200, 210), ForeColor = isDarkPlat ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
                btnOK.FlatAppearance.BorderColor = isDarkPlat ? Color.FromArgb(80, 80, 100) : Color.Gray;
                btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 100, 38), 6));
                btnOK.Click += (s, e) => dlg.DialogResult = DialogResult.OK;
                dlg.Controls.Add(btnOK);

                Button btnCancel = new Button { Text = "取消", Location = new Point(420, 140), Size = new Size(100, 38), Font = GetFont(11), BackColor = isDarkPlat ? Color.FromArgb(55, 55, 70) : Color.FromArgb(200, 200, 210), ForeColor = isDarkPlat ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 } };
                btnCancel.FlatAppearance.BorderColor = isDarkPlat ? Color.FromArgb(80, 80, 100) : Color.Gray;
                btnCancel.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 100, 38), 6));
                btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(btnCancel);

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                string selectedText = lstPlatforms.SelectedItem.ToString();
                string ruleName = selectedText.Split('-')[0].Trim();

                SelectNavItem("检测");

                string[] lines = System.IO.File.ReadAllLines(ipFile, Encoding.UTF8);
                var ipList = new List<string>();
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;
                    if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?$"))
                        ipList.Add(trimmed);
                }

                if (ipList.Count == 0)
                {
                    DarkMessageBox.Show("未找到有效的IP地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                hasSearchPlatformData = true;
                int addedCount = 0;
                DateTime parseTime = DateTime.Now;

                if (ruleName == "智慧光迅")
                {
                    foreach (string ipPort in ipList)
                    {
                        var parts = ipPort.Split(':');
                        if (parts.Length != 2) continue;

                        string ip = parts[0];
                        string port = parts[1];
                        string rootHttp = $"http://{ip}:{port}";
                        string url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                        bool exists = allChannels.Any(c => c.Url == url);
                        if (!exists)
                        {
                            allChannels.Add(new ChannelInfo { Name = $"智慧光迅_{ipPort}", Url = url, Group = "智慧光迅解析", Status = "待解析", ParseDateTime = parseTime });
                            addedCount++;
                        }
                    }
                }
                else if (ruleName == "华视美达")
                {
                    var scanConfig = await ShowScanConfigDialogAsync();
                    if (scanConfig == null) return;
                    int scanCount = scanConfig.Item1;
                    int threadCount = scanConfig.Item2;

                    foreach (string ipPort in ipList)
                    {
                        var parts = ipPort.Split(':');
                        if (parts.Length != 2) continue;

                        string ip = parts[0];
                        string port = parts[1];
                        string rootHttp = $"http://{ip}:{port}";

                        if (lblProgressText != null)
                            {
                                lblProgressText.Text = $"华视美达扫描进度:";
                                lblProgressText.Refresh();
                            }
                        if (lblPercent != null)
                            {
                                lblPercent.Text = "0%";
                                lblPercent.Refresh();
                            }
                        if (statusBarRef != null)
                            LayoutStatusBar(statusBarRef);
                        this.Refresh();

                        var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<string, string>>();
                        var cidList = Enumerable.Range(1, scanCount).ToList();
                        int processedCount = 0;

                        await Task.Run(() =>
                        {
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                httpClient.Timeout = TimeSpan.FromSeconds(2.5);
                                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                                Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, cid =>
                                {
                                    string url = $"{rootHttp}/newlive/live/hls/{cid}/live.m3u8";
                                    try
                                    {
                                        var headTask = httpClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                                        var headResp = headTask.Result;
                                        if (headResp.IsSuccessStatusCode)
                                        {
                                            var getTask = httpClient.GetAsync(url);
                                            var getResp = getTask.Result;
                                            if (getResp.IsSuccessStatusCode)
                                            {
                                                var contentTask = getResp.Content.ReadAsStringAsync();
                                                string content = contentTask.Result;
                                                if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                                {
                                                    validResults.Add(Tuple.Create(url, content));
                                                }
                                            }
                                            return;
                                        }
                                    }
                                    catch { }

                                    try
                                    {
                                        var getTask = httpClient.GetAsync(url);
                                        var getResp = getTask.Result;
                                        if (getResp.IsSuccessStatusCode)
                                        {
                                            var contentTask = getResp.Content.ReadAsStringAsync();
                                            string content = contentTask.Result;
                                            if (!string.IsNullOrEmpty(content) && content.Contains("#EXTM3U"))
                                            {
                                                validResults.Add(Tuple.Create(url, content));
                                            }
                                        }
                                    }
                                    catch { }

                                    int current = System.Threading.Interlocked.Increment(ref processedCount);
                                    int pct = (int)(current * 100.0 / scanCount);
                                    if (lblPercent != null && !lblPercent.IsDisposed)
                                    {
                                        try
                                        {
                                            lblPercent.Invoke(new Action(() =>
                                            {
                                                if (lblPercent != null && !lblPercent.IsDisposed)
                                                    lblPercent.Text = $"{pct}%";
                                                if (statusBarRef != null && !statusBarRef.IsDisposed)
                                                {
                                                    progressBarWidth = (int)(statusBarRef.ClientSize.Width * pct / 100);
                                                    if (progressBarWidth > 0)
                                                        UpdateLabelColorsBasedOnProgress();
                                                    else
                                                        RestoreLabelColors();
                                                    statusBarRef.Refresh();
                                                }
                                            }));
                                        }
                                        catch { }
                                    }
                                });
                            }
                        });

                        if (lblProgressText != null && !lblProgressText.IsDisposed)
                            lblProgressText.Text = $"华视美达扫描完成:";
                        if (lblPercent != null && !lblPercent.IsDisposed)
                            lblPercent.Text = $"找到{validResults.Count}个";
                        if (statusBarRef != null)
                            LayoutStatusBar(statusBarRef);
                        this.Refresh();

                        foreach (var result in validResults)
                        {
                            bool exists = allChannels.Any(c => c.Url == result.Item1);
                            if (!exists)
                            {
                                string[] urlParts = result.Item1.Split('/');
                                string cid = urlParts.Length > 1 ? urlParts[urlParts.Length - 2] : "";
                                allChannels.Add(new ChannelInfo { Name = $"华视美达_{ipPort}_CID{cid}", Url = result.Item1, Group = "华视美达解析", Status = "待解析", ParseDateTime = parseTime });
                                addedCount++;
                            }
                        }

                        if (lblProgressText != null && !lblProgressText.IsDisposed)
                            lblProgressText.Text = "检测进度:";
                        if (lblPercent != null && !lblPercent.IsDisposed)
                            lblPercent.Text = "0%";
                        if (statusBarRef != null)
                            LayoutStatusBar(statusBarRef);
                    }
                }
                else
                {
                    foreach (string ipPort in ipList)
                    {
                        var parts = ipPort.Split(':');
                        if (parts.Length != 2) continue;

                        string ip = parts[0];
                        string port = parts[1];
                        string rootHttp = $"http://{ip}:{port}";
                        string url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                        bool exists = allChannels.Any(c => c.Url == url);
                        if (!exists)
                        {
                            allChannels.Add(new ChannelInfo { Name = $"智慧桌面_{ipPort}", Url = url, Group = "智慧桌面解析", Status = "待解析", ParseDateTime = parseTime });
                            addedCount++;
                        }
                    }
                }

                RefreshGrid();
                UpdateStatusBar();

                if (!autoParseLink)
                {
                    if (btnParseLink != null)
                        btnParseLink.Visible = true;
                }

                DarkMessageBox.Show($"已添加 {addedCount} 条待解析链接\n分组: {ruleName}解析", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowIptvParserDialog()
        {
            bool isDark = theme.Name == "深色";
            Color bgColor = isDark ? Color.FromArgb(35, 35, 45) : Color.White;
            Color panelBg = isDark ? Color.FromArgb(45, 45, 55) : Color.FromArgb(248, 249, 252);
            Color textColor = isDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 55, 65);
            Color labelColor = isDark ? Color.FromArgb(180, 180, 190) : Color.FromArgb(100, 105, 115);
            Color borderColor = isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(220, 220, 228);
            Color btnColor = Color.FromArgb(138, 43, 226);
            Color btnHover = Color.FromArgb(158, 63, 246);
            Color btnText = Color.White;
            Color successColor = Color.FromArgb(76, 175, 80);
            Color errorColor = Color.FromArgb(244, 67, 54);

            using (Form dlg = new Form())
            {
                dlg.Text = "直播源解析";
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = bgColor;
                SetFormDarkModeTitleBar(dlg, isDark);

                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int formWidth = (int)(screen.Width * 0.9);
                int formHeight = (int)(screen.Height * 0.85);
                formWidth = Math.Max(formWidth, 900);
                formHeight = Math.Max(formHeight, 650);
                dlg.Size = new Size(formWidth, formHeight);

                CenterForm(dlg, this);

                int padding = SX(12);
                    int labelWidth = SX(80);
                    int inputHeight = SY(30);
                    int btnHeight = SY(34);
                    int btnWidth = SX(110);
                    int leftPanelWidth = (int)(dlg.ClientSize.Width * 0.28);
                    leftPanelWidth = Math.Max(leftPanelWidth, SX(300));
                    leftPanelWidth = Math.Min(leftPanelWidth, SX(380));

                    Panel leftPanel = new Panel
                    {
                        Location = new Point(padding, padding),
                        Size = new Size(leftPanelWidth, dlg.ClientSize.Height - padding * 2),
                        BackColor = Color.Transparent
                    };
                    leftPanel.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, leftPanel.Width - 1, leftPanel.Height - 1), 12))
                        {
                        using (SolidBrush bgBrush = new SolidBrush(panelBg))
                            e.Graphics.FillPath(bgBrush, path);
                        using (Pen borderPen = new Pen(borderColor, 1))
                            e.Graphics.DrawPath(borderPen, path);
                    }
                };
                dlg.Controls.Add(leftPanel);

                Panel rightPanel = new Panel
                    {
                        Location = new Point(padding + leftPanelWidth + padding, padding),
                        Size = new Size(dlg.ClientSize.Width - padding * 3 - leftPanelWidth, dlg.ClientSize.Height - padding * 2),
                        BackColor = Color.Transparent
                    };
                    rightPanel.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, rightPanel.Width - 1, rightPanel.Height - 1), 12))
                        {
                        using (SolidBrush bgBrush = new SolidBrush(panelBg))
                            e.Graphics.FillPath(bgBrush, path);
                        using (Pen borderPen = new Pen(borderColor, 1))
                            e.Graphics.DrawPath(borderPen, path);
                    }
                };
                dlg.Controls.Add(rightPanel);

                    Action<Button, Color> ApplyRoundButton = (btn, bg) =>
                    {
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderSize = 0;
                        btn.UseVisualStyleBackColor = false;
                        btn.BackColor = bg;
                        btn.Region?.Dispose();
                        using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 6))
                            btn.Region = new Region(path);
                        btn.Resize += (s, e) =>
                        {
                            btn.Region?.Dispose();
                            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 6))
                                btn.Region = new Region(path);
                        };
                        btn.MouseEnter += (s, e) =>
                        {
                            btn.BackColor = LightenColor(bg, 20);
                        };
                        btn.MouseLeave += (s, e) =>
                        {
                            btn.BackColor = bg;
                        };
                        btn.MouseDown += (s, e) =>
                        {
                            btn.BackColor = LightenColor(bg, 40);
                            btn.Location = new Point(btn.Location.X + 1, btn.Location.Y + 1);
                        };
                        btn.MouseUp += (s, e) =>
                        {
                            btn.BackColor = LightenColor(bg, 20);
                            btn.Location = new Point(btn.Location.X - 1, btn.Location.Y - 1);
                        };
                    };

                int y = SX(16);

                Label lblTitle = new Label
                {
                    Text = "解析配置",
                    Font = GetFont(SF(10.5f), FontStyle.Bold),
                    ForeColor = textColor,
                    Location = new Point(SX(16), y-SX(6)),
                    AutoSize = true
                };
                leftPanel.Controls.Add(lblTitle);
                y += SY(36);

                Label lblPlatform = new Label
                {
                    Text = "平台选择",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), y),
                    Size = new Size(labelWidth, inputHeight)
                };
                leftPanel.Controls.Add(lblPlatform);

                ComboBox cboPlatform = new ComboBox
                {
                    Location = new Point(labelWidth + SX(16), y),
                    Size = new Size(SX(270), inputHeight),
                    Font = GetFont(SF(8.5f)),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = textColor
                };
                cboPlatform.Items.AddRange(new object[] {
                    "1. 智慧光迅 ZHGXTV",
                    "2. 华视美达 频道扫描",
                    "3. 智能KUTV JSON接口"
                });
                cboPlatform.SelectedIndex = 0;
                leftPanel.Controls.Add(cboPlatform);
                y += SY(40);

                Label lblIpPort = new Label
                {
                    Text = "IP端口",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), y),
                    Size = new Size(labelWidth, inputHeight)
                };
                leftPanel.Controls.Add(lblIpPort);

                TextBox txtIpPort = new TextBox
                {
                    Location = new Point(labelWidth + SX(16), y),
                    Size = new Size(SX(270), inputHeight),
                    Font = GetFont(SF(8.5f)),
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = isDark ? Color.FromArgb(120, 120, 130) : Color.FromArgb(150, 150, 160),
                    Text = "示例：110.72.103.69:8181"
                };
                txtIpPort.GotFocus += (s, e) =>
                {
                    if (txtIpPort.Text == "示例：110.72.103.69:8181")
                    {
                        txtIpPort.Text = "";
                        txtIpPort.ForeColor = textColor;
                    }
                };
                txtIpPort.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtIpPort.Text))
                    {
                        txtIpPort.Text = "示例：110.72.103.69:8181";
                        txtIpPort.ForeColor = isDark ? Color.FromArgb(120, 120, 130) : Color.FromArgb(150, 150, 160);
                    }
                };
                leftPanel.Controls.Add(txtIpPort);
                y += SY(40);

                Button btnAutoBuild = new Button
                {
                    Text = "自动拼接完整URL",
                    Location = new Point((leftPanelWidth - btnWidth) / 2, y),
                    Size = new Size(btnWidth+100, btnHeight),
                    Font = GetFont(SF(9f)),
                    BackColor = btnColor,
                    ForeColor = btnText,
                    FlatStyle = FlatStyle.Flat
                };
                btnAutoBuild.FlatAppearance.BorderSize = 0;
                ApplyRoundButton(btnAutoBuild, btnColor);
                leftPanel.Controls.Add(btnAutoBuild);
                y += SY(45);

                Label lblTimeout = new Label
                {
                    Text = "请求超时(秒)",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), y),
                    Size = new Size(SX(150), inputHeight),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                leftPanel.Controls.Add(lblTimeout);

                TextBox txtTimeout = new TextBox
                {
                    Location = new Point(SX(200) + SX(16), y),
                    Size = new Size(SX(80), inputHeight),
                    Font = GetFont(SF(9f)),
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = textColor,
                    Text = "8",
                    TextAlign = HorizontalAlignment.Center
                };
                leftPanel.Controls.Add(txtTimeout);
                y += SY(40);

                Panel huashiPanel = new Panel
                {
                    Location = new Point(SX(16), y),
                    Size = new Size(SX(368), SY(100)),
                    BackColor = Color.Transparent,
                    Visible = false
                };
                Color huashiBg = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(240, 242, 248);
                huashiPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, huashiPanel.Width - 1, huashiPanel.Height - 1), 8))
                    {
                        using (SolidBrush bgBrush = new SolidBrush(huashiBg))
                            e.Graphics.FillPath(bgBrush, path);
                        using (Pen borderPen = new Pen(borderColor, 1))
                            e.Graphics.DrawPath(borderPen, path);
                    }
                };
                leftPanel.Controls.Add(huashiPanel);

                cboPlatform.SelectedIndexChanged += (s, e) =>
                {
                    huashiPanel.Visible = (cboPlatform.SelectedIndex == 1);
                };

                Label lblHuashiRange = new Label
                {
                    Text = "扫描ID区间",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), SY(12)),
                    Size = new Size(SX(90), inputHeight),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                huashiPanel.Controls.Add(lblHuashiRange);

                TextBox txtHuashiRange = new TextBox
                {
                    Location = new Point(SX(106), SY(12)),
                    Size = new Size(SX(140), inputHeight),
                    Font = GetFont(SF(9f)),
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = textColor,
                    Text = "1-100",
                    TextAlign = HorizontalAlignment.Center
                };
                huashiPanel.Controls.Add(txtHuashiRange);

                Label lblHuashiThread = new Label
                {
                    Text = "并发线程数",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), SY(48)),
                    Size = new Size(SX(90), inputHeight),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                huashiPanel.Controls.Add(lblHuashiThread);

                TextBox txtHuashiThread = new TextBox
                {
                    Location = new Point(SX(106), SY(48)),
                    Size = new Size(SX(140), inputHeight),
                    Font = GetFont(SF(9f)),
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = textColor,
                    Text = "8",
                    TextAlign = HorizontalAlignment.Center
                };
                huashiPanel.Controls.Add(txtHuashiThread);
                y += SY(110);

                Label lblHistory = new Label
                {
                    Text = "历史记录",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), y+40),
                    AutoSize = true
                };
                leftPanel.Controls.Add(lblHistory);
                y += SY(24);

                Panel historyPanel = new Panel
                {
                    Location = new Point(SX(16), y+50),
                    Size = new Size(SX(368), SY(140)),
                    BackColor = Color.Transparent
                };
                Color historyBg = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(240, 242, 248);
                historyPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, historyPanel.Width - 1, historyPanel.Height - 1), 8))
                    {
                        using (SolidBrush bgBrush = new SolidBrush(historyBg))
                            e.Graphics.FillPath(bgBrush, path);
                        using (Pen borderPen = new Pen(borderColor, 1))
                            e.Graphics.DrawPath(borderPen, path);
                    }
                };
                leftPanel.Controls.Add(historyPanel);

                ListBox lstHistory = new ListBox
                {
                    Location = new Point(SX(16), SY(6)),
                    Size = new Size(SX(336), SY(120)),
                    Font = GetFont(SF(8.5f)),
                    BackColor = isDark ? Color.FromArgb(45, 45, 55) : Color.White,
                    ForeColor = textColor,
                    BorderStyle = BorderStyle.None,
                    SelectionMode = SelectionMode.One,
                    ScrollAlwaysVisible = true
                };
                foreach (string ip in iptvHistoryIps)
                {
                    lstHistory.Items.Add(ip);
                }
                lstHistory.SelectedIndexChanged += (s, e) =>
                {
                    if (lstHistory.SelectedItem != null)
                    {
                        txtIpPort.Text = lstHistory.SelectedItem.ToString();
                        txtIpPort.ForeColor = textColor;
                    }
                };
                lstHistory.MouseDoubleClick += (s, e) =>
                {
                    if (lstHistory.SelectedItem != null)
                    {
                        txtIpPort.Text = lstHistory.SelectedItem.ToString();
                        txtIpPort.ForeColor = textColor;
                    }
                };

                ContextMenuStrip historyContextMenu = new ContextMenuStrip();
                historyContextMenu.Font = GetFont(SF(8.5f));
                historyContextMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(IsDarkColor(theme.Bg)));
                historyContextMenu.BackColor = theme.Surface;
                historyContextMenu.ForeColor = theme.TextPrimary;
                ToolStripMenuItem menuDelete = new ToolStripMenuItem("删除选中项");
                menuDelete.Click += (s, e) =>
                {
                    if (lstHistory.SelectedItem != null)
                    {
                        string selected = lstHistory.SelectedItem.ToString();
                        lstHistory.Items.Remove(selected);
                        iptvHistoryIps.Remove(selected);
                        SaveConfig();
                    }
                };
                historyContextMenu.Items.Add(menuDelete);

                ToolStripMenuItem menuClear = new ToolStripMenuItem("清空历史记录");
                menuClear.Click += (s, e) =>
                {
                    iptvHistoryIps.Clear();
                    lstHistory.Items.Clear();
                    SaveConfig();
                };
                historyContextMenu.Items.Add(menuClear);

                lstHistory.ContextMenuStrip = historyContextMenu;
                historyPanel.Controls.Add(lstHistory);
                y += SY(150);

                Label lblHeaders = new Label
                {
                    Text = "自定义请求头",
                    Font = GetFont(SF(9f)),
                    ForeColor = labelColor,
                    Location = new Point(SX(16), y+70),
                    AutoSize = true
                };
                leftPanel.Controls.Add(lblHeaders);
                y += SY(24);

                TextBox txtHeaders = new TextBox
                {
                    Location = new Point(SX(16), y+80),
                    Size = new Size(SX(368), SY(140)),
                    Font = new Font("Consolas", SF(8.5f)),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.White,
                    ForeColor = textColor,
                    Text = "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36\nAccept-Language: zh-CN,zh;q=0.9\nAccept: */*"
                };
                leftPanel.Controls.Add(txtHeaders);
                y += SY(115);

                Button btnExecute = new Button
                {
                    Text = "开始解析",
                    Location = new Point(SX(60), leftPanel.ClientSize.Height - SY(64)),
                    Size = new Size(btnWidth, btnHeight),
                    Font = GetFont(SF(8f)),
                    BackColor = btnColor,
                    ForeColor = btnText,
                    FlatStyle = FlatStyle.Flat
                };
                btnExecute.FlatAppearance.BorderSize = 0;
                ApplyRoundButton(btnExecute, btnColor);
                leftPanel.Controls.Add(btnExecute);

                Button btnCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(SX(200), leftPanel.ClientSize.Height - SY(64)),
                    Size = new Size(btnWidth, btnHeight),
                    Font = GetFont(SF(8f)),
                    BackColor = isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(220, 220, 228),
                    ForeColor = textColor,
                    FlatStyle = FlatStyle.Flat
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                ApplyRoundButton(btnCancel, btnCancel.BackColor);
                leftPanel.Controls.Add(btnCancel);

                Label lblResult = new Label
                {
                    Text = "解析结果",
                    Font = GetFont(SF(10.5f), FontStyle.Bold),
                    ForeColor = textColor,
                    Location = new Point(SX(10), SY(10)),
                    AutoSize = true
                };
                rightPanel.Controls.Add(lblResult);

                int tabW = rightPanel.ClientSize.Width - SX(20);
                int tabH = rightPanel.ClientSize.Height - SY(110);
                DarkTabControl tabResult = new DarkTabControl
                {
                    Location = new Point(SX(10), SY(40)),
                    Size = new Size(tabW, tabH),
                    Font = GetFont(SF(8f)),
                    BackColor = isDark ? Color.FromArgb(35, 35, 35) : panelBg,
                    ForeColor = textColor,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                tabResult.ApplyTheme(isDark);
                tabResult.TabWidths = new int[] { SX(90), SX(90), SX(110), SX(90) };
                tabResult.TabHeight = SY(30);
                tabResult.TabXOffset = SX(10);
                tabResult.TabSpacing = SX(8);
                rightPanel.Controls.Add(tabResult);

                TabPage tabRaw = new TabPage("原始文本");
                tabRaw.BackColor = panelBg;
                TextBox txtRaw = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", SF(9f)),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    BackColor = isDark ? Color.FromArgb(30, 30, 38) : Color.White,
                    ForeColor = textColor
                };
                tabRaw.Controls.Add(txtRaw);
                tabResult.TabPages.Add(tabRaw);

                TabPage tabPreview = new TabPage("频道预览");
                tabPreview.BackColor = panelBg;
                TextBox txtPreview = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", SF(9f)),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    BackColor = isDark ? Color.FromArgb(30, 30, 38) : Color.White,
                    ForeColor = textColor
                };
                tabPreview.Controls.Add(txtPreview);
                tabResult.TabPages.Add(tabPreview);

                TabPage tabM3u = new TabPage("M3U播放列表");
                tabM3u.BackColor = panelBg;
                TextBox txtM3u = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", SF(9f)),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    BackColor = isDark ? Color.FromArgb(30, 30, 38) : Color.White,
                    ForeColor = textColor
                };
                tabM3u.Controls.Add(txtM3u);
                tabResult.TabPages.Add(tabM3u);

                TabPage tabLog = new TabPage("运行日志");
                tabLog.BackColor = panelBg;
                TextBox txtLog = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", SF(9f)),
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    BackColor = isDark ? Color.FromArgb(30, 30, 38) : Color.White,
                    ForeColor = successColor
                };
                tabLog.Controls.Add(txtLog);
                tabResult.TabPages.Add(tabLog);

                int btnStartY = rightPanel.ClientSize.Height - SY(64);
                int btnExportWidth = SX(140);
                int btnAddWidth = SX(140);

                Button btnExport = new Button
                {
                    Text = "导出全部文件",
                    Location = new Point(SX(16), btnStartY),
                    Size = new Size(btnExportWidth, btnHeight),
                    Font = GetFont(SF(8f)),
                    BackColor = successColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnExport.FlatAppearance.BorderSize = 0;
                ApplyRoundButton(btnExport, successColor);
                rightPanel.Controls.Add(btnExport);

                Button btnAddToList = new Button
                {
                    Text = "添加到列表",
                    Location = new Point(SX(16) + btnExportWidth + SX(12), btnStartY),
                    Size = new Size(btnAddWidth, btnHeight),
                    Font = GetFont(SF(8f)),
                    BackColor = btnColor,
                    ForeColor = btnText,
                    FlatStyle = FlatStyle.Flat
                };
                btnAddToList.FlatAppearance.BorderSize = 0;
                ApplyRoundButton(btnAddToList, btnColor);
                rightPanel.Controls.Add(btnAddToList);

                Label lblStats = new Label
                {
                    Text = "频道：0 | 状态：就绪",
                    Font = GetFont(SF(8f)),
                    ForeColor = labelColor,
                    Location = new Point(rightPanel.ClientSize.Width - SX(220), btnStartY + 24),
                    AutoSize = true
                };
                rightPanel.Controls.Add(lblStats);

                void Log(string msg)
                {
                    if (txtLog.IsDisposed) return;
                    if (txtLog.InvokeRequired)
                        txtLog.BeginInvoke(new Action(() =>
                        {
                            txtLog.AppendText($"{DateTime.Now.ToString("HH:mm:ss")} | {msg}\r\n");
                            txtLog.ScrollToCaret();
                        }));
                    else
                    {
                        txtLog.AppendText($"{DateTime.Now.ToString("HH:mm:ss")} | {msg}\r\n");
                        txtLog.ScrollToCaret();
                    }
                }

                void SetStats(string stats)
                {
                    if (lblStats.InvokeRequired)
                        lblStats.BeginInvoke(new Action(() => { lblStats.Text = stats; }));
                    else
                        lblStats.Text = stats;
                }

                void ClearResults()
                {
                    txtRaw.Clear();
                    txtPreview.Clear();
                    txtM3u.Clear();
                    txtLog.Clear();
                }

                bool ValidateIpPort(string ipPort, out string error)
                {
                    error = "";
                    if (string.IsNullOrWhiteSpace(ipPort))
                    {
                        error = "IP端口不能为空";
                        return false;
                    }
                    ipPort = ipPort.Trim();
                    var match = System.Text.RegularExpressions.Regex.Match(ipPort, @"(?:http://|https://)?((?:\d{1,3}\.){3}\d{1,3}:\d+)");
                    if (!match.Success)
                    {
                        error = "IP端口格式错误，示例：110.72.103.69:8181";
                        return false;
                    }
                    ipPort = match.Groups[1].Value;
                    if (!ipPort.Contains(":"))
                    {
                        error = "缺少端口号";
                        return false;
                    }
                    var parts = ipPort.Split(':');
                    var ipParts = parts[0].Split('.');
                    if (ipParts.Length != 4)
                    {
                        error = $"IPv4分段错误：{parts[0]}";
                        return false;
                    }
                    foreach (var seg in ipParts)
                    {
                        if (!int.TryParse(seg, out int val) || val < 0 || val > 255)
                        {
                            error = $"非法IP段：{seg}";
                            return false;
                        }
                    }
                    if (!int.TryParse(parts[1], out int port) || port < 1 || port > 65535)
                    {
                        error = $"非法端口：{parts[1]}";
                        return false;
                    }
                    return true;
                }

                Dictionary<string, string> ParseHeaders(string headerText)
                {
                    var headers = new Dictionary<string, string>();
                    if (string.IsNullOrWhiteSpace(headerText)) return headers;
                    var lines = headerText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var idx = line.IndexOf(':');
                        if (idx <= 0) continue;
                        var key = line.Substring(0, idx).Trim();
                        var value = line.Substring(idx + 1).Trim();
                        headers[key] = value;
                    }
                    return headers;
                }

                HttpClient CreateHttpClient(Dictionary<string, string> customHeaders, string ipPort)
                {
                    var handler = new HttpClientHandler
                    {
                        MaxConnectionsPerServer = 32,
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                        UseCookies = false,
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 5
                    };
                    var client = new HttpClient(handler);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    if (!string.IsNullOrEmpty(ipPort))
                    {
                        var ipOnly = ipPort.Split(':')[0];
                        client.DefaultRequestHeaders.Add("Referer", $"http://{ipOnly}/");
                    }
                    if (customHeaders != null)
                    {
                        foreach (var kv in customHeaders)
                        {
                            try { client.DefaultRequestHeaders.Add(kv.Key, kv.Value); }
                            catch { }
                        }
                    }
                    return client;
                }

                string CleanText(string text)
                {
                    if (string.IsNullOrEmpty(text)) return text;
                    return System.Text.RegularExpressions.Regex.Replace(text, @"[\x00-\x1F\x7F]", "").Trim();
                }

                string GetUniqueFilePath(string baseName, string ext)
                {
                    string workDir = Application.StartupPath;
                    int num = 0;
                    while (true)
                    {
                        string fname = num == 0 ? $"{baseName}{ext}" : $"{baseName}_{num}{ext}";
                        string fullPath = Path.Combine(workDir, fname);
                        if (!File.Exists(fullPath)) return fullPath;
                        num++;
                    }
                }

                bool SafeWriteFile(string filePath, string content)
                {
                    try
                    {
                        File.WriteAllText(filePath, content, Encoding.UTF8);
                        return true;
                    }
                    catch { return false; }
                }

                string currentPlatform = "";
                string currentIpPort = "";
                string currentRaw = "";
                string currentPreview = "";
                string currentM3u = "";
                int currentValidCount = 0;

                cboPlatform.SelectedIndexChanged += (s, e) =>
                {
                    huashiPanel.Visible = cboPlatform.SelectedIndex == 1;
                };
                huashiPanel.Visible = cboPlatform.SelectedIndex == 1;

                btnAutoBuild.Click += (s, e) =>
                {
                    string ipRaw = txtIpPort.Text.Trim();
                    if (ipRaw.StartsWith("http://") || ipRaw.StartsWith("https://"))
                    {
                        Log("检测到完整HTTP链接，直接使用");
                        return;
                    }
                    if (!ValidateIpPort(ipRaw, out string error))
                    {
                        Log($"IP校验错误：{error}");
                        DarkMessageBox.Show(error, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(ipRaw, @"(?:http://|https://)?((?:\d{1,3}\.){3}\d{1,3}:\d+)");
                    string ipPort = match.Groups[1].Value;
                    string rootHttp = $"http://{ipPort}";
                    string url = "";
                    switch (cboPlatform.SelectedIndex)
                    {
                        case 0:
                            url = $"{rootHttp}/ZHGXTV/Public/json/live_interface.txt";
                            break;
                        case 1:
                            url = rootHttp;
                            break;
                        case 2:
                            url = $"{rootHttp}/iptv/live/1000.json?key=txiptv";
                            break;
                    }
                    txtIpPort.Text = ipPort;
                    Log($"已自动拼接标准地址：{url}");
                };

                btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;

                btnExecute.Click += async (s, e) =>
                {
                    ClearResults();
                    btnExecute.Enabled = false;
                    btnExecute.Text = "解析中...";
                    SetStats("频道：0 | 状态：解析中");

                    string ipRaw = txtIpPort.Text.Trim();
                    if (!ValidateIpPort(ipRaw, out string error))
                    {
                        DarkMessageBox.Show(error, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnExecute.Enabled = true;
                        btnExecute.Text = "开始解析";
                        return;
                    }

                    var match = System.Text.RegularExpressions.Regex.Match(ipRaw, @"(?:http://|https://)?((?:\d{1,3}\.){3}\d{1,3}:\d+)");
                    currentIpPort = match.Groups[1].Value;

                    if (!iptvHistoryIps.Contains(currentIpPort))
                    {
                        iptvHistoryIps.Insert(0, currentIpPort);
                        if (iptvHistoryIps.Count > 20)
                        {
                            iptvHistoryIps.RemoveRange(20, iptvHistoryIps.Count - 20);
                        }
                        SaveConfig();
                        if (!lstHistory.Items.Contains(currentIpPort))
                        {
                            lstHistory.Items.Insert(0, currentIpPort);
                            if (lstHistory.Items.Count > 20)
                            {
                                lstHistory.Items.RemoveAt(lstHistory.Items.Count - 1);
                            }
                        }
                    }

                    int timeout = 8;
                    int.TryParse(txtTimeout.Text.Trim(), out timeout);

                    var customHeaders = ParseHeaders(txtHeaders.Text);
                    var httpClient = CreateHttpClient(customHeaders, currentIpPort);
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout);

                    currentPlatform = cboPlatform.SelectedItem.ToString().Split(' ')[0];
                    Log($"===== 启动任务 | 平台{currentPlatform} | 服务器 {currentIpPort} =====");

                    try
                    {
                        switch (cboPlatform.SelectedIndex)
                        {
                            case 0:
                                await ParseZhgx(httpClient, timeout);
                                break;
                            case 1:
                                await ParseHuashi(httpClient);
                                break;
                            case 2:
                                await ParseKutv(httpClient, timeout);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"任务运行异常：{ex.Message}");
                        SetStats($"频道：0 | 状态：错误");
                    }
                    finally
                    {
                        httpClient.Dispose();
                        btnExecute.Enabled = true;
                        btnExecute.Text = "开始解析";
                    }
                };

                async Task ParseZhgx(HttpClient httpClient, int timeout)
                {
                    string url = $"http://{currentIpPort}/ZHGXTV/Public/json/live_interface.txt";
                    Log($"请求地址：{url}");

                    string rawText = "";
                    for (int retry = 0; retry <= 2; retry++)
                    {
                        try
                        {
                            var resp = await httpClient.GetAsync(url);
                            resp.EnsureSuccessStatusCode();
                            rawText = await resp.Content.ReadAsStringAsync();
                            break;
                        }
                        catch (Exception e)
                        {
                            Log($"第{retry + 1}次请求失败：{e.Message}，等待1秒重试...");
                            await Task.Delay(1000);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(rawText))
                    {
                        Log("服务器返回空内容");
                        SetStats("频道：0 | 状态：无数据");
                        return;
                    }

                    currentRaw = rawText;
                    var previewLines = new List<string>();
                    var m3uLines = new List<string> { "#EXTM3U" };
                    int validCnt = 0, errCnt = 0;

                    var lines = rawText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        try
                        {
                            var parts = trimmed.Split(new[] { ',' }, 2);
                            if (parts.Length >= 2)
                            {
                                string name = CleanText(parts[0]);
                                string playUrl = parts[1].Trim();
                                previewLines.Add($"{name} , {playUrl}");
                                m3uLines.Add($"#EXTINF:-1,{name}");
                                m3uLines.Add(playUrl);
                                validCnt++;
                            }
                        }
                        catch { errCnt++; }
                    }

                    currentPreview = string.Join("\r\n", previewLines);
                    currentM3u = string.Join("\r\n", m3uLines);
                    currentValidCount = validCnt;

                    txtRaw.Text = currentRaw;
                    txtPreview.Text = currentPreview;
                    txtM3u.Text = currentM3u;
                    Log($"智慧光迅解析完成，有效频道 {validCnt}，解析异常行 {errCnt}");
                    SetStats($"频道：{validCnt} | 状态：完成");
                }

                async Task ParseHuashi(HttpClient httpClient)
                {
                    string rangeStr = txtHuashiRange.Text.Trim();
                    int threadNum = 8;
                    int.TryParse(txtHuashiThread.Text.Trim(), out threadNum);

                    if (!rangeStr.Contains("-"))
                    {
                        Log("扫描区间格式错误");
                        DarkMessageBox.Show("扫描区间格式错误，标准示例：1-100", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var rangeParts = rangeStr.Split('-');
                    if (!int.TryParse(rangeParts[0], out int startId) || !int.TryParse(rangeParts[1], out int endId))
                    {
                        Log("扫描区间数字非法");
                        DarkMessageBox.Show("扫描区间必须为纯数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (startId <= 0 || endId < startId)
                    {
                        Log("扫描区间数字非法");
                        DarkMessageBox.Show("起始必须小于结束且大于0", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    threadNum = Math.Max(1, Math.Min(20, threadNum));

                    Log($"开始并发扫描 ID {startId}~{endId}，并发线程 {threadNum}");

                    var cidList = Enumerable.Range(startId, endId - startId + 1).ToList();
                    var validResults = new System.Collections.Concurrent.ConcurrentBag<Tuple<int, string>>();
                    int processedCount = 0;

                    var localHandler = new HttpClientHandler
                    {
                        MaxConnectionsPerServer = 32,
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                        UseCookies = false,
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 5
                    };
                    var localClient = new HttpClient(localHandler) { Timeout = TimeSpan.FromSeconds(2.5) };
                    localClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");

                    await Task.Run(() =>
                    {
                        Parallel.ForEach(cidList, new ParallelOptions { MaxDegreeOfParallelism = threadNum }, cid =>
                        {
                            string url = $"http://{currentIpPort}/newlive/live/hls/{cid}/live.m3u8";
                            try
                            {
                                var headReq = new HttpRequestMessage(HttpMethod.Head, url);
                                var headTask = localClient.SendAsync(headReq);
                                var headResp = headTask.Result;
                                if (headResp.IsSuccessStatusCode)
                                {
                                    validResults.Add(Tuple.Create(cid, url));
                                    Log($"OK ID:{cid} | {url}");
                                    return;
                                }
                            }
                            catch { }

                            try
                            {
                                var getTask = localClient.GetAsync(url);
                                var getResp = getTask.Result;
                                if (getResp.IsSuccessStatusCode)
                                {
                                    var contentTask = getResp.Content.ReadAsStringAsync();
                                    string content = contentTask.Result;
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        string checkContent = content.Length > 500 ? content.Substring(0, 500) : content;
                                        if (checkContent.Contains("m3u8") || checkContent.Contains("#EXTM3U"))
                                        {
                                            validResults.Add(Tuple.Create(cid, url));
                                            Log($"OK ID:{cid} | {url}");
                                            return;
                                        }
                                    }
                                }
                            }
                            catch { }

                            Log($"FAIL ID:{cid}");
                            int current = System.Threading.Interlocked.Increment(ref processedCount);
                            int pct = (int)(current * 100.0 / cidList.Count);
                            SetStats($"频道：{validResults.Count} | 状态：扫描中 {pct}%");
                        });
                    });

                    localClient.Dispose();
                    localHandler.Dispose();

                    var m3uLines = new List<string> { "#EXTM3U" };
                    var previewLines = new List<string>();
                    foreach (var result in validResults.OrderBy(r => r.Item1))
                    {
                        m3uLines.Add($"#EXTINF:-1,华视频道{result.Item1}");
                        m3uLines.Add(result.Item2);
                        previewLines.Add($"OK ID:{result.Item1} | {result.Item2}");
                    }

                    currentRaw = $"华视扫描汇总\r\n扫描区间：{startId}-{endId}\r\n总扫描数量：{cidList.Count}\r\n有效频道：{validResults.Count}";
                    currentPreview = string.Join("\r\n", previewLines);
                    currentM3u = string.Join("\r\n", m3uLines);
                    currentValidCount = validResults.Count;

                    txtRaw.Text = currentRaw;
                    txtPreview.Text = currentPreview;
                    txtM3u.Text = currentM3u;
                    Log($"华视扫描全部完成，有效频道 {validResults.Count}");
                    SetStats($"频道：{validResults.Count} | 状态：完成");
                }

                async Task ParseKutv(HttpClient httpClient, int timeout)
                {
                    string url = $"http://{currentIpPort}/iptv/live/1000.json?key=txiptv";
                    Log($"请求地址：{url}");

                    string jsonText = "";
                    for (int retry = 0; retry <= 2; retry++)
                    {
                        try
                        {
                            var resp = await httpClient.GetAsync(url);
                            resp.EnsureSuccessStatusCode();
                            jsonText = await resp.Content.ReadAsStringAsync();
                            break;
                        }
                        catch (Exception e)
                        {
                            Log($"第{retry + 1}次请求失败：{e.Message}，等待1秒重试...");
                            await Task.Delay(1000);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(jsonText))
                    {
                        Log("服务器返回空内容");
                        SetStats("频道：0 | 状态：无数据");
                        return;
                    }

                    try
                    {
                        var codeMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"code\"\\s*:\\s*(\\d+)");
                        if (codeMatch.Success && codeMatch.Groups[1].Value != "0")
                        {
                            var msgMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"msg\"\\s*:\\s*\"([^\"]+)\"");
                            string msg = msgMatch.Success ? msgMatch.Groups[1].Value : "未知错误";
                            Log($"接口返回异常 code={codeMatch.Groups[1].Value} msg={msg}");
                            SetStats("频道：0 | 状态：接口异常");
                            return;
                        }

                        var dataMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"data\"\\s*:\\s*(\\[.+?\\])", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (!dataMatch.Success)
                        {
                            Log("接口返回数据格式错误");
                            SetStats("频道：0 | 状态：数据错误");
                            return;
                        }

                        currentRaw = CleanText(jsonText);

                        var nameMatches = System.Text.RegularExpressions.Regex.Matches(dataMatch.Groups[1].Value, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                        var urlMatches = System.Text.RegularExpressions.Regex.Matches(dataMatch.Groups[1].Value, "\"url\"\\s*:\\s*\"([^\"]+)\"");

                        var previewLines = new List<string>();
                        var m3uLines = new List<string> { "#EXTM3U" };
                        int validCnt = 0;
                        string baseHttp = $"http://{currentIpPort}";

                        for (int i = 0; i < nameMatches.Count && i < urlMatches.Count; i++)
                        {
                            string rawName = nameMatches[i].Groups[1].Value;
                            string chName = CleanText(rawName);
                            string relUrl = urlMatches[i].Groups[1].Value;
                            if (string.IsNullOrEmpty(relUrl)) continue;

                            string fullPlay = relUrl.StartsWith("http") ? relUrl : $"{baseHttp}{relUrl}";
                            string csvLine = $"{chName},{fullPlay}";
                            previewLines.Add(csvLine);
                            m3uLines.Add($"#EXTINF:-1,{chName}");
                            m3uLines.Add(fullPlay);
                            validCnt++;
                        }

                        currentPreview = string.Join("\r\n", previewLines);
                        currentM3u = string.Join("\r\n", m3uLines);
                        currentValidCount = validCnt;

                        txtRaw.Text = currentRaw;
                        txtPreview.Text = currentPreview;
                        txtM3u.Text = currentM3u;
                        Log($"智能KUTV JSON解析完成，有效频道 {validCnt}");
                        SetStats($"频道：{validCnt} | 状态：完成");
                    }
                    catch (Exception ex)
                    {
                        Log($"解析异常：{ex.Message}");
                        SetStats("频道：0 | 状态：解析异常");
                    }
                }

                btnExport.Click += (s, e) =>
                {
                    if (currentValidCount == 0)
                    {
                        DarkMessageBox.Show("请先点击【开始解析】获取数据后再导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string ipSafe = currentIpPort.Replace(":", "_");
                    string prefix = "";
                    switch (cboPlatform.SelectedIndex)
                    {
                        case 0: prefix = "智慧光迅"; break;
                        case 1: prefix = "华视美达"; break;
                        case 2: prefix = "智能KUTV"; break;
                    }

                    var errors = new List<string>();

                    if (cboPlatform.SelectedIndex == 0)
                    {
                        string txtPath = GetUniqueFilePath($"{prefix}_原始_{ipSafe}", ".txt");
                        string m3uPath = GetUniqueFilePath($"{prefix}_直播列表_{ipSafe}", ".m3u");
                        if (!SafeWriteFile(txtPath, currentRaw)) errors.Add($"写入 {Path.GetFileName(txtPath)} 失败");
                        if (!SafeWriteFile(m3uPath, currentM3u)) errors.Add($"写入 {Path.GetFileName(m3uPath)} 失败");
                        if (errors.Count == 0)
                            DarkMessageBox.Show($"导出成功\r\n原始文本：{Path.GetFileName(txtPath)}\r\nM3U播放列表：{Path.GetFileName(m3uPath)}\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (cboPlatform.SelectedIndex == 1)
                    {
                        string m3uPath = GetUniqueFilePath($"{prefix}_有效源_{ipSafe}", ".m3u");
                        if (!SafeWriteFile(m3uPath, currentM3u)) errors.Add($"写入 {Path.GetFileName(m3uPath)} 失败");
                        if (errors.Count == 0)
                            DarkMessageBox.Show($"导出成功\r\nM3U文件：{Path.GetFileName(m3uPath)}\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string txtCsv = GetUniqueFilePath($"{prefix}_逗号清单_{ipSafe}", ".txt");
                        string txtJson = GetUniqueFilePath($"{prefix}_原始JSON_{ipSafe}", ".txt");
                        string m3uPath = GetUniqueFilePath($"{prefix}_直播列表_{ipSafe}", ".m3u");
                        if (!SafeWriteFile(txtCsv, currentPreview)) errors.Add($"写入 {Path.GetFileName(txtCsv)} 失败");
                        if (!SafeWriteFile(txtJson, currentRaw)) errors.Add($"写入 {Path.GetFileName(txtJson)} 失败");
                        if (!SafeWriteFile(m3uPath, currentM3u)) errors.Add($"写入 {Path.GetFileName(m3uPath)} 失败");
                        if (errors.Count == 0)
                            DarkMessageBox.Show($"导出3个文件成功\r\n逗号清单txt / 原始JSON / M3U播放列表\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    if (errors.Count > 0)
                        DarkMessageBox.Show(string.Join("\r\n", errors), "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                btnAddToList.Click += (s, e) =>
                {
                    if (currentValidCount == 0)
                    {
                        DarkMessageBox.Show("请先点击【开始解析】获取数据后再添加", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int addedCount = 0;
                    DateTime parseTime = DateTime.Now;
                    string groupName = $"{currentPlatform}解析";

                    if (cboPlatform.SelectedIndex == 0)
                    {
                        string[] lines = currentPreview.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string[] parts = line.Split(new string[] { " , " }, StringSplitOptions.None);
                            if (parts.Length >= 2)
                            {
                                string name = CleanText(parts[0]);
                                string url = parts[1].Trim();
                                if (!allChannels.Any(c => c.Url == url))
                                {
                                    allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = groupName, Status = "待解析", ParseDateTime = parseTime });
                                    addedCount++;
                                }
                            }
                        }
                    }
                    else if (cboPlatform.SelectedIndex == 1)
                    {
                        string[] lines = currentPreview.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (!line.StartsWith("OK")) continue;
                            var urlMatch = System.Text.RegularExpressions.Regex.Match(line, @"http[^\s]+");
                            if (urlMatch.Success)
                            {
                                string url = urlMatch.Value;
                                string name = $"华视美达_{currentIpPort}_{url.Split('/')[url.Split('/').Length - 2]}";
                                if (!allChannels.Any(c => c.Url == url))
                                {
                                    allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = groupName, Status = "待解析", ParseDateTime = parseTime });
                                    addedCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] lines = currentPreview.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string[] parts = line.Split(new char[] { ',' }, 2);
                            if (parts.Length >= 2)
                            {
                                string name = CleanText(parts[0]);
                                string url = parts[1].Trim();
                                if (!allChannels.Any(c => c.Url == url))
                                {
                                    allChannels.Add(new ChannelInfo { Name = name, Url = url, Group = groupName, Status = "待解析", ParseDateTime = parseTime });
                                    addedCount++;
                                }
                            }
                        }
                    }

                    totalCount = allChannels.Count;
                    RefreshGrid();
                    UpdateEmptyState();
                    SaveChannelList();

                    // 更新状态栏显示解析结果
                    if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
                    {
                        lblDetected.Text = $"已检测: 0/{totalCount}";
                        lblAvailable.Text = $"可用: 0";
                        lblPercent.Text = "0.00%";
                        progressBarWidth = 0;
                        RestoreLabelColors();
                        statusBarRef.PerformLayout();
                        LayoutStatusBar(statusBarRef);
                        statusBarRef.Refresh();
                    }

                    DarkMessageBox.Show($"已添加 {addedCount} 条链接到检测列表\r\n分组: {groupName}", "添加成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                // 强制创建所有子控件句柄后再应用主题，避免白色闪烁
                dlg.CreateControl();
                ForceCreateChildHandles(dlg);
                UpdateScrollBarTheme(dlg);
                dlg.ShowDialog(this);
            }
        }

    }

    public class DarkComboBox : ComboBox
    {
        private const int WM_CTLCOLORLISTBOX = 0x0134;
        private const int TRANSPARENT = 1;

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern int SetBkColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern int SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern int SetBkMode(IntPtr hdc, int iBkMode);

        private Color _borderColor = Color.FromArgb(68, 68, 78);
        private Color _focusBorderColor = Color.FromArgb(88, 101, 242);
        private Color _backColor = Color.FromArgb(44, 44, 52);
        private Color _foreColor = Color.FromArgb(225, 225, 232);
        private Color _hoverBackColor;
        private Color _itemBackColor;
        private Color _itemSelectedBackColor;
        private Color _itemHoverBackColor;
        private bool _isHover;
        private bool _isFocused;
        private int _cornerRadius = 6;
        private Color _dropDownBackColor;
        private IntPtr _dropDownBrush = IntPtr.Zero;
        private Color _dropDownBrushColor = Color.Empty;

        public Color BorderColor { get { return _borderColor; } set { _borderColor = value; Invalidate(); } }
        public Color FocusBorderColor { get { return _focusBorderColor; } set { _focusBorderColor = value; Invalidate(); } }
        public new Color BackColor
        {
            get { return _backColor; }
            set { _backColor = value; base.BackColor = value; RecalcDerived(); InvalidateDropDownBrush(); Invalidate(); }
        }
        public new Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; base.ForeColor = value; Invalidate(); }
        }
        public Color ItemBackColor
        {
            get { return _itemBackColor; }
            set { _itemBackColor = value; InvalidateDropDownBrush(); Invalidate(); }
        }
        public Color ItemSelectedBackColor { get { return _itemSelectedBackColor; } set { _itemSelectedBackColor = value; Invalidate(); } }
        public Color ItemHoverBackColor { get { return _itemHoverBackColor; } set { _itemHoverBackColor = value; Invalidate(); } }
        public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = value; UpdateRegion(); Invalidate(); } }

        public DarkComboBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.DropDownStyle = ComboBoxStyle.DropDownList;
            this.FlatStyle = FlatStyle.Flat;
            base.BackColor = _backColor;
            base.ForeColor = _foreColor;
            RecalcDerived();
            _itemBackColor = _backColor;
            _itemSelectedBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 25), Math.Min(255, _backColor.G + 25), Math.Min(255, _backColor.B + 30));
            _itemHoverBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 18), Math.Min(255, _backColor.G + 18), Math.Min(255, _backColor.B + 22));
            _borderColor = Color.FromArgb(Math.Max(0, _backColor.R - 20), Math.Max(0, _backColor.G - 20), Math.Max(0, _backColor.B - 18));
        }

        private void RecalcDerived()
        {
            _hoverBackColor = Color.FromArgb(Math.Min(255, _backColor.R + 12), Math.Min(255, _backColor.G + 12), Math.Min(255, _backColor.B + 12));
            _dropDownBackColor = _backColor;
        }

        private void InvalidateDropDownBrush()
        {
            if (_dropDownBrush != IntPtr.Zero)
            {
                DeleteObject(_dropDownBrush);
                _dropDownBrush = IntPtr.Zero;
            }
        }

        private void EnsureDropDownBrush()
        {
            if (_dropDownBrush == IntPtr.Zero || _dropDownBrushColor != _itemBackColor)
            {
                if (_dropDownBrush != IntPtr.Zero) DeleteObject(_dropDownBrush);
                _dropDownBrushColor = _itemBackColor;
                _dropDownBrush = CreateSolidBrush(ColorTranslator.ToWin32(_itemBackColor));
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_backColor != base.BackColor)
            {
                _backColor = base.BackColor;
                RecalcDerived();
                _itemBackColor = _backColor;
                InvalidateDropDownBrush();
                Invalidate();
            }
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (_foreColor != base.ForeColor)
            {
                _foreColor = base.ForeColor;
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        protected override void Dispose(bool disposing)
        {
            if (_dropDownBrush != IntPtr.Zero)
            {
                DeleteObject(_dropDownBrush);
                _dropDownBrush = IntPtr.Zero;
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CTLCOLORLISTBOX)
            {
                IntPtr hdc = m.WParam;
                SetBkColor(hdc, ColorTranslator.ToWin32(_itemBackColor));
                SetTextColor(hdc, ColorTranslator.ToWin32(_foreColor));
                SetBkMode(hdc, TRANSPARENT);
                EnsureDropDownBrush();
                m.Result = _dropDownBrush;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
            this.Invalidate();
        }

        private void UpdateRegion()
        {
            if (this.IsHandleCreated && this.Width > 0 && this.Height > 0)
            {
                using (GraphicsPath path = GetRoundedRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), _cornerRadius))
                {
                    this.Region = new Region(path);
                }
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bg = _isHover ? _hoverBackColor : _backColor;
            Color bc = _isFocused ? _focusBorderColor : _borderColor;
            float penWidth = _isFocused ? 1.5f : 1f;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using (GraphicsPath path = GetRoundedRectPath(rect, _cornerRadius))
            {
                using (SolidBrush br = new SolidBrush(bg))
                    g.FillPath(br, path);
                using (Pen pen = new Pen(bc, penWidth))
                    g.DrawPath(pen, path);
            }

            if (!string.IsNullOrEmpty(this.Text))
            {
                TextRenderer.DrawText(g, this.Text, this.Font, new Rectangle(8, 0, this.Width - 28, this.Height), _foreColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            DrawArrow(g, bc);
        }

        private void DrawArrow(Graphics g, Color arrowColor)
        {
            using (SolidBrush arrow = new SolidBrush(arrowColor))
            {
                int ax = this.Width - 18, ay = this.Height / 2;
                if (this.DroppedDown)
                {
                    Point[] tri = { new Point(ax, ay + 3), new Point(ax + 8, ay + 3), new Point(ax + 4, ay - 2) };
                    g.FillPolygon(arrow, tri);
                }
                else
                {
                    Point[] tri = { new Point(ax, ay - 2), new Point(ax + 8, ay - 2), new Point(ax + 4, ay + 3) };
                    g.FillPolygon(arrow, tri);
                }
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool isSelected = (e.State & DrawItemState.Selected) != 0;
            bool isHover = (e.State & DrawItemState.HotLight) != 0;

            Color bgColor = _itemBackColor;
            if (isSelected) bgColor = _itemSelectedBackColor;
            else if (isHover) bgColor = _itemHoverBackColor;

            using (SolidBrush br = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(br, e.Bounds);

            string text = this.Items[e.Index].ToString();
            TextRenderer.DrawText(e.Graphics, text, e.Font, new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height),
                _foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHover = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHover = false;
            this.Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            _isFocused = true;
            this.Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            _isFocused = false;
            _isHover = false;
            this.Invalidate();
        }

        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            this.Invalidate();
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            this.Invalidate();
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}


