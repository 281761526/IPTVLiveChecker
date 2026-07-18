﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
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
            WarnColor = Color.FromArgb(240, 170, 40)
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
            WarnColor = Color.FromArgb(235, 175, 45)
        };
    }

    public partial class Form1 : Form
    {
        private DataGridView dgvData;
        private Label lblDetected;
        private Label lblAvailable;
        private Label lblPercent;
        private Label lblProgressText;
        private Panel emptyStatePanel;
        private Label emptyLabel;
        private Panel navPanel;
        private Panel navSepRef;
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
        private bool isDetecting = false;
        private Button btnStartDetect;
        private int detectConcurrency = 10;
        private string customPlayerPath = "";
        private string ffplayPath = "";
        private string ffprobePath = "";
        private string ffmpegPath = "";
        private string detectEngine = "FFMPEG";
        private int timeoutSeconds = 5;
        private bool autoClearInvalid = false;
        private bool persistList = true;
        private bool enableIPv6 = false;
        private string customSearchUrl = "";
        private int totalCount = 0;
        private int detectedCount = 0;
        private int availableCount = 0;
        private List<ChannelInfo> allChannels = new List<ChannelInfo>();
        private AppTheme theme = AppTheme.GetAutoTheme();
        private string themePreference = "浅色";
        private string sortedColumn = null;
        private SortOrder sortDirection = SortOrder.Ascending;
        private Panel outerWrap;
        private Panel mainArea;
        private Panel actionArea;
        private Panel statusBarContainer;
        private Panel statusBarRef;
        private Panel searchPanelRef;
        private Panel searchBoxHostRef;
        private Panel gridContainerRef;
        private Panel settingsPanel;
        private Panel aboutPanel;
        private Panel titleBarPanel;
        private Panel bottomBarRef;
        private Button btnThemeToggle;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private Label lblTitleRef;
        private PictureBox titleIconRef;
        private string currentView = "检测";
        private bool _applyingTheme = false;
        private bool _buildingSettings = false;
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
        }

        /// <summary>
        /// 区域配置基类 - 包含字体和布局的通用结构
        /// </summary>
        private class RegionConfig
        {
            public FontConfig Font = new FontConfig();
            public LayoutConfig Layout = new LayoutConfig();
        }

        /// <summary>
        /// 字体配置类 - 各区域的字体设置
        /// </summary>
        private class FontConfig
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

            public void Initialize(float dpiScale, FontDefaults defaults)
            {
                Icon = defaults.Icon ?? new Font("Segoe UI Symbol", 16f * dpiScale);
                Text = defaults.Text ?? new Font("Microsoft YaHei", 9f * dpiScale);
                Title = defaults.Title ?? new Font("Microsoft YaHei", 11f * dpiScale, FontStyle.Bold);
                Button = defaults.Button ?? new Font("Microsoft YaHei", 8.5f * dpiScale);
                Label = defaults.Label ?? new Font("Microsoft YaHei", 9f * dpiScale);
                Input = defaults.Input ?? new Font("Microsoft YaHei", 8.5f * dpiScale);
                Hint = defaults.Hint ?? new Font("Microsoft YaHei", 8.5f * dpiScale);
                Content = defaults.Content ?? new Font("Microsoft YaHei", 9f * dpiScale);
                Header = defaults.Header ?? new Font("Microsoft YaHei", 9f * dpiScale);
                Pill = defaults.Pill ?? new Font("Microsoft YaHei", 6.7f * dpiScale);
                Url = defaults.Url ?? new Font("Consolas", 6.7f * dpiScale);
                Active = defaults.Active ?? new Font("Microsoft YaHei", 8.5f * dpiScale, FontStyle.Bold);
                Normal = defaults.Normal ?? new Font("Microsoft YaHei", 8.5f * dpiScale);
                Btn = defaults.Btn ?? new Font("Microsoft YaHei", 11f * dpiScale);
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
        /// </summary>
        private class LayoutConfig
        {
            public int Width;
            public int Height;
            public int MinWidth;
            public int MinHeight;
            public int Padding;
            public int Margin;
            public int Gap;
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int IconSize;
            public int IconGap;
            public int TitleHeight;
            public int TitleGap;
            public int BtnHeight;
            public int BtnWidth;
            public int BtnGap;
            public int LabelWidth;
            public int LabelGap;
            public int InputHeight;
            public int CornerRadius;
            public int RowHeight;
            public int HeaderHeight;
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
        /// 全局配置类 - 统一管理应用程序中所有区域的字体和布局设置
        /// 按功能区域分组，每个区域包含独立的字体和布局配置
        /// </summary>
        private class AppConfig
        {
            #region 主窗口配置
            public RegionConfig Window = new RegionConfig();
            #endregion

            #region 标题栏配置
            public RegionConfig TitleBar = new RegionConfig();
            #endregion

            #region 导航栏配置
            public RegionConfig Navigation = new RegionConfig();
            #endregion

            #region 搜索面板配置
            public RegionConfig SearchPanel = new RegionConfig();
            #endregion

            #region 左侧操作区配置
            public RegionConfig ActionArea = new RegionConfig();
            #endregion

            #region 数据表格配置
            public RegionConfig DataGrid = new RegionConfig();
            #endregion

            #region 状态栏配置
            public RegionConfig StatusBar = new RegionConfig();
            #endregion

            #region 药丸标签配置
            public RegionConfig Pill = new RegionConfig();
            #endregion

            #region 操作按钮配置
            public RegionConfig DataGridButton = new RegionConfig();
            #endregion

            #region 弹窗配置
            public RegionConfig Dialog = new RegionConfig();
            #endregion

            #region 步骤指示器配置
            public RegionConfig StepIndicator = new RegionConfig();
            #endregion

            #region Toast提示配置
            public RegionConfig Toast = new RegionConfig();
            #endregion

            #region 空状态配置
            public RegionConfig EmptyState = new RegionConfig();
            #endregion

            #region 设置页面配置
            public RegionConfig Settings = new RegionConfig();
            #endregion

            #region 关于页面配置
            public RegionConfig About = new RegionConfig();
            #endregion

            #region 右键菜单配置
            public RegionConfig ContextMenu = new RegionConfig();
            #endregion

            #region ToggleSwitch控件配置
            public RegionConfig ToggleSwitch = new RegionConfig();
            #endregion

            /// <summary>
            /// 初始化所有区域配置
            /// </summary>
            /// <param name="dpiScale">DPI缩放因子</param>
            public void Initialize(float dpiScale)
            {
                // 主窗口配置
                Window.Layout.Initialize(new LayoutDefaults
                {
                    Width = 0, Height = 0, MinWidth = 1280, MinHeight = 800,
                    Gap = 1
                });
                Window.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = new Font("Microsoft YaHei", 11f * dpiScale)
                });

                // 标题栏配置
                TitleBar.Layout.Initialize(new LayoutDefaults
                {
                    Height = 32, Left = 12, IconSize = 18, IconGap = 8,
                    BtnWidth = 40
                });

                // 导航栏配置
                Navigation.Layout.Initialize(new LayoutDefaults
                {
                    Width = 48, IconSize = 32, Gap = 70, Top = 6,
                    IconGap = 4
                });
                Navigation.Font.Initialize(dpiScale, new FontDefaults
                {
                    Icon = new Font("Segoe UI Symbol", 16f * dpiScale),
                    Text = new Font("Microsoft YaHei", 9f * dpiScale)
                });

                // 搜索面板配置
                SearchPanel.Layout.Initialize(new LayoutDefaults
                {
                    Height = 46, Left = 12, Padding = 26,
                    Gap = 98, IconGap = 298,
                    BtnWidth = 130, BtnHeight = 26,
                    CornerRadius = 6
                });
                SearchPanel.Font.Initialize(dpiScale, new FontDefaults
                {
                    Label = new Font("Microsoft YaHei", 10f * dpiScale),
                    Input = new Font("Microsoft YaHei", 8.5f * dpiScale),
                    Text = new Font("Microsoft YaHei", 9.5f * dpiScale)
                });

                // 左侧操作区配置
                ActionArea.Layout.Initialize(new LayoutDefaults
                {
                    Width = 180, Padding = 10, BtnHeight = 36,
                    BtnGap = 8, Gap = 130, Top = 8,
                    IconGap = 30
                });
                ActionArea.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = new Font("Microsoft YaHei", 11f * dpiScale, FontStyle.Bold),
                    Button = new Font("Microsoft YaHei", 8.5f * dpiScale),
                    Content = new Font("Microsoft YaHei", 9.5f * dpiScale),
                    Label = new Font("Microsoft YaHei", 9.5f * dpiScale, FontStyle.Bold)
                });

                // 数据表格配置
                DataGrid.Layout.Initialize(new LayoutDefaults
                {
                    HeaderHeight = 36, RowHeight = 30, Padding = 10,
                    DividerWidth = 1
                });
                DataGrid.Font.Initialize(dpiScale, new FontDefaults
                {
                    Content = new Font("Microsoft YaHei", 6.7f * dpiScale),
                    Header = new Font("Microsoft YaHei", 9f * dpiScale),
                    Pill = new Font("Microsoft YaHei", 6.7f * dpiScale),
                    Button = new Font("Microsoft YaHei", 6.7f * dpiScale),
                    Url = new Font("Consolas", 6.7f * dpiScale)
                });

                // 状态栏配置
                StatusBar.Layout.Initialize(new LayoutDefaults
                {
                    Height = 26, Padding = 12, Gap = 10,
                    BtnHeight = 38, IconSize = 6,
                    CornerRadius = 3
                });
                StatusBar.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = new Font("Microsoft YaHei", 9.5f * dpiScale)
                });

                // 药丸标签配置
                Pill.Layout.Initialize(new LayoutDefaults
                {
                    Height = 26, CornerRadius = 13, Padding = 12
                });
                Pill.Font.Initialize(dpiScale, new FontDefaults
                {
                    Pill = new Font("Microsoft YaHei", 6.7f * dpiScale)
                });

                // 操作按钮配置
                DataGridButton.Layout.Initialize(new LayoutDefaults
                {
                    Height = 22, Width = 60, CornerRadius = 4, Gap = 4
                });
                DataGridButton.Font.Initialize(dpiScale, new FontDefaults
                {
                    Button = new Font("Microsoft YaHei", 6.7f * dpiScale)
                });

                // 弹窗配置
                Dialog.Layout.Initialize(new LayoutDefaults
                {
                    Width = 780, Height = 640, CornerRadius = 12,
                    TitleHeight = 56, Padding = 32,
                    BtnWidth = 150, BtnHeight = 42,
                    BtnGap = 16, Bottom = 28
                });
                Dialog.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = new Font("Microsoft YaHei", 15f * dpiScale, FontStyle.Bold),
                    Text = new Font("Microsoft YaHei", 11f * dpiScale),
                    Input = new Font("Microsoft YaHei", 11f * dpiScale),
                    Hint = new Font("Microsoft YaHei", 11f * dpiScale),
                    Btn = new Font("Microsoft YaHei", 11f * dpiScale),
                    Url = new Font("Consolas", 11f * dpiScale)
                });

                // 步骤指示器配置
                StepIndicator.Layout.Initialize(new LayoutDefaults
                {
                    Height = 105, IconSize = 14, Gap = 2, IconGap = 8
                });

                // Toast提示配置
                Toast.Layout.Initialize(new LayoutDefaults
                {
                    Width = 280, Height = 50, CornerRadius = 8,
                    Right = 20, Bottom = 60, IconSize = 24, IconGap = 12,
                    Gap = 2000
                });

                // 空状态配置
                EmptyState.Layout.Initialize(new LayoutDefaults
                {
                    Width = 200, Height = 140, IconSize = 64, IconGap = 16
                });

                // 设置页面配置
                Settings.Layout.Initialize(new LayoutDefaults
                {
                    Width = 560, IconSize = 64, Left = 30,
                    Gap = 75, RowHeight = 56, LabelWidth = 170,
                    InputHeight = 30, LabelGap = 20, BtnGap = 2
                });
                Settings.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = new Font("Microsoft YaHei", 16f * dpiScale, FontStyle.Bold),
                    Label = new Font("Microsoft YaHei", 9f * dpiScale),
                    Input = new Font("Microsoft YaHei", 10f * dpiScale),
                    Hint = new Font("Microsoft YaHei", 8.5f * dpiScale)
                });

                // 关于页面配置
                About.Layout.Initialize(new LayoutDefaults
                {
                    Width = 560, Top = 80, Gap = 70, IconGap = 45
                });
                About.Font.Initialize(dpiScale, new FontDefaults
                {
                    Title = new Font("Microsoft YaHei", 20f * dpiScale, FontStyle.Bold),
                    Text = new Font("Microsoft YaHei", 10.5f * dpiScale),
                    Content = new Font("Microsoft YaHei", 9f * dpiScale)
                });

                // 右键菜单配置
                ContextMenu.Layout.Initialize(new LayoutDefaults
                {
                    BtnHeight = 28, Padding = 4, IconSize = 16, IconGap = 8
                });
                ContextMenu.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = new Font("Microsoft YaHei", 9f * dpiScale)
                });

                // ToggleSwitch控件配置
                ToggleSwitch.Layout.Initialize(new LayoutDefaults
                {
                    Width = 70, Height = 24, IconSize = 18
                });
                ToggleSwitch.Font.Initialize(dpiScale, new FontDefaults
                {
                    Text = new Font("Microsoft YaHei", 8.5f * dpiScale)
                });
            }
        }
        /// <summary>全局配置实例，所有组件共享此配置</summary>
        private AppConfig config = new AppConfig();

        private Color ColorPurple => theme.Primary;
        private Color ColorPurpleDark => theme.PrimaryDark;
        private Color ColorPink => theme.Accent;
        private Color ColorStatusBar => theme.StatusBarBg;
        private Color ColorNavSelected => theme.Primary;
        private Color ColorNavNormal => theme.TextSecondary;
        private Color ColorBorder => theme.Border;

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
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

        /// <summary>
        /// 在系统PATH、应用目录和常见位置查找ffplay.exe和ffprobe.exe
        /// </summary>
        private void FindFFplay()
        {
            ffplayPath = "";
            ffprobePath = "";
            ffmpegPath = "";
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? new string[0];
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string[] extraDirs = {
                appDir,
                Path.Combine(appDir, "ffmpeg", "bin"),
                Path.Combine(appDir, "bin"),
                @"C:\ffmpeg\bin",
                @"C:\Program Files\ffmpeg\bin",
                @"C:\msys64\ucrt64\bin",
                @"C:\msys64\mingw64\bin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ffmpeg", "bin")
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
                    if (!string.IsNullOrEmpty(ffplayPath) && !string.IsNullOrEmpty(ffprobePath) && !string.IsNullOrEmpty(ffmpegPath)) break;
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

        private Form CreateDownloadForm()
        {
            Form dlg = new Form
            {
                Text = "正在安装 FFmpeg 组件",
                Size = new Size(500, 260),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                ShowInTaskbar = false,
                TopMost = true
            };
            Label lblTitle = new Label
            {
                Text = "⏳ 正在下载 FFmpeg 组件（播放和检测功能必需）",
                Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 55, 65),
                Location = new Point(20, 18),
                AutoSize = true
            };
            dlg.Controls.Add(lblTitle);
            Label lblStatus = new Label
            {
                Text = "正在准备下载...",
                Font = new Font("Microsoft YaHei", 9.5f),
                ForeColor = Color.FromArgb(100, 105, 115),
                Location = new Point(20, 52),
                AutoSize = true
            };
            dlg.Controls.Add(lblStatus);
            ProgressBar progressBar = new ProgressBar
            {
                Location = new Point(20, 80),
                Width = 445,
                Height = 24,
                Style = ProgressBarStyle.Blocks,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            dlg.Controls.Add(progressBar);
            TextBox txtLog = new TextBox
            {
                Location = new Point(20, 118),
                Width = 445,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8.5f),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            dlg.Controls.Add(txtLog);
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
                            MessageBox.Show(dlg, "FFmpeg 自动下载失败，请手动下载 ffmpeg 并将 ffmpeg.exe、ffplay.exe、ffprobe.exe 放到程序根目录。\n下载地址：https://www.gyan.dev/ffmpeg/builds/", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show(dlg, "已下载但未能在压缩包中找到所需组件，请手动将 ffmpeg.exe、ffplay.exe、ffprobe.exe 复制到程序目录：\n" + appDir, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show(dlg, "FFmpeg 安装过程出错：\n" + ex.Message + "\n\n请手动从 https://www.gyan.dev/ffmpeg/builds/ 下载并解压到程序目录。", "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (b.Length != 4) return "";
            if (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 169 && b[1] == 254)
            {
                string lan = "内网";
                lock (ipLocationCache) { ipLocationCache[ip] = lan; }
                return lan;
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
                result = GuessIpLocation(b[0], b[1]);
                if (string.IsNullOrEmpty(result)) result = "海外";
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
                    }
                }
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
                using (var cts = new CancellationTokenSource(5000))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fp,
                        Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"{url}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return "";
                        var outputTask = proc.StandardOutput.ReadToEndAsync();
                        var errorTask = proc.StandardError.ReadToEndAsync();
                        var waitTask = Task.Run(() => proc.WaitForExit(5000));
                        await Task.WhenAny(waitTask, Task.Delay(6000, linked.Token));
                        if (!proc.HasExited)
                        {
                            try { proc.Kill(); } catch { }
                            return "";
                        }
                        string output = await outputTask;
                        await errorTask;
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

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            int r = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (r < 2) r = 2;
            GraphicsPath path = new GraphicsPath();
            int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + w - r, y, r, r, 270, 90);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void StyleRoundButton(Button btn, int radius = 8, Color? borderColor = null, int borderWidth = 0)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Empty;
            btn.UseVisualStyleBackColor = false;
            Color bc = borderColor ?? Color.Empty;
            int bw = borderWidth;
            Color normalBg = btn.BackColor;
            Color hoverBg = Color.FromArgb(Math.Min(255, normalBg.R + 15), Math.Min(255, normalBg.G + 15), Math.Min(255, normalBg.B + 15));
            Color pressedBg = Color.FromArgb(Math.Max(0, normalBg.R - 15), Math.Max(0, normalBg.G - 15), Math.Max(0, normalBg.B - 15));
            bool isHover = false, isPressed = false;
            btn.MouseEnter += (s, e) => { isHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHover = false; isPressed = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { isPressed = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { isPressed = false; btn.Invalidate(); };
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color parentBg = btn.Parent != null ? btn.Parent.BackColor : Color.White;
                using (SolidBrush clear = new SolidBrush(parentBg))
                    e.Graphics.FillRectangle(clear, 0, 0, btn.Width, btn.Height);
                Rectangle rect = new Rectangle(bw / 2, bw / 2, btn.Width - 1 - bw, btn.Height - 1 - bw);
                Color bg = isPressed ? pressedBg : (isHover ? hoverBg : normalBg);
                int innerR = Math.Max(1, radius - bw);
                using (GraphicsPath path = GetRoundedPath(rect, innerR))
                using (SolidBrush br = new SolidBrush(bg))
                    e.Graphics.FillPath(br, path);
                if (bw > 0 && bc != Color.Empty)
                {
                    Rectangle borderRect = new Rectangle(bw / 2, bw / 2, btn.Width - 1 - bw, btn.Height - 1 - bw);
                    using (GraphicsPath bp = GetRoundedPath(borderRect, innerR))
                    using (Pen pen = new Pen(bc, bw))
                    {
                        pen.Alignment = PenAlignment.Center;
                        e.Graphics.DrawPath(pen, bp);
                    }
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            };
            btn.Resize += (s, e) => btn.Invalidate();
            btn.Invalidate();
        }

        private static void StyleRoundTextBox(TextBox txt, int radius = 6, Color? borderColor = null, int borderWidth = 1)
        {
            txt.BorderStyle = BorderStyle.None;
            Color bc = borderColor ?? Color.FromArgb(200, 200, 200);
            int bw = borderWidth;
            Panel host = txt.Parent as Panel;
            if (host == null) return;
            host.Paint += (s, e) =>
            {
                if (!txt.Visible || txt.IsDisposed) return;
                Rectangle rect = new Rectangle(txt.Left - bw, txt.Top - bw, txt.Width + bw * 2, txt.Height + bw * 2);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(rect, radius))
                using (SolidBrush br = new SolidBrush(txt.BackColor))
                    e.Graphics.FillPath(br, path);
                using (GraphicsPath path = GetRoundedPath(rect, radius))
                using (Pen pen = new Pen(bc, bw))
                    e.Graphics.DrawPath(pen, path);
            };
            txt.Resize += (s, e) => host.Invalidate();
            txt.LocationChanged += (s, e) => host.Invalidate();
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

        private class ToggleSwitch : Control
        {
            private bool _checked;
            public bool Checked
            {
                get { return _checked; }
                set { if (_checked != value) { _checked = value; Invalidate(); OnCheckedChanged(EventArgs.Empty); } }
            }
            public string OnText { get; set; } = "是";
            public string OffText { get; set; } = "手动";
            public Color OnColor { get; set; } = Color.FromArgb(46, 169, 92);
            public Color OffColor { get; set; } = Color.FromArgb(205, 205, 210);
            public event EventHandler CheckedChanged;
            protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }

            public ToggleSwitch()
            {
                this.Size = new Size(70, 26);
                this.DoubleBuffered = true;
                this.Cursor = Cursors.Hand;
                this.Font = new Font("Microsoft YaHei", 8.5f);
                this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.BackColor = Color.Transparent;
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
            }

            protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Color bgColor = this.Parent != null ? this.Parent.BackColor : Color.White;
                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                    e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);

                int w = this.Width, h = this.Height;
                int pillH = Math.Min(h - 4, 24);
                int pillY = (h - pillH) / 2;
                int pillR = pillH / 2;
                Rectangle pillRect = new Rectangle(0, pillY, w, pillH);

                Color pillColor = _checked ? OnColor : OffColor;
                using (SolidBrush br = new SolidBrush(pillColor))
                using (GraphicsPath path = GetRoundedPath(pillRect, pillR))
                    e.Graphics.FillPath(br, path);

                int dotMargin = 2;
                int dotSize = pillH - dotMargin * 2;
                int dotY = pillY + dotMargin;
                int dotX = _checked ? w - dotSize - dotMargin : dotMargin;

                using (SolidBrush wb = new SolidBrush(Color.White))
                    e.Graphics.FillEllipse(wb, dotX, dotY, dotSize, dotSize);

                string txt = _checked ? OnText : OffText;
                Color textColor = _checked ? Color.White : Color.FromArgb(140, 140, 150);
                float fontSize = Math.Min(8.5f, pillH * 0.42f);
                using (Font txtFont = new Font(this.Font.FontFamily, fontSize))
                {
                    SizeF ts = e.Graphics.MeasureString(txt, txtFont);
                    float tx, ty;
                    ty = pillY + (pillH - ts.Height) / 2 - 1;
                    if (_checked)
                    {
                        float leftAreaW = dotX - dotMargin;
                        tx = dotMargin + (leftAreaW - ts.Width) / 2;
                    }
                    else
                    {
                        float rightAreaStart = dotX + dotSize + dotMargin;
                        float rightAreaW = (w - dotMargin) - rightAreaStart;
                        tx = rightAreaStart + (rightAreaW - ts.Width) / 2;
                    }
                    using (SolidBrush tb = new SolidBrush(textColor))
                        e.Graphics.DrawString(txt, txtFont, tb, tx, ty);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "app.ico");
                if (!File.Exists(iconPath))
                {
                    try { SaveMultiSizeIcon(iconPath); }
                    catch
                    {
                        try
                        {
                            using (Bitmap bmp = GenerateAppIconBitmap(512))
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                File.WriteAllBytes(iconPath, MakeSimpleIcon(ms.ToArray(), 512, 512));
                            }
                        }
                        catch { }
                    }
                }
                if (File.Exists(iconPath))
                    this.Icon = new Icon(iconPath);
                else
                    this.Icon = GenerateAppIcon(512);
            }
            catch { this.Icon = GenerateAppIcon(512); }
            BuildUI();
            this.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(300);
                if (!FFComponentsReady())
                {
                    var dr = MessageBox.Show(this, "未检测到 FFmpeg 组件（ffmpeg.exe / ffplay.exe / ffprobe.exe）。\n播放和分辨率检测功能需要该组件。\n\n是否自动下载安装？", "组件缺失", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes)
                    {
                        bool ok = await DownloadFFmpegAsync(this);
                        if (!ok && !FFComponentsReady())
                        {
                            MessageBox.Show(this, "组件安装失败，播放功能将不可用。\n可稍后手动下载 ffmpeg 并解压到程序目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }));
        }

        private void CreateTitleBar()
        {
            titleBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = theme.Bg
            };

            PictureBox titleIcon = new PictureBox
            {
                Size = new Size(22, 22),
                Location = new Point(12, 9),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            titleIconRef = titleIcon;
            titleIcon.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush tvBrush = new SolidBrush(theme.Primary))
                {
                    using (GraphicsPath tvPath = RoundedRectPath(new Rectangle(2, 4, 18, 13), 3))
                        g.FillPath(tvBrush, tvPath);
                    using (SolidBrush screenBrush = new SolidBrush(theme.Bg))
                        g.FillRectangle(screenBrush, new Rectangle(4, 6, 14, 9));
                    g.FillRectangle(tvBrush, 7, 17, 7, 2);
                    g.FillRectangle(tvBrush, 5, 19, 12, 2);
                }
            };
            titleBarPanel.Controls.Add(titleIcon);

            Label lblTitle = new Label
            {
                Text = "wtv工具箱 pro",
                Font = new Font("Microsoft YaHei", 12f),
                ForeColor = theme.TextPrimary,
                Location = new Point(40, 0),
                Size = new Size(220, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            lblTitleRef = lblTitle;
            titleBarPanel.Controls.Add(lblTitle);

            int btnSize = 40;

            void UpdateRightX()
            {
                int w = titleBarPanel.ClientSize.Width;
                int totalBtnsWidth = btnSize * 4;
                int startX = w - totalBtnsWidth;
                btnThemeToggle.Left = startX;
                btnMin.Left = startX + btnSize;
                btnMax.Left = startX + btnSize * 2;
                btnClose.Left = startX + btnSize * 3;
            }

            Color titleBtnBg = theme.Bg;
            Color titleBtnFg = theme.TextSecondary;
            Color titleBtnHover = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235);
            Color closeBtnHover = Color.FromArgb(232, 17, 35);
            Color closeBtnFg = Color.White;

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
                return b;
            }

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
                if (isDark)
                {
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

            btnMin = CreateTitleButton();
            btnMin.Paint += (s, e) =>
            {
                bool isHover = btnMin.ClientRectangle.Contains(btnMin.PointToClient(Cursor.Position));
                Color ic = isHover ? theme.TextPrimary : theme.TextSecondary;
                int baseSize = 40;
                using (Pen pen = new Pen(ic, 1.5f))
                    e.Graphics.DrawLine(pen, baseSize / 2 - 8, baseSize / 2 + 6, baseSize / 2 + 8, baseSize / 2 + 6);
            };
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            titleBarPanel.Controls.Add(btnMin);

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
                        e.Graphics.DrawRectangle(pen, baseSize / 2 - 7, baseSize / 2 - 5, 9, 9);
                        e.Graphics.DrawRectangle(pen, baseSize / 2 - 4, baseSize / 2 - 8, 9, 9);
                    }
                    else
                    {
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

            btnClose = CreateTitleButton();
            btnClose.FlatAppearance.MouseOverBackColor = closeBtnHover;
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
            btnClose.Paint += (s, e) =>
            {
                bool isHover = btnClose.ClientRectangle.Contains(btnClose.PointToClient(Cursor.Position));
                bool isDown = MouseButtons == MouseButtons.Left && isHover;
                int baseSize = 40;
                Color bgColor = isDown ? Color.FromArgb(200, 15, 30) : (isHover ? closeBtnHover : btnClose.BackColor);
                using (SolidBrush bgBr = new SolidBrush(bgColor))
                    e.Graphics.FillRectangle(bgBr, 0, 0, baseSize, baseSize);
                Color ic = isHover ? closeBtnFg : theme.TextSecondary;
                int offset = isDown ? 1 : 0;
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

            titleBarPanel.Resize += (s, e) => UpdateRightX();

            titleBarPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            lblTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            titleIcon.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };

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

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int WM_GETMINMAXINFO = 0x0024;
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

        private void BuildUI()
        {
            if (themePreference == "深色") theme = AppTheme.Dark;
            else theme = AppTheme.Light;
            this.Text = "wtv工具箱 pro";
            this.AutoScaleMode = AutoScaleMode.None;
            using (Graphics g = this.CreateGraphics())
                dpiScale = g.DpiX / 96f;
            config.Initialize(dpiScale);
            // 根据屏幕分辨率自动设置窗口大小（屏幕工作区的88%，最小1280x800）
            int screenW = Screen.PrimaryScreen.WorkingArea.Width;
            int screenH = Screen.PrimaryScreen.WorkingArea.Height;
            int winW = Math.Max(1280, (int)(screenW * 0.88));
            int winH = Math.Max(800, (int)(screenH * 0.88));
            this.Size = new Size(winW, winH);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                (screenW - winW) / 2,
                (screenH - winH) / 2
            );
            this.Font = new Font("Microsoft YaHei", SF(11f));
            this.MinimumSize = new Size(SX(900), SY(600));
            this.BackColor = theme.Border;

            // 外层边框容器，用于实现细边框效果
            outerWrap = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.Border,
                Padding = new Padding(1)
            };
            this.Controls.Add(outerWrap);

            // 自定义标题栏
            CreateTitleBar();

            // ================================================================
            //  WinForms Dock布局规则: Dock=Left时,最后Add的控件在最左边
            //  目标顺序(从左到右): navPanel -> navSep -> actionArea -> actionSep -> mainArea(Fill)
            // ================================================================

            // ==================== 右侧主数据区(Dock=Fill,最先Add) ====================
            mainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.BgAlt
            };
            mainArea.Resize += (s, e) =>
            {
                if (settingsPanel != null && settingsPanel.Visible) LayoutFillPanel(settingsPanel);
                if (aboutPanel != null && aboutPanel.Visible) LayoutFillPanel(aboutPanel);
                UpdateScrollBarTheme(mainArea);
            };

            // ---- 数据表格容器(Dock=Fill,在mainArea内最先Add) ----
            gridContainerRef = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.BgAlt
            };

            // DataGridView
            dgvData = new DataGridView();
            dgvData.Dock = DockStyle.Fill;
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvData.BackgroundColor = theme.BgAlt;
            dgvData.RowHeadersVisible = false;
            dgvData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvData.ReadOnly = false;
            dgvData.AllowUserToAddRows = false;
            dgvData.AllowUserToDeleteRows = false;
            dgvData.AllowUserToResizeColumns = false;
            dgvData.AllowUserToResizeRows = false;
            dgvData.AllowUserToOrderColumns = false;
            dgvData.EditMode = DataGridViewEditMode.EditOnF2;
            dgvData.Font = new Font("Microsoft YaHei", SF(6.7f));
            dgvData.RowTemplate.Height = SY(42);
            dgvData.CellDoubleClick += DgvData_CellDoubleClick;
            dgvData.CellEndEdit += DgvData_CellEndEdit;
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
            dgvData.EnableHeadersVisualStyles = false;
            dgvData.GridColor = theme.Border;
            dgvData.BorderStyle = BorderStyle.None;
            dgvData.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical;
            dgvData.ColumnHeadersVisible = true;
            dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvData.ColumnHeadersHeight = SY(36);
            dgvData.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvData.DefaultCellStyle.SelectionBackColor = theme.SelectRow;
            dgvData.DefaultCellStyle.SelectionForeColor = theme.SelectRowText;
            dgvData.RowTemplate.Height = SY(36);

            // 表头样式
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            headerStyle.BackColor = theme.HeaderBg;
            headerStyle.ForeColor = theme.TextSecondary;
            headerStyle.Font = new Font("Microsoft YaHei", SF(9f));
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            headerStyle.Padding = new Padding(SX(10), 0, 0, 0);
            headerStyle.SelectionBackColor = theme.HeaderBg;
            headerStyle.SelectionForeColor = theme.TextSecondary;
            dgvData.ColumnHeadersDefaultCellStyle = headerStyle;

            // 行样式
            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
            rowStyle.BackColor = theme.Surface;
            rowStyle.ForeColor = theme.TextPrimary;
            rowStyle.SelectionBackColor = theme.SelectRow;
            rowStyle.SelectionForeColor = theme.SelectRowText;
            rowStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0));
            rowStyle.Font = new Font("Microsoft YaHei", SF(6.7f));
            dgvData.RowsDefaultCellStyle = rowStyle;
            DataGridViewCellStyle altStyle = new DataGridViewCellStyle();
            altStyle.BackColor = theme.Surface;
            altStyle.ForeColor = theme.TextPrimary;
            altStyle.SelectionBackColor = theme.SelectRow;
            altStyle.SelectionForeColor = theme.SelectRowText;
            altStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0));
            altStyle.Font = new Font("Microsoft YaHei", SF(6.7f));
            dgvData.AlternatingRowsDefaultCellStyle = altStyle;

            // 添加8列（含操作列，自绘双按钮）
            dgvData.Columns.Add("colName", "名称");
            dgvData.Columns.Add("colUrl", "链接");
            dgvData.Columns.Add("colLocation", "归属地");
            dgvData.Columns.Add("colResolution", "分辨率");
            dgvData.Columns.Add("colSpeed", "响应速度");
            dgvData.Columns.Add("colGroup", "分组");
            dgvData.Columns.Add("colStatus", "状态");
            dgvData.Columns.Add("colAction", "操作");

            dgvData.CellClick += DgvData_CellClick;
            dgvData.ColumnHeaderMouseClick += DgvData_ColumnHeaderMouseClick;
            dgvData.CellPainting += DgvData_CellPainting;
            dgvData.CellFormatting += DgvData_CellFormatting;
            dgvData.CellMouseMove += DgvData_CellMouseMove;
            dgvData.CellMouseDown += DgvData_CellMouseDown;
            dgvData.CellMouseUp += DgvData_CellMouseUp;
            dgvData.ShowCellToolTips = true;
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
            dgvData.MouseLeave += (s, e) => { if (_hoverRow != -1) { _hoverRow = -1; _hoverBtn = -1; dgvData.Invalidate(); } };

            dgvData.Columns["colName"].FillWeight = 25;
            dgvData.Columns["colUrl"].FillWeight = 120;
            dgvData.Columns["colLocation"].FillWeight = 37;
            dgvData.Columns["colResolution"].FillWeight = 25;
            dgvData.Columns["colSpeed"].FillWeight = 28;
            dgvData.Columns["colGroup"].FillWeight = 20;
            dgvData.Columns["colStatus"].FillWeight = 22;
            dgvData.Columns["colAction"].FillWeight = 30;

            // 设置列的最小宽度（确保默认窗口下右侧列内容完整显示）
            dgvData.Columns["colName"].MinimumWidth = SX(20);
            dgvData.Columns["colUrl"].MinimumWidth = SX(520);
            dgvData.Columns["colLocation"].MinimumWidth = SX(20);
            dgvData.Columns["colResolution"].MinimumWidth = SX(15);
            dgvData.Columns["colSpeed"].MinimumWidth = SX(20);
            dgvData.Columns["colGroup"].MinimumWidth = SX(18);
            dgvData.Columns["colStatus"].MinimumWidth = SX(18);
            dgvData.Columns["colAction"].MinimumWidth = SX(20);

            // 所有列居中显示（除名称和链接左对齐）
            foreach (DataGridViewColumn col in dgvData.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            dgvData.Columns["colName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colUrl"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colLocation"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colResolution"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns["colGroup"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            // 药丸（响应速度、状态）和按钮（操作）保持居中
            dgvData.Columns["colSpeed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns["colStatus"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns["colAction"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvData.Columns["colName"].ReadOnly = false;
            dgvData.Columns["colUrl"].ReadOnly = true;
            dgvData.Columns["colUrl"].DefaultCellStyle.Font = new Font("Consolas", SF(6.7f));
            dgvData.Columns["colLocation"].ReadOnly = true;
            dgvData.Columns["colResolution"].ReadOnly = true;
            dgvData.Columns["colSpeed"].ReadOnly = true;
            dgvData.Columns["colGroup"].ReadOnly = true;
            dgvData.Columns["colStatus"].ReadOnly = true;
            dgvData.Columns["colAction"].ReadOnly = true;

            // 默认按名称排序
            sortedColumn = "colName";
            sortDirection = SortOrder.Ascending;

            gridContainerRef.Controls.Add(dgvData);

            // 空状态面板
            emptyStatePanel = new Panel
            {
                BackColor = Color.Transparent,
                Size = new Size(SX(140), SY(110))
            };

            PictureBox emptyIconBox = new PictureBox
            {
                Size = new Size(SX(56), SY(56)),
                Location = new Point(SX(42), SY(0)),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            emptyIconBox.Paint += EmptyIcon_Paint;

            emptyLabel = new Label
            {
                Text = "无效站",
                Font = new Font("Microsoft YaHei", SF(11f)),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            emptyStatePanel.Controls.Add(emptyIconBox);
            emptyStatePanel.Controls.Add(emptyLabel);
            gridContainerRef.Controls.Add(emptyStatePanel);
            emptyStatePanel.BringToFront();

            gridContainerRef.Resize += (s, e) => CenterEmptyState();

            // ---- 搜索栏(Dock=Top,在gridContainer上方Add) ----
            searchPanelRef = new Panel
            {
                Dock = DockStyle.Top,
                Height = SY(38),
                BackColor = theme.BgAlt
            };

            Label lblSearch = new Label
            {
                Text = "搜 索 :",
                Font = new Font("Microsoft YaHei", SF(8.5f)),
                ForeColor = theme.TextPrimary,
                Location = new Point(0, SY(0)),
                AutoSize = true
            };
            lblSearch.Height = SY(26);
            lblSearch.Top = (SY(38) - SY(26)) / 2;
            searchPanelRef.Controls.Add(lblSearch);

            // 分组筛选下拉框 - DarkComboBox自绘圆角（放在右边）
            cboGroupHost = new Panel
            {
                BackColor = theme.BgAlt,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(0, (SY(38) - SY(26)) / 2),
                Size = new Size(110, SY(26))
            };
            DarkComboBox darkCbo = new DarkComboBox
            {
                Font = new Font("Microsoft YaHei", SF(8.5f)),
                Dock = DockStyle.Fill,
                BackColor = theme.Surface,
                ForeColor = theme.TextPrimary,
                BorderColor = theme.Border,
                FocusBorderColor = theme.Primary,
                ItemBackColor = theme.Surface,
                ItemSelectedBackColor = theme.BgAlt,
                ItemHoverBackColor = Color.FromArgb(Math.Min(255, theme.Surface.R + 10), Math.Min(255, theme.Surface.G + 10), Math.Min(255, theme.Surface.B + 10)),
                CornerRadius = 6,
                ItemHeight = SY(22)
            };
            cboGroup = darkCbo;
            cboGroup.Items.Add("全部");
            cboGroup.SelectedIndex = 0;
            cboGroup.SelectedIndexChanged += CboGroup_SelectedIndexChanged;
            cboGroupHost.Controls.Add(cboGroup);
            searchPanelRef.Controls.Add(cboGroupHost);

            // 分组筛选标签
            Label lblGroup = new Label
            {
                Text = "分组:",
                Font = new Font("Microsoft YaHei", SF(8.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Visible = false,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblGroup.Height = SY(26);
            lblGroup.Top = (SY(38) - SY(26)) / 2;
            lblGroupFilter = lblGroup;
            searchPanelRef.Controls.Add(lblGroup);

            // 搜索框圆角容器（左边是"地址"标签，右边是分组下拉框区域）
            Panel searchBoxHost = new Panel
            {
                Location = new Point(98, (SY(38) - SY(26)) / 2),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Size = new Size(40, SY(26)),
                BackColor = theme.Surface
            };
            searchBoxHostRef = searchBoxHost;
            searchPanelRef.Controls.Add(searchBoxHost);

            TextBox txtSearch = new TextBox
            {
                Font = new Font("Microsoft YaHei", SF(8f)),
                BorderStyle = BorderStyle.None,
                Location = new Point(18, SY(2)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = searchBoxHost.Width - 20,
                Height = SY(18),
                Text = "输入搜索内容，按下回车键搜索",
                ForeColor = theme.TextSecondary,
                BackColor = theme.Surface
            };
            txtSearchBox = txtSearch;
            searchBoxHost.Controls.Add(txtSearch);

            cboGroup.HandleCreated += (s, e) =>
            {
                SetWindowTheme(cboGroup.Handle, "", "");
            };

            // 统一绘制圆角边框：搜索框和下拉框容器
            bool searchFocus = false;
            txtSearch.GotFocus += (s, e) =>
            {
                searchFocus = true;
                searchBoxHost.Invalidate();
                if (txtSearch.Text == "输入搜索内容，按下回车键搜索")
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = theme.TextPrimary;
                }
            };
            txtSearch.LostFocus += (s, e) =>
            {
                searchFocus = false;
                searchBoxHost.Invalidate();
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = "输入搜索内容，按下回车键搜索";
                    txtSearch.ForeColor = theme.TextSecondary;
                }
            };
            searchBoxHost.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, searchBoxHost.Width - 1, searchBoxHost.Height - 1);
                Color bc = searchFocus ? theme.Primary : theme.Border;
                using (GraphicsPath sp = GetRoundedPath(rect, 6))
                {
                    using (SolidBrush br = new SolidBrush(theme.Surface))
                        e.Graphics.FillPath(br, sp);
                    using (Pen pen = new Pen(bc, searchFocus ? 1.5f : 1f))
                        e.Graphics.DrawPath(pen, sp);
                }
            };

            void UpdateSearchBoxRegion()
            {
                // 不再使用Region裁剪，避免裁掉子控件TextBox
                // 圆角效果通过Paint事件绘制边框实现
            }
            UpdateSearchBoxRegion();

            void UpdateCboGroupRegion()
            {
                if (cboGroupHost.Width > 0 && cboGroupHost.Height > 0)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, cboGroupHost.Width - 1, cboGroupHost.Height - 1), 6))
                    {
                        cboGroupHost.Region = new Region(path);
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
                Renderer = new ToolStripProfessionalRenderer(new MenuColorTable()),
                Font = new Font("Microsoft YaHei", SF(10f)),
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
                Height = SY(38),
                BackColor = theme.Bg,
                Padding = new Padding(SX(12), SY(6), SX(12), SY(6))
            };

            statusBarRef = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = theme.StatusBarBg
            };

            lblDetected = new Label
            {
                Text = "已检测: 0/0",
                Font = new Font("Microsoft YaHei", SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblDetected);

            lblAvailable = new Label
            {
                Text = "可用: 0",
                Font = new Font("Microsoft YaHei", SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblAvailable);

            lblProgressText = new Label
            {
                Text = "检测进度:",
                Font = new Font("Microsoft YaHei", SF(9.5f)),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblProgressText);

            lblPercent = new Label
            {
                Text = "0.00%",
                Font = new Font("Microsoft YaHei", SF(10.5f), FontStyle.Bold),
                ForeColor = theme.Primary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            statusBarRef.Controls.Add(lblPercent);

            statusBarRef.Resize += (s, e) =>
            {
                LayoutStatusBar(statusBarRef);
                UpdateStatusBarRegion();
            };
            statusBarContainer.Controls.Add(statusBarRef);
            LayoutStatusBar(statusBarRef);

            // 按Dock顺序添加到mainArea(从下到上:gridContainer -> searchPanel -> statusBarContainer)
            mainArea.Controls.Add(gridContainerRef);
            mainArea.Controls.Add(searchPanelRef);
            mainArea.Controls.Add(statusBarContainer);

            // ==================== 中间:操作按钮区(Dock=Left,第二个Add) ====================
            actionArea = new Panel
            {
                Dock = DockStyle.Left,
                Width = SX(180),
                BackColor = theme.BgAlt,
                AutoScroll = true
            };

            int ay = SY(14);
            int btnW = SX(156);
            int leftX = SX(12);

            // 1. 选择m3u/txt按钮
            Button btnSelectFile = new Button
            {
                Text = "📄选择m3u/txt",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(32)),
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Surface,
                ForeColor = ColorPurple,
                Font = new Font("Microsoft YaHei", SF(8.5f)),
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
                using (Pen pen = new Pen(ColorPurple, 1.5f))
                {
                    g.DrawRectangle(pen, iconX, iconY, 12, 10);
                    g.DrawLine(pen, iconX, iconY + 3, iconX + 12, iconY + 3);
                }
                using (SolidBrush brush = new SolidBrush(ColorPurple))
                {
                    g.FillRectangle(brush, iconX + 2, iconY + 1, 3, 2);
                    g.FillRectangle(brush, iconX + 7, iconY + 1, 3, 2);
                }
            };
            btnSelectFile.FlatAppearance.MouseOverBackColor = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(248, 242, 255);
            btnSelectFile.FlatAppearance.BorderColor = ColorPurple;
            btnSelectFile.FlatAppearance.BorderSize = 1;
            btnSelectFile.Click += BtnSelectFile_Click;
            actionArea.Controls.Add(btnSelectFile);
            StyleRoundButton(btnSelectFile, 4, ColorPurple, 1);
            ay += SY(32) + SY(10);

            // 2. 开始检测按钮
            btnStartDetect = new Button
            {
                Text = "⚡️开始检测",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(36)),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPurple,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", SF(9.5f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft
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
                using (SolidBrush playBrush = new SolidBrush(ColorPurple))
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
            btnStartDetect.FlatAppearance.MouseOverBackColor = ColorPurpleDark;
            btnStartDetect.FlatAppearance.BorderSize = 0;
            btnStartDetect.Click += BtnStartDetect_Click;
            actionArea.Controls.Add(btnStartDetect);
            StyleRoundButton(btnStartDetect, 4);
            ay += SY(36) + SY(10);

            // 3. 导出按钮
            Button btnExport = new Button
            {
                Text = "🚀 导  出",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPink,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", SF(9f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 60, 110);
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            actionArea.Controls.Add(btnExport);
            StyleRoundButton(btnExport, 4);
            ay += SY(34) + SY(10);

            // 4. 扫源按钮
            Button btnScanSource = new Button
            {
                Text = "🔎 扫  源",
                Location = new Point(leftX, ay),
                Size = new Size(btnW, SY(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", SF(9f), FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnScanSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 120, 220);
            btnScanSource.FlatAppearance.BorderSize = 0;
            btnScanSource.Click += (s, e) => ShowScanSourceDialog();
            actionArea.Controls.Add(btnScanSource);
            StyleRoundButton(btnScanSource, 4);
            ay += SY(34) + SY(14);

            // 5. 提示框
            int tipW = btnW;
            Panel tipBox = new Panel
            {
                Location = new Point(leftX, ay),
                Size = new Size(tipW, SY(130)),
                BackColor = theme.TipBg
            };
            Label tipTitle = new Label
            {
                Text = "提示",
                Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                Location = new Point(10, 8),
                BackColor = Color.Transparent
            };
            tipBox.Controls.Add(tipTitle);
            Label tipContent = new Label
            {
                Text = "1. 列表位置，点击右键发现更多功能\r\n2. 双击名称，删除这一行\r\n3. 检测IPv6相关地址，需要在设置里打开IPv6功能",
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = theme.TextSecondary,
                AutoSize = false,
                Location = new Point(10, 30),
                Size = new Size(tipW - 20, SY(95)),
                BackColor = Color.Transparent
            };
            tipBox.Controls.Add(tipContent);
            tipBox.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(theme.Border))
                {
                    Rectangle rect = new Rectangle(0, 0, tipBox.Width - 1, tipBox.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };
            actionArea.Controls.Add(tipBox);

            // ==================== 最左侧:图标导航栏(Dock=Left,最后Add,Z-order最高在最左) ====================
            navPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = SX(60),
                BackColor = theme.NavBg
            };

            // 导航项
            CreateNavButton("检测", "⚡", SY(6), true);
            CreateNavButton("设置", "⚙", SY(82), false);
            CreateNavButton("关于", "···", SY(158), false);

            // 在navPanel和actionArea之间添加分隔线
            navSepRef = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = ColorBorder
            };

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
            outerWrap.Controls.Add(navSepRef);
            outerWrap.Controls.Add(navPanel);
            CreateBottomBar();
            outerWrap.Controls.Add(bottomBarRef);
            outerWrap.Controls.Add(titleBarPanel);

            // 右键菜单
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Font = new Font("Microsoft YaHei", SF(9f));
            cms.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable());
            cms.ShowImageMargin = true;
            cms.ShowCheckMargin = false;
            cms.ShowItemToolTips = false;

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

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("从剪贴板粘贴链接", icoPaste, (s, e) => PasteFromClipboard());
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
            cms.Items.Add(pasteItem);

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
            cms.Items.Add(detectMenuItem);

            // 排序子菜单
            ToolStripMenuItem sortMenuItem = new ToolStripMenuItem("排序", icoSort);
            sortMenuItem.DropDownItems.Add("按名称排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按延迟排序", null, (s, e) => { allChannels.Sort((a, b) => { int ta = ParseSpeed(a.Speed), tb = ParseSpeed(b.Speed); return ta.CompareTo(tb); }); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按状态排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Status, b.Status, StringComparison.Ordinal)); RefreshGrid(); });
            sortMenuItem.DropDownItems.Add("按分组排序", null, (s, e) => { allChannels.Sort((a, b) => string.Compare(a.Group, b.Group, StringComparison.Ordinal)); RefreshGrid(); });
            cms.Items.Add(sortMenuItem);

            // 播放子菜单
            ToolStripMenuItem playMenuItem = new ToolStripMenuItem("播放", icoPlay);
            playMenuItem.DropDownItems.Add("第三方播放器", null, (s, e) => { if (dgvData.SelectedRows.Count > 0) { string u = dgvData.SelectedRows[0].Cells[1].Value?.ToString(); if (!string.IsNullOrWhiteSpace(u)) PlayChannelCustom(u); } });
            playMenuItem.DropDownItems.Add(new ToolStripSeparator());
            playMenuItem.DropDownItems.Add("设置第三方播放器路径...", null, (s, e) => SetCustomPlayerPath());
            cms.Items.Add(playMenuItem);

            cms.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem copyItem = new ToolStripMenuItem("复制链接", icoCopy, (s, e) => CopyLink());
            copyItem.ShortcutKeyDisplayString = "Ctrl+C";
            cms.Items.Add(copyItem);

            ToolStripMenuItem copyAllItem = new ToolStripMenuItem("复制所有链接", icoCopyAll, (s, e) => CopyAllLinks());
            copyAllItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
            cms.Items.Add(copyAllItem);

            ToolStripMenuItem selectAllItem = new ToolStripMenuItem("全选", icoSelectAll, (s, e) => SelectAllRows());
            selectAllItem.ShortcutKeyDisplayString = "Ctrl+A";
            cms.Items.Add(selectAllItem);

            ToolStripMenuItem renameItem = new ToolStripMenuItem("重命名", icoRename, (s, e) => BeginRenameSelected());
            renameItem.ShortcutKeyDisplayString = "F2";
            cms.Items.Add(renameItem);

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除此行", icoDelete, (s, e) => DeleteRow());
            deleteItem.ShortcutKeyDisplayString = "Del";
            cms.Items.Add(deleteItem);

            ToolStripMenuItem detailItem = new ToolStripMenuItem("查看详情", icoInfo, (s, e) => ViewDetails());
            detailItem.ShortcutKeyDisplayString = "Enter";
            cms.Items.Add(detailItem);

            cms.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem clearInvalidItem = new ToolStripMenuItem("清空无效链接", icoClearInv, (s, e) => ClearInvalidLinks());
            cms.Items.Add(clearInvalidItem);

            ToolStripMenuItem clearAllItem = new ToolStripMenuItem("清空所有列表", icoClearAll, (s, e) => ClearAllLinks());
            cms.Items.Add(clearAllItem);

            dgvData.ContextMenuStrip = cms;

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
                this.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(100);
                    dgvData.PerformLayout();
                    dgvData.Invalidate();
                }));
            };

            this.FormClosing += (s, e) =>
            {
                KillRunningPlayer();
            };

            UpdateStatusBar();
            ApplyTheme();
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
        /// 创建导航按钮项
        /// </summary>
        private void CreateNavButton(string text, string icon, int top, bool selected)
        {
            Panel navItem = new Panel
            {
                Size = new Size(SX(60), SY(76)),
                Location = new Point(0, top),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Tag = text
            };

            Label iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Symbol", SF(16f)),
                ForeColor = selected ? ColorNavSelected : ColorNavNormal,
                AutoSize = false,
                Size = new Size(SX(60), SY(36)),
                Location = new Point(0, SY(4)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            Label textLbl = new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", SF(9f)),
                ForeColor = selected ? ColorNavSelected : ColorNavNormal,
                AutoSize = false,
                Size = new Size(SX(60), SY(22)),
                Location = new Point(0, SY(48)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            navItem.Controls.Add(iconLbl);
            navItem.Controls.Add(textLbl);
            navPanel.Controls.Add(navItem);

            navItem.Click += (s, e) => SelectNavItem(text);
            iconLbl.Click += (s, e) => SelectNavItem(text);
            textLbl.Click += (s, e) => SelectNavItem(text);
        }

        /// <summary>
        /// 选中导航项（切换视图）
        /// </summary>
        private void SelectNavItem(string name)
        {
            foreach (Control c in navPanel.Controls)
            {
                if (c is Panel item && item.Tag is string tag)
                {
                    bool isSelected = (tag == name);
                    foreach (Control child in item.Controls)
                    {
                        if (child is Label lbl)
                            lbl.ForeColor = isSelected ? ColorNavSelected : ColorNavNormal;
                    }
                }
            }

            currentView = name;
            SwitchView(name);
        }

        private void SwitchView(string name)
        {
            bool isDetect = (name == "检测");
            bool isSettings = (name == "设置");
            bool isAbout = (name == "关于");

            if (statusBarRef != null) statusBarRef.Visible = isDetect;
            if (searchPanelRef != null) searchPanelRef.Visible = isDetect;
            if (gridContainerRef != null) gridContainerRef.Visible = isDetect;

            if (actionArea != null) actionArea.Visible = isDetect;
            if (actionSepRef != null) actionSepRef.Visible = isDetect;
            if (navSepRef != null) navSepRef.Visible = isDetect;

            if (actionArea != null)
            {
                foreach (Control c in actionArea.Controls)
                    c.Visible = isDetect;
            }

            if (isSettings && settingsPanel == null)
                BuildSettingsPanel();
            if (isAbout && aboutPanel == null)
                BuildAboutPanel();

            if (settingsPanel != null)
            {
                settingsPanel.Visible = isSettings;
                if (isSettings)
                {
                    LayoutFillPanel(settingsPanel);
                    settingsPanel.BringToFront();
                }
            }
            if (aboutPanel != null)
            {
                aboutPanel.Visible = isAbout;
                if (isAbout)
                {
                    LayoutFillPanel(aboutPanel);
                    aboutPanel.BringToFront();
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

        private void UpdateScrollBarTheme(Control parent)
        {
            if (parent == null) return;
            bool isDark = (theme == AppTheme.Dark);
            string themeName = isDark ? "DarkMode_Explorer" : null;
            int darkMode = isDark ? 1 : 0;
            foreach (Control c in parent.Controls)
            {
                if (c is ScrollableControl || c is Panel || c is DataGridView)
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

        private int SortValue(ChannelInfo ch, string colName)
        {
            return 0;
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
            using (Font tagFont = new Font("Microsoft YaHei", SF(6.7f)))
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
            using (Font btnFont = new Font("Microsoft YaHei", SF(6.7f)))
            {
                TextRenderer.DrawText(g, text, btnFont, rect, foreColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        private Image CreateMenuIcon(string iconType, Color color)
        {
            int s = 16;
            Bitmap bmp = new Bitmap(s, s);
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
                        case "sub":
                            Point[] arrow = { new Point(6, 4), new Point(11, 8), new Point(6, 12) };
                            g.FillPolygon(whiteB, arrow);
                            break;
                    }
                }
            }
            return bmp;
        }

        private class MenuColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => Color.FromArgb(200, 200, 205);
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => Color.FromArgb(230, 225, 245);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 225, 245);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 225, 245);
            public override Color MenuStripGradientBegin => Color.White;
            public override Color MenuStripGradientEnd => Color.White;
            public override Color ToolStripBorder => Color.FromArgb(200, 200, 205);
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color SeparatorDark => Color.FromArgb(220, 220, 225);
            public override Color SeparatorLight => Color.Transparent;
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
                using (Font headerFont = dgvData.ColumnHeadersDefaultCellStyle.Font ?? new Font("Microsoft YaHei", SF(9f)))
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

                    int r = e.RowIndex;

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

                    if (_flashRow == r)
                    {
                        if (_flashCount % 2 == 0)
                        {
                            if (_flashBtn == 0) { copyBg = Color.White; copyFg = theme.CopyBtnBg; }
                            else { playBg = Color.White; playFg = theme.PlayBtnBg; }
                        }
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
            }
        }

        /// <summary>
        /// 单元格点击：处理复制/播放双按钮
        /// </summary>
        private void DgvData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 7)
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

                dgvData.Rows[e.RowIndex].Selected = true;

                if (copyBtnRect.Contains(relX, relY))
                {
                    StartButtonFlash(e.RowIndex, 0);
                    CopyTextToClipboard(url, cellRect.Y + cellRect.Height / 2);
                }
                else if (playBtnRect.Contains(relX, relY))
                {
                    StartButtonFlash(e.RowIndex, 1);
                    PlayChannelFFplay(url);
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

        private void StartButtonFlash(int row, int btn)
        {
            _flashRow = row;
            _flashBtn = btn;
            _flashCount = 0;
            if (_btnFlashTimer == null)
            {
                _btnFlashTimer = new System.Windows.Forms.Timer { Interval = 80 };
                _btnFlashTimer.Tick += (s, args) =>
                {
                    _flashCount++;
                    int curRow = _flashRow;
                    if (_flashCount >= 4)
                    {
                        _flashRow = -1;
                        _flashBtn = -1;
                        _btnFlashTimer.Stop();
                    }
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
                    if (oldHoverRow >= 0) dgvData.InvalidateCell(7, oldHoverRow);
                    if (_hoverRow >= 0) dgvData.InvalidateCell(7, _hoverRow);
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
                    dgvData.InvalidateCell(7, oldRow);
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
        private int _flashRow = -1;
        private int _flashBtn = -1;
        private int _flashCount = 0;

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
                    MessageBox.Show("复制失败，剪贴板被占用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ShowCopyToast(string url, int? targetY = null)
        {
            if (_toastPanel == null)
            {
                _toastPanel = new Panel
                {
                    Size = new Size(SX(380), SY(60)),
                    BackColor = Color.FromArgb(60, 63, 70),
                    Visible = false
                };
                _toastPanel.Paint += (s, pe) =>
                {
                    Graphics g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = GetRoundedPath(_toastPanel.ClientRectangle, SX(10)))
                    {
                        _toastPanel.Region = new Region(path);
                        using (SolidBrush br = new SolidBrush(Color.FromArgb(60, 63, 70)))
                            g.FillPath(br, path);
                    }
                };

                Label lblIcon = new Label
                {
                    Text = "✓",
                    Font = new Font("Microsoft YaHei", SF(11f), FontStyle.Bold),
                    ForeColor = Color.FromArgb(46, 189, 96),
                    Location = new Point(SX(16), SY(8)),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _toastPanel.Controls.Add(lblIcon);

                Label lblMsg = new Label
                {
                    Text = "链接已复制",
                    Font = new Font("Microsoft YaHei", SF(8.5f), FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(SX(44), SY(6)),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                _toastPanel.Controls.Add(lblMsg);

                Label lblUrl = new Label
                {
                    Font = new Font("Consolas", SF(9f)),
                    ForeColor = Color.FromArgb(200, 203, 210),
                    Location = new Point(SX(44), SY(30)),
                    Size = new Size(SX(320), SY(22)),
                    BackColor = Color.Transparent,
                    AutoEllipsis = true,
                    Name = "lblUrlContent"
                };
                _toastPanel.Controls.Add(lblUrl);

                dgvData.Controls.Add(_toastPanel);
                _toastPanel.BringToFront();

                _toastTimer = new System.Windows.Forms.Timer { Interval = 2200 };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    _toastPanel.Visible = false;
                };
            }

            Label urlLbl = (Label)_toastPanel.Controls["lblUrlContent"];
            urlLbl.Text = url;

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
            else MessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            playMenu.Font = new Font("Microsoft YaHei", SF(9f));
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
                MessageBox.Show($"无法使用系统播放器打开链接：\n{ex.Message}", "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// FFplay播放
        /// </summary>
        private void KillRunningPlayer()
        {
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
            }
        }

        private void PlayChannelFFplay(string url)
        {
            try
            {
                KillRunningPlayer();

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
                _runningPlayer.StartInfo = new ProcessStartInfo
                {
                    FileName = playerPath,
                    Arguments = isFfplay ? $"-autoexit -v quiet -nostats \"{url}\"" : $"\"{url}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
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
            }
            catch
            {
                try { _runningPlayer = null; PlayChannelDefault(url); } catch { }
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
                    MessageBox.Show("未设置第三方播放器路径或文件不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetCustomPlayerPath();
                    return;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = customPlayerPath,
                    Arguments = $"\"{url}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"第三方播放器播放失败：\n{ex.Message}", "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show($"已设置第三方播放器：\n{customPlayerPath}", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                dgvData.Rows.Clear();
                string selectedGroup = cboGroup?.SelectedItem?.ToString() ?? "全部";
                string searchText = GetSearchText();

                foreach (var ch in allChannels)
                {
                    string chGroup = string.IsNullOrWhiteSpace(ch.Group) ? "未分组" : ch.Group;
                    bool matchGroup = selectedGroup == "全部" || chGroup == selectedGroup;
                    bool matchSearch = string.IsNullOrWhiteSpace(searchText) ||
                        ch.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ch.Url.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (matchGroup && matchSearch)
                    {
                        dgvData.Rows.Add(ch.Name, ch.Url, ch.Location, ch.Resolution, ch.Speed, chGroup, ch.Status, "");
                    }
                }
            }
            finally
            {
                dgvData.ResumeLayout();
                SendMessage(dgvData.Handle, WM_SETREDRAW, 1, 0);
                dgvData.Invalidate();
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
                    string msg = $"成功导入 {newCount} 个频道";
                    if (beforeCount > 0) msg += $"（追加到列表，总计 {totalCount} 个）";
                    if (dupCount > 0) msg += $"\n跳过重复链接 {dupCount} 个";
                    MessageBox.Show(msg, "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 开始/停止检测
        /// </summary>
        private async void BtnStartDetect_Click(object sender, EventArgs e)
        {
            if (isDetecting)
            {
                cts?.Cancel();
                btnStartDetect.Text = "⏺ 开始检测";
                btnStartDetect.BackColor = ColorPurple;
                isDetecting = false;
                return;
            }
            if (allChannels.Count == 0)
            {
                MessageBox.Show("请先导入频道数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            isDetecting = true;
            btnStartDetect.Text = "⏹ 停止检测";
            btnStartDetect.BackColor = ColorPink;
            await StartDetection();
            isDetecting = false;
            btnStartDetect.Text = "⏺ 开始检测";
            btnStartDetect.BackColor = ColorPurple;
        }

        /// <summary>
        /// 导出
        /// </summary>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (allChannels.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            int padLeft = 20;
            int padRight = 20;
            int gap = 30;
            int padY = (h - (int)lblDetected.Font.GetHeight()) / 2;

            lblDetected.Location = new Point(padLeft, padY);
            lblAvailable.Location = new Point(lblDetected.Right + gap, padY);

            int progTotalW = lblProgressText.Width + 6 + lblPercent.Width;
            int progX = w - padRight - progTotalW;
            lblProgressText.Location = new Point(progX, padY);
            lblPercent.Location = new Point(progX + lblProgressText.Width + 6, padY - 1);
        }

        private void ApplyTheme()
        {
            this.BackColor = theme.Border;
            if (outerWrap != null)
            {
                outerWrap.BackColor = theme.Border;
            }
            if (titleBarPanel != null)
            {
                titleBarPanel.BackColor = theme.Bg;
                if (lblTitleRef != null) lblTitleRef.ForeColor = theme.TextPrimary;
                Color titleBtnHover = theme.Name == "深色" ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235);
                Color titleBtnFg = theme.TextSecondary;
                if (btnThemeToggle != null)
                {
                    btnThemeToggle.BackColor = theme.Bg;
                    btnThemeToggle.FlatAppearance.MouseOverBackColor = titleBtnHover;
                    btnThemeToggle.Invalidate();
                }
                if (btnMin != null) { btnMin.BackColor = theme.Bg; btnMin.FlatAppearance.MouseOverBackColor = titleBtnHover; btnMin.Invalidate(); }
                if (btnMax != null) { btnMax.BackColor = theme.Bg; btnMax.FlatAppearance.MouseOverBackColor = titleBtnHover; btnMax.Invalidate(); }
                if (btnClose != null) { btnClose.BackColor = theme.Bg; btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35); btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30); btnClose.Invalidate(); }
                if (titleIconRef != null) titleIconRef.Invalidate();
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
            }
            if (navPanel != null)
            {
                navPanel.BackColor = theme.NavBg;
                foreach (Control c in navPanel.Controls) { ApplyThemeToControl(c); }
            }
            if (navSepRef != null) navSepRef.BackColor = theme.Border;
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
                dgvData.Invalidate();
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
                statusBarRef.Invalidate();
            }
            if (searchPanelRef != null)
            {
                searchPanelRef.BackColor = theme.BgAlt;
                foreach (Control c in searchPanelRef.Controls) { ApplyThemeToControl(c); }
                if (searchBoxHostRef != null) searchBoxHostRef.BackColor = theme.Surface;
                if (cboGroupHost != null) cboGroupHost.BackColor = theme.BgAlt;
                searchPanelRef.Invalidate();
            }
            if (gridContainerRef != null)
            {
                gridContainerRef.BackColor = theme.BgAlt;
            }
            if (cboGroupHost != null)
            {
                cboGroupHost.Invalidate();
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
                cboGroup.Invalidate();
            }
            if (emptyLabel != null) emptyLabel.ForeColor = theme.TextSecondary;
            if (searchPanelRef != null) searchPanelRef.Invalidate();

            // 重建设置/关于面板以应用新主题颜色
            if (settingsPanel != null)
            {
                bool wasVisible = settingsPanel.Visible;
                mainArea.Controls.Remove(settingsPanel);
                settingsPanel.Dispose();
                settingsPanel = null;
                if (wasVisible || currentView == "设置")
                {
                    _buildingSettings = true;
                    try { BuildSettingsPanel(); } finally { _buildingSettings = false; }
                    if (settingsPanel != null)
                    {
                        LayoutFillPanel(settingsPanel);
                        settingsPanel.Visible = true;
                        settingsPanel.BringToFront();
                    }
                }
            }
            if (aboutPanel != null)
            {
                bool wasVisible = aboutPanel.Visible;
                mainArea.Controls.Remove(aboutPanel);
                aboutPanel.Dispose();
                aboutPanel = null;
                if (wasVisible || currentView == "关于")
                {
                    BuildAboutPanel();
                    if (aboutPanel != null)
                    {
                        LayoutFillPanel(aboutPanel);
                        aboutPanel.Visible = true;
                        aboutPanel.BringToFront();
                    }
                }
            }

            LayoutStatusBar(statusBarRef);
            SelectNavItem(currentView);
            UpdateScrollBarTheme(mainArea);
            this.Invalidate(true);
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
                else if (lbl.Parent == navPanel)
                    lbl.ForeColor = lbl.Font.Bold ? theme.PrimaryDark : theme.TextSecondary;
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
                statusBarRef.PerformLayout();
                LayoutStatusBar(statusBarRef);
            }
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
                MessageBox.Show($"未找到包含 \"{keyword}\" 的频道", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"导入文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show("剪贴板为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show(msg, "粘贴成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"粘贴失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>
        /// 真实HTTP异步检测（并发检测，UI节流更新）
        /// </summary>
        private async System.Threading.Tasks.Task StartDetection()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;

            foreach (var ch in allChannels)
            {
                ch.Status = "未检测";
                ch.Speed = "";
                if (string.IsNullOrWhiteSpace(ch.Location))
                    ch.Location = ExtractLocationFromUrl(ch.Url);
                if (string.IsNullOrWhiteSpace(ch.Resolution))
                    ch.Resolution = "";
            }
            detectedCount = 0; availableCount = 0;
            RefreshGrid();
            UpdateStatusBar();

            int concurrency = Math.Min(detectConcurrency, allChannels.Count);
            int uiUpdateCounter = 0;
            var startTime = DateTime.Now;

            int uiRefreshNeeded = 0;
            System.Windows.Forms.Timer uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            uiRefreshTimer.Tick += (s, e) =>
            {
                if (Interlocked.Exchange(ref uiRefreshNeeded, 0) == 1)
                {
                    RefreshGrid();
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
                                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4095);
                                    using (var ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
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
                                                    if (snippet.Contains("#EXTM3U") || snippet.Contains("#EXTINF")) ok = true;
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
                                        else if (contentType.Contains("video") || contentType.Contains("flv") || contentType.Contains("mp4"))
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
                                        else if (contentType.Contains("octet-stream") || contentType.Contains("audio"))
                                        {
                                            ok = true;
                                        }
                                        speed = $"{sw.ElapsedMilliseconds}ms";
                                        if (string.IsNullOrEmpty(location))
                                            location = ExtractLocationFromUrl(ch.Url);
                                    }
                                    resp.Dispose();
                                }

                                if (ok && string.IsNullOrEmpty(resolution) && !string.IsNullOrEmpty(ffplayPath))
                                {
                                    resolution = await TryGetResolutionWithFfprobe(ch.Url, token);
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
                        catch (OperationCanceledException)
                        {
                            ch.Status = "不可用";
                            ch.Speed = "超时";
                            ch.Location = ExtractLocationFromUrl(ch.Url);
                            ch.Resolution = "";
                            Interlocked.Increment(ref detectedCount);
                            Interlocked.Increment(ref uiUpdateCounter);
                            Interlocked.Exchange(ref uiRefreshNeeded, 1);
                            return;
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
            }
            finally
            {
                uiRefreshTimer.Stop();
                uiRefreshTimer.Dispose();
            }
            RefreshGrid();
            UpdateStatusBar();
            UpdateEmptyState();
            if (!token.IsCancellationRequested)
                MessageBox.Show($"检测完成！\n已检测: {detectedCount}/{totalCount}\n可用: {availableCount}", "检测完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
                MessageBox.Show($"成功导出 {merged.Count} 条数据（已合并相同频道，共 {allChannels.Count} 个源）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
                MessageBox.Show($"成功导出 {allChannels.Count} 条数据（{merged.Count} 个频道，按名称分组）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
                        MessageBox.Show($"成功导出 {unique.Count} 条数据（已合并相同频道）", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        foreach (var ch in allChannels)
                            sw.WriteLine($"{ch.Name}|{ch.Url}|{ch.Location}|{ch.Resolution}|{ch.Speed}|{ch.Group}|{ch.Status}");
                        MessageBox.Show($"成功导出 {allChannels.Count} 条数据", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
            else MessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void CopyAllLinks()
        {
            if (allChannels.Count == 0)
            {
                MessageBox.Show("列表为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        }

        private void DeleteRow()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("确定删除选中行？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                    UpdateStatusBar(); UpdateEmptyState();
                }
            }
            else MessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ViewDetails()
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                var r = dgvData.SelectedRows[0];
                MessageBox.Show($"名称: {r.Cells[0].Value}\n链接: {r.Cells[1].Value}\n归属地: {r.Cells[2].Value}\n分辨率: {r.Cells[3].Value}\n响应速度: {r.Cells[4].Value}\n分组: {r.Cells[5].Value}\n状态: {r.Cells[6].Value}",
                    "频道详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else MessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            UpdateStatusBar(); UpdateEmptyState();
        }

        private void ClearAllLinks()
        {
            if (isDetecting)
            {
                MessageBox.Show("检测正在进行中，请先停止检测后再清空列表。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvData.Rows.Count == 0)
            {
                MessageBox.Show("列表为空，无需清空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            allChannels.Clear();
            dgvData.Rows.Clear();
            totalCount = 0; detectedCount = 0; availableCount = 0;
            UpdateGroupFilter();
            UpdateStatusBar(); UpdateEmptyState();
        }

        /// <summary>
        /// 构建内联设置页面
        /// </summary>
        private void BuildSettingsPanel()
        {
            settingsPanel = new Panel
            {
                BackColor = theme.BgAlt,
                Visible = false,
                Dock = DockStyle.Fill
            };

            Panel scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = theme.BgAlt
            };
            settingsPanel.Controls.Add(scrollHost);

            Panel content = new Panel
            {
                Width = 720,
                BackColor = Color.Transparent,
                Location = new Point(0, 40)
            };
            scrollHost.Controls.Add(content);
            scrollHost.Resize += (s, e) =>
            {
                int left = Math.Max(30, (scrollHost.ClientSize.Width - content.Width) / 2);
                content.Location = new Point(left, 40);
            };

            int y = 0;

            // 标题行：TV图标 + 文字（左对齐）
            Panel titlePanel = new Panel
            {
                BackColor = Color.Transparent,
                Height = 100,
                Width = content.Width
            };

            PictureBox tvIcon = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            tvIcon.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                const int sz = 100;
                float scale = sz / 64f;
                // 天线
                using (Pen pen = new Pen(Color.Gray, 2.5f * scale))
                {
                    g.DrawLine(pen, 22 * scale, 16 * scale, 29 * scale, 2 * scale);
                    g.DrawLine(pen, 42 * scale, 16 * scale, 35 * scale, 2 * scale);
                }
                g.FillEllipse(Brushes.Gray, 19 * scale, 12 * scale, 6 * scale, 6 * scale);
                g.FillEllipse(Brushes.Gray, 39 * scale, 12 * scale, 6 * scale, 6 * scale);
                // 电视机身
                using (GraphicsPath tvPath = RoundedRectPath(new Rectangle((int)(6 * scale), (int)(18 * scale), (int)(52 * scale), (int)(38 * scale)), (int)(5 * scale)))
                using (SolidBrush tvBrush = new SolidBrush(Color.FromArgb(75, 80, 90)))
                    g.FillPath(tvBrush, tvPath);
                // 屏幕
                using (SolidBrush screenBrush = new SolidBrush(Color.FromArgb(40, 140, 220)))
                    g.FillRectangle(screenBrush, new Rectangle((int)(11 * scale), (int)(23 * scale), (int)(42 * scale), (int)(26 * scale)));
                using (LinearGradientBrush lg = new LinearGradientBrush(new Rectangle((int)(11 * scale), (int)(23 * scale), (int)(42 * scale), (int)(13 * scale)),
                    Color.FromArgb(120, 200, 255), Color.FromArgb(40, 140, 220), 90f))
                    g.FillRectangle(lg, new Rectangle((int)(11 * scale), (int)(23 * scale), (int)(42 * scale), (int)(13 * scale)));
                using (SolidBrush hl = new SolidBrush(Color.FromArgb(80, Color.White)))
                    g.FillRectangle(hl, new Rectangle((int)(15 * scale), (int)(26 * scale), (int)(14 * scale), (int)(3 * scale)));
                // 底座
                g.FillRectangle(Brushes.DarkGray, 24 * scale, 56 * scale, 16 * scale, 4 * scale);
                g.FillRectangle(Brushes.Gray, 17 * scale, 60 * scale, 30 * scale, 4 * scale);
                // 按钮
                g.FillEllipse(Brushes.Gray, 47 * scale, 44 * scale, 4 * scale, 4 * scale);
                g.FillEllipse(Brushes.Yellow, 48 * scale, 38 * scale, 4 * scale, 4 * scale);
            };
            titlePanel.Controls.Add(tvIcon);

            Label lblAppTitle = new Label
            {
                Text = "Wtv工具箱 for Windows",
                Font = new Font("Microsoft YaHei", 20f, FontStyle.Bold),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lblAppTitle.Location = new Point(120, (100 - lblAppTitle.PreferredHeight) / 2 - 2);
            titlePanel.Controls.Add(lblAppTitle);

            titlePanel.Location = new Point(0, y);
            content.Controls.Add(titlePanel);
            y += 120;

            int labelW = 220;
            int rowH = 56;
            int inputH = 30;
            int ctrlX = labelW;

            void AddLabel(string text, int rowY)
            {
                Label lbl = new Label
                {
                    Text = text,
                    Font = new Font("Microsoft YaHei", 10.5f),
                    ForeColor = theme.TextPrimary,
                    Location = new Point(0, rowY + (rowH - inputH) / 2),
                    Size = new Size(labelW, inputH),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };
                content.Controls.Add(lbl);
            }

            void AddHint(string text, ref int curY)
            {
                Label hint = new Label
                {
                    Text = text,
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = Color.Red,
                    Location = new Point(ctrlX, curY + 2),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                content.Controls.Add(hint);
                curY += 26;
            }

            // 1. 切换检测引擎
            AddLabel("切换检测引擎", y);
            Panel enginePanel = new Panel { BackColor = Color.Transparent, Size = new Size(380, inputH), Location = new Point(ctrlX, y + (rowH - inputH) / 2) };
            RadioButton rbHttp = new RadioButton
            {
                Text = "HTTP",
                Location = new Point(0, 5),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10.5f),
                ForeColor = theme.TextPrimary,
                BackColor = Color.Transparent,
                Checked = detectEngine == "HTTP"
            };
            RadioButton rbFfmpeg = new RadioButton
            {
                Text = "FFMPEG",
                Location = new Point(200, 5),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 10.5f),
                ForeColor = theme.TextPrimary,
                BackColor = Color.Transparent,
                Checked = detectEngine == "FFMPEG"
            };
            enginePanel.Controls.Add(rbHttp);
            enginePanel.Controls.Add(rbFfmpeg);
            content.Controls.Add(enginePanel);
            rbHttp.CheckedChanged += (s, e) => { if (rbHttp.Checked) detectEngine = "HTTP"; };
            rbFfmpeg.CheckedChanged += (s, e) => { if (rbFfmpeg.Checked) detectEngine = "FFMPEG"; };
            y += rowH;
            AddHint("友情提示：HTTP 模式不支持分辨率检测", ref y);
            y += 10;

            // 2. 极速模式并发检测数量
            AddLabel("极速模式并发检测数量", y);
            NumericUpDown numConcur = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = Math.Min(20, Math.Max(1, detectConcurrency)),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.Surface,
                ForeColor = theme.TextPrimary,
                Font = new Font("Microsoft YaHei", 10.5f),
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(100, inputH),
                TextAlign = HorizontalAlignment.Center
            };
            numConcur.ValueChanged += (s, e) => { detectConcurrency = (int)numConcur.Value; };
            content.Controls.Add(numConcur);
            y += rowH;
            AddHint("友情提示：最大数量20，超过最大建议数量可能造成检测不准确、应用被卡顿", ref y);
            y += 10;

            // 3. 超时时间设置（秒）
            AddLabel("超时时间设置（秒）", y);
            NumericUpDown numTimeout = new NumericUpDown
            {
                Minimum = 2,
                Maximum = 60,
                Value = Math.Min(60, Math.Max(2, timeoutSeconds)),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.Surface,
                ForeColor = theme.TextPrimary,
                Font = new Font("Microsoft YaHei", 10.5f),
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(100, inputH),
                TextAlign = HorizontalAlignment.Center
            };
            numTimeout.ValueChanged += (s, e) => { timeoutSeconds = (int)numTimeout.Value; };
            content.Controls.Add(numTimeout);
            y += rowH + 6;

            // 4. 自动清除无效源
            AddLabel("自动清除无效源", y);
            ToggleSwitch swAutoClear = new ToggleSwitch
            {
                Checked = autoClearInvalid,
                OffText = "手动",
                OnText = "自动",
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(90, inputH)
            };
            swAutoClear.CheckedChanged += (s, e) => { autoClearInvalid = swAutoClear.Checked; };
            content.Controls.Add(swAutoClear);
            y += rowH;

            // 5. 检测列表持久化
            AddLabel("检测列表持久化", y);
            ToggleSwitch swPersist = new ToggleSwitch
            {
                Checked = persistList,
                OffText = "关闭",
                OnText = "是",
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(90, inputH)
            };
            swPersist.CheckedChanged += (s, e) => { persistList = swPersist.Checked; };
            content.Controls.Add(swPersist);
            y += rowH;

            // 6. IPv6
            AddLabel("IPv6", y);
            ToggleSwitch swIPv6 = new ToggleSwitch
            {
                Checked = enableIPv6,
                OffText = "关闭",
                OnText = "是",
                OnColor = Color.FromArgb(66, 133, 244),
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(90, inputH)
            };
            swIPv6.CheckedChanged += (s, e) => { enableIPv6 = swIPv6.Checked; };
            content.Controls.Add(swIPv6);
            y += rowH + 6;

            // 7. 自定义搜索地址
            AddLabel("自定义搜索地址", y);
            TextBox txtSearchUrl = new TextBox
            {
                Text = customSearchUrl,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.Surface,
                ForeColor = theme.TextPrimary,
                Font = new Font("Microsoft YaHei", 10.5f),
                Location = new Point(ctrlX, y + (rowH - inputH) / 2),
                Size = new Size(380, inputH)
            };
            txtSearchUrl.TextChanged += (s, e) => { customSearchUrl = txtSearchUrl.Text.Trim(); };
            content.Controls.Add(txtSearchUrl);
            y += rowH + 10;

            // 8. 第三方播放器（新增功能，保留）
            AddLabel("第三方播放器", y);
            int playerW = 380;
            int btnW = 80;
            int tbW = playerW - btnW - 10;
            Panel customP = new Panel { BackColor = Color.Transparent, Size = new Size(playerW, inputH), Location = new Point(ctrlX, y + (rowH - inputH) / 2) };
            TextBox txtCustom = new TextBox
            {
                Location = new Point(0, 0),
                Width = tbW,
                Height = inputH,
                Text = customPlayerPath,
                ReadOnly = true,
                Font = new Font("Microsoft YaHei", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.Surface,
                ForeColor = theme.TextSecondary
            };
            Button btnBrowseC = new Button
            {
                Text = "浏览...",
                Location = new Point(tbW + 10, 0),
                Width = btnW,
                Height = inputH,
                FlatStyle = FlatStyle.Flat,
                BackColor = theme.Surface,
                ForeColor = theme.TextPrimary,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei", 10.5f)
            };
            btnBrowseC.FlatAppearance.BorderColor = theme.Border;
            btnBrowseC.FlatAppearance.BorderSize = 1;
            StyleRoundButton(btnBrowseC, 5);
            btnBrowseC.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "可执行文件|*.exe|所有文件|*.*";
                    ofd.Title = "选择播放器exe (vlc.exe/mpv.exe/potplayer等)";
                    if (ofd.ShowDialog() == DialogResult.OK) { customPlayerPath = ofd.FileName; txtCustom.Text = customPlayerPath; }
                }
            };
            customP.Controls.Add(txtCustom);
            customP.Controls.Add(btnBrowseC);
            content.Controls.Add(customP);
            y += rowH;

            content.Height = y + 60;
            mainArea.Controls.Add(settingsPanel);
            settingsPanel.BringToFront();
            UpdateScrollBarTheme(settingsPanel);
        }

        /// <summary>
        /// 构建关于页面
        /// </summary>
        private void BuildAboutPanel()
        {
            aboutPanel = new Panel
            {
                BackColor = theme.BgAlt,
                Visible = false,
                Dock = DockStyle.Fill
            };

            Panel center = new Panel
            {
                Width = 560,
                BackColor = Color.Transparent
            };
            aboutPanel.Controls.Add(center);

            int y = 80;
            Label lblAboutTitle = new Label
            {
                Text = "wtv工具箱 pro",
                Font = new Font("Microsoft YaHei", 22f, FontStyle.Bold),
                ForeColor = theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, y)
            };
            center.Controls.Add(lblAboutTitle);
            y += 70;

            Label lblVersion = new Label
            {
                Text = "版本: 1.0.0",
                Font = new Font("Microsoft YaHei", 12f),
                ForeColor = theme.TextSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, y)
            };
            center.Controls.Add(lblVersion);
            y += 45;

            Label lblDesc = new Label
            {
                Text = "IPTV 直播源检测与管理工具",
                Font = new Font("Microsoft YaHei", 12f),
                ForeColor = theme.TextSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, y)
            };
            center.Controls.Add(lblDesc);

            aboutPanel.Resize += (s, e) =>
            {
                center.Location = new Point(Math.Max(20, (aboutPanel.ClientSize.Width - center.Width) / 2), 80);
            };

            mainArea.Controls.Add(aboutPanel);
            aboutPanel.BringToFront();
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
            bool isDark = theme.Name == "深色";
            Color GreenMain = isDark ? Color.FromArgb(70, 200, 110) : Color.FromArgb(46, 189, 96);
            Color GreenDark = isDark ? Color.FromArgb(55, 180, 95) : Color.FromArgb(39, 174, 86);
            Color GrayText = isDark ? theme.TextSecondary : Color.FromArgb(153, 153, 153);
            Color GrayLine = isDark ? theme.Border : Color.FromArgb(230, 232, 238);
            Color GrayBorder = isDark ? theme.Border : Color.FromArgb(200, 203, 210);
            Color DarkText = isDark ? theme.TextPrimary : Color.FromArgb(51, 51, 51);
            Color RedHighlight = isDark ? theme.ErrorColor : Color.FromArgb(231, 76, 60);
            Color LightBtnBg = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(248, 249, 250);
            Color InputBg = isDark ? theme.Surface : Color.White;
            Color InputFocusBorder = GreenMain;
            Color PanelBg = isDark ? theme.Bg : Color.White;
            Color StepLineGray = isDark ? Color.FromArgb(80, 80, 92) : Color.FromArgb(210, 213, 220);
            Color NumPadHover = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(235, 236, 240);
            Color NumPadDown = isDark ? Color.FromArgb(75, 75, 85) : Color.FromArgb(225, 226, 232);
            Color CloseHover = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(245, 245, 245);
            Color CloseDown = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(230, 230, 230);

            int DLG_W = 780;
            int DLG_H = 640;
            int CONTENT_PAD = 32;
            int INPUT_HEIGHT = 40;
            int BTN_HEIGHT = 42;
            Font BASE_FONT = new Font("Microsoft YaHei", 10.5f);
            Font TITLE_FONT = new Font("Microsoft YaHei", 15f, FontStyle.Bold);
            Font HINT_FONT = new Font("Microsoft YaHei", 11f);
            Font URL_FONT = new Font("Consolas", 11f);
            Font URL_SEL_FONT = new Font("Consolas", 11f, FontStyle.Bold | FontStyle.Underline);
            Font URL_BOLD_FONT = new Font("Consolas", 11f, FontStyle.Bold);
            Font BTN_FONT = new Font("Microsoft YaHei", 11f, FontStyle.Bold);

            Form dlg = new Form
            {
                Text = "扫源助手",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = PanelBg,
                ClientSize = new Size(DLG_W, DLG_H),
                Font = BASE_FONT,
                KeyPreview = true
            };

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
                        if (string.IsNullOrWhiteSpace(chName)) chName = "扫源导入";
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
                        result.Add(new ChannelInfo { Name = name, Url = url, Group = string.IsNullOrEmpty(pendingGroup) ? "扫源导入" : pendingGroup, Status = "未检测", Visible = true });
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
                    Font = new Font("Microsoft YaHei", 9.5f),
                    Renderer = new ToolStripProfessionalRenderer(new MenuColorTable())
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
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = GreenDark;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(34, 160, 76);
                btn.BackColor = GreenMain;
                btn.ForeColor = Color.White;
                btn.Font = BTN_FONT;
                btn.Cursor = Cursors.Hand;
                StyleRoundButton(btn, 8);
            }

            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = PanelBg
            };
            Label lblTitle = new Label
            {
                Text = "🔍 扫源助手",
                Font = TITLE_FONT,
                ForeColor = DarkText,
                Location = new Point(CONTENT_PAD, 14),
                AutoSize = true
            };
            titleBar.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 50),
                Location = new Point(DLG_W - 50, 0),
                ForeColor = GrayText,
                BackColor = PanelBg,
                Font = new Font("Microsoft YaHei", 13f),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = CloseHover;
            btnClose.FlatAppearance.MouseDownBackColor = CloseDown;
            btnClose.Click += (s, e) => dlg.Close();
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

            Panel sepTitle = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = GrayLine };

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

            Panel stepIndicator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = PanelBg
            };
            string[] stepLabelsArr = { "输入直播源地址", "选择扫描字段", "设置数值范围" };
            int stepCircleR = 14;

            stepIndicator.Paint += (s, pe) =>
            {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                int w = stepIndicator.ClientSize.Width;
                int h = stepIndicator.Height;

                float circleD = stepCircleR * 2;
                float circleY = 16;
                float lineY = circleY + stepCircleR;
                float sectionW = w / 3f;
                float[] circleCX = new float[3];
                for (int i = 0; i < 3; i++)
                    circleCX[i] = sectionW * i + sectionW / 2f;

                Font stepLblFont = new Font("Microsoft YaHei", 11f);
                Font stepLblFontBold = new Font("Microsoft YaHei", 11f, FontStyle.Bold);

                using (Pen linePen = new Pen(GrayLine, 2f))
                using (Pen greenPen = new Pen(GreenMain, 2f))
                using (Brush greenBrush = new SolidBrush(GreenMain))
                using (Brush whiteBrush = new SolidBrush(PanelBg))
                using (Brush grayBrush = new SolidBrush(GrayText))
                using (Font numFont = new Font("Microsoft YaHei", 10.5f, FontStyle.Bold))
                using (Font checkFont = new Font("Microsoft YaHei", 11f, FontStyle.Bold))
                using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    for (int seg = 0; seg < 2; seg++)
                    {
                        Pen p = (currentStep >= seg + 2) ? greenPen : linePen;
                        float x1 = circleCX[seg] + stepCircleR + 8;
                        float x2 = circleCX[seg + 1] - stepCircleR - 8;
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
                        SizeF lblSize = g.MeasureString(stepLabelsArr[i], lblF);
                        float lblX = cx - lblSize.Width / 2;
                        float lblY = circleY + circleD + 14;
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
            contentHost.Controls.Add(stepIndicator);

            Panel step1Panel = new Panel { Dock = DockStyle.Fill, BackColor = PanelBg };
            Panel step2Panel = new Panel { BackColor = PanelBg, Visible = false, AutoScroll = true };
            Panel step3Panel = new Panel { BackColor = PanelBg, Visible = false };

            stepContainer.Controls.Add(step2Panel);
            stepContainer.Controls.Add(step3Panel);
            stepContainer.Controls.Add(step1Panel);

            Label lblStep1Hint = new Label
            {
                Text = "直播源地址",
                Font = HINT_FONT,
                ForeColor = DarkText,
                Location = new Point(CONTENT_PAD, 28),
                AutoSize = true,
                BackColor = PanelBg
            };
            int hint1W = TextRenderer.MeasureText(lblStep1Hint.Text, lblStep1Hint.Font).Width;
            Label lblStep1Star = new Label
            {
                Text = "*",
                Font = HINT_FONT,
                ForeColor = RedHighlight,
                Location = new Point(CONTENT_PAD + hint1W + 3, 28),
                AutoSize = true,
                BackColor = PanelBg
            };
            step1Panel.Controls.Add(lblStep1Hint);
            step1Panel.Controls.Add(lblStep1Star);

            TextBox txtStep1Url = new TextBox
            {
                Location = new Point(CONTENT_PAD, 62),
                Width = DLG_W - CONTENT_PAD * 2,
                Height = INPUT_HEIGHT,
                Font = URL_FONT,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = InputBg
            };
            txtStep1Url.ContextMenuStrip = CreateInputContextMenu(txtStep1Url);
            Color phColor = isDark ? theme.TextSecondary : Color.FromArgb(170, 173, 180);
            bool phStep1Active = true;
            txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选扫描段";
            txtStep1Url.ForeColor = phColor;
            txtStep1Url.GotFocus += (s, e) =>
            {
                if (phStep1Active) { phStep1Active = false; txtStep1Url.Text = ""; txtStep1Url.ForeColor = DarkText; }
            };
            txtStep1Url.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtStep1Url.Text))
                { phStep1Active = true; txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选扫描段"; txtStep1Url.ForeColor = phColor; }
            };
            step1Panel.Controls.Add(txtStep1Url);

            Panel pnlSmartHint = new Panel
            {
                Location = new Point(CONTENT_PAD, 120),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 70),
                BackColor = theme.StatusTagBg,
                BorderStyle = BorderStyle.None
            };
            Label lblStep1SmartHint = new Label
            {
                Text = "💡 智能识别：输入标准URL进入向导模式；输入带 [起始-结束] 的地址直接生成\n（如 http://example.com/[1-100].m3u8）",
                Font = new Font("Microsoft YaHei", 10.5f),
                ForeColor = theme.SuccessColor,
                Location = new Point(16, 12),
                AutoSize = false,
                Size = new Size(DLG_W - CONTENT_PAD * 2 - 32, 50),
                BackColor = theme.StatusTagBg
            };
            pnlSmartHint.Controls.Add(lblStep1SmartHint);
            pnlSmartHint.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(theme.StatusTagBorder, 2.5f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, 0, pnlSmartHint.Height);
                }
            };
            step1Panel.Controls.Add(pnlSmartHint);

            Label lblStep2Hint = new Label
            {
                Text = "请选择要扫描的字符段",
                Font = HINT_FONT,
                ForeColor = DarkText,
                Location = new Point(CONTENT_PAD, 28),
                AutoSize = true,
                BackColor = PanelBg
            };
            int hint2W = TextRenderer.MeasureText(lblStep2Hint.Text, lblStep2Hint.Font).Width;
            Label lblStep2Star = new Label
            {
                Text = "*",
                Font = HINT_FONT,
                ForeColor = RedHighlight,
                Location = new Point(CONTENT_PAD + hint2W + 3, 28),
                AutoSize = true,
                BackColor = PanelBg
            };
            step2Panel.Controls.Add(lblStep2Hint);
            step2Panel.Controls.Add(lblStep2Star);

            int step2ContentTop = 62;
            Panel segListContainer = new Panel
            {
                Location = new Point(CONTENT_PAD, step2ContentTop),
                Width = DLG_W - CONTENT_PAD * 2,
                Height = 300,
                BackColor = PanelBg,
                AutoScroll = true
            };
            step2Panel.Controls.Add(segListContainer);

            int numPanelW = 200;
            int numPanelY = 58;
            int pFromX = CONTENT_PAD + 40;
            int pToX = DLG_W - CONTENT_PAD - numPanelW - 40;

            Label lblStep3From = new Label
            {
                Text = "起始数字",
                Font = new Font("Microsoft YaHei", 11f),
                ForeColor = DarkText,
                Location = new Point(pFromX, 22),
                AutoSize = false,
                Size = new Size(numPanelW, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = PanelBg
            };
            step3Panel.Controls.Add(lblStep3From);
            Label lblStep3To = new Label
            {
                Text = "结束数字",
                Font = new Font("Microsoft YaHei", 11f),
                ForeColor = DarkText,
                Location = new Point(pToX, 22),
                AutoSize = false,
                Size = new Size(numPanelW, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = PanelBg
            };
            step3Panel.Controls.Add(lblStep3To);

            TextBox txtFrom = null, txtTo = null;
            Panel pnlTextOptions = null;
            CheckedListBox clstTextCandidates = null;
            List<string> selectedTextValues = null;

            Panel CreateNumPanel(int x, long initialVal, out TextBox outTextBox)
            {
                Panel p = new Panel
                {
                    Location = new Point(x, numPanelY),
                    Width = numPanelW,
                    Height = INPUT_HEIGHT,
                    BackColor = InputBg,
                    BorderStyle = BorderStyle.FixedSingle
                };
                int btnW = INPUT_HEIGHT;
                Button btnMinus = new Button
                {
                    Text = "−",
                    Size = new Size(btnW, INPUT_HEIGHT - 2),
                    Location = new Point(0, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = LightBtnBg,
                    ForeColor = DarkText,
                    Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnMinus.FlatAppearance.BorderSize = 0;
                btnMinus.FlatAppearance.MouseOverBackColor = NumPadHover;
                btnMinus.FlatAppearance.MouseDownBackColor = NumPadDown;
                TextBox tb = new TextBox
                {
                    Text = initialVal.ToString(),
                    Location = new Point(btnW + 2, (INPUT_HEIGHT - 24) / 2),
                    Width = numPanelW - btnW * 2 - 6,
                    Height = 24,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Microsoft YaHei", 12f),
                    ForeColor = DarkText,
                    BackColor = InputBg,
                    TextAlign = HorizontalAlignment.Center
                };
                tb.ContextMenuStrip = CreateInputContextMenu(tb);
                Button btnPlus = new Button
                {
                    Text = "+",
                    Size = new Size(btnW, INPUT_HEIGHT - 2),
                    Location = new Point(numPanelW - btnW - 1, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = LightBtnBg,
                    ForeColor = DarkText,
                    Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnPlus.FlatAppearance.BorderSize = 0;
                btnPlus.FlatAppearance.MouseOverBackColor = NumPadHover;
                btnPlus.FlatAppearance.MouseDownBackColor = NumPadDown;

                btnMinus.Click += (s, e) =>
                {
                    long v;
                    if (long.TryParse(tb.Text, out v) && v > 0) { v--; tb.Text = v.ToString(); }
                    else { tb.Text = "0"; }
                };
                btnPlus.Click += (s, e) =>
                {
                    long v;
                    if (long.TryParse(tb.Text, out v) && v < 9999999999L) { v++; tb.Text = v.ToString(); }
                    else if (!long.TryParse(tb.Text, out v)) { tb.Text = "0"; }
                };
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

            Panel pFrom = CreateNumPanel(pFromX, fromVal, out txtFrom);
            step3Panel.Controls.Add(pFrom);
            Panel pTo = CreateNumPanel(pToX, toVal, out txtTo);
            step3Panel.Controls.Add(pTo);

            Panel pnlRangeHint = new Panel
            {
                Location = new Point(CONTENT_PAD, 114),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 48),
                BackColor = theme.TipBg,
                BorderStyle = BorderStyle.None
            };
            Label lblStep3RangeHint = new Label
            {
                Text = "⚠ 最大扫描范围为10000，范围过大可能导致检测时间过长",
                Font = new Font("Microsoft YaHei", 10.5f),
                ForeColor = theme.WarnColor,
                Location = new Point(14, 8),
                AutoSize = false,
                Size = new Size(DLG_W - CONTENT_PAD * 2 - 28, 34),
                BackColor = theme.TipBg
            };
            pnlRangeHint.Controls.Add(lblStep3RangeHint);
            pnlRangeHint.Paint += (s, pe) =>
            {
                using (Pen p = new Pen(theme.WarnColor, 2.5f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, 0, pnlRangeHint.Height);
                }
            };
            step3Panel.Controls.Add(pnlRangeHint);

            Label lblStep3Preview = new Label
            {
                Text = "",
                Font = URL_FONT,
                ForeColor = GreenMain,
                Location = new Point(CONTENT_PAD, 176),
                AutoSize = false,
                Size = new Size(DLG_W - CONTENT_PAD * 2, 60),
                BackColor = theme.StatusTagBg,
                Visible = false,
                Padding = new Padding(10, 8, 10, 8)
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
                Font = new Font("Microsoft YaHei", 11f),
                ForeColor = DarkText,
                Location = new Point(0, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlTextOptions.Controls.Add(lblTextOptTitle);
            clstTextCandidates = new CheckedListBox
            {
                Location = new Point(0, 30),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 140),
                Font = new Font("Microsoft YaHei", 10f),
                BackColor = InputBg,
                ForeColor = DarkText,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };
            pnlTextOptions.Controls.Add(clstTextCandidates);
            FlowLayoutPanel pnlTextBtns = new FlowLayoutPanel
            {
                Location = new Point(0, 176),
                Size = new Size(DLG_W - CONTENT_PAD * 2, 32),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            Button btnTextCheckAll = new Button
            {
                Text = "全选",
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = LightBtnBg,
                ForeColor = DarkText,
                Font = new Font("Microsoft YaHei", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnTextCheckAll.FlatAppearance.BorderSize = 1;
            btnTextCheckAll.FlatAppearance.BorderColor = GrayBorder;
            btnTextCheckAll.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(235, 236, 240);
            btnTextCheckAll.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(225, 226, 232);
            btnTextCheckAll.Click += (s, e) =>
            {
                for (int i = 0; i < clstTextCandidates.Items.Count; i++)
                    clstTextCandidates.SetItemChecked(i, true);
            };
            Button btnTextUncheckAll = new Button
            {
                Text = "全不选",
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = LightBtnBg,
                ForeColor = DarkText,
                Font = new Font("Microsoft YaHei", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnTextUncheckAll.FlatAppearance.BorderSize = 1;
            btnTextUncheckAll.FlatAppearance.BorderColor = GrayBorder;
            btnTextUncheckAll.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(55, 55, 65) : Color.FromArgb(235, 236, 240);
            btnTextUncheckAll.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(65, 65, 75) : Color.FromArgb(225, 226, 232);
            btnTextUncheckAll.Click += (s, e) =>
            {
                for (int i = 0; i < clstTextCandidates.Items.Count; i++)
                    clstTextCandidates.SetItemChecked(i, false);
            };
            pnlTextBtns.Controls.Add(btnTextCheckAll);
            pnlTextBtns.Controls.Add(btnTextUncheckAll);
            pnlTextOptions.Controls.Add(pnlTextBtns);
            step3Panel.Controls.Add(pnlTextOptions);

            Panel sepBottom = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = GrayLine };

            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = PanelBg,
                Padding = new Padding(CONTENT_PAD, 0, CONTENT_PAD, 0)
            };

            Button btnPrev = new Button
            {
                Text = "← 上一步 (B)",
                Size = new Size(150, BTN_HEIGHT),
                Location = new Point(CONTENT_PAD, 18),
                Visible = false
            };
            StyleGreenButton(btnPrev);
            bottomBar.Controls.Add(btnPrev);

            Button btnAction = new Button
            {
                Text = "下一步 (N) →",
                Size = new Size(150, BTN_HEIGHT),
                Location = new Point(DLG_W - CONTENT_PAD - 150, 18)
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
                    btnAction.Text = "开始扫源 (Enter)";
                else
                    btnAction.Text = (currentStep == 3) ? "开始扫源 (Enter)" : "下一步 (N) →";
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
                        Text = "❌ 未找到可扫描的字段，请检查URL格式（支持数字段如/123/、频道名如cctv1、分辨率如1080p等）",
                        ForeColor = RedHighlight,
                        Font = new Font("Microsoft YaHei", 10.5f),
                        Location = new Point(0, 8),
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

                int itemY = 4;
                int itemH = 34;
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
                        Margin = new Padding(0),
                        Padding = new Padding(0),
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
                        Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
                    };
                    rowFlow.Controls.Add(lblBefore);

                    Panel radioCircle = new Panel
                    {
                        Size = new Size(radioSize, radioSize),
                        BackColor = Color.Transparent,
                        Tag = i,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(4, (itemH - radioSize) / 2, 4, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                            Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                        Margin = new Padding(0, (itemH - 22) / 2, 0, 0)
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
                    itemY += itemH + 2;
                }

                adjPanel = new Panel
                {
                    Location = new Point(0, itemY + 4),
                    Width = segListContainer.Width - 20,
                    Height = 38,
                    BackColor = Color.Transparent,
                    Visible = false
                };

                btnLeftExpand = new Button { Text = "◀ {", Size = new Size(50, 30), Location = new Point(0, 2), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = new Font("Microsoft YaHei", 8.5f), Cursor = Cursors.Hand };
                btnLeftShrink = new Button { Text = "{ ▶", Size = new Size(50, 30), Location = new Point(54, 2), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = new Font("Microsoft YaHei", 8.5f), Cursor = Cursors.Hand };
                btnRightShrink = new Button { Text = "} ◀", Size = new Size(50, 30), Location = new Point(108, 2), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = new Font("Microsoft YaHei", 8.5f), Cursor = Cursors.Hand };
                btnRightExpand = new Button { Text = "} ▶", Size = new Size(50, 30), Location = new Point(162, 2), FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = DarkText, Font = new Font("Microsoft YaHei", 8.5f), Cursor = Cursors.Hand };
                btnSelectAll = new Button { Text = "全选本段", Size = new Size(72, 30), Location = new Point(220, 2), FlatStyle = FlatStyle.Flat, BackColor = bracketActiveColor, ForeColor = Color.White, Font = new Font("Microsoft YaHei", 8.5f), Cursor = Cursors.Hand };

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
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = theme.TextSecondary,
                    AutoSize = false,
                    Location = new Point(300, 6),
                    Size = new Size(adjPanel.Width - 300, 24),
                    BackColor = Color.Transparent
                };
                adjPanel.Controls.Add(lblSelInfo);

                segListContainer.Controls.Add(adjPanel);
                itemY = adjPanel.Bottom;

                Label lblHint = new Label
                {
                    Text = "💡 点击单选按钮选择要扫描的字段，绿色●为当前选中。数字段可用 ◀{ {▶ }◀ }▶ 按钮调整大括号框选部分位数（长数字可选子范围），含前导零将保持补零。频道/分辨率段将提供候选列表供选择。",
                    Font = new Font("Microsoft YaHei", 9f),
                    ForeColor = theme.SuccessColor,
                    Location = new Point(0, itemY + 6),
                    AutoSize = false,
                    Size = new Size(segListContainer.Width - 20, 56),
                    BackColor = theme.StatusTagBg,
                    Padding = new Padding(8, 4, 8, 4)
                };
                segListContainer.Controls.Add(lblHint);
                itemY = lblHint.Bottom + 6;

                CheckBox chkMultiRes = new CheckBox
                {
                    Text = "📐 同时扫描多个分辨率（1080p/720p/540p/480p/360p）",
                    Font = new Font("Microsoft YaHei", 9.5f),
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
                    error = "扫描范围过大，请控制在10000以内";
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
                        channels.Add(new ChannelInfo { Name = "源" + (channels.Count + 1), Url = url, Group = "扫源", Status = "未检测", Visible = true });
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
                    MessageBox.Show("生成的源数量超过10000，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        channels.Add(new ChannelInfo { Name = resName, Url = resUrl, Group = "扫源", Status = "未检测", Visible = true });
                        if (channels.Count > 10000) break;
                    }
                }
                else
                {
                    channels.Add(new ChannelInfo { Name = baseName, Url = baseUrl, Group = "扫源", Status = "未检测", Visible = true });
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
                            var dr = MessageBox.Show(
                                string.Format("检测到 {0} 条频道列表（名称+地址格式），是否直接导入到检测窗口？\n\n点击「是」直接导入全部频道\n点击「否」使用第一条URL进行扫源", parsedList.Count),
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
                        MessageBox.Show("请输入直播源地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        MessageBox.Show(bktErr, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!Uri.IsWellFormedUriString(input, UriKind.Absolute))
                    {
                        MessageBox.Show("请输入有效的直播源地址（如 http://example.com/1.m3u8）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        MessageBox.Show("未找到可扫描的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (selectedSegIndex < 0 || selectedSegIndex >= segs.Count)
                    {
                        MessageBox.Show("请选择要扫描的字段（点击RadioButton或字段文本）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show("请输入有效的范围值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show("起始值必须小于结束值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        long estCount = tv - fv + 1;
                        if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
                            estCount *= ResolutionList.Length;
                        if (estCount > 10000)
                        {
                            MessageBox.Show("扫描范围过大，预计生成超过10000个源，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show("请至少选择一个选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        long estCount = selectedTextValues.Count;
                        if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
                            estCount *= ResolutionList.Length;
                        if (estCount > 10000)
                        {
                            MessageBox.Show("选择过多，预计生成超过10000个源，请减少选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        HashSet<string> existingUrls = new HashSet<string>(allChannels.Select(c => c.Url.ToLowerInvariant()));
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
                        UpdateGroupFilter();
                        RefreshGrid();
                        UpdateEmptyState();
                        UpdateStatusBar();
                    }));
                }
            };
            opacityForm.Show(this);
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
}
