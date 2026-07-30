using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using IPTVLiveChecker.Resources;
using Microsoft.Win32;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain : Form
{
	private enum ScanSegType
	{
		Number,
		CctvChannel,
		PayChannel,
		WsChannel,
		MovieChannel,
		Resolution
	}

	private class ScanSegInfo
	{
		public ScanSegType Type;

		public int GlobalStart;

		public int GlobalEnd;

		public int PathStart;

		public string OriginalText;

		public string Label;

		public List<string> Candidates = new List<string>();
	}

	private class RoundedMenuRenderer : ToolStripProfessionalRenderer
	{
		private readonly bool _isDark;

		private readonly int _radius = 8;

		public RoundedMenuRenderer(AppTheme theme)
			: base(new MenuColorTable(theme))
		{
			_isDark = DrawingUtils.IsDarkColor(theme.Bg);
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			using GraphicsPath path = GetRoundedRectangle(e.AffectedBounds, _radius);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using Pen pen = new Pen(_isDark ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 205));
			e.Graphics.DrawPath(pen, path);
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			using GraphicsPath path = GetRoundedRectangle(e.AffectedBounds, _radius);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush = new SolidBrush(_isDark ? Color.FromArgb(45, 45, 55) : Color.White);
			e.Graphics.FillPath(brush, path);
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			if (!e.Item.Selected)
			{
				e.Item.BackColor = Color.Transparent;
				return;
			}
			Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
			using GraphicsPath path = GetRoundedRectangle(rect, _radius);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush = new SolidBrush(_isDark ? Color.FromArgb(70, 70, 85) : Color.FromArgb(230, 225, 245));
			e.Graphics.FillPath(brush, path);
		}

		private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
		{
			return DrawingUtils.RoundedRect(rect, radius);
		}
	}

	private enum SearchMode
	{
		Browser,
		WebView2
	}

	private class AppConfig
	{
		public RegionConfig Window = new RegionConfig();

		public RegionConfig TitleBar = new RegionConfig();

		public RegionConfig Navigation = new RegionConfig();

		public RegionConfig SearchPanel = new RegionConfig();

		public RegionConfig ActionArea = new RegionConfig();

		public RegionConfig DataGrid = new RegionConfig();

		public RegionConfig StatusBar = new RegionConfig();

		public RegionConfig Pill = new RegionConfig();

		public RegionConfig DataGridButton = new RegionConfig();

		public RegionConfig Dialog = new RegionConfig();

		public RegionConfig StepIndicator = new RegionConfig();

		public RegionConfig Toast = new RegionConfig();

		public RegionConfig EmptyState = new RegionConfig();

		public RegionConfig ContextMenu = new RegionConfig();

		public RegionConfig ToggleSwitch = new RegionConfig();

		public void Initialize(float dpiScale)
		{
			Window.Layout.Initialize(new LayoutDefaults
			{
				Width = 0,
				Height = 0,
				MinWidth = 1280,
				MinHeight = 800,
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
			TitleBar.Layout.Initialize(new LayoutDefaults
			{
				Height = 32,
				Left = 12,
				IconSize = 18,
				IconGap = 8,
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
			Navigation.Layout.Initialize(new LayoutDefaults
			{
				Width = 48,
				IconSize = 32,
				Gap = 70,
				Top = 6,
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
			SearchPanel.Layout.Initialize(new LayoutDefaults
			{
				Height = 46,
				Left = 12,
				Padding = 26,
				Gap = 98,
				IconGap = 298,
				BtnWidth = 130,
				BtnHeight = 26,
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
			ActionArea.Layout.Initialize(new LayoutDefaults
			{
				Width = 180,
				Padding = 10,
				BtnHeight = 36,
				BtnGap = 8,
				Gap = 130,
				Top = 8,
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
			DataGrid.Layout.Initialize(new LayoutDefaults
			{
				HeaderHeight = 36,
				RowHeight = 30,
				Padding = 10,
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
			StatusBar.Layout.Initialize(new LayoutDefaults
			{
				Height = 26,
				Padding = 12,
				Gap = 10,
				BtnHeight = 38,
				IconSize = 6,
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
			Pill.Layout.Initialize(new LayoutDefaults
			{
				Height = 26,
				CornerRadius = 13,
				Padding = 12
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
			DataGridButton.Layout.Initialize(new LayoutDefaults
			{
				Height = 22,
				Width = 60,
				CornerRadius = 4,
				Gap = 4
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
			Dialog.Layout.Initialize(new LayoutDefaults
			{
				Width = 900,
				Height = 750,
				CornerRadius = 12,
				TitleHeight = 56,
				Padding = 32,
				BtnWidth = 150,
				BtnHeight = 42,
				BtnGap = 16,
				Bottom = 28
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
			StepIndicator.Layout.Initialize(new LayoutDefaults
			{
				Height = 105,
				IconSize = 14,
				Gap = 2,
				IconGap = 8
			});
			StepIndicator.Color.Initialize(new ColorDefaults
			{
				Background = Color.Transparent,
				Primary = Color.FromArgb(138, 43, 226),
				Border = Color.FromArgb(60, 60, 60),
				Foreground = Color.FromArgb(240, 240, 240),
				ForegroundSecondary = Color.FromArgb(160, 160, 160)
			});
			Toast.Layout.Initialize(new LayoutDefaults
			{
				Width = 280,
				Height = 50,
				CornerRadius = 8,
				Right = 20,
				Bottom = 60,
				IconSize = 24,
				IconGap = 12,
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
			EmptyState.Layout.Initialize(new LayoutDefaults
			{
				Width = 200,
				Height = 140,
				IconSize = 64,
				IconGap = 16
			});
			EmptyState.Color.Initialize(new ColorDefaults
			{
				Background = Color.Transparent,
				Foreground = Color.FromArgb(160, 160, 160),
				Error = Color.FromArgb(244, 67, 54)
			});
			ContextMenu.Layout.Initialize(new LayoutDefaults
			{
				BtnHeight = 28,
				Padding = 4,
				IconSize = 16,
				IconGap = 8
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
			ToggleSwitch.Layout.Initialize(new LayoutDefaults
			{
				Width = 70,
				Height = 24,
				IconSize = 18
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

	private delegate bool WndEnumProc(IntPtr hWnd, IntPtr lParam);

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

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

	private struct POINT
	{
		public int x = 0;

		public int y = 0;

		public POINT()
		{
		}
	}

	private static readonly Dictionary<string, string[]> CctvChannelMap = new Dictionary<string, string[]> { 
	{
		"cctv",
		new string[20]
		{
			"cctv1", "cctv2", "cctv3", "cctv4", "cctv5", "cctv5p", "cctv6", "cctv7", "cctv8", "cctv9",
			"cctv10", "cctv11", "cctv12", "cctv13", "cctv14", "cctv15", "cctv16", "cctv17", "cctv4k", "cctv8k"
		}
	} };

	private static readonly List<KeyValuePair<string, string>> PayChannelList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("cwjd", "重温经典"),
		new KeyValuePair<string, string>("dyjc", "CCTV第一剧场"),
		new KeyValuePair<string, string>("fyjc", "CCTV风云剧场"),
		new KeyValuePair<string, string>("hjjc", "CCTV怀旧剧场"),
		new KeyValuePair<string, string>("gfjs", "CCTV兵器科技"),
		new KeyValuePair<string, string>("nxss", "CCTV女性时尚"),
		new KeyValuePair<string, string>("sjdl", "CCTV世界地理"),
		new KeyValuePair<string, string>("wsjk", "CCTV卫生健康"),
		new KeyValuePair<string, string>("ysjp", "CCTV央视文化精品"),
		new KeyValuePair<string, string>("fyyy", "CCTV风云音乐"),
		new KeyValuePair<string, string>("ystq", "CCTV央视台球"),
		new KeyValuePair<string, string>("fyzq", "CCTV风云足球"),
		new KeyValuePair<string, string>("gefwq", "CCTV高尔夫网球"),
		new KeyValuePair<string, string>("jbty", "劲爆体育")
	};

	private static readonly List<KeyValuePair<string, string>> WsChannelList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("jsws", "江苏卫视"),
		new KeyValuePair<string, string>("dfws", "东方卫视"),
		new KeyValuePair<string, string>("zjws", "浙江卫视"),
		new KeyValuePair<string, string>("sdws", "山东卫视"),
		new KeyValuePair<string, string>("hnws", "河南卫视"),
		new KeyValuePair<string, string>("hbws", "湖北卫视"),
		new KeyValuePair<string, string>("hunantv", "湖南卫视"),
		new KeyValuePair<string, string>("hunanws", "湖南卫视"),
		new KeyValuePair<string, string>("gdws", "广东卫视"),
		new KeyValuePair<string, string>("szws", "深圳卫视"),
		new KeyValuePair<string, string>("bjws", "北京卫视"),
		new KeyValuePair<string, string>("tjws", "天津卫视"),
		new KeyValuePair<string, string>("ahws", "安徽卫视"),
		new KeyValuePair<string, string>("jxws", "江西卫视"),
		new KeyValuePair<string, string>("lnws", "辽宁卫视"),
		new KeyValuePair<string, string>("jlws", "吉林卫视"),
		new KeyValuePair<string, string>("hljws", "黑龙江卫视"),
		new KeyValuePair<string, string>("hebeiws", "河北卫视"),
		new KeyValuePair<string, string>("hebs", "河北卫视"),
		new KeyValuePair<string, string>("sxws", "山西卫视"),
		new KeyValuePair<string, string>("sxxws", "陕西卫视"),
		new KeyValuePair<string, string>("gsws", "甘肃卫视"),
		new KeyValuePair<string, string>("qhws", "青海卫视"),
		new KeyValuePair<string, string>("scws", "四川卫视"),
		new KeyValuePair<string, string>("ynws", "云南卫视"),
		new KeyValuePair<string, string>("gzws", "贵州卫视"),
		new KeyValuePair<string, string>("gxws", "广西卫视"),
		new KeyValuePair<string, string>("nmgws", "内蒙古卫视"),
		new KeyValuePair<string, string>("nmg", "内蒙古卫视"),
		new KeyValuePair<string, string>("xjws", "新疆卫视"),
		new KeyValuePair<string, string>("xzws", "西藏卫视"),
		new KeyValuePair<string, string>("nxws", "宁夏卫视"),
		new KeyValuePair<string, string>("hnws2", "海南卫视"),
		new KeyValuePair<string, string>("lyws", "旅游卫视"),
		new KeyValuePair<string, string>("cqws", "重庆卫视"),
		new KeyValuePair<string, string>("fjws", "福建卫视"),
		new KeyValuePair<string, string>("dnws", "东南卫视")
	};

	private static readonly List<KeyValuePair<string, string>> MovieChannelList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("vlcl", "成龙影院"),
		new KeyValuePair<string, string>("vlzxc", "周星驰影院"),
		new KeyValuePair<string, string>("vllzy", "林正英影院"),
		new KeyValuePair<string, string>("sscy", "邵氏楚原专场"),
		new KeyValuePair<string, string>("ssgf", "邵氏功夫电影"),
		new KeyValuePair<string, string>("ssnx", "邵氏女侠"),
		new KeyValuePair<string, string>("ssqa", "邵氏奇案"),
		new KeyValuePair<string, string>("sswx", "邵氏武侠电影"),
		new KeyValuePair<string, string>("ssxj", "邵氏喜剧电影"),
		new KeyValuePair<string, string>("sszc", "邵氏张彻专场")
	};

	private static readonly string[] ResolutionList = new string[9] { "4k", "2160p", "1080p", "720p", "540p", "480p", "360p", "sd", "hd" };

	private static readonly Regex RxResolutionTag = new Regex("[/_-]((?:4k|2160p|1080p|720p|540p|480p|360p|sd|hd))(?=[/._?-]|$)", RegexOptions.IgnoreCase);

	private static readonly Regex RxUrlToken = new Regex("[^\\s/_.?=&\\-]+");

	private Panel _toastPanel;

	private System.Windows.Forms.Timer _toastTimer;

	private int _hoverRow = -1;

	private int _hoverBtn = -1;

	private int _pressRow = -1;

	private int _pressBtn = -1;

	private System.Windows.Forms.Timer _btnFlashTimer;

	private int _clickRow = -1;

	private int _clickBtn = -1;

	private IntPtr embeddedPreviewHwnd = IntPtr.Zero;

	private string themePreference = "青瓷薄荷";

	internal static string customFontFamily = "Microsoft YaHei";

	private string configPath = Path.Combine(Application.StartupPath, "config.ini");

	private string channelListPath = Path.Combine(Application.StartupPath, "channellist.txt");

	private bool disclaimerAgreed;

	private bool skipDisclaimerPrompt;

	private string sortedColumn;

	private SortOrder sortDirection = SortOrder.Ascending;

	private Panel outerWrap;

	private ContextMenuStrip dataGridViewContextMenu;

	private DoubleBufferedPanel mainArea;

	private Panel actionArea;

	private Panel statusBarContainer;

	private Panel statusBarRef;

	private Panel searchPanelRef;

	private Panel searchBoxHostRef;

	private Button btnSearchRef;

	private DoubleBufferedPanel gridContainerRef;

	private Panel previewPanel;

	private ChannelPlayer channelPlayer;

	// 预览换台防抖：快速上下移动选中行时合并为一次加载，避免每经过一行都新建 VLC 输入导致卡死
	private System.Windows.Forms.Timer _previewDebounceTimer;

	// 导入期间标志：导入（解析+重建网格+弹窗）会长时间占用 UI 线程，
	// 此时禁止自动触发预览加载，避免后台 VLC 切台与阻塞的 UI 线程争用视频输出(HWND)而死锁卡死
	private bool _isImporting;

	private Button btnTogglePreview;

	private Label _detailName;

	private Label _detailUrl;

	private Label _detailResolution;

	private Label _detailGroup;

	private Label _detailStatus;

	private Label _detailBitrate;

	private string _detailUrlText = "";

	private string _detailGroupText = "";

	private Label _statusStreamInfo;

	private Label _statusName;

	private Label _statusResolution;

	private Label _statusLocation;

	private Label _statusCodec;

	private Label _statusFps;

	private Label _statusBitrate;

	private Label _statusChannels;

	private Label _statusSampleRate;

	private Label _statusSent;

	private Label _statusSpeed;

	private Label _statusTime;

	private Panel _linkStatusPanel;

	private System.Windows.Forms.Timer _linkStatusTimer;

	private string _previewChannelName = "";

	private string _previewChannelLocation = "";

	private Panel titleBarPanel;

	private Panel bottomBarRef;

	private Button btnThemeToggle;

	private Button btnMin;

	private Button btnMax;

	private Button btnClose;

	private PictureBox titleIconRef;

	private string currentView = "检测";

	private bool _applyingTheme;

	private float dpiScale = 1f;

	private bool _isRestoringFromMinimize;

	private bool IsRestoringFromMinimize => _isRestoringFromMinimize;

	private AppConfig config = new AppConfig();

	private Panel _borderOverlay;

	private readonly int _windowRadius = 10;

	private FormWindowState _lastWindowState = FormWindowState.Normal;

	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_LAYERED = 524288;

	private const int LWA_ALPHA = 2;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_24H2 = 19;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	private const int DWMWCP_DEFAULT = 0;

	private const int DWMWCP_DONOTROUND = 1;

	private const int DWMWCP_ROUND = 2;

	private const int DWMWCP_ROUNDSMALL = 3;

	private const int SB_HORZ = 0;

	private const int SB_VERT = 1;

	private const int EM_GETFIRSTVISIBLELINE = 206;

	private const int EM_GETLINECOUNT = 186;

	private const int EM_LINESCROLL = 182;

	private const int WM_NCHITTEST = 132;

	private const int WM_NCLBUTTONDBLCLK = 163;

	private const int WM_GETMINMAXINFO = 36;

	private const int WM_DPICHANGED = 736;

	private const int WM_SETREDRAW = 11;

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

	private LowLevelMouseProc _mouseHookProc;

	private const int WH_MOUSE_LL = 14;

	private const int WM_RBUTTONUP = 517;

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

	private int _droppedFrames;

	private int _totalFrames;

	private string _currentDecodedFrames = "";

	private string _currentDisplayedFrames = "";

	private CancellationTokenSource _ffplayOutputCts;

	private long _lastStreamTimeMs;

	private bool _showStreamInfoOverlay;

	private System.Windows.Forms.Timer _streamInfoOverlayTimer;

	private Form _streamInfoOverlayForm;

	private Label _streamInfoLabel;

	private DataGridView dgvData;

	private DarkScrollBar darkVScrollBar;

	private Label lblDetected;

	private Label lblAvailable;

	private Label lblPercent;

	private Label lblStreamInfo;

	private Label lblProgressText;

	private int progressBarWidth;

	private Panel emptyStatePanel;

	private Label emptyLabel;

	private Button btnNavDetect;

	private Button btnNavFile;

	private ContextMenuStrip fileMenu;

	private ContextMenuStrip themeMenuStrip;

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

	// 预编译正则（net472：RegexOptions.Compiled 避免每次调用重复编译）
	private static readonly Regex RxSegmentNumber = new Regex(@"[/_-](\d+)(?=[/._?-]|$)", RegexOptions.Compiled);
	private static readonly Regex RxBraceSingle = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
	private static readonly Regex RxBraceRange = new Regex(@"\{(\d+)-(\d+)\}", RegexOptions.Compiled);
	private static readonly Regex RxBracketRange = new Regex(@"\[(\d+)-(\d+)\]", RegexOptions.Compiled);
	private static readonly Regex RxResolution = new Regex(@"RESOLUTION=(\d+)x(\d+)", RegexOptions.Compiled);
	private static readonly Regex RxCctv = new Regex(@"^cctv\d+[a-z0-9]*$", RegexOptions.Compiled);
	private static readonly Regex RxIpV4 = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?$", RegexOptions.Compiled);
	private static readonly Regex RxResolutionTagScan = new Regex(@"[/_-]((?:2160|1080|720|540|480|360)p|4k|2k|hd|sd)(?=[/._?-]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex RxUrlTokenScan = new Regex(@"[a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Process previewProcess;

	private System.Windows.Forms.Timer previewResizeTimer;

	private bool isDetecting;

	private bool isPaused;

	private Button btnStartDetect;

	private Button btnStopDetect;

	private Button btnExport;

	private Button btnTbImport;

	private Panel importHost;

	private Panel toolbarRef;

	private Button btnNavSearch;

	private Button btnNavSettings;

	private Button btnNavAbout;

	private Color navBtnHoverBg;

	private string webViewPendingUrl = "";

	private Panel webViewNavPanel;

	private ComboBox webViewCboEngine;

	private TextBox webViewTxtUrl;

	private Label webViewStatusUrl;

	// WebView2 玻璃导航栏：所有颜色字段化，便于切换主题时刷新（避免创建时闭包捕获旧 theme）
	private Panel _webViewStatusBarRef;
	private Label _webViewLblStatusUrl;
	private Label _webViewLblStatusIp;
	private Label _webViewLblStatusEngine;
	private Panel _webViewChipContainer;
	private List<Panel> _webViewEngineChips;
	private Panel _webViewRuleChip;
	private Panel _webViewAddrBarHost;
	private Panel _webViewBtnExtractIp;
	private ComboBox _webViewCboSearchRule;
	private Color _glassNavBg;
	private Color _glassStatusBg;
	private Color _glassBorder;
	private Color _chipNormalBg;
	private Color _chipHoverBg;
	private Color _addrBarBg;
	private Color _addrBarBorder;
	private Color _chipTextColor;
	private Color _statusTextColor;
	private Color _addrTextColor;
	private Color _addrBarOpaqueColor;
	private bool _webViewDynamic;
	private List<Color> _webViewStops;

	private Button btnScanSource;

	private Button btnParseLink;

	private Panel tipBox;

	private bool hasSearchPlatformData;

	private bool autoParseLink;

	private int detectConcurrency = 10;

	// 预览窗：VLC 播不了某链接时，自动切换外部播放器（内嵌）
	private bool autoSwitchExternalPlayer = false;

	private string detectEngine = "HTTP";

	private string customPlayerPath = "";

	private string ffplayPath = "";

	private string ffprobePath = "";

	private string ffmpegPath = "";

	private string mediainfoPath = "";

	private int timeoutSeconds = 5;

	private bool autoClearInvalid;

	private bool persistList = true;

	private bool _vlcPromptShown;

	private bool _vlcCheckQueued;

	private bool watchSearchWindow;

	private bool showSearchButton;

	private bool autoExtractIpPort;

	private string loginDataPath = "";

	private List<string> iptvHistoryIps = new List<string>();

	private Dictionary<string, Dictionary<string, string>> savedLogins = new Dictionary<string, Dictionary<string, string>>();

	private int totalCount;

	private int detectedCount;

	private int availableCount;

	private List<ChannelInfo> allChannels = new List<ChannelInfo>();

	private AppTheme theme = AppTheme.GetAutoTheme();
    private IContainer components = null;

	private Color ColorPurple => theme.Primary;

	private Color ColorPurpleDark => theme.PrimaryDark;

	private Color ColorPink => theme.Accent;

	private Color ColorGreen => Color.FromArgb(76, 175, 80);

	private Color ColorOrange => Color.FromArgb(255, 152, 0);

	private Color ColorStatusBar => theme.StatusBarBg;

	private Color ColorNavSelected => theme.Primary;

	private Color ColorNavNormal => theme.TextSecondary;

	private Color ColorBorder => theme.Border;

	private void SetCustomPlayerPath()
	{
		using OpenFileDialog ofd = new OpenFileDialog();
		ofd.Filter = "可执行文件|*.exe|所有文件|*.*";
		ofd.Title = "选择播放器exe文件（如vlc.exe、mpv.exe、potplayer等）";
		if (ofd.ShowDialog() == DialogResult.OK)
		{
			customPlayerPath = ofd.FileName;
			ExternalPlayerHelper.SetCustomPlayer(customPlayerPath);
			DarkMessageBox.Show("已设置第三方播放器：\n" + customPlayerPath, "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private void CboGroup_SelectedIndexChanged(object sender, EventArgs e)
	{
		RefreshGrid();
		UpdateEmptyState();
	}

	private void RefreshGrid()
	{
		SendMessage(dgvData.Handle, 11, 0, 0);
		dgvData.SuspendLayout();
		try
		{
			string selectedGroup = cboGroup?.SelectedItem?.ToString() ?? "全部";
			string searchText = GetSearchText();
			List<ChannelInfo> filteredChannels = new List<ChannelInfo>();
			foreach (ChannelInfo ch in allChannels)
			{
				string chGroup = (string.IsNullOrWhiteSpace(ch.Group) ? "未分组" : ch.Group);
				bool num = selectedGroup == "全部" || chGroup == selectedGroup;
				bool matchSearch = string.IsNullOrWhiteSpace(searchText) || ch.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 || MatchPinyinAbbreviation(ch.Name, searchText);
				if (num && matchSearch)
				{
					filteredChannels.Add(ch);
				}
			}
			if (dgvData.Rows.Count != filteredChannels.Count)
			{
				dgvData.Rows.Clear();
				if (filteredChannels.Count > 0)
				{
					dgvData.Rows.Add(filteredChannels.Count);
				}
			}
			for (int i = 0; i < filteredChannels.Count; i++)
			{
				ChannelInfo ch2 = filteredChannels[i];
				DataGridViewRow dataGridViewRow = dgvData.Rows[i];
				dataGridViewRow.Cells[0].Value = ch2.Name;
				dataGridViewRow.Cells[1].Value = ch2.Url;
				dataGridViewRow.Cells[2].Value = ch2.Location;
				dataGridViewRow.Cells[3].Value = ch2.Resolution;
				dataGridViewRow.Cells[4].Value = ch2.Speed;
				dataGridViewRow.Cells[5].Value = (string.IsNullOrWhiteSpace(ch2.Group) ? "未分组" : ch2.Group);
				dataGridViewRow.Cells[6].Value = ch2.Status;
				dataGridViewRow.Cells[7].Value = "";
			}
		}
		finally
		{
			dgvData.ResumeLayout();
			SendMessage(dgvData.Handle, 11, 1, 0);
			dgvData.Invalidate();
			UpdateActionButtonsVisibility();
			UpdateScrollBarTheme(dgvData);
			ApplyColumnWidthsManual();
			UpdateGridScrollBar();
			if (dgvData.IsHandleCreated)
			{
				dgvData.BeginInvoke((Action)delegate
				{
					UpdateScrollBarTheme(dgvData);
					ApplyColumnWidthsManual();
					UpdateGridScrollBar();
				});
			}
		}
	}

	private string GetSearchText()
	{
		if (txtSearchBox == null)
		{
			return "";
		}
		if (txtSearchBox.Text == "输入搜索内容，按下回车键搜索")
		{
			return "";
		}
		return txtSearchBox.Text;
	}

	private bool MatchPinyinAbbreviation(string name, string keyword)
	{
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(keyword))
		{
			return false;
		}
		return GetPinyinAbbreviation(name).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private string GetPinyinAbbreviation(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
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

	private bool IsChineseChar(char c)
	{
		if (c >= '一')
		{
			return c <= '鿿';
		}
		return false;
	}

	private char GetPinyinFirstLetter(char c)
	{
		if (!IsChineseChar(c))
		{
			return char.ToLower(c);
		}
		string[] pinyinTable = new string[23]
		{
			"啊", "芭", "擦", "搭", "蛾", "发", "噶", "哈", "击", "喀",
			"垃", "妈", "拿", "哦", "啪", "期", "然", "撒", "塌", "挖",
			"昔", "压", "匝"
		};
		char[] letters = new char[23]
		{
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k',
			'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'w',
			'x', 'y', 'z'
		};
		for (int i = 0; i < pinyinTable.Length; i++)
		{
			if (c <= pinyinTable[i][0])
			{
				return letters[i];
			}
		}
		return 'z';
	}

	private void UpdateGroupFilter()
	{
		List<string> list = (from result in allChannels.Select((ChannelInfo c) => (!string.IsNullOrWhiteSpace(c.Group)) ? c.Group : "未分组").Distinct()
			orderby result
			select result).ToList();
		cboGroup.Items.Clear();
		cboGroup.Items.Add("全部");
		foreach (string g in list)
		{
			cboGroup.Items.Add(g);
		}
		cboGroup.SelectedIndex = 0;
		cboGroup.Visible = allChannels.Count > 0;
		cboGroupHost.Visible = allChannels.Count > 0;
		if (lblGroupFilter != null)
		{
			lblGroupFilter.Visible = allChannels.Count > 0;
		}
		if (allChannels.Count <= 0 || searchPanelRef == null)
		{
			return;
		}
		int rightAreaWidth = 328;
		int leftMargin = 98;
		if (searchBoxHostRef != null)
		{
			searchBoxHostRef.Left = leftMargin;
			searchBoxHostRef.Width = searchPanelRef.ClientSize.Width - leftMargin - rightAreaWidth;
			if (txtSearchBox != null)
			{
				txtSearchBox.Width = searchBoxHostRef.Width - 20;
			}
			searchBoxHostRef.Invalidate();
		}
		if (lblGroupFilter != null)
		{
			lblGroupFilter.Left = searchPanelRef.ClientSize.Width - 298;
		}
		cboGroupHost.Left = searchPanelRef.ClientSize.Width - 158;
		cboGroupHost.Width = 130;
		if (searchBoxHostRef != null)
		{
			cboGroupHost.Top = searchBoxHostRef.Top;
		}
		cboGroupHost.Invalidate();
	}

	private void RecalcStats()
	{
		detectedCount = allChannels.Count((ChannelInfo c) => c.Status != "未检测" && c.Status != "检测中");
		availableCount = allChannels.Count((ChannelInfo c) => c.Status == "可用");
	}

	private void BtnSelectFile_Click(object sender, EventArgs e)
	{
		using OpenFileDialog ofd = new OpenFileDialog();
		ofd.Filter = "m3u/txt文件|*.m3u;*.txt|m3u文件|*.m3u|txt文件|*.txt|所有文件|*.*";
		ofd.Title = "选择m3u或txt文件";
		ofd.Multiselect = true;
		if (ofd.ShowDialog() == DialogResult.OK)
		{
			int beforeCount = allChannels.Count;
			int newCount = 0;
			int dupCount = 0;
			HashSet<string> existingUrls = new HashSet<string>(allChannels.Select((ChannelInfo c) => c.Url.ToLowerInvariant()));
			string[] fileNames = ofd.FileNames;
			foreach (string file in fileNames)
			{
				(int, int) result = ImportFromFile(file, existingUrls);
				newCount += result.Item1;
				dupCount += result.Item2;
			}
			totalCount = allChannels.Count;
			UpdateGroupFilter();
			RefreshGrid();
			UpdateStatusBar();
			UpdateEmptyState();
			UpdateActionButtonsVisibility();
			string msg = $"成功导入 {newCount} 个频道";
			if (beforeCount > 0)
			{
				msg += $"（追加到列表，总计 {totalCount} 个）";
			}
			if (dupCount > 0)
			{
				msg += $"\n跳过重复链接 {dupCount} 个";
			}
			DarkMessageBox.Show(msg, "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private async void BtnStartDetect_Click(object sender, EventArgs e)
	{
		if (isDetecting && !isPaused)
		{
			isPaused = true;
			btnStartDetect.Text = "继续检测";
			btnStartDetect.BackColor = ColorGreen;
			btnStartDetect.ForeColor = Color.White;
			return;
		}
		if (isDetecting && isPaused)
		{
			isPaused = false;
			btnStartDetect.Text = "暂停检测";
			btnStartDetect.BackColor = ColorOrange;
			btnStartDetect.ForeColor = Color.White;
			return;
		}
		if (allChannels.Count == 0)
		{
			DarkMessageBox.Show("请先导入频道数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		await CheckAndDownloadComponentsAsync();
		isDetecting = true;
		isPaused = false;
		btnStartDetect.Text = "暂停检测";
		btnStartDetect.BackColor = ColorOrange;
		btnStartDetect.ForeColor = Color.White;
		if (btnScanSource != null)
		{
			btnScanSource.Enabled = false;
		}
		await StartDetection();
		isDetecting = false;
		isPaused = false;
		btnStartDetect.Text = "开始检测";
		btnStartDetect.BackColor = theme.InfoColor;
		btnStartDetect.ForeColor = Color.White;
		if (btnScanSource != null)
		{
			btnScanSource.Enabled = true;
		}
	}

	private void BtnExport_Click(object sender, EventArgs e)
	{
		if (allChannels.Count == 0)
		{
			DarkMessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		using SaveFileDialog sfd = new SaveFileDialog();
		sfd.Filter = "m3u文件(标准)|*.m3u|m3u文件(按名称分组)|*.m3u|txt文件(标准)|*.txt|txt文件(合并频道)|*.txt|m3u文件(合并+台标)|*.m3u|txt文件(合并+台标)|*.txt";
		sfd.Title = "选择导出格式";
		sfd.FileName = $"channels_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
		if (sfd.ShowDialog() == DialogResult.OK)
		{
			switch (sfd.FilterIndex)
			{
			case 1:
				ExportToM3u(sfd.FileName);
				break;
			case 2:
				ExportToM3uMergeGroup(sfd.FileName);
				break;
			case 3:
				ExportToTxt(sfd.FileName, merge: false);
				break;
			case 5:
				ExportToM3uMergeLogo(sfd.FileName);
				break;
			case 6:
				ExportToTxtMergeLogo(sfd.FileName);
				break;
			default:
				ExportToTxtMergeUrl(sfd.FileName);
				break;
			}
		}
	}

	private void UpdateStatusBarRegion()
	{
		if (statusBarRef == null || statusBarRef.Width <= 0 || statusBarRef.Height <= 0)
		{
			return;
		}
		int r = statusBarRef.Height / 2;
		using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, statusBarRef.Width - 1, statusBarRef.Height - 1), r);
		statusBarRef.Region = new Region(path);
	}

	private void LayoutStatusBar(Panel statusBar)
	{
		if (lblDetected == null || lblAvailable == null || lblProgressText == null || lblPercent == null)
		{
			return;
		}
		int w = statusBar.ClientSize.Width;
		int h = statusBar.ClientSize.Height;
		if (w > 0)
		{
			int padLeft = SX(20);
			int padRight = SX(20);
			int gap = SX(40);
			int padY = (h - lblDetected.Height) / 2;
			if (padY < 0)
			{
				padY = 0;
			}
			lblDetected.Location = At(padLeft, padY);
			lblAvailable.Location = At(lblDetected.Right + gap, padY);
			if (lblStreamInfo != null && lblStreamInfo.Visible && !string.IsNullOrEmpty(lblStreamInfo.Text))
			{
				lblStreamInfo.Location = At(lblAvailable.Right + gap, padY);
			}
			int progTotalW = lblProgressText.Width + SX(6) + lblPercent.Width;
			int progX = ((!lblProgressText.Text.Contains("华视美达")) ? (w - padRight - progTotalW) : ((w - progTotalW) / 2));
			lblProgressText.Location = At(progX, padY);
			lblPercent.Location = At(progX + lblProgressText.Width + SX(6), padY);
			statusBarRef.Invalidate();
		}
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
		{
			btn.Region = new Region(path);
		}
		Color bc = borderColor ?? Color.Empty;
		int bw = borderWidth;
		bool isHover = false;
		bool isPressed = false;
		bool isClicked = false;
		int pressOffset = 2;
		int animDuration = 150;
		btn.MouseEnter += delegate
		{
			isHover = true;
			btn.Invalidate();
		};
		btn.MouseLeave += delegate
		{
			isHover = false;
			isPressed = false;
			btn.Invalidate();
		};
		btn.MouseDown += delegate
		{
			isPressed = true;
			btn.Invalidate();
		};
		btn.MouseUp += delegate
		{
			isPressed = false;
			btn.Invalidate();
		};
		btn.MouseClick += delegate
		{
			isClicked = true;
			btn.Invalidate();
			System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer
			{
				Interval = animDuration
			};
			animTimer.Tick += delegate
			{
				animTimer.Stop();
				animTimer.Dispose();
				isClicked = false;
				btn.Invalidate();
			};
			animTimer.Start();
		};
		btn.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (SolidBrush brush = new SolidBrush((btn.Parent != null) ? btn.Parent.BackColor : Color.White))
			{
				e.Graphics.FillRectangle(brush, 0, 0, btn.Width, btn.Height);
			}
			int num = ((isPressed || isClicked) ? pressOffset : 0);
			int num2 = ((isPressed || isClicked) ? 1 : 0);
			Rectangle rect = new Rectangle(bw / 2 + num2, bw / 2 + num, btn.Width - 1 - bw, btn.Height - 1 - bw);
			Color backColor = btn.BackColor;
			bool num3 = DrawingUtils.IsDarkColor(backColor);
			int num4 = (num3 ? 35 : 18);
			int num5 = (num3 ? 30 : 25);
			int num6 = (num3 ? 45 : 40);
			Color color = Color.FromArgb(Math.Min(255, backColor.R + num4), Math.Min(255, backColor.G + num4), Math.Min(255, backColor.B + num4));
			Color color2 = Color.FromArgb(Math.Max(0, backColor.R - num5), Math.Max(0, backColor.G - num5), Math.Max(0, backColor.B - num5));
			Color color3 = Color.FromArgb(Math.Max(0, backColor.R - num6), Math.Max(0, backColor.G - num6), Math.Max(0, backColor.B - num6));
			Color color4 = ((!btn.Enabled) ? Color.FromArgb((int)((double)(int)backColor.R * 0.6), (int)((double)(int)backColor.G * 0.6), (int)((double)(int)backColor.B * 0.6)) : (isClicked ? color3 : (isPressed ? color2 : ((!isHover) ? backColor : color))));
			int num7 = Math.Max(1, radius - bw);
			using (GraphicsPath path2 = GetRoundedPath(rect, num7))
			{
				using SolidBrush brush2 = new SolidBrush(color4);
				e.Graphics.FillPath(brush2, path2);
			}
			if (bw > 0 && bc != Color.Empty)
			{
				using GraphicsPath path3 = GetRoundedPath(new Rectangle(bw / 2 + num2, bw / 2 + num, btn.Width - 1 - bw, btn.Height - 1 - bw), num7);
				using Pen pen = new Pen(bc, bw);
				pen.Alignment = PenAlignment.Center;
				e.Graphics.DrawPath(pen, path3);
			}
			if (isHover)
			{
				using GraphicsPath path4 = GetRoundedPath(new Rectangle(2 + num2, 2 + num, btn.Width - 5 - bw, btn.Height - 5 - bw), num7 - 1);
				using Pen pen2 = new Pen(Color.FromArgb(40, Color.White), 1.5f);
				e.Graphics.DrawPath(pen2, path4);
			}
			if (customDraw != null)
			{
				customDraw(e.Graphics, num2, num);
			}
			TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(num2, num, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		};
		btn.Resize += delegate
		{
			btn.Region?.Dispose();
			using (GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
			{
				btn.Region = new Region(path2);
			}
			btn.Invalidate();
		};
		btn.BackColorChanged += delegate
		{
			btn.Invalidate();
		};
		btn.ParentChanged += delegate
		{
			if (btn.Parent != null)
			{
				btn.Parent.BackColorChanged += delegate
				{
					btn.Invalidate();
				};
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
		{
			btn.Region = new Region(path);
		}
		bool isHover = false;
		bool isPressed = false;
		float animProgress = 0f;
		int animSpeed = 8;
		System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer
		{
			Interval = 16
		};
		Color hoverBg = bc;
		Color hoverText = (DrawingUtils.IsDarkColor(bc) ? Color.White : Color.White);
		animTimer.Tick += delegate
		{
			float num = (isHover ? 1f : 0f);
			if (Math.Abs(animProgress - num) < 0.01f)
			{
				animProgress = num;
				animTimer.Stop();
			}
			else
			{
				animProgress += (num - animProgress) * (float)animSpeed / 100f;
			}
			btn.Invalidate();
		};
		btn.MouseEnter += delegate
		{
			isHover = true;
			animTimer.Start();
		};
		btn.MouseLeave += delegate
		{
			isHover = false;
			isPressed = false;
			animTimer.Start();
		};
		btn.MouseDown += delegate
		{
			isPressed = true;
			btn.Invalidate();
		};
		btn.MouseUp += delegate
		{
			isPressed = false;
			btn.Invalidate();
		};
		btn.Paint += delegate(object s, PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			Color color = ((btn.Parent != null) ? btn.Parent.BackColor : Color.White);
			using (SolidBrush brush = new SolidBrush(color))
			{
				graphics.FillRectangle(brush, 0, 0, btn.Width, btn.Height);
			}
			int num = (isPressed ? 1 : 0);
			Rectangle rect = new Rectangle(0, num, btn.Width - 1, btn.Height - 1 - num);
			Color color2 = DrawingUtils.LerpColor(color, hoverBg, animProgress);
			Color color3 = DrawingUtils.LerpColor(tc, hoverText, animProgress);
			using (GraphicsPath path2 = GetRoundedPath(rect, radius))
			{
				if (animProgress > 0.01f)
				{
					using SolidBrush brush2 = new SolidBrush(color2);
					graphics.FillPath(brush2, path2);
				}
				using Pen pen = new Pen(bc, 1.5f);
				graphics.DrawPath(pen, path2);
			}
			using SolidBrush brush3 = new SolidBrush(color3);
			using StringFormat format = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(btn.Text, btn.Font, brush3, new RectangleF(0f, num, btn.Width, btn.Height - num), format);
		};
		btn.Resize += delegate
		{
			btn.Region?.Dispose();
			using (GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius))
			{
				btn.Region = new Region(path2);
			}
			btn.Invalidate();
		};
		btn.BackColorChanged += delegate
		{
			btn.Invalidate();
		};
		btn.ParentChanged += delegate
		{
			if (btn.Parent != null)
			{
				btn.Parent.BackColorChanged += delegate
				{
					btn.Invalidate();
				};
			}
		};
		btn.Invalidate();
	}



	private static void StyleRoundTextBox(TextBox txt, int radius = 6, Color? borderColor = null, int borderWidth = 1)
	{
		txt.BorderStyle = BorderStyle.None;
		Color bc = borderColor ?? Color.FromArgb(200, 200, 200);
		int bw = borderWidth;
		Panel host = txt.Parent as Panel;
		if (host == null)
		{
			return;
		}
		host.Tag = bc;
		host.Paint += delegate(object s, PaintEventArgs e)
		{
			if (!txt.Visible || txt.IsDisposed)
			{
				return;
			}
			Rectangle rect = new Rectangle(txt.Left - bw, txt.Top - bw, txt.Width + bw * 2, txt.Height + bw * 2);
			Color color = ((host.Tag is Color) ? ((Color)host.Tag) : bc);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = GetRoundedPath(rect, radius))
			{
				using SolidBrush brush = new SolidBrush(txt.BackColor);
				e.Graphics.FillPath(brush, path);
			}
			using GraphicsPath path2 = GetRoundedPath(rect, radius);
			using Pen pen = new Pen(color, bw);
			e.Graphics.DrawPath(pen, path2);
		};
		txt.Resize += delegate
		{
			host.Invalidate();
		};
		txt.LocationChanged += delegate
		{
			host.Invalidate();
		};
		txt.BackColorChanged += delegate
		{
			host.Invalidate();
		};
		host.Invalidate();
	}

	private static void MakeRounded(Control ctrl, int radius = 6)
	{
		ctrl.Region?.Dispose();
		using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius))
		{
			ctrl.Region = new Region(path);
		}
		ctrl.Resize += delegate
		{
			ctrl.Region?.Dispose();
			using (GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius))
			{
				ctrl.Region = new Region(path2);
			}
			ctrl.Invalidate();
		};
	}

	private void ShowSettingsDialog()
	{
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg); // 是否深色主题（决定后续文字/强调色取亮色还是暗色）
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast); // 霓虹调色板（外框、发光、强调色来源）
		Color bgColor = theme.Bg; // 设置窗口整体背景色（取当前主题背景）
		Color textColor = theme.TextPrimary; // 主文本颜色
		Color accentColor = pal.Neon; // 强调/高亮色（霓虹色，用于按钮高亮、链接等）
		Color borderColor = pal.Border; // 通用边框颜色
		Color engineCardBg = ControlPaint.Light(theme.Surface, 0.06f); // 检测引擎卡片底色（在表面色上轻微提亮）
		Color engineCardBorder = pal.Border; // 检测引擎卡片边框色
		Color perfCardBg = ControlPaint.Light(theme.Surface, 0.06f); // 性能设置卡片底色
		Color perfCardBorder = pal.Border; // 性能设置卡片边框色
		Color funcCardBg = ControlPaint.Light(theme.Surface, 0.06f); // 功能开关卡片底色
		Color funcCardBorder = pal.Border; // 功能开关卡片边框色
		Color customCardBg = ControlPaint.Light(theme.Surface, 0.06f); // 个性化卡片底色
		Color customCardBorder = pal.Border; // 个性化卡片边框色
		Rectangle screenWorkArea = Screen.GetWorkingArea(this); // 获取屏幕可用工作区（不含任务栏）
		int screenWidth = screenWorkArea.Width; // 工作区宽度
		int screenHeight = screenWorkArea.Height; // 工作区高度
		int scrollTopPad = SY(20); // 滚动容器内顶部留白
		int scrollBottomPad = SY(8); // 滚动容器内底部留白
		int scrollRightPad = SX(16); // 滚动容器内右侧留白（与卡片右边框对齐，避免裁切）
		int cardStartY = SY(15); // 第一张卡片顶部起始 Y
		int engineCardH = SY(105); // 检测引擎卡片高度
		int perfCardH = SY(125); // 性能设置卡片高度
		int funcCardH = SY(270); // 功能开关卡片高度
		int customCardH = SY(225); // 个性化卡片高度（含“自动切换外部播放器”开关）
		int cardGap = SY(12); // 卡片之间的垂直间距
		int btnPanelH = SY(65); // 底部按钮面板高度
		int contentTotalH = engineCardH + perfCardH + customCardH + cardGap * 3 + scrollBottomPad; // 全部卡片+间距的总高度
		int val = scrollTopPad + contentTotalH + btnPanelH; // 窗口内容所需最小高度（含上下留白与按钮区）
		int windowWidth = Math.Min(SX(900), (int)((double)screenWidth * 0.92)); // 窗口宽度：最大 900，且不超过屏幕 92%
		int windowHeight = Math.Min(Math.Max(val, SY(450)), (int)((double)screenHeight * 0.95)); // 窗口高度：不小于 450，且不超过屏幕 95%
		// 设置窗口主窗体：无边框（由 NeonChrome 绘制圆角外框），禁止最大化/最小化
		Form dlg = new Form
		{
			Text = "设置",
			Size = new Size(windowWidth, windowHeight),
			StartPosition = FormStartPosition.CenterScreen,
			MaximizeBox = false,
			MinimizeBox = false,
			Icon = this.Icon
		};
		var ctx = NeonChrome.Apply(dlg, pal, "设置", dpiScale); // 套用霓虹外观：生成标题栏、内容面板 Body、圆角外框
		Point At(int x, int yy) => new Point(x, yy); // 坐标辅助函数（x,y）→ Point，便于按 DPI 布局
		int cardWidth = ctx.Body.Width - SX(32); // 卡片宽度：内容区宽度减左右各 16 边距，保证左右对称
		int cardX = SX(16); // 卡片左边距（与右侧对称）
		int cardY = cardStartY; // 当前卡片顶部 Y（随布局向下累加）
		// 滚动容器：承载全部卡片，内容超出窗口高度时自动出现滚动条；Padding 决定卡片与窗口边缘留白
		Panel scrollContainer = new Panel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			BackColor = bgColor,
			Padding = new Padding(scrollRightPad, scrollTopPad, scrollRightPad, scrollBottomPad) // 左右内边距对称（均 = scrollRightPad），保证卡片左右留白一致
		};
		Panel engineCard = CreateCard(engineCardBg, engineCardBorder);
		PaintCardBorder(engineCard, engineCardBorder);
		// 卡片标题“检测引擎”（加粗字体）
		Label engineTitle = new Label
		{
			Text = "检测引擎",
			Font = GetFont(11f, FontStyle.Bold),
			ForeColor = (isDark ? Color.FromArgb(100, 180, 255) : Color.FromArgb(30, 100, 180)),
			Size = new Size(cardWidth - SY(30), 28),
			Location = At(SY(15), SY(12)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		engineCard.Controls.Add(engineTitle);
		// 单选按钮：检测引擎 = HTTP 直连（直接读取 m3u8/ts 流）
		RadioButton rbHttp = new RadioButton
		{
			Text = "HTTP",
			Font = GetFont(10f),
			ForeColor = textColor,
			BackColor = engineCardBg,
			Checked = (detectEngine == "HTTP"),
			Location = At(SX(40), SY(48))
		};
		engineCard.Controls.Add(rbHttp);
		// 单选按钮：检测引擎 = FFmpeg 解码（调用本地 FFmpeg 解码更多编码格式）
		RadioButton rbFfmpeg = new RadioButton
		{
			Text = "FFMPEG",
			Font = GetFont(10f),
			ForeColor = textColor,
			BackColor = engineCardBg,
			Checked = (detectEngine == "FFMPEG"),
			Location = At(cardWidth - SX(120), SY(48)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		engineCard.Controls.Add(rbFfmpeg);
		// 检测引擎说明文字（浅灰，随主题明暗取不同亮度）
		Label engineTip = new Label
		{
			Text = "提示：HTTP模式不支持分辨率检测",
			Font = GetFont(8.5f),
			ForeColor = (isDark ? Color.FromArgb(200, 100, 100) : Color.FromArgb(200, 80, 80)),
			Size = new Size(cardWidth - SY(30), 22),
			Location = At(SY(15), SY(78)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		engineCard.Controls.Add(engineTip);
		engineCard.Size = new Size(cardWidth, engineCardH);
		scrollContainer.Controls.Add(engineCard);
		cardY += engineCardH + cardGap;
		// 性能设置卡片容器（Panel）：承载并发数、超时时间等性能相关控件
		Panel perfCard = CreateCard(perfCardBg, perfCardBorder);
		PaintCardBorder(perfCard, perfCardBorder);
		// 性能设置卡片标题（加粗，橙色调）
		Label perfTitle = new Label
		{
			Text = "\ud83d\ude80 性能设置",
			Font = GetFont(11f, FontStyle.Bold),
			ForeColor = (isDark ? Color.FromArgb(255, 160, 80) : Color.FromArgb(200, 100, 30)),
			Size = new Size(cardWidth - SY(30), 28),
			Location = At(SY(15), SY(12)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		perfCard.Controls.Add(perfTitle);
		Color perfTipColor = (isDark ? Color.FromArgb(255, 180, 100) : Color.FromArgb(200, 100, 30)); // 性能提示文字颜色（橙色，随主题明暗取不同亮度）
		// 标签：并发检测数量
		Label concurrencyLabel = new Label
		{
			Text = "并发检测数量",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(120), 24),
			Location = At(SY(15), SY(48))
		};
		perfCard.Controls.Add(concurrencyLabel);
		Label concurrencyTip = new Label
		{
			Text = "（范围：1-20，过大可能导致检测不准确）",
			Font = GetFont(8.5f),
			ForeColor = perfTipColor,
			AutoSize = true,
			Location = At(SY(140), SY(50))
		};
		perfCard.Controls.Add(concurrencyTip);
		// 输入框：并发检测数量（数字，右对齐）
		TextBox txtConcurrency = new TextBox
		{
			Text = detectConcurrency.ToString(),
			Font = GetFont(9.5f),
			ForeColor = textColor,
			BackColor = bgColor,
			BorderStyle = BorderStyle.FixedSingle,
			TextAlign = HorizontalAlignment.Right,
			Size = new Size(SY(80), SY(28)),
			Location = At(cardWidth - SY(110), SY(46)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		perfCard.Controls.Add(txtConcurrency);
		Label timeoutLabel = new Label
		{
			Text = "超时时间（秒）",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(120), 24),
			Location = At(SY(15), SY(88))
		};
		perfCard.Controls.Add(timeoutLabel);
		// 提示文字：超时时间范围（1-60秒）
		Label timeoutTip = new Label
		{
			Text = "（范围：1-60秒）",
			Font = GetFont(8.5f),
			ForeColor = perfTipColor,
			AutoSize = true,
			Location = At(SY(140), SY(90))
		};
		perfCard.Controls.Add(timeoutTip);
		// 输入框：超时时间（秒，数字，右对齐）
		TextBox txtTimeout = new TextBox
		{
			Text = timeoutSeconds.ToString(),
			Font = GetFont(9.5f),
			ForeColor = textColor,
			BackColor = bgColor,
			BorderStyle = BorderStyle.FixedSingle,
			TextAlign = HorizontalAlignment.Right,
			Size = new Size(SY(80), SY(28)),
			Location = At(cardWidth - SY(110), SY(86)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		perfCard.Controls.Add(txtTimeout);
		perfCard.Size = new Size(cardWidth, perfCardH);
		scrollContainer.Controls.Add(perfCard);
		cardY += perfCardH + cardGap;
		// 功能开关卡片容器（默认隐藏，由底部“高级功能”按钮展开/收起）
		Panel funcCard = CreateCard(funcCardBg, funcCardBorder);
		PaintCardBorder(funcCard, funcCardBorder);
		// 功能开关卡片标题（加粗，绿色调）
		Label funcTitle = new Label
		{
			Text = "\ud83c\udfaf 功能开关",
			Font = GetFont(11f, FontStyle.Bold),
			ForeColor = (isDark ? Color.FromArgb(120, 220, 150) : Color.FromArgb(40, 160, 80)),
			Size = new Size(cardWidth - SY(30), 28),
			Location = At(SY(15), SY(12)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		funcCard.Controls.Add(funcTitle);
		// 标签：自动清除无效源
		Label autoClearLabel = new Label
		{
			Text = "自动清除无效源",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(180), 24),
			Location = At(SY(15), SY(48))
		};
		funcCard.Controls.Add(autoClearLabel);
		// 开关：自动清除无效源（检测完成后自动移除失效源）
		ToggleSwitch toggleAutoClear = new ToggleSwitch
		{
			Checked = autoClearInvalid,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(47)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(toggleAutoClear);
		// 标签：检测列表持久化
		Label persistLabel = new Label
		{
			Text = "检测列表持久化",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(180), 24),
			Location = At(SY(15), SY(85))
		};
		funcCard.Controls.Add(persistLabel);
		// 开关：检测列表持久化（保存并恢复检测列表）
		ToggleSwitch togglePersist = new ToggleSwitch
		{
			Checked = persistList,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(84)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(togglePersist);
		// 标签：关闭搜索提示框（鼠标移出搜索结果时是否隐藏提示框）
		Label watchLabel = new Label
		{
			Text = "关闭搜索提示框",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(180), 24),
			Location = At(SY(15), SY(122))
		};
		funcCard.Controls.Add(watchLabel);
		// 开关：关闭搜索提示框
		ToggleSwitch toggleWatch = new ToggleSwitch
		{
			Checked = watchSearchWindow,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(121)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(toggleWatch);
		// 标签：自动解析链接
		Label autoParseLabel = new Label
		{
			Text = "自动解析链接",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(180), 24),
			Location = At(SY(15), SY(159))
		};
		funcCard.Controls.Add(autoParseLabel);
		// 开关：自动解析链接（粘贴 m3u8 等链接时自动解析）
		ToggleSwitch toggleAutoParse = new ToggleSwitch
		{
			Checked = autoParseLink,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(158)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(toggleAutoParse);
		// 标签：搜索功能（主界面是否显示“搜索”导航按钮）
		Label searchBtnLabel = new Label
		{
			Text = "搜索功能",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(180), 24),
			Location = At(SY(15), SY(196))
		};
		funcCard.Controls.Add(searchBtnLabel);
		// 开关：搜索功能（控制主界面“搜索”按钮可见性）
		ToggleSwitch toggleSearchBtn = new ToggleSwitch
		{
			Checked = showSearchButton,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(195)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(toggleSearchBtn);
		Label skipDisclaimerLabel = new Label
		{
			Text = "下次启动不再提示免责声明",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(240), 24),
			Location = At(SY(15), SY(233))
		};
		funcCard.Controls.Add(skipDisclaimerLabel);
		// 开关：下次启动不再提示免责声明
		ToggleSwitch toggleSkipDisclaimer = new ToggleSwitch
		{
			Checked = skipDisclaimerPrompt,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(232)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		funcCard.Controls.Add(toggleSkipDisclaimer);
		funcCard.Size = new Size(cardWidth, funcCardH);
		scrollContainer.Controls.Add(funcCard);
		cardY += funcCardH + cardGap;
		// 个性化卡片容器（Panel）：承载字体、播放器、预览等个性化设置
		Panel customCard = CreateCard(customCardBg, customCardBorder);
		PaintCardBorder(customCard, customCardBorder);
		// 个性化卡片标题（加粗，紫色调）
		Label customTitle = new Label
		{
			Text = "个性化",
			Font = GetFont(11f, FontStyle.Bold),
			ForeColor = (isDark ? Color.FromArgb(200, 150, 255) : Color.FromArgb(120, 60, 180)),
			Size = new Size(cardWidth - SY(30), 28),
			Location = At(SY(15), SY(12)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		customCard.Controls.Add(customTitle);
		// 标签：字体设置
		Label fontLabel = new Label
		{
			Text = "字体设置",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(120), 24),
			Location = At(SY(15), SY(48))
		};
		customCard.Controls.Add(fontLabel);
		int cmbFontW = SX(220); // 字体下拉框宽度
		int btnBrowseW = SX(75); // “应用”/“浏览”按钮宽度
		int btnRightMargin = SX(20); // 右侧按钮距卡片右边距
		int inputBtnGap = SX(12); // 输入框与按钮之间的水平间距
		// 字体下拉选择框（只选不填，列出系统所有字体族）
		ComboBox cmbFont = new ComboBox
		{
			Font = GetFont(9f),
			ForeColor = textColor,
			BackColor = bgColor,
			Size = new Size(cmbFontW, SY(28)),
			Location = At(cardWidth - btnRightMargin - btnBrowseW - inputBtnGap - cmbFontW, SY(46)),
			DropDownStyle = ComboBoxStyle.DropDownList,
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		// 枚举系统已安装字体族，填充到下拉框；默认选中当前自定义字体（否则选第一个）
		FontFamily[] families = FontFamily.Families;
		foreach (FontFamily fontFamily in families)
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
		// “应用”按钮：将选中的字体立即生效并保存
		Button btnFontApply = new Button
		{
			Text = "应用",
			Font = GetFont(9f),
			ForeColor = Color.White,
			BackColor = accentColor,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(btnBrowseW, SY(28)),
			Location = At(cardWidth - btnRightMargin - btnBrowseW, SY(46)),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		btnFontApply.FlatAppearance.BorderSize = 0;
		btnFontApply.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnBrowseW, SY(28)), 6));
		btnFontApply.Click += delegate
		{
			if (cmbFont.SelectedItem != null)
			{
				string text = cmbFont.SelectedItem.ToString();
				if (text != customFontFamily)
				{
					customFontFamily = text;
					RefreshFontsImmediately();
					RefreshControlFonts(dlg.Controls);
					dlg.Invalidate();
					SaveConfig();
				}
			}
		};
		customCard.Controls.Add(btnFontApply);
		// 标签：第三方播放器（用于双击播放直播源）
		Label playerLabel = new Label
		{
			Text = "第三方播放器",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(100), 24),
			Location = At(SY(15), SY(85))
		};
		customCard.Controls.Add(playerLabel);
		int playerInputW = cardWidth - SX(130) - btnRightMargin - btnBrowseW - inputBtnGap; // 播放器路径输入框宽度（按卡片宽度自适应）
		// 输入框：第三方播放器可执行文件路径
		TextBox txtPlayerPath = new TextBox
		{
			Text = customPlayerPath,
			Font = GetFont(9f),
			ForeColor = textColor,
			BackColor = bgColor,
			BorderStyle = BorderStyle.FixedSingle,
			Size = new Size(playerInputW, SY(28)),
			Location = At(cardWidth - btnRightMargin - btnBrowseW - inputBtnGap - playerInputW, SY(83)),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		customCard.Controls.Add(txtPlayerPath);
		// “浏览...”按钮：打开文件选择对话框挑选播放器 exe
		Button btnBrowsePlayer = new Button
		{
			Text = "浏览...",
			Font = GetFont(9f),
			ForeColor = Color.White,
			BackColor = accentColor,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(btnBrowseW, SY(28)),
			Location = At(cardWidth - btnRightMargin - btnBrowseW, SY(83)),
			Cursor = Cursors.Hand,
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		btnBrowsePlayer.FlatAppearance.BorderSize = 0;
		btnBrowsePlayer.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, btnBrowseW, SY(28)), 6));
		btnBrowsePlayer.Click += delegate
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "可执行文件|*.exe|所有文件|*.*";
			openFileDialog.Title = "选择第三方播放器";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				txtPlayerPath.Text = openFileDialog.FileName;
			}
		};
		customCard.Controls.Add(btnBrowsePlayer);
		// 分隔线（1px 高的 Label，用主题边框色绘制，分隔上方设置与下方预览开关）
		Label sepLine = new Label
		{
			Text = "",
			Size = new Size(cardWidth - SY(30), 1),
			Location = At(SY(15), SY(123)),
			BackColor = theme.Border,
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};
		customCard.Controls.Add(sepLine);
		// 标签：预览窗（右侧内嵌播放器）开关
		Label previewToggleLabel = new Label
		{
			Text = "预览窗（右侧内嵌播放器）",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(240), 24),
			Location = At(SY(15), SY(133))
		};
		customCard.Controls.Add(previewToggleLabel);
		// 开关：预览窗（控制主界面右侧内嵌播放器是否显示）
		ToggleSwitch togglePreview = new ToggleSwitch
		{
			Checked = (previewPanel != null && previewPanel.Visible),
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(132)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		customCard.Controls.Add(togglePreview);
		// 标签：预览窗自动切换外部播放器（内嵌）
		Label autoSwitchExtLabel = new Label
		{
			Text = "预览窗自动切外部播放器(内嵌)",
			Font = GetFont(9.5f),
			ForeColor = textColor,
			Size = new Size(SY(240), 24),
			Location = At(SY(15), SY(169))
		};
		customCard.Controls.Add(autoSwitchExtLabel);
		// 开关：VLC 无法播放某链接时，自动将视频内嵌切换到 PotPlayer/MPV
		ToggleSwitch toggleAutoSwitch = new ToggleSwitch
		{
			Checked = autoSwitchExternalPlayer,
			Size = new Size(SY(80), SY(30)),
			Location = At(cardWidth - SY(110), SY(168)),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};
		customCard.Controls.Add(toggleAutoSwitch);
		customCard.Size = new Size(cardWidth, customCardH);
		scrollContainer.Controls.Add(customCard);
		cardY += customCardH + cardGap;
		scrollContainer.AutoScrollMinSize = new Size(cardWidth, contentTotalH); // 设置滚动容器最小尺寸，保证卡片可完整滚动显示
		// 底部按钮面板（停靠在窗口底部，承载“恢复默认/确定/取消/高级功能”按钮）
		Panel btnPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = btnPanelH,
			BackColor = bgColor,
			Padding = new Padding(0, SY(12), 0, SY(12))
		};
		ctx.Body.Controls.Add(btnPanel);
		ctx.Body.Controls.Add(scrollContainer);
		// “恢复默认”按钮：经二次确认后将全部设置重置为默认值
		Button btnReset = new Button
		{
			Text = "恢复默认",
			Font = GetFont(10f),
			ForeColor = textColor,
			BackColor = Color.Transparent,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(SY(110), SY(35)),
			Cursor = Cursors.Hand
		};
		StyleOutlineButton(btnReset, 17, borderColor, textColor);
		btnReset.Click += delegate
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
				showSearchButton = false;
				autoExtractIpPort = false;
				customFontFamily = "Microsoft YaHei";
				themePreference = "青瓷薄荷";
				theme = MakeEffectiveTheme();
				disclaimerAgreed = false;
				skipDisclaimerPrompt = false;
				autoSwitchExternalPlayer = false;
				ChannelPlayer.EnableAutoSwitchExternal = false;
				iptvHistoryIps = new List<string>();
				loginDataPath = "";
				rbHttp.Checked = true;
				rbFfmpeg.Checked = false;
				txtConcurrency.Text = "10";
				txtTimeout.Text = "5";
				toggleAutoClear.Checked = false;
				togglePersist.Checked = true;
				toggleAutoParse.Checked = false;
				toggleWatch.Checked = false;
				toggleSearchBtn.Checked = false;
				toggleSkipDisclaimer.Checked = false;
				toggleAutoSwitch.Checked = false;
				txtPlayerPath.Text = "";
				if (cmbFont.Items.Contains("Microsoft YaHei"))
				{
					cmbFont.SelectedItem = "Microsoft YaHei";
				}
				if (btnNavSearch != null)
				{
					btnNavSearch.Visible = showSearchButton;
					RefreshNavButtonSizes();
				}
				SaveConfig();
				ApplyTheme();
				RefreshFontsImmediately();
				// 原地刷新设置窗口主题：更新 NeonChrome 调色板 + 递归刷新 Body 内控件颜色
				NeonPalette newPal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
				ctx.UpdatePalette(newPal);
				RefreshDialogControlColors(ctx.Body, theme, newPal);
				dlg.Invalidate(true);
			}
		};
		// “确定”按钮：收集各控件值、保存配置、应用主题后关闭窗口
		Button btnOK = new Button
		{
			Text = "确定",
			Font = GetFont(10f, FontStyle.Bold),
			ForeColor = Color.White,
			BackColor = accentColor,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(SY(110), SY(35)),
			Cursor = Cursors.Hand
		};
		btnOK.FlatAppearance.BorderSize = 0;
		btnOK.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, SY(110), SY(35)), 6));
		btnOK.Click += delegate
		{
			detectEngine = (rbHttp.Checked ? "HTTP" : "FFMPEG");
			if (int.TryParse(txtConcurrency.Text, out var result))
			{
				detectConcurrency = Math.Max(1, Math.Min(20, result));
			}
			if (int.TryParse(txtTimeout.Text, out var result2))
			{
				timeoutSeconds = Math.Max(1, Math.Min(60, result2));
			}
			autoClearInvalid = toggleAutoClear.Checked;
			skipDisclaimerPrompt = toggleSkipDisclaimer.Checked;
			persistList = togglePersist.Checked;
			customPlayerPath = txtPlayerPath.Text;
			watchSearchWindow = toggleWatch.Checked;
			autoParseLink = toggleAutoParse.Checked;
			showSearchButton = toggleSearchBtn.Checked;
			autoSwitchExternalPlayer = toggleAutoSwitch.Checked;
			ChannelPlayer.EnableAutoSwitchExternal = autoSwitchExternalPlayer;
			if (previewPanel != null)
			{
				bool flag = togglePreview.Checked;
				if (flag && !previewPanel.Visible)
				{
					previewPanel.Visible = true;
					LoadSelectedChannelToPreview();
				}
				else if (!flag && previewPanel.Visible)
				{
					previewPanel.Visible = false;
					if (channelPlayer != null)
					{
						channelPlayer.StopAsync();
					}
				}
				if (btnTogglePreview != null)
				{
					btnTogglePreview.BackColor = (flag ? theme.Primary : theme.Surface);
					btnTogglePreview.ForeColor = (flag ? Color.White : theme.Primary);
				}
			}
			theme = MakeEffectiveTheme();
			ApplyTheme();
			if (btnNavSearch != null)
			{
				btnNavSearch.Visible = showSearchButton;
				RefreshNavButtonSizes();
			}
			SaveConfig();
			dlg.DialogResult = DialogResult.OK;
			dlg.Close();
		};
		// “取消”按钮：不保存任何修改，直接关闭窗口
		Button btnCancel = new Button
		{
			Text = "取消",
			Font = GetFont(10f),
			ForeColor = textColor,
			BackColor = Color.Transparent,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(SY(110), SY(35)),
			Cursor = Cursors.Hand
		};
		StyleOutlineButton(btnCancel, 17, borderColor, textColor);
		btnCancel.Click += delegate
		{
			dlg.Close();
		};
		int effCardW = cardWidth; // 当前有效卡片宽度（出现竖直滚动条时会扣除其宽度，以保持左右对称）
		// 布局刷新委托：按当前可见卡片重新排列各卡片位置，并据内容总高度调整窗口尺寸（保证底部按钮不被隔断）
		Action UpdateCardsLayout = delegate
		{
			int num = cardStartY;
			engineCard.Location = At(cardX, num);
			num += engineCardH + cardGap;
			perfCard.Location = At(cardX, num);
			num += perfCardH + cardGap;
			if (funcCard.Visible)
			{
				funcCard.Location = At(cardX, num);
				num += funcCardH + cardGap;
			}
			customCard.Location = At(cardX, num);
			num += customCardH + cardGap;
			int num2 = num - cardStartY + scrollBottomPad; // 内容所需总高度
			// 计算窗口客户区高度（含上下留白、按钮区、外框边距与标题栏）；被屏幕高度限制时可能出现竖直滚动条
			int num3 = Math.Min(Math.Max(num2 + scrollTopPad + scrollBottomPad + btnPanelH + ctx.TitleHeight + 2 * ctx.Margin + SY(2), SY(450)), (int)((double)screenHeight * 0.95));
			// 判断竖直滚动条是否会出现：内容高度超过可见区域高度
			int scrollAvailH = num3 - 2 * ctx.Margin - ctx.TitleHeight - btnPanelH - scrollTopPad - scrollBottomPad;
			bool vScroll = num2 > scrollAvailH;
			int scW = vScroll ? SystemInformation.VerticalScrollBarWidth : 0;
			// 出现滚动条时，从卡片宽度与窗口宽度中【各扣除一次】滚动条宽度，使卡片在 Body 内仍左右居中、左右留白对称
			effCardW = (windowWidth - SX(64)) - scW;
			engineCard.Size = new Size(effCardW, engineCardH);
			perfCard.Size = new Size(effCardW, perfCardH);
			if (funcCard.Visible) funcCard.Size = new Size(effCardW, funcCardH);
			customCard.Size = new Size(effCardW, customCardH);
			scrollContainer.AutoScrollMinSize = new Size(effCardW, num2);
			int targetW = windowWidth - scW;
			if (dlg.ClientSize.Width != targetW || dlg.ClientSize.Height != num3)
			{
				dlg.ClientSize = new Size(targetW, num3);
				dlg.Location = At(screenWorkArea.Left + (screenWorkArea.Width - dlg.Width) / 2, screenWorkArea.Top + (screenWorkArea.Height - dlg.Height) / 2);
			}
		};
		// “高级功能”按钮（默认隐藏，悬停“恢复默认”3 秒彩蛋后出现）：点击展开/收起“功能开关”卡片
		Button btnAdvanced = new Button
		{
			Text = "高级功能",
			Font = GetFont(10f),
			ForeColor = accentColor,
			BackColor = Color.Transparent,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(SX(110), SY(35)),
			Visible = false,
			Cursor = Cursors.Hand
		};
		btnAdvanced.FlatAppearance.BorderSize = 0;
		btnAdvanced.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, SX(110), SY(35)), 17));
		btnAdvanced.Click += delegate
		{
			funcCard.Visible = !funcCard.Visible;
			UpdateCardsLayout();
		};
		btnPanel.Controls.Add(btnAdvanced);
		btnPanel.Controls.Add(btnReset);
		btnPanel.Controls.Add(btnOK);
		btnPanel.Controls.Add(btnCancel);
		// 彩蛋计时器：悬停“恢复默认”按钮满 3 秒后显示“高级功能”按钮
		System.Windows.Forms.Timer advEggTimer = new System.Windows.Forms.Timer
		{
			Interval = 3000
		};
		try
		{
			// 彩蛋计时器：鼠标移出“高级功能”按钮 1 秒后将其隐藏
			System.Windows.Forms.Timer advHideTimer = new System.Windows.Forms.Timer
			{
				Interval = 1000
			};
			try
			{
				advEggTimer.Tick += delegate
				{
					advEggTimer.Stop();
					btnAdvanced.Visible = true;
					btnAdvanced.Refresh();
				};
				advHideTimer.Tick += delegate
				{
					advHideTimer.Stop();
					btnAdvanced.Visible = false;
				};
				// 彩蛋逻辑：递归为控件及其子控件挂接鼠标事件——悬停“恢复默认”启动显示计时器、移出则取消
		Action<Control> resetWireUpWithEgg = null;
				resetWireUpWithEgg = delegate(Control ctrl)
				{
					ctrl.MouseEnter += delegate
					{
						advEggTimer.Start();
						advHideTimer.Stop();
					};
					ctrl.MouseLeave += delegate
					{
						advEggTimer.Stop();
						if (btnAdvanced.Visible)
						{
							advHideTimer.Start();
						}
					};
					foreach (Control obj in ctrl.Controls)
					{
						resetWireUpWithEgg(obj);
					}
				};
				resetWireUpWithEgg(btnReset);
				// 彩蛋逻辑：为“高级功能”按钮挂接鼠标事件——悬停取消隐藏计时器、移出则启动隐藏计时器
		Action<Control> advWireUpWithHide = null;
				advWireUpWithHide = delegate(Control ctrl)
				{
					ctrl.MouseEnter += delegate
					{
						advHideTimer.Stop();
					};
					ctrl.MouseLeave += delegate
					{
						if (btnAdvanced.Visible)
						{
							advHideTimer.Start();
						}
					};
					foreach (Control obj in ctrl.Controls)
					{
						advWireUpWithHide(obj);
					}
				};
				advWireUpWithHide(btnAdvanced);
				funcCard.Visible = false;
			UpdateCardsLayout();
			// 按钮面板重绘时定位四个按钮：恢复默认/高级功能靠左，确定/取消靠右
			btnPanel.Paint += delegate
				{
					int num = cardX + effCardW;
					btnReset.Location = At(cardX, SY(15));
					btnAdvanced.Location = At(cardX, SY(15));
					btnOK.Location = At(num - SX(250), SY(15));
					btnCancel.Location = At(num - SX(115), SY(15));
				};
				dlg.ShowDialog();
			}
			finally
			{
				if (advHideTimer != null)
				{
					((IDisposable)advHideTimer).Dispose();
				}
			}
		}
		finally
		{
			if (advEggTimer != null)
			{
				((IDisposable)advEggTimer).Dispose();
			}
		}
		// 辅助方法：创建一个带指定背景色/边框色的卡片容器 Panel（尺寸由 cardWidth 决定，高度后续单独设置）
		Panel CreateCard(Color bg, Color border)
		{
			return new Panel
			{
				Size = new Size(cardWidth, 0),
				Location = At(cardX, cardY),
				BackColor = bg,
				BorderStyle = BorderStyle.None
			};
		}
		// 辅助方法：为卡片 Panel 绘制 2px 抗锯齿圆角边框（跟随 panel 实时尺寸，确保缩放后边框仍完整）
		static void PaintCardBorder(Panel panel, Color border)
		{
			panel.Paint += delegate(object s, PaintEventArgs pe)
			{
				pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(border, 2f);
				using GraphicsPath path = GetRoundedPath(new Rectangle(1, 1, panel.Width - 3, panel.Height - 3), 8);
				pe.Graphics.DrawPath(pen, path);
			};
		}
	}

	private void ShowAboutDialog()
	{
		bool isDark = theme != null && DrawingUtils.IsDarkColor(theme.Bg);
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Color bgColor = theme.Bg;
		Color textColor = pal.Label;
		Color subTextColor = pal.Muted;
		Color accentColor = pal.Neon;
		Color featureCardBg = isDark ? ControlPaint.Light(theme.Surface, 0.06f) : ControlPaint.Light(theme.Surface, 0.5f);
		Color promoCardBg = isDark ? ControlPaint.Light(theme.Surface, 0.12f) : ControlPaint.Light(theme.Surface, 0.65f);
		Color feedbackCardBg = isDark ? ControlPaint.Light(theme.Surface, 0.09f) : ControlPaint.Light(theme.Surface, 0.55f);
		Color featureCardBorder = pal.Border;
		Color promoCardBorder = pal.Border;
		Color feedbackCardBorder = pal.Border;
		string version = AppConstants.CurrentVersion;
		int dlgW = SX(880);
		int dlgH = SY(400);
		int cx = SX(14);
		int cw = SX(820);
		int cardRadius = SX(8);
		int cardGap = SY(10);
		Form dlg = new Form();
		try
		{
			dlg.Text = "关于";
			dlg.StartPosition = FormStartPosition.CenterParent;
			dlg.MaximizeBox = false;
			dlg.MinimizeBox = false;
			dlg.ForeColor = textColor;
			dlg.Font = GetFont(SF(9f));
			dlg.ShowInTaskbar = false;
			dlg.TopMost = false;
			dlg.Icon = this.Icon;
			dlg.ClientSize = new Size(dlgW, dlgH);
			var ctx = NeonChrome.Apply(dlg, pal, "关于", dpiScale);
			Point At(int x, int yy) => new Point(x, yy);
			try
			{
				typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(dlg, true, null);
			}
			catch
			{
			}
			dlg.HandleCreated += delegate
			{
				if (isDark)
				{
					try
					{
						int darkMode = 1;
						DarkMessageBox.ApplyDarkTitleBar(dlg.Handle, darkMode);
					}
					catch
					{
					}
				}
			};
			int y = SY(16);
			int topCardH = SY(88);
			Panel topCard = new Panel
			{
				Location = At(cx, y),
				Size = new Size(cw, topCardH),
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(topCard);
			Font topTitleFont = GetFont(SF(15f), FontStyle.Bold);
			Font verFont = GetFont(SF(9.5f));
			int iconSize = SX(56);
			Size topTitleSize = TextRenderer.MeasureText("IPTV 直播源检测工具", topTitleFont);
			TextRenderer.MeasureText("版本 " + version, verFont);
			int gap1 = SX(18);
			int totalW = iconSize + gap1 + topTitleSize.Width;
			int startX = (cw - totalW) / 2;
			int contentH = iconSize;
			int num = (topCardH - contentH) / 2;
			int iconY = num + (contentH - iconSize) / 2;
			int titleY = num + (contentH - topTitleSize.Height) / 2;
			Panel iconPanel = new Panel
			{
				Location = At(startX, iconY),
				Size = new Size(iconSize, iconSize),
				BackColor = Color.Transparent
			};
			iconPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using (SolidBrush brush = new SolidBrush(accentColor))
				{
					using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, iconSize - 1, iconSize - 1), SX(12));
					graphics.FillPath(brush, path);
				}
				int num3 = SX(12);
				int num4 = SX(14);
				int num5 = SX(32);
				int num6 = SY(22);
				using Pen pen = new Pen(Color.White, 2f);
				graphics.DrawRectangle(pen, num3, num4, num5, num6);
				graphics.DrawLine(pen, num3 + SX(6), num4 + num6 + SX(7), num3 + num5 - SX(6), num4 + num6 + SX(7));
				graphics.DrawLine(pen, num3 + num5 / 2, num4 + num6, num3 + num5 / 2, num4 + num6 + SX(7));
			};
			topCard.Controls.Add(iconPanel);
			int textStartX = startX + iconSize + gap1;
			Label lblTitle = new Label
			{
				Text = "IPTV 直播源检测工具",
				Font = topTitleFont,
				Location = At(textStartX, titleY),
				AutoSize = true,
				ForeColor = textColor,
				BackColor = Color.Transparent
			};
			topCard.Controls.Add(lblTitle);
			Font verFontSmall = GetFont(SF(6f));
			Size versionSizeSmall = TextRenderer.MeasureText("版本 " + version, verFontSmall);
			Label lblVersion = new Label
			{
				Text = "版本 " + version,
				Font = verFontSmall,
				Location = At(textStartX + topTitleSize.Width - versionSizeSmall.Width, titleY + topTitleSize.Height + SY(2)),
				AutoSize = true,
				ForeColor = accentColor,
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleRight
			};
			topCard.Controls.Add(lblVersion);
			y += topCardH + cardGap;
			int featCardH = SY(150);
			Panel featCard = new Panel
			{
				Location = At(cx, y),
				Size = new Size(cw, featCardH),
				BackColor = featureCardBg
			};
			featCard.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, featCard.Width - 1, featCard.Height - 1), cardRadius);
				using Pen pen = new Pen(featureCardBorder, 1f);
				graphics.DrawPath(pen, path);
			};
			ctx.Body.Controls.Add(featCard);
			Label lblFeatTitle = new Label
			{
				Text = "功能概述",
				Font = GetFont(SF(10.5f), FontStyle.Bold),
				Location = At(SX(16), SY(14)),
				AutoSize = true,
				ForeColor = textColor,
				BackColor = Color.Transparent
			};
			featCard.Controls.Add(lblFeatTitle);
			string[][] features = new string[6][]
			{
				new string[2] { "•", "批量检测 IPTV 直播源可用性" },
				new string[2] { "•", "自动识别视频分辨率和编码格式" },
				new string[2] { "•", "支持 ffprobe/ffmpeg/MediaInfo" },
				new string[2] { "•", "内置链接解析、搜索、分组管理" },
				new string[2] { "•", "支持合并导出、源生成器批量生成" },
				new string[2] { "•", "支持 IP 归属地、响应速度测试" }
			};
			int colCount = 2;
			int itemH = SY(30);
			int startYFeat = SY(42);
			int colW = (cw - SX(32)) / colCount;
			for (int i = 0; i < features.Length; i++)
			{
				int col = i % colCount;
				int row = i / colCount;
				int itemX = SX(16) + col * colW;
				int itemY = startYFeat + row * itemH;
				Panel itemPanel = new Panel
				{
					Location = At(itemX, itemY),
					Size = new Size(colW - SX(8), itemH - SY(4)),
					BackColor = Color.Transparent
				};
				itemPanel.MouseEnter += delegate
				{
					itemPanel.BackColor = ControlPaint.Light(featureCardBg, 0.1f);
				};
				itemPanel.MouseLeave += delegate
				{
					itemPanel.BackColor = Color.Transparent;
				};
				featCard.Controls.Add(itemPanel);
				Label lblIcon = new Label
				{
					Text = features[i][0],
					Font = GetFont(SF(10f)),
					Location = At(SX(4), SY(3)),
					Size = new Size(SX(24), SY(22)),
					ForeColor = textColor,
					BackColor = Color.Transparent,
					TextAlign = ContentAlignment.MiddleCenter
				};
				itemPanel.Controls.Add(lblIcon);
				Label lblDesc = new Label
				{
					Text = features[i][1],
					Font = GetFont(SF(9f)),
					Location = At(SX(32), SY(2)),
					Size = new Size(colW - SX(42), SY(22)),
					ForeColor = subTextColor,
					BackColor = Color.Transparent,
					TextAlign = ContentAlignment.MiddleLeft
				};
				itemPanel.Controls.Add(lblDesc);
			}
			y += featCardH + cardGap;
			int promoCardH = SY(220);
			Panel promoCard = new Panel
			{
				Location = At(cx, y),
				Size = new Size(cw, promoCardH),
				BackColor = promoCardBg,
				Cursor = Cursors.Hand,
				Visible = false
			};
			promoCard.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, promoCard.Width - 1, promoCard.Height - 1), cardRadius);
				using Pen pen = new Pen(promoCardBorder, 1f);
				graphics.DrawPath(pen, path);
			};
			ctx.Body.Controls.Add(promoCard);
			Font promoLeftFont = GetFont(SF(12f), FontStyle.Bold);
			Font promoMidFont = GetFont(SF(10f));
			Font promoRightFont = GetFont(SF(9.5f), FontStyle.Italic);
			string promoLeftText = "\ud83c\udfaf 关注公众号";
			string promoMidText = "微信搜一搜「文娱茶话会」";
			string promoRightText = "点击复制";
			Size promoLeftSize = TextRenderer.MeasureText(promoLeftText, promoLeftFont);
			Size promoMidSize = TextRenderer.MeasureText(promoMidText, promoMidFont);
			Size promoRightSize = TextRenderer.MeasureText(promoRightText, promoRightFont);
			int textBarHeight = SY(32);
			int textBarPadding = SY(14);
			int textBarY = SY(2);
			Label lblPromoLeft = new Label
			{
				Text = promoLeftText,
				Font = promoLeftFont,
				Size = promoLeftSize,
				ForeColor = (isDark ? Color.FromArgb(120, 220, 140) : Color.FromArgb(40, 140, 70)),
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleLeft
			};
			lblPromoLeft.Location = At(textBarPadding, textBarY + (textBarHeight - promoLeftSize.Height) / 2);
			promoCard.Controls.Add(lblPromoLeft);
			Label lblPromoMid = new Label
			{
				Text = promoMidText,
				Font = promoMidFont,
				Size = promoMidSize,
				ForeColor = textColor,
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter
			};
			lblPromoMid.Location = At((cw - promoMidSize.Width) / 2, textBarY + (textBarHeight - promoMidSize.Height) / 2);
			promoCard.Controls.Add(lblPromoMid);
			Label lblPromoRight = new Label
			{
				Text = promoRightText,
				Font = promoRightFont,
				Size = promoRightSize,
				ForeColor = (isDark ? Color.FromArgb(100, 200, 120) : Color.FromArgb(60, 160, 90)),
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleRight
			};
			lblPromoRight.Location = At(cw - textBarPadding - promoRightSize.Width, textBarY + (textBarHeight - promoRightSize.Height) / 2);
			promoCard.Controls.Add(lblPromoRight);
			int imgAreaTopPad = SY(2);
			SY(8);
			int imgAreaLeftPad = SX(40);
			int imgAreaRightPad = SX(40);
			int num2 = textBarY + textBarHeight + imgAreaTopPad;
			int promoImgTargetW = cw - imgAreaLeftPad - imgAreaRightPad;
			int borderSize = SX(2);
			Bitmap promoImg = LoadWechatPromoImage(promoImgTargetW);
			int promoImgW;
			int promoImgH;
			if (promoImg != null)
			{
				promoImgW = promoImg.Width;
				promoImgH = promoImg.Height;
			}
			else
			{
				promoImgW = promoImgTargetW;
				promoImgH = (int)((double)promoImgTargetW * 219.0 / 600.0);
			}
			int promoImgX = (cw - promoImgW) / 2;
			int promoImgY = num2;
			Color greenBorderColor = (isDark ? Color.FromArgb(60, 160, 90) : Color.FromArgb(80, 180, 110));
			Panel imgPanel = new Panel
			{
				Location = At(promoImgX - borderSize, promoImgY - borderSize),
				Size = new Size(promoImgW + borderSize * 2, promoImgH + borderSize * 2),
				BackColor = Color.White
			};
			imgPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				using (GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, imgPanel.Width - 1, imgPanel.Height - 1), SX(8)))
				{
					using SolidBrush brush = new SolidBrush(Color.White);
					graphics.FillPath(brush, path);
				}
				if (promoImg != null)
				{
					int num3 = borderSize;
					int num4 = borderSize;
					int num5 = promoImgW;
					int num6 = promoImgH;
					graphics.DrawImage(promoImg, num3, num4, num5, num6);
				}
				using GraphicsPath path2 = CreateRoundedRectPath(new Rectangle(1, 1, imgPanel.Width - 3, imgPanel.Height - 3), SX(8));
				using Pen pen = new Pen(greenBorderColor, borderSize);
				graphics.DrawPath(pen, path2);
			};
			promoCard.Controls.Add(imgPanel);
			imgPanel.BringToFront();
			Color promoNormalBg = promoCardBg;
			Color promoHoverBg = (isDark ? Color.FromArgb(35, 60, 48) : Color.FromArgb(225, 242, 228));
			Color promoPressBg = (isDark ? Color.FromArgb(42, 70, 55) : Color.FromArgb(210, 235, 218));
			bool promoIsHover = false;
			Action<Control> promoWireUp = null;
			promoWireUp = delegate(Control ctrl)
			{
				ctrl.MouseEnter += delegate
				{
					promoIsHover = true;
					promoCard.BackColor = promoHoverBg;
					promoCard.Cursor = Cursors.Hand;
				};
				ctrl.MouseLeave += delegate
				{
					promoIsHover = false;
					promoCard.BackColor = promoNormalBg;
					promoCard.Cursor = Cursors.Default;
				};
				ctrl.MouseDown += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						promoCard.BackColor = promoPressBg;
					}
				};
				ctrl.MouseUp += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						promoCard.BackColor = (promoIsHover ? promoHoverBg : promoNormalBg);
					}
				};
				ctrl.Click += async delegate
				{
					try
					{
						Clipboard.SetText("文娱茶话会");
						promoCard.BackColor = promoPressBg;
						await Task.Delay(100);
						promoCard.BackColor = (promoIsHover ? promoHoverBg : promoNormalBg);
					}
					catch
					{
					}
				};
				foreach (Control obj2 in ctrl.Controls)
				{
					promoWireUp(obj2);
				}
			};
			promoWireUp(promoCard);
			y += promoCardH + cardGap;
			int fbCardH = SY(110);
			Panel fbCard = new Panel
			{
				Location = At(cx, y),
				Size = new Size(cw, fbCardH),
				BackColor = feedbackCardBg
			};
			fbCard.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, fbCard.Width - 1, fbCard.Height - 1), cardRadius);
				using Pen pen = new Pen(feedbackCardBorder, 1f);
				graphics.DrawPath(pen, path);
			};
			ctx.Body.Controls.Add(fbCard);
			Label lblBugTitle = new Label
			{
				Text = "问题反馈 & 交流",
				Font = GetFont(SF(10.5f), FontStyle.Bold),
				Location = At(SX(16), SY(14)),
				AutoSize = true,
				ForeColor = textColor,
				BackColor = Color.Transparent
			};
			fbCard.Controls.Add(lblBugTitle);
			int infoCardW = (cw - SX(32) - SX(12)) / 2;
			int infoCardH = SY(54);
			int infoCardY = SY(44);
			Label lblEmail = null;
			Panel emailCard = new Panel
			{
				Location = At(SX(16), infoCardY),
				Size = new Size(infoCardW, infoCardH),
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			emailCard.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, emailCard.Width - 1, emailCard.Height - 1), SX(6));
				using Pen pen = new Pen(feedbackCardBorder, 1f);
				graphics.DrawPath(pen, path);
			};
			Color emailNormalBg = Color.Transparent;
			Color emailHoverBg = (isDark ? Color.FromArgb(40, 52, 75) : Color.FromArgb(225, 233, 248));
			Color emailPressBg = (isDark ? Color.FromArgb(50, 65, 90) : Color.FromArgb(210, 222, 245));
			bool emailIsHover = false;
			Action<Control> emailWireUp = null;
			emailWireUp = delegate(Control ctrl)
			{
				ctrl.MouseEnter += delegate
				{
					emailIsHover = true;
					emailCard.BackColor = emailHoverBg;
					emailCard.Cursor = Cursors.Hand;
					if (lblEmail != null)
					{
						lblEmail.Font = GetFont(SF(8.5f), FontStyle.Underline);
					}
				};
				ctrl.MouseLeave += delegate
				{
					emailIsHover = false;
					emailCard.BackColor = emailNormalBg;
					emailCard.Cursor = Cursors.Default;
					if (lblEmail != null)
					{
						lblEmail.Font = GetFont(SF(8.5f));
					}
				};
				ctrl.MouseDown += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						emailCard.BackColor = emailPressBg;
					}
				};
				ctrl.MouseUp += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						emailCard.BackColor = (emailIsHover ? emailHoverBg : emailNormalBg);
					}
				};
				ctrl.Click += async delegate
				{
					try
					{
						Clipboard.SetText("xiaomiren0510@gmail.com");
						emailCard.BackColor = emailPressBg;
						await Task.Delay(80);
						emailCard.BackColor = (emailIsHover ? emailHoverBg : emailNormalBg);
						Process.Start("mailto:xiaomiren0510@gmail.com?subject=IPTV直播源检测工具 - BUG反馈");
					}
					catch
					{
					}
				};
				foreach (Control obj2 in ctrl.Controls)
				{
					emailWireUp(obj2);
				}
			};
			fbCard.Controls.Add(emailCard);
			Label lblEmailIcon = new Label
			{
				Text = "\ud83d\udce7",
				Font = GetFont(SF(14f)),
				Location = At(SX(12), SY(13)),
				Size = new Size(SX(28), SY(28)),
				ForeColor = textColor,
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter
			};
			emailCard.Controls.Add(lblEmailIcon);
			Label lblEmailTitle = new Label
			{
				Text = "邮箱反馈",
				Font = GetFont(SF(8.5f), FontStyle.Bold),
				Location = At(SX(46), SY(8)),
				AutoSize = true,
				ForeColor = subTextColor,
				BackColor = Color.Transparent
			};
			emailCard.Controls.Add(lblEmailTitle);
			lblEmail = new Label
			{
				Text = "xiaomiren0510@gmail.com",
				Font = GetFont(SF(8.5f)),
				Location = At(SX(46), SY(26)),
				AutoSize = true,
				ForeColor = accentColor,
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			emailCard.Controls.Add(lblEmail);
			emailWireUp(emailCard);
			Label lblTgChannel = null;
			Panel tgCard = new Panel
			{
				Location = At(SX(16) + infoCardW + SX(12), infoCardY),
				Size = new Size(infoCardW, infoCardH),
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			tgCard.Paint += delegate(object s, PaintEventArgs e)
			{
				using Graphics graphics = e.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = CreateRoundedRectPath(new Rectangle(0, 0, tgCard.Width - 1, tgCard.Height - 1), SX(6));
				using Pen pen = new Pen(feedbackCardBorder, 1f);
				graphics.DrawPath(pen, path);
			};
			Color tgNormalBg = Color.Transparent;
			Color tgHoverBg = (isDark ? Color.FromArgb(40, 52, 75) : Color.FromArgb(225, 233, 248));
			Color tgPressBg = (isDark ? Color.FromArgb(50, 65, 90) : Color.FromArgb(210, 222, 245));
			bool tgIsHover = false;
			bool isTgRevealed = false;
			Action<Control> tgWireUp = null;
			tgWireUp = delegate(Control ctrl)
			{
				ctrl.MouseEnter += delegate
				{
					tgIsHover = true;
					tgCard.BackColor = tgHoverBg;
					tgCard.Cursor = Cursors.Hand;
					if (lblTgChannel != null)
					{
						lblTgChannel.Font = GetFont(SF(8.5f), FontStyle.Underline);
					}
				};
				ctrl.MouseLeave += delegate
				{
					tgIsHover = false;
					tgCard.BackColor = tgNormalBg;
					tgCard.Cursor = Cursors.Default;
					if (lblTgChannel != null)
					{
						lblTgChannel.Font = GetFont(SF(8.5f));
					}
				};
				ctrl.MouseDown += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						tgCard.BackColor = tgPressBg;
					}
				};
				ctrl.MouseUp += delegate(object s, MouseEventArgs e)
				{
					if (e.Button == MouseButtons.Left)
					{
						tgCard.BackColor = (tgIsHover ? tgHoverBg : tgNormalBg);
					}
				};
				ctrl.Click += async delegate
				{
					try
					{
						tgCard.BackColor = tgPressBg;
						await Task.Delay(80);
						tgCard.BackColor = (tgIsHover ? tgHoverBg : tgNormalBg);
						if (isTgRevealed)
						{
							Process.Start("https://t.me/+jTncKg0Vbrg5YjI1");
						}
						else
						{
							Process.Start("https://github.com/281761526/IPTVLiveChecker");
						}
					}
					catch
					{
					}
				};
				foreach (Control obj2 in ctrl.Controls)
				{
					tgWireUp(obj2);
				}
			};
			fbCard.Controls.Add(tgCard);
			Label lblTgIcon = new Label
			{
				Text = "\ud83d\udcbb",
				Font = GetFont(SF(14f)),
				Location = At(SX(12), SY(13)),
				Size = new Size(SX(28), SY(28)),
				ForeColor = textColor,
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter
			};
			tgCard.Controls.Add(lblTgIcon);
			Label lblTgTitle = new Label
			{
				Text = "GitHub",
				Font = GetFont(SF(8.5f), FontStyle.Bold),
				Location = At(SX(46), SY(8)),
				AutoSize = true,
				ForeColor = subTextColor,
				BackColor = Color.Transparent
			};
			tgCard.Controls.Add(lblTgTitle);
			lblTgChannel = new Label
			{
				Text = "github.com/281761526/IPTVLiveChecker",
				Font = GetFont(SF(8.5f)),
				Location = At(SX(46), SY(26)),
				AutoSize = true,
				ForeColor = accentColor,
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			tgCard.Controls.Add(lblTgChannel);
			tgWireUp(tgCard);
			Label lblDisclaimerLink = new Label
			{
				Text = "免责声明",
				Font = GetFont(SF(8.5f), FontStyle.Underline),
				Location = At(SX(16), SY(14)),
				AutoSize = true,
				ForeColor = accentColor,
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			lblDisclaimerLink.MouseEnter += delegate
			{
				lblDisclaimerLink.Font = GetFont(SF(8.5f), FontStyle.Bold | FontStyle.Underline);
			};
			lblDisclaimerLink.MouseLeave += delegate
			{
				lblDisclaimerLink.Font = GetFont(SF(8.5f), FontStyle.Underline);
			};
			lblDisclaimerLink.Click += delegate
			{
				dlg.Close();
				ShowDisclaimerDialog();
			};
			fbCard.Controls.Add(lblDisclaimerLink);
			Font authorFont = GetFont(SF(8f));
			Size authorSize = TextRenderer.MeasureText("— Designed by 半步沧桑 —", authorFont);
			int authorH = authorSize.Height + SY(4);
			Label lblAuthor = new Label
			{
				Text = "— Designed by 半步沧桑 —",
				Font = authorFont,
				AutoSize = true,
				ForeColor = (isDark ? Color.FromArgb(110, 120, 135) : Color.FromArgb(170, 180, 195)),
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter
			};
			ctx.Body.Controls.Add(lblAuthor);
			UpdateLayout();
			System.Windows.Forms.Timer promoEggTimer = new System.Windows.Forms.Timer
			{
				Interval = 3000
			};
			try
			{
				System.Windows.Forms.Timer promoHideTimer = new System.Windows.Forms.Timer
				{
					Interval = 1000
				};
				try
				{
					System.Windows.Forms.Timer tgEggTimer = new System.Windows.Forms.Timer
					{
						Interval = 3000
					};
					try
					{
						promoEggTimer.Tick += delegate
						{
							promoEggTimer.Stop();
							promoCard.Visible = true;
							promoCard.Refresh();
							UpdateLayout();
						};
						promoHideTimer.Tick += delegate
						{
							promoHideTimer.Stop();
							promoCard.Visible = false;
							UpdateLayout();
						};
						tgEggTimer.Tick += delegate
						{
							tgEggTimer.Stop();
							isTgRevealed = true;
							lblTgIcon.Text = "\ud83d\udce2";
							lblTgTitle.Text = "TG 频道";
							lblTgChannel.Text = "t.me/+jTncKg0Vbrg5YjI1";
						};
						Action<Control> emailWireUpWithEgg = null;
						emailWireUpWithEgg = delegate(Control ctrl)
						{
							ctrl.MouseEnter += delegate
							{
								promoEggTimer.Start();
								promoHideTimer.Stop();
							};
							ctrl.MouseLeave += delegate
							{
								promoEggTimer.Stop();
								if (promoCard.Visible)
								{
									promoHideTimer.Start();
								}
							};
							foreach (Control obj2 in ctrl.Controls)
							{
								emailWireUpWithEgg(obj2);
							}
						};
						emailWireUpWithEgg(emailCard);
						Action<Control> promoWireUpWithHide = null;
						promoWireUpWithHide = delegate(Control ctrl)
						{
							ctrl.MouseEnter += delegate
							{
								promoHideTimer.Stop();
							};
							ctrl.MouseLeave += delegate
							{
								if (promoCard.Visible)
								{
									promoHideTimer.Start();
								}
							};
							foreach (Control obj2 in ctrl.Controls)
							{
								promoWireUpWithHide(obj2);
							}
						};
						promoWireUpWithHide(promoCard);
						Action<Control> tgWireUpWithEgg = null;
						tgWireUpWithEgg = delegate(Control ctrl)
						{
							ctrl.MouseEnter += delegate
							{
								if (!isTgRevealed)
								{
									tgEggTimer.Start();
								}
							};
							ctrl.MouseLeave += delegate
							{
								tgEggTimer.Stop();
							};
							foreach (Control obj2 in ctrl.Controls)
							{
								tgWireUpWithEgg(obj2);
							}
						};
						tgWireUpWithEgg(tgCard);
						dlg.ShowDialog(this);
					}
					finally
					{
						if (tgEggTimer != null)
						{
							((IDisposable)tgEggTimer).Dispose();
						}
					}
				}
				finally
				{
					if (promoHideTimer != null)
					{
						((IDisposable)promoHideTimer).Dispose();
					}
				}
			}
			finally
			{
				if (promoEggTimer != null)
				{
					((IDisposable)promoEggTimer).Dispose();
				}
			}
			void UpdateLayout()
			{
				dlg.SuspendLayout();
				int promoOffset = (promoCard.Visible ? (promoCardH + cardGap) : 0);
				promoCard.Location = At(cx, SY(16) + topCardH + cardGap + featCardH + cardGap);
				fbCard.Location = At(cx, SY(16) + topCardH + cardGap + featCardH + cardGap + promoOffset);
				int authorY = SY(16) + topCardH + cardGap + featCardH + cardGap + promoOffset + fbCardH + cardGap;
				lblAuthor.Location = At((dlgW - authorSize.Width) / 2, authorY);
				int totalH = authorY + authorH + SY(14) + ctx.TitleHeight + 2 * ctx.Margin;
				dlg.ClientSize = new Size(dlgW, totalH);
				ctx.Body.Size = new Size(dlgW - 2 * ctx.Margin, totalH - 2 * ctx.Margin - ctx.TitleHeight);
				int ownerX = base.Left + (base.Width - dlg.Width) / 2;
				int ownerY = base.Top + (base.Height - dlg.Height) / 2;
				dlg.Location = At(Math.Max(0, ownerX), Math.Max(0, ownerY));
				dlg.ResumeLayout();
				dlg.Invalidate();
				dlg.Update();
			}
		}
		finally
		{
			if (dlg != null)
			{
				((IDisposable)dlg).Dispose();
			}
		}
	}

	private Point At(int x, int yy) => new Point(x, yy);

	private void RefreshFontsImmediately()
	{
		Font = GetFont(SF(10.5f));
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
		RefreshControlFonts(base.Controls);
		RefreshContextMenuFonts();
		config.Initialize(dpiScale);
		RefreshNavButtonSizes();
		RefreshComponentSizes();
		Invalidate();
	}

	private void RefreshNavButtonSizes()
	{
		if (titleBarPanel == null || btnNavDetect == null)
		{
			return;
		}
		Font navFont = btnNavDetect.Font;
		int requiredBtnWidth = 0;
		using (Graphics g = Graphics.FromHwnd(base.Handle))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			Button[] array = new Button[4] { btnNavDetect, btnNavSearch, btnNavSettings, btnNavAbout };
			foreach (Button btn in array)
			{
				if (btn != null)
				{
					SizeF textSize = g.MeasureString(btn.Text, btn.Font);
					requiredBtnWidth = Math.Max(requiredBtnWidth, (int)textSize.Width);
				}
			}
		}
		requiredBtnWidth += 24;
		int requiredBtnHeight = (int)((double)navFont.Height * 1.4);
		int maxBtnHeight = (int)((double)titleBarPanel.Height * 0.9);
		requiredBtnHeight = Math.Min(requiredBtnHeight, maxBtnHeight);
		int navBtnY = (titleBarPanel.Height - requiredBtnHeight) / 2;
		int navBtnRadius = 4;
		int navBtnGap = 1;
		int startX = SX(42);
		Action<Button> updateBtn = delegate(Button button)
		{
			if (button != null)
			{
				button.Width = requiredBtnWidth;
				button.Height = requiredBtnHeight;
				button.Top = navBtnY;
				button.Region?.Dispose();
				using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, button.Width, button.Height), navBtnRadius))
				{
					button.Region = new Region(path);
				}
				button.Invalidate();
			}
		};
		if (btnNavFile != null)
		{
			btnNavFile.Left = startX;
			updateBtn(btnNavFile);
			startX = btnNavFile.Right + navBtnGap;
		}
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
			using (Graphics.FromHwnd(base.Handle))
			{
				int btnW = SX(126);
				int leftX = SX(12);
				foreach (Control control in actionArea.Controls)
				{
					if (control is Button btn && !string.IsNullOrEmpty(btn.Text))
					{
						btn.Width = btnW;
						btn.Left = leftX;
						btn.Region?.Dispose();
						using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), 8))
						{
							btn.Region = new Region(path);
						}
						btn.Invalidate();
					}
				}
			}
		}
		if (searchPanelRef != null)
		{
			using Graphics g = Graphics.FromHwnd(base.Handle);
			Font baseFont = GetFont(SF(8.5f));
			SizeF textSize = g.MeasureString("搜 索 :", baseFont);
			int requiredHeight = (int)((double)textSize.Height * 1.6) + 8;
			requiredHeight = Math.Max(requiredHeight, SY(32));
			searchPanelRef.Height = requiredHeight;
			foreach (Control ctrl in searchPanelRef.Controls)
			{
				if (ctrl is Label lbl && (lbl.Text == "搜 索 :" || lbl.Text == "分组:"))
				{
					lbl.Height = (int)textSize.Height + 4;
					lbl.Top = (searchPanelRef.Height - lbl.Height) / 2;
				}
				else if (ctrl is Panel { Width: 110 } p)
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
		if (statusBarRef != null)
		{
			using (Graphics g2 = Graphics.FromHwnd(base.Handle))
			{
				Font statusFont = GetFont(SF(9.5f));
				int requiredHeight2 = (int)((double)g2.MeasureString("已检测: 0/0", statusFont).Height * 1.6) + 4;
				requiredHeight2 = Math.Max(requiredHeight2, SY(24));
				statusBarRef.Height = requiredHeight2;
			}
			LayoutStatusBar(statusBarRef);
			UpdateStatusBarRegion();
		}
		if (dgvData != null)
		{
			using (Graphics g3 = Graphics.FromHwnd(base.Handle))
			{
				Font rowFont = GetFont(SF(6.7f));
				int requiredRowHeight = (int)((double)g3.MeasureString("测试文字", rowFont).Height * 1.4) + 4;
				requiredRowHeight = Math.Max(requiredRowHeight, SY(28));
				dgvData.RowTemplate.Height = requiredRowHeight;
				Font headerFont = GetFont(SF(9f));
				int requiredHeaderHeight = (int)((double)g3.MeasureString("名称", headerFont).Height * 1.4) + 4;
				requiredHeaderHeight = Math.Max(requiredHeaderHeight, SY(30));
				dgvData.ColumnHeadersHeight = requiredHeaderHeight;
			}
			ApplyColumnWidthsManual();
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
				if (item is ToolStripMenuItem { HasDropDownItems: not false } menuItem)
				{
					RefreshDropDownMenuFonts(menuItem.DropDownItems, menuFont);
				}
			}
		}
		foreach (Control ctrl in base.Controls)
		{
			if (ctrl.ContextMenuStrip != null)
			{
				ctrl.ContextMenuStrip.Font = menuFont;
			}
		}
		if (_toastPanel == null)
		{
			return;
		}
		foreach (Control control in _toastPanel.Controls)
		{
			if (control is Label lbl)
			{
				lbl.Font = ((lbl.Text == "✓") ? GetFont(SF(11f), FontStyle.Bold) : GetFont(SF(9f), FontStyle.Bold));
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
			if (ctrl is Label || ctrl is Button || ctrl is TextBox || ctrl is ComboBox || ctrl is RadioButton || ctrl is CheckBox || ctrl is GroupBox || ctrl is Panel || ctrl is TabControl || ctrl is ListBox || ctrl is ToggleSwitch)
			{
				ctrl.Font = GetFont(size, style);
			}
			RefreshControlFonts(ctrl.Controls);
		}
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
		if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
		{
			return;
		}
		string[] array = files;
		foreach (string filePath in array)
		{
			try
			{
				string ext = Path.GetExtension(filePath).ToLowerInvariant();
				switch (ext)
				{
				case ".m3u":
				case ".m3u8":
				case ".json":
				{
					string content = File.ReadAllText(filePath, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(content))
					{
						if (ext == ".json")
						{
							await ParseTvboxSubscriptionFromContent(content);
						}
						else
						{
							await ParseM3uContent(content, filePath);
						}
					}
					break;
				}
				case ".txt":
					ImportFromFile(filePath);
					break;
				}
			}
			catch (Exception ex)
			{
				DarkMessageBox.Show("导入文件失败: " + ex.Message, "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
	}

	private async Task ParseTvboxSubscriptionFromContent(string jsonContent)
	{
		_isImporting = true;
		try
		{
			Dictionary<string, object> json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(jsonContent);
			if (json == null || !json.ContainsKey("lives"))
			{
				DarkMessageBox.Show("订阅源格式不正确，未找到lives数组", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else if (json["lives"] is List<object> { Count: not 0 } livesArray)
			{
				int addedCount = 0;
				int duplicateCount = 0;
				HashSet<string> existingUrls = new HashSet<string>(allChannels.Select((ChannelInfo c) => c.Url.ToLowerInvariant()));
				foreach (object item in livesArray)
				{
					if (!(item is Dictionary<string, object> live))
					{
						continue;
					}
					string name = ((!live.ContainsKey("name")) ? "" : (live["name"]?.ToString()?.Trim() ?? ""));
					string url = ((!live.ContainsKey("url")) ? "" : (live["url"]?.ToString()?.Trim() ?? ""));
					if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url))
					{
						if (existingUrls.Contains(url.ToLowerInvariant()))
						{
							duplicateCount++;
							continue;
						}
						ChannelInfo channel = new ChannelInfo
						{
							Name = name,
							Url = url,
							Group = ((!live.ContainsKey("group")) ? "未分组" : (live["group"]?.ToString()?.Trim() ?? "未分组")),
							Status = "未检测",
							Location = ExtractLocationFromUrl(url)
						};
						allChannels.Add(channel);
						existingUrls.Add(url.ToLowerInvariant());
						addedCount++;
					}
				}
				RefreshGrid();
				totalCount = allChannels.Count;
				UpdateStatusBar();
				UpdateEmptyState();
				DarkMessageBox.Show($"成功导入 {addedCount} 个直播源，{duplicateCount} 个重复", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			else
			{
				DarkMessageBox.Show("订阅源中没有直播源", "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("解析JSON失败: " + ex.Message, "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_isImporting = false;
		}
	}

	private async Task ParseM3uContent(string content, string fileName)
	{
		_isImporting = true;
		try
		{
			string[] array = content.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			int addedCount = 0;
			int duplicateCount = 0;
			HashSet<string> existingUrls = new HashSet<string>(allChannels.Select((ChannelInfo c) => c.Url.ToLowerInvariant()));
			string currentGroup = "未分组";
			string currentName = "";
			string[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				string trimmed = array2[num].Trim();
				if (trimmed.StartsWith("#EXTGRP:", StringComparison.OrdinalIgnoreCase))
				{
					currentGroup = trimmed.Substring(8).Trim();
				}
				else if (trimmed.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
				{
					int colonIdx = trimmed.IndexOf(':');
					int commaIdx = trimmed.IndexOf(',');
					currentName = ((commaIdx <= colonIdx) ? "" : ChannelLogoHelper.StandardNameCctvOnly(trimmed.Substring(commaIdx + 1).Trim()));
				}
				else if (!trimmed.StartsWith("#") && Uri.IsWellFormedUriString(trimmed, UriKind.Absolute))
				{
					string url = trimmed;
					string name = (string.IsNullOrWhiteSpace(currentName) ? Path.GetFileNameWithoutExtension(fileName) : currentName);
					if (existingUrls.Contains(url.ToLowerInvariant()))
					{
						duplicateCount++;
						continue;
					}
					ChannelInfo channel = new ChannelInfo
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
			DarkMessageBox.Show($"成功导入 {addedCount} 个直播源，{duplicateCount} 个重复", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("解析M3U文件失败: " + ex.Message, "解析失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_isImporting = false;
		}
	}

	private void FindFFplay()
	{
		ffplayPath = "";
		ffprobePath = "";
		ffmpegPath = "";
		mediainfoPath = "";
		string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? new string[0];
		string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
		string[] extraDirs = new string[14]
		{
			appDir,
			Path.Combine(appDir, "ffmpeg", "bin"),
			Path.Combine(appDir, "bin"),
			Path.Combine(appDir, "mediainfo"),
			"C:\\ffmpeg\\bin",
			"C:\\Program Files\\ffmpeg\\bin",
			"C:\\Program Files\\MediaInfo",
			"C:\\Program Files (x86)\\MediaInfo",
			"C:\\msys64\\ucrt64\\bin",
			"C:\\msys64\\mingw64\\bin",
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ffmpeg", "bin"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ffmpeg", "bin"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "mediainfo"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "mediainfo")
		};
		foreach (string dir in (from value in paths.Concat(extraDirs)
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct())
		{
			try
			{
				string d = dir.Trim();
				if (string.IsNullOrEmpty(ffplayPath))
				{
					string fp = Path.Combine(d, "ffplay.exe");
					if (File.Exists(fp))
					{
						ffplayPath = fp;
					}
				}
				if (string.IsNullOrEmpty(ffprobePath))
				{
					string fp2 = Path.Combine(d, "ffprobe.exe");
					if (File.Exists(fp2))
					{
						ffprobePath = fp2;
					}
				}
				if (string.IsNullOrEmpty(ffmpegPath))
				{
					string fp3 = Path.Combine(d, "ffmpeg.exe");
					if (File.Exists(fp3))
					{
						ffmpegPath = fp3;
					}
				}
				if (string.IsNullOrEmpty(mediainfoPath))
				{
					string fp4 = Path.Combine(d, "mediainfo.exe");
					if (File.Exists(fp4))
					{
						mediainfoPath = fp4;
					}
				}
				if (!string.IsNullOrEmpty(ffplayPath) && !string.IsNullOrEmpty(ffprobePath) && !string.IsNullOrEmpty(ffmpegPath) && !string.IsNullOrEmpty(mediainfoPath))
				{
					break;
				}
			}
			catch
			{
			}
		}
	}

	private bool FFComponentsReady()
	{
		if (!string.IsNullOrEmpty(ffplayPath) && File.Exists(ffplayPath) && !string.IsNullOrEmpty(ffprobePath) && File.Exists(ffprobePath) && !string.IsNullOrEmpty(ffmpegPath))
		{
			return File.Exists(ffmpegPath);
		}
		return false;
	}

	private bool MediaInfoReady()
	{
		if (!string.IsNullOrEmpty(mediainfoPath))
		{
			return File.Exists(mediainfoPath);
		}
		return false;
	}

	private async Task CheckAndDownloadComponentsAsync()
	{
		FindFFplay();
		bool num = FFComponentsReady();
		bool hasMediaInfo = MediaInfoReady();
		if (!num)
		{
			string message = "当前检测模式：极速HTTP检测\n\n此模式仅检测链接可用性，无法获取视频分辨率。\n\n若要启用完整功能（获取分辨率、内置播放），\n需要下载安装 FFmpeg 组件。\n\n是否下载安装？";
			if (DarkMessageBox.Show(this, message, "检测模式", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
			{
				bool num2 = await DownloadFFmpegAsync(this);
				FindFFplay();
				if (num2 && FFComponentsReady())
				{
					detectEngine = "FFMPEG";
					DarkMessageBox.Show(this, "\ud83c\udf89 FFmpeg 组件安装成功！\n\n已自动切换到【完整检测模式】\n\n• 将获取视频分辨率信息\n• 支持内置播放器播放", "安装成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				else
				{
					DarkMessageBox.Show(this, "⚠ FFmpeg 安装失败\n\n将继续使用【极速HTTP检测】模式\n\n• 无法获取分辨率\n• 默认播放器不可用\n\n可稍后手动下载 ffmpeg 并解压到程序目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
		}
		if (hasMediaInfo || !(detectEngine == "FFMPEG"))
		{
			return;
		}
		string message2 = "检测到 MediaInfo 组件缺失\n\nMediaInfo 功能说明：\n• 提高分辨率检测精度\n• 支持更多视频格式解析\n• 补充 FFmpeg 无法识别的编码\n\n是否下载安装 MediaInfo 组件？";
		if (DarkMessageBox.Show(this, message2, "组件提示", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
		{
			bool num3 = await DownloadMediaInfoAsync(this);
			FindFFplay();
			if (!num3 && !MediaInfoReady())
			{
				DarkMessageBox.Show(this, "⚠ MediaInfo 安装失败\n\n分辨率检测将使用 FFmpeg 方案\n\n可稍后手动下载 mediainfo 并解压到程序目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}

	private Form CreateDownloadForm()
	{
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Form obj = new Form
		{
			Text = "正在安装 FFmpeg 组件",
			Size = new Size(SX(580), SY(320)),
			StartPosition = FormStartPosition.CenterScreen,
			FormBorderStyle = FormBorderStyle.None,
			MaximizeBox = false,
			MinimizeBox = false,
			BackColor = pal.FormBg,
			ShowInTaskbar = false,
			TopMost = true
		};
		var ctx = NeonChrome.Apply(obj, pal, "正在安装 FFmpeg 组件", dpiScale);
		Point At(int x, int yy) => new Point(x, yy);
		Label lblTitle = new Label
		{
			Text = "⏳ 正在下载 FFmpeg 组件（播放和检测功能必需）",
			Font = GetFont(SF(11f), FontStyle.Bold),
			ForeColor = pal.GhostText,
			Location = At(SX(20), SY(18)),
			Size = new Size(SX(530), SY(30)),
			TextAlign = ContentAlignment.TopLeft,
			BackColor = pal.PanelBg
		};
		ctx.Body.Controls.Add(lblTitle);
		Label lblStatus = new Label
		{
			Text = "正在准备下载...",
			Font = GetFont(SF(9.5f)),
			ForeColor = pal.Label,
			Location = At(SX(20), SY(55)),
			Size = new Size(SX(530), SY(22)),
			TextAlign = ContentAlignment.TopLeft,
			BackColor = pal.PanelBg
		};
		ctx.Body.Controls.Add(lblStatus);
		ProgressBar progressBar = new ProgressBar
		{
			Location = At(SX(20), SY(85)),
			Width = SX(525),
			Height = SY(24),
			Style = ProgressBarStyle.Blocks,
			Minimum = 0,
			Maximum = 100,
			Value = 0
		};
		ctx.Body.Controls.Add(progressBar);
		TextBox txtLog = new TextBox
		{
			Location = At(SX(20), SY(120)),
			Width = SX(525),
			Height = SY(100),
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			ReadOnly = true,
			Font = new Font("Consolas", SF(8.5f)),
			BackColor = pal.PanelBg,
			ForeColor = pal.Muted,
			BorderStyle = BorderStyle.FixedSingle
		};
		ctx.Body.Controls.Add(txtLog);
		obj.Tag = new Tuple<Label, ProgressBar, TextBox>(lblStatus, progressBar, txtLog);
		return obj;
	}

	private async Task<bool> DownloadFFmpegAsync(IWin32Window owner)
	{
		string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
		string tempDir = Path.Combine(Path.GetTempPath(), "wtv_ffmpeg_dl_" + Guid.NewGuid().ToString("N").Substring(0, 8));
		string zipPath = Path.Combine(tempDir, "ffmpeg.zip");
		string extractDir = Path.Combine(tempDir, "extract");
		bool downloadSuccess = false;
		Label lblStatus;
		ProgressBar progressBar;
		TextBox txtLog;
		try
		{
			Directory.CreateDirectory(tempDir);
			Form dlg = CreateDownloadForm();
			Tuple<Label, ProgressBar, TextBox> tuple = (Tuple<Label, ProgressBar, TextBox>)dlg.Tag;
			lblStatus = tuple.Item1;
			progressBar = tuple.Item2;
			txtLog = tuple.Item3;
			dlg.Shown += async delegate
			{
				_ = 3;
				try
				{
					string[] urls = new string[2] { "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip" };
					string downloadedFile = null;
					string[] array = urls;
					foreach (string dlUrl in array)
					{
						try
						{
							SetStatus("正在连接下载服务器：" + new Uri(dlUrl).Host);
							Log("尝试下载：" + dlUrl);
							SetProgress(2);
							using (WebClient client = new WebClient())
							{
								client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
								client.DownloadProgressChanged += delegate(object cs, DownloadProgressChangedEventArgs ce)
								{
									if (ce.TotalBytesToReceive > 0)
									{
										int pct = (int)Math.Min(90L, 2 + ce.BytesReceived * 88 / ce.TotalBytesToReceive);
										SetProgress(pct);
										SetStatus($"正在下载... {(double)ce.BytesReceived / 1024.0 / 1024.0:F1}MB / {(double)ce.TotalBytesToReceive / 1024.0 / 1024.0:F1}MB");
									}
									else
									{
										SetStatus($"正在下载... {(double)ce.BytesReceived / 1024.0 / 1024.0:F1}MB");
									}
								};
								await client.DownloadFileTaskAsync(new Uri(dlUrl), zipPath);
							}
							if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 1048576)
							{
								downloadedFile = zipPath;
								Log("下载完成：" + ((double)new FileInfo(zipPath).Length / 1024.0 / 1024.0).ToString("F1") + "MB");
								break;
							}
						}
						catch (Exception ex)
						{
							Log("下载失败：" + ex.Message);
							try
							{
								if (File.Exists(zipPath))
								{
									File.Delete(zipPath);
								}
							}
							catch
							{
							}
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
						catch (Exception ex2)
						{
							Log("PowerShell下载失败: " + ex2.Message);
						}
					}
					if (downloadedFile == null || !File.Exists(downloadedFile))
					{
						Log("所有下载方式均失败");
						DarkMessageBox.Show(dlg, "FFmpeg 自动下载失败，请手动下载 ffmpeg 并将 ffmpeg.exe、ffplay.exe、ffprobe.exe 放到程序根目录。\n下载地址：https://www.gyan.dev/ffmpeg/builds/", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dlg.DialogResult = DialogResult.Cancel;
						dlg.Close();
					}
					else
					{
						SetStatus("正在解压...");
						SetProgress(92);
						Log("开始解压文件...");
						Directory.CreateDirectory(extractDir);
						try
						{
							await ExtractZipAsync(downloadedFile, extractDir, Log);
						}
						catch (Exception ex3)
						{
							Log("解压失败: " + ex3.Message);
						}
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
								string allFiles = string.Join(", ", (from f in Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories)
									select Path.GetFileName(f)).Take(20));
								Log("找到的exe：" + allFiles);
							}
							catch
							{
							}
							DarkMessageBox.Show(dlg, "已下载但未能在压缩包中找到所需组件，请手动将 ffmpeg.exe、ffplay.exe、ffprobe.exe 复制到程序目录：\n" + appDir, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							dlg.DialogResult = DialogResult.Cancel;
							dlg.Close();
						}
						else
						{
							File.Copy(fp, Path.Combine(appDir, "ffplay.exe"), overwrite: true);
							File.Copy(fpr, Path.Combine(appDir, "ffprobe.exe"), overwrite: true);
							File.Copy(ffm, Path.Combine(appDir, "ffmpeg.exe"), overwrite: true);
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
					}
				}
				catch (Exception ex4)
				{
					Log("安装异常：" + ex4.Message);
					try
					{
						DarkMessageBox.Show(dlg, "FFmpeg 安装过程出错：\n" + ex4.Message + "\n\n请手动从 https://www.gyan.dev/ffmpeg/builds/ 下载并解压到程序目录。", "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					catch
					{
					}
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
			try
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, recursive: true);
				}
			}
			catch
			{
			}
		}
		void Log(string msg)
		{
			if (!txtLog.IsDisposed)
			{
				if (txtLog.InvokeRequired)
				{
					txtLog.BeginInvoke((Action)delegate
					{
						txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
					});
				}
				else
				{
					txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
				}
			}
		}
		void SetProgress(int pct)
		{
			if (!progressBar.IsDisposed)
			{
				if (progressBar.InvokeRequired)
				{
					progressBar.BeginInvoke((Action)delegate
					{
						progressBar.Value = Math.Max(0, Math.Min(100, pct));
					});
				}
				else
				{
					progressBar.Value = Math.Max(0, Math.Min(100, pct));
				}
			}
		}
		void SetStatus(string msg)
		{
			if (!lblStatus.IsDisposed)
			{
				if (lblStatus.InvokeRequired)
				{
					lblStatus.BeginInvoke((Action)delegate
					{
						lblStatus.Text = msg;
					});
				}
				else
				{
					lblStatus.Text = msg;
				}
			}
		}
	}

	private async Task<bool> DownloadMediaInfoAsync(IWin32Window owner)
	{
		string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
		string tempDir = Path.Combine(Path.GetTempPath(), "wtv_mediainfo_dl_" + Guid.NewGuid().ToString("N").Substring(0, 8));
		string zipPath = Path.Combine(tempDir, "mediainfo.zip");
		string extractDir = Path.Combine(tempDir, "extract");
		bool downloadSuccess = false;
		Label lblStatus;
		ProgressBar progressBar;
		TextBox txtLog;
		try
		{
			Directory.CreateDirectory(tempDir);
			Form dlg = CreateDownloadForm();
			dlg.Text = "正在安装 MediaInfo 组件";
			Tuple<Label, ProgressBar, TextBox> tuple = (Tuple<Label, ProgressBar, TextBox>)dlg.Tag;
			lblStatus = tuple.Item1;
			progressBar = tuple.Item2;
			txtLog = tuple.Item3;
			dlg.Shown += async delegate
			{
				_ = 3;
				try
				{
					bool is64Bit = Environment.Is64BitOperatingSystem;
					string arch = (is64Bit ? "x64" : "i386");
					string version = "26.05";
					Log("系统架构：" + (is64Bit ? "64位" : "32位") + "，选择 MediaInfo " + arch + " 版本");
					string[] urls = new string[2]
					{
						"https://mediaarea.net/download/binary/mediainfo/" + version + "/MediaInfo_CLI_" + version + "_Windows_" + arch + ".zip",
						"https://github.com/MediaArea/MediaInfo/releases/download/v" + version + "/MediaInfo_CLI_" + version + "_Windows_" + arch + ".zip"
					};
					string downloadedFile = null;
					string[] array = urls;
					foreach (string dlUrl in array)
					{
						try
						{
							SetStatus("正在连接下载服务器：" + new Uri(dlUrl).Host);
							Log("尝试下载：" + dlUrl);
							SetProgress(2);
							using (WebClient client = new WebClient())
							{
								client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
								client.DownloadProgressChanged += delegate(object cs, DownloadProgressChangedEventArgs ce)
								{
									if (ce.TotalBytesToReceive > 0)
									{
										int pct = (int)Math.Min(90L, 2 + ce.BytesReceived * 88 / ce.TotalBytesToReceive);
										SetProgress(pct);
										SetStatus($"正在下载... {(double)ce.BytesReceived / 1024.0 / 1024.0:F1}MB / {(double)ce.TotalBytesToReceive / 1024.0 / 1024.0:F1}MB");
									}
									else
									{
										SetStatus($"正在下载... {(double)ce.BytesReceived / 1024.0 / 1024.0:F1}MB");
									}
								};
								await client.DownloadFileTaskAsync(new Uri(dlUrl), zipPath);
							}
							if (File.Exists(zipPath) && new FileInfo(zipPath).Length > 102400)
							{
								downloadedFile = zipPath;
								Log("下载完成：" + ((double)new FileInfo(zipPath).Length / 1024.0 / 1024.0).ToString("F1") + "MB");
								break;
							}
						}
						catch (Exception ex)
						{
							Log("下载失败：" + ex.Message);
							try
							{
								if (File.Exists(zipPath))
								{
									File.Delete(zipPath);
								}
							}
							catch
							{
							}
						}
					}
					if (downloadedFile == null)
					{
						SetStatus("下载失败，正在尝试备用方式...");
						Log("主下载地址均失败，尝试PowerShell方式...");
						try
						{
							string psUrl = "https://mediaarea.net/download/binary/mediainfo/" + version + "/MediaInfo_CLI_" + version + "_Windows_" + arch + ".zip";
							downloadedFile = await DownloadViaPowerShellWithUrl(zipPath, psUrl, Log, SetStatus, SetProgress);
						}
						catch (Exception ex2)
						{
							Log("PowerShell下载失败: " + ex2.Message);
						}
					}
					if (downloadedFile == null || !File.Exists(downloadedFile))
					{
						Log("所有下载方式均失败");
						DarkMessageBox.Show(dlg, "MediaInfo 自动下载失败，请手动下载 MediaInfo CLI 并将 mediainfo.exe 放到程序根目录。\n下载地址：https://mediaarea.net/zh-CN/MediaInfo/Download/Windows", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dlg.DialogResult = DialogResult.Cancel;
						dlg.Close();
					}
					else
					{
						SetStatus("正在解压...");
						SetProgress(92);
						Log("开始解压文件...");
						Directory.CreateDirectory(extractDir);
						try
						{
							await ExtractZipAsync(downloadedFile, extractDir, Log);
						}
						catch (Exception ex3)
						{
							Log("解压失败: " + ex3.Message);
						}
						SetStatus("正在查找组件...");
						SetProgress(97);
						Log("查找 mediainfo.exe...");
						string mi = FindFileInDir(extractDir, "mediainfo.exe");
						if (string.IsNullOrEmpty(mi))
						{
							Log("未在解压目录找到mediainfo.exe！");
							try
							{
								string allFiles = string.Join(", ", (from f in Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories)
									select Path.GetFileName(f)).Take(20));
								Log("找到的exe：" + allFiles);
							}
							catch
							{
							}
							DarkMessageBox.Show(dlg, "已下载但未能在压缩包中找到 mediainfo.exe，请手动将 mediainfo.exe 复制到程序目录：\n" + appDir, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							dlg.DialogResult = DialogResult.Cancel;
							dlg.Close();
						}
						else
						{
							string miDir = Path.GetDirectoryName(mi);
							File.Copy(mi, Path.Combine(appDir, "mediainfo.exe"), overwrite: true);
							Log("已复制 mediainfo.exe 到：" + appDir);
							int dllCount = 0;
							try
							{
								string[] files = Directory.GetFiles(miDir, "*.dll", SearchOption.TopDirectoryOnly);
								foreach (string obj5 in files)
								{
									string dllName = Path.GetFileName(obj5);
									File.Copy(obj5, Path.Combine(appDir, dllName), overwrite: true);
									dllCount++;
									Log("已复制依赖：" + dllName);
								}
							}
							catch (Exception ex4)
							{
								Log("复制DLL时出错: " + ex4.Message);
							}
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
					}
				}
				catch (Exception ex5)
				{
					Log("安装异常：" + ex5.Message);
					try
					{
						DarkMessageBox.Show(dlg, "MediaInfo 安装过程出错：\n" + ex5.Message + "\n\n请手动从 https://mediaarea.net/zh-CN/MediaInfo/Download/Windows 下载并解压到程序目录。", "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					catch
					{
					}
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
			try
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, recursive: true);
				}
			}
			catch
			{
			}
		}
		void Log(string msg)
		{
			if (!txtLog.IsDisposed)
			{
				if (txtLog.InvokeRequired)
				{
					txtLog.BeginInvoke((Action)delegate
					{
						txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
					});
				}
				else
				{
					txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\r\n");
				}
			}
		}
		void SetProgress(int pct)
		{
			if (!progressBar.IsDisposed)
			{
				if (progressBar.InvokeRequired)
				{
					progressBar.BeginInvoke((Action)delegate
					{
						progressBar.Value = Math.Max(0, Math.Min(100, pct));
					});
				}
				else
				{
					progressBar.Value = Math.Max(0, Math.Min(100, pct));
				}
			}
		}
		void SetStatus(string msg)
		{
			if (!lblStatus.IsDisposed)
			{
				if (lblStatus.InvokeRequired)
				{
					lblStatus.BeginInvoke((Action)delegate
					{
						lblStatus.Text = msg;
					});
				}
				else
				{
					lblStatus.Text = msg;
				}
			}
		}
	}

	private async Task<string> DownloadViaPowerShell(string destPath, Action<string> log, Action<string> setStatus, Action<int> setProgress)
	{
		_ = 1;
		try
		{
			setStatus("通过 PowerShell 下载...");
			log("尝试通过 PowerShell Invoke-WebRequest 下载...");
			setProgress(5);
			string psUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
			string script = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '" + psUrl + "' -OutFile '" + destPath + "' -UseBasicParsing";
			Process proc = new Process();
			try
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
				await Task.Run(delegate
				{
					proc.WaitForExit();
				});
				if (!string.IsNullOrEmpty(err))
				{
					log("PS日志: " + err.Substring(0, Math.Min(300, err.Length)));
				}
			}
			finally
			{
				if (proc != null)
				{
					((IDisposable)proc).Dispose();
				}
			}
			if (File.Exists(destPath) && new FileInfo(destPath).Length > 1048576)
			{
				return destPath;
			}
		}
		catch (Exception ex)
		{
			log("PowerShell下载失败: " + ex.Message);
		}
		return null;
	}

	private async Task<string> DownloadViaPowerShellWithUrl(string destPath, string url, Action<string> log, Action<string> setStatus, Action<int> setProgress)
	{
		_ = 1;
		try
		{
			setStatus("通过 PowerShell 下载...");
			log("尝试通过 PowerShell Invoke-WebRequest 下载...");
			setProgress(5);
			string script = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '" + url + "' -OutFile '" + destPath + "' -UseBasicParsing";
			Process proc = new Process();
			try
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
				await Task.Run(delegate
				{
					proc.WaitForExit();
				});
				if (!string.IsNullOrEmpty(err))
				{
					log("PS日志: " + err.Substring(0, Math.Min(300, err.Length)));
				}
			}
			finally
			{
				if (proc != null)
				{
					((IDisposable)proc).Dispose();
				}
			}
			if (File.Exists(destPath) && new FileInfo(destPath).Length > 102400)
			{
				return destPath;
			}
		}
		catch (Exception ex)
		{
			log("PowerShell下载失败: " + ex.Message);
		}
		return null;
	}

	private async Task ExtractZipAsync(string zipPath, string destDir, Action<string> log)
	{
		try
		{
			Process proc = new Process();
			try
			{
				string script = "Expand-Archive -Path '" + zipPath + "' -DestinationPath '" + destDir + "' -Force";
				proc.StartInfo = new ProcessStartInfo
				{
					FileName = "powershell.exe",
					Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};
				proc.Start();
				await Task.Run(delegate
				{
					proc.WaitForExit();
				});
			}
			finally
			{
				if (proc != null)
				{
					((IDisposable)proc).Dispose();
				}
			}
			log("解压完成");
		}
		catch (Exception ex)
		{
			log("PowerShell解压失败：" + ex.Message + "，尝试备用方式...");
			try
			{
				log("尝试使用.NET内置解压...");
				if (!Directory.Exists(destDir))
				{
					Directory.CreateDirectory(destDir);
				}
				string psScript2 = "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory('" + zipPath + "', '" + destDir + "')";
				using (Process proc2 = new Process())
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
			return Directory.GetFiles(rootDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private async Task<string> TryGetResolutionWithFfprobe(string url, CancellationToken token)
	{
		_ = 3;
		try
		{
			string fp = ffprobePath;
			if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
			{
				string candidate = Path.Combine(Path.GetDirectoryName(ffplayPath), "ffprobe.exe");
				if (File.Exists(candidate))
				{
					fp = candidate;
				}
			}
			if (string.IsNullOrEmpty(fp))
			{
				return "";
			}
			using CancellationTokenSource cts = new CancellationTokenSource(30000);
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = fp,
				Arguments = "-v error -fflags +fastseek+genpts+nobuffer -avioflags direct -rtbufsize 64000 -analyzeduration 10M -probesize 10M -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"" + url + "\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			Process proc = Process.Start(psi);
			try
			{
				if (proc == null)
				{
					return "";
				}
				bool wasCanceled = false;
				Task<bool> waitTask = Task.Run(() => proc.WaitForExit(30000));
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
					try
					{
						proc.Kill();
					}
					catch
					{
					}
					try
					{
						await waitTask;
					}
					catch
					{
					}
					return "";
				}
				string output = await proc.StandardOutput.ReadToEndAsync();
				await proc.StandardError.ReadToEndAsync();
				if (!string.IsNullOrWhiteSpace(output))
				{
					string[] array = output.Trim().Split('\n', '\r');
					foreach (string line in array)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							string[] wh = line.Trim().Split(',');
							if (wh.Length >= 2 && int.TryParse(wh[0].Trim(), out var w) && int.TryParse(wh[1].Trim(), out var h) && w > 0 && h > 0)
							{
								return $"{w}x{h}";
							}
						}
					}
				}
			}
			finally
			{
				if (proc != null)
				{
					((IDisposable)proc).Dispose();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private async Task<string> TryGetResolutionWithFfmpeg(string url, CancellationToken token)
	{
		_ = 3;
		try
		{
			string fp = ffmpegPath;
			if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
			{
				string candidate = Path.Combine(Path.GetDirectoryName(ffplayPath), "ffmpeg.exe");
				if (File.Exists(candidate))
				{
					fp = candidate;
				}
			}
			if (string.IsNullOrEmpty(fp))
			{
				return "";
			}
			using CancellationTokenSource cts = new CancellationTokenSource(15000);
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = fp,
				Arguments = "-analyzeduration 10M -probesize 10M -i \"" + url + "\" -hide_banner",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			Process proc = Process.Start(psi);
			try
			{
				if (proc == null)
				{
					return "";
				}
				bool wasCanceled = false;
				Task<bool> waitTask = Task.Run(() => proc.WaitForExit(15000));
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
					try
					{
						proc.Kill();
					}
					catch
					{
					}
					try
					{
						await waitTask;
					}
					catch
					{
					}
					return "";
				}
				string allText = await proc.StandardOutput.ReadToEndAsync() + "\n" + await proc.StandardError.ReadToEndAsync();
				string[] array = new string[5] { "(\\d{2,5})x(\\d{2,5})", "(\\d{2,5})\\s*[x×]\\s*(\\d{2,5})", "Stream.*Video.*?(\\d{2,5})[x×](\\d{2,5})", "Video:.*?(\\d{2,5})x(\\d{2,5})", "width\\s*[=:]\\s*(\\d{2,5}).*?height\\s*[=:]\\s*(\\d{2,5})" };
				foreach (string pattern in array)
				{
					Match match = Regex.Match(allText, pattern, RegexOptions.IgnoreCase);
					if (match.Success && int.TryParse(match.Groups[1].Value, out var w) && int.TryParse(match.Groups[2].Value, out var h) && w > 0 && h > 0 && w < 8000 && h < 8000)
					{
						return $"{w}x{h}";
					}
				}
			}
			finally
			{
				if (proc != null)
				{
					((IDisposable)proc).Dispose();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private async Task<string> TryGetResolutionWithMediainfo(string url, CancellationToken token)
	{
		Process proc = null;
		try
		{
			string fp = mediainfoPath;
			if (string.IsNullOrEmpty(fp))
			{
				return "";
			}
			using CancellationTokenSource cts = new CancellationTokenSource(15000);
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = fp,
				Arguments = "--Full \"" + url + "\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			proc = Process.Start(psi);
			if (proc == null)
			{
				return "";
			}
			bool wasCanceled = false;
			Task<bool> waitTask = Task.Run(() => proc.WaitForExit(15000));
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
				try
				{
					proc.Kill();
				}
				catch
				{
				}
				try
				{
					await waitTask;
				}
				catch
				{
				}
				return "";
			}
			string allText = await proc.StandardOutput.ReadToEndAsync() + "\n" + await proc.StandardError.ReadToEndAsync();
			if (!string.IsNullOrWhiteSpace(allText))
			{
				Match wMatch = Regex.Match(allText, "Width\\s*:\\s*(\\d{2,5})");
				Match hMatch = Regex.Match(allText, "Height\\s*:\\s*(\\d{2,5})");
				if (wMatch.Success && hMatch.Success)
				{
					int w = int.Parse(wMatch.Groups[1].Value);
					int h = int.Parse(hMatch.Groups[1].Value);
					if (w > 0 && h > 0 && w < 8000 && h < 8000)
					{
						return $"{w}x{h}";
					}
				}
				string[] array = allText.Trim().Split('\n', '\r');
				foreach (string line in array)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						Match match = Regex.Match(line.Trim(), "(\\d{2,5})\\s*[x×]\\s*(\\d{2,5})");
						if (match.Success && int.TryParse(match.Groups[1].Value, out var w2) && int.TryParse(match.Groups[2].Value, out var h2) && w2 > 0 && h2 > 0 && w2 < 8000 && h2 < 8000)
						{
							return $"{w2}x{h2}";
						}
					}
				}
			}
		}
		catch (InvalidOperationException)
		{
		}
		catch (IOException)
		{
		}
		catch (Exception)
		{
		}
		finally
		{
			if (proc != null)
			{
				try
				{
					if (!proc.HasExited)
					{
						proc.Kill();
					}
				}
				catch
				{
				}
				proc.Dispose();
			}
		}
		return "";
	}

	private async Task<string> GetResolutionWithFallback(string url, CancellationToken token)
	{
		string resolution = await TryGetResolutionWithFfprobe(url, token);
		if (!string.IsNullOrEmpty(resolution))
		{
			return resolution;
		}
		resolution = await TryGetResolutionWithFfmpeg(url, token);
		if (!string.IsNullOrEmpty(resolution))
		{
			return resolution;
		}
		resolution = await TryGetResolutionWithMediainfo(url, token);
		if (!string.IsNullOrEmpty(resolution))
		{
			return resolution;
		}
		return "";
	}

	private void GetFullStreamInfoWithFfprobeSync(string url)
	{
		try
		{
			string fp = ffprobePath;
			if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
			{
				string candidate = Path.Combine(Path.GetDirectoryName(ffplayPath), "ffprobe.exe");
				if (File.Exists(candidate))
				{
					fp = candidate;
				}
			}
			if (string.IsNullOrEmpty(fp))
			{
				return;
			}
			using Process proc = Process.Start(new ProcessStartInfo
			{
				FileName = fp,
				Arguments = "-v quiet -analyzeduration 10M -probesize 10M -print_format json -show_streams -show_format \"" + url + "\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			if (proc == null)
			{
				return;
			}
			if (!proc.WaitForExit(20000))
			{
				try
				{
					proc.Kill();
					return;
				}
				catch
				{
					return;
				}
			}
			string output = proc.StandardOutput.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(output))
			{
				ParseFfprobeJson(output);
			}
		}
		catch
		{
		}
	}

	private async Task GetFullStreamInfoWithFfprobe(string url)
	{
		_ = 3;
		try
		{
			string fp = ffprobePath;
			if (string.IsNullOrEmpty(fp) && !string.IsNullOrEmpty(ffplayPath))
			{
				string candidate = Path.Combine(Path.GetDirectoryName(ffplayPath), "ffprobe.exe");
				if (File.Exists(candidate))
				{
					fp = candidate;
				}
			}
			if (string.IsNullOrEmpty(fp))
			{
				return;
			}
			using (new CancellationTokenSource(20000))
			{
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = fp,
					Arguments = "-v quiet -print_format json -show_streams -show_format \"" + url + "\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				Process proc = Process.Start(psi);
				try
				{
					if (proc == null)
					{
						return;
					}
					Task<bool> waitTask = Task.Run(() => proc.WaitForExit(20000));
					await Task.WhenAny(waitTask, Task.Delay(21000));
					if (!proc.HasExited)
					{
						try
						{
							proc.Kill();
						}
						catch
						{
						}
						try
						{
							await waitTask;
							return;
						}
						catch
						{
							return;
						}
					}
					string output = await proc.StandardOutput.ReadToEndAsync();
					await proc.StandardError.ReadToEndAsync();
					if (!string.IsNullOrWhiteSpace(output))
					{
						ParseFfprobeJson(output);
					}
				}
				finally
				{
					if (proc != null)
					{
						((IDisposable)proc).Dispose();
					}
				}
			}
		}
		catch
		{
		}
	}

	private void ParseFfprobeJson(string json)
	{
		try
		{
			Dictionary<string, object> root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
			if (root == null)
			{
				return;
			}
			if (root.TryGetValue("format", out var formatObj) && formatObj is Dictionary<string, object> format)
			{
				if (format.TryGetValue("format_long_name", out var fln))
				{
					_currentFormat = fln?.ToString() ?? "";
				}
				if (string.IsNullOrEmpty(_currentFormat) && format.TryGetValue("format_name", out var fn))
				{
					_currentFormat = fn?.ToString() ?? "";
				}
				if (format.TryGetValue("bit_rate", out var brObj) && double.TryParse(brObj?.ToString(), out var bitRate))
				{
					_currentBitrate = $"{bitRate / 1000.0:F1} kb/s";
				}
				if (format.TryGetValue("duration", out var durObj) && double.TryParse(durObj?.ToString(), out var dur))
				{
					int hours = (int)(dur / 3600.0);
					int minutes = (int)(dur % 3600.0 / 60.0);
					double seconds = dur % 60.0;
					_currentDuration = $"{hours:00}:{minutes:00}:{seconds:00.00}";
				}
			}
			if (!root.TryGetValue("streams", out var streamsObj) || !(streamsObj is ArrayList streams))
			{
				return;
			}
			foreach (object item in streams)
			{
				if (!(item is Dictionary<string, object> stream) || !stream.TryGetValue("codec_type", out var codecTypeObj))
				{
					continue;
				}
				string codecType = codecTypeObj?.ToString()?.ToLowerInvariant() ?? "";
				if (codecType == "video")
				{
					string codecName = GetDictString(stream, "codec_name");
					string codecLongName = GetDictString(stream, "codec_long_name");
					string profile = GetDictString(stream, "profile");
					string dictString = GetDictString(stream, "width");
					string height = GetDictString(stream, "height");
					string sar = GetDictString(stream, "sample_aspect_ratio");
					string dar = GetDictString(stream, "display_aspect_ratio");
					string pixFmt = GetDictString(stream, "pix_fmt");
					string fps = GetDictString(stream, "r_frame_rate");
					string level = GetDictString(stream, "level");
					string colorSpace = GetDictString(stream, "color_space");
					string colorRange = GetDictString(stream, "color_range");
					string colorPrimaries = GetDictString(stream, "color_primaries");
					string colorTransfer = GetDictString(stream, "color_transfer");
					if (!string.IsNullOrEmpty(codecName))
					{
						_currentCodec = ((!string.IsNullOrEmpty(codecLongName)) ? codecLongName : codecName.ToUpper());
						if (!string.IsNullOrEmpty(profile))
						{
							_currentCodec = _currentCodec + " (" + profile + ")";
						}
					}
					if (int.TryParse(dictString, out var w) && int.TryParse(height, out var h) && w > 0 && h > 0)
					{
						_currentResolution = $"{w}x{h}";
					}
					if (!string.IsNullOrEmpty(sar))
					{
						_currentSar = sar;
					}
					if (!string.IsNullOrEmpty(dar))
					{
						_currentDar = dar;
					}
					if (!string.IsNullOrEmpty(fps))
					{
						string[] fpsParts = fps.Split('/');
						if (fpsParts.Length == 2 && int.TryParse(fpsParts[0], out var fpsNum) && int.TryParse(fpsParts[1], out var fpsDen) && fpsNum > 0 && fpsDen > 0)
						{
							_currentFps = $"{(double)fpsNum / (double)fpsDen:F2} FPS";
						}
					}
					_currentPixFmt = pixFmt;
					_currentLevel = level;
					_currentColorSpace = colorSpace;
					_currentColorRange = colorRange;
					_currentColorPrimaries = colorPrimaries;
					_currentColorTransfer = colorTransfer;
				}
				else
				{
					if (!(codecType == "audio"))
					{
						continue;
					}
					string codecName2 = GetDictString(stream, "codec_name");
					string codecLongName2 = GetDictString(stream, "codec_long_name");
					string dictString2 = GetDictString(stream, "sample_rate");
					string channels = GetDictString(stream, "channels");
					string channelLayout = GetDictString(stream, "channel_layout");
					string sampleFmt = GetDictString(stream, "sample_fmt");
					string bitsPerSample = GetDictString(stream, "bits_per_sample");
					if (!string.IsNullOrEmpty(codecName2))
					{
						string audioCodec = ((!string.IsNullOrEmpty(codecLongName2)) ? codecLongName2 : codecName2.ToUpper());
						if (!string.IsNullOrEmpty(_currentCodec))
						{
							_currentCodec = _currentCodec + " + " + audioCodec;
						}
						else
						{
							_currentCodec = audioCodec;
						}
					}
					if (int.TryParse(dictString2, out var sr))
					{
						_currentAudioSampleRate = $"{sr} Hz";
					}
					if (int.TryParse(channels, out var ch))
					{
						if (!string.IsNullOrEmpty(channelLayout))
						{
							_currentAudioChannels = channelLayout;
						}
						else
						{
							_currentAudioChannels = $"{ch}声道";
						}
					}
					if (!string.IsNullOrEmpty(sampleFmt))
					{
						_currentAudioBitdepth = sampleFmt;
					}
					if (int.TryParse(bitsPerSample, out var bps))
					{
						_currentAudioBitdepth = $"{bps} bits";
					}
				}
			}
		}
		catch
		{
		}
	}

	private string GetDictString(Dictionary<string, object> dict, string key)
	{
		if (dict != null && dict.TryGetValue(key, out var value) && value != null)
		{
			return value.ToString();
		}
		return null;
	}

	internal static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
	{
		int d = radius * 2;
		d = Math.Min(d, Math.Min(rect.Width, rect.Height));
		if (d < 4)
		{
			d = 4;
		}
		GraphicsPath graphicsPath = new GraphicsPath();
		int x = rect.X;
		int y = rect.Y;
		int w = rect.Width;
		int h = rect.Height;
		graphicsPath.AddArc(x, y, d, d, 180f, 90f);
		graphicsPath.AddArc(x + w - d, y, d, d, 270f, 90f);
		graphicsPath.AddArc(x + w - d, y + h - d, d, d, 0f, 90f);
		graphicsPath.AddArc(x, y + h - d, d, d, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private void WebView2InitCompletedHandler(object sender, EventArgs e)
	{
		try
		{
			// 事实：CoreWebView2InitializationCompleted 事件参数携带 IsSuccess / InitializationException。
			// 原代码完全忽略初始化是否成功；失败时 CoreWebView2 为 null 且静默返回 -> 白屏且无任何提示。
			bool isSuccess = true;
			string initError = null;
			try
			{
				PropertyInfo isSuccessProp = e?.GetType().GetProperty("IsSuccess");
				if (isSuccessProp != null && isSuccessProp.GetValue(e) is bool s)
				{
					isSuccess = s;
				}
				if (!isSuccess)
				{
					PropertyInfo exProp = e?.GetType().GetProperty("InitializationException");
					initError = exProp?.GetValue(e)?.ToString();
				}
			}
			catch
			{
			}
			PropertyInfo coreProp = sender.GetType().GetProperty("CoreWebView2");
			if (!(coreProp != null))
			{
				return;
			}
			object core = coreProp.GetValue(sender);
			if (!isSuccess || core == null)
			{
				// 显式暴露初始化失败，避免“窗口打开但空白、无任何报错”的体验。
				DarkMessageBox.Show(
					"WebView2 内核初始化失败，无法加载网页。\n\n原因: " + (initError ?? "CoreWebView2 为 null（运行时可能未安装或被禁用）"),
					"WebView2 初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			PropertyInfo settingsProp = core.GetType().GetProperty("Settings");
			if (settingsProp != null)
			{
				object settings = settingsProp.GetValue(core);
				settings.GetType().GetProperty("UserAgent")?.SetValue(settings, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			}
			// 初始化成功后 CoreWebView2 已就绪，统一在此处执行待导航（调用 CoreWebView2.Navigate 最可靠）。
			// 通过对 webViewPendingUrl 一次性消费，避免与 dlg.Load 中的导航形成竞争/重复导航导致白屏。
			if (!string.IsNullOrEmpty(webViewPendingUrl))
			{
				MethodInfo navMethod = core.GetType().GetMethod("Navigate", new Type[1] { typeof(string) });
				if (navMethod != null)
				{
					navMethod.Invoke(core, new object[] { webViewPendingUrl });
				}
				else
				{
					sender.GetType().GetProperty("Source")?.SetValue(sender, new Uri(webViewPendingUrl));
				}
				webViewPendingUrl = null;
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("WebView2初始化失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private async void WebView2NavCompletedHandler(object sender, EventArgs e)
	{
		try
		{
			Type type = sender.GetType();
			PropertyInfo coreProp = type.GetProperty("CoreWebView2");
			if (!(coreProp != null))
			{
				return;
			}
			object core = coreProp.GetValue(sender);
			if (core == null)
			{
				return;
			}
			MethodInfo execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new Type[1] { typeof(string) });
			if (execMethod == null)
			{
				return;
			}
			try
			{
				string jsResult = (await (Task<string>)execMethod.Invoke(core, new object[1] { "(() => { let bg = window.getComputedStyle(document.body).backgroundColor; if (!bg || bg === 'transparent') { bg = window.getComputedStyle(document.documentElement).backgroundColor; } if (!bg || bg === 'transparent') { bg = '#1a1a2e'; } return bg; })()" }))?.Trim('"');
				if (!string.IsNullOrEmpty(jsResult) && jsResult.StartsWith("#"))
				{
					try
					{
						Color pageBg = ColorTranslator.FromHtml(jsResult);
						AdjustToolbarColors(webViewNavPanel, webViewCboEngine, webViewTxtUrl, pageBg);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			try
			{
				string loginJs = "(function() {   if (window._loginFormHooked) return;   window._loginFormHooked = true;   document.addEventListener('submit', function(e) {     try {       var form = e.target;       var pwd = form.querySelector('input[type=\"password\"]');       if (!pwd || !pwd.value) return;       var userInput = form.querySelector('input[type=\"text\"], input[type=\"email\"], input[name*=\"user\"], input[name*=\"account\"], input[name*=\"email\"], input[name*=\"login\"]');       if (!userInput) { var allInputs = form.querySelectorAll('input'); for (var i=0; i<allInputs.length; i++) { if (allInputs[i] !== pwd && allInputs[i].type !== 'hidden' && allInputs[i].type !== 'password') { userInput = allInputs[i]; break; } } }       if (userInput && userInput.value) {         window.chrome.webview.postMessage(JSON.stringify({type:'login', url:location.origin, username:userInput.value, password:pwd.value}));       }     } catch(ex) {}   }, true);   document.addEventListener('keydown', function(e) {     if (e.key === 'Enter') {       var el = e.target;       if (el && el.type === 'password' && el.value) {         var form = el.closest('form');         if (form) { var userInput = form.querySelector('input[type=\"text\"], input[type=\"email\"], input[name*=\"user\"], input[name*=\"account\"], input[name*=\"email\"]');           if (!userInput) { var allInputs = form.querySelectorAll('input'); for (var i=0; i<allInputs.length; i++) { if (allInputs[i] !== el && allInputs[i].type !== 'hidden' && allInputs[i].type !== 'password') { userInput = allInputs[i]; break; } } }           if (userInput && userInput.value) {             window.chrome.webview.postMessage(JSON.stringify({type:'login', url:location.origin, username:userInput.value, password:el.value}));           }         }       }     }   }, true); })();";
				await (Task)execMethod.Invoke(core, new object[1] { loginJs });
			}
			catch
			{
			}
			try
			{
				string url = type.GetProperty("Source")?.GetValue(sender)?.ToString() ?? "";
				if (webViewStatusUrl != null)
				{
					if (webViewStatusUrl.InvokeRequired)
					{
						webViewStatusUrl.BeginInvoke((Action)delegate
						{
							webViewStatusUrl.Text = ((url.Length > 50) ? (url.Substring(0, 50) + "...") : url);
						});
					}
					else
					{
						webViewStatusUrl.Text = ((url.Length > 50) ? (url.Substring(0, 50) + "...") : url);
					}
				}
			}
			catch
			{
			}
			if (!autoExtractIpPort)
			{
				return;
			}
			try
			{
				string extractJs = "(function() {   var allText = '';   allText += document.body.innerText || '';   allText += ' ' + document.documentElement.outerHTML || '';   try {     var iframes = document.querySelectorAll('iframe');     for (var k=0; k<iframes.length; k++) {       try { if (iframes[k].contentDocument) { allText += ' ' + iframes[k].contentDocument.body.innerText; } } catch(e) {}     }   } catch(e) {}   var valid = {};   var ipv4Regex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b/g;   var matches = allText.match(ipv4Regex) || [];   for (var i=0; i<matches.length; i++) { valid[matches[i]] = true; }   var urlIpRegex = /(?:http|https):\\/\\/(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?::(\\d{2,5}))?(?:\\/|\\?|$)/gi;   var urlMatches = allText.match(urlIpRegex) || [];   for (var i=0; i<urlMatches.length; i++) {     var urlMatch = urlMatches[i].replace(/^https?:\\/\\//i, '');     var portMatch = urlMatch.match(/:(\\d{2,5})$/);     var ip = urlMatch.replace(/:\\d{2,5}$/, '');     if (portMatch) { valid[ip + ':' + portMatch[1]] = true; } else { valid[ip] = true; }   }   var ipWithPortRegex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):(\\d{2,5})\\b/g;   var portMatches = allText.match(ipWithPortRegex) || [];   for (var i=0; i<portMatches.length; i++) { valid[portMatches[i]] = true; }   var ipList = Object.keys(valid);   ipList = ipList.filter(function(ip) {     var parts = ip.split(':')[0].split('.');     if (parts.length !== 4) return false;     for (var j=0; j<4; j++) { var n = parseInt(parts[j]); if (isNaN(n) || n<0 || n>255) return false; }     return true;   });   return JSON.stringify(ipList); })()";
				string ipResult = await (Task<string>)execMethod.Invoke(core, new object[1] { extractJs });
				if (string.IsNullOrEmpty(ipResult))
				{
					return;
				}
				List<string> ips = new List<string>();
				try
				{
					ipResult = ipResult.Trim();
					foreach (Match item in Regex.Matches(ipResult, "(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}):(\\d{2,5})"))
					{
						string ip = item.Groups[1].Value;
						string port = item.Groups[2].Value;
						string[] parts = ip.Split('.');
						if (parts.Length != 4)
						{
							continue;
						}
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
						if (isValid && ipParts[0] != 10 && (ipParts[0] != 172 || ipParts[1] < 16 || ipParts[1] > 31) && (ipParts[0] != 192 || ipParts[1] != 168) && ipParts[0] != 127 && (ipParts[0] != 0 || ipParts[1] != 0 || ipParts[2] != 0 || ipParts[3] != 0) && (ipParts[0] != 255 || ipParts[1] != 255 || ipParts[2] != 255 || ipParts[3] != 255) && (ipParts[0] != 169 || ipParts[1] != 254) && int.TryParse(port, out var portNum) && portNum >= 1 && portNum <= 65535)
						{
							string fullIp = ip + ":" + port;
							if (!ips.Contains(fullIp))
							{
								ips.Add(fullIp);
							}
						}
					}
				}
				catch
				{
				}
				if (ips == null || ips.Count <= 0)
				{
					return;
				}
				using (StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "extracted_ips.txt"), append: true, Encoding.UTF8))
				{
					string currentSrc = type.GetProperty("Source")?.GetValue(sender)?.ToString() ?? "";
					sw.WriteLine($"# 提取时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} 来源: {currentSrc} 共{ips.Count}条");
					foreach (string ip2 in ips)
					{
						sw.WriteLine(ip2);
					}
				}
				if (webViewCboEngine != null && webViewCboEngine.InvokeRequired)
				{
					webViewCboEngine.BeginInvoke((Action)delegate
					{
						DarkMessageBox.Show($"已提取 {ips.Count} 条IP地址\n保存到: extracted_ips.txt", "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					});
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private void WebView2WebMessageReceivedHandler(object sender, EventArgs e)
	{
		try
		{
			Type argsType = e.GetType();
			PropertyInfo msgProp = argsType.GetProperty("Message") ?? argsType.GetProperty("TryGetWebMessageAsString");
			string message = null;
			if (msgProp != null && msgProp.PropertyType == typeof(string))
			{
				message = msgProp.GetValue(e) as string;
			}
			else
			{
				MethodInfo tryGetMethod = argsType.GetMethod("TryGetWebMessageAsString");
				if (tryGetMethod != null)
				{
					message = tryGetMethod.Invoke(e, null) as string;
				}
			}
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			Dictionary<string, string> data = new Dictionary<string, string>();
			foreach (Match m in Regex.Matches(message, "\"(\\w+)\":\"([^\"]*)\""))
			{
				data[m.Groups[1].Value] = m.Groups[2].Value;
			}
			if (data == null || !data.ContainsKey("type") || !(data["type"] == "login"))
			{
				return;
			}
			string url = (data.ContainsKey("url") ? data["url"] : "");
			string username = (data.ContainsKey("username") ? data["username"] : "");
			string password = (data.ContainsKey("password") ? data["password"] : "");
			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(username))
			{
				return;
			}
			loginDataPath = Path.Combine(Application.StartupPath, "login_data.txt");
			bool exists = File.Exists(loginDataPath);
			bool hasExisting = false;
			if (exists)
			{
				string[] array = File.ReadAllLines(loginDataPath, Encoding.UTF8);
				foreach (string line in array)
				{
					if (line.Contains(url) && line.Contains(username))
					{
						hasExisting = true;
						break;
					}
				}
			}
			if (hasExisting)
			{
				return;
			}
			using StreamWriter sw = new StreamWriter(loginDataPath, append: true, Encoding.UTF8);
			if (!exists)
			{
				sw.WriteLine("# WebView2登录信息记录");
			}
			sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {url} | 用户名: {username} | 密码: {password}");
		}
		catch
		{
		}
	}

		private async Task ParseAndDownloadLiveSources(object webView2, Type webView2Type, string ruleName)
		{
			try
		{
			string extractJs = "(function() {   var html = document.documentElement.outerHTML;   var matches = html.match(/(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?::(\\d{2,5}))?/g) || [];   var valid = [];   for (var i=0; i<matches.length && valid.length<300; i++) {     var parts = matches[i].split('.');     var ok = true;     for (var j=0; j<4; j++) { var n = parseInt(parts[j]); if (n<0||n>255) { ok=false; break; } }     if (ok) { if (valid.indexOf(matches[i]) === -1) valid.push(matches[i]); }   }   return JSON.stringify(valid); })()";
			PropertyInfo coreProp = webView2Type.GetProperty("CoreWebView2");
			if (coreProp == null)
			{
				return;
			}
			object core = coreProp.GetValue(webView2);
			if (core == null)
			{
				return;
			}
			MethodInfo execMethod = core.GetType().GetMethod("ExecuteScriptAsync", new Type[1] { typeof(string) });
			if (execMethod == null)
			{
				return;
			}
			MatchCollection matchCollection = Regex.Matches(await (Task<string>)execMethod.Invoke(core, new object[1] { extractJs }), "\"([^\"]+)\"");
			List<string> ipList = new List<string>();
			foreach (Match item in matchCollection)
			{
				string val = item.Groups[1].Value;
				if (Regex.IsMatch(val, "^\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}(:\\d+)?$"))
				{
					ipList.Add(val);
				}
			}
			if (ipList.Count == 0)
			{
				DarkMessageBox.Show("未在当前页面找到IP地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}
			hasSearchPlatformData = true;
			int addedCount = 0;
			DateTime parseTime = DateTime.Now;
			if (!autoParseLink)
			{
				foreach (string ipPort in ipList)
				{
					string[] parts = ipPort.Split(':');
					if (parts.Length != 2)
					{
						continue;
					}
					string ip = parts[0];
					string port = parts[1];
					string rootHttp = "http://" + ip + ":" + port;
					if (ruleName == "智慧光迅")
					{
						string url = rootHttp + "/ZHGXTV/Public/json/live_interface.txt";
						if (!allChannels.Any((ChannelInfo c) => c.Url == url))
						{
							allChannels.Add(new ChannelInfo
							{
								Name = ipPort,
								Url = url,
								Group = "解析待处理",
								Status = "待解析",
								ParseDateTime = parseTime
							});
							addedCount++;
						}
					}
					else if (ruleName == "华视美达")
					{
						Tuple<int, int> scanConfig = await ShowScanConfigDialogAsync();
						if (scanConfig == null)
						{
							continue;
						}
						int scanCount = scanConfig.Item1;
						int threadCount = scanConfig.Item2;
						if (lblProgressText != null)
						{
							lblProgressText.Text = "华视美达扫描进度:";
							lblProgressText.Refresh();
						}
						if (lblPercent != null)
						{
							lblPercent.Text = "0%";
							lblPercent.Refresh();
						}
						if (statusBarRef != null)
						{
							LayoutStatusBar(statusBarRef);
						}
						Refresh();
						ConcurrentBag<Tuple<string, string>> validResults = new ConcurrentBag<Tuple<string, string>>();
						List<int> cidList = Enumerable.Range(1, scanCount).ToList();
						int processedCount = 0;
						await Task.Run(delegate
						{
							//IL_000e: Unknown result type (might be due to invalid IL or missing references)
							//IL_0018: Expected O, but got Unknown
							HttpClient httpClient2 = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 32 });
							try
							{
								httpClient2.Timeout = TimeSpan.FromSeconds(2.5);
								((HttpHeaders)httpClient2.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
								Parallel.ForEach(cidList, new ParallelOptions
								{
									MaxDegreeOfParallelism = threadCount
								}, delegate(int num)
								{
									//IL_0035: Unknown result type (might be due to invalid IL or missing references)
									//IL_003f: Expected O, but got Unknown
									string text = $"{rootHttp}/newlive/live/hls/{num}/live.m3u8";
									try
									{
										if (httpClient2.SendAsync(new HttpRequestMessage(HttpMethod.Head, text)).Result.IsSuccessStatusCode)
										{
											HttpResponseMessage result3 = httpClient2.GetAsync(text).Result;
											if (result3.IsSuccessStatusCode)
											{
												string result4 = result3.Content.ReadAsStringAsync().Result;
												if (!string.IsNullOrEmpty(result4) && result4.Contains("#EXTM3U"))
												{
													validResults.Add(Tuple.Create(text, result4));
												}
											}
											return;
										}
									}
									catch
									{
									}
									try
									{
										HttpResponseMessage result5 = httpClient2.GetAsync(text).Result;
										if (result5.IsSuccessStatusCode)
										{
											string result6 = result5.Content.ReadAsStringAsync().Result;
											if (!string.IsNullOrEmpty(result6) && result6.Contains("#EXTM3U"))
											{
												validResults.Add(Tuple.Create(text, result6));
											}
										}
									}
									catch
									{
									}
									int num2 = Interlocked.Increment(ref processedCount);
									int pct = (int)((double)num2 * 100.0 / (double)scanCount);
									if (lblPercent != null && !lblPercent.IsDisposed)
									{
										try
										{
											lblPercent.Invoke((Action)delegate
											{
												if (lblPercent != null && !lblPercent.IsDisposed)
												{
													lblPercent.Text = $"{pct}%";
												}
												if (statusBarRef != null && !statusBarRef.IsDisposed)
												{
													progressBarWidth = statusBarRef.ClientSize.Width * pct / 100;
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
											});
										}
										catch
										{
										}
									}
								});
							}
							finally
							{
								if (httpClient2 != null)
								{
									((IDisposable)httpClient2).Dispose();
								}
							}
						});
						if (lblProgressText != null && !lblProgressText.IsDisposed)
						{
							lblProgressText.Text = "华视美达扫描完成:";
						}
						if (lblPercent != null && !lblPercent.IsDisposed)
						{
							lblPercent.Text = $"找到{validResults.Count}个";
						}
						if (statusBarRef != null)
						{
							LayoutStatusBar(statusBarRef);
						}
						Refresh();
						foreach (Tuple<string, string> result in validResults)
						{
							if (!allChannels.Any((ChannelInfo c) => c.Url == result.Item1))
							{
								string[] urlParts = result.Item1.Split('/');
								string cid = ((urlParts.Length > 1) ? urlParts[urlParts.Length - 2] : "");
								allChannels.Add(new ChannelInfo
								{
									Name = ipPort + "_CID" + cid,
									Url = result.Item1,
									Group = "解析待处理",
									Status = "待解析",
									ParseDateTime = parseTime
								});
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
						{
							LayoutStatusBar(statusBarRef);
						}
					}
					else
					{
						string url2 = rootHttp + "/iptv/live/1000.json?key=txiptv";
						if (!allChannels.Any((ChannelInfo c) => c.Url == url2))
						{
							allChannels.Add(new ChannelInfo
							{
								Name = ipPort,
								Url = url2,
								Group = "解析待处理",
								Status = "待解析",
								ParseDateTime = parseTime
							});
							addedCount++;
						}
					}
				}
			}
			else
			{
				HttpClient httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 32 });
				try
				{
					httpClient.Timeout = TimeSpan.FromSeconds(8.0);
					((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
					foreach (string ipPort in ipList)
					{
						string[] parts2 = ipPort.Split(':');
						if (parts2.Length != 2)
						{
							continue;
						}
						string ip2 = parts2[0];
						string port2 = parts2[1];
						string rootHttp2 = "http://" + ip2 + ":" + port2;
						string url3;
						if (ruleName == "智慧光迅")
						{
							url3 = rootHttp2 + "/ZHGXTV/Public/json/live_interface.txt";
							try
							{
								HttpResponseMessage resp = await httpClient.GetAsync(url3);
								if (resp.IsSuccessStatusCode)
								{
									string content = await resp.Content.ReadAsStringAsync();
									if (!string.IsNullOrEmpty(content))
									{
										ParseZhgxTv(content, url3, parseTime);
										addedCount++;
									}
								}
							}
							catch
							{
							}
							continue;
						}
						if (ruleName == "华视美达")
						{
							Tuple<int, int> scanConfig2 = await ShowScanConfigDialogAsync();
							if (scanConfig2 == null)
							{
								continue;
							}
							int scanCount2 = scanConfig2.Item1;
							int threadCount2 = scanConfig2.Item2;
							ConcurrentBag<Tuple<string, string>> validResults2 = new ConcurrentBag<Tuple<string, string>>();
							List<int> cidList2 = Enumerable.Range(1, scanCount2).ToList();
							await Task.Run(delegate
							{
								Parallel.ForEach(cidList2, new ParallelOptions
								{
									MaxDegreeOfParallelism = threadCount2
								}, delegate(int num)
								{
									//IL_0028: Unknown result type (might be due to invalid IL or missing references)
									//IL_0032: Expected O, but got Unknown
									string text = $"{rootHttp2}/newlive/live/hls/{num}/live.m3u8";
									try
									{
										if (httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, text)).Result.IsSuccessStatusCode)
										{
											HttpResponseMessage result3 = httpClient.GetAsync(text).Result;
											if (result3.IsSuccessStatusCode)
											{
												string result4 = result3.Content.ReadAsStringAsync().Result;
												if (!string.IsNullOrEmpty(result4) && result4.Contains("#EXTM3U"))
												{
													validResults2.Add(Tuple.Create(text, result4));
												}
											}
											return;
										}
									}
									catch
									{
									}
									try
									{
										HttpResponseMessage result5 = httpClient.GetAsync(text).Result;
										if (result5.IsSuccessStatusCode)
										{
											string result6 = result5.Content.ReadAsStringAsync().Result;
											if (!string.IsNullOrEmpty(result6) && result6.Contains("#EXTM3U"))
											{
												validResults2.Add(Tuple.Create(text, result6));
											}
										}
									}
									catch
									{
									}
								});
							});
							foreach (Tuple<string, string> result2 in validResults2)
							{
								if (!allChannels.Any((ChannelInfo c) => c.Url == result2.Item1))
								{
									string[] urlParts2 = result2.Item1.Split('/');
									string cid2 = ((urlParts2.Length > 1) ? urlParts2[urlParts2.Length - 2] : "");
									allChannels.Add(new ChannelInfo
									{
										Name = ipPort + "_CID" + cid2,
										Url = result2.Item1,
										Group = "解析待处理",
										Status = "待解析",
										ParseDateTime = parseTime
									});
									addedCount++;
								}
							}
							continue;
						}
						url3 = rootHttp2 + "/iptv/live/1000.json?key=txiptv";
						try
						{
							HttpResponseMessage resp2 = await httpClient.GetAsync(url3);
							if (resp2.IsSuccessStatusCode)
							{
								string content2 = await resp2.Content.ReadAsStringAsync();
								if (!string.IsNullOrEmpty(content2))
								{
									ParseKutvJson(content2, url3, parseTime);
									addedCount++;
								}
							}
						}
						catch
						{
						}
					}
				}
				finally
				{
					if (httpClient != null)
					{
						((IDisposable)httpClient).Dispose();
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
				if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
				{
					lblDetected.Text = $"已检测: 0/{totalCount}";
					lblAvailable.Text = "可用: 0";
					lblPercent.Text = "0.00%";
					progressBarWidth = 0;
					RestoreLabelColors();
					statusBarRef.PerformLayout();
					LayoutStatusBar(statusBarRef);
					statusBarRef.Refresh();
				}
				if (!autoParseLink)
				{
					DarkMessageBox.Show($"已提取 {addedCount} 条链接到待解析列表\n请点击\"解析链接\"按钮进行解析", "提取完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				else
				{
					DarkMessageBox.Show($"解析完成！\n成功: {addedCount} 个IP\n请点击\"开始检测\"按钮验证链接有效性", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			}
			else
			{
				DarkMessageBox.Show($"未解析到有效直播源\n共检测 {ipList.Count} 个IP，全部失败", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("解析下载出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private bool IsIPv6Supported()
	{
		try
		{
			if (!Socket.OSSupportsIPv6)
			{
				return false;
			}
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface ni in allNetworkInterfaces)
			{
				if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation unicastAddress in ni.GetIPProperties().UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6)
					{
						return true;
					}
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
		if (m.Msg == 274)
		{
			int wparam = m.WParam.ToInt32() & 0xFFF0;
			if (wparam == 61440)
			{
				_isRestoringFromMinimize = true;
			}
		}
		if (m.Msg == 36)
		{
			base.WndProc(ref m);
			Screen screen = Screen.FromControl(this);
			Marshal.WriteInt32(m.LParam, 16, screen.WorkingArea.Left);
			Marshal.WriteInt32(m.LParam, 20, screen.WorkingArea.Top);
			Marshal.WriteInt32(m.LParam, 8, screen.WorkingArea.Width);
			Marshal.WriteInt32(m.LParam, 12, screen.WorkingArea.Height);
		}
		else if (m.Msg == 132 && base.WindowState != FormWindowState.Maximized)
		{
			base.WndProc(ref m);
			if ((int)m.Result == 1)
			{
				int x = (short)(m.LParam.ToInt32() & 0xFFFF);
				int y = (short)(m.LParam.ToInt32() >> 16);
				Point pt = PointToClient(new Point(x, y));
				int border = 6;
				bool left = pt.X <= border;
				bool right = pt.X >= base.ClientSize.Width - border;
				bool top = pt.Y <= border;
				bool bottom = pt.Y >= base.ClientSize.Height - border;
				if (top && left)
				{
					m.Result = (IntPtr)13;
				}
				else if (top && right)
				{
					m.Result = (IntPtr)14;
				}
				else if (bottom && left)
				{
					m.Result = (IntPtr)16;
				}
				else if (bottom && right)
				{
					m.Result = (IntPtr)17;
				}
				else if (left)
				{
					m.Result = (IntPtr)10;
				}
				else if (right)
				{
					m.Result = (IntPtr)11;
				}
				else if (top)
				{
					m.Result = (IntPtr)12;
				}
				else if (bottom)
				{
					m.Result = (IntPtr)15;
				}
			}
		}
		else if (m.Msg == 736)
		{
			float newScale = (float)(int)(m.WParam.ToInt64() & 0xFFFF) / 96f;
			if (Math.Abs(newScale - dpiScale) > 0.01f)
			{
				dpiScale = newScale;
				DarkMessageBox.DpiScale = dpiScale;
				config.Initialize(dpiScale);
				if (base.IsHandleCreated && !base.IsDisposed && !IsRestoringFromMinimize)
				{
					Invoke((Action)delegate
					{
						if (!IsRestoringFromMinimize)
						{
							SuspendLayout();
							base.Controls.Clear();
							BuildUI();
							ResumeLayout(performLayout: true);
						}
					});
				}
			}
			m.Result = IntPtr.Zero;
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	private void DgvData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}
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
				speedColor = ((ms < 1000) ? Color.FromArgb(39, 174, 96) : ((ms >= 3500) ? Color.FromArgb(231, 76, 60) : Color.FromArgb(230, 126, 34)));
			}
			e.CellStyle.ForeColor = speedColor;
			e.CellStyle.SelectionForeColor = speedColor;
		}
	}

	private void DgvData_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.ColumnIndex < 0)
		{
			return;
		}
		string colName = dgvData.Columns[e.ColumnIndex].Name;
		if (!(colName == "colAction"))
		{
			if (sortedColumn == colName)
			{
				sortDirection = ((sortDirection != SortOrder.Ascending) ? SortOrder.Ascending : SortOrder.Descending);
			}
			else
			{
				sortedColumn = colName;
				sortDirection = SortOrder.Ascending;
			}
			SortChannels();
			dgvData.Invalidate();
		}
	}

	private Color LightenColor(Color color, int percent)
	{
		float factor = 1f + (float)percent / 100f;
		int r = Math.Min(255, (int)((float)(int)color.R * factor));
		int g = Math.Min(255, (int)((float)(int)color.G * factor));
		int b = Math.Min(255, (int)((float)(int)color.B * factor));
		return Color.FromArgb(color.A, r, g, b);
	}

	private async Task<Tuple<int, int>> ShowScanConfigDialogAsync()
	{
		if (base.InvokeRequired || !base.IsHandleCreated)
		{
			try
			{
				return await (Task<Tuple<int, int>>)Invoke(new Func<Task<Tuple<int, int>>>(ShowScanConfigDialogAsync));
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}
		int scanCount = 100;
		int threadCount = 8;
		TaskCompletionSource<DialogResult> tcs = new TaskCompletionSource<DialogResult>();
		Form scanDlg = new Form();
		try
		{
			NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
			scanDlg.Text = "华视美达扫描配置";
			scanDlg.ClientSize = new Size(SX(420), SY(360));
			scanDlg.StartPosition = FormStartPosition.Manual;
			scanDlg.MaximizeBox = false;
			scanDlg.MinimizeBox = false;
			scanDlg.Icon = this.Icon;
			var ctx = NeonChrome.Apply(scanDlg, pal, "华视美达扫描配置", dpiScale);
			int ox = ctx.Margin, oy = ctx.Margin + ctx.TitleHeight;
			Point At(int x, int y) => new Point(x - ox, y);
			CenterForm(scanDlg, this);
			int labelW = SX(130);
			int inputX = SX(150);
			int inputW = SX(120);
			int rowH = SY(45);
			int startY = SY(30);
			Label lblCount = new Label
			{
				Text = "扫描CID数量:",
				Location = At(SX(25), startY),
				Size = new Size(labelW, SY(28)),
				Font = GetFont(SF(11f)),
				ForeColor = pal.Label,
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(lblCount);
			NeonTextBox txtCount = new NeonTextBox
			{
				Text = "100",
				Location = At(inputX, startY + SY(2)),
				Size = new Size(inputW, SY(30)),
				Font = GetFont(SF(11f)),
				BackColorX = pal.PanelBg,
				BorderColor = pal.Border,
				FocusColor = pal.FocusBorder,
				GlowColor = pal.Glow,
				TextColor = pal.InputText,
				GlowEnabled = !pal.SuppressGlow
			};
			ctx.Body.Controls.Add(txtCount);
			Label lblThread = new Label
			{
				Text = "并发线程数:",
				Location = At(SX(25), startY + rowH),
				Size = new Size(labelW, SY(28)),
				Font = GetFont(SF(11f)),
				ForeColor = pal.Label,
				BackColor = Color.Transparent
			};
			ctx.Body.Controls.Add(lblThread);
			NeonTextBox txtThread = new NeonTextBox
			{
				Text = "8",
				Location = At(inputX, startY + rowH + SY(2)),
				Size = new Size(inputW, SY(30)),
				Font = GetFont(SF(11f)),
				BackColorX = pal.PanelBg,
				BorderColor = pal.Border,
				FocusColor = pal.FocusBorder,
				GlowColor = pal.Glow,
				TextColor = pal.InputText,
				GlowEnabled = !pal.SuppressGlow
			};
			ctx.Body.Controls.Add(txtThread);
			int btnW = SX(100);
			int btnH = SY(38);
			int btnGap = SX(30);
			int btnGroupW = btnW * 2 + btnGap;
			int btnStartX = (scanDlg.ClientSize.Width - btnGroupW) / 2;
			int btnY = SY(195);
			NeonButton btnOK = new NeonButton
			{
				Text = "确定",
				Location = At(btnStartX, btnY),
				Size = new Size(btnW, btnH),
				Font = GetFont(SF(11f)),
				IsPrimary = true,
				GradientStart = pal.Neon,
				GradientEnd = pal.Neon2,
				TextColorX = pal.PrimaryText,
				GlowColor = pal.Glow,
				GlowEnabled = !pal.SuppressGlow,
				Radius = 6
			};
			btnOK.Click += delegate
			{
				tcs.SetResult(DialogResult.OK);
				scanDlg.Close();
			};
			ctx.Body.Controls.Add(btnOK);
			NeonButton btnCancel = new NeonButton
			{
				Text = "取消",
				Location = At(btnStartX + btnW + btnGap, btnY),
				Size = new Size(btnW, btnH),
				Font = GetFont(SF(11f)),
				IsPrimary = false,
				BorderColor = pal.Border,
				GlowColor = pal.Glow,
				GlowEnabled = !pal.SuppressGlow,
				Radius = 6
			};
			btnCancel.Click += delegate
			{
				tcs.SetResult(DialogResult.Cancel);
				scanDlg.Close();
			};
			ctx.Body.Controls.Add(btnCancel);
			scanDlg.FormClosing += delegate
			{
				if (!tcs.Task.IsCompleted)
				{
					tcs.SetResult(DialogResult.Cancel);
				}
			};
			scanDlg.Show(this);
			if (await tcs.Task == DialogResult.OK)
			{
				int.TryParse(txtCount.Text, out scanCount);
				int.TryParse(txtThread.Text, out threadCount);
				if (scanCount < 1)
				{
					scanCount = 1;
				}
				if (scanCount > 500)
				{
					scanCount = 500;
				}
				if (threadCount < 1)
				{
					threadCount = 1;
				}
				if (threadCount > 20)
				{
					threadCount = 20;
				}
				return Tuple.Create(scanCount, threadCount);
			}
		}
		finally
		{
			if (scanDlg != null)
			{
				((IDisposable)scanDlg).Dispose();
			}
		}
		return null;
	}

	private void SortChannels()
	{
		if (string.IsNullOrEmpty(sortedColumn))
		{
			return;
		}
		int asc = ((sortDirection == SortOrder.Ascending) ? 1 : (-1));
		Comparison<ChannelInfo> cmp = null;
		switch (sortedColumn)
		{
		case "colName":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal) * asc;
			break;
		case "colUrl":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Url, b.Url, StringComparison.Ordinal) * asc;
			break;
		case "colLocation":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Location, b.Location, StringComparison.Ordinal) * asc;
			break;
		case "colResolution":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Resolution, b.Resolution, StringComparison.Ordinal) * asc;
			break;
		case "colSpeed":
			cmp = delegate(ChannelInfo a, ChannelInfo b)
			{
				int num = ParseSpeed(a.Speed);
				int value = ParseSpeed(b.Speed);
				return num.CompareTo(value) * asc;
			};
			break;
		case "colGroup":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Group, b.Group, StringComparison.Ordinal) * asc;
			break;
		case "colStatus":
			cmp = (ChannelInfo a, ChannelInfo b) => string.Compare(a.Status, b.Status, StringComparison.Ordinal) * asc;
			break;
		}
		if (cmp != null)
		{
			allChannels.Sort(cmp);
			RefreshGrid();
		}
	}

	private static GraphicsPath RoundedRectPath(Rectangle r, int radius)
	{
		return DrawingUtils.RoundedRect(r, radius);
	}

	private void DrawStatusTag(Graphics g, Rectangle bounds, string text, Color bg, Color border, Color foreColor)
	{
		int tagH = SY(22);
		int tagPad = SX(10);
		using Font tagFont = GetFont(SF(6.7f));
		int tagW = TextRenderer.MeasureText(text, tagFont).Width + tagPad * 2;
		int tagX = bounds.X + (bounds.Width - tagW) / 2;
		int tagY = bounds.Y + (bounds.Height - tagH) / 2;
		Rectangle tagRect = new Rectangle(tagX, tagY, tagW, tagH);
		using (GraphicsPath path = RoundedRectPath(tagRect, SX(11)))
		{
			using (SolidBrush bgBrush = new SolidBrush(bg))
			{
				g.FillPath(bgBrush, path);
			}
			using Pen pen = new Pen(border, 1f);
			g.DrawPath(pen, path);
		}
		TextRenderer.DrawText(g, text, tagFont, tagRect, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}

	private void DrawRoundedButton(Graphics g, Rectangle rect, string text, Color bg, Color foreColor)
	{
		using (GraphicsPath path = RoundedRectPath(rect, SX(4)))
		{
			using SolidBrush bgBrush = new SolidBrush(bg);
			g.FillPath(bgBrush, path);
		}
		using Font btnFont = GetFont(SF(6.7f));
		TextRenderer.DrawText(g, text, btnFont, rect, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
				using SolidBrush bg = new SolidBrush(color);
				using GraphicsPath rounded = RoundedRectPath(new Rectangle(1, 1, s - 2, s - 2), 3);
				using Pen white = new Pen(Color.White, 1.5f);
				using SolidBrush whiteB = new SolidBrush(Color.White);
				g.FillPath(bg, rounded);
				if (iconType != null)
				{
					switch (iconType.Length)
					{
					case 6:
						switch (iconType[2])
						{
						case 't':
							if (iconType == "detect")
							{
								g.DrawString("⚡", new Font("Segoe UI Symbol", 9f), whiteB, 0f, 0f);
							}
							break;
						case 'n':
							if (iconType == "rename")
							{
								g.DrawLine(white, 4, 12, 10, 6);
								g.DrawLine(white, 10, 6, 12, 8);
								g.DrawLine(white, 4, 12, 6, 12);
							}
							break;
						case 'l':
							if (iconType == "delete")
							{
								g.DrawLine(white, 4, 4, 12, 12);
								g.DrawLine(white, 12, 4, 4, 12);
							}
							break;
						case 'x':
							if (iconType == "fixAll")
							{
								g.DrawString("\ud83d\udd27", new Font("Segoe UI Symbol", 7f), whiteB, 0f, 1f);
								g.DrawString("↻", new Font("Segoe UI Symbol", 7f), whiteB, 7f, 1f);
							}
							break;
						}
						break;
					case 4:
						switch (iconType[0])
						{
						case 's':
							if (iconType == "sort")
							{
								g.DrawLine(white, 8, 3, 8, 13);
								g.DrawLine(white, 5, 6, 8, 3);
								g.DrawLine(white, 11, 6, 8, 3);
								g.DrawLine(white, 5, 10, 8, 13);
								g.DrawLine(white, 11, 10, 8, 13);
							}
							break;
						case 'p':
							if (iconType == "play")
							{
								Point[] tri = new Point[3]
								{
									new Point(5, 3),
									new Point(13, 8),
									new Point(5, 13)
								};
								g.FillPolygon(whiteB, tri);
							}
							break;
						case 'c':
							if (iconType == "copy")
							{
								g.DrawRectangle(white, 4, 4, 6, 7);
								g.DrawRectangle(white, 7, 6, 6, 7);
								g.FillRectangle(bg, 7, 6, 2, 1);
								g.FillRectangle(bg, 4, 10, 3, 1);
							}
							break;
						case 'i':
							if (iconType == "info")
							{
								g.FillEllipse(whiteB, 7, 3, 2, 2);
								g.DrawLine(white, 8, 6, 8, 12);
							}
							break;
						}
						break;
					case 8:
						switch (iconType[5])
						{
						case 'I':
							if (iconType == "clearInv")
							{
								g.DrawLine(white, 3, 13, 13, 3);
								g.DrawEllipse(white, 3, 3, 10, 10);
							}
							break;
						case 'A':
							if (iconType == "clearAll")
							{
								g.DrawLine(white, 4, 5, 12, 5);
								g.DrawLine(white, 5, 5, 5, 12);
								g.DrawLine(white, 11, 5, 11, 12);
								g.DrawLine(white, 5, 12, 11, 12);
								g.DrawLine(white, 7, 3, 9, 3);
								g.DrawLine(white, 7, 3, 7, 5);
								g.DrawLine(white, 9, 3, 9, 5);
								g.DrawLine(white, 3, 5, 13, 5);
							}
							break;
						}
						break;
					case 3:
						switch (iconType[0])
						{
						case 'f':
							if (iconType == "fix")
							{
								g.DrawString("\ud83d\udd27", new Font("Segoe UI Symbol", 9f), whiteB, 0f, 0f);
							}
							break;
						case 's':
							if (iconType == "sub")
							{
								Point[] arrow = new Point[3]
								{
									new Point(6, 4),
									new Point(11, 8),
									new Point(6, 12)
								};
								g.FillPolygon(whiteB, arrow);
							}
							break;
						}
						break;
					case 5:
						if (iconType == "paste")
						{
							g.DrawRectangle(white, 4, 3, 8, 10);
							g.FillRectangle(whiteB, 6, 2, 4, 2);
							g.FillRectangle(bg, 6, 3, 4, 2);
						}
						break;
					case 9:
						if (iconType == "selectAll")
						{
							g.DrawRectangle(white, 3, 3, 5, 5);
							g.DrawRectangle(white, 9, 3, 5, 5);
							g.DrawRectangle(white, 3, 9, 5, 5);
							g.DrawRectangle(white, 9, 9, 5, 5);
						}
						break;
					case 7:
						if (iconType == "copyAll")
						{
							g.DrawRectangle(white, 3, 3, 5, 6);
							g.DrawRectangle(white, 9, 3, 5, 6);
							g.DrawRectangle(white, 3, 9, 5, 6);
							g.DrawRectangle(white, 9, 9, 5, 6);
						}
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

	private void DgvData_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
	{
		e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		if (e.RowIndex == -1 && e.ColumnIndex >= 0)
		{
			e.PaintBackground(e.ClipBounds, cellsPaintSelectionBackground: false);
			string colName = dgvData.Columns[e.ColumnIndex].Name;
			bool isSorted = colName == sortedColumn;
			using (SolidBrush bgBrush = new SolidBrush(theme.HeaderBg))
			{
				e.Graphics.FillRectangle(bgBrush, e.CellBounds);
			}
			using (Pen pen = new Pen(theme.Border, 1f))
			{
				e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
			}
			if (e.ColumnIndex < dgvData.Columns.Count - 1)
			{
				using Pen sepPen = new Pen(theme.Border, 1f);
				e.Graphics.DrawLine(sepPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
			}
			string headerText = e.FormattedValue?.ToString() ?? "";
			using (Font headerFont = dgvData.ColumnHeadersDefaultCellStyle.Font ?? GetFont(SF(9f)))
			{
				int arrowW = (isSorted ? SX(16) : 0);
				int textPad = SX(12);
				Rectangle textRect = new Rectangle(e.CellBounds.X + textPad, e.CellBounds.Y, e.CellBounds.Width - textPad - arrowW - 4, e.CellBounds.Height);
				TextFormatFlags tff = TextFormatFlags.VerticalCenter;
				tff = ((!(colName == "colName") && !(colName == "colUrl")) ? (tff | TextFormatFlags.HorizontalCenter) : (tff | TextFormatFlags.Default));
				Color headerColor = (isSorted ? theme.Primary : theme.TextPrimary);
				TextRenderer.DrawText(e.Graphics, headerText, headerFont, textRect, headerColor, tff);
				if (isSorted && colName != "colAction")
				{
					string arrow = ((sortDirection == SortOrder.Ascending) ? "▲" : "▼");
					using Font arrowFont = new Font(dgvData.Font.FontFamily, SF(7f), FontStyle.Bold);
					Size arrowSize = TextRenderer.MeasureText(arrow, arrowFont);
					int arrowX = textRect.Right + 2;
					int arrowY = e.CellBounds.Y + (e.CellBounds.Height - arrowSize.Height) / 2;
					TextRenderer.DrawText(e.Graphics, arrow, arrowFont, new Point(arrowX, arrowY), theme.Primary);
				}
			}
			e.Handled = true;
		}
		else
		{
			if (e.RowIndex < 0)
			{
				return;
			}
			string colName2 = dgvData.Columns[e.ColumnIndex].Name;
			Color rowSepColor = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(75, 75, 90) : Color.FromArgb(220, 220, 230));
			if (e.ColumnIndex < dgvData.Columns.Count - 1)
			{
				using Pen sepPen2 = new Pen(rowSepColor, 1f);
				e.Graphics.DrawLine(sepPen2, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
			}
			if (colName2 == "colStatus")
			{
				e.PaintBackground(e.ClipBounds, cellsPaintSelectionBackground: false);
				int r = e.RowIndex;
				bool selected = dgvData.Rows[r].Selected;
				bool isHover = _hoverRow == r;
				if (selected)
				{
					using SolidBrush selBrush = new SolidBrush(theme.SelectRow);
					e.Graphics.FillRectangle(selBrush, e.CellBounds);
				}
				else if (isHover)
				{
					using SolidBrush hoverBrush = new SolidBrush(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(65, 60, 80) : Color.FromArgb(245, 240, 252));
					e.Graphics.FillRectangle(hoverBrush, e.CellBounds);
				}
				string status = e.Value?.ToString() ?? "";
				switch (status)
				{
				case "可用":
					DrawStatusTag(e.Graphics, e.CellBounds, status, theme.StatusTagBg, theme.StatusTagBorder, theme.SuccessColor);
					break;
				case "不可用":
				{
					Color bg2 = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(80, 40, 40) : Color.FromArgb(255, 235, 235));
					DrawStatusTag(e.Graphics, e.CellBounds, status, bg2, theme.ErrorColor, theme.ErrorColor);
					break;
				}
				case "检测中":
				{
					Color bg = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(80, 65, 30) : Color.FromArgb(255, 248, 230));
					DrawStatusTag(e.Graphics, e.CellBounds, status, bg, theme.WarnColor, theme.WarnColor);
					break;
				}
				default:
					TextRenderer.DrawText(e.Graphics, status, dgvData.Font, e.CellBounds, theme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
					break;
				}
				using (Pen pen2 = new Pen(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247), 1f))
				{
					e.Graphics.DrawLine(pen2, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
				}
				e.Handled = true;
				return;
			}
			if (colName2 == "colAction")
			{
				e.PaintBackground(e.ClipBounds, cellsPaintSelectionBackground: false);
				int r2 = e.RowIndex;
				bool selected2 = dgvData.Rows[r2].Selected;
				bool isHover2 = _hoverRow == r2;
				if (selected2)
				{
					using SolidBrush selBrush2 = new SolidBrush(theme.SelectRow);
					e.Graphics.FillRectangle(selBrush2, e.CellBounds);
				}
				else if (isHover2)
				{
					using SolidBrush hoverBrush2 = new SolidBrush(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(65, 60, 80) : Color.FromArgb(245, 240, 252));
					e.Graphics.FillRectangle(hoverBrush2, e.CellBounds);
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
				if (_pressRow == r2 && _pressBtn == 0)
				{
					copyBg = Color.FromArgb(Math.Min(255, theme.CopyBtnBg.R + 30), Math.Min(255, theme.CopyBtnBg.G + 30), Math.Min(255, theme.CopyBtnBg.B + 30));
					copyRect.Offset(0, 1);
				}
				else if (_hoverRow == r2 && _hoverBtn == 0)
				{
					copyBg = Color.FromArgb(Math.Min(255, theme.CopyBtnBg.R + 18), Math.Min(255, theme.CopyBtnBg.G + 18), Math.Min(255, theme.CopyBtnBg.B + 18));
				}
				if (_pressRow == r2 && _pressBtn == 1)
				{
					playBg = Color.FromArgb(Math.Min(255, theme.PlayBtnBg.R + 30), Math.Min(255, theme.PlayBtnBg.G + 30), Math.Min(255, theme.PlayBtnBg.B + 30));
					playRect.Offset(0, 1);
				}
				else if (_hoverRow == r2 && _hoverBtn == 1)
				{
					playBg = Color.FromArgb(Math.Min(255, theme.PlayBtnBg.R + 18), Math.Min(255, theme.PlayBtnBg.G + 18), Math.Min(255, theme.PlayBtnBg.B + 18));
				}
				if (_clickRow == r2 && _clickBtn == 0)
				{
					copyBg = Color.FromArgb(Math.Max(0, theme.CopyBtnBg.R - 30), Math.Max(0, theme.CopyBtnBg.G - 30), Math.Max(0, theme.CopyBtnBg.B - 30));
					copyRect.Offset(0, 1);
				}
				if (_clickRow == r2 && _clickBtn == 1)
				{
					playBg = Color.FromArgb(Math.Max(0, theme.PlayBtnBg.R - 30), Math.Max(0, theme.PlayBtnBg.G - 30), Math.Max(0, theme.PlayBtnBg.B - 30));
					playRect.Offset(0, 1);
				}
				DrawRoundedButton(e.Graphics, copyRect, "复制", copyBg, copyFg);
				DrawRoundedButton(e.Graphics, playRect, "播放", playBg, playFg);
				Rectangle firstCell = dgvData.GetCellDisplayRectangle(0, e.RowIndex, cutOverflow: false);
				using (Pen pen3 = new Pen(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247), 1f))
				{
					e.Graphics.DrawLine(pen3, firstCell.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
				}
				e.Handled = true;
				return;
			}
			e.PaintBackground(e.ClipBounds, cellsPaintSelectionBackground: false);
			int r3 = e.RowIndex;
			bool isSelected = dgvData.Rows[r3].Selected;
			bool isHover3 = _hoverRow == r3;
			if (isSelected)
			{
				using SolidBrush selBrush3 = new SolidBrush(theme.SelectRow);
				e.Graphics.FillRectangle(selBrush3, e.CellBounds);
			}
			else if (isHover3)
			{
				using SolidBrush hoverBrush3 = new SolidBrush(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(65, 60, 80) : Color.FromArgb(245, 240, 252));
				e.Graphics.FillRectangle(hoverBrush3, e.CellBounds);
			}
			string cellText = e.FormattedValue?.ToString() ?? "";
			if (!string.IsNullOrEmpty(cellText))
			{
				Color textColor = (isSelected ? theme.SelectRowText : e.CellStyle.ForeColor);
				Font baseFont = e.CellStyle.Font ?? dgvData.Font;
				int padding = SX(10);
				Rectangle textRect2 = new Rectangle(e.CellBounds.X + padding, e.CellBounds.Y, e.CellBounds.Width - padding * 2, e.CellBounds.Height);
				TextFormatFlags tff2 = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
				switch (colName2)
				{
				case "colName":
				case "colUrl":
				case "colLocation":
				case "colResolution":
				case "colGroup":
					tff2 |= TextFormatFlags.Default;
					break;
				default:
					tff2 |= TextFormatFlags.HorizontalCenter;
					break;
				}
				Size textSize = TextRenderer.MeasureText(cellText, baseFont);
				if (textSize.Width > textRect2.Width)
				{
					float ratio = (float)textRect2.Width / (float)textSize.Width;
					float newSize = Math.Max(baseFont.Size * ratio, SF(6f));
					using Font scaledFont = new Font(baseFont.FontFamily, newSize, baseFont.Style);
					TextRenderer.DrawText(e.Graphics, cellText, scaledFont, textRect2, textColor, tff2);
				}
				else
				{
					TextRenderer.DrawText(e.Graphics, cellText, baseFont, textRect2, textColor, tff2);
				}
			}
			using (Pen pen4 = new Pen(DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(242, 242, 247), 1f))
			{
				e.Graphics.DrawLine(pen4, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
			}
			e.Handled = true;
		}
	}

	private void DgvData_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}
		dgvData.ClearSelection();
		dgvData.Rows[e.RowIndex].Selected = true;
		if (e.ColumnIndex != 7)
		{
			return;
		}
		Rectangle cellRect = dgvData.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, cutOverflow: false);
		Point mousePos = dgvData.PointToClient(Cursor.Position);
		int relX = mousePos.X - cellRect.X;
		int relY = mousePos.Y - cellRect.Y;
		int num = cellRect.Width;
		int cellH = cellRect.Height;
		int btnH = SY(26);
		int btnW = SX(46);
		int gap = SX(14);
		int totalW = btnW * 2 + gap;
		int startX = (num - totalW) / 2;
		int startY = (cellH - btnH) / 2;
		Rectangle copyBtnRect = new Rectangle(startX, startY, btnW, btnH);
		Rectangle playBtnRect = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
		string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}
		if (copyBtnRect.Contains(relX, relY))
		{
			StartButtonPress(e.RowIndex, 0);
			CopyTextToClipboard(url, cellRect.Y + cellRect.Height / 2);
		}
		else if (playBtnRect.Contains(relX, relY))
		{
			StartButtonPress(e.RowIndex, 1);
			if (!string.IsNullOrWhiteSpace(customPlayerPath) && File.Exists(customPlayerPath))
			{
				PlayChannelCustom(url);
			}
			else
			{
				StartPreview(url);
			}
		}
	}

	private int GetActionBtnIndex(int rowIndex, int x, int y)
	{
		if (rowIndex < 0)
		{
			return -1;
		}
		Rectangle cellRect = dgvData.GetCellDisplayRectangle(7, rowIndex, cutOverflow: false);
		if (cellRect.Width <= 0)
		{
			return -1;
		}
		int relX = x - cellRect.X;
		int relY = y - cellRect.Y;
		int num = cellRect.Width;
		int cellH = cellRect.Height;
		int btnH = SY(26);
		int btnW = SX(46);
		int gap = SX(14);
		int totalW = btnW * 2 + gap;
		int startX = (num - totalW) / 2;
		int startY = (cellH - btnH) / 2;
		Rectangle copyRect = new Rectangle(startX, startY, btnW, btnH);
		Rectangle playRect = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
		if (copyRect.Contains(relX, relY))
		{
			return 0;
		}
		if (playRect.Contains(relX, relY))
		{
			return 1;
		}
		return -1;
	}

	private void StartButtonPress(int row, int btn)
	{
		_clickRow = row;
		_clickBtn = btn;
		dgvData.InvalidateCell(7, row);
		if (_btnFlashTimer == null)
		{
			_btnFlashTimer = new System.Windows.Forms.Timer
			{
				Interval = 150
			};
			_btnFlashTimer.Tick += delegate
			{
				int clickRow = _clickRow;
				_clickRow = -1;
				_clickBtn = -1;
				_btnFlashTimer.Stop();
				if (clickRow >= 0)
				{
					dgvData.InvalidateCell(7, clickRow);
				}
			};
		}
		_btnFlashTimer.Stop();
		_btnFlashTimer.Start();
	}

	private void DgvData_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.ColumnIndex == 7 && e.RowIndex >= 0)
		{
			Rectangle cellRect = dgvData.GetCellDisplayRectangle(7, e.RowIndex, cutOverflow: false);
			int btnIdx = GetActionBtnIndex(e.RowIndex, cellRect.X + e.X, cellRect.Y + e.Y);
			int oldHoverRow = _hoverRow;
			int oldHoverBtn = _hoverBtn;
			_hoverRow = ((btnIdx >= 0) ? e.RowIndex : (-1));
			_hoverBtn = btnIdx;
			dgvData.Cursor = ((btnIdx >= 0) ? Cursors.Hand : Cursors.Default);
			if (oldHoverRow != _hoverRow || oldHoverBtn != _hoverBtn)
			{
				if (oldHoverRow >= 0)
				{
					dgvData.InvalidateRow(oldHoverRow);
				}
				if (_hoverRow >= 0)
				{
					dgvData.InvalidateRow(_hoverRow);
				}
			}
		}
		else if (e.RowIndex >= 0)
		{
			int oldHoverRow2 = _hoverRow;
			_hoverRow = e.RowIndex;
			_hoverBtn = -1;
			dgvData.Cursor = Cursors.Default;
			if (oldHoverRow2 != _hoverRow)
			{
				if (oldHoverRow2 >= 0)
				{
					dgvData.InvalidateRow(oldHoverRow2);
				}
				if (_hoverRow >= 0)
				{
					dgvData.InvalidateRow(_hoverRow);
				}
			}
		}
		else if (_hoverRow != -1)
		{
			int oldRow = _hoverRow;
			_hoverRow = -1;
			_hoverBtn = -1;
			dgvData.Cursor = Cursors.Default;
			if (oldRow >= 0 && oldRow < dgvData.Rows.Count)
			{
				dgvData.InvalidateRow(oldRow);
			}
		}
	}

	private void DgvData_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.ColumnIndex == 7 && e.RowIndex >= 0)
		{
			Rectangle cellRect = dgvData.GetCellDisplayRectangle(7, e.RowIndex, cutOverflow: false);
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

	private void CopyTextToClipboard(string text, int? targetY = null)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			Clipboard.Clear();
			Clipboard.SetDataObject(text, copy: true, 5, 100);
			ShowCopyToast(text, targetY);
		}
		catch
		{
			try
			{
				Thread.Sleep(50);
				Clipboard.SetDataObject(text, copy: true);
				ShowCopyToast(text, targetY);
			}
			catch
			{
				DarkMessageBox.Show("复制失败，剪贴板被占用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}

	private void ShowCopyToast(string url, int? targetY = null)
	{
		Point At(int x, int yy) => new Point(x, yy);
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		Color toastBg = theme.Surface;
		Color toastText = theme.TextPrimary;
		if (_toastPanel == null)
		{
			_toastPanel = new Panel
			{
				Size = new Size(SX(260), SY(50)),
				BackColor = toastBg,
				Visible = false
			};
			_toastPanel.Paint += delegate(object s, PaintEventArgs pe)
			{
				Graphics graphics = pe.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(_toastPanel.ClientRectangle, SX(12));
				_toastPanel.Region = new Region(path);
				using (SolidBrush brush = new SolidBrush(toastBg))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(theme.Border, 1f);
				graphics.DrawPath(pen, path);
			};
			Label lblIcon = new Label
			{
				Text = "✓",
				Font = GetFont(SF(11f), FontStyle.Bold),
				ForeColor = Color.FromArgb(46, 189, 96),
				Location = At(SX(18), SY(12)),
				AutoSize = true,
				BackColor = Color.Transparent
			};
			_toastPanel.Controls.Add(lblIcon);
			Label lblMsg = new Label
			{
				Text = "复制成功",
				Font = GetFont(SF(9f), FontStyle.Bold),
				ForeColor = toastText,
				Location = At(SX(46), SY(10)),
				AutoSize = true,
				BackColor = Color.Transparent
			};
			_toastPanel.Controls.Add(lblMsg);
			dgvData.Controls.Add(_toastPanel);
			_toastPanel.BringToFront();
			_toastTimer = new System.Windows.Forms.Timer
			{
				Interval = 2000
			};
			_toastTimer.Tick += delegate
			{
				_toastTimer.Stop();
				_toastPanel.Visible = false;
			};
		}
		else
		{
			_toastPanel.BackColor = toastBg;
			foreach (Control control in _toastPanel.Controls)
			{
				if (control is Label lbl)
				{
					lbl.ForeColor = ((lbl.Text == "✓") ? Color.FromArgb(46, 189, 96) : toastText);
					lbl.Font = ((lbl.Text == "✓") ? GetFont(SF(11f), FontStyle.Bold) : GetFont(SF(9f), FontStyle.Bold));
				}
			}
			_toastPanel.Invalidate();
		}
		int yPos;
		if (targetY.HasValue)
		{
			yPos = targetY.Value - _toastPanel.Height / 2;
			if (yPos < 4)
			{
				yPos = 4;
			}
			if (yPos > dgvData.ClientSize.Height - _toastPanel.Height - 4)
			{
				yPos = dgvData.ClientSize.Height - _toastPanel.Height - 4;
			}
		}
		else
		{
			yPos = dgvData.ClientSize.Height / 2 - _toastPanel.Height / 2;
		}
		int xPos = (dgvData.ClientSize.Width - _toastPanel.Width) / 2;
		if (xPos < 4)
		{
			xPos = 4;
		}
		_toastPanel.Location = At(xPos, yPos);
		_toastPanel.Visible = true;
		_toastPanel.BringToFront();
		_toastTimer.Stop();
		_toastTimer.Start();
	}

	private void DgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex >= 0 && e.ColumnIndex == 0)
		{
			dgvData.BeginEdit(selectAll: true);
		}
		else if (e.RowIndex >= 0 && e.ColumnIndex == 1)
		{
			string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString();
			if (!string.IsNullOrWhiteSpace(url))
			{
				ShowReplaceUrlDialog(url);
			}
		}
		else if (e.RowIndex >= 0)
		{
			// 双击其他列：在预览窗口中播放该频道
			PlaySelectedChannelInPreview();
		}
	}

	private void PlaySelectedChannelInPreview()
	{
		if (channelPlayer == null || previewPanel == null || !previewPanel.Visible || dgvData.SelectedRows.Count == 0)
		{
			return;
		}
		DataGridViewRow row = dgvData.SelectedRows[0];
		object urlCell = row.Cells["colUrl"].Value;
		if (urlCell == null)
		{
			return;
		}
		string url = urlCell.ToString();
		string name = row.Cells["colName"].Value?.ToString() ?? "";
		channelPlayer.LoadChannel(url, name);
	}

	private void DgvData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex >= 0 && e.ColumnIndex == 0)
		{
			string url = dgvData.Rows[e.RowIndex].Cells[1].Value?.ToString();
			string newName = dgvData.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
			ChannelInfo ch = allChannels.FirstOrDefault((ChannelInfo c) => c.Url == url);
			if (ch != null && !string.IsNullOrWhiteSpace(newName))
			{
				ch.Name = newName;
			}
		}
	}

	private (string protocol, string host, string port, string path) ParseUrl(string url)
	{
		try
		{
			Match match = Regex.Match(url, "^(https?|rtmp|rtsp)://([^:/]+)(?::(\\d+))?(.*)$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return (protocol: match.Groups[1].Value.ToLowerInvariant(), host: match.Groups[2].Value, port: match.Groups[3].Value, path: match.Groups[4].Value);
			}
		}
		catch
		{
		}
		return (protocol: "", host: "", port: "", path: "");
	}

	private void ShowReplaceUrlDialog(string originalUrl)
	{
		var (protocol, host, port, path) = ParseUrl(originalUrl);
		if (string.IsNullOrEmpty(protocol))
		{
			DarkMessageBox.Show("无法解析此链接格式！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		// 方案 E · 霓虹暗夜：调色板从当前主题派生，暗色=霓虹青/品红，亮色=主题强调色（严格适配所有主题）
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		using Form dlg = new Form();
		dlg.Text = "修复直播源地址";
		dlg.StartPosition = FormStartPosition.Manual;
		dlg.FormBorderStyle = FormBorderStyle.None;
		dlg.MaximizeBox = false;
		dlg.MinimizeBox = false;
		dlg.ShowInTaskbar = false;
		dlg.Font = GetFont(SF(10f));
		dlg.Width = SX(480);
		dlg.Height = SY(360);
		CenterForm(dlg, this);
		dlg.BackColor = pal.FormBg;
		int M = SX(16);
		int outerR = SX(14);
		int panelX = M;
		int panelY = M;
		int panelW = dlg.Width - 2 * M;
		int panelH = dlg.Height - 2 * M;
		int titleH = SY(36);
		int bodyX = panelX + SX(20);
		int bodyW = panelW - 2 * SX(20);
		int bodyTop = panelY + titleH + SY(14);
		dlg.Region = new Region(NeonHelper.RoundedRectPath(new Rectangle(0, 0, dlg.Width, dlg.Height), outerR));
		dlg.Paint += delegate (object s, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle panel = new Rectangle(panelX, panelY, panelW, panelH);
			using (GraphicsPath path = NeonHelper.RoundedRectPath(panel, outerR))
			{
				if (!pal.SuppressGlow)
				{
					for (int i = 6; i >= 1; i--)
					{
						using (Pen p = new Pen(Color.FromArgb(50 / i, pal.Glow.R, pal.Glow.G, pal.Glow.B), i * 2f))
						{
							g.DrawPath(p, path);
						}
					}
				}
				using (Brush b = new SolidBrush(pal.PanelBg))
				{
					g.FillPath(b, path);
				}
				using (Pen p = new Pen(NeonHelper.WithAlpha(pal.Neon, 120), 1.5f))
				{
					g.DrawPath(p, path);
				}
			}
		};
		Panel bodyPanel = new Panel
		{
			Location = new Point(0, 0),
			Size = dlg.ClientSize,
			BackColor = pal.FormBg
		};
		dlg.Controls.Add(bodyPanel);
		var ctx = new NeonChrome.Context { Body = bodyPanel, Margin = M, TitleHeight = titleH, Palette = pal };
		Point At(int x, int yy) => new Point(x, yy);
		// 自定义标题栏（顶部圆角 + 底部霓虹分隔线）
		Panel titlePanel = new Panel
		{
			Location = At(panelX, panelY),
			Size = new Size(panelW, titleH),
			BackColor = pal.TitleBg
		};
		{
			int d = outerR * 2;
			GraphicsPath tp = new GraphicsPath();
			tp.AddArc(0, 0, d, d, 180f, 90f);
			tp.AddArc(panelW - d, 0, d, d, 270f, 90f);
			tp.AddLine(panelW, outerR, panelW, titleH);
			tp.AddLine(panelW, titleH, 0, titleH);
			tp.AddLine(0, titleH, 0, outerR);
			tp.CloseFigure();
			titlePanel.Region = new Region(tp);
		}
		titlePanel.Paint += delegate (object s, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle r = titlePanel.ClientRectangle;
			using (GraphicsPath path = NeonHelper.RoundedRectPath(new Rectangle(0, 0, r.Width, r.Height + outerR), outerR))
			{
				using (Brush b = new SolidBrush(pal.TitleBg))
				{
					g.FillPath(b, path);
				}
			}
			using (Pen p = new Pen(NeonHelper.WithAlpha(pal.Neon, 60), 1f))
			{
				g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
			}
		};
		ctx.Body.Controls.Add(titlePanel);
		Label lblDot1 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(255, 95, 87),
			Location = At(SX(14), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblDot2 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(254, 188, 46),
			Location = At(SX(30), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblDot3 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(40, 200, 64),
			Location = At(SX(46), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblHead = new Label
		{
			Text = "修复直播源地址",
			Font = GetFont(SF(10f), FontStyle.Bold),
			ForeColor = pal.GhostText,
			Location = At(SX(64), SY(8)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblX = new Label
		{
			Text = "✕",
			Font = GetFont(SF(10f)),
			ForeColor = pal.GhostText,
			Location = At(panelW - SX(26), SY(8)),
			BackColor = pal.TitleBg,
			AutoSize = true,
			Cursor = Cursors.Hand
		};
		titlePanel.Controls.Add(lblDot1);
		titlePanel.Controls.Add(lblDot2);
		titlePanel.Controls.Add(lblDot3);
		titlePanel.Controls.Add(lblHead);
		titlePanel.Controls.Add(lblX);
		// 标题栏拖动
		Point dragOffset = Point.Empty;
		bool dragging = false;
		titlePanel.MouseDown += delegate (object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				dragging = true;
				dragOffset = e.Location;
			}
		};
		titlePanel.MouseMove += delegate (object s, MouseEventArgs e)
		{
			if (dragging)
			{
				dlg.Location = At(dlg.Left + e.X - dragOffset.X, dlg.Top + e.Y - dragOffset.Y);
			}
		};
		titlePanel.MouseUp += delegate (object s, MouseEventArgs e)
		{
			dragging = false;
		};
		lblHead.MouseDown += delegate (object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				dragging = true;
				dragOffset = e.Location;
			}
		};
		lblX.MouseEnter += delegate (object s, EventArgs e)
		{
			lblX.ForeColor = pal.Neon;
		};
		lblX.MouseLeave += delegate (object s, EventArgs e)
		{
			lblX.ForeColor = pal.GhostText;
		};
		lblX.Click += delegate (object s, EventArgs e)
		{
			dlg.DialogResult = DialogResult.Cancel;
			dlg.Close();
		};
		// 副标题
		Label lblSub = new Label
		{
			Text = "将 IP / 域名或网址替换为新地址",
			Font = GetFont(SF(8.5f), FontStyle.Bold),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblSub);
		// 原始地址（只读，暗色用 muted 色）
		Label lblOriginal = new Label
		{
			Text = "原始地址：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(24)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblOriginal);
		NeonTextBox txtOriginal = new NeonTextBox
		{
			Text = originalUrl,
			ReadOnly = true,
			Location = At(bodyX, bodyTop + SY(44)),
			Size = new Size(bodyW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.SuppressGlow ? pal.Border : NeonHelper.WithAlpha(pal.Neon, 50),
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.Muted,
			GlowEnabled = !pal.SuppressGlow
		};
		txtOriginal.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtOriginal);
		// 新 IP/域名
		Label lblHost = new Label
		{
			Text = "新 IP / 域名：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(82)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblHost);
		int hostW = bodyW - SX(152);
		NeonTextBox txtHost = new NeonTextBox
		{
			Text = host,
			Location = At(bodyX, bodyTop + SY(102)),
			Size = new Size(hostW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.Border,
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.InputText,
			GlowEnabled = !pal.SuppressGlow
		};
		txtHost.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtHost);
		// 新端口
		int portX = bodyX + hostW + SX(12);
		int portW = SX(140);
		Label lblPort = new Label
		{
			Text = "新端口：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(portX, bodyTop + SY(82)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblPort);
		NeonTextBox txtPort = new NeonTextBox
		{
			Text = port,
			Location = At(portX, bodyTop + SY(102)),
			Size = new Size(portW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.Border,
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.InputText,
			GlowEnabled = !pal.SuppressGlow
		};
		txtPort.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtPort);
		// 预览
		Label lblPreview = new Label
		{
			Text = "预览：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(140)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblPreview);
		NeonPreviewBox previewBox = new NeonPreviewBox
		{
			Location = At(bodyX, bodyTop + SY(160)),
			Size = new Size(bodyW, SY(34)),
			BackColorX = pal.SuppressGlow ? pal.PanelBg : NeonHelper.WithAlpha(pal.Neon, pal.IsDark ? 20 : 40),
			BorderColor = pal.SuppressGlow ? pal.Border : NeonHelper.WithAlpha(pal.Neon, 110),
			MutedColor = pal.Muted,
			NeonColor = pal.Neon,
			GlowColor = pal.Glow
		};
		previewBox.Font = GetFont(SF(7.5f));
		ctx.Body.Controls.Add(previewBox);
		txtHost.TextChanged += delegate
		{
			AutoParseIpPort(txtHost.Text);
			UpdatePreview();
		};
		txtPort.TextChanged += delegate
		{
			UpdatePreview();
		};
		UpdatePreview();
		// 按钮
		int btnY = panelY + panelH - SY(48);
		int btnW = SX(95);
		int btnH = SY(34);
		NeonButton btnOK = new NeonButton
		{
			Text = "确定替换",
			BackColor = Color.Transparent,
			IsPrimary = true,
			GradientStart = pal.Neon,
			GradientEnd = pal.Neon2,
			TextColorX = pal.PrimaryText,
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			Location = At(bodyX + bodyW - btnW, btnY),
			Size = new Size(btnW, btnH),
			DialogResult = DialogResult.OK
		};
		btnOK.Font = GetFont(SF(8f));
		ctx.Body.Controls.Add(btnOK);
		NeonButton btnCancel = new NeonButton
		{
			Text = "取消",
			BackColor = Color.Transparent,
			IsPrimary = false,
			BorderColor = NeonHelper.WithAlpha(pal.Neon, 120),
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			TextColorX = pal.GhostText,
			Location = At(bodyX + bodyW - btnW * 2 - SX(12), btnY),
			Size = new Size(btnW, btnH),
			DialogResult = DialogResult.Cancel
		};
		btnCancel.Font = GetFont(SF(8f));
		ctx.Body.Controls.Add(btnCancel);
		dlg.AcceptButton = btnOK;
		dlg.CancelButton = btnCancel;
		if (dlg.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		string newHost = txtHost.Text.Trim();
		string newPort = txtPort.Text.Trim();
		if (string.IsNullOrEmpty(newHost))
		{
			DarkMessageBox.Show("IP/域名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		string newUrl = protocol + "://" + newHost;
		if (!string.IsNullOrEmpty(newPort))
		{
			newUrl = newUrl + ":" + newPort;
		}
		newUrl += path;
		ChannelInfo ch = allChannels.FirstOrDefault((ChannelInfo c) => c.Url == originalUrl);
		if (ch != null)
		{
			ch.Url = newUrl;
			ch.Status = "未检测";
			ch.Speed = "";
			RefreshGrid();
			DarkMessageBox.Show("直播源地址已替换成功！\n\n新地址：\n" + newUrl, "替换成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		void AutoParseIpPort(string input)
		{
			input = input.Trim();
			Match match = Regex.Match(input, "^([^:]+):(\\d+)$");
			if (match.Success)
			{
				txtHost.Text = match.Groups[1].Value.Trim();
				txtPort.Text = match.Groups[2].Value.Trim();
				UpdatePreview();
			}
		}
		void UpdatePreview()
		{
			string nh = txtHost.Text.Trim();
			string np = txtPort.Text.Trim();
			previewBox.Protocol = protocol;
			previewBox.Host = nh;
			previewBox.Port = np;
			previewBox.Path = path;
			previewBox.Invalidate();
		}
	}

	private void ReplaceAllUrls()
	{
		if (allChannels.Count == 0)
		{
			DarkMessageBox.Show("没有可修复的直播源！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		ChannelInfo firstCh = allChannels[0];
		var (protocol, host, port, path) = ParseUrl(firstCh.Url);
		if (string.IsNullOrEmpty(protocol))
		{
			DarkMessageBox.Show("无法解析直播源格式！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		// 方案 E · 霓虹暗夜：调色板从当前主题派生，暗色=霓虹青/品红，亮色=主题强调色（严格适配所有主题、字体与高对比度）
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		using Form dlg = new Form();
		dlg.Text = "一键替换所有直播源的IP+端口";
		dlg.StartPosition = FormStartPosition.Manual;
		dlg.FormBorderStyle = FormBorderStyle.None;
		dlg.MaximizeBox = false;
		dlg.MinimizeBox = false;
		dlg.ShowInTaskbar = false;
		dlg.Font = GetFont(SF(10f));
		dlg.Width = SX(480);
		dlg.Height = SY(392);
		CenterForm(dlg, this);
		dlg.BackColor = pal.FormBg;
		int M = SX(16);
		int outerR = SX(14);
		int panelX = M;
		int panelY = M;
		int panelW = dlg.Width - 2 * M;
		int panelH = dlg.Height - 2 * M;
		int titleH = SY(36);
		int bodyX = panelX + SX(20);
		int bodyW = panelW - 2 * SX(20);
		int bodyTop = panelY + titleH + SY(14);
		dlg.Region = new Region(NeonHelper.RoundedRectPath(new Rectangle(0, 0, dlg.Width, dlg.Height), outerR));
		dlg.Paint += delegate (object s, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle panel = new Rectangle(panelX, panelY, panelW, panelH);
			using (GraphicsPath path = NeonHelper.RoundedRectPath(panel, outerR))
			{
				if (!pal.SuppressGlow)
				{
					for (int i = 6; i >= 1; i--)
					{
						using (Pen p = new Pen(Color.FromArgb(50 / i, pal.Glow.R, pal.Glow.G, pal.Glow.B), i * 2f))
						{
							g.DrawPath(p, path);
						}
					}
				}
				using (Brush b = new SolidBrush(pal.PanelBg))
				{
					g.FillPath(b, path);
				}
				using (Pen p = new Pen(NeonHelper.WithAlpha(pal.Neon, 120), 1.5f))
				{
					g.DrawPath(p, path);
				}
			}
		};
		Panel bodyPanel = new Panel
		{
			Location = new Point(0, 0),
			Size = dlg.ClientSize,
			BackColor = pal.FormBg
		};
		dlg.Controls.Add(bodyPanel);
		var ctx = new NeonChrome.Context { Body = bodyPanel, Margin = M, TitleHeight = titleH, Palette = pal };
		Point At(int x, int yy) => new Point(x, yy);
		// 自定义标题栏（顶部圆角 + 底部霓虹分隔线）
		Panel titlePanel = new Panel
		{
			Location = At(panelX, panelY),
			Size = new Size(panelW, titleH),
			BackColor = pal.TitleBg
		};
		{
			int d = outerR * 2;
			GraphicsPath tp = new GraphicsPath();
			tp.AddArc(0, 0, d, d, 180f, 90f);
			tp.AddArc(panelW - d, 0, d, d, 270f, 90f);
			tp.AddLine(panelW, outerR, panelW, titleH);
			tp.AddLine(panelW, titleH, 0, titleH);
			tp.AddLine(0, titleH, 0, outerR);
			tp.CloseFigure();
			titlePanel.Region = new Region(tp);
		}
		titlePanel.Paint += delegate (object s, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle r = titlePanel.ClientRectangle;
			using (GraphicsPath path = NeonHelper.RoundedRectPath(new Rectangle(0, 0, r.Width, r.Height + outerR), outerR))
			{
				using (Brush b = new SolidBrush(pal.TitleBg))
				{
					g.FillPath(b, path);
				}
			}
			using (Pen p = new Pen(NeonHelper.WithAlpha(pal.Neon, 60), 1f))
			{
				g.DrawLine(p, 0, r.Height - 1, r.Width, r.Height - 1);
			}
		};
		ctx.Body.Controls.Add(titlePanel);
		Label lblDot1 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(255, 95, 87),
			Location = At(SX(14), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblDot2 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(254, 188, 46),
			Location = At(SX(30), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblDot3 = new Label
		{
			Text = "●",
			Font = GetFont(SF(7f)),
			ForeColor = Color.FromArgb(40, 200, 64),
			Location = At(SX(46), SY(9)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblHead = new Label
		{
			Text = "一键替换所有直播源的IP+端口",
			Font = GetFont(SF(10f), FontStyle.Bold),
			ForeColor = pal.GhostText,
			Location = At(SX(64), SY(8)),
			BackColor = pal.TitleBg,
			AutoSize = true
		};
		Label lblX = new Label
		{
			Text = "✕",
			Font = GetFont(SF(10f)),
			ForeColor = pal.GhostText,
			Location = At(panelW - SX(26), SY(8)),
			BackColor = pal.TitleBg,
			AutoSize = true,
			Cursor = Cursors.Hand
		};
		titlePanel.Controls.Add(lblDot1);
		titlePanel.Controls.Add(lblDot2);
		titlePanel.Controls.Add(lblDot3);
		titlePanel.Controls.Add(lblHead);
		titlePanel.Controls.Add(lblX);
		// 标题栏拖动
		Point dragOffset = Point.Empty;
		bool dragging = false;
		titlePanel.MouseDown += delegate (object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				dragging = true;
				dragOffset = e.Location;
			}
		};
		titlePanel.MouseMove += delegate (object s, MouseEventArgs e)
		{
			if (dragging)
			{
				dlg.Location = At(dlg.Left + e.X - dragOffset.X, dlg.Top + e.Y - dragOffset.Y);
			}
		};
		titlePanel.MouseUp += delegate (object s, MouseEventArgs e)
		{
			dragging = false;
		};
		lblHead.MouseDown += delegate (object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				dragging = true;
				dragOffset = e.Location;
			}
		};
		lblX.MouseEnter += delegate (object s, EventArgs e)
		{
			lblX.ForeColor = pal.Neon;
		};
		lblX.MouseLeave += delegate (object s, EventArgs e)
		{
			lblX.ForeColor = pal.GhostText;
		};
		lblX.Click += delegate (object s, EventArgs e)
		{
			dlg.DialogResult = DialogResult.Cancel;
			dlg.Close();
		};
		// 提示（随主题派生的警告色，严格适配所有主题）
		Label lblTip = new Label
		{
			Text = $"共 {allChannels.Count} 条直播源，将替换所有链接的IP和端口\n（保留路径部分不变）",
			Font = GetFont(SF(7.5f)),
			ForeColor = theme.WarnColor,
			Location = At(bodyX, bodyTop),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblTip);
		// 原始地址（只读，暗色用 muted 色）
		Label lblOriginal = new Label
		{
			Text = "原始地址（示例）：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(40)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblOriginal);
		NeonTextBox txtOriginal = new NeonTextBox
		{
			Text = firstCh.Url,
			ReadOnly = true,
			Location = At(bodyX, bodyTop + SY(60)),
			Size = new Size(bodyW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.SuppressGlow ? pal.Border : NeonHelper.WithAlpha(pal.Neon, 50),
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.Muted,
			GlowEnabled = !pal.SuppressGlow
		};
		txtOriginal.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtOriginal);
		// 新 IP/域名
		Label lblHost = new Label
		{
			Text = "新 IP / 域名：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(98)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblHost);
		int hostW = bodyW - SX(152);
		NeonTextBox txtHost = new NeonTextBox
		{
			Text = host,
			Location = At(bodyX, bodyTop + SY(118)),
			Size = new Size(hostW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.Border,
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.InputText,
			GlowEnabled = !pal.SuppressGlow
		};
		txtHost.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtHost);
		// 新端口
		int portX = bodyX + hostW + SX(12);
		int portW = SX(140);
		Label lblPort = new Label
		{
			Text = "新端口：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(portX, bodyTop + SY(98)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblPort);
		NeonTextBox txtPort = new NeonTextBox
		{
			Text = port,
			Location = At(portX, bodyTop + SY(118)),
			Size = new Size(portW, SY(28)),
			BackColorX = pal.PanelBg,
			BorderColor = pal.Border,
			FocusColor = pal.FocusBorder,
			GlowColor = pal.Glow,
			TextColor = pal.InputText,
			GlowEnabled = !pal.SuppressGlow
		};
		txtPort.Font = GetFont(SF(7f));
		ctx.Body.Controls.Add(txtPort);
		// 预览（host:port 段霓虹高亮）
		Label lblPreview = new Label
		{
			Text = "替换后预览（所有链接）：",
			Font = GetFont(SF(7.5f)),
			ForeColor = pal.Label,
			Location = At(bodyX, bodyTop + SY(156)),
			BackColor = pal.PanelBg,
			AutoSize = true
		};
		ctx.Body.Controls.Add(lblPreview);
		NeonPreviewBox previewBox = new NeonPreviewBox
		{
			Location = At(bodyX, bodyTop + SY(176)),
			Size = new Size(bodyW, SY(34)),
			BackColorX = pal.SuppressGlow ? pal.PanelBg : NeonHelper.WithAlpha(pal.Neon, pal.IsDark ? 20 : 40),
			BorderColor = pal.SuppressGlow ? pal.Border : NeonHelper.WithAlpha(pal.Neon, 110),
			MutedColor = pal.Muted,
			NeonColor = pal.Neon,
			GlowColor = pal.Glow
		};
		previewBox.Font = GetFont(SF(7.5f));
		ctx.Body.Controls.Add(previewBox);
		txtHost.TextChanged += delegate
		{
			AutoParseIpPort(txtHost.Text);
			UpdatePreview();
		};
		txtPort.TextChanged += delegate
		{
			UpdatePreview();
		};
		UpdatePreview();
		// 按钮
		int btnY = panelY + panelH - SY(48);
		int btnW = SX(95);
		int btnH = SY(34);
		NeonButton btnOK = new NeonButton
		{
			Text = "确定替换",
			BackColor = Color.Transparent,
			IsPrimary = true,
			GradientStart = pal.Neon,
			GradientEnd = pal.Neon2,
			TextColorX = pal.PrimaryText,
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			Location = At(bodyX + bodyW - btnW, btnY),
			Size = new Size(btnW, btnH),
			DialogResult = DialogResult.OK
		};
		btnOK.Font = GetFont(SF(8f));
		ctx.Body.Controls.Add(btnOK);
		NeonButton btnCancel = new NeonButton
		{
			Text = "取消",
			BackColor = Color.Transparent,
			IsPrimary = false,
			BorderColor = NeonHelper.WithAlpha(pal.Neon, 120),
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			TextColorX = pal.GhostText,
			Location = At(bodyX + bodyW - btnW * 2 - SX(12), btnY),
			Size = new Size(btnW, btnH),
			DialogResult = DialogResult.Cancel
		};
		btnCancel.Font = GetFont(SF(8f));
		ctx.Body.Controls.Add(btnCancel);
		dlg.AcceptButton = btnOK;
		dlg.CancelButton = btnCancel;
		if (dlg.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		string newHost = txtHost.Text.Trim();
		string newPort = txtPort.Text.Trim();
		if (string.IsNullOrEmpty(newHost))
		{
			DarkMessageBox.Show("IP/域名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		// 执行批量替换（保留协议与路径，替换 host:port）
		int replaced = 0;
		int failed = 0;
		foreach (ChannelInfo ch in allChannels)
		{
			var (p, _, _, path2) = ParseUrl(ch.Url);
			if (string.IsNullOrEmpty(p))
			{
				failed++;
				continue;
			}
			string newUrl = p + "://" + newHost;
			if (!string.IsNullOrEmpty(newPort))
			{
				newUrl = newUrl + ":" + newPort;
			}
			newUrl += path2;
			ch.Url = newUrl;
			ch.Status = "未检测";
			ch.Speed = "";
			replaced++;
		}
		RefreshGrid();
		string msg = $"批量替换完成！\n成功替换: {replaced} 条";
		if (failed > 0)
		{
			msg += $"\n跳过(格式不支持): {failed} 条";
		}
		DarkMessageBox.Show(msg, "替换完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		void AutoParseIpPort(string input)
		{
			input = input.Trim();
			Match match = Regex.Match(input, "^([^:]+):(\\d+)$");
			if (match.Success)
			{
				txtHost.Text = match.Groups[1].Value.Trim();
				txtPort.Text = match.Groups[2].Value.Trim();
				UpdatePreview();
			}
		}
		void UpdatePreview()
		{
			string nh = txtHost.Text.Trim();
			string np = txtPort.Text.Trim();
			previewBox.Protocol = protocol;
			previewBox.Host = nh;
			previewBox.Port = np;
			previewBox.Path = path;
			previewBox.Invalidate();
		}
	}

	private void BeginRenameSelected()
	{
		if (dgvData.SelectedRows.Count > 0)
		{
			int idx = dgvData.SelectedRows[0].Index;
			dgvData.CurrentCell = dgvData.Rows[idx].Cells[0];
			dgvData.BeginEdit(selectAll: true);
		}
		else
		{
			DarkMessageBox.Show("请先选择一行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private int ParseSpeed(string speed)
	{
		if (string.IsNullOrWhiteSpace(speed))
		{
			return int.MaxValue;
		}
		if (speed == "超时")
		{
			return 2147483646;
		}
		if (int.TryParse(new string(speed.TakeWhile((char c) => char.IsDigit(c)).ToArray()), out var ms))
		{
			return ms;
		}
		return int.MaxValue;
	}

	private void ShowPlayMenu(string url)
	{
		ContextMenuStrip playMenu = new ContextMenuStrip();
		playMenu.Font = GetFont(SF(9f));
		AnimatedMenuRenderer playMenuRenderer = new AnimatedMenuRenderer(theme);
		playMenu.Renderer = playMenuRenderer;
		playMenuRenderer.Register(playMenu);
		playMenu.BackColor = theme.Surface;
		playMenu.ForeColor = theme.TextPrimary;
		playMenu.Items.Add("系统默认播放器", null, delegate
		{
			PlayChannelDefault(url);
		});
		bool hasFFplay = !string.IsNullOrWhiteSpace(ffplayPath) && File.Exists(ffplayPath);
		ToolStripMenuItem ffplayItem = new ToolStripMenuItem(hasFFplay ? ("FFplay播放 (" + ffplayPath + ")") : "FFplay播放(未找到ffplay)");
		ffplayItem.Enabled = hasFFplay;
		ffplayItem.Click += delegate
		{
			PlayChannelFFplay(url);
		};
		playMenu.Items.Add(ffplayItem);
		bool hasCustom = !string.IsNullOrWhiteSpace(customPlayerPath) && File.Exists(customPlayerPath);
		ToolStripMenuItem customItem = new ToolStripMenuItem(hasCustom ? ("第三方播放器 (" + Path.GetFileName(customPlayerPath) + ")") : "第三方播放器(未设置)");
		customItem.Enabled = hasCustom;
		customItem.Click += delegate
		{
			PlayChannelCustom(url);
		};
		playMenu.Items.Add(customItem);
		playMenu.Items.Add(new ToolStripSeparator());
		playMenu.Items.Add("设置第三方播放器路径...", null, delegate
		{
			SetCustomPlayerPath();
		});
		if (hasFFplay)
		{
			playMenu.Items.Add(new ToolStripSeparator());
			ToolStripMenuItem autoItem = new ToolStripMenuItem("自动(优先FFplay)");
			autoItem.Click += delegate
			{
				try
				{
					PlayChannelFFplay(url);
				}
				catch
				{
					PlayChannelDefault(url);
				}
			};
			playMenu.Items.Add(autoItem);
		}
		Point p = dgvData.PointToClient(Cursor.Position);
		playMenu.Show(dgvData, p);
	}

	private void PlayChannelDefault(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("无法使用系统播放器打开链接：\n" + ex.Message, "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void KillRunningPlayer()
	{
		try
		{
			_ffplayOutputCts?.Cancel();
			Thread.Sleep(100);
		}
		catch
		{
		}
		try
		{
			if (_runningPlayer != null && !_runningPlayer.HasExited)
			{
				_runningPlayer.Kill();
				_runningPlayer.WaitForExit(2000);
			}
		}
		catch
		{
		}
		finally
		{
			try
			{
				_runningPlayer?.Dispose();
			}
			catch
			{
			}
			_runningPlayer = null;
			try
			{
				_ffplayOutputCts?.Dispose();
			}
			catch
			{
			}
			_ffplayOutputCts = null;
			if (lblStreamInfo != null)
			{
				lblStreamInfo.Visible = false;
			}
			StopStreamInfoOverlay();
			_showStreamInfoOverlay = false;
		}
	}

	private void StartPreview(string url)
	{
		if (string.IsNullOrWhiteSpace(ffplayPath) || !File.Exists(ffplayPath))
		{
			DarkMessageBox.Show("未找到 ffplay.exe，无法预览。请确保 FFmpeg 组件已安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		StopPreview();
		try
		{
			int playWidth = 800;
			int playHeight = 600;
			string ffplayArgs = BuildFfplayArguments(url);
			ffplayArgs = ffplayArgs.Replace("\"" + url + "\"", "\"" + url + "\"") + $" -x {playWidth} -y {playHeight}";
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = ffplayPath,
				Arguments = ffplayArgs,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			previewProcess = new Process
			{
				StartInfo = psi,
				EnableRaisingEvents = true
			};
			previewProcess.Exited += delegate
			{
				try
				{
					BeginInvoke((Action)delegate
					{
						embeddedPreviewHwnd = IntPtr.Zero;
						previewProcess = null;
					});
				}
				catch
				{
				}
			};
			previewProcess.Start();
		StartMouseHook();
		_ = ReadFfplayOutputAsync(previewProcess);
			Task.Run(delegate
			{
				int num = 0;
				while (num < 20)
				{
					if (previewProcess == null || previewProcess.HasExited)
					{
						break;
					}
					IntPtr targetHwnd = IntPtr.Zero;
					uint targetPid = (uint)previewProcess.Id;
					EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
					{
						if (!IsWindowVisible(hWnd))
						{
							return true;
						}
						GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
						if (lpdwProcessId != targetPid)
						{
							return true;
						}
						StringBuilder stringBuilder = new StringBuilder(256);
						GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
						string text = stringBuilder.ToString().ToLower();
						if (text.Contains("sdl") || text.Contains("ffplay"))
						{
							targetHwnd = hWnd;
							return false;
						}
						return true;
					}, IntPtr.Zero);
					if (targetHwnd != IntPtr.Zero)
					{
						BeginInvoke((Action)delegate
						{
							int num2 = (int)SafeGetWindowLongPtr(targetHwnd, -16);
							num2 |= 0xCF0000;
							SafeSetWindowLongPtr(targetHwnd, -16, (IntPtr)num2);
							int num3 = (int)SafeGetWindowLongPtr(targetHwnd, -20);
							num3 |= 0x40008;
							SafeSetWindowLongPtr(targetHwnd, -20, (IntPtr)num3);
							SetWindowPos(targetHwnd, (IntPtr)(-1), 0, 0, playWidth, playHeight, 36u);
							embeddedPreviewHwnd = targetHwnd;
							ShowWindow(targetHwnd, 3);
						});
						break;
					}
					num++;
					Thread.Sleep(250);
				}
			});
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("预览失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
		catch
		{
		}
		finally
		{
			try
			{
				previewProcess?.Dispose();
			}
			catch
			{
			}
			previewProcess = null;
			embeddedPreviewHwnd = IntPtr.Zero;
			try
			{
				previewResizeTimer?.Dispose();
			}
			catch
			{
			}
			previewResizeTimer = null;
			try
			{
				_ffplayOutputCts?.Cancel();
			}
			catch
			{
			}
			try
			{
				_ffplayOutputCts?.Dispose();
			}
			catch
			{
			}
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
				DataGridViewRow row = dgvData.SelectedRows[0];
				_currentChannelName = row.Cells[0].Value?.ToString() ?? "";
			}
			if (string.IsNullOrWhiteSpace(_currentResolution) && dgvData.SelectedRows.Count > 0)
			{
				string backupResolution = dgvData.SelectedRows[0].Cells[3].Value?.ToString() ?? "";
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
				using OpenFileDialog ofd = new OpenFileDialog();
				ofd.Filter = "播放器程序|*.exe|所有文件|*.*";
				ofd.Title = "未找到FFplay，请选择播放器exe文件（ffplay.exe/vlc.exe/potplayer/mpv.exe等）";
				if (ofd.ShowDialog() != DialogResult.OK)
				{
					PlayChannelDefault(url);
					return;
				}
				string selected = ofd.FileName;
				if (Path.GetFileName(selected).ToLower() == "ffplay.exe")
				{
					ffplayPath = selected;
				}
				else
				{
					customPlayerPath = selected;
				}
				playerPath = selected;
			}
			bool isFfplay = Path.GetFileName(playerPath).ToLower() == "ffplay.exe";
			_runningPlayer = new Process();
			string ffplayArgs = BuildFfplayArguments(url);
			_runningPlayer.StartInfo = new ProcessStartInfo
			{
				FileName = playerPath,
				Arguments = (isFfplay ? ffplayArgs : ("\"" + url + "\"")),
				UseShellExecute = false,
				CreateNoWindow = !isFfplay,
				WindowStyle = ((!isFfplay) ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal),
				RedirectStandardOutput = isFfplay,
				RedirectStandardError = isFfplay
			};
			_runningPlayer.EnableRaisingEvents = true;
			_runningPlayer.Exited += delegate(object s, EventArgs e)
			{
				try
				{
					if (_runningPlayer == s)
					{
						_runningPlayer = null;
					}
					((Process)s)?.Dispose();
				}
				catch
				{
				}
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
			try
			{
				_runningPlayer = null;
				PlayChannelDefault(url);
			}
			catch
			{
			}
		}
	}

	private string BuildFfplayArguments(string url)
	{
		StringBuilder args = new StringBuilder();
		args.Append("-autoexit ");
		args.Append("-stats ");
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
		args.Append("\"" + url + "\"");
		return args.ToString();
	}

	private async Task ReadFfplayOutputAsync(Process proc)
	{
		if (proc == null || !proc.StartInfo.RedirectStandardError)
		{
			return;
		}
		StreamReader reader;
		try
		{
			reader = proc.StandardError;
		}
		catch
		{
			return;
		}
		if (reader == null)
		{
			return;
		}
		_ffplayOutputCts = new CancellationTokenSource();
		CancellationToken token = _ffplayOutputCts.Token;
		try
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					string line = await reader.ReadLineAsync();
					if (line == null)
					{
						break;
					}
					if (!string.IsNullOrWhiteSpace(line) && line.Length > 5 && (line.Contains("Video:") || line.Contains("Audio:") || line.Contains("frame=") || line.Contains("fps=") || line.Contains("time=") || line.Contains("bitrate=") || line.Contains("speed=") || line.Contains("KB queue:") || line.Contains("dropped") || line.Contains("size=")))
					{
						ParseFfplayStats(line);
					}
				}
				catch
				{
					break;
				}
			}
		}
		catch
		{
		}
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
			if (line.IndexOf("Video:", StringComparison.Ordinal) >= 0)
			{
				Match match = Regex.Match(line, "Video:\\s*([^\\s,]+)");
				if (match.Success)
				{
					codec = match.Groups[1].Value.ToUpper();
				}
				string videoPart = line;
				int videoIdx = videoPart.IndexOf("Video:", StringComparison.Ordinal);
				if (videoIdx >= 0)
				{
					videoPart = videoPart.Substring(videoIdx);
				}
				videoPart = Regex.Replace(videoPart, "\\[\\d+x\\d+\\]", "");
				match = Regex.Match(videoPart, "(\\d{2,5})x(\\d{2,5})");
				if (match.Success)
				{
					resolution = match.Groups[1].Value + "x" + match.Groups[2].Value;
				}
				match = Regex.Match(line, "(\\d+(?:\\.\\d+)?)\\s*fps");
				if (match.Success)
				{
					fps = match.Groups[1].Value + " FPS";
				}
				match = Regex.Match(line, "delay\\s*=\\s*([\\d.]+)");
				if (match.Success)
				{
					delay = match.Groups[1].Value + "s";
				}
				match = Regex.Match(line, "SAR\\s*(\\d+:\\d+)");
				if (match.Success)
				{
					_currentSar = match.Groups[1].Value;
				}
				match = Regex.Match(line, "DAR\\s*(\\d+:\\d+)");
				if (match.Success)
				{
					_currentDar = match.Groups[1].Value;
				}
			}
			if (line.Contains("Audio:"))
			{
				Match match2 = Regex.Match(line, "Audio:\\s*([^\\s,]+)");
				if (match2.Success && !string.IsNullOrEmpty(codec))
				{
					codec = codec + " + " + match2.Groups[1].Value.ToUpper();
				}
				else if (match2.Success)
				{
					codec = match2.Groups[1].Value.ToUpper();
				}
				match2 = Regex.Match(line, "(\\d+)\\s*Hz");
				if (match2.Success)
				{
					audioSampleRate = match2.Groups[1].Value + " Hz";
				}
				match2 = Regex.Match(line, "(mono|stereo|surround|5\\.1)");
				if (match2.Success)
				{
					audioChannels = match2.Groups[1].Value;
				}
				if (string.IsNullOrEmpty(audioChannels))
				{
					match2 = Regex.Match(line, "(\\d+)\\s*channels?");
					if (match2.Success)
					{
						audioChannels = match2.Groups[1].Value + "声道";
					}
				}
				match2 = Regex.Match(line, "(\\d+)\\s*bps");
				if (match2.Success)
				{
					_currentAudioBitdepth = match2.Groups[1].Value + " bps";
				}
			}
			if (line.Contains("frame="))
			{
				Match match3 = Regex.Match(line, "frame=\\s*(\\d+)");
				if (match3.Success)
				{
					frameCount = match3.Groups[1].Value;
				}
				match3 = Regex.Match(line, "fps=\\s*([\\d.]+)");
				if (match3.Success)
				{
					fps = match3.Groups[1].Value + " FPS";
				}
				match3 = Regex.Match(line, "time=\\s*([\\d:.]+)");
				if (match3.Success)
				{
					currentTime = match3.Groups[1].Value;
				}
				match3 = Regex.Match(line, "bitrate=\\s*([\\d.]+)");
				if (match3.Success)
				{
					bitrate = match3.Groups[1].Value + " kb/s";
				}
				match3 = Regex.Match(line, "speed=\\s*([\\d.]+)x?");
				if (match3.Success)
				{
					speed = match3.Groups[1].Value + "x";
				}
				match3 = Regex.Match(line, "size=\\s*(\\d+)");
				if (match3.Success)
				{
					_currentSize = match3.Groups[1].Value + " bytes";
				}
				match3 = Regex.Match(line, "decoded=\\s*(\\d+)");
				if (match3.Success)
				{
					_currentDecodedFrames = "已解码: " + match3.Groups[1].Value;
				}
				match3 = Regex.Match(line, "displayed=\\s*(\\d+)");
				if (match3.Success)
				{
					_currentDisplayedFrames = "已显示: " + match3.Groups[1].Value;
				}
			}
			if (line.Contains("KB queue:"))
			{
				Match match4 = Regex.Match(line, "KB queue:\\s*(\\d+)");
				if (match4.Success)
				{
					buffer = match4.Groups[1].Value + " KB";
				}
			}
			if (line.Contains("dropped"))
			{
				Match match5 = Regex.Match(line, "dropped\\s*=\\s*(\\d+)");
				if (match5.Success)
				{
					int.TryParse(match5.Groups[1].Value, out _droppedFrames);
				}
				match5 = Regex.Match(line, "total\\s*=\\s*(\\d+)");
				if (match5.Success)
				{
					int.TryParse(match5.Groups[1].Value, out _totalFrames);
				}
			}
			UpdateStreamInfoDisplay(codec, resolution, fps, bitrate, audioChannels, audioSampleRate, delay, frameCount, currentTime, speed, buffer);
		}
		catch
		{
		}
	}

	private void UpdateStreamInfoDisplay(string codec, string resolution, string fps, string bitrate, string audioChannels = "", string audioSampleRate = "", string delay = "", string frameCount = "", string currentTime = "", string speed = "", string buffer = "")
	{
		if (!string.IsNullOrEmpty(codec))
		{
			_currentCodec = codec;
		}
		if (!string.IsNullOrEmpty(resolution))
		{
			_currentResolution = resolution;
		}
		if (!string.IsNullOrEmpty(fps))
		{
			_currentFps = fps;
		}
		if (!string.IsNullOrEmpty(bitrate))
		{
			_currentBitrate = bitrate;
		}
		if (!string.IsNullOrEmpty(audioChannels))
		{
			_currentAudioChannels = audioChannels;
		}
		if (!string.IsNullOrEmpty(audioSampleRate))
		{
			_currentAudioSampleRate = audioSampleRate;
		}
		if (!string.IsNullOrEmpty(delay))
		{
			_currentDelay = delay;
		}
		if (!string.IsNullOrEmpty(frameCount))
		{
			_currentFrameCount = frameCount;
		}
		if (!string.IsNullOrEmpty(currentTime))
		{
			_currentTime = currentTime;
		}
		if (!string.IsNullOrEmpty(speed))
		{
			_currentSpeed = speed;
		}
		if (!string.IsNullOrEmpty(buffer))
		{
			_currentBuffer = buffer;
		}
		if (lblStreamInfo != null)
		{
			lblStreamInfo.Visible = false;
		}
		if (_showStreamInfoOverlay && _streamInfoLabel != null && !_streamInfoLabel.IsDisposed)
		{
			UpdateStreamInfoOverlay();
		}
	}

	private void PlayChannelCustom(string url)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(customPlayerPath) || !File.Exists(customPlayerPath))
			{
				DarkMessageBox.Show("未设置第三方播放器路径或文件不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				SetCustomPlayerPath();
				return;
			}
			Process.Start(new ProcessStartInfo
			{
				FileName = customPlayerPath,
				Arguments = "\"" + url + "\"",
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("第三方播放器播放失败：\n" + ex.Message, "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void DgvData_Paint(object sender, PaintEventArgs e)
	{
		if (dgvData == null)
		{
			return;
		}
		int displayRight = dgvData.DisplayRectangle.Right;
		int clientRight = dgvData.ClientSize.Width;
		if (displayRight >= clientRight)
		{
			return;
		}
		using SolidBrush bgBrush = new SolidBrush(theme.BgAlt);
		e.Graphics.FillRectangle(bgBrush, displayRight, 0, clientRight - displayRight, dgvData.ClientSize.Height);
	}

	private void UpdateGridScrollBar()
	{
		if (dgvData == null || dgvData.IsDisposed || darkVScrollBar == null || darkVScrollBar.IsDisposed || !dgvData.IsHandleCreated)
		{
			return;
		}
		int rowCount = dgvData.RowCount;
		if (rowCount <= 0)
		{
			darkVScrollBar.Visible = false;
			return;
		}
		int visibleRows = GetGridVisibleRowCount();
		if (visibleRows <= 0)
		{
			visibleRows = 1;
		}
		if (visibleRows >= rowCount)
		{
			darkVScrollBar.Visible = false;
			return;
		}
		darkVScrollBar.Visible = true;
		darkVScrollBar.Minimum = 0;
		darkVScrollBar.Maximum = rowCount - visibleRows;
		darkVScrollBar.LargeChange = visibleRows;
		darkVScrollBar.SmallChange = 1;
		int idx = dgvData.FirstDisplayedScrollingRowIndex;
		if (idx < 0)
		{
			idx = 0;
		}
		if (idx > darkVScrollBar.Maximum)
		{
			idx = darkVScrollBar.Maximum;
		}
		if (idx != darkVScrollBar.Value)
		{
			darkVScrollBar.Value = idx;
		}
	}

	private void SyncGridScrollBar()
	{
		if (dgvData != null && darkVScrollBar != null && darkVScrollBar.Visible)
		{
			int idx = dgvData.FirstDisplayedScrollingRowIndex;
			if (idx < 0)
			{
				idx = 0;
			}
			if (idx > darkVScrollBar.Maximum)
			{
				idx = darkVScrollBar.Maximum;
			}
			if (idx != darkVScrollBar.Value)
			{
				darkVScrollBar.Value = idx;
			}
		}
	}

	private int GetGridVisibleRowCount()
	{
		if (dgvData == null || !dgvData.IsHandleCreated || dgvData.RowCount == 0)
		{
			return 0;
		}
		try
		{
			int count = dgvData.DisplayedRowCount(includePartialRow: false);
			if (count > 0)
			{
				return count;
			}
		}
		catch
		{
		}
		int rowHeight = dgvData.RowTemplate.Height;
		if (rowHeight <= 0)
		{
			rowHeight = 36;
		}
		int displayH = dgvData.DisplayRectangle.Height - dgvData.ColumnHeadersHeight;
		return Math.Max(1, displayH / rowHeight);
	}

	private void ShowScanSourceDialog()
	{
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		Color GreenMain = (isDark ? Color.FromArgb(70, 200, 110) : theme.PlayBtnBg);
		Color GreenDark = (isDark ? Color.FromArgb(55, 180, 95) : theme.PlayBtnBg);
		Color GrayText = theme.TextSecondary;
		Color GrayLine = theme.Border;
		Color GrayBorder = theme.Border;
		Color DarkText = theme.TextPrimary;
		Color RedHighlight = theme.ErrorColor;
		Color LightBtnBg = theme.Surface;
		Color InputBg = theme.Bg;
		Color InputFocusBorder = GreenMain;
		Color PanelBg = theme.Surface;
		Color StepLineGray = isDark ? ControlPaint.Light(theme.Border, 0.2f) : ControlPaint.Dark(theme.Border, 0.1f);
		Color NumPadHover = isDark ? ControlPaint.Light(theme.Surface, 0.12f) : ControlPaint.Light(theme.Surface, 0.4f);
		Color NumPadDown = isDark ? ControlPaint.Light(theme.Surface, 0.22f) : ControlPaint.Dark(theme.Surface, 0.06f);
		Color CloseHover = isDark ? ControlPaint.Light(theme.Surface, 0.18f) : ControlPaint.Light(theme.Surface, 0.5f);
		Color CloseDown = isDark ? ControlPaint.Light(theme.Surface, 0.3f) : ControlPaint.Dark(theme.Surface, 0.08f);
		int DLG_W = SX(900);
		int DLG_H = SY(620);
		int CONTENT_PAD = SX(32);
		int CONTROL_GAP = SY(12);
		int TOP_PADDING = SY(20);
		int INPUT_HEIGHT = SY(50);
		int BTN_HEIGHT = SY(38);
		int HINT_HEIGHT = SY(68);
		int TITLE_BAR_H = SY(52);
		int STEP_INDICATOR_H = SY(80);
		Font BASE_FONT = GetFont(10f);
		Font TITLE_FONT = GetFont(15f, FontStyle.Bold);
		Font LABEL_FONT = GetFont(12f);
		Font HINT_FONT = GetFont(10.5f);
		Font URL_FONT = new Font("Consolas", SF(10.5f));
		Font URL_BOLD_FONT = new Font("Consolas", SF(11.5f), FontStyle.Bold);
		Font BTN_FONT = GetFont(11f, FontStyle.Bold);
		Font NUMPAD_BTN_FONT = GetFont(14f, FontStyle.Bold);
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
		ApplyWindowChrome(dlg, 12);
		Panel titleBar = new Panel
		{
			Dock = DockStyle.Top,
			Height = TITLE_BAR_H,
			BackColor = PanelBg
		};
		Label lblTitle = new Label
		{
			Text = "\ud83d\udd0d 直播源生成器",
			Font = TITLE_FONT,
			ForeColor = DarkText,
			Location = At(CONTENT_PAD, (TITLE_BAR_H - SY(22)) / 2),
			AutoSize = true
		};
		titleBar.Controls.Add(lblTitle);
		Button btnClose = new Button
		{
			Text = "✕",
			FlatStyle = FlatStyle.Flat,
			Size = new Size(SX(40), TITLE_BAR_H),
			Location = At(DLG_W - SX(40), 0),
			ForeColor = GrayText,
			BackColor = PanelBg,
			Font = GetFont(11f),
			Cursor = Cursors.Hand
		};
		btnClose.FlatAppearance.BorderSize = 0;
		btnClose.FlatAppearance.MouseOverBackColor = CloseHover;
		btnClose.FlatAppearance.MouseDownBackColor = CloseDown;
		btnClose.Click += delegate
		{
			dlg.Close();
		};
		titleBar.Controls.Add(btnClose);
		MakeDraggable(titleBar);
		MakeDraggable(lblTitle);
		dlg.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				dlg.Close();
			}
		};
		Panel sepTitle = new Panel
		{
			Dock = DockStyle.Top,
			Height = SX(1),
			BackColor = GrayLine
		};
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
		long fromVal = 0L;
		long toVal = 0L;
		bool segPadZero = false;
		int segPadWidth = 0;
		bool isCustomRangeMode = false;
		long customRangeStart = 0L;
		long customRangeEnd = 0L;
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
			Height = STEP_INDICATOR_H,
			BackColor = PanelBg
		};
		string[] stepLabelsArr = new string[3] { "输入源地址", "选择字段", "设置范围" };
		int stepCircleR = SX(12);
		stepIndicator.Paint += delegate(object s, PaintEventArgs pe)
		{
			Graphics graphics = pe.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			int num = stepIndicator.ClientSize.Width;
			int num2 = stepIndicator.Height;
			float num3 = stepCircleR * 2;
			float num4 = SY(14);
			float num5 = num4 + (float)stepCircleR;
			float num6 = (float)num / 3f;
			float[] array = new float[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = num6 * (float)i + num6 / 2f;
			}
			float num7 = num6 - (float)SX(40);
			float num8 = 11f;
			Font font = null;
			Font font2 = null;
			SizeF sizeF;
			do
			{
				font?.Dispose();
				font2?.Dispose();
				font = GetFont(num8);
				font2 = GetFont(num8, FontStyle.Bold);
				sizeF = graphics.MeasureString("输入源地址", font2);
				num8 -= 0.5f;
			}
			while (sizeF.Width > num7 && num8 >= 8f);
			float num9 = num8 * 0.9f;
			if (num9 < 7.5f)
			{
				num9 = 7.5f;
			}
			Font font3 = GetFont(num9, FontStyle.Bold);
			Font font4 = GetFont(num9 + 1f, FontStyle.Bold);
			float num10 = (float)num2 - num4 - num3 - sizeF.Height;
			if (num10 < (float)SY(8))
			{
				num10 = SY(8);
			}
			using Pen pen = new Pen(GrayLine, 2f);
			using Pen pen2 = new Pen(GreenMain, 2f);
			using Brush brush = new SolidBrush(GreenMain);
			using Brush brush2 = new SolidBrush(PanelBg);
			using Brush brush3 = new SolidBrush(GrayText);
			using (font3)
			{
				using (font4)
				{
					using (font)
					{
						using (font2)
						{
							using StringFormat format = new StringFormat
							{
								Alignment = StringAlignment.Center,
								LineAlignment = StringAlignment.Center
							};
							for (int j = 0; j < 2; j++)
							{
								Pen pen3 = ((currentStep >= j + 2) ? pen2 : pen);
								float x = array[j] + (float)stepCircleR + (float)SX(10);
								float x2 = array[j + 1] - (float)stepCircleR - (float)SX(10);
								graphics.DrawLine(pen3, x, num5, x2, num5);
							}
							for (int k = 0; k < 3; k++)
							{
								float num11 = array[k];
								RectangleF rectangleF = new RectangleF(num11 - (float)stepCircleR, num4, num3, num3);
								bool flag = k < currentStep - 1;
								bool flag2 = k == currentStep - 1;
								if (flag)
								{
									graphics.FillEllipse(brush, rectangleF);
									graphics.DrawEllipse(pen2, rectangleF);
									RectangleF layoutRectangle = new RectangleF(num11 - (float)stepCircleR, num4 + 1f, num3, num3);
									graphics.DrawString("✓", font4, brush2, layoutRectangle, format);
								}
								else if (flag2)
								{
									graphics.FillEllipse(brush, rectangleF);
									graphics.DrawEllipse(pen2, rectangleF);
									graphics.DrawString((k + 1).ToString(), font3, brush2, rectangleF, format);
								}
								else
								{
									graphics.FillEllipse(brush2, rectangleF);
									using (Pen pen4 = new Pen(StepLineGray, 2f))
									{
										graphics.DrawEllipse(pen4, rectangleF);
									}
									graphics.DrawString((k + 1).ToString(), font3, brush3, rectangleF, format);
								}
								Brush brush4 = ((flag2 || flag) ? new SolidBrush(GreenMain) : brush3);
								Font font5 = (flag2 ? font2 : font);
								float num12 = num11 - graphics.MeasureString(stepLabelsArr[k], font5).Width / 2f;
								float num13 = num4 + num3 + num10;
								graphics.DrawString(stepLabelsArr[k], font5, brush4, num12, num13);
							}
						}
					}
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
			Height = SY(28),
			BackColor = PanelBg
		};
		contentHost.Controls.Add(stepIndicatorTopGap);
		contentHost.Controls.Add(stepIndicator);
		Panel step1Panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = PanelBg
		};
		Panel step2Panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = PanelBg,
			Visible = false
		};
		Panel step3Panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = PanelBg,
			Visible = false
		};
		stepContainer.Controls.Add(step2Panel);
		stepContainer.Controls.Add(step3Panel);
		stepContainer.Controls.Add(step1Panel);
		int step1Top = TOP_PADDING;
		Label lblStep1Hint = new Label
		{
			Text = "直播源地址",
			Font = LABEL_FONT,
			ForeColor = DarkText,
			Location = At(CONTENT_PAD, step1Top),
			AutoSize = true,
			BackColor = PanelBg
		};
		int hint1W = TextRenderer.MeasureText(lblStep1Hint.Text, lblStep1Hint.Font).Width;
		int hint1H = TextRenderer.MeasureText(lblStep1Hint.Text, lblStep1Hint.Font).Height;
		Label lblStep1Star = new Label
		{
			Text = "*",
			Font = LABEL_FONT,
			ForeColor = RedHighlight,
			Location = At(CONTENT_PAD + hint1W + SX(3), step1Top),
			AutoSize = true,
			BackColor = PanelBg
		};
		step1Panel.Controls.Add(lblStep1Hint);
		step1Panel.Controls.Add(lblStep1Star);
		Label lblStep1Sub = new Label
		{
			Text = "请粘贴直播源地址，或输入自定义范围格式（如 {0001-0100}）",
			Font = GetFont(10f),
			ForeColor = (isDark ? Color.FromArgb(150, 155, 165) : Color.FromArgb(120, 123, 130)),
			Location = At(CONTENT_PAD, step1Top + hint1H + SY(2)),
			AutoSize = true,
			BackColor = PanelBg
		};
		int hintSubH = TextRenderer.MeasureText(lblStep1Sub.Text, lblStep1Sub.Font).Height;
		step1Panel.Controls.Add(lblStep1Sub);
		int step1InputTop = step1Top + hint1H + SY(2) + hintSubH + CONTROL_GAP;
		int step1InputH = SY(64);
		TextBox txtStep1Url = new TextBox
		{
			Location = At(CONTENT_PAD, step1InputTop),
			Width = DLG_W - CONTENT_PAD * 2,
			Height = step1InputH,
			Font = GetFont(11.5f),
			BorderStyle = BorderStyle.None,
			BackColor = InputBg,
			Padding = new Padding(SX(8), SX(2), SX(8), SX(2))
		};
		txtStep1Url.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, txtStep1Url.Width, txtStep1Url.Height), SX(6)));
		txtStep1Url.Paint += delegate(object s, PaintEventArgs pe)
		{
			using Pen pen = new Pen(txtStep1Url.Focused ? InputFocusBorder : (isDark ? Color.FromArgb(135, 145, 165) : Color.FromArgb(100, 100, 100)), 2.5f);
			pe.Graphics.DrawPath(pen, CreateRoundedRectPath(new Rectangle(0, 0, txtStep1Url.Width - 1, txtStep1Url.Height - 1), SX(6)));
		};
		txtStep1Url.ContextMenuStrip = CreateInputContextMenu(txtStep1Url);
		Color phColor = (isDark ? Color.FromArgb(120, 125, 135) : Color.FromArgb(130, 133, 140));
		bool phStep1Active = true;
		txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选生成段";
		txtStep1Url.ForeColor = phColor;
		txtStep1Url.GotFocus += delegate
		{
			if (phStep1Active)
			{
				phStep1Active = false;
				txtStep1Url.Text = "";
				txtStep1Url.ForeColor = DarkText;
			}
			txtStep1Url.Invalidate();
		};
		txtStep1Url.LostFocus += delegate
		{
			if (string.IsNullOrWhiteSpace(txtStep1Url.Text))
			{
				phStep1Active = true;
				txtStep1Url.Text = "请输入直播源地址，支持标准URL或{0001-0100}/[1-100]自定义范围，也可用{数字}手动框选生成段";
				txtStep1Url.ForeColor = phColor;
			}
		};
		step1Panel.Controls.Add(txtStep1Url);
		int step1HintTop = step1InputTop + step1InputH + CONTROL_GAP;
		Panel pnlSmartHint = new Panel
		{
			Location = At(CONTENT_PAD, step1HintTop),
			Size = new Size(DLG_W - CONTENT_PAD * 2, HINT_HEIGHT),
			BackColor = theme.StatusTagBg,
			BorderStyle = BorderStyle.None
		};
		pnlSmartHint.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, pnlSmartHint.Width, pnlSmartHint.Height), SX(6)));
		Label lblStep1SmartHint = new Label
		{
			Text = "\ud83d\udca1 智能识别：输入标准URL进入向导模式；输入带 [起始-结束] 的地址直接生成\n（如 http://example.com/[1-100].m3u8）",
			Font = HINT_FONT,
			ForeColor = theme.SuccessColor,
			Location = At(SX(16), SY(8)),
			AutoSize = false,
			Size = new Size(DLG_W - CONTENT_PAD * 2 - SX(32), HINT_HEIGHT - SY(16)),
			BackColor = theme.StatusTagBg
		};
		pnlSmartHint.Controls.Add(lblStep1SmartHint);
		pnlSmartHint.Paint += delegate(object s, PaintEventArgs pe)
		{
			using Pen pen = new Pen(theme.StatusTagBorder, 1f);
			pe.Graphics.DrawPath(pen, CreateRoundedRectPath(new Rectangle(0, 0, pnlSmartHint.Width - 1, pnlSmartHint.Height - 1), SX(6)));
		};
		step1Panel.Controls.Add(pnlSmartHint);
		int step2Top = TOP_PADDING;
		Label lblStep2Hint = new Label
		{
			Text = "请选择要生成的字符段",
			Font = LABEL_FONT,
			ForeColor = DarkText,
			Location = At(CONTENT_PAD, step2Top),
			AutoSize = true,
			BackColor = PanelBg
		};
		int hint2W = TextRenderer.MeasureText(lblStep2Hint.Text, lblStep2Hint.Font).Width;
		int hint2H = TextRenderer.MeasureText(lblStep2Hint.Text, lblStep2Hint.Font).Height;
		Label lblStep2Star = new Label
		{
			Text = "*",
			Font = LABEL_FONT,
			ForeColor = RedHighlight,
			Location = At(CONTENT_PAD + hint2W + SX(3), step2Top),
			AutoSize = true,
			BackColor = PanelBg
		};
		step2Panel.Controls.Add(lblStep2Hint);
		step2Panel.Controls.Add(lblStep2Star);
		int step2ContentTop = step2Top + hint2H + CONTROL_GAP;
		Panel segListContainer = new Panel
		{
			Location = At(CONTENT_PAD, step2ContentTop),
			Width = DLG_W - CONTENT_PAD * 2,
			Height = SY(310),
			BackColor = PanelBg,
			AutoScroll = true
		};
		step2Panel.Controls.Add(segListContainer);
		int numPanelW = SX(180);
		int step3Top = TOP_PADDING;
		int panelGap = SX(64);
		int twoPanelsW = numPanelW * 2 + panelGap;
		// 计算步骤指示器标签的左右边缘，使"起始/结束数字"标签与下方输入框与之对齐
		float stepColW3 = (float)DLG_W / 3f;
		float step1Center3 = stepColW3 * 0.5f;
		float step3Center3 = stepColW3 * 2.5f;
		int step1LabelLeftX;
		int step3LabelRightX;
		using (Graphics gMeasure = stepIndicator.CreateGraphics())
		{
			float fitW = stepColW3 - (float)SX(40);
			float fSize = 11f;
			Font fReg = null, fBold = null;
			SizeF sf;
			do
			{
				fReg?.Dispose();
				fBold?.Dispose();
				fReg = GetFont(fSize);
				fBold = GetFont(fSize, FontStyle.Bold);
				sf = gMeasure.MeasureString("输入源地址", fBold);
				fSize -= 0.5f;
			}
			while (sf.Width > fitW && fSize >= 8f);
			float w1 = gMeasure.MeasureString("输入源地址", fReg).Width;
			float w3 = gMeasure.MeasureString("设置范围", fBold).Width;
			step1LabelLeftX = (int)(step1Center3 - w1 / 2f);
			step3LabelRightX = (int)(step3Center3 + w3 / 2f);
			fReg?.Dispose();
			fBold?.Dispose();
		}
		int wFrom = TextRenderer.MeasureText("起始数字", LABEL_FONT).Width;
		int wTo = TextRenderer.MeasureText("结束数字", LABEL_FONT).Width;
		int pFromX = step1LabelLeftX + wFrom / 2 - numPanelW / 2;
		int pToX = step3LabelRightX - wTo / 2 - numPanelW / 2;
		Label lblStep3From = new Label
		{
			Text = "起始数字",
			Font = LABEL_FONT,
			ForeColor = DarkText,
			Location = At(step1LabelLeftX, step3Top),
			AutoSize = true,
			TextAlign = ContentAlignment.MiddleLeft,
			BackColor = PanelBg
		};
		step3Panel.Controls.Add(lblStep3From);
		Label lblStep3To = new Label
		{
			Text = "结束数字",
			Font = LABEL_FONT,
			ForeColor = DarkText,
			Location = At(step3LabelRightX - wTo, step3Top),
			AutoSize = true,
			TextAlign = ContentAlignment.MiddleLeft,
			BackColor = PanelBg
		};
		step3Panel.Controls.Add(lblStep3To);
		TextBox txtFrom = null;
		TextBox txtTo = null;
		Panel pnlTextOptions = null;
		CheckedListBox clstTextCandidates = null;
		List<string> selectedTextValues = null;
		int step3PanelTop = step3Top + SY(28) + CONTROL_GAP;
		Panel pFrom = CreateNumPanel(pFromX, fromVal, out txtFrom);
		pFrom.Location = At(pFromX, step3PanelTop);
		step3Panel.Controls.Add(pFrom);
		Panel pTo = CreateNumPanel(pToX, toVal, out txtTo);
		pTo.Location = At(pToX, step3PanelTop);
		step3Panel.Controls.Add(pTo);
		int step3HintTop = step3PanelTop + INPUT_HEIGHT + SY(24);
		Panel pnlRangeHint = new Panel
		{
			Location = At(CONTENT_PAD, step3HintTop),
			Size = new Size(DLG_W - CONTENT_PAD * 2, SY(72)),
			BackColor = theme.TipBg,
			BorderStyle = BorderStyle.None
		};
		pnlRangeHint.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, pnlRangeHint.Width, pnlRangeHint.Height), SX(6)));
		Label lblStep3RangeHint = new Label
		{
			Text = "⚠ 最大生成范围为10000，范围过大可能导致检测时间过长",
			Font = GetFont(10f),
			ForeColor = theme.WarnColor,
			Location = At(SX(16), SY(10)),
			AutoSize = false,
			Size = new Size(DLG_W - CONTENT_PAD * 2 - SX(32), SY(52)),
			BackColor = theme.TipBg,
			TextAlign = ContentAlignment.MiddleLeft
		};
		pnlRangeHint.Controls.Add(lblStep3RangeHint);
		pnlRangeHint.Paint += delegate(object s, PaintEventArgs pe)
		{
			using Pen pen = new Pen(theme.WarnColor, 1.5f);
			pe.Graphics.DrawPath(pen, CreateRoundedRectPath(new Rectangle(0, 0, pnlRangeHint.Width - 1, pnlRangeHint.Height - 1), SX(6)));
		};
		step3Panel.Controls.Add(pnlRangeHint);
		Label lblStep3Preview = new Label
		{
			Text = "",
			Font = URL_FONT,
			ForeColor = GreenMain,
			Location = At(CONTENT_PAD, SY(176)),
			AutoSize = false,
			Size = new Size(DLG_W - CONTENT_PAD * 2, 60),
			BackColor = theme.StatusTagBg,
			Visible = false,
			Padding = new Padding(SX(10), SY(8), SX(10), SY(8))
		};
		step3Panel.Controls.Add(lblStep3Preview);
		pnlTextOptions = new Panel
		{
			Location = At(CONTENT_PAD, 22),
			Size = new Size(DLG_W - CONTENT_PAD * 2, 210),
			BackColor = Color.Transparent,
			Visible = false
		};
		Label lblTextOptTitle = new Label
		{
			Text = "选择要替换的选项：",
			Font = GetFont(11f),
			ForeColor = DarkText,
			Location = At(0, 0),
			AutoSize = true,
			BackColor = Color.Transparent
		};
		pnlTextOptions.Controls.Add(lblTextOptTitle);
		clstTextCandidates = new CheckedListBox
		{
			Location = At(0, SY(30)),
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
			Location = At(0, SY(176)),
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
		btnTextCheckAll.FlatAppearance.MouseOverBackColor = NumPadHover;
		btnTextCheckAll.FlatAppearance.MouseDownBackColor = NumPadDown;
		btnTextCheckAll.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 70, 30), 6));
		btnTextCheckAll.Click += delegate
		{
			for (int i = 0; i < clstTextCandidates.Items.Count; i++)
			{
				clstTextCandidates.SetItemChecked(i, value: true);
			}
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
		btnTextUncheckAll.FlatAppearance.MouseOverBackColor = NumPadHover;
		btnTextUncheckAll.FlatAppearance.MouseDownBackColor = NumPadDown;
		btnTextUncheckAll.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, 70, 30), 6));
		btnTextUncheckAll.Click += delegate
		{
			for (int i = 0; i < clstTextCandidates.Items.Count; i++)
			{
				clstTextCandidates.SetItemChecked(i, value: false);
			}
		};
		pnlTextBtns.Controls.Add(btnTextCheckAll);
		pnlTextBtns.Controls.Add(btnTextUncheckAll);
		pnlTextOptions.Controls.Add(pnlTextBtns);
		step3Panel.Controls.Add(pnlTextOptions);
		Panel sepBottom = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 1,
			BackColor = GrayLine
		};
		Panel bottomBar = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = SY(68),
			BackColor = PanelBg,
			Padding = new Padding(CONTENT_PAD, 0, CONTENT_PAD, 0)
		};
		int btnBottomY = (bottomBar.Height - BTN_HEIGHT) / 2;
		Button btnPrev = new Button
		{
			Text = "← 上一步 (B)",
			Size = new Size(SX(130), BTN_HEIGHT),
			Location = At(CONTENT_PAD, btnBottomY),
			Visible = false
		};
		StyleGreenButton(btnPrev);
		bottomBar.Controls.Add(btnPrev);
		Button btnAction = new Button
		{
			Text = "下一步 (N) →",
			Size = new Size(SX(130), BTN_HEIGHT),
			Location = At(DLG_W - CONTENT_PAD - SX(130), btnBottomY)
		};
		StyleGreenButton(btnAction);
		bottomBar.Controls.Add(btnAction);
		List<ChannelInfo> generatedChannels = null;
		btnAction.Click += delegate
		{
			if (currentStep == 1)
			{
				string text = (phStep1Active ? "" : txtStep1Url.Text.Trim());
				string text2 = ExtractUrlFromText(text);
				if (!string.IsNullOrEmpty(text2) && text2 != text)
				{
					List<ChannelInfo> list = ParseChannelList(text);
					if (list.Count > 1)
					{
						switch (DarkMessageBox.Show($"检测到 {list.Count} 条频道列表（名称+地址格式），是否直接导入到检测窗口？\n\n点击「是」直接导入全部频道\n点击「否」使用第一条URL进行生成", "检测到频道列表", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
						{
						case DialogResult.Cancel:
							return;
						case DialogResult.Yes:
							generatedChannels = list;
							dlg.DialogResult = DialogResult.OK;
							dlg.Close();
							return;
						}
					}
					text = text2;
					phStep1Active = false;
					txtStep1Url.Text = text;
					txtStep1Url.ForeColor = DarkText;
				}
				step1Url = text;
				string error;
				long start;
				long end;
				int padW;
				bool padZero;
				int replacePos;
				int replaceLen;
				string template;
				if (string.IsNullOrWhiteSpace(text))
				{
					DarkMessageBox.Show("请输入直播源地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else if (ValidateCustomRangeUrl(text, out error, out start, out end, out padW, out padZero, out replacePos, out replaceLen, out template))
				{
					isCustomRangeMode = true;
					customRangeStart = start;
					customRangeEnd = end;
					customPadWidth = padW;
					customPadZero = padZero;
					customReplacePos = replacePos;
					customReplaceLen = replaceLen;
					customUrlTemplate = template;
					currentStep = 3;
					txtFrom.Text = start.ToString();
					txtTo.Text = end.ToString();
					pnlTextOptions.Visible = false;
					lblStep3From.Visible = true;
					lblStep3To.Visible = true;
					pFrom.Visible = true;
					pTo.Visible = true;
					pnlRangeHint.Visible = true;
					string arg = template.Substring(0, replacePos) + PadNumber(start, padW, padZero) + template.Substring(replacePos + replaceLen);
					lblStep3Preview.Text = $"✅ 检测到自定义范围格式\n将生成 {end - start + 1} 个源地址\n示例：{arg}";
					lblStep3Preview.Visible = true;
					UpdateStepUI();
				}
				else
				{
					int preGlobalPos = -1;
					int preGlobalLen = 0;
					if (ParseManualBracketUrl(text, out var error2, out var cleanUrl, out var _, out var replacePos2, out var replaceLen2))
					{
						text = cleanUrl;
						preGlobalPos = replacePos2;
						preGlobalLen = replaceLen2;
					}
					if (!string.IsNullOrEmpty(error2))
					{
						DarkMessageBox.Show(error2, "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					else if (!Uri.IsWellFormedUriString(text, UriKind.Absolute))
					{
						DarkMessageBox.Show("请输入有效的直播源地址（如 http://example.com/1.m3u8）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					else
					{
						isCustomRangeMode = false;
						lblStep3Preview.Visible = false;
						BuildUrlPreview(text, -1, 0, 0, preGlobalPos, preGlobalLen);
						currentStep = 2;
						UpdateStepUI();
					}
				}
			}
			else if (currentStep == 2)
			{
				if (segs == null || segs.Count == 0)
				{
					DarkMessageBox.Show("未找到可生成的字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else if (selectedSegIndex < 0 || selectedSegIndex >= segs.Count)
				{
					DarkMessageBox.Show("请选择要生成的字段（点击RadioButton或字段文本）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					ScanSegInfo scanSegInfo = segs[selectedSegIndex];
					selectedTextValues = null;
					if (scanSegInfo.Type == ScanSegType.Number)
					{
						string originalText = scanSegInfo.OriginalText;
						int num = ((subSegStart != null && selectedSegIndex >= 0 && selectedSegIndex < subSegStart.Length) ? subSegStart[selectedSegIndex] : 0);
						int num2 = ((subSegLen != null && selectedSegIndex >= 0 && selectedSegIndex < subSegLen.Length) ? subSegLen[selectedSegIndex] : originalText.Length);
						if (num < 0)
						{
							num = 0;
						}
						if (num + num2 > originalText.Length)
						{
							num2 = originalText.Length - num;
						}
						if (num2 <= 0)
						{
							num = 0;
							num2 = originalText.Length;
						}
						string text3 = originalText.Substring(num, num2);
						segPadWidth = num2;
						segPadZero = text3.Length > 1 && text3.StartsWith("0");
						if (long.TryParse(text3, out var result))
						{
							txtFrom.Text = result.ToString();
							long num3 = result;
							int num4 = ((result < 100) ? 10 : ((result < 10000) ? 20 : 50));
							txtTo.Text = (num3 + num4).ToString();
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
						List<string> obj = scanSegInfo.Candidates ?? new List<string>();
						string originalText2 = scanSegInfo.OriginalText;
						List<string> list2 = new List<string>();
						if (obj.Contains(originalText2))
						{
							list2.Add(originalText2);
						}
						foreach (string current in obj)
						{
							if (current != originalText2 && !list2.Contains(current))
							{
								list2.Add(current);
							}
						}
						int num5 = -1;
						for (int i = 0; i < list2.Count; i++)
						{
							string text4 = list2[i];
							string text5 = GetChannelDisplayName(text4, scanSegInfo.Type);
							clstTextCandidates.Items.Add(text4 + ((text5 != text4) ? (" (" + text5 + ")") : ""));
							if (text4 == originalText2)
							{
								num5 = i;
							}
						}
						for (int j = 0; j < clstTextCandidates.Items.Count; j++)
						{
							clstTextCandidates.SetItemChecked(j, j == num5 || j < 5);
						}
						string text6 = "选项";
						if (scanSegInfo.Type == ScanSegType.CctvChannel)
						{
							text6 = "CCTV频道";
						}
						else if (scanSegInfo.Type == ScanSegType.PayChannel)
						{
							text6 = "付费频道";
						}
						else if (scanSegInfo.Type == ScanSegType.WsChannel)
						{
							text6 = "卫视频道";
						}
						else if (scanSegInfo.Type == ScanSegType.MovieChannel)
						{
							text6 = "影视频道";
						}
						else if (scanSegInfo.Type == ScanSegType.Resolution)
						{
							text6 = "分辨率";
						}
						lblTextOptTitle.Text = "选择要替换的" + text6 + "：";
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
			}
			else if (currentStep == 3)
			{
				if (isCustomRangeMode)
				{
					if (!long.TryParse(txtFrom.Text, out var result2))
					{
						result2 = 0L;
					}
					if (!long.TryParse(txtTo.Text, out var result3))
					{
						result3 = 0L;
					}
					if (result2 < customRangeStart || result2 > customRangeEnd || result3 < customRangeStart || result3 > customRangeEnd || result2 >= result3)
					{
						DarkMessageBox.Show("请输入有效的范围值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					else
					{
						fromVal = result2;
						toVal = result3;
						DoGenerate();
					}
				}
				else
				{
					ScanSegInfo scanSegInfo2 = segs[selectedSegIndex];
					if (scanSegInfo2.Type == ScanSegType.Number)
					{
						if (!long.TryParse(txtFrom.Text, out var result4))
						{
							result4 = 0L;
						}
						if (!long.TryParse(txtTo.Text, out var result5))
						{
							result5 = 0L;
						}
						if (result4 >= result5)
						{
							DarkMessageBox.Show("起始值必须小于结束值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return;
						}
						long num6 = result5 - result4 + 1;
						if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
						{
							num6 *= ResolutionList.Length;
						}
						if (num6 > 10000)
						{
							DarkMessageBox.Show("生成范围过大，预计生成超过10000个源，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return;
						}
						fromVal = result4;
						toVal = result5;
					}
					else
					{
						selectedTextValues = new List<string>();
						List<string> list3 = scanSegInfo2.Candidates ?? new List<string>();
						string originalText3 = scanSegInfo2.OriginalText;
						List<string> list4 = new List<string>();
						if (list3.Contains(originalText3))
						{
							list4.Add(originalText3);
						}
						foreach (string current2 in list3)
						{
							if (current2 != originalText3 && !list4.Contains(current2))
							{
								list4.Add(current2);
							}
						}
						for (int k = 0; k < clstTextCandidates.Items.Count; k++)
						{
							if (clstTextCandidates.GetItemChecked(k) && k < list4.Count)
							{
								selectedTextValues.Add(list4[k]);
							}
						}
						if (selectedTextValues.Count == 0)
						{
							DarkMessageBox.Show("请至少选择一个选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return;
						}
						long num7 = selectedTextValues.Count;
						if (multiResEnabled && selectedResSegIndex >= 0 && selectedSegIndex != selectedResSegIndex)
						{
							num7 *= ResolutionList.Length;
						}
						if (num7 > 10000)
						{
							DarkMessageBox.Show("选择过多，预计生成超过10000个源，请减少选项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							return;
						}
					}
					DoGenerate();
				}
			}
		};
		btnPrev.Click += delegate
		{
			GoPrev();
		};
		BindEnter(txtStep1Url);
		BindEnter(txtFrom);
		BindEnter(txtTo);
		dlg.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (!e.Alt && !e.Control)
			{
				if (e.KeyCode == Keys.N && currentStep < 3 && !isCustomRangeMode)
				{
					btnAction.PerformClick();
				}
				if (e.KeyCode == Keys.B && btnPrev.Visible)
				{
					GoPrev();
				}
			}
		};
		dlg.Controls.Add(contentHost);
		dlg.Controls.Add(sepBottom);
		dlg.Controls.Add(bottomBar);
		dlg.Controls.Add(sepTitle);
		dlg.Controls.Add(titleBar);
		dlg.Shown += delegate
		{
			UpdateStepUI();
			txtStep1Url.Focus();
		};
		Form opacityForm = new Form
		{
			FormBorderStyle = FormBorderStyle.None,
			BackColor = Color.Black,
			Opacity = 0.35,
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Bounds = base.Bounds
		};
		opacityForm.Shown += delegate
		{
			dlg.Location = At(base.Left + (base.Width - dlg.Width) / 2, base.Top + (base.Height - dlg.Height) / 2);
			dlg.ShowDialog(opacityForm);
			opacityForm.Close();
			if (generatedChannels != null && generatedChannels.Count > 0)
			{
				BeginInvoke((Action)delegate
				{
					allChannels.Clear();
					HashSet<string> hashSet = new HashSet<string>();
					int num = 0;
					int num2 = 0;
					foreach (ChannelInfo current in generatedChannels)
					{
						string item = current.Url.ToLowerInvariant();
						if (hashSet.Contains(item))
						{
							num2++;
						}
						else
						{
							if (string.IsNullOrEmpty(current.Name) || Regex.IsMatch(current.Name, "^源\\d+$"))
							{
								current.Name = $"源{allChannels.Count + 1}";
							}
							allChannels.Add(current);
							hashSet.Add(item);
							num++;
						}
					}
					totalCount = allChannels.Count;
					detectedCount = 0;
					availableCount = 0;
					UpdateGroupFilter();
					RefreshGrid();
					UpdateEmptyState();
					UpdateStatusBar();
					UpdateActionButtonsVisibility();
				});
			}
		};
		opacityForm.Show(this);
		void AddChannelWithResVariants(List<ChannelInfo> channels, string prefixPart, string baseNewPath, ScanSegInfo selSeg, string baseName, int deltaLen)
		{
			string baseUrl = prefixPart + baseNewPath;
			if (multiResEnabled && selectedResSegIndex >= 0 && selectedResSegIndex < segs.Count && selectedSegIndex != selectedResSegIndex)
			{
				ScanSegInfo scanSegInfo = segs[selectedResSegIndex];
				int resPathStart = scanSegInfo.PathStart;
				int resPathLen = scanSegInfo.OriginalText.Length;
				if (selectedSegIndex < selectedResSegIndex)
				{
					resPathStart += deltaLen;
				}
				string[] resolutionList = ResolutionList;
				foreach (string res in resolutionList)
				{
					string resNewPath = baseNewPath.Substring(0, resPathStart) + res + baseNewPath.Substring(resPathStart + resPathLen);
					string resUrl = prefixPart + resNewPath;
					string resName = baseName + "-" + res;
					channels.Add(new ChannelInfo
					{
						Name = resName,
						Url = resUrl,
						Group = "生成器",
						Status = "未检测",
						Visible = true
					});
					if (channels.Count > 10000)
					{
						break;
					}
				}
			}
			else
			{
				channels.Add(new ChannelInfo
				{
					Name = baseName,
					Url = baseUrl,
					Group = "生成器",
					Status = "未检测",
					Visible = true
				});
			}
		}
		void BindEnter(TextBox tb)
		{
			tb.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Return)
				{
					e.SuppressKeyPress = true;
					btnAction.PerformClick();
				}
			};
		}
		void BuildUrlPreview(string url, int preSelectSeg = -1, int preSubStart = 0, int preSubLen = 0, int preGlobalPos = -1, int preGlobalLen = 0)
		{
			url = url.Trim().Trim('"', ' ', '`', '\t');
			int qPos = url.IndexOf('?');
			if (qPos >= 0)
			{
				url = url.Substring(0, qPos);
			}
			segBaseUrl = url;
			segListContainer.Controls.Clear();
			selectedSegIndex = -1;
			multiResEnabled = false;
			selectedResSegIndex = -1;
			int pathStart = url.IndexOf("://");
			if (pathStart >= 0)
			{
				pathStart = url.IndexOf('/', pathStart + 3);
			}
			if (pathStart < 0)
			{
				pathStart = 0;
			}
			string pathPart = ((pathStart > 0) ? url.Substring(pathStart) : url);
			string prefixPart = ((pathStart > 0) ? url.Substring(0, pathStart) : "");
			Dictionary<string, string> payDict = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> kv in PayChannelList)
			{
				payDict[kv.Key] = kv.Value;
			}
			Dictionary<string, string> wsDict = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> kv2 in WsChannelList)
			{
				wsDict[kv2.Key] = kv2.Value;
			}
			Dictionary<string, string> movieDict = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> kv3 in MovieChannelList)
			{
				movieDict[kv3.Key] = kv3.Value;
			}
			string[] cctvCandidates = CctvChannelMap["cctv"];
			HashSet<string> cctvSet = new HashSet<string>(cctvCandidates);
			List<string> payKeys = new List<string>(payDict.Keys);
			List<string> wsKeys = new List<string>(wsDict.Keys);
			List<string> movieKeys = new List<string>(movieDict.Keys);
			segs = new List<ScanSegInfo>();
			foreach (Match m in RxSegmentNumber.Matches(pathPart))
			{
				string num = m.Groups[1].Value;
				segs.Add(new ScanSegInfo
				{
					Type = ScanSegType.Number,
					PathStart = m.Groups[1].Index,
					GlobalStart = pathStart + m.Groups[1].Index,
					GlobalEnd = pathStart + m.Groups[1].Index + m.Groups[1].Length,
					OriginalText = num,
					Label = "\ud83d\udd22 数字段: " + num,
					Candidates = null
				});
			}
			foreach (Match m2 in RxResolutionTagScan.Matches(pathPart))
			{
				string res = m2.Groups[1].Value.ToLower();
				segs.Add(new ScanSegInfo
				{
					Type = ScanSegType.Resolution,
					PathStart = m2.Groups[1].Index,
					GlobalStart = pathStart + m2.Groups[1].Index,
					GlobalEnd = pathStart + m2.Groups[1].Index + m2.Groups[1].Length,
					OriginalText = res,
					Label = "\ud83d\udcd0 分辨率: " + res,
					Candidates = new List<string>(ResolutionList)
				});
			}
			List<Tuple<int, int>> coveredRanges = new List<Tuple<int, int>>();
			foreach (ScanSegInfo seg in segs)
			{
				coveredRanges.Add(Tuple.Create(seg.PathStart, seg.PathStart + seg.OriginalText.Length));
			}
			int fileExtPos = -1;
			Match extMatch = Regex.Match(pathPart, "\\.(m3u8|flv|ts|mp4)(?=[/._?-]|$)", RegexOptions.IgnoreCase);
			if (extMatch.Success)
			{
				fileExtPos = extMatch.Index;
			}
			foreach (Match item2 in RxUrlTokenScan.Matches(pathPart))
			{
				int tokStart = item2.Index;
				int tokLen = item2.Length;
				string tok = item2.Value.ToLower();
				bool overlaps = false;
				foreach (Tuple<int, int> range in coveredRanges)
				{
					if (tokStart < range.Item2 && tokStart + tokLen > range.Item1)
					{
						overlaps = true;
						break;
					}
				}
				if (!overlaps && (fileExtPos < 0 || tokStart < fileExtPos) && tok.Length >= 2 && !Regex.IsMatch(tok, "^\\d+$"))
				{
					ScanSegType segType = ScanSegType.Number;
					string segLabel = null;
					List<string> segCandidates = null;
					bool found = false;
					if (RxCctv.IsMatch(tok) && cctvSet.Contains(tok))
					{
						segType = ScanSegType.CctvChannel;
						segLabel = "\ud83d\udcfa CCTV频道: " + tok;
						segCandidates = new List<string>(cctvCandidates);
						found = true;
					}
					else if (payDict.ContainsKey(tok))
					{
						segType = ScanSegType.PayChannel;
						segLabel = "\ud83d\udcfa 付费频道: " + tok;
						segCandidates = payKeys;
						found = true;
					}
					else if (wsDict.ContainsKey(tok))
					{
						segType = ScanSegType.WsChannel;
						segLabel = "\ud83d\udce1 卫视频道: " + tok;
						segCandidates = wsKeys;
						found = true;
					}
					else if (movieDict.ContainsKey(tok))
					{
						segType = ScanSegType.MovieChannel;
						segLabel = "\ud83c\udfac 影视频道: " + tok;
						segCandidates = movieKeys;
						found = true;
					}
					else if (fileExtPos >= 0 && tokStart + tokLen == fileExtPos && Regex.IsMatch(tok, "^[a-z]{2,}\\d*[a-z]*$"))
					{
						if (cctvSet.Contains(tok))
						{
							segType = ScanSegType.CctvChannel;
							segLabel = "\ud83d\udcfa CCTV频道: " + tok;
							segCandidates = new List<string>(cctvCandidates);
							found = true;
						}
						else if (payDict.ContainsKey(tok))
						{
							segType = ScanSegType.PayChannel;
							segLabel = "\ud83d\udcfa 付费频道: " + tok;
							segCandidates = payKeys;
							found = true;
						}
						else if (wsDict.ContainsKey(tok))
						{
							segType = ScanSegType.WsChannel;
							segLabel = "\ud83d\udce1 卫视频道: " + tok;
							segCandidates = wsKeys;
							found = true;
						}
						else if (movieDict.ContainsKey(tok))
						{
							segType = ScanSegType.MovieChannel;
							segLabel = "\ud83c\udfac 影视频道: " + tok;
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
			}
			segs.Sort((ScanSegInfo a, ScanSegInfo b) => a.PathStart.CompareTo(b.PathStart));
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
			Color bracketColor;
			Color bracketActiveColor;
			Color subSelBg;
			Color dimFg;
			Panel[] radioCircles;
			Label[] segPrefixLbl;
			Label[] segBracketL;
			Label[] segSelTextLbl;
			Label[] segBracketR;
			Label[] segSuffixLbl;
			Label[] segTypeTagLbl;
			bool[] segSelected;
			Panel adjPanel;
			Button btnLeftShrink;
			Button btnLeftExpand;
			Button btnRightShrink;
			Button btnRightExpand;
			Label lblSelInfo;
			if (segs.Count == 0)
			{
				Label noMatch = new Label
				{
					Text = "❌ 未找到可生成的字段，请检查URL格式（支持数字段如/123/、频道名如cctv1、分辨率如1080p等）",
					ForeColor = RedHighlight,
					Font = GetFont(10.5f),
					Location = At(0, SY(8)),
					AutoSize = true,
					BackColor = Color.Transparent
				};
				segListContainer.Controls.Add(noMatch);
			}
			else
			{
				subSegStart = new int[segs.Count];
				subSegLen = new int[segs.Count];
				for (int i2 = 0; i2 < segs.Count; i2++)
				{
					if (segs[i2].Type == ScanSegType.Number)
					{
						subSegStart[i2] = 0;
						subSegLen[i2] = segs[i2].OriginalText.Length;
					}
					else
					{
						subSegStart[i2] = 0;
						subSegLen[i2] = 0;
					}
				}
				int itemY = 8;
				int itemH = SY(44);
				int radioSize = 18;
				Color rowBgNormal = PanelBg;
				Color radioBorderColor = GrayBorder;
				bracketColor = (isDark ? theme.TextSecondary : Color.FromArgb(150, 153, 160));
				bracketActiveColor = GreenMain;
				subSelBg = (isDark ? Color.FromArgb(30, 90, 50) : Color.FromArgb(195, 240, 205));
				dimFg = (isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(120, 125, 135));
				radioCircles = new Panel[segs.Count];
				Panel[] rowPanels = new Panel[segs.Count];
				segPrefixLbl = new Label[segs.Count];
				segBracketL = new Label[segs.Count];
				segSelTextLbl = new Label[segs.Count];
				segBracketR = new Label[segs.Count];
				segSuffixLbl = new Label[segs.Count];
				segTypeTagLbl = new Label[segs.Count];
				segSelected = new bool[segs.Count];
				adjPanel = null;
				btnLeftShrink = null;
				btnLeftExpand = null;
				btnRightShrink = null;
				btnRightExpand = null;
				Button btnSelectAll = null;
				lblSelInfo = null;
				for (int i3 = 0; i3 < segs.Count; i3++)
				{
					ScanSegInfo seg2 = segs[i3];
					int segStartInPath = seg2.PathStart;
					int segLen = seg2.OriginalText.Length;
					string beforeText = pathPart.Substring(0, segStartInPath);
					string afterText = pathPart.Substring(segStartInPath + segLen);
					string fullBefore = prefixPart + beforeText;
					Panel rowPanel = new Panel
					{
						Location = At(0, itemY),
						Width = segListContainer.Width - 20,
						Height = itemH,
						BackColor = rowBgNormal,
						Cursor = Cursors.Hand
					};
					int idxI = i3;
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
						Tag = i3,
						Margin = new Padding(0)
					};
					rowFlow.Controls.Add(lblBefore);
					Panel radioCircle = new Panel
					{
						Size = new Size(radioSize, radioSize),
						BackColor = Color.Transparent,
						Tag = i3,
						Cursor = Cursors.Hand,
						Margin = new Padding(4, 0, 4, 0)
					};
					radioCircle.Paint += delegate(object s, PaintEventArgs pe)
					{
						Graphics graphics = pe.Graphics;
						graphics.SmoothingMode = SmoothingMode.AntiAlias;
						bool num2 = segSelected[idxI];
						Rectangle rect = new Rectangle(0, 0, radioSize - 1, radioSize - 1);
						if (num2)
						{
							using (SolidBrush brush = new SolidBrush(bracketActiveColor))
							{
								graphics.FillEllipse(brush, rect);
							}
							using (Pen pen = new Pen(bracketActiveColor))
							{
								graphics.DrawEllipse(pen, rect);
							}
							int num3 = 6;
							int num4 = (radioSize - num3) / 2;
							using SolidBrush brush2 = new SolidBrush(PanelBg);
							graphics.FillEllipse(brush2, new Rectangle(num4, num4, num3, num3));
							return;
						}
						using Pen pen2 = new Pen(radioBorderColor, 1.5f);
						graphics.DrawEllipse(pen2, rect);
					};
					rowFlow.Controls.Add(radioCircle);
					radioCircles[i3] = radioCircle;
					if (seg2.Type == ScanSegType.Number)
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
						segPrefixLbl[i3] = lblPre;
						Label lblBL = new Label
						{
							Text = "{",
							Font = URL_FONT,
							ForeColor = bracketColor,
							AutoSize = true,
							BackColor = Color.Transparent,
							Cursor = Cursors.Hand,
							Tag = i3,
							Margin = new Padding(0)
						};
						rowFlow.Controls.Add(lblBL);
						segBracketL[i3] = lblBL;
						Label lblNum = new Label
						{
							Text = seg2.OriginalText,
							Font = URL_FONT,
							ForeColor = DarkText,
							AutoSize = true,
							BackColor = Color.Transparent,
							Cursor = Cursors.Hand,
							Tag = i3,
							Margin = new Padding(0)
						};
						rowFlow.Controls.Add(lblNum);
						segSelTextLbl[i3] = lblNum;
						Label lblBR = new Label
						{
							Text = "}",
							Font = URL_FONT,
							ForeColor = bracketColor,
							AutoSize = true,
							BackColor = Color.Transparent,
							Cursor = Cursors.Hand,
							Tag = i3,
							Margin = new Padding(0)
						};
						rowFlow.Controls.Add(lblBR);
						segBracketR[i3] = lblBR;
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
						segSuffixLbl[i3] = lblSuf;
					}
					else
					{
						segPrefixLbl[i3] = null;
						segBracketL[i3] = null;
						segBracketR[i3] = null;
						segSuffixLbl[i3] = null;
						Label lblText = new Label
						{
							Text = seg2.OriginalText,
							Font = URL_FONT,
							ForeColor = DarkText,
							AutoSize = true,
							BackColor = Color.Transparent,
							Cursor = Cursors.Hand,
							Tag = i3,
							Margin = new Padding(0)
						};
						rowFlow.Controls.Add(lblText);
						segSelTextLbl[i3] = lblText;
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
					EventHandler rowClickHandler = delegate
					{
						SelectSegment(idxI);
					};
					lblBefore.Click += rowClickHandler;
					lblAfter.Click += rowClickHandler;
					if (segSelTextLbl[i3] != null)
					{
						segSelTextLbl[i3].Click += rowClickHandler;
					}
					if (segBracketL[i3] != null)
					{
						segBracketL[i3].Click += rowClickHandler;
					}
					if (segBracketR[i3] != null)
					{
						segBracketR[i3].Click += rowClickHandler;
					}
					radioCircle.Click += rowClickHandler;
					rowPanel.Click += rowClickHandler;
					rowFlow.Click += rowClickHandler;
					rowPanel.Controls.Add(rowFlow);
					rowPanels[i3] = rowPanel;
					segListContainer.Controls.Add(rowPanel);
					itemY += itemH + CONTROL_GAP;
				}
				adjPanel = new Panel
				{
					Location = At(0, itemY),
					Width = segListContainer.Width - 20,
					Height = SY(50),
					BackColor = Color.Transparent,
					Visible = false
				};
				int btnH = SY(34);
				int btnY = (SY(50) - btnH) / 2;
				btnLeftExpand = new Button
				{
					Text = "◀ {",
					Size = new Size(SX(54), btnH),
					Location = At(0, btnY),
					FlatStyle = FlatStyle.Flat,
					BackColor = PanelBg,
					ForeColor = DarkText,
					Font = GetFont(9f),
					Cursor = Cursors.Hand
				};
				btnLeftShrink = new Button
				{
					Text = "{ ▶",
					Size = new Size(SX(54), btnH),
					Location = At(SX(58), btnY),
					FlatStyle = FlatStyle.Flat,
					BackColor = PanelBg,
					ForeColor = DarkText,
					Font = GetFont(9f),
					Cursor = Cursors.Hand
				};
				btnRightShrink = new Button
				{
					Text = "} ◀",
					Size = new Size(SX(54), btnH),
					Location = At(SX(116), btnY),
					FlatStyle = FlatStyle.Flat,
					BackColor = PanelBg,
					ForeColor = DarkText,
					Font = GetFont(9f),
					Cursor = Cursors.Hand
				};
				btnRightExpand = new Button
				{
					Text = "} ▶",
					Size = new Size(SX(54), btnH),
					Location = At(SX(174), btnY),
					FlatStyle = FlatStyle.Flat,
					BackColor = PanelBg,
					ForeColor = DarkText,
					Font = GetFont(9f),
					Cursor = Cursors.Hand
				};
				btnSelectAll = new Button
				{
					Text = "全选本段",
					Size = new Size(SX(80), btnH),
					Location = At(SX(236), btnY),
					FlatStyle = FlatStyle.Flat,
					BackColor = bracketActiveColor,
					ForeColor = Color.White,
					Font = GetFont(9f),
					Cursor = Cursors.Hand
				};
				StyleBtn(btnLeftShrink);
				StyleBtn(btnLeftExpand);
				StyleBtn(btnRightShrink);
				StyleBtn(btnRightExpand);
				btnSelectAll.FlatAppearance.BorderSize = 0;
				btnSelectAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 140, 66);
				btnSelectAll.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 120, 56);
				btnLeftShrink.Click += delegate
				{
					if (selectedSegIndex >= 0 && segs[selectedSegIndex].Type == ScanSegType.Number && subSegLen[selectedSegIndex] > 1)
					{
						subSegStart[selectedSegIndex]++;
						subSegLen[selectedSegIndex]--;
						UpdateSegDisplay(selectedSegIndex);
						UpdateAdjButtons();
					}
				};
				btnLeftExpand.Click += delegate
				{
					if (selectedSegIndex >= 0 && segs[selectedSegIndex].Type == ScanSegType.Number && subSegStart[selectedSegIndex] > 0)
					{
						subSegStart[selectedSegIndex]--;
						subSegLen[selectedSegIndex]++;
						UpdateSegDisplay(selectedSegIndex);
						UpdateAdjButtons();
					}
				};
				btnRightShrink.Click += delegate
				{
					if (selectedSegIndex >= 0 && segs[selectedSegIndex].Type == ScanSegType.Number && subSegLen[selectedSegIndex] > 1)
					{
						subSegLen[selectedSegIndex]--;
						UpdateSegDisplay(selectedSegIndex);
						UpdateAdjButtons();
					}
				};
				btnRightExpand.Click += delegate
				{
					if (selectedSegIndex >= 0 && segs[selectedSegIndex].Type == ScanSegType.Number)
					{
						int length = segs[selectedSegIndex].OriginalText.Length;
						if (subSegStart[selectedSegIndex] + subSegLen[selectedSegIndex] < length)
						{
							subSegLen[selectedSegIndex]++;
							UpdateSegDisplay(selectedSegIndex);
							UpdateAdjButtons();
						}
					}
				};
				btnSelectAll.Click += delegate
				{
					if (selectedSegIndex >= 0 && segs[selectedSegIndex].Type == ScanSegType.Number)
					{
						subSegStart[selectedSegIndex] = 0;
						subSegLen[selectedSegIndex] = segs[selectedSegIndex].OriginalText.Length;
						UpdateSegDisplay(selectedSegIndex);
						UpdateAdjButtons();
					}
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
					Location = At(SX(320), (SY(50) - SY(22)) / 2),
					Size = new Size(adjPanel.Width - SX(320), SY(22)),
					BackColor = Color.Transparent
				};
				adjPanel.Controls.Add(lblSelInfo);
				segListContainer.Controls.Add(adjPanel);
				itemY = adjPanel.Bottom;
				Label lblHint = new Label
				{
					Text = "\ud83d\udca1 点击单选按钮选择要生成的字段，绿色●为当前选中。数字段可用 ◀{ {▶ }◀ }▶ 按钮调整大括号框选部分位数（长数字可选子范围），含前导零将保持补零。频道/分辨率段将提供候选列表供选择。",
					Font = GetFont(8.5f),
					ForeColor = theme.SuccessColor,
					Location = At(0, itemY + SY(20)),
					AutoSize = false,
					Size = new Size(segListContainer.Width - 20, SY(56)),
					BackColor = theme.StatusTagBg,
					Padding = new Padding(SX(10), SY(8), SX(10), SY(8)),
					TextAlign = ContentAlignment.TopLeft
				};
				segListContainer.Controls.Add(lblHint);
				itemY = lblHint.Bottom + CONTROL_GAP;
				CheckBox chkMultiRes = new CheckBox
				{
					Text = "\ud83d\udcd0 同时生成多个分辨率（1080p/720p/540p/480p/360p）",
					Font = GetFont(9.5f),
					ForeColor = (hasResolution ? DarkText : GrayText),
					Location = At(0, itemY),
					AutoSize = true,
					BackColor = Color.Transparent,
					Enabled = hasResolution,
					Checked = false,
					Cursor = (hasResolution ? Cursors.Hand : Cursors.Default)
				};
				chkMultiRes.CheckedChanged += delegate
				{
					multiResEnabled = chkMultiRes.Checked;
					selectedResSegIndex = (multiResEnabled ? resSegIdx : (-1));
				};
				segListContainer.Controls.Add(chkMultiRes);
				if (segs.Count > 0)
				{
					int initSeg = 0;
					if (preGlobalPos >= 0 && preGlobalLen > 0)
					{
						for (int si = 0; si < segs.Count; si++)
						{
							if (segs[si].Type == ScanSegType.Number && preGlobalPos >= segs[si].GlobalStart && preGlobalPos + preGlobalLen <= segs[si].GlobalEnd)
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
					if (preGlobalPos >= 0 && preGlobalLen > 0 && segs[initSeg].Type == ScanSegType.Number && preGlobalPos >= segs[initSeg].GlobalStart && preGlobalPos + preGlobalLen <= segs[initSeg].GlobalEnd)
					{
						int ss = preGlobalPos - segs[initSeg].GlobalStart;
						int numLen = segs[initSeg].OriginalText.Length;
						if (ss >= 0 && ss + preGlobalLen <= numLen)
						{
							subSegStart[initSeg] = ss;
							subSegLen[initSeg] = preGlobalLen;
							UpdateSegDisplay(initSeg);
							UpdateAdjButtons();
						}
					}
					else if (preSelectSeg >= 0 && preSelectSeg < segs.Count && preSubLen > 0 && segs[preSelectSeg].Type == ScanSegType.Number)
					{
						int numLen2 = segs[preSelectSeg].OriginalText.Length;
						if (preSubStart >= 0 && preSubStart + preSubLen <= numLen2)
						{
							subSegStart[preSelectSeg] = preSubStart;
							subSegLen[preSelectSeg] = preSubLen;
							UpdateSegDisplay(preSelectSeg);
							UpdateAdjButtons();
						}
					}
				}
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
			void UpdateAdjButtons()
			{
				if (selectedSegIndex < 0 || adjPanel == null)
				{
					if (adjPanel != null)
					{
						adjPanel.Visible = false;
					}
				}
				else
				{
					ScanSegInfo curSeg = segs[selectedSegIndex];
					if (curSeg.Type != ScanSegType.Number)
					{
						adjPanel.Visible = false;
					}
					else
					{
						adjPanel.Visible = true;
						int numLen3 = curSeg.OriginalText.Length;
						int ss2 = subSegStart[selectedSegIndex];
						int sl = subSegLen[selectedSegIndex];
						btnLeftShrink.Enabled = sl > 1;
						btnLeftExpand.Enabled = ss2 > 0;
						btnRightShrink.Enabled = sl > 1;
						btnRightExpand.Enabled = ss2 + sl < numLen3;
						string selDigits = curSeg.OriginalText.Substring(ss2, sl);
						string fullNum = curSeg.OriginalText;
						if (ss2 == 0 && sl == numLen3)
						{
							lblSelInfo.Text = $"已选中整段 {{{selDigits}}}（{numLen3}位），若需框选部分位数请用按钮调整大括号";
						}
						else
						{
							lblSelInfo.Text = string.Format("已框选：{0}{{{1}}}{2}", fullNum.Substring(0, ss2), selDigits, (ss2 + sl < numLen3) ? fullNum.Substring(ss2 + sl) : "");
						}
					}
				}
			}
			void UpdateSegDisplay(int segIdx)
			{
				if (segIdx >= 0 && segIdx < segs.Count)
				{
					ScanSegInfo seg3 = segs[segIdx];
					bool isSel = segIdx == selectedSegIndex;
					Label bl = segBracketL[segIdx];
					Label preL = segPrefixLbl[segIdx];
					Label selL = segSelTextLbl[segIdx];
					Label sufL = segSuffixLbl[segIdx];
					Label br = segBracketR[segIdx];
					Panel rad = radioCircles[segIdx];
					Label tagL = segTypeTagLbl[segIdx];
					if (seg3.Type == ScanSegType.Number)
					{
						int ss2 = subSegStart[segIdx];
						int sl = subSegLen[segIdx];
						string num2 = seg3.OriginalText;
						int numLen3 = num2.Length;
						if (isSel)
						{
							if (bl != null)
							{
								bl.Visible = true;
								bl.ForeColor = bracketActiveColor;
								bl.Font = URL_BOLD_FONT;
							}
							if (br != null)
							{
								br.Visible = true;
								br.ForeColor = bracketActiveColor;
								br.Font = URL_BOLD_FONT;
							}
							string prefix = ((ss2 > 0) ? num2.Substring(0, ss2) : "");
							string selDigits = num2.Substring(ss2, sl);
							string suffix = ((ss2 + sl < numLen3) ? num2.Substring(ss2 + sl) : "");
							if (preL != null)
							{
								preL.Text = prefix;
								preL.Visible = prefix.Length > 0;
								preL.ForeColor = dimFg;
								preL.Font = URL_FONT;
							}
							if (selL != null)
							{
								selL.Text = selDigits;
								selL.Visible = true;
								selL.ForeColor = bracketActiveColor;
								selL.Font = URL_BOLD_FONT;
								selL.BackColor = subSelBg;
							}
							if (sufL != null)
							{
								sufL.Text = suffix;
								sufL.Visible = suffix.Length > 0;
								sufL.ForeColor = dimFg;
								sufL.Font = URL_FONT;
							}
						}
						else
						{
							if (bl != null)
							{
								bl.Visible = true;
								bl.ForeColor = bracketColor;
								bl.Font = URL_FONT;
							}
							if (br != null)
							{
								br.Visible = true;
								br.ForeColor = bracketColor;
								br.Font = URL_FONT;
							}
							if (preL != null)
							{
								preL.Visible = false;
							}
							if (selL != null)
							{
								selL.Text = num2;
								selL.Visible = true;
								selL.ForeColor = DarkText;
								selL.Font = URL_FONT;
								selL.BackColor = Color.Transparent;
							}
							if (sufL != null)
							{
								sufL.Visible = false;
							}
						}
					}
					else
					{
						if (bl != null)
						{
							bl.Visible = false;
						}
						if (br != null)
						{
							br.Visible = false;
						}
						if (preL != null)
						{
							preL.Visible = false;
						}
						if (sufL != null)
						{
							sufL.Visible = false;
						}
						if (selL != null)
						{
							selL.Text = seg3.OriginalText;
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
						tagL.ForeColor = (isSel ? bracketActiveColor : GrayText);
						tagL.Font = (isSel ? URL_BOLD_FONT : URL_FONT);
					}
					rad?.Invalidate();
				}
			}
		}
		static string CleanUrlToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
			{
				return "";
			}
			string t = token.Trim().Trim().Trim('"', ' ', '`', '\t');
			int btStart = t.IndexOf('`');
			if (btStart >= 0)
			{
				int btEnd = t.IndexOf('`', btStart + 1);
				if (btEnd > btStart)
				{
					t = t.Substring(btStart + 1, btEnd - btStart - 1);
				}
			}
			return t.Trim().Trim('"', ' ', '`');
		}
		ContextMenuStrip CreateInputContextMenu(TextBox targetTb)
		{
		ContextMenuStrip cms = new ContextMenuStrip
		{
			Font = GetFont(9.5f),
			BackColor = theme.Surface,
			ForeColor = theme.TextPrimary
		};
		AnimatedMenuRenderer cmsRenderer = new AnimatedMenuRenderer(theme);
		cms.Renderer = cmsRenderer;
		cmsRenderer.Register(cms);
			ToolStripMenuItem miCut = new ToolStripMenuItem("剪切(T)", null, delegate
			{
				targetTb.Cut();
			})
			{
				ShortcutKeyDisplayString = "Ctrl+X"
			};
			ToolStripMenuItem miCopy = new ToolStripMenuItem("复制(C)", null, delegate
			{
				targetTb.Copy();
			})
			{
				ShortcutKeyDisplayString = "Ctrl+C"
			};
			ToolStripMenuItem miPaste = new ToolStripMenuItem("粘贴(P)", null, delegate
			{
				targetTb.Paste();
			})
			{
				ShortcutKeyDisplayString = "Ctrl+V"
			};
			ToolStripMenuItem miSelectAll = new ToolStripMenuItem("全选(A)", null, delegate
			{
				targetTb.SelectAll();
			})
			{
				ShortcutKeyDisplayString = "Ctrl+A"
			};
			ToolStripMenuItem miClear = new ToolStripMenuItem("清空(L)", null, delegate
			{
				targetTb.Clear();
				targetTb.Focus();
			});
			cms.Items.AddRange(new ToolStripItem[6]
			{
				miCut,
				miCopy,
				miPaste,
				miSelectAll,
				new ToolStripSeparator(),
				miClear
			});
			cms.Opening += delegate
			{
				bool flag = targetTb.SelectionLength > 0;
				bool flag2 = Clipboard.ContainsText();
				miCut.Enabled = flag && !targetTb.ReadOnly;
				miCopy.Enabled = flag;
				miPaste.Enabled = flag2 && !targetTb.ReadOnly;
				miSelectAll.Enabled = targetTb.TextLength > 0;
			};
			return cms;
		}
		Panel CreateNumPanel(int x, long initialVal, out TextBox outTextBox)
		{
			Panel panel = new Panel();
			panel.Location = At(x, 0);
			panel.Width = numPanelW;
			panel.Height = INPUT_HEIGHT;
			panel.BackColor = InputBg;
			panel.BorderStyle = BorderStyle.None;
			panel.Region = new Region(CreateRoundedRectPath(new Rectangle(0, 0, numPanelW, INPUT_HEIGHT), SX(6)));
			panel.Paint += delegate(object s, PaintEventArgs pe)
			{
				Graphics graphics = pe.Graphics;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using Pen pen = new Pen(isDark ? Color.FromArgb(100, 105, 115) : Color.FromArgb(130, 135, 145), 2.5f);
				graphics.DrawPath(pen, CreateRoundedRectPath(new Rectangle(0, 0, numPanelW - 1, INPUT_HEIGHT - 1), SX(6)));
			};
			int btnW = INPUT_HEIGHT;
			Color numBtnBg = InputBg;
			Color numBtnHover = (isDark ? NumPadHover : Color.FromArgb(220, 225, 232));
			Color numBtnDown = (isDark ? NumPadDown : Color.FromArgb(205, 210, 220));
			Button btnMinus = new Button
			{
				Text = "−",
				Size = new Size(btnW, INPUT_HEIGHT),
				Location = At(0, 0),
				FlatStyle = FlatStyle.Flat,
				BackColor = numBtnBg,
				ForeColor = DarkText,
				Font = NUMPAD_BTN_FONT,
				Cursor = Cursors.Hand
			};
			btnMinus.FlatAppearance.BorderSize = 0;
			btnMinus.FlatAppearance.MouseOverBackColor = numBtnHover;
			btnMinus.FlatAppearance.MouseDownBackColor = numBtnDown;
			TextBox tb = new TextBox
			{
				Text = initialVal.ToString(),
				Location = At(btnW, (INPUT_HEIGHT - 24) / 2),
				Width = numPanelW - btnW * 2,
				Height = 24,
				BorderStyle = BorderStyle.None,
				Font = NUM_INPUT_FONT,
				ForeColor = DarkText,
				BackColor = InputBg,
				TextAlign = HorizontalAlignment.Center
			};
			tb.ContextMenuStrip = CreateInputContextMenu(tb);
			Button btnPlus = new Button
			{
				Text = "+",
				Size = new Size(btnW, INPUT_HEIGHT),
				Location = At(numPanelW - btnW, 0),
				FlatStyle = FlatStyle.Flat,
				BackColor = numBtnBg,
				ForeColor = DarkText,
				Font = NUMPAD_BTN_FONT,
				Cursor = Cursors.Hand
			};
			btnPlus.FlatAppearance.BorderSize = 0;
			btnPlus.FlatAppearance.MouseOverBackColor = numBtnHover;
			btnPlus.FlatAppearance.MouseDownBackColor = numBtnDown;
			btnMinus.Click += delegate
			{
				if (long.TryParse(tb.Text, out var result) && result > 0)
				{
					result--;
					tb.Text = result.ToString();
				}
				else
				{
					tb.Text = "0";
				}
			};
			btnPlus.Click += delegate
			{
				if (long.TryParse(tb.Text, out var result) && result < 9999999999L)
				{
					result++;
					tb.Text = result.ToString();
				}
				else if (!long.TryParse(tb.Text, out result))
				{
					tb.Text = "0";
				}
			};
			tb.KeyPress += delegate(object s, KeyPressEventArgs e)
			{
				if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
				{
					e.Handled = true;
				}
			};
			panel.Controls.Add(btnMinus);
			panel.Controls.Add(tb);
			panel.Controls.Add(btnPlus);
			outTextBox = tb;
			return panel;
		}
		void DoGenerate()
		{
			List<ChannelInfo> channels = new List<ChannelInfo>();
			if (isCustomRangeMode)
			{
				for (long v = customRangeStart; v <= customRangeEnd; v++)
				{
					string url = customUrlTemplate.Substring(0, customReplacePos) + PadNumber(v, customPadWidth, customPadZero) + customUrlTemplate.Substring(customReplacePos + customReplaceLen);
					channels.Add(new ChannelInfo
					{
						Name = "源" + (channels.Count + 1),
						Url = url,
						Group = "生成器",
						Status = "未检测",
						Visible = true
					});
				}
			}
			else
			{
				ScanSegInfo selSeg = segs[selectedSegIndex];
				int pathStart = segBaseUrl.IndexOf("://");
				if (pathStart >= 0)
				{
					pathStart = segBaseUrl.IndexOf('/', pathStart + 3);
				}
				if (pathStart < 0)
				{
					pathStart = 0;
				}
				string prefixPart = ((pathStart > 0) ? segBaseUrl.Substring(0, pathStart) : "");
				string pathPart = ((pathStart > 0) ? segBaseUrl.Substring(pathStart) : segBaseUrl);
				int primStart = selSeg.PathStart;
				int primLen = selSeg.OriginalText.Length;
				if (selSeg.Type == ScanSegType.Number)
				{
					string numStr = selSeg.OriginalText;
					int subStart = ((subSegStart != null && selectedSegIndex >= 0 && selectedSegIndex < subSegStart.Length) ? subSegStart[selectedSegIndex] : 0);
					int subLen = ((subSegLen != null && selectedSegIndex >= 0 && selectedSegIndex < subSegLen.Length) ? subSegLen[selectedSegIndex] : numStr.Length);
					if (subStart < 0)
					{
						subStart = 0;
					}
					if (subStart + subLen > numStr.Length)
					{
						subLen = numStr.Length - subStart;
					}
					if (subLen <= 0)
					{
						subStart = 0;
						subLen = numStr.Length;
					}
					primStart = selSeg.PathStart + subStart;
					primLen = subLen;
					for (long v2 = fromVal; v2 <= toVal; v2++)
					{
						string replText = PadNumber(v2, segPadWidth, segPadZero);
						string newPath = pathPart.Substring(0, primStart) + replText + pathPart.Substring(primStart + primLen);
						int deltaLen = replText.Length - primLen;
						string baseName = "源" + (channels.Count + 1);
						AddChannelWithResVariants(channels, prefixPart, newPath, selSeg, baseName, deltaLen);
					}
				}
				else
				{
					foreach (string val in selectedTextValues ?? new List<string>())
					{
						string newPath2 = pathPart.Substring(0, primStart) + val + pathPart.Substring(primStart + primLen);
						int deltaLen2 = val.Length - primLen;
						string baseName2 = GetChannelDisplayName(val, selSeg.Type);
						AddChannelWithResVariants(channels, prefixPart, newPath2, selSeg, baseName2, deltaLen2);
					}
				}
			}
			if (channels.Count > 10000)
			{
				DarkMessageBox.Show("生成的源数量超过10000，请缩小范围", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				generatedChannels = channels;
				dlg.DialogResult = DialogResult.OK;
				dlg.Close();
			}
		}
		static string ExtractUrlFromText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			string[] lines = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (text.Contains("#EXTINF"))
			{
				for (int i = 0; i < lines.Length; i++)
				{
					if (lines[i].Trim().StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
					{
						for (int j = i + 1; j < lines.Length; j++)
						{
							string ul = CleanUrlToken(lines[j]);
							if (!ul.StartsWith("#") && IsUrl(ul))
							{
								return ul;
							}
						}
					}
				}
			}
			string[] array = lines;
			for (int k = 0; k < array.Length; k++)
			{
				string line = array[k].Trim();
				if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
				{
					int commaIdx = line.IndexOf(',');
					if (commaIdx > 0)
					{
						string afterComma = line.Substring(commaIdx + 1);
						string cleaned = CleanUrlToken(afterComma);
						if (IsUrl(cleaned))
						{
							return cleaned;
						}
						Match btMatch = Regex.Match(afterComma, "`([^`]+)`");
						if (btMatch.Success && IsUrl(btMatch.Groups[1].Value.Trim()))
						{
							return btMatch.Groups[1].Value.Trim();
						}
					}
					string wholeLine = CleanUrlToken(line);
					if (IsUrl(wholeLine))
					{
						return wholeLine;
					}
				}
			}
			array = lines;
			for (int k = 0; k < array.Length; k++)
			{
				string url = Regex.Match(array[k], "(https?://[^\\s`\"<>]+)").Groups[1].Value;
				if (!string.IsNullOrWhiteSpace(url) && IsUrl(url.Trim()))
				{
					return url.Trim();
				}
			}
			return text;
		}
		static string GetChannelDisplayName(string key, ScanSegType type)
		{
			if (type == ScanSegType.CctvChannel)
			{
				switch (key)
				{
				case "cctv4k":
					return "CCTV-4K";
				case "cctv8k":
					return "CCTV-8K";
				case "cctv5p":
					return "CCTV-5+";
				default:
				{
					string numPart = key.Substring(4);
					return "CCTV-" + numPart;
				}
				}
			}
			foreach (KeyValuePair<string, string> kv in PayChannelList)
			{
				if (kv.Key == key)
				{
					return kv.Value;
				}
			}
			foreach (KeyValuePair<string, string> kv2 in WsChannelList)
			{
				if (kv2.Key == key)
				{
					return kv2.Value;
				}
			}
			foreach (KeyValuePair<string, string> kv3 in MovieChannelList)
			{
				if (kv3.Key == key)
				{
					return kv3.Value;
				}
			}
			return key;
		}
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
		static bool IsUrl(string s)
		{
			if (string.IsNullOrWhiteSpace(s))
			{
				return false;
			}
			if (!s.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase))
			{
				return s.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		void MakeDraggable(Control c)
		{
			c.MouseDown += delegate(object s, MouseEventArgs e)
			{
				if (e.Button == MouseButtons.Left)
				{
					ReleaseCapture();
					SendMessage(dlg.Handle, 161, 2, 0);
				}
			};
		}
		static string PadNumber(long num, int width, bool padZero)
		{
			string s = num.ToString();
			if (padZero && s.Length < width)
			{
				return s.PadLeft(width, '0');
			}
			return s;
		}
		static List<ChannelInfo> ParseChannelList(string text)
		{
			List<ChannelInfo> result = new List<ChannelInfo>();
			if (string.IsNullOrWhiteSpace(text))
			{
				return result;
			}
			string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string pendingName = null;
			string pendingGroup = "";
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string line = array2[i].Trim();
				if (!string.IsNullOrWhiteSpace(line))
				{
					if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
					{
						string info = line.Substring(8);
						int ci = info.LastIndexOf(',');
						string attrs = ((ci >= 0) ? info.Substring(0, ci) : info);
						string chName = ((ci >= 0) ? info.Substring(ci + 1).Trim().Trim('"', ' ', '`') : "");
						Match gm = Regex.Match(attrs, "group-title\\s*=\\s*\"([^\"]*)\"");
						string grp = (gm.Success ? gm.Groups[1].Value.Trim() : "");
						Match tnm = Regex.Match(attrs, "tvg-name\\s*=\\s*\"([^\"]*)\"");
						if (tnm.Success && !string.IsNullOrWhiteSpace(tnm.Groups[1].Value))
						{
							string tn = tnm.Groups[1].Value.Trim().Trim('"', ' ', '`');
							if (!string.IsNullOrWhiteSpace(tn) && string.IsNullOrWhiteSpace(chName))
							{
								chName = tn;
							}
						}
						if (string.IsNullOrWhiteSpace(chName))
						{
							chName = "生成器导入";
						}
						pendingName = chName;
						pendingGroup = grp;
					}
					else if (!line.StartsWith("#"))
					{
						string name = null;
						string url = null;
						int commaIdx = line.LastIndexOf(',');
						if (commaIdx > 0 && commaIdx < line.Length - 1)
						{
							string before = line.Substring(0, commaIdx).Trim().Trim('"', ' ', '`', '\t', '，');
							string cleaned = CleanUrlToken(line.Substring(commaIdx + 1));
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
								string before2 = line.Substring(0, tabIdx).Trim().Trim('"', ' ', '`');
								string cleaned2 = CleanUrlToken(line.Substring(tabIdx + 1));
								if (IsUrl(cleaned2) && !string.IsNullOrWhiteSpace(before2))
								{
									name = before2;
									url = cleaned2;
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
							if (string.IsNullOrWhiteSpace(name))
							{
								name = pendingName ?? $"源{result.Count + 1}";
							}
							result.Add(new ChannelInfo
							{
								Name = name,
								Url = url,
								Group = (string.IsNullOrEmpty(pendingGroup) ? "生成器导入" : pendingGroup),
								Status = "未检测",
								Visible = true
							});
							pendingName = null;
							pendingGroup = "";
						}
					}
				}
			}
			return result;
		}
		static bool ParseManualBracketUrl(string url, out string error, out string cleanUrl, out long bracketNum, out int replacePos, out int replaceLen)
		{
			error = "";
			cleanUrl = "";
			bracketNum = 0L;
			replacePos = (replaceLen = 0);
			Regex regex = RxBraceSingle;
			Regex rangePattern = RxBraceRange;
			MatchCollection bracketMatches = regex.Matches(url);
			MatchCollection rangeMatches = rangePattern.Matches(url);
			if (bracketMatches.Count == 0 && rangeMatches.Count == 0)
			{
				return false;
			}
			if (rangeMatches.Count > 0)
			{
				return false;
			}
			if (bracketMatches.Count > 1)
			{
				error = "每次只能框选一个数字段（仅允许一对{数字}）";
				return false;
			}
			Match m = bracketMatches[0];
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
		void StyleBtn(Button b)
		{
			b.FlatAppearance.BorderSize = 1;
			b.FlatAppearance.BorderColor = GrayBorder;
			b.FlatAppearance.MouseOverBackColor = NumPadHover;
			b.FlatAppearance.MouseDownBackColor = NumPadDown;
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
			StyleRoundButton(btn, SX(8));
		}
		void UpdateStepUI()
		{
			step1Panel.Dock = DockStyle.None;
			step1Panel.Visible = false;
			step2Panel.Dock = DockStyle.None;
			step2Panel.Visible = false;
			step3Panel.Dock = DockStyle.None;
			step3Panel.Visible = false;
			Panel active = ((currentStep == 1) ? step1Panel : ((currentStep != 2) ? step3Panel : step2Panel));
			active.Dock = DockStyle.Fill;
			active.Visible = true;
			active.BringToFront();
			stepIndicator.Invalidate();
			btnPrev.Visible = currentStep > 1;
			if (isCustomRangeMode)
			{
				btnAction.Text = "开始生成";
			}
			else
			{
				btnAction.Text = ((currentStep == 3) ? "开始生成" : "下一步 (N) →");
			}
		}
		static bool ValidateCustomRangeUrl(string url, out string error, out long start, out long end, out int padW, out bool padZero, out int replacePos, out int replaceLen, out string template)
		{
			error = "";
			start = (end = 0L);
			padW = 0;
			padZero = false;
			replacePos = (replaceLen = 0);
			template = "";
			Regex regex = RxBracketRange;
			Regex rangePattern2 = RxBraceRange;
			Match m = null;
			MatchCollection mc1 = regex.Matches(url);
			MatchCollection mc2 = rangePattern2.Matches(url);
			int totalMatches = mc1.Count + mc2.Count;
			if (totalMatches == 0)
			{
				return false;
			}
			if (totalMatches > 1)
			{
				error = "每次只能配置一个变量范围（仅允许一对[数字-数字]或{数字-数字}）";
				return false;
			}
			m = ((mc1.Count != 1) ? mc2[0] : mc1[0]);
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
			if (!Uri.IsWellFormedUriString(url.Substring(0, m.Index) + "12345" + url.Substring(m.Index + m.Length), UriKind.Absolute))
			{
				error = "URL格式不正确，请检查地址";
				return false;
			}
			return true;
		}
	}

	private bool IsWebView2Supported()
	{
		try
		{
			if (!CheckWindowsVersionSupported())
			{
				return false;
			}
			if (CheckEdgeBrowserInstalled())
			{
				return true;
			}
			if (CheckWebView2LoaderExists())
			{
				return true;
			}
			if (CheckWebView2FromRegistry())
			{
				return true;
			}
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
			RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", writable: false);
			if (key != null)
			{
				string productName = key.GetValue("ProductName") as string;
				string currentBuild = key.GetValue("CurrentBuild") as string;
				key.Close();
				if (!string.IsNullOrEmpty(productName) && productName.Contains("Windows 11"))
				{
					return true;
				}
				if (!string.IsNullOrEmpty(currentBuild) && int.TryParse(currentBuild, out var build))
				{
					return build >= 17763;
				}
			}
			Version osVersion = Environment.OSVersion.Version;
			if (osVersion.Major >= 10 && osVersion.Build >= 17763)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool CheckWebView2FromRegistry()
	{
		string[] array = new string[4] { "SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", "SOFTWARE\\WOW6432Node\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", "SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{D20EA4E1-3957-407C-9457-4CA219C63F57}", "SOFTWARE\\WOW6432Node\\Microsoft\\EdgeUpdate\\Clients\\{D20EA4E1-3957-407C-9457-4CA219C63F57}" };
		foreach (string path in array)
		{
			try
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(path, writable: false);
				if (key != null)
				{
					string value = key.GetValue("pv") as string;
					key.Close();
					if (!string.IsNullOrEmpty(value))
					{
						return true;
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			RegistryKey key2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", writable: false);
			if (key2 != null)
			{
				string value2 = key2.GetValue("pv") as string;
				key2.Close();
				if (!string.IsNullOrEmpty(value2))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private bool CheckEdgeBrowserInstalled()
	{
		try
		{
			RegistryKey edgeKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Edge", writable: false);
			if (edgeKey != null)
			{
				string value = edgeKey.GetValue("Version") as string;
				edgeKey.Close();
				if (!string.IsNullOrEmpty(value))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			string programFilesEdge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe");
			string programFilesX86Edge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe");
			if (File.Exists(programFilesEdge) || File.Exists(programFilesX86Edge))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool CheckWebView2LoaderExists()
	{
		try
		{
			string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
			string syswow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
			string[] array = new string[4]
			{
				Path.Combine(system32, "WebView2Loader.dll"),
				Path.Combine(syswow64, "WebView2Loader.dll"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "EdgeWebView2", "Application", "WebView2Loader.dll"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView2", "Application", "WebView2Loader.dll")
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (File.Exists(array[i]))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private List<string> CheckRuntimeDependencies()
	{
		List<string> missingDependencies = new List<string>();
		try
		{
			bool num = CheckVcRuntimeInstalled("x86");
			bool x64Installed = CheckVcRuntimeInstalled("x64");
			if (!num)
			{
				missingDependencies.Add("Microsoft Visual C++ 2015-2022 运行时 (x86)");
			}
			if (!x64Installed)
			{
				missingDependencies.Add("Microsoft Visual C++ 2015-2022 运行时 (x64)");
			}
		}
		catch
		{
		}
		return missingDependencies;
	}

	private bool CheckVcRuntimeInstalled(string arch)
	{
		try
		{
			string[] array = new string[3]
			{
				"SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\" + arch,
				"SOFTWARE\\WOW6432Node\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\" + arch,
				"SOFTWARE\\Classes\\Installer\\Dependencies\\Microsoft.VS.VC_RuntimeAdditionalVSU_" + ((arch == "x64") ? "amd64" : "x86") + ",v14"
			};
			foreach (string path in array)
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(path, writable: false);
				if (key != null)
				{
					key.Close();
					return true;
				}
			}
			if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msvcp140.dll")))
			{
				return true;
			}
			string vcDllX86 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "msvcp140.dll");
			if (arch == "x86" && File.Exists(vcDllX86))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool InstallWebView2Runtime()
	{
		try
		{
			string downloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
			string tempFile = Path.Combine(Path.GetTempPath(), "WebView2RuntimeInstaller.exe");
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
				client.DownloadFile(downloadUrl, tempFile);
			}
			Process process = Process.Start(new ProcessStartInfo(tempFile)
			{
				Arguments = "/install /quiet /norestart",
				Verb = "runas",
				UseShellExecute = true,
				CreateNoWindow = true
			});
			if (process != null)
			{
				process.WaitForExit();
				return process.ExitCode == 0;
			}
		}
		catch
		{
		}
		return InstallWebView2RuntimeViaPowerShell();
	}

	private bool InstallWebView2RuntimeViaPowerShell()
	{
		try
		{
			string tempFile = Path.Combine(Path.GetTempPath(), "WebView2RuntimeInstaller.exe");
			string downloadScript = "\n[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;\n$url = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';\n$output = '" + tempFile + "';\nInvoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing;\nif (Test-Path $output) { Start-Process $output -ArgumentList '/install /quiet /norestart' -Wait; exit $LASTEXITCODE }\nelse { exit 1 }\n";
			using Process proc = new Process();
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
		catch
		{
		}
		return false;
	}

	private bool InstallVcRuntime()
	{
		try
		{
			string vcUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
			string tempFile = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
				client.DownloadFile(vcUrl, tempFile);
			}
			Process process = Process.Start(new ProcessStartInfo(tempFile)
			{
				Arguments = "/install /quiet /norestart",
				Verb = "runas",
				UseShellExecute = true,
				CreateNoWindow = true
			});
			if (process != null)
			{
				process.WaitForExit();
				return process.ExitCode == 0;
			}
		}
		catch
		{
		}
		return InstallVcRuntimeViaPowerShell();
	}

	private bool InstallVcRuntimeViaPowerShell()
	{
		try
		{
			string tempFile = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");
			string script = "\n[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;\n$url = 'https://aka.ms/vs/17/release/vc_redist.x64.exe';\n$output = '" + tempFile + "';\nInvoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing;\nif (Test-Path $output) { Start-Process $output -ArgumentList '/install /quiet /norestart' -Wait; exit $LASTEXITCODE }\nelse { exit 1 }\n";
			using Process proc = new Process();
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
		catch
		{
		}
		return false;
	}

	private SearchMode ShowModeSelectionDialog()
	{
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Form dlg = new Form
		{
			Text = "选择搜索模式",
			StartPosition = FormStartPosition.Manual,
			MaximizeBox = false,
			MinimizeBox = false,
			ShowInTaskbar = false,
			ClientSize = new Size(SX(600), SY(500)),
			KeyPreview = true,
			Icon = this.Icon
		};
		var ctx = NeonChrome.Apply(dlg, pal, "选择搜索模式", dpiScale);
		int ox = ctx.Margin, oy = ctx.Margin + ctx.TitleHeight;
		Point At(int x, int y) => new Point(x - ox, y);
		Color PanelBg = pal.PanelBg;
		Color TextPrimary = pal.Label;
		Color TextSecondary = pal.Muted;
		Color BorderColor = pal.Border;
		Color PrimaryColor = pal.Neon;
		CenterForm(dlg, this);
		SearchMode result = (SearchMode)(-1);
		bool isConfirmed = false;
		dlg.FormClosing += delegate
		{
			if (!isConfirmed)
			{
				result = (SearchMode)(-1);
			}
		};
		RadioButton rbBrowser = new RadioButton
		{
			Text = "\ud83c\udf10 浏览器模式",
			Font = GetFont(14f),
			ForeColor = TextPrimary,
			BackColor = PanelBg,
			Location = At(SX(30), SY(30)),
			Size = new Size(SX(540), SY(40)),
			Checked = true,
			TextAlign = ContentAlignment.MiddleLeft,
			Cursor = Cursors.Hand
		};
		ctx.Body.Controls.Add(rbBrowser);
		Label lblBrowserDesc = new Label
		{
			Text = "使用系统默认浏览器打开网络空间搜索引擎",
			Font = GetFont(12f),
			ForeColor = TextSecondary,
			Location = At(SX(30), SY(78)),
			Size = new Size(SX(540), SY(32)),
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};
		ctx.Body.Controls.Add(lblBrowserDesc);
		RadioButton rbWebView2 = new RadioButton
		{
			Text = "\ud83d\udda5\ufe0f WebView2窗口模式",
			Font = GetFont(14f),
			ForeColor = TextPrimary,
			BackColor = PanelBg,
			Location = At(SX(30), SY(120)),
			Size = new Size(SX(540), SY(40)),
			TextAlign = ContentAlignment.MiddleLeft,
			Cursor = Cursors.Hand
		};
		ctx.Body.Controls.Add(rbWebView2);
		Label lblWebView2Desc = new Label
		{
			Text = "在应用内窗口中使用Edge内核显示搜索页面",
			Font = GetFont(12f),
			ForeColor = TextSecondary,
			Location = At(SX(30), SY(168)),
			Size = new Size(SX(540), SY(32)),
			TextAlign = ContentAlignment.MiddleCenter,
			Cursor = Cursors.Hand
		};
		ctx.Body.Controls.Add(lblWebView2Desc);
		rbBrowser.Click += delegate
		{
			rbBrowser.Checked = true;
		};
		lblBrowserDesc.Click += delegate
		{
			rbBrowser.Checked = true;
		};
		rbWebView2.Click += delegate
		{
			rbWebView2.Checked = true;
		};
		lblWebView2Desc.Click += delegate
		{
			rbWebView2.Checked = true;
		};
		Label lblStatus = new Label
		{
			Text = "",
			Font = GetFont(12f),
			ForeColor = (isDark ? Color.FromArgb(100, 255, 100) : Color.Green),
			Location = At(SX(30), SY(210)),
			Size = new Size(SX(540), SY(36)),
			AutoSize = false
		};
		ctx.Body.Controls.Add(lblStatus);
		if (!IsWebView2Supported())
		{
			lblStatus.Text = "⚠\ufe0f 系统未安装WebView2运行库，选择此模式将自动下载安装";
			lblStatus.ForeColor = (theme.WarnColor);
		}
		else
		{
			lblStatus.Text = "✓ WebView2运行库已安装";
			lblStatus.ForeColor = (isDark ? Color.FromArgb(100, 255, 100) : Color.Green);
		}
		int btnSpacing = (dlg.ClientSize.Width - 120 - 120) / 3;
		NeonButton btnOK = new NeonButton
		{
			Text = "确定",
			Size = new Size(SX(120), SY(36)),
			Font = GetFont(10f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			IsPrimary = true,
			GradientStart = pal.Neon,
			GradientEnd = pal.Neon2,
			TextColorX = pal.PrimaryText,
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			Radius = 6,
			Location = At(btnSpacing, SY(340))
		};
		ctx.Body.Controls.Add(btnOK);
		NeonButton btnCancel = new NeonButton
		{
			Text = "取消",
			Size = new Size(SX(120), SY(36)),
			Font = GetFont(10f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			IsPrimary = false,
			BorderColor = pal.Border,
			GlowColor = pal.Glow,
			GlowEnabled = !pal.SuppressGlow,
			Radius = 6,
			Location = At(btnSpacing + SX(120) + btnSpacing, SY(340))
		};
		ctx.Body.Controls.Add(btnCancel);
		btnOK.Click += delegate
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
		btnCancel.Click += delegate
		{
			isConfirmed = false;
			result = (SearchMode)(-1);
			dlg.Close();
		};
		dlg.ShowDialog(this);
		if (!isConfirmed)
		{
			return (SearchMode)(-1);
		}
		return result;
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
			if (mode == (SearchMode)(-1))
			{
				return;
			}
		}
		if (mode == SearchMode.WebView2)
		{
			if (!CheckWindowsVersionSupported())
			{
				DarkMessageBox.Show("您的系统版本过低，WebView2需要Windows 10 1809或更高版本。将自动使用浏览器模式打开。", "系统版本不支持", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				mode = SearchMode.Browser;
			}
			else
			{
				List<string> missingDeps = CheckRuntimeDependencies();
				if (missingDeps.Count > 0)
				{
					string depList = string.Join("\n", missingDeps);
					if (DarkMessageBox.Show("检测到缺少以下运行库依赖：\n" + depList + "\n\n是否自动下载安装？", "缺少运行库", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						Form progressForm = CreateProgressForm("正在安装运行库依赖...");
						progressForm.Show(this);
						Application.DoEvents();
						bool num = InstallVcRuntime();
						progressForm.Close();
						if (!num)
						{
							DarkMessageBox.Show("VC++运行时安装失败，WebView2可能无法正常工作。将使用浏览器模式打开。", "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
					if (DarkMessageBox.Show("系统未安装WebView2运行库，是否自动下载安装？", "缺少WebView2运行库", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						Form progressForm2 = CreateProgressForm("正在下载并安装WebView2运行库，请稍候...");
						progressForm2.Show(this);
						Application.DoEvents();
						bool num2 = InstallWebView2Runtime();
						progressForm2.Close();
						if (!num2)
						{
							DarkMessageBox.Show("WebView2运行库安装失败，将使用浏览器模式打开", "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

	private Form CreateProgressForm(string message)
	{
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Form obj = new Form
		{
			Text = "安装进度",
			StartPosition = FormStartPosition.Manual,
			FormBorderStyle = FormBorderStyle.None,
			MaximizeBox = false,
			MinimizeBox = false,
			ShowInTaskbar = false,
			ClientSize = new Size(SX(400), SY(160)),
			BackColor = pal.FormBg
		};
		var ctx = NeonChrome.Apply(obj, pal, "安装进度", dpiScale);
		Point At(int x, int yy) => new Point(x, yy);
		Label lblProgress = new Label
		{
			Text = message,
			Font = GetFont(10f),
			ForeColor = pal.GhostText,
			Location = At(SX(20), SY(40)),
			Size = new Size(SX(360), SY(24)),
			BackColor = pal.PanelBg
		};
		ctx.Body.Controls.Add(lblProgress);
		return obj;
	}

	private void ShowBrowserSearchDialog()
	{
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		Color PanelBg = theme.Surface;
		Color SurfaceBg = theme.Surface;
		Color TextPrimary = theme.TextPrimary;
		Color TextSecondary = theme.TextSecondary;
		Color BorderColor = theme.Border;
		Color PrimaryColor = theme.Primary;
		FormWindowState originalState = base.WindowState;
		Rectangle originalBounds = base.Bounds;
		Form dlg = new Form
		{
			Text = "规则搜索",
			StartPosition = FormStartPosition.Manual,
			FormBorderStyle = FormBorderStyle.Sizable,
			MaximizeBox = true,
			MinimizeBox = true,
			ShowInTaskbar = true,
			BackColor = PanelBg,
			ClientSize = new Size(SX(900), SY(550)),
			KeyPreview = true,
			Icon = this.Icon
		};
		SetFormDarkModeTitleBar(dlg, isDark);
		CenterForm(dlg, this);
		Hide();
		dlg.Closed += delegate
		{
			Show();
			base.WindowState = originalState;
			if (originalState != FormWindowState.Maximized)
			{
				base.Bounds = originalBounds;
			}
			Activate();
			Refresh();
		};
		Panel topBar = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(60),
			BackColor = SurfaceBg
		};
		dlg.Controls.Add(topBar);
		Panel mainPanel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = PanelBg,
			Padding = new Padding(SX(20))
		};
		dlg.Controls.Add(mainPanel);
		Label lblTitle = new Label
		{
			Text = "规则搜索",
			Font = GetFont(12f, FontStyle.Bold),
			ForeColor = TextPrimary,
			Location = At(SX(16), 0),
			Size = new Size(SX(200), SY(48)),
			TextAlign = ContentAlignment.MiddleLeft,
			BackColor = Color.Transparent
		};
		topBar.Controls.Add(lblTitle);
		ComboBox cboSearchRule = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Size = new Size(SX(120), SY(32)),
			FlatStyle = FlatStyle.Flat,
			BackColor = (theme.Surface),
			ForeColor = TextPrimary,
			Font = GetFont(10f),
			Cursor = Cursors.Hand,
			Location = At(SX(220), SY(14))
		};
		cboSearchRule.Items.AddRange(new object[3] { "智慧桌面", "智慧光迅", "华视美达" });
		cboSearchRule.SelectedIndex = 0;
		cboSearchRule.Paint += delegate(object s, PaintEventArgs e)
		{
			ComboBox comboBox = (ComboBox)s;
			using Pen pen = new Pen(BorderColor);
			e.Graphics.DrawRectangle(pen, 0, 0, comboBox.Width - 1, comboBox.Height - 1);
		};
		topBar.Controls.Add(cboSearchRule);
		Dictionary<string, string> searchRules = new Dictionary<string, string>
		{
			{ "智慧桌面", "body=\"/iptv/live/zh_cn.js\"" },
			{ "智慧光迅", "body=\"ZHGXTV\"" },
			{ "华视美达", "body=\"华视美达\"" }
		};
		Dictionary<string, string> searchEngines = new Dictionary<string, string>(EngineHomeUrls);
		string selectedEngine = "FOFA";
		Button btnSearch = new Button
		{
			Text = "打开搜索",
			Size = new Size(SX(100), SY(32)),
			FlatStyle = FlatStyle.Flat,
			BackColor = PrimaryColor,
			ForeColor = Color.White,
			Font = GetFont(10f, FontStyle.Bold),
			Cursor = Cursors.Hand,
			Location = At(SX(780), SY(14)),
			Visible = showSearchButton
		};
		btnSearch.FlatAppearance.BorderSize = 0;
		btnSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, PrimaryColor.R + 20), Math.Min(255, PrimaryColor.G + 20), Math.Min(255, PrimaryColor.B + 20));
		StyleRoundButton(btnSearch, 6, null, 0, "dynamic");
		btnSearch.Click += delegate
		{
			string key = cboSearchRule.SelectedItem?.ToString() ?? "智慧桌面";
			string fileName = BuildSearchUrl(selectedEngine, key);
			try
			{
				Process.Start(new ProcessStartInfo(fileName)
				{
					UseShellExecute = true
				});
			}
			catch
			{
			}
		};
		topBar.Controls.Add(btnSearch);
		btnSearchRef = btnSearch;
		topBar.Paint += delegate(object s, PaintEventArgs e)
		{
			Panel panel = (Panel)s;
			using Pen pen = new Pen(BorderColor);
			e.Graphics.DrawLine(pen, 0, SY(47), panel.Width, SY(47));
		};
		Panel listPanel = new Panel
		{
			Location = At(SX(20), SY(80)),
			Size = new Size(SX(860), SY(420)),
			BackColor = SurfaceBg,
			BorderStyle = BorderStyle.FixedSingle
		};
		mainPanel.Controls.Add(listPanel);
		Color hoverColor = ControlPaint.Light(PanelBg, 0.3f);
		int y = 10;
		foreach (KeyValuePair<string, string> engine in searchEngines)
		{
			Panel itemPanel = new Panel
			{
				Location = At(SX(10), y),
				Size = new Size(SX(840), SY(55)),
				BackColor = Color.Transparent
			};
			listPanel.Controls.Add(itemPanel);
			itemPanel.MouseEnter += delegate
			{
				itemPanel.BackColor = hoverColor;
			};
			itemPanel.MouseLeave += delegate
			{
				itemPanel.BackColor = Color.Transparent;
			};
			RadioButton rbEngine = new RadioButton
			{
				Text = engine.Key,
				Size = new Size(SX(100), SY(36)),
				Location = At(0, SY(7)),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Transparent,
				ForeColor = theme.TextPrimary,
				Font = GetFont(10f, FontStyle.Bold),
				Cursor = Cursors.Hand,
				Checked = (engine.Key == selectedEngine)
			};
			rbEngine.CheckedChanged += delegate
			{
				if (rbEngine.Checked)
				{
					selectedEngine = engine.Key;
				}
			};
			itemPanel.Controls.Add(rbEngine);
			Label lblUrl = new Label
			{
				Text = engine.Value,
				Size = new Size(SX(630), SY(36)),
				Location = At(SX(130), SY(10)),
				Font = GetFont(10f),
				ForeColor = TextSecondary,
				TextAlign = ContentAlignment.MiddleLeft
			};
			itemPanel.Controls.Add(lblUrl);
			Button btnGo = new Button
			{
				Text = "访问",
				Size = new Size(SX(70), SY(36)),
				Location = At(SX(760), SY(10)),
				FlatStyle = FlatStyle.Flat,
				BackColor = PrimaryColor,
				ForeColor = Color.White,
				Font = GetFont(9f, FontStyle.Bold),
				Cursor = Cursors.Hand
			};
			btnGo.FlatAppearance.BorderSize = 0;
			btnGo.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, PrimaryColor.R + 20), Math.Min(255, PrimaryColor.G + 20), Math.Min(255, PrimaryColor.B + 20));
			StyleRoundButton(btnGo, 6, null, 0, "dynamic");
			btnGo.Click += delegate
			{
				try
				{
					Process.Start(new ProcessStartInfo(engine.Value)
					{
						UseShellExecute = true
					});
				}
				catch
				{
				}
			};
			itemPanel.Controls.Add(btnGo);
			y += 60;
		}
		dlg.ShowDialog(this);
	}

	private void ShowWebView2SearchDialog()
	{
		try
		{
			bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
			Color PanelBg = theme.Surface;
			Color SurfaceBg = theme.Surface;
			Color TextPrimary = theme.TextPrimary;
			Color TextSecondary = theme.TextSecondary;
			Color BorderColor = theme.Border;
			FormWindowState originalState = base.WindowState;
			Rectangle originalBounds = base.Bounds;
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
				ClientSize = base.ClientSize,
				Icon = this.Icon
			};
			SetFormDarkModeTitleBar(dlg, isDark);
			CenterForm(dlg, this);
			Hide();
			bool wasMaximized = false;
			Action SetRoundedRegion = delegate
			{
				if (dlg.WindowState == FormWindowState.Maximized)
				{
					dlg.Region = null;
					return;
				}
				int num2 = 12;
				using GraphicsPath graphicsPath = new GraphicsPath();
				Rectangle rectangle = new Rectangle(0, 0, dlg.Width, dlg.Height);
				graphicsPath.AddArc(rectangle.X, rectangle.Y, num2, num2, 180f, 90f);
				graphicsPath.AddArc(rectangle.X + rectangle.Width - num2, rectangle.Y, num2, num2, 270f, 90f);
				graphicsPath.AddArc(rectangle.X + rectangle.Width - num2, rectangle.Y + rectangle.Height - num2, num2, num2, 0f, 90f);
				graphicsPath.AddArc(rectangle.X, rectangle.Y + rectangle.Height - num2, num2, num2, 90f, 90f);
				graphicsPath.CloseAllFigures();
				dlg.Region = new Region(graphicsPath);
			};
		dlg.ResizeEnd += delegate
		{
			if (dlg.WindowState == FormWindowState.Maximized)
				{
					wasMaximized = true;
				}
				else if (dlg.WindowState == FormWindowState.Normal && wasMaximized)
				{
					int num2 = Screen.PrimaryScreen.WorkingArea.Width;
					int num3 = Screen.PrimaryScreen.WorkingArea.Height;
					int num4 = (num2 - dlg.Width) / 2;
					int num5 = (num3 - dlg.Height) / 2;
					dlg.Location = At(num4, num5);
					wasMaximized = false;
				}
			};
			dlg.Closed += delegate
			{
				Show();
				base.WindowState = originalState;
				if (originalState != FormWindowState.Maximized)
				{
					base.Bounds = originalBounds;
					CenterToScreen();
				}
				Activate();
				Refresh();
			};
			int BORDER_WIDTH = 8;
			int STATUS_BAR_H = 28;
			Panel webViewContainer = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = theme.Bg
			};
			Microsoft.Web.WebView2.WinForms.WebView2 webView2 = null;
			try
			{
				webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
				webView2.Dock = DockStyle.Fill;
				webView2.Visible = true;
				webViewContainer.Controls.Add(webView2);
			}
			catch (Exception ex)
			{
				DarkMessageBox.Show("WebView2控件创建失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			Panel leftBorder = new Panel
			{
				Width = BORDER_WIDTH,
				BackColor = BorderColor,
				Dock = DockStyle.Left
			};
			Panel rightBorder = new Panel
			{
				Width = BORDER_WIDTH,
				BackColor = BorderColor,
				Dock = DockStyle.Right
			};
			Panel statusBar = new Panel
			{
				Height = STATUS_BAR_H,
				BackColor = SurfaceBg,
				Dock = DockStyle.Bottom,
				Padding = new Padding(8, 0, 8, 0)
			};
			int GLASS_NAV_H = 42;
			int GLASS_STATUS_H = 30;
			// 颜色字段化：切换主题时由 ApplyWebViewNavTheme() 刷新这些字段，Paint 委托读字段而非闭包
			_glassNavBg = Color.FromArgb(210, theme.Surface);
			_glassStatusBg = Color.FromArgb(210, theme.Surface);
			_glassBorder = Color.FromArgb(60, theme.Border);
			_chipNormalBg = Color.FromArgb(30, theme.TextSecondary);
			_chipHoverBg = Color.FromArgb(50, theme.TextSecondary);
			_addrBarBg = Color.FromArgb(40, theme.TextSecondary);
			_addrBarBorder = Color.FromArgb(50, theme.Border);
			_addrBarOpaqueColor = Color.FromArgb(255, theme.Surface.R, theme.Surface.G, theme.Surface.B);
			Color okGreen = Color.FromArgb(255, 40, 180, 100);
			Color okGreenLight = Color.FromArgb(255, 60, 200, 120);
			double GetLuminance(Color c)
			{
				double r = c.R / 255.0; double g = c.G / 255.0; double b = c.B / 255.0;
				r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
				g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
				b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
				return 0.2126 * r + 0.7152 * g + 0.0722 * b;
			}
			Color GetHighContrastText(Color bg)
			{
				double lum = GetLuminance(bg);
				return (lum < 0.5) ? Color.White : Color.FromArgb(255, 30, 30, 30);
			}
			_chipTextColor = GetHighContrastText(_glassNavBg);
			_statusTextColor = GetHighContrastText(_glassStatusBg);
			_addrTextColor = GetHighContrastText(_addrBarOpaqueColor);
			UpdateWebViewDynamicState();
			Panel navPanel = new Panel
			{
				Height = GLASS_NAV_H,
				BackColor = _glassNavBg,
				Top = -GLASS_NAV_H,
				Width = dlg.ClientSize.Width
			};
			typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(navPanel, true, null);
			navPanel.Paint += delegate(object sp, PaintEventArgs pe)
			{
				Graphics g = pe.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				Rectangle full = new Rectangle(0, 0, navPanel.Width, navPanel.Height);
				// 动态/毛玻璃主题：用主题渐变光斑填充导航栏，呼应主窗口流动极光
				if (_webViewDynamic && _webViewStops != null && _webViewStops.Count >= 2)
				{
					using LinearGradientBrush gb = new LinearGradientBrush(full, _webViewStops[0], _webViewStops[1], LinearGradientMode.Horizontal);
					ColorBlend cb = new ColorBlend();
					cb.Colors = _webViewStops.ToArray();
					float[] positions = new float[_webViewStops.Count];
					for (int i = 0; i < _webViewStops.Count; i++)
					{
						positions[i] = (float)i / (_webViewStops.Count - 1);
					}
					cb.Positions = positions;
					gb.InterpolationColors = cb;
					g.FillRectangle(gb, full);
				}
				else
				{
					using SolidBrush sb = new SolidBrush(_glassNavBg);
					g.FillRectangle(sb, full);
				}
				Rectangle r = new Rectangle(0, navPanel.Height - 1, navPanel.Width, 1);
				using LinearGradientBrush brush = new LinearGradientBrush(r, Color.FromArgb(0, _glassBorder), _glassBorder, LinearGradientMode.Horizontal);
				g.FillRectangle(brush, r);
			};
			navPanel.Resize += delegate
			{
				navPanel.Invalidate();
			};
			statusBar.BackColor = _glassStatusBg;
			_webViewStatusBarRef = statusBar;
			Label lblStatusUrl = new Label
			{
				Text = "",
				Font = GetFont(9f),
				ForeColor = _statusTextColor,
				BackColor = _glassStatusBg,
				Size = new Size(400, GLASS_STATUS_H),
				Location = At(12, 0),
				TextAlign = ContentAlignment.MiddleLeft,
				AutoSize = false
			};
			Label lblStatusIp = new Label
			{
				Text = "● IP提取: 关",
				Font = GetFont(9f),
				ForeColor = _statusTextColor,
				BackColor = _glassStatusBg,
				Size = new Size(150, GLASS_STATUS_H),
				Location = At(dlg.ClientSize.Width - BORDER_WIDTH * 2 - 150, 0),
				TextAlign = ContentAlignment.MiddleRight,
				AutoSize = false
			};
			Label lblStatusEngine = new Label
			{
				Text = "引擎: FOFA",
				Font = GetFont(9f),
				ForeColor = _statusTextColor,
				BackColor = _glassStatusBg,
				Size = new Size(120, GLASS_STATUS_H),
				Location = At(dlg.ClientSize.Width - BORDER_WIDTH * 2 - 290, 0),
				TextAlign = ContentAlignment.MiddleRight,
				AutoSize = false
			};
			_webViewLblStatusUrl = lblStatusUrl;
			_webViewLblStatusIp = lblStatusIp;
			_webViewLblStatusEngine = lblStatusEngine;
			statusBar.Controls.Add(lblStatusUrl);
			statusBar.Controls.Add(lblStatusIp);
			statusBar.Controls.Add(lblStatusEngine);
			dlg.Controls.Add(webViewContainer);
			dlg.Controls.Add(leftBorder);
			dlg.Controls.Add(rightBorder);
			dlg.Controls.Add(statusBar);
			dlg.Controls.Add(navPanel);
			navPanel.BringToFront();
			webViewStatusUrl = lblStatusUrl;
			System.Windows.Forms.Timer navTimer = new System.Windows.Forms.Timer();
			navTimer.Interval = 100;
			bool isNavVisible = false;
			bool isDropdownOpen = false;
			int hideCountdown = 0;
			Action ShowNav = delegate
			{
				if (!isNavVisible)
				{
					navPanel.Top = 0;
					isNavVisible = true;
				}
				hideCountdown = 0;
			};
			Action HideNav = delegate
			{
				if (isNavVisible)
				{
					navPanel.Top = -GLASS_NAV_H;
					isNavVisible = false;
				}
			};
			navTimer.Tick += delegate
			{
				if (isDropdownOpen)
				{
					hideCountdown = 0;
				}
				else
				{
					Point mousePosition = Control.MousePosition;
					Point point = dlg.PointToScreen(new Point(0, 0));
					Point point2 = new Point(point.X, point.Y);
					bool num2 = mousePosition.X >= point2.X && mousePosition.X <= point2.X + navPanel.Width && mousePosition.Y >= point2.Y && mousePosition.Y <= point2.Y + GLASS_NAV_H;
					bool flag = mousePosition.X >= point.X && mousePosition.X <= point.X + dlg.Width && mousePosition.Y >= point.Y && mousePosition.Y <= point.Y + 30;
					if (num2 || flag)
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
				}
			};
			navTimer.Enabled = true;
			int chipH = GLASS_NAV_H - 14;
			int chipW = 68;
			int chipGap = 6;
			int engineStartX = 10;
			string[] engineNames = new string[6] { "FOFA", "Quake", "Hunter", "ZoomEye", "Shodan", "Censys" };
			ComboBox cboEngine = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Size = new Size(0, 0),
				FlatStyle = FlatStyle.Flat,
				BackColor = (theme.Surface),
				ForeColor = TextPrimary,
				Font = GetFont(11f),
				Cursor = Cursors.Hand,
				Location = At(-100, -100),
				Visible = false
			};
			cboEngine.Items.AddRange(engineNames);
			cboEngine.SelectedIndex = 0;
			cboEngine.DropDown += delegate
			{
				isDropdownOpen = true;
			};
			cboEngine.DropDownClosed += delegate
			{
				isDropdownOpen = false;
			};
			navPanel.Controls.Add(cboEngine);
			Panel chipContainer = new Panel
			{
				AutoSize = false,
				Location = At(engineStartX, (GLASS_NAV_H - chipH) / 2),
				Height = chipH,
				Width = engineNames.Length * chipW + (engineNames.Length - 1) * chipGap,
				BackColor = Color.Transparent
			};
			navPanel.Controls.Add(chipContainer);
			_webViewChipContainer = chipContainer;
			List<Panel> engineChips = new List<Panel>();
			_webViewEngineChips = engineChips;
			for (int ei = 0; ei < engineNames.Length; ei++)
			{
				string engName = engineNames[ei];
				int chipX = ei * (chipW + chipGap);
				Panel chip = new Panel
				{
					Size = new Size(chipW, chipH),
					Location = At(chipX, 0),
					BackColor = Color.Transparent,
					Cursor = Cursors.Hand
				};
				MakeRounded(chip, chipH / 2);
				int chipIndex = ei;
				bool chipHover = false;
				chip.MouseEnter += delegate
				{
					chipHover = true;
					chip.Invalidate();
				};
				chip.MouseLeave += delegate
				{
					chipHover = false;
					chip.Invalidate();
				};
				chip.Click += delegate
				{
					cboEngine.SelectedIndex = chipIndex;
				};
				chip.Paint += delegate(object sc, PaintEventArgs pe)
				{
					Graphics g = pe.Graphics;
					g.SmoothingMode = SmoothingMode.AntiAlias;
					Rectangle cr = new Rectangle(0, 0, chip.Width - 1, chip.Height - 1);
					int radius = chipH / 2;
					bool active = (cboEngine.SelectedIndex == chipIndex);
					using (GraphicsPath path = GetRoundedPath(cr, radius))
					{
						if (active)
						{
							// 激活态：蓝紫渐变 + 发光描边（固定色，不随主题变）
							using (LinearGradientBrush bgBrush = new LinearGradientBrush(cr, Color.FromArgb(255, 79, 140, 255), Color.FromArgb(255, 139, 92, 246), 45f))
							{
								g.FillPath(bgBrush, path);
							}
							using (Pen glowPen = new Pen(Color.FromArgb(60, 79, 140, 255), 2f))
							{
								g.DrawPath(glowPen, path);
							}
						}
						else
						{
							// 非激活态：读字段 _chipHoverBg/_chipNormalBg（随主题刷新）
							Color bg = chipHover ? _chipHoverBg : _chipNormalBg;
							using (SolidBrush bgBrush = new SolidBrush(bg))
							{
								g.FillPath(bgBrush, path);
							}
							using (Pen borderPen = new Pen(Color.FromArgb(40, theme.Border), 1f))
							{
								g.DrawPath(borderPen, path);
							}
						}
					}
					// 文字色读字段 _chipTextColor（随主题刷新）
					Color textColor = active ? Color.White : _chipTextColor;
					using (SolidBrush textBrush = new SolidBrush(textColor))
					{
						StringFormat sf = new StringFormat
						{
							Alignment = StringAlignment.Center,
							LineAlignment = StringAlignment.Center
						};
						g.DrawString(engName, GetFont(10f, active ? FontStyle.Bold : FontStyle.Regular), textBrush, cr, sf);
					}
				};
				engineChips.Add(chip);
				chipContainer.Controls.Add(chip);
			}
			chipContainer.Width = engineNames.Length * chipW + (engineNames.Length - 1) * chipGap;
			ComboBox cboSearchRule = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Size = new Size(0, 0),
				FlatStyle = FlatStyle.Flat,
				BackColor = (theme.Surface),
				ForeColor = TextPrimary,
				Font = GetFont(11f),
				Cursor = Cursors.Hand,
				Location = At(-100, -100),
				Visible = false
			};
			cboSearchRule.Items.AddRange(new object[3] { "智慧桌面", "智慧光迅", "华视美达" });
			cboSearchRule.SelectedIndex = 0;
			cboSearchRule.DropDown += delegate
			{
				isDropdownOpen = true;
			};
			cboSearchRule.DropDownClosed += delegate
			{
				isDropdownOpen = false;
			};
			navPanel.Controls.Add(cboSearchRule);
			int ruleChipW = 100;
			int ruleStartX = engineStartX + chipContainer.Width + 12;
			Panel ruleChip = new Panel
			{
				Size = new Size(ruleChipW, chipH),
				Location = At(ruleStartX, (GLASS_NAV_H - chipH) / 2),
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			MakeRounded(ruleChip, chipH / 2);
			bool ruleHover = false;
			ruleChip.MouseEnter += delegate
			{
				ruleHover = true;
				ruleChip.Invalidate();
			};
			ruleChip.MouseLeave += delegate
			{
				ruleHover = false;
				ruleChip.Invalidate();
			};
			ruleChip.Click += delegate
			{
				if (cboSearchRule.SelectedIndex < cboSearchRule.Items.Count - 1)
				{
					cboSearchRule.SelectedIndex++;
				}
				else
				{
					cboSearchRule.SelectedIndex = 0;
				}
			};
			ruleChip.Paint += delegate(object sc, PaintEventArgs pe)
			{
				Graphics g = pe.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				Rectangle cr = new Rectangle(0, 0, ruleChip.Width - 1, ruleChip.Height - 1);
				int radius = chipH / 2;
				using (GraphicsPath path = GetRoundedPath(cr, radius))
				{
					// 读字段 _chipHoverBg/_chipNormalBg（随主题刷新）
					Color bg = ruleHover ? _chipHoverBg : _chipNormalBg;
					using (SolidBrush bgBrush = new SolidBrush(bg))
					{
						g.FillPath(bgBrush, path);
					}
					using (Pen borderPen = new Pen(Color.FromArgb(40, theme.Border), 1f))
					{
						g.DrawPath(borderPen, path);
					}
				}
				// 文字色读字段 _chipTextColor（随主题刷新）
				using (SolidBrush textBrush = new SolidBrush(_chipTextColor))
				{
					StringFormat sf = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center
					};
					string ruleText = (cboSearchRule.SelectedItem?.ToString() ?? "智慧桌面") + " ▾";
					g.DrawString(ruleText, GetFont(10f), textBrush, cr, sf);
				}
			};
			navPanel.Controls.Add(ruleChip);
			_webViewRuleChip = ruleChip;
			_webViewCboSearchRule = cboSearchRule;
			Dictionary<string, string> searchRules = new Dictionary<string, string>
			{
				{ "智慧桌面", "body=\"/iptv/live/zh_cn.js\"" },
				{ "智慧光迅", "body=\"ZHGXTV\"" },
				{ "华视美达", "body=\"华视美达\"" }
			};
			int addrBarH = chipH + 8;
			int addrBarW = 420;
			int addrBarX = ruleStartX + ruleChipW + 16;
			Panel addrBarHost = new Panel
			{
				Size = new Size(addrBarW, addrBarH),
				Location = At(addrBarX, (GLASS_NAV_H - addrBarH) / 2),
				BackColor = _addrBarOpaqueColor
			};
			typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(addrBarHost, true, null);
			MakeRounded(addrBarHost, addrBarH / 2);
			TextBox txtUrl = new TextBox
			{
				Size = new Size(addrBarW - 24, addrBarH - 4),
				Font = GetFont(10.5f),
				ForeColor = _addrTextColor,
				BackColor = _addrBarOpaqueColor,
				BorderStyle = BorderStyle.None,
				Location = At(12, 2),
				ReadOnly = true,
				Cursor = Cursors.Default
			};
			addrBarHost.Controls.Add(txtUrl);
			_webViewAddrBarHost = addrBarHost;
			webViewTxtUrl = txtUrl;
			bool addrHover = false;
			addrBarHost.MouseEnter += delegate
			{
				addrHover = true;
				addrBarHost.Invalidate();
			};
			addrBarHost.MouseLeave += delegate
			{
				addrHover = false;
				addrBarHost.Invalidate();
			};
			txtUrl.MouseEnter += delegate
			{
				addrHover = true;
				addrBarHost.Invalidate();
			};
			txtUrl.MouseLeave += delegate
			{
				addrHover = false;
				addrBarHost.Invalidate();
			};
			addrBarHost.Paint += delegate(object ap, PaintEventArgs pe)
			{
				Graphics g = pe.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				Rectangle cr = new Rectangle(0, 0, addrBarHost.Width - 1, addrBarHost.Height - 1);
				int radius = addrBarH / 2;
				using (GraphicsPath path = GetRoundedPath(cr, radius))
				{
					// 读字段 _addrBarBg（随主题刷新）
					Color bg = addrHover ? Color.FromArgb(50, theme.TextSecondary) : _addrBarBg;
					using (SolidBrush bgBrush = new SolidBrush(bg))
					{
						g.FillPath(bgBrush, path);
					}
					// 读字段 _addrBarBorder（随主题刷新）
					using (Pen borderPen = new Pen(_addrBarBorder, 1f))
					{
						g.DrawPath(borderPen, path);
					}
				}
				// 读字段 _addrTextColor（随主题刷新）
				using (SolidBrush iconBrush = new SolidBrush(_addrTextColor))
				{
					StringFormat sf = new StringFormat
					{
						Alignment = StringAlignment.Near,
						LineAlignment = StringAlignment.Center
					};
					g.DrawString("🔒", GetFont(9f), iconBrush, new Rectangle(4, 0, 16, addrBarH), sf);
				}
			};
			navPanel.Controls.Add(addrBarHost);
			dlg.FormClosing += delegate
			{
				try
				{
					navTimer.Enabled = false;
					navTimer.Dispose();
				}
				catch
				{
				}
			};
			int btnExtractW = 100;
			int btnExtractH = addrBarH;
			int btnExtractX = addrBarX + addrBarW + 16;
			if (btnExtractX + btnExtractW > dlg.ClientSize.Width - 20)
			{
				btnExtractX = dlg.ClientSize.Width - btnExtractW - 12;
			}
			Panel btnExtractIp = new Panel
			{
				Size = new Size(btnExtractW, btnExtractH),
				Location = At(btnExtractX, (GLASS_NAV_H - btnExtractH) / 2),
				BackColor = Color.Transparent,
				Cursor = Cursors.Hand
			};
			MakeRounded(btnExtractIp, btnExtractH / 2);
			bool extractHover = false;
			bool extractPressed = false;
			btnExtractIp.MouseEnter += delegate
			{
				extractHover = true;
				btnExtractIp.Invalidate();
			};
			btnExtractIp.MouseLeave += delegate
			{
				extractHover = false;
				extractPressed = false;
				btnExtractIp.Invalidate();
			};
			btnExtractIp.MouseDown += delegate
			{
				extractPressed = true;
				btnExtractIp.Invalidate();
			};
			btnExtractIp.MouseUp += delegate
			{
				extractPressed = false;
				btnExtractIp.Invalidate();
			};
			btnExtractIp.Paint += delegate(object ep, PaintEventArgs pe)
			{
				Graphics g = pe.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				Rectangle cr = new Rectangle(0, 0, btnExtractIp.Width - 1, btnExtractIp.Height - 1);
				int radius = btnExtractH / 2;
				bool active = autoExtractIpPort;
				using (GraphicsPath path = GetRoundedPath(cr, radius))
				{
					if (active)
					{
						// 激活态：绿色渐变（固定色，IP 提取的语义色）
						Color c1 = Color.FromArgb(255, 61, 214, 140);
						Color c2 = Color.FromArgb(255, 40, 180, 100);
						using (LinearGradientBrush bgBrush = new LinearGradientBrush(cr, c1, c2, 45f))
						{
							if (extractPressed)
							{
								bgBrush.RotateTransform(90f);
							}
							g.FillPath(bgBrush, path);
						}
						using (Pen glowPen = new Pen(Color.FromArgb(50, 61, 214, 140), 2f))
						{
							g.DrawPath(glowPen, path);
						}
					}
					else
					{
						// 非激活态：读字段 theme（Paint 时读字段，随主题刷新）
						Color bg = extractHover ? Color.FromArgb(40, theme.TextSecondary) : Color.FromArgb(25, theme.TextSecondary);
						using (SolidBrush bgBrush = new SolidBrush(bg))
						{
							g.FillPath(bgBrush, path);
						}
						using (Pen borderPen = new Pen(Color.FromArgb(30, 61, 214, 140), 1f))
						{
							g.DrawPath(borderPen, path);
						}
					}
				}
				Color textColor = active ? Color.White : GetHighContrastText(Color.FromArgb(25, theme.TextSecondary));
				using (SolidBrush textBrush = new SolidBrush(textColor))
				{
					StringFormat sf = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center
					};
					string btnText = "⚡ IP提取";
					g.DrawString(btnText, GetFont(10f, FontStyle.Bold), textBrush, cr, sf);
				}
			};
			navPanel.Controls.Add(btnExtractIp);
			_webViewBtnExtractIp = btnExtractIp;
			btnExtractIp.Click += async delegate
			{
				autoExtractIpPort = !autoExtractIpPort;
				btnExtractIp.Invalidate();
				lblStatusIp.Text = (autoExtractIpPort ? "● IP提取: 就绪" : "● IP提取: 关");
				lblStatusIp.ForeColor = (autoExtractIpPort ? okGreen : _statusTextColor);
				SaveConfig();
				if (webView2 != null)
				{
					btnExtractIp.Enabled = false;
					btnExtractIp.Invalidate();
					try
					{
						string extractJs = "(function() {   var allText = '';   allText += document.body.innerText || '';   allText += ' ' + document.documentElement.outerHTML || '';   try {     var iframes = document.querySelectorAll('iframe');     for (var k=0; k<iframes.length; k++) {       try { if (iframes[k].contentDocument) { allText += ' ' + iframes[k].contentDocument.body.innerText; } } catch(e) {}     }   } catch(e) {}   var valid = {};   var ipv4Regex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b/g;   var matches = allText.match(ipv4Regex) || [];   for (var i=0; i<matches.length; i++) {     valid[matches[i]] = true;   }   var urlIpRegex = /(?:http|https):\\/\\/(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?::(\\d{2,5}))?(?:\\/|\\?|$)/gi;   var urlMatches = allText.match(urlIpRegex) || [];   for (var i=0; i<urlMatches.length; i++) {     var urlMatch = urlMatches[i].replace(/^https?:\\/\\//i, '');     var portMatch = urlMatch.match(/:(\\d{2,5})$/);     var ip = urlMatch.replace(/:\\d{2,5}$/, '');     if (portMatch) { valid[ip + ':' + portMatch[1]] = true; }     else { valid[ip] = true; }   }   var ipWithPortRegex = /\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):(\\d{2,5})\\b/g;   var portMatches = allText.match(ipWithPortRegex) || [];   for (var i=0; i<portMatches.length; i++) {     valid[portMatches[i]] = true; }   var ipList = Object.keys(valid);   ipList = ipList.filter(function(ip) {     var parts = ip.split(':')[0].split('.');     if (parts.length !== 4) return false;     for (var j=0; j<4; j++) {       var n = parseInt(parts[j]);       if (isNaN(n) || n < 0 || n > 255) return false;     }     return true;   });   return JSON.stringify(ipList); })()";
						if (webView2.CoreWebView2 != null)
						{
							string testResult = await webView2.CoreWebView2.ExecuteScriptAsync("document.body.innerText.length.toString()");
							string htmlResult = await webView2.CoreWebView2.ExecuteScriptAsync("(function(){var t=document.body.innerText.substring(0,500);return t.indexOf('.')>=0?'FOUND_DOT':'NO_DOT';})()");
							string ipResult = await webView2.CoreWebView2.ExecuteScriptAsync(extractJs);
							if (!string.IsNullOrEmpty(ipResult))
							{
								List<string> ips = new List<string>();
								try
								{
									ipResult = ipResult.Trim();
									foreach (Match item in Regex.Matches(ipResult, "(\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}):(\\d{2,5})"))
									{
										string ip = item.Groups[1].Value;
										string port = item.Groups[2].Value;
										string[] parts = ip.Split('.');
										if (parts.Length == 4)
										{
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
											if (isValid && ipParts[0] != 10 && (ipParts[0] != 172 || ipParts[1] < 16 || ipParts[1] > 31) && (ipParts[0] != 192 || ipParts[1] != 168) && ipParts[0] != 127 && (ipParts[0] != 0 || ipParts[1] != 0 || ipParts[2] != 0 || ipParts[3] != 0) && (ipParts[0] != 255 || ipParts[1] != 255 || ipParts[2] != 255 || ipParts[3] != 255) && (ipParts[0] != 169 || ipParts[1] != 254) && int.TryParse(port, out var portNum) && portNum >= 1 && portNum <= 65535)
											{
												string fullIp = ip + ":" + port;
												if (!ips.Contains(fullIp))
												{
													ips.Add(fullIp);
												}
											}
										}
									}
								}
								catch
								{
								}
								if (ips.Count > 0)
								{
									using (StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "extracted_ips.txt"), append: true, Encoding.UTF8))
									{
										string currentSrc = webView2.Source?.ToString() ?? "";
										sw.WriteLine($"# 提取时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} 来源: {currentSrc} 共{ips.Count}条");
										foreach (string ip2 in ips)
										{
											sw.WriteLine(ip2);
										}
									}
									DateTime extractTime = DateTime.Now;
									string ruleName = cboSearchRule.SelectedItem?.ToString() ?? "智慧桌面";
									int addedCount = 0;
									hasSearchPlatformData = true;
									if (!autoParseLink)
									{
												foreach (string ipPort in ips)
												{
													string[] parts2 = ipPort.Split(':');
													if (parts2.Length == 2)
													{
														string ip3 = parts2[0];
														string port2 = parts2[1];
														string rootHttp = "http://" + ip3 + ":" + port2;
														if (ruleName == "智慧光迅")
														{
															string url = rootHttp + "/ZHGXTV/Public/json/live_interface.txt";
															if (!allChannels.Any((ChannelInfo c) => c.Url == url))
															{
																allChannels.Add(new ChannelInfo
																{
																	Name = ipPort,
																	Url = url,
																	Group = "解析待处理",
																	Status = "待解析",
																	ParseDateTime = extractTime
																});
																addedCount++;
															}
														}
														else if (ruleName == "华视美达")
														{
															Tuple<int, int> scanConfig = await ShowScanConfigDialogAsync();
															if (scanConfig != null)
															{
																int scanCount = scanConfig.Item1;
																int threadCount = scanConfig.Item2;
																Invoke((Action)delegate
																{
																	Show();
																	Activate();
																});
																if (lblProgressText != null && lblProgressText.IsHandleCreated)
																{
																	lblProgressText.Invoke((Action)delegate
																	{
																		lblProgressText.Text = "华视美达扫描进度:";
																		lblProgressText.Refresh();
																	});
																}
																if (lblPercent != null && lblPercent.IsHandleCreated)
																{
																	lblPercent.Invoke((Action)delegate
																	{
																		lblPercent.Text = "0%";
																		lblPercent.Refresh();
																	});
																}
																if (statusBarRef != null && statusBarRef.IsHandleCreated)
																{
																	statusBarRef.Invoke((Action)delegate
																	{
																		LayoutStatusBar(statusBarRef);
																	});
																}
																dlg.Invoke((Action)delegate
																{
																	dlg.Hide();
																});
																ConcurrentBag<Tuple<string, string>> validResults = new ConcurrentBag<Tuple<string, string>>();
																List<int> cidList = Enumerable.Range(1, scanCount).ToList();
																int processedCount = 0;
																await Task.Run(delegate
																{
																	//IL_000e: Unknown result type (might be due to invalid IL or missing references)
																	//IL_0018: Expected O, but got Unknown
																	HttpClient httpClient2 = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 32 });
																	try
																	{
																		httpClient2.Timeout = TimeSpan.FromSeconds(2.5);
																		((HttpHeaders)httpClient2.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
																		Parallel.ForEach(cidList, new ParallelOptions
																		{
																			MaxDegreeOfParallelism = threadCount
																		}, delegate(int num2)
																		{
																			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
																			//IL_003f: Expected O, but got Unknown
																			string text = $"{rootHttp}/newlive/live/hls/{num2}/live.m3u8";
																			try
																			{
																				if (httpClient2.SendAsync(new HttpRequestMessage(HttpMethod.Head, text)).Result.IsSuccessStatusCode)
																				{
																					HttpResponseMessage result3 = httpClient2.GetAsync(text).Result;
																					if (result3.IsSuccessStatusCode)
																					{
																						string result4 = result3.Content.ReadAsStringAsync().Result;
																						if (!string.IsNullOrEmpty(result4) && result4.Contains("#EXTM3U"))
																						{
																							validResults.Add(Tuple.Create(text, result4));
																						}
																					}
																					return;
																				}
																			}
																			catch
																			{
																			}
																			try
																			{
																				HttpResponseMessage result5 = httpClient2.GetAsync(text).Result;
																				if (result5.IsSuccessStatusCode)
																				{
																					string result6 = result5.Content.ReadAsStringAsync().Result;
																					if (!string.IsNullOrEmpty(result6) && result6.Contains("#EXTM3U"))
																					{
																						validResults.Add(Tuple.Create(text, result6));
																					}
																				}
																			}
																			catch
																			{
																			}
																			int num3 = Interlocked.Increment(ref processedCount);
																			int pct = (int)((double)num3 * 100.0 / (double)scanCount);
																			if (lblPercent != null && !lblPercent.IsDisposed)
																			{
																				try
																				{
																					lblPercent.Invoke((Action)delegate
																					{
																						if (lblPercent != null && !lblPercent.IsDisposed)
																						{
																							lblPercent.Text = $"{pct}%";
																						}
																						if (statusBarRef != null && !statusBarRef.IsDisposed)
																						{
																							progressBarWidth = statusBarRef.ClientSize.Width * pct / 100;
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
																					});
																				}
																				catch
																				{
																				}
																			}
																		});
																	}
																	finally
																	{
																		if (httpClient2 != null)
																		{
																			((IDisposable)httpClient2).Dispose();
																		}
																	}
																});
																if (lblProgressText != null && !lblProgressText.IsDisposed)
																{
																	lblProgressText.Invoke((Action)delegate
																	{
																		lblProgressText.Text = "华视美达扫描完成:";
																	});
																}
																if (lblPercent != null && !lblPercent.IsDisposed)
																{
																	lblPercent.Invoke((Action)delegate
																	{
																		lblPercent.Text = $"找到{validResults.Count}个";
																	});
																}
																if (statusBarRef != null)
																{
																	statusBarRef.Invoke((Action)delegate
																	{
																		LayoutStatusBar(statusBarRef);
																	});
																}
																foreach (Tuple<string, string> result in validResults)
																{
																	if (!allChannels.Any((ChannelInfo c) => c.Url == result.Item1))
																	{
																		string[] urlParts = result.Item1.Split('/');
																		string cid = ((urlParts.Length > 1) ? urlParts[urlParts.Length - 2] : "");
																		allChannels.Add(new ChannelInfo
																		{
																			Name = ipPort + "_CID" + cid,
																			Url = result.Item1,
																			Group = "解析待处理",
																			Status = "待解析",
																			ParseDateTime = extractTime
																		});
																		addedCount++;
																	}
																}
																if (lblProgressText != null && !lblProgressText.IsDisposed)
																{
																	lblProgressText.Invoke((Action)delegate
																	{
																		lblProgressText.Text = "检测进度:";
																	});
																}
																if (lblPercent != null && !lblPercent.IsDisposed)
																{
																	lblPercent.Invoke((Action)delegate
																	{
																		lblPercent.Text = "0%";
																	});
																}
																if (statusBarRef != null)
																{
																	statusBarRef.Invoke((Action)delegate
																	{
																		LayoutStatusBar(statusBarRef);
																	});
																}
															}
														}
														else
														{
															string url2 = rootHttp + "/iptv/live/1000.json?key=txiptv";
															if (!allChannels.Any((ChannelInfo c) => c.Url == url2))
															{
																allChannels.Add(new ChannelInfo
																{
																	Name = ipPort,
																	Url = url2,
																	Group = "解析待处理",
																	Status = "待解析",
																	ParseDateTime = extractTime
																});
																addedCount++;
															}
														}
													}
												}
											}
											else
											{
												HttpClient httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 32 });
												try
												{
													httpClient.Timeout = TimeSpan.FromSeconds(5.0);
													((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
													foreach (string ipPort in ips)
													{
														string[] parts3 = ipPort.Split(':');
														if (parts3.Length == 2)
														{
															string ip4 = parts3[0];
															string port3 = parts3[1];
															string rootHttp2 = "http://" + ip4 + ":" + port3;
															if (ruleName == "智慧光迅")
															{
																string url3 = rootHttp2 + "/ZHGXTV/Public/json/live_interface.txt";
																try
																{
																	HttpResponseMessage resp = await httpClient.GetAsync(url3);
																	if (resp.IsSuccessStatusCode)
																	{
																		string content = await resp.Content.ReadAsStringAsync();
																		if (!string.IsNullOrEmpty(content))
																		{
																			ParseZhgxTv(content, url3, extractTime);
																			addedCount++;
																		}
																	}
																}
																catch
																{
																}
															}
															else if (ruleName == "华视美达")
															{
																Tuple<int, int> scanConfig2 = await ShowScanConfigDialogAsync();
																if (scanConfig2 != null)
																{
																	int scanCount2 = scanConfig2.Item1;
																	int threadCount2 = scanConfig2.Item2;
																	ConcurrentBag<Tuple<string, string>> validResults2 = new ConcurrentBag<Tuple<string, string>>();
																	List<int> cidList2 = Enumerable.Range(1, scanCount2).ToList();
																	await Task.Run(delegate
																	{
																		Parallel.ForEach(cidList2, new ParallelOptions
																		{
																			MaxDegreeOfParallelism = threadCount2
																		}, delegate(int num2)
																		{
																			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
																			//IL_0032: Expected O, but got Unknown
																			string text = $"{rootHttp2}/newlive/live/hls/{num2}/live.m3u8";
																			try
																			{
																				if (httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, text)).Result.IsSuccessStatusCode)
																				{
																					HttpResponseMessage result3 = httpClient.GetAsync(text).Result;
																					if (result3.IsSuccessStatusCode)
																					{
																						string result4 = result3.Content.ReadAsStringAsync().Result;
																						if (!string.IsNullOrEmpty(result4) && result4.Contains("#EXTM3U"))
																						{
																							validResults2.Add(Tuple.Create(text, result4));
																						}
																					}
																					return;
																				}
																			}
																			catch
																			{
																			}
																			try
																			{
																				HttpResponseMessage result5 = httpClient.GetAsync(text).Result;
																				if (result5.IsSuccessStatusCode)
																				{
																					string result6 = result5.Content.ReadAsStringAsync().Result;
																					if (!string.IsNullOrEmpty(result6) && result6.Contains("#EXTM3U"))
																					{
																						validResults2.Add(Tuple.Create(text, result6));
																					}
																				}
																			}
																			catch
																			{
																			}
																		});
																	});
																	foreach (Tuple<string, string> result2 in validResults2)
																	{
																		if (!allChannels.Any((ChannelInfo c) => c.Url == result2.Item1))
																		{
																			string[] urlParts2 = result2.Item1.Split('/');
																			string cid2 = ((urlParts2.Length > 1) ? urlParts2[urlParts2.Length - 2] : "");
																			allChannels.Add(new ChannelInfo
																			{
																				Name = ipPort + "_CID" + cid2,
																				Url = result2.Item1,
																				Group = "解析待处理",
																				Status = "待解析",
																				ParseDateTime = extractTime
																			});
																			addedCount++;
																		}
																	}
																}
															}
															else
															{
																string url3 = rootHttp2 + "/iptv/live/1000.json?key=txiptv";
																try
																{
																	HttpResponseMessage resp2 = await httpClient.GetAsync(url3);
																	if (resp2.IsSuccessStatusCode)
																	{
																		string content2 = await resp2.Content.ReadAsStringAsync();
																		if (!string.IsNullOrEmpty(content2))
																		{
																			ParseKutvJson(content2, url3, extractTime);
																			addedCount++;
																		}
																	}
																}
																catch
																{
																}
															}
														}
													}
												}
												finally
												{
													if (httpClient != null)
													{
														((IDisposable)httpClient).Dispose();
													}
												}
											}
											totalCount = allChannels.Count;
											RefreshGrid();
											UpdateEmptyState();
											UpdateActionButtonsVisibility();
											SaveChannelList();
											Show();
											dlg.Close();
											if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
											{
												lblDetected.Text = $"已检测: 0/{totalCount}";
												lblAvailable.Text = "可用: 0";
												lblPercent.Text = "0.00%";
												progressBarWidth = 0;
												RestoreLabelColors();
												statusBarRef.PerformLayout();
												LayoutStatusBar(statusBarRef);
												statusBarRef.Refresh();
											}
											if (!autoParseLink)
											{
												DarkMessageBox.Show($"已提取 {addedCount} 条链接到待解析列表\n请点击\"解析链接\"按钮进行解析", "提取完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
											}
											else
											{
												DarkMessageBox.Show($"解析完成！\n成功: {addedCount} 个IP\n请点击\"开始检测\"按钮验证链接有效性", "解析下载", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
											}
										}
										else
										{
											DarkMessageBox.Show("未在当前页面找到IP地址\n调试信息:\n文本长度: " + testResult + "\n包含点号: " + htmlResult + "\nJS返回: " + ipResult?.Substring(0, Math.Min(ipResult.Length, 200)), "IP提取", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
										}
									}
								}
					}
					catch (Exception ex3)
					{
						DarkMessageBox.Show("IP提取失败: " + ex3.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
					finally
					{
						btnExtractIp.Enabled = true;
						btnExtractIp.Invalidate();
					}
				}
			};
			Action UpdateWebViewSize = delegate
			{
				if (webView2 != null)
				{
					webView2.Dock = DockStyle.Fill;
				}
			};
			dlg.SizeChanged += delegate
			{
				navPanel.Width = dlg.ClientSize.Width;
				UpdateWebViewSize();
				lblStatusIp.Location = At(statusBar.Width - 130, 0);
				lblStatusEngine.Location = At(statusBar.Width - 280, 0);
				leftBorder.Invalidate();
				rightBorder.Invalidate();
				statusBar.Invalidate();
				navPanel.Invalidate();
			};
			Dictionary<string, string> searchEngines = new Dictionary<string, string>(EngineHomeUrls);
			string currentUrl = searchEngines["FOFA"];
			txtUrl.Text = currentUrl;
			webViewNavPanel = navPanel;
			webViewCboEngine = cboEngine;
			webViewTxtUrl = txtUrl;
			if (webView2 != null)
			{
				webView2.CoreWebView2InitializationCompleted += WebView2InitCompletedHandler;
				webView2.NavigationCompleted += WebView2NavCompletedHandler;
				webView2.WebMessageReceived += WebView2WebMessageReceivedHandler;
			}
			dlg.Load += async delegate
			{
				try
				{
					if (webView2 != null)
					{
						await webView2.EnsureCoreWebView2Async();
						if (webView2.CoreWebView2 != null)
						{
							webView2.CoreWebView2.Settings.IsScriptEnabled = true;
							webView2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
							webView2.CoreWebView2.Navigate(currentUrl);
						}
					}
				}
				catch (Exception ex3)
				{
					DarkMessageBox.Show("WebView2加载失败: " + ex3.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			};
			Action DoSearch = delegate
			{
				try
				{
					string key = cboEngine.SelectedItem?.ToString() ?? "FOFA";
					if (searchEngines.TryGetValue(key, out var value))
					{
						txtUrl.Text = value;
						if (webView2 != null)
						{
							if (webView2.CoreWebView2 != null)
							{
								webView2.CoreWebView2.Navigate(value);
							}
							else
							{
								webView2.Source = new Uri(value);
							}
						}
					}
				}
				catch (Exception ex3)
				{
					DarkMessageBox.Show("搜索失败: " + ex3.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			};
			cboEngine.SelectedIndexChanged += delegate
			{
				lblStatusEngine.Text = "引擎: " + cboEngine.SelectedItem;
				foreach (Panel chip in engineChips)
				{
					chip.Invalidate();
				}
				DoSearch();
			};
			cboSearchRule.SelectedIndexChanged += async delegate
			{
				ruleChip.Invalidate();
				try
				{
					string ruleName = cboSearchRule.SelectedItem?.ToString();
					if (!string.IsNullOrEmpty(ruleName) && webView2 != null)
					{
						string engineName = cboEngine.SelectedItem?.ToString() ?? "FOFA";
						string searchRule = BuildSearchQuery(engineName, ruleName);
						string escapedRule = searchRule.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");
						string js = "let targetInput = null; let allInputs = document.querySelectorAll('input[type=\"text\"], input[type=\"search\"], textarea'); allInputs.forEach(function(input) { input.value = ''; input.dispatchEvent(new Event('input')); input.dispatchEvent(new Event('change')); }); let searchSelectors = [   'input[placeholder*=\"搜索\"]',   'input[placeholder*=\"Search\"]',   'input[placeholder*=\"search\"]',   'input[placeholder*=\"query\"]',   'input[placeholder*=\"Query\"]',   'input[id*=\"search\"]',   'input[id*=\"Search\"]',   'input[name*=\"search\"]',   'input[name*=\"q\"]',   'input[class*=\"search\"]',   'input[class*=\"Search\"]' ]; for(let i=0; i<searchSelectors.length; i++) {   let el = document.querySelector(searchSelectors[i]);   if(el && el.offsetParent !== null) { targetInput = el; break; } } if(!targetInput && allInputs.length > 0) {   targetInput = allInputs[0]; } if(targetInput) {   targetInput.focus();   targetInput.value = '" + escapedRule + "';   targetInput.dispatchEvent(new Event('input', { bubbles: true }));   targetInput.dispatchEvent(new Event('change', { bubbles: true }));   targetInput.dispatchEvent(new Event('keyup', { bubbles: true }));   targetInput.dispatchEvent(new Event('keydown', { bubbles: true })); }";
						if (webView2.CoreWebView2 != null)
						{
							await webView2.CoreWebView2.ExecuteScriptAsync(js);
						}
					}
				}
				catch (Exception ex3)
				{
					DarkMessageBox.Show("自动搜索失败: " + ex3.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			};
			dlg.ShowDialog(this);
		}
		catch (Exception ex2)
		{
			Show();
			DarkMessageBox.Show("WebView2窗口初始化失败，将使用浏览器模式打开。\n\n错误信息: " + ex2.Message + "\n\n堆栈跟踪:\n" + ex2.StackTrace + "\n\n内部异常: " + (ex2.InnerException?.Message ?? "无"), "WebView2初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			ShowBrowserSearchDialog();
		}
	}

	private static readonly Dictionary<string, string> PlatformKeywords = new Dictionary<string, string>
	{
		{ "智慧桌面", "/iptv/live/zh_cn.js" },
		{ "智慧光迅", "ZHGXTV" },
		{ "华视美达", "华视美达" }
	};

	private static readonly Dictionary<string, string> EngineHomeUrls = new Dictionary<string, string>
	{
		{ "FOFA", "https://fofa.info/" },
		{ "Quake", "https://quake.360.net/" },
		{ "Hunter", "https://hunter.qianxin.com/" },
		{ "ZoomEye", "https://www.zoomeye.org/" },
		{ "Shodan", "https://www.shodan.io/" },
		{ "Censys", "https://search.censys.io/" }
	};

	private static readonly Dictionary<string, string> EngineSyntaxTemplates = new Dictionary<string, string>
	{
		{ "FOFA", "body=\"{0}\"" },
		{ "Quake", "response:\"{0}\"" },
		{ "Hunter", "web.body=\"{0}\"" },
		{ "ZoomEye", "body:\"{0}\"" },
		{ "Shodan", "http.html:\"{0}\"" },
		{ "Censys", "web.endpoints.http.body: \"{0}\"" }
	};

	private static string BuildSearchQuery(string engineName, string platformName)
	{
		string keyword = PlatformKeywords.ContainsKey(platformName) ? PlatformKeywords[platformName] : platformName;
		string template = EngineSyntaxTemplates.ContainsKey(engineName) ? EngineSyntaxTemplates[engineName] : EngineSyntaxTemplates["FOFA"];
		return string.Format(template, keyword);
	}

	private static string BuildSearchUrl(string engineName, string platformName)
	{
		string query = BuildSearchQuery(engineName, platformName);
		switch (engineName)
		{
		case "FOFA":
			return "https://fofa.info/result?qbase64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
		case "Quake":
			return "https://quake.360.net/quake/#/searchResult?query=" + Uri.EscapeDataString(query);
		case "Hunter":
			return "https://hunter.qianxin.com/#/search?search=" + Uri.EscapeDataString(query);
		case "ZoomEye":
			return "https://www.zoomeye.org/searchResult?q=" + Uri.EscapeDataString(query);
		case "Shodan":
			return "https://www.shodan.io/search?query=" + Uri.EscapeDataString(query);
		case "Censys":
			return "https://platform.censys.io/search?q=" + Uri.EscapeDataString(query);
		default:
			return "https://fofa.info/result?qbase64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
		}
	}

	private string GetSearchUrl(string baseUrl, string query)
	{
		string encodedQuery = Uri.EscapeDataString(query);
		if (baseUrl.Contains("fofa.info"))
		{
			return "https://fofa.info/result?qbase64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
		}
		if (baseUrl.Contains("quake.360.net"))
		{
			return "https://quake.360.net/quake/#/searchResult?query=" + encodedQuery;
		}
		if (baseUrl.Contains("hunter.qianxin.com"))
		{
			return "https://hunter.qianxin.com/#/search?search=" + encodedQuery;
		}
		if (baseUrl.Contains("zoomeye.org"))
		{
			return "https://www.zoomeye.org/searchResult?q=" + encodedQuery;
		}
		if (baseUrl.Contains("shodan.io"))
		{
			return "https://www.shodan.io/search?query=" + encodedQuery;
		}
		if (baseUrl.Contains("censys.io"))
		{
			return "https://platform.censys.io/search?q=" + encodedQuery;
		}
		return baseUrl;
	}

	private void AdjustToolbarColors(Panel navPanel, ComboBox cboEngine, TextBox txtUrl, Color pageBg)
	{
		try
		{
			bool isDarkPage = ((double)(int)pageBg.R * 0.299 + (double)(int)pageBg.G * 0.587 + (double)(int)pageBg.B * 0.114) / 255.0 < 0.5;
			Color toolbarBg = Color.FromArgb(210, pageBg.R, pageBg.G, pageBg.B);
			Color neutral = (isDarkPage ? Color.White : Color.Black);
			Color textOnPage = (isDarkPage ? Color.White : Color.FromArgb(255, 30, 30, 30));
			bool dyn = _webViewDynamic;
			if (!dyn)
			{
				// 普通主题：融入页面（自动适配）
				_glassNavBg = toolbarBg;
				_glassStatusBg = toolbarBg;
				_glassBorder = Color.FromArgb(60, neutral);
				_chipNormalBg = Color.FromArgb(30, neutral);
				_chipHoverBg = Color.FromArgb(50, neutral);
				_addrBarBg = Color.FromArgb(40, neutral);
				_addrBarBorder = Color.FromArgb(50, (isDarkPage ? Color.Black : Color.White));
				_addrBarOpaqueColor = Color.FromArgb(255, pageBg.R, pageBg.G, pageBg.B);
				_chipTextColor = textOnPage;
				_statusTextColor = textOnPage;
				_addrTextColor = textOnPage;
			}
			if (navPanel != null && !dyn)
			{
				navPanel.BackColor = toolbarBg;
				navPanel.Invalidate();
			}
			if (_webViewStatusBarRef != null && !dyn)
			{
				_webViewStatusBarRef.BackColor = toolbarBg;
				_webViewStatusBarRef.Invalidate();
			}
			if (_webViewLblStatusUrl != null)
			{
				_webViewLblStatusUrl.ForeColor = _statusTextColor;
				if (!dyn) _webViewLblStatusUrl.BackColor = toolbarBg;
			}
			if (_webViewLblStatusIp != null)
			{
				_webViewLblStatusIp.ForeColor = _statusTextColor;
				if (!dyn) _webViewLblStatusIp.BackColor = toolbarBg;
			}
			if (_webViewLblStatusEngine != null)
			{
				_webViewLblStatusEngine.ForeColor = _statusTextColor;
				if (!dyn) _webViewLblStatusEngine.BackColor = toolbarBg;
			}
			if (_webViewChipContainer != null)
			{
				_webViewChipContainer.Invalidate();
			}
			if (_webViewAddrBarHost != null)
			{
				_webViewAddrBarHost.Invalidate();
			}
			if (_webViewEngineChips != null)
			{
				foreach (Panel chip in _webViewEngineChips)
				{
					chip.Invalidate();
				}
			}
			if (_webViewRuleChip != null)
			{
				_webViewRuleChip.Invalidate();
			}
			if (_webViewBtnExtractIp != null)
			{
				_webViewBtnExtractIp.Invalidate();
			}
		}
		catch
		{
		}
	}

	/// <summary>根据当前主题判定是否为“动态/毛玻璃”主题，并解析导航栏渐变光斑色（呼应主窗口极光）。</summary>
	private void UpdateWebViewDynamicState()
	{
		if (theme == null)
		{
			_webViewDynamic = false;
			_webViewStops = null;
			return;
		}
		bool dynamic = theme.GlassEnabled || !string.IsNullOrEmpty(theme.AnimationType);
		List<Color> stops = (theme.GradientStops != null && theme.GradientStops.Count >= 2)
			? new List<Color>(theme.GradientStops)
			: new List<Color> { theme.Bg, theme.Primary, theme.Accent, theme.BgAlt };
		_webViewStops = stops;
		_webViewDynamic = dynamic;
	}

	/// <summary>切换主题时刷新 WebView2 玻璃导航栏/状态栏的全部颜色字段，并触发重绘，使窗口跟随主题。</summary>
	private void ApplyWebViewNavTheme()
	{
		if (theme == null)
		{
			return;
		}
		_glassNavBg = Color.FromArgb(210, theme.Surface);
		_glassStatusBg = Color.FromArgb(210, theme.Surface);
		_glassBorder = Color.FromArgb(60, theme.Border);
		_chipNormalBg = Color.FromArgb(30, theme.TextSecondary);
		_chipHoverBg = Color.FromArgb(50, theme.TextSecondary);
		_addrBarBg = Color.FromArgb(40, theme.TextSecondary);
		_addrBarBorder = Color.FromArgb(50, theme.Border);
		_addrBarOpaqueColor = Color.FromArgb(255, theme.Surface.R, theme.Surface.G, theme.Surface.B);
		double LumOf(Color c)
		{
			double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
			r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
			g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
			b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
			return 0.2126 * r + 0.7152 * g + 0.0722 * b;
		}
		Color Contrast(Color bg) => (LumOf(bg) < 0.5) ? Color.White : Color.FromArgb(255, 30, 30, 30);
		_chipTextColor = Contrast(_glassNavBg);
		_statusTextColor = Contrast(_glassStatusBg);
		_addrTextColor = Contrast(_addrBarOpaqueColor);
		UpdateWebViewDynamicState();
		// 窗口已关闭则释放引用，避免对已 Dispose 的控件调用 Invalidate
		if (webViewNavPanel != null && webViewNavPanel.IsDisposed) webViewNavPanel = null;
		if (_webViewStatusBarRef != null && _webViewStatusBarRef.IsDisposed) _webViewStatusBarRef = null;
		if (_webViewLblStatusUrl != null && _webViewLblStatusUrl.IsDisposed) _webViewLblStatusUrl = null;
		if (_webViewLblStatusIp != null && _webViewLblStatusIp.IsDisposed) _webViewLblStatusIp = null;
		if (_webViewLblStatusEngine != null && _webViewLblStatusEngine.IsDisposed) _webViewLblStatusEngine = null;
		if (_webViewChipContainer != null && _webViewChipContainer.IsDisposed) _webViewChipContainer = null;
		if (_webViewAddrBarHost != null && _webViewAddrBarHost.IsDisposed) _webViewAddrBarHost = null;
		if (_webViewRuleChip != null && _webViewRuleChip.IsDisposed) _webViewRuleChip = null;
		if (_webViewBtnExtractIp != null && _webViewBtnExtractIp.IsDisposed) _webViewBtnExtractIp = null;
		if (_webViewEngineChips != null)
		{
			_webViewEngineChips = _webViewEngineChips.Where((Panel c) => c != null && !c.IsDisposed).ToList();
		}
		// 跟随主题（覆盖页面取色）
		if (webViewNavPanel != null)
		{
			webViewNavPanel.BackColor = _glassNavBg;
			webViewNavPanel.Invalidate();
		}
		if (_webViewStatusBarRef != null)
		{
			_webViewStatusBarRef.BackColor = _glassStatusBg;
			_webViewStatusBarRef.Invalidate();
		}
		if (_webViewLblStatusUrl != null)
		{
			_webViewLblStatusUrl.ForeColor = _statusTextColor;
			_webViewLblStatusUrl.BackColor = _glassStatusBg;
		}
		if (_webViewLblStatusIp != null)
		{
			_webViewLblStatusIp.ForeColor = _statusTextColor;
			_webViewLblStatusIp.BackColor = _glassStatusBg;
		}
		if (_webViewLblStatusEngine != null)
		{
			_webViewLblStatusEngine.ForeColor = _statusTextColor;
			_webViewLblStatusEngine.BackColor = _glassStatusBg;
		}
		if (_webViewChipContainer != null)
		{
			_webViewChipContainer.Invalidate();
		}
		if (_webViewAddrBarHost != null)
		{
			_webViewAddrBarHost.BackColor = _addrBarOpaqueColor;
			_webViewAddrBarHost.Invalidate();
		}
		if (webViewTxtUrl != null)
		{
			webViewTxtUrl.ForeColor = _addrTextColor;
			webViewTxtUrl.BackColor = _addrBarOpaqueColor;
		}
		if (_webViewCboSearchRule != null)
		{
			_webViewCboSearchRule.BackColor = theme.Surface;
			_webViewCboSearchRule.ForeColor = theme.TextPrimary;
		}
		if (_webViewEngineChips != null)
		{
			foreach (Panel chip in _webViewEngineChips)
			{
				chip.Invalidate();
			}
		}
		if (_webViewRuleChip != null)
		{
			_webViewRuleChip.Invalidate();
		}
		if (_webViewBtnExtractIp != null)
		{
			_webViewBtnExtractIp.Invalidate();
		}
	}

	private async void LoadAndParseIpPorts()
	{
		string ipFile = Path.Combine(Application.StartupPath, "extracted_ips.txt");
		if (!File.Exists(ipFile))
		{
			DarkMessageBox.Show("未找到提取的IP文件: extracted_ips.txt\n请先使用搜索平台提取IP+端口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		Form dlg = new Form();
		try
		{
			bool isDarkPlat = DrawingUtils.IsDarkColor(theme.Bg);
			NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
			dlg.Text = "选择平台规则";
			dlg.ClientSize = new Size(SX(680), SY(310));
			dlg.StartPosition = FormStartPosition.Manual;
			dlg.MaximizeBox = false;
			dlg.MinimizeBox = false;
			dlg.Icon = this.Icon;
			var ctx = NeonChrome.Apply(dlg, pal, "选择平台规则", dpiScale);
			int ox = ctx.Margin, oy = ctx.Margin + ctx.TitleHeight;
			Point At(int x, int yy) => new Point(x - ox, yy);
			CenterForm(dlg, this);
			ListBox lstPlatforms = new ListBox
			{
				Location = At(20, 20),
				Size = new Size(615, 120),
				Font = GetFont(11f),
				SelectionMode = SelectionMode.One,
				BackColor = pal.PanelBg,
				ForeColor = pal.InputText,
				BorderStyle = BorderStyle.None
			};
			lstPlatforms.Items.AddRange(new object[3] { "智慧光迅 - /ZHGXTV/Public/json/live_interface.txt", "华视美达 - /newlive/live/hls/{cid}/live.m3u8", "智慧桌面 - /iptv/live/1000.json?key=txiptv" });
			lstPlatforms.SelectedIndex = 0;
			ctx.Body.Controls.Add(lstPlatforms);
			NeonButton btnOK = new NeonButton
			{
				Text = "确定",
				Location = At(100, 140),
				Size = new Size(100, 38),
				Font = GetFont(11f),
				IsPrimary = true,
				GradientStart = pal.Neon,
				GradientEnd = pal.Neon2,
				TextColorX = pal.PrimaryText,
				GlowColor = pal.Glow,
				GlowEnabled = !pal.SuppressGlow,
				Radius = 6
			};
			btnOK.Click += delegate
			{
				dlg.DialogResult = DialogResult.OK;
			};
			ctx.Body.Controls.Add(btnOK);
			NeonButton btnCancel = new NeonButton
			{
				Text = "取消",
				Location = At(420, 140),
				Size = new Size(100, 38),
				Font = GetFont(11f),
				IsPrimary = false,
				BorderColor = pal.Border,
				GlowColor = pal.Glow,
				GlowEnabled = !pal.SuppressGlow,
				Radius = 6
			};
			btnCancel.Click += delegate
			{
				dlg.DialogResult = DialogResult.Cancel;
			};
			ctx.Body.Controls.Add(btnCancel);
			if (dlg.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			string selectedText = lstPlatforms.SelectedItem.ToString();
			string ruleName = selectedText.Split('-')[0].Trim();
			SelectNavItem("检测");
			string[] array = File.ReadAllLines(ipFile, Encoding.UTF8);
			List<string> ipList = new List<string>();
			string[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				string trimmed = array2[num].Trim();
				if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && RxIpV4.IsMatch(trimmed))
				{
					ipList.Add(trimmed);
				}
			}
			if (ipList.Count == 0)
			{
				DarkMessageBox.Show("未找到有效的IP地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}
			hasSearchPlatformData = true;
			int addedCount = 0;
			DateTime parseTime = DateTime.Now;
			if (ruleName == "智慧光迅")
			{
				foreach (string ipPort in ipList)
				{
					string[] parts = ipPort.Split(':');
					if (parts.Length == 2)
					{
						string ip = parts[0];
						string port = parts[1];
						string rootHttp = "http://" + ip + ":" + port;
						string url = rootHttp + "/ZHGXTV/Public/json/live_interface.txt";
						if (!allChannels.Any((ChannelInfo c) => c.Url == url))
						{
							allChannels.Add(new ChannelInfo
							{
								Name = "智慧光迅_" + ipPort,
								Url = url,
								Group = "智慧光迅解析",
								Status = "待解析",
								ParseDateTime = parseTime
							});
							addedCount++;
						}
					}
				}
			}
			else if (ruleName == "华视美达")
			{
				Tuple<int, int> scanConfig = await ShowScanConfigDialogAsync();
				if (scanConfig == null)
				{
					return;
				}
				int scanCount = scanConfig.Item1;
				int threadCount = scanConfig.Item2;
				foreach (string ipPort2 in ipList)
				{
					string[] parts2 = ipPort2.Split(':');
					if (parts2.Length != 2)
					{
						continue;
					}
					string ip2 = parts2[0];
					string port2 = parts2[1];
					string rootHttp2 = "http://" + ip2 + ":" + port2;
					if (lblProgressText != null)
					{
						lblProgressText.Text = "华视美达扫描进度:";
						lblProgressText.Refresh();
					}
					if (lblPercent != null)
					{
						lblPercent.Text = "0%";
						lblPercent.Refresh();
					}
					if (statusBarRef != null)
					{
						LayoutStatusBar(statusBarRef);
					}
					Refresh();
					ConcurrentBag<Tuple<string, string>> validResults = new ConcurrentBag<Tuple<string, string>>();
					List<int> cidList = Enumerable.Range(1, scanCount).ToList();
					int processedCount = 0;
					await Task.Run(delegate
					{
						//IL_000e: Unknown result type (might be due to invalid IL or missing references)
						//IL_0018: Expected O, but got Unknown
						HttpClient httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 32 });
						try
						{
							httpClient.Timeout = TimeSpan.FromSeconds(2.5);
							((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
							Parallel.ForEach(cidList, new ParallelOptions
							{
								MaxDegreeOfParallelism = threadCount
							}, delegate(int num2)
							{
								//IL_0035: Unknown result type (might be due to invalid IL or missing references)
								//IL_003f: Expected O, but got Unknown
								string text = $"{rootHttp2}/newlive/live/hls/{num2}/live.m3u8";
								try
								{
									if (httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, text)).Result.IsSuccessStatusCode)
									{
										HttpResponseMessage result2 = httpClient.GetAsync(text).Result;
										if (result2.IsSuccessStatusCode)
										{
											string result3 = result2.Content.ReadAsStringAsync().Result;
											if (!string.IsNullOrEmpty(result3) && result3.Contains("#EXTM3U"))
											{
												validResults.Add(Tuple.Create(text, result3));
											}
										}
										return;
									}
								}
								catch
								{
								}
								try
								{
									HttpResponseMessage result4 = httpClient.GetAsync(text).Result;
									if (result4.IsSuccessStatusCode)
									{
										string result5 = result4.Content.ReadAsStringAsync().Result;
										if (!string.IsNullOrEmpty(result5) && result5.Contains("#EXTM3U"))
										{
											validResults.Add(Tuple.Create(text, result5));
										}
									}
								}
								catch
								{
								}
								int num3 = Interlocked.Increment(ref processedCount);
								int pct = (int)((double)num3 * 100.0 / (double)scanCount);
								if (lblPercent != null && !lblPercent.IsDisposed)
								{
									try
									{
										lblPercent.Invoke((Action)delegate
										{
											if (lblPercent != null && !lblPercent.IsDisposed)
											{
												lblPercent.Text = $"{pct}%";
											}
											if (statusBarRef != null && !statusBarRef.IsDisposed)
											{
												progressBarWidth = statusBarRef.ClientSize.Width * pct / 100;
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
										});
									}
									catch
									{
									}
								}
							});
						}
						finally
						{
							if (httpClient != null)
							{
								((IDisposable)httpClient).Dispose();
							}
						}
					});
					if (lblProgressText != null && !lblProgressText.IsDisposed)
					{
						lblProgressText.Text = "华视美达扫描完成:";
					}
					if (lblPercent != null && !lblPercent.IsDisposed)
					{
						lblPercent.Text = $"找到{validResults.Count}个";
					}
					if (statusBarRef != null)
					{
						LayoutStatusBar(statusBarRef);
					}
					Refresh();
					foreach (Tuple<string, string> result in validResults)
					{
						if (!allChannels.Any((ChannelInfo c) => c.Url == result.Item1))
						{
							string[] urlParts = result.Item1.Split('/');
							string cid = ((urlParts.Length > 1) ? urlParts[urlParts.Length - 2] : "");
							allChannels.Add(new ChannelInfo
							{
								Name = "华视美达_" + ipPort2 + "_CID" + cid,
								Url = result.Item1,
								Group = "华视美达解析",
								Status = "待解析",
								ParseDateTime = parseTime
							});
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
					{
						LayoutStatusBar(statusBarRef);
					}
				}
			}
			else
			{
				foreach (string ipPort3 in ipList)
				{
					string[] parts3 = ipPort3.Split(':');
					if (parts3.Length == 2)
					{
						string ip3 = parts3[0];
						string port3 = parts3[1];
						string rootHttp3 = "http://" + ip3 + ":" + port3;
						string url2 = rootHttp3 + "/iptv/live/1000.json?key=txiptv";
						if (!allChannels.Any((ChannelInfo c) => c.Url == url2))
						{
							allChannels.Add(new ChannelInfo
							{
								Name = "智慧桌面_" + ipPort3,
								Url = url2,
								Group = "智慧桌面解析",
								Status = "待解析",
								ParseDateTime = parseTime
							});
							addedCount++;
						}
					}
				}
			}
			RefreshGrid();
			UpdateStatusBar();
			if (!autoParseLink && btnParseLink != null)
			{
				btnParseLink.Visible = true;
			}
			DarkMessageBox.Show($"已添加 {addedCount} 条待解析链接\n分组: {ruleName}解析", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		finally
		{
			if (dlg != null)
			{
				((IDisposable)dlg).Dispose();
			}
		}
	}

	private void ShowIptvParserDialog()
	{
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Color bgColor = theme.Bg;
		Color panelBg = theme.Surface;
		Color textColor = pal.Label;
		Color labelColor = pal.Muted;
		Color borderColor = pal.Border;
		Color btnColor = pal.Neon;
		Color btnText = pal.PrimaryText;
		Color successColor = theme.SuccessColor;
		Color errorColor = theme.ErrorColor;
		Form dlg = new Form();
		try
		{
			dlg.Text = "直播源解析";
			dlg.StartPosition = FormStartPosition.Manual;
			dlg.MaximizeBox = false;
			dlg.MinimizeBox = false;
			dlg.Icon = this.Icon;
			Rectangle screen = Screen.PrimaryScreen.WorkingArea;
			int formWidth = (int)((double)screen.Width * 0.9);
			int formHeight = (int)((double)screen.Height * 0.85);
			formWidth = Math.Max(formWidth, 900);
			formHeight = Math.Max(formHeight, 650);
			dlg.ClientSize = new Size(formWidth, formHeight + SX(68));
			var ctx = NeonChrome.Apply(dlg, pal, "直播源解析", dpiScale);
			Point At(int x, int yy) => new Point(x, yy);
			CenterForm(dlg, this);
			int padding = SX(12);                                    // 窗口内边距
			int inputHeight = SY(30);                               // 输入框高度
			int btnHeight = SY(34);                                 // 按钮高度
			int btnWidth = SX(110);                                 // 按钮宽度
			int leftPanelWidth = (int)((double)ctx.Body.Width * 0.28); // 左侧面板宽度占比28%
			leftPanelWidth = Math.Max(leftPanelWidth, SX(300));     // 左侧面板最小宽度300px
			leftPanelWidth = Math.Min(leftPanelWidth, SX(380));     // 左侧面板最大宽度380px
			// 平台选择控件参数（独立定义，互不影响）
			// IP端口控件参数（独立定义，互不影响）
			int ipLabelWidth = SX(80);                              // IP端口标签宽度
			// 请求超时控件参数（独立定义，互不影响）
			int timeoutLabelWidth = SX(120);                        // 请求超时标签宽度（较长文本）
			int timeoutInputWidth = SX(100);                        // 请求超时输入框宽度（较短，仅需数字）
			Panel leftPanel = new Panel
			{
				Location = At(padding, padding),
				Size = new Size(leftPanelWidth, ctx.Body.Height - padding * 2),
				BackColor = Color.Transparent
			};
			leftPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, leftPanel.Width - 1, leftPanel.Height - 1), 12);
				using (SolidBrush brush = new SolidBrush(panelBg))
				{
					e.Graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(borderColor, 1f);
				e.Graphics.DrawPath(pen, path);
			};
			ctx.Body.Controls.Add(leftPanel);
			Panel rightPanel = new Panel
			{
				Location = At(padding + leftPanelWidth + padding, padding),
				Size = new Size(ctx.Body.Width - padding * 3 - leftPanelWidth, ctx.Body.Height - padding * 2),
				BackColor = Color.Transparent
			};
			rightPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, rightPanel.Width - 1, rightPanel.Height - 1), 12);
				using (SolidBrush brush = new SolidBrush(panelBg))
				{
					e.Graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(borderColor, 1f);
				e.Graphics.DrawPath(pen, path);
			};
			ctx.Body.Controls.Add(rightPanel);
			Action<Button, Color> ApplyRoundButton = delegate(Button btn, Color bg)
			{
				btn.FlatStyle = FlatStyle.Flat;
				btn.FlatAppearance.BorderSize = 0;
				btn.UseVisualStyleBackColor = false;
				btn.BackColor = bg;
				btn.Region?.Dispose();
				using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 6))
				{
					btn.Region = new Region(path);
				}
				btn.Resize += delegate
				{
					btn.Region?.Dispose();
					using GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 6);
					btn.Region = new Region(path2);
				};
				btn.MouseEnter += delegate
				{
					btn.BackColor = LightenColor(bg, 20);
				};
				btn.MouseLeave += delegate
				{
					btn.BackColor = bg;
				};
				btn.MouseDown += delegate
				{
					btn.BackColor = LightenColor(bg, 40);
					btn.Location = At(btn.Location.X + 1, btn.Location.Y + 1);
				};
				btn.MouseUp += delegate
				{
					btn.BackColor = LightenColor(bg, 20);
					btn.Location = At(btn.Location.X - 1, btn.Location.Y - 1);
				};
			};
			int y = SX(16);
			Label lblTitle = new Label
			{
				Text = "解析配置",
				Font = GetFont(SF(10.5f), FontStyle.Bold),
				ForeColor = textColor,
				Location = At(SX(16), y - SX(6)),
				AutoSize = true
			};
			leftPanel.Controls.Add(lblTitle);
			y += SY(36);
			Label lblPlatform = new Label
			{
				Text = "平台选择",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), y),
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft
			};
			leftPanel.Controls.Add(lblPlatform);
			ComboBox cboPlatform = new ComboBox
			{
				Location = At(lblPlatform.Right + SX(12), y),
				Size = new Size(leftPanelWidth - lblPlatform.Right - SX(28), inputHeight),
				Font = GetFont(SF(8.5f)),
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			cboPlatform.Items.AddRange(new object[3] { "1. 智慧光迅 ZHGXTV", "2. 华视美达 频道扫描", "3. 智能KUTV JSON接口" });
			cboPlatform.SelectedIndex = 0;
			OwnerDrawComboBox(cboPlatform, isDark, borderColor, theme.Surface, textColor);
			leftPanel.Controls.Add(cboPlatform);
			y += SY(40);
			Label lblIpPort = new Label
			{
				Text = "IP端口",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), y),
				Size = new Size(ipLabelWidth, inputHeight)
			};
			leftPanel.Controls.Add(lblIpPort);
			Panel ipPortPanel = new Panel
			{
				Location = At(cboPlatform.Left, y),
				Size = new Size(cboPlatform.Width, inputHeight),
				BackColor = theme.Surface
			};
			ipPortPanel.Region?.Dispose();
			using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, ipPortPanel.Width, ipPortPanel.Height), 6))
			{
				ipPortPanel.Region = new Region(path);
			}
			ipPortPanel.Resize += delegate
			{
				ipPortPanel.Region?.Dispose();
				using GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, ipPortPanel.Width, ipPortPanel.Height), 6);
				ipPortPanel.Region = new Region(path2);
			};
			ipPortPanel.Paint += delegate(object sender, PaintEventArgs e)
			{
				using Pen pen = new Pen(theme.TextSecondary, 1);
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath borderPath = GetRoundedPath(new Rectangle(0, 0, ipPortPanel.Width - 1, ipPortPanel.Height - 1), 6);
				e.Graphics.DrawPath(pen, borderPath);
			};
			TextBox txtIpPort = new TextBox
			{
				Location = At(2, 2),
				Size = new Size(ipPortPanel.Width - 4, ipPortPanel.Height - 4),
				Font = GetFont(SF(8.5f)),
				BorderStyle = BorderStyle.None,
				BackColor = theme.Surface,
				ForeColor = theme.TextSecondary,
				Text = "示例：110.72.103.69:8181"
			};
			txtIpPort.GotFocus += delegate
			{
				if (txtIpPort.Text == "示例：110.72.103.69:8181")
				{
					txtIpPort.Text = "";
					txtIpPort.ForeColor = textColor;
				}
			};
			txtIpPort.LostFocus += delegate
			{
				if (string.IsNullOrWhiteSpace(txtIpPort.Text))
				{
					txtIpPort.Text = "示例：110.72.103.69:8181";
					txtIpPort.ForeColor = theme.TextSecondary;
				}
			};
			ipPortPanel.Controls.Add(txtIpPort);
			leftPanel.Controls.Add(ipPortPanel);
			y += SY(40);
			Button btnAutoBuild = new Button
			{
				Text = "自动拼接完整URL",
				Location = At((leftPanelWidth - btnWidth) / 2, y),
				Size = new Size(btnWidth + 100, btnHeight),
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
				Location = At(SX(16), y),
				Size = new Size(timeoutLabelWidth, inputHeight),
				TextAlign = ContentAlignment.MiddleLeft
			};
			leftPanel.Controls.Add(lblTimeout);
			Panel timeoutPanel = new Panel
			{
				Location = At(timeoutLabelWidth + SX(50), y),
				Size = new Size(timeoutInputWidth, inputHeight),
				BackColor = theme.Surface
			};
			timeoutPanel.Region?.Dispose();
			using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, timeoutPanel.Width, timeoutPanel.Height), 6))
			{
				timeoutPanel.Region = new Region(path);
			}
			timeoutPanel.Resize += delegate
			{
				timeoutPanel.Region?.Dispose();
				using GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, timeoutPanel.Width, timeoutPanel.Height), 6);
				timeoutPanel.Region = new Region(path2);
			};
			timeoutPanel.Paint += delegate(object sender, PaintEventArgs e)
			{
				using Pen pen = new Pen(theme.TextSecondary, 1);
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath borderPath = GetRoundedPath(new Rectangle(0, 0, timeoutPanel.Width - 1, timeoutPanel.Height - 1), 6);
				e.Graphics.DrawPath(pen, borderPath);
			};
			TextBox txtTimeout = new TextBox
			{
				Location = At(2, 2),
				Size = new Size(timeoutPanel.Width - 4, timeoutPanel.Height -4),
				Font = GetFont(SF(9f)),
				BorderStyle = BorderStyle.None,
				BackColor = theme.Surface,
				ForeColor = textColor,
				Text = "8",
				TextAlign = HorizontalAlignment.Center
			};
			timeoutPanel.Controls.Add(txtTimeout);
			leftPanel.Controls.Add(timeoutPanel);
			y += SY(40);
			Panel huashiPanel = new Panel
			{
				Location = At(SX(16), y),
				Size = new Size(leftPanelWidth - SX(32), SY(80)),
				BackColor = Color.Transparent,
				Visible = false
			};
			Color huashiBg = panelBg;
			huashiPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, huashiPanel.Width - 1, huashiPanel.Height - 1), 8);
				using (SolidBrush brush = new SolidBrush(huashiBg))
				{
					e.Graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(borderColor, 1f);
				e.Graphics.DrawPath(pen, path);
			};
			leftPanel.Controls.Add(huashiPanel);
			cboPlatform.SelectedIndexChanged += delegate
			{
				huashiPanel.Visible = cboPlatform.SelectedIndex == 1;
			};
			Label lblHuashiRange = new Label
			{
				Text = "扫描ID区间",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), SY(10)),
				Size = new Size(SX(120), inputHeight),
				TextAlign = ContentAlignment.MiddleLeft
			};
			huashiPanel.Controls.Add(lblHuashiRange);
		Panel huashiRangePanel = new Panel
		{
			Location = At(huashiPanel.Width - SX(120) - SX(8), SY(10)),
			Size = new Size(SX(120), inputHeight),
				BackColor = theme.Surface
			};
			huashiRangePanel.Region?.Dispose();
			using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, huashiRangePanel.Width, huashiRangePanel.Height), 6))
			{
				huashiRangePanel.Region = new Region(path);
			}
			huashiRangePanel.Resize += delegate
			{
				huashiRangePanel.Region?.Dispose();
				using GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, huashiRangePanel.Width, huashiRangePanel.Height), 6);
				huashiRangePanel.Region = new Region(path2);
			};
			huashiRangePanel.Paint += delegate(object sender, PaintEventArgs e)
			{
				using Pen pen = new Pen(theme.TextSecondary, 1);
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath borderPath = GetRoundedPath(new Rectangle(0, 0, huashiRangePanel.Width - 1, huashiRangePanel.Height - 1), 6);
				e.Graphics.DrawPath(pen, borderPath);
			};
			TextBox txtHuashiRange = new TextBox
			{
				Location = At(2, 2),
				Size = new Size(huashiRangePanel.Width - 4, huashiRangePanel.Height - 4),
				Font = GetFont(SF(9f)),
				BorderStyle = BorderStyle.None,
				BackColor = theme.Surface,
				ForeColor = textColor,
				Text = "1-100",
				TextAlign = HorizontalAlignment.Center
			};
			huashiRangePanel.Controls.Add(txtHuashiRange);
			huashiPanel.Controls.Add(huashiRangePanel);
			Label lblHuashiThread = new Label
			{
				Text = "并发线程数",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), SY(43)),
				Size = new Size(SX(120), inputHeight),
				TextAlign = ContentAlignment.MiddleLeft
			};
			huashiPanel.Controls.Add(lblHuashiThread);
		Panel huashiThreadPanel = new Panel
		{
			Location = At(huashiPanel.Width - SX(120) - SX(8), SY(43)),
			Size = new Size(SX(120), inputHeight),
			BackColor = theme.Surface
		};
			huashiThreadPanel.Region?.Dispose();
			using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, huashiThreadPanel.Width, huashiThreadPanel.Height), 6))
			{
				huashiThreadPanel.Region = new Region(path);
			}
			huashiThreadPanel.Resize += delegate
			{
				huashiThreadPanel.Region?.Dispose();
				using GraphicsPath path2 = GetRoundedPath(new Rectangle(0, 0, huashiThreadPanel.Width, huashiThreadPanel.Height), 6);
				huashiThreadPanel.Region = new Region(path2);
			};
			huashiThreadPanel.Paint += delegate(object sender, PaintEventArgs e)
			{
				using Pen pen = new Pen(theme.TextSecondary, 1);
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath borderPath = GetRoundedPath(new Rectangle(0, 0, huashiThreadPanel.Width - 1, huashiThreadPanel.Height - 1), 6);
				e.Graphics.DrawPath(pen, borderPath);
			};
			TextBox txtHuashiThread = new TextBox
			{
				Location = At(2, 2),
				Size = new Size(huashiThreadPanel.Width - 4, huashiThreadPanel.Height - 4),
				Font = GetFont(SF(9f)),
				BorderStyle = BorderStyle.None,
				BackColor = theme.Surface,
				ForeColor = textColor,
				Text = "8",
				TextAlign = HorizontalAlignment.Center
			};
			huashiThreadPanel.Controls.Add(txtHuashiThread);
			huashiPanel.Controls.Add(huashiThreadPanel);
			huashiPanel.Resize += delegate
			{
				if (huashiRangePanel != null && !huashiRangePanel.IsDisposed)
				{
					huashiRangePanel.Left = huashiPanel.Width - huashiRangePanel.Width - SX(8);
				}
				if (huashiThreadPanel != null && !huashiThreadPanel.IsDisposed)
				{
					huashiThreadPanel.Left = huashiPanel.Width - huashiThreadPanel.Width - SX(8);
				}
			};
			y += SY(110);
			Label lblHistory = new Label
			{
				Text = "历史记录",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), y + 40),
				AutoSize = true
			};
			leftPanel.Controls.Add(lblHistory);
			y += SY(24);
			Panel historyPanel = new Panel
			{
				Location = At(SX(16), y + 50),
				Size = new Size(SX(350), SY(140)),
				BackColor = Color.Transparent
			};
			Color historyBg = panelBg;
			historyPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, historyPanel.Width - 1, historyPanel.Height - 1), 8);
				using (SolidBrush brush = new SolidBrush(historyBg))
				{
					e.Graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(borderColor, 1f);
				e.Graphics.DrawPath(pen, path);
			};
			leftPanel.Controls.Add(historyPanel);
			ListBox lstHistory;
			Panel historyScrollPanel = CreateListBoxWithDarkScrollBar(isDark, textColor, out lstHistory, GetFont(SF(8.5f)));
			historyScrollPanel.Dock = DockStyle.None;
			historyScrollPanel.Location = At(SX(16), SY(6));
			historyScrollPanel.Size = new Size(SX(326), SY(120));
			historyPanel.Controls.Add(historyScrollPanel);
			foreach (string ip in iptvHistoryIps)
			{
				lstHistory.Items.Add(ip);
			}
			lstHistory.SelectedIndexChanged += delegate
			{
				if (lstHistory.SelectedItem != null)
				{
					txtIpPort.Text = lstHistory.SelectedItem.ToString();
					txtIpPort.ForeColor = textColor;
				}
			};
			lstHistory.MouseDoubleClick += delegate
			{
				if (lstHistory.SelectedItem != null)
				{
					txtIpPort.Text = lstHistory.SelectedItem.ToString();
					txtIpPort.ForeColor = textColor;
				}
			};
			ContextMenuStrip historyContextMenu = new ContextMenuStrip();
			historyContextMenu.Font = GetFont(SF(8.5f));
			AnimatedMenuRenderer historyMenuRenderer = new AnimatedMenuRenderer(theme);
			historyContextMenu.Renderer = historyMenuRenderer;
			historyMenuRenderer.Register(historyContextMenu);
			historyContextMenu.BackColor = theme.Surface;
			historyContextMenu.ForeColor = theme.TextPrimary;
			ToolStripMenuItem menuDelete = new ToolStripMenuItem("删除选中项");
			menuDelete.Click += delegate
			{
				if (lstHistory.SelectedItem != null)
				{
					string text = lstHistory.SelectedItem.ToString();
					lstHistory.Items.Remove(text);
					iptvHistoryIps.Remove(text);
					SaveConfig();
				}
			};
			historyContextMenu.Items.Add(menuDelete);
			ToolStripMenuItem menuClear = new ToolStripMenuItem("清空历史记录");
			menuClear.Click += delegate
			{
				iptvHistoryIps.Clear();
				lstHistory.Items.Clear();
				SaveConfig();
			};
			historyContextMenu.Items.Add(menuClear);
			lstHistory.ContextMenuStrip = historyContextMenu;
			y += SY(150);
			Label lblHeaders = new Label
			{
				Text = "自定义请求头",
				Font = GetFont(SF(9f)),
				ForeColor = labelColor,
				Location = At(SX(16), y + 70),
				AutoSize = true
			};
			leftPanel.Controls.Add(lblHeaders);
			y += SY(24);
			Panel pHeaders = new Panel
			{
				Location = At(SX(16), y + 80),
				Size = new Size(SX(348), SY(155)),
				BackColor = Color.Transparent
			};
			TextBox txtHeaders;
			Panel headersScrollPanel = CreateTextBoxWithDarkScrollBar(isDark, textColor, out txtHeaders, new Font("Consolas", SF(8.5f)), readOnly: false, wordWrap: true, acceptsReturn: true);
			txtHeaders.Text = "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36\r\nAccept-Language: zh-CN,zh;q=0.9\r\nAccept: */*";
			pHeaders.Controls.Add(headersScrollPanel);
			leftPanel.Controls.Add(pHeaders);
			y += SY(115);
			Button btnExecute = new Button
			{
				Text = "开始解析",
				Location = At(SX(60), leftPanel.ClientSize.Height - SY(65)),
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
				Location = At(SX(200), leftPanel.ClientSize.Height - SY(65)),
				Size = new Size(btnWidth, btnHeight),
				Font = GetFont(SF(8f)),
				BackColor = panelBg,
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
				Location = At(SX(10), SY(10)),
				AutoSize = true
			};
			rightPanel.Controls.Add(lblResult);
			int tabW = rightPanel.ClientSize.Width - SX(20);
			int tabH = rightPanel.ClientSize.Height - SY(115);
			DarkTabControl tabResult = new DarkTabControl
			{
				Location = At(SX(10), SY(40)),
				Size = new Size(tabW, tabH),
				Font = GetFont(SF(8f)),
				BackColor = panelBg,
				ForeColor = textColor,
				Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right)
			};
			tabResult.ApplyTheme(theme);
			tabResult.TabWidths = new int[4]
			{
				SX(90),
				SX(90),
				SX(110),
				SX(90)
			};
			tabResult.TabHeight = SY(30);
			tabResult.TabXOffset = SX(10);
			tabResult.TabSpacing = SX(8);
			rightPanel.Controls.Add(tabResult);
			TabPage tabRaw = new TabPage("原始文本");
			tabRaw.BackColor = panelBg;
			tabRaw.BorderStyle = BorderStyle.None;
			TextBox txtRaw;
			Panel rawPanel = CreateTextBoxWithDarkScrollBar(isDark, textColor, out txtRaw, new Font("Consolas", SF(9f)));
			tabRaw.Controls.Add(rawPanel);
			tabResult.TabPages.Add(tabRaw);
			TabPage tabPreview = new TabPage("频道预览");
			tabPreview.BackColor = panelBg;
			tabPreview.BorderStyle = BorderStyle.None;
			TextBox txtPreview;
			Panel previewPanel = CreateTextBoxWithDarkScrollBar(isDark, textColor, out txtPreview, new Font("Consolas", SF(9f)));
			tabPreview.Controls.Add(previewPanel);
			tabResult.TabPages.Add(tabPreview);
			TabPage tabM3u = new TabPage("M3U播放列表");
			tabM3u.BackColor = panelBg;
			tabM3u.BorderStyle = BorderStyle.None;
			TextBox txtM3u;
			Panel m3uPanel = CreateTextBoxWithDarkScrollBar(isDark, textColor, out txtM3u, new Font("Consolas", SF(9f)));
			tabM3u.Controls.Add(m3uPanel);
			tabResult.TabPages.Add(tabM3u);
			TabPage tabLog = new TabPage("运行日志");
			tabLog.BackColor = panelBg;
			tabLog.BorderStyle = BorderStyle.None;
			TextBox txtLog;
			Panel logPanel = CreateTextBoxWithDarkScrollBar(isDark, successColor, out txtLog, new Font("Consolas", SF(9f)));
			tabLog.Controls.Add(logPanel);
			tabResult.TabPages.Add(tabLog);
			int btnStartY = rightPanel.ClientSize.Height - SY(65);
			int btnExportWidth = SX(140);
			int btnAddWidth = SX(140);
			Button btnExport = new Button
			{
				Text = "导出全部文件",
				Location = At(SX(16), btnStartY),
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
				Location = At(SX(16) + btnExportWidth + SX(12), btnStartY),
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
				Location = At(rightPanel.ClientSize.Width - SX(260), rightPanel.ClientSize.Height - SY(28)),
				Size = new Size(SX(240), SY(20)),
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleRight,
				Anchor = AnchorStyles.Right | AnchorStyles.Bottom
			};
			rightPanel.Controls.Add(lblStats);
			string currentPlatform = "";
			string currentIpPort = "";
			string currentRaw = "";
			string currentPreview = "";
			string currentM3u = "";
			int currentValidCount = 0;
			cboPlatform.SelectedIndexChanged += delegate
			{
				huashiPanel.Visible = cboPlatform.SelectedIndex == 1;
			};
			huashiPanel.Visible = cboPlatform.SelectedIndex == 1;
			btnAutoBuild.Click += delegate
			{
				string text = txtIpPort.Text.Trim();
				string error;
				string fullIpPort;
				if (text.StartsWith("http://") || text.StartsWith("https://"))
				{
					Log("检测到完整HTTP链接，直接使用");
				}
				else if (!ValidateIpPort(text, out error, out fullIpPort))
				{
					Log("IP校验错误：" + error);
					DarkMessageBox.Show(error, "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					string text2 = "http://" + fullIpPort;
					string text3 = "";
					switch (cboPlatform.SelectedIndex)
					{
					case 0:
						text3 = text2 + "/ZHGXTV/Public/json/live_interface.txt";
						break;
					case 1:
						text3 = text2;
						break;
					case 2:
						text3 = text2 + "/iptv/live/1000.json?key=txiptv";
						break;
					}
					txtIpPort.Text = fullIpPort;
					Log("已自动拼接标准地址：" + text3);
				}
			};
			btnCancel.Click += delegate
			{
				dlg.DialogResult = DialogResult.Cancel;
			};
			CancellationTokenSource cts = null;
			btnExecute.Click += async delegate
			{
				if (btnExecute.Text == "停止解析")
				{
					cts?.Cancel();
					btnExecute.Enabled = false;
					btnExecute.Text = "停止中...";
					return;
				}
				ClearResults();
				btnExecute.Enabled = false;
				btnExecute.Text = "停止解析";
				cts = new CancellationTokenSource();
				SetStats("频道：0 | 状态：解析中");
				string ipRaw = txtIpPort.Text.Trim();
				string fullIpPort;
				if (!ValidateIpPort(ipRaw, out var error, out fullIpPort))
				{
					DarkMessageBox.Show(error, "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					btnExecute.Enabled = true;
					btnExecute.Text = "开始解析";
					return;
				}
				txtIpPort.Text = fullIpPort;
				currentIpPort = fullIpPort;
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
				Dictionary<string, string> customHeaders = ParseHeaders(txtHeaders.Text);
				HttpClient httpClient = CreateHttpClient(customHeaders, currentIpPort);
				httpClient.Timeout = TimeSpan.FromSeconds(timeout);
				currentPlatform = cboPlatform.SelectedItem.ToString().Split(' ')[0];
				Log("===== 启动任务 | 平台" + currentPlatform + " | 服务器 " + currentIpPort + " =====");
				try
				{
					switch (cboPlatform.SelectedIndex)
					{
					case 0:
						await ParseZhgx(httpClient, timeout, cts.Token);
						break;
					case 1:
						await ParseHuashi(httpClient, cts.Token);
						break;
					case 2:
						await ParseKutv(httpClient, timeout, cts.Token);
						break;
					}
				}
				catch (OperationCanceledException)
				{
					Log("任务已被用户取消");
					SetStats("频道：0 | 状态：已取消");
				}
				catch (Exception ex)
				{
					Log("任务运行异常：" + ex.Message);
					SetStats("频道：0 | 状态：错误");
				}
				finally
				{
					((HttpMessageInvoker)httpClient).Dispose();
					cts?.Dispose();
					btnExecute.Enabled = true;
					btnExecute.Text = "开始解析";
				}
			};
			btnExport.Click += delegate
			{
				if (currentValidCount == 0)
				{
					DarkMessageBox.Show("请先点击【开始解析】获取数据后再导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					string text = currentIpPort.Replace(":", "_");
					string text2 = "";
					switch (cboPlatform.SelectedIndex)
					{
					case 0:
						text2 = "智慧光迅";
						break;
					case 1:
						text2 = "华视美达";
						break;
					case 2:
						text2 = "智能KUTV";
						break;
					}
					List<string> list = new List<string>();
					if (cboPlatform.SelectedIndex == 0)
					{
						string text3 = GetUniqueFilePath(text2 + "_原始_" + text, ".txt");
						string text4 = GetUniqueFilePath(text2 + "_直播列表_" + text, ".m3u");
						if (!SafeWriteFile(text3, currentRaw))
						{
							list.Add("写入 " + Path.GetFileName(text3) + " 失败");
						}
						if (!SafeWriteFile(text4, currentM3u))
						{
							list.Add("写入 " + Path.GetFileName(text4) + " 失败");
						}
						if (list.Count == 0)
						{
							DarkMessageBox.Show($"导出成功\r\n原始文本：{Path.GetFileName(text3)}\r\nM3U播放列表：{Path.GetFileName(text4)}\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
					}
					else if (cboPlatform.SelectedIndex == 1)
					{
						string text5 = GetUniqueFilePath(text2 + "_有效源_" + text, ".m3u");
						if (!SafeWriteFile(text5, currentM3u))
						{
							list.Add("写入 " + Path.GetFileName(text5) + " 失败");
						}
						if (list.Count == 0)
						{
							DarkMessageBox.Show($"导出成功\r\nM3U文件：{Path.GetFileName(text5)}\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
					}
					else
					{
						string text6 = GetUniqueFilePath(text2 + "_逗号清单_" + text, ".txt");
						string text7 = GetUniqueFilePath(text2 + "_原始JSON_" + text, ".txt");
						string text8 = GetUniqueFilePath(text2 + "_直播列表_" + text, ".m3u");
						if (!SafeWriteFile(text6, currentPreview))
						{
							list.Add("写入 " + Path.GetFileName(text6) + " 失败");
						}
						if (!SafeWriteFile(text7, currentRaw))
						{
							list.Add("写入 " + Path.GetFileName(text7) + " 失败");
						}
						if (!SafeWriteFile(text8, currentM3u))
						{
							list.Add("写入 " + Path.GetFileName(text8) + " 失败");
						}
						if (list.Count == 0)
						{
							DarkMessageBox.Show($"导出3个文件成功\r\n逗号清单txt / 原始JSON / M3U播放列表\r\n有效频道：{currentValidCount}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						}
					}
					if (list.Count > 0)
					{
						DarkMessageBox.Show(string.Join("\r\n", list), "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
				}
			};
			btnAddToList.Click += delegate
			{
				if (currentValidCount == 0)
				{
					DarkMessageBox.Show("请先点击【开始解析】获取数据后再添加", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					int num = 0;
					DateTime now = DateTime.Now;
					string text = currentPlatform + "解析";
					if (cboPlatform.SelectedIndex == 0)
					{
						string[] array = currentPreview.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						for (int i = 0; i < array.Length; i++)
						{
							string[] array2 = array[i].Split(new string[1] { " , " }, StringSplitOptions.None);
							if (array2.Length >= 2)
							{
								string name = CleanText(array2[0]);
								string url = array2[1].Trim();
								if (!allChannels.Any((ChannelInfo c) => c.Url == url))
								{
									allChannels.Add(new ChannelInfo
									{
										Name = name,
										Url = url,
										Group = text,
										Status = "待解析",
										ParseDateTime = now
									});
									num++;
								}
							}
						}
					}
					else if (cboPlatform.SelectedIndex == 1)
					{
						string[] array = currentPreview.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string text2 in array)
						{
							if (text2.StartsWith("OK"))
							{
								Match match = Regex.Match(text2, "http[^\\s]+");
								if (match.Success)
								{
									string url2 = match.Value;
									string name2 = "华视美达_" + currentIpPort + "_" + url2.Split('/')[url2.Split('/').Length - 2];
									if (!allChannels.Any((ChannelInfo c) => c.Url == url2))
									{
										allChannels.Add(new ChannelInfo
										{
											Name = name2,
											Url = url2,
											Group = text,
											Status = "待解析",
											ParseDateTime = now
										});
										num++;
									}
								}
							}
						}
					}
					else
					{
						string[] array = currentPreview.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						for (int i = 0; i < array.Length; i++)
						{
							string[] array3 = array[i].Split(new char[1] { ',' }, 2);
							if (array3.Length >= 2)
							{
								string name3 = CleanText(array3[0]);
								string url3 = array3[1].Trim();
								if (!allChannels.Any((ChannelInfo c) => c.Url == url3))
								{
									allChannels.Add(new ChannelInfo
									{
										Name = name3,
										Url = url3,
										Group = text,
										Status = "待解析",
										ParseDateTime = now
									});
									num++;
								}
							}
						}
					}
					totalCount = allChannels.Count;
					RefreshGrid();
					UpdateEmptyState();
					SaveChannelList();
					if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
					{
						lblDetected.Text = $"已检测: 0/{totalCount}";
						lblAvailable.Text = "可用: 0";
						lblPercent.Text = "0.00%";
						progressBarWidth = 0;
						RestoreLabelColors();
						statusBarRef.PerformLayout();
						LayoutStatusBar(statusBarRef);
						statusBarRef.Refresh();
					}
					DarkMessageBox.Show($"已添加 {num} 条链接到检测列表\r\n分组: {text}", "添加成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			};
			dlg.CreateControl();
			ForceCreateChildHandles(dlg);
			UpdateScrollBarTheme(dlg);
			dlg.ShowDialog(this);
			void ClearResults()
			{
				txtRaw.Clear();
				txtPreview.Clear();
				txtM3u.Clear();
				txtLog.Clear();
			}
			void Log(string msg)
			{
				if (!txtLog.IsDisposed)
				{
					if (txtLog.InvokeRequired)
					{
						txtLog.BeginInvoke((Action)delegate
						{
							txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " | " + msg + "\r\n");
							txtLog.ScrollToCaret();
						});
					}
					else
					{
						txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " | " + msg + "\r\n");
						txtLog.ScrollToCaret();
					}
				}
			}
			async Task ParseHuashi(HttpClient httpClient, CancellationToken token)
			{
				string rangeStr = txtHuashiRange.Text.Trim();
				int threadNum = 8;
				int.TryParse(txtHuashiThread.Text.Trim(), out threadNum);
				if (!rangeStr.Contains("-"))
				{
					Log("扫描区间格式错误");
					DarkMessageBox.Show("扫描区间格式错误，标准示例：1-100", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				else
				{
					string[] rangeParts = rangeStr.Split('-');
					if (!int.TryParse(rangeParts[0], out var startId) || !int.TryParse(rangeParts[1], out var endId))
					{
						Log("扫描区间数字非法");
						DarkMessageBox.Show("扫描区间必须为纯数字", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					else if (startId <= 0 || endId < startId)
					{
						Log("扫描区间数字非法");
						DarkMessageBox.Show("起始必须小于结束且大于0", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					else
					{
						threadNum = Math.Max(1, Math.Min(20, threadNum));
						Log($"开始并发扫描 ID {startId}~{endId}，并发线程 {threadNum}");
						List<int> cidList = Enumerable.Range(startId, endId - startId + 1).ToList();
						ConcurrentBag<Tuple<int, string>> validResults = new ConcurrentBag<Tuple<int, string>>();
						int processedCount = 0;
						HttpClientHandler localHandler = new HttpClientHandler
						{
							MaxConnectionsPerServer = 32,
							AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
							UseCookies = false,
							AllowAutoRedirect = true,
							MaxAutomaticRedirections = 5
						};
						HttpClient localClient = new HttpClient((HttpMessageHandler)(object)localHandler)
						{
							Timeout = TimeSpan.FromSeconds(2.5)
						};
						((HttpHeaders)localClient.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
						await Task.Run(delegate
						{
							Parallel.ForEach(cidList, new ParallelOptions
							{
								MaxDegreeOfParallelism = threadNum,
								CancellationToken = token
							}, delegate(int cid)
							{
								token.ThrowIfCancellationRequested();
								string text = $"http://{currentIpPort}/newlive/live/hls/{cid}/live.m3u8";
								try
								{
									HttpRequestMessage val = new HttpRequestMessage(HttpMethod.Head, text);
									if (localClient.SendAsync(val, token).Result.IsSuccessStatusCode)
									{
										validResults.Add(Tuple.Create(cid, text));
										Log($"OK ID:{cid} | {text}");
										return;
									}
								}
								catch (OperationCanceledException)
								{
									throw;
								}
								catch
								{
								}
								try
								{
									HttpResponseMessage result2 = localClient.GetAsync(text, token).Result;
									if (result2.IsSuccessStatusCode)
									{
										string result3 = result2.Content.ReadAsStringAsync().Result;
										if (!string.IsNullOrEmpty(result3))
										{
											string text2 = ((result3.Length > 500) ? result3.Substring(0, 500) : result3);
											if (text2.Contains("m3u8") || text2.Contains("#EXTM3U"))
											{
												validResults.Add(Tuple.Create(cid, text));
												Log($"OK ID:{cid} | {text}");
												return;
											}
										}
									}
								}
								catch (OperationCanceledException)
								{
									throw;
								}
								catch
								{
								}
								Log($"FAIL ID:{cid}");
								int num = (int)((double)Interlocked.Increment(ref processedCount) * 100.0 / (double)cidList.Count);
								SetStats($"频道：{validResults.Count} | 状态：扫描中 {num}%");
							});
						}, token);
						((HttpMessageInvoker)localClient).Dispose();
						((HttpMessageHandler)localHandler).Dispose();
						List<string> m3uLines = new List<string> { "#EXTM3U" };
						List<string> previewLines = new List<string>();
						foreach (Tuple<int, string> result in validResults.OrderBy((Tuple<int, string> r) => r.Item1))
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
				}
			}
			async Task ParseKutv(HttpClient httpClient, int timeout, CancellationToken token)
			{
				string url = "http://" + currentIpPort + "/iptv/live/1000.json?key=txiptv";
				Log("请求地址：" + url);
				string jsonText = "";
				for (int retry = 0; retry <= 2; retry++)
				{
					token.ThrowIfCancellationRequested();
					try
					{
						HttpResponseMessage obj = await httpClient.GetAsync(url, token);
						obj.EnsureSuccessStatusCode();
						jsonText = await obj.Content.ReadAsStringAsync();
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception ex)
					{
						Log($"第{retry + 1}次请求失败：{ex.Message}，等待1秒重试...");
						await Task.Delay(1000, token);
						continue;
					}
					break;
				}
				if (string.IsNullOrWhiteSpace(jsonText))
				{
					Log("服务器返回空内容");
					SetStats("频道：0 | 状态：无数据");
					return;
				}
				try
				{
					Match codeMatch = Regex.Match(jsonText, "\"code\"\\s*:\\s*(\\d+)");
					if (codeMatch.Success && codeMatch.Groups[1].Value != "0")
					{
						Match msgMatch = Regex.Match(jsonText, "\"msg\"\\s*:\\s*\"([^\"]+)\"");
						string msg = (msgMatch.Success ? msgMatch.Groups[1].Value : "未知错误");
						Log("接口返回异常 code=" + codeMatch.Groups[1].Value + " msg=" + msg);
						SetStats("频道：0 | 状态：接口异常");
					}
					else
					{
						Match dataMatch = Regex.Match(jsonText, "\"data\"\\s*:\\s*(\\[.+?\\])", RegexOptions.Singleline);
						if (!dataMatch.Success)
						{
							Log("接口返回数据格式错误");
							SetStats("频道：0 | 状态：数据错误");
						}
						else
						{
							currentRaw = CleanText(jsonText);
							MatchCollection nameMatches = Regex.Matches(dataMatch.Groups[1].Value, "\"name\"\\s*:\\s*\"([^\"]+)\"");
							MatchCollection urlMatches = Regex.Matches(dataMatch.Groups[1].Value, "\"url\"\\s*:\\s*\"([^\"]+)\"");
							List<string> previewLines = new List<string>();
							List<string> m3uLines = new List<string> { "#EXTM3U" };
							int validCnt = 0;
							string baseHttp = "http://" + currentIpPort;
							for (int i = 0; i < nameMatches.Count && i < urlMatches.Count; i++)
							{
								string chName = CleanText(nameMatches[i].Groups[1].Value);
								string relUrl = urlMatches[i].Groups[1].Value;
								if (!string.IsNullOrEmpty(relUrl))
								{
									string fullPlay = (relUrl.StartsWith("http") ? relUrl : (baseHttp + relUrl));
									string csvLine = chName + "," + fullPlay;
									previewLines.Add(csvLine);
									m3uLines.Add("#EXTINF:-1," + chName);
									m3uLines.Add(fullPlay);
									validCnt++;
								}
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
					}
				}
				catch (Exception ex2)
				{
					Log("解析异常：" + ex2.Message);
					SetStats("频道：0 | 状态：解析异常");
				}
			}
			async Task ParseZhgx(HttpClient httpClient, int timeout, CancellationToken token)
			{
				string url = "http://" + currentIpPort + "/ZHGXTV/Public/json/live_interface.txt";
				Log("请求地址：" + url);
				string rawText = "";
				for (int retry = 0; retry <= 2; retry++)
				{
					token.ThrowIfCancellationRequested();
					try
					{
						HttpResponseMessage obj = await httpClient.GetAsync(url, token);
						obj.EnsureSuccessStatusCode();
						rawText = await obj.Content.ReadAsStringAsync();
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception ex)
					{
						Log($"第{retry + 1}次请求失败：{ex.Message}，等待1秒重试...");
						await Task.Delay(1000, token);
						continue;
					}
					break;
				}
				if (string.IsNullOrWhiteSpace(rawText))
				{
					Log("服务器返回空内容");
					SetStats("频道：0 | 状态：无数据");
				}
				else
				{
					currentRaw = rawText;
					List<string> previewLines = new List<string>();
					List<string> m3uLines = new List<string> { "#EXTM3U" };
					int validCnt = 0;
					int errCnt = 0;
					string[] array = rawText.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						string trimmed = array[i].Trim();
						if (!string.IsNullOrEmpty(trimmed))
						{
							try
							{
								string[] parts = trimmed.Split(new char[1] { ',' }, 2);
								if (parts.Length >= 2)
								{
									string name = CleanText(parts[0]);
									string playUrl = parts[1].Trim();
									previewLines.Add(name + " , " + playUrl);
									m3uLines.Add("#EXTINF:-1," + name);
									m3uLines.Add(playUrl);
									validCnt++;
								}
							}
							catch
							{
								errCnt++;
							}
						}
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
			}
			void SetStats(string stats)
			{
				if (lblStats.InvokeRequired)
				{
					lblStats.BeginInvoke((Action)delegate
					{
						lblStats.Text = stats;
					});
				}
				else
				{
					lblStats.Text = stats;
				}
			}
		}
		finally
		{
			if (dlg != null)
			{
				((IDisposable)dlg).Dispose();
			}
		}
		static string CleanText(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}
			return Regex.Replace(text, "[\\x00-\\x1F\\x7F]", "").Trim();
		}
		static HttpClient CreateHttpClient(Dictionary<string, string> customHeaders, string ipPort)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
			HttpClient client = new HttpClient((HttpMessageHandler)new HttpClientHandler
			{
				MaxConnectionsPerServer = 32,
				AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
				UseCookies = false,
				AllowAutoRedirect = true,
				MaxAutomaticRedirections = 5
			});
			((HttpHeaders)client.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
			((HttpHeaders)client.DefaultRequestHeaders).Add("Accept-Language", "zh-CN,zh;q=0.9");
			((HttpHeaders)client.DefaultRequestHeaders).Add("Accept", "*/*");
			if (!string.IsNullOrEmpty(ipPort))
			{
				string ipOnly = ipPort.Split(':')[0];
				((HttpHeaders)client.DefaultRequestHeaders).Add("Referer", "http://" + ipOnly + "/");
			}
			if (customHeaders != null)
			{
				foreach (KeyValuePair<string, string> kv in customHeaders)
				{
					try
					{
						((HttpHeaders)client.DefaultRequestHeaders).Add(kv.Key, kv.Value);
					}
					catch
					{
					}
				}
			}
			return client;
		}
		static string GetUniqueFilePath(string baseName, string ext)
		{
			string workDir = Application.StartupPath;
			int num = 0;
			string fullPath;
			while (true)
			{
				string fname = ((num == 0) ? (baseName + ext) : $"{baseName}_{num}{ext}");
				fullPath = Path.Combine(workDir, fname);
				if (!File.Exists(fullPath))
				{
					break;
				}
				num++;
			}
			return fullPath;
		}
		static Dictionary<string, string> ParseHeaders(string headerText)
		{
			Dictionary<string, string> headers = new Dictionary<string, string>();
			if (string.IsNullOrWhiteSpace(headerText))
			{
				return headers;
			}
			string[] array = headerText.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in array)
			{
				int idx = line.IndexOf(':');
				if (idx > 0)
				{
					string key = line.Substring(0, idx).Trim();
					string value = line.Substring(idx + 1).Trim();
					headers[key] = value;
				}
			}
			return headers;
		}
		static bool SafeWriteFile(string filePath, string content)
		{
			try
			{
				File.WriteAllText(filePath, content, Encoding.UTF8);
				return true;
			}
			catch
			{
				return false;
			}
		}
		static bool ValidateIpPort(string ipPort, out string error, out string fullIpPort)
		{
			error = "";
			fullIpPort = "";
			if (string.IsNullOrWhiteSpace(ipPort))
			{
				error = "IP端口不能为空";
				return false;
			}
			ipPort = ipPort.Trim();
			Match matchWithPort = Regex.Match(ipPort, "(?:http://|https://)?((?:\\d{1,3}\\.){3}\\d{1,3}:\\d{2,5})");
			if (matchWithPort.Success)
			{
				fullIpPort = matchWithPort.Groups[1].Value;
			}
			else
			{
				Match matchIpOnly = Regex.Match(ipPort, "(?:http://|https://)?((?:\\d{1,3}\\.){3}\\d{1,3})");
				if (matchIpOnly.Success)
				{
					fullIpPort = matchIpOnly.Groups[1].Value + ":80";
				}
				else
				{
					error = "IP端口格式错误，示例：110.72.103.69:8181 或 110.72.103.69";
					return false;
				}
			}
			string[] parts = fullIpPort.Split(':');
			string[] ipParts = parts[0].Split('.');
			if (ipParts.Length != 4)
			{
				error = "IPv4分段错误：" + parts[0];
				return false;
			}
			foreach (string seg in ipParts)
			{
				if (!int.TryParse(seg, out var val) || val < 0 || val > 255)
				{
					error = "非法IP段：" + seg;
					return false;
				}
			}
			if (!int.TryParse(parts[1], out var port) || port < 1 || port > 65535)
			{
				error = "非法端口：" + parts[1];
				return false;
			}
			return true;
		}
	}

	private void AddChannelToList(string content, string baseUrl, DateTime parseTime = default(DateTime))
	{
		if (parseTime == default(DateTime))
		{
			parseTime = DateTime.Now;
		}
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
				MatchCollection jsonMatches = Regex.Matches(content, "\"(name|title|channel)\":\\s*\"([^\"]+)\"");
				MatchCollection urlMatches = Regex.Matches(content, "\"(url|link|src)\":\\s*\"([^\"]+)\"");
				for (int i = 0; i < jsonMatches.Count && i < urlMatches.Count; i++)
				{
					string name = jsonMatches[i].Groups[2].Value;
					string url = urlMatches[i].Groups[2].Value;
					if (url.StartsWith("/"))
					{
						url = baseUrl.Replace(baseUrl.Split('/')[3], "") + url.TrimStart('/');
					}
					allChannels.Add(new ChannelInfo
					{
						Name = name,
						Url = url,
						Group = "解析结果",
						Status = "未检测",
						ParseDateTime = parseTime
					});
				}
			}
			else if (content.Contains(".m3u8"))
			{
				MatchCollection m3u8Matches = Regex.Matches(content, "^#EXTINF:\\d+,\\s*(.+)$", RegexOptions.Multiline);
				MatchCollection urlMatches2 = Regex.Matches(content, "^(http[^\\n]+)$", RegexOptions.Multiline);
				for (int j = 0; j < m3u8Matches.Count && j < urlMatches2.Count; j++)
				{
					string name2 = m3u8Matches[j].Groups[1].Value;
					string url2 = urlMatches2[j].Groups[1].Value;
					if (!url2.StartsWith("http"))
					{
						url2 = baseUrl + "/" + url2;
					}
					allChannels.Add(new ChannelInfo
					{
						Name = name2,
						Url = url2,
						Group = "解析结果",
						Status = "未检测",
						ParseDateTime = parseTime
					});
				}
			}
			else
			{
				if (!content.Contains(".txt"))
				{
					return;
				}
				string[] array = content.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string line in array)
				{
					if (line.StartsWith("http"))
					{
						string name3 = "直播源";
						int idx = line.IndexOf(",");
						if (idx > 0)
						{
							name3 = line.Substring(0, idx);
						}
						allChannels.Add(new ChannelInfo
						{
							Name = name3,
							Url = line,
							Group = "解析结果",
							Status = "未检测",
							ParseDateTime = parseTime
						});
					}
				}
			}
		}
		catch
		{
		}
	}

	private void ParseZhgxTv(string content, string baseUrl, DateTime parseTime)
	{
		string[] array = content.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string trimmedLine = array[i].Trim();
			if (string.IsNullOrEmpty(trimmedLine))
			{
				continue;
			}
			string[] parts = trimmedLine.Split(new char[1] { ',' }, 2);
			if (parts.Length >= 2)
			{
				string name = CleanText(parts[0].Trim());
				string url = parts[1].Trim();
				if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
				{
					allChannels.Add(new ChannelInfo
					{
						Name = name,
						Url = url,
						Group = "解析结果",
						Status = "未检测",
						ParseDateTime = parseTime
					});
				}
			}
		}
	}

	private void ParseKutvJson(string content, string baseUrl, DateTime parseTime)
	{
		try
		{
			Match ipPortMatch = Regex.Match(baseUrl, "http://([^/]+)");
			string baseHttp = (ipPortMatch.Success ? ipPortMatch.Value : baseUrl);
			MatchCollection nameMatches = Regex.Matches(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
			MatchCollection urlMatches = Regex.Matches(content, "\"url\"\\s*:\\s*\"([^\"]+)\"");
			for (int i = 0; i < nameMatches.Count && i < urlMatches.Count; i++)
			{
				string name = CleanText(nameMatches[i].Groups[1].Value);
				string relUrl = urlMatches[i].Groups[1].Value;
				if (!string.IsNullOrEmpty(relUrl))
				{
					string fullUrl = (relUrl.StartsWith("http") ? relUrl : (baseHttp + relUrl));
					allChannels.Add(new ChannelInfo
					{
						Name = name,
						Url = fullUrl,
						Group = "解析结果",
						Status = "未检测",
						ParseDateTime = parseTime
					});
				}
			}
		}
		catch
		{
		}
	}

	private void ParseHuashiM3u8(string content, string baseUrl, DateTime parseTime)
	{
		if (content.Contains("#EXTM3U"))
		{
			string[] array = content.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			string name = "";
			string[] array2 = array;
			foreach (string line in array2)
			{
				if (line.StartsWith("#EXTINF:"))
				{
					int commaIdx = line.IndexOf(',');
					name = ((commaIdx > 0) ? CleanText(line.Substring(commaIdx + 1).Trim()) : "华视频道");
				}
				else if (line.StartsWith("http") && !string.IsNullOrEmpty(name))
				{
					allChannels.Add(new ChannelInfo
					{
						Name = name,
						Url = line.Trim(),
						Group = "解析结果",
						Status = "未检测",
						ParseDateTime = parseTime
					});
					name = "";
				}
			}
		}
		else if (!string.IsNullOrEmpty(content) && content.Length < 500)
		{
			allChannels.Add(new ChannelInfo
			{
				Name = "华视频道",
				Url = baseUrl,
				Group = "解析结果",
				Status = "未检测",
				ParseDateTime = parseTime
			});
		}
	}

	private string CleanText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Regex.Replace(text, "[\\x00-\\x1F\\x7F]", "").Trim();
	}

	public IPTVLiveCheckerMain()
	{
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Expected O, but got Unknown
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Expected O, but got Unknown
		InitializeComponent();
		DarkMessageBox.IsDarkProvider = () => theme != null && DrawingUtils.IsDarkColor(theme.Bg);
		DarkMessageBox.ThemeProvider = () => theme;
		DarkMessageBox.DpiScale = dpiScale;
		base.FormBorderStyle = FormBorderStyle.None;
		DoubleBuffered = true;
		SetStyle(ControlStyles.ResizeRedraw, value: true);
		base.KeyPreview = true;
		AllowDrop = true;
		base.DragEnter += IPTVLiveCheckerMain_DragEnter;
		base.DragDrop += IPTVLiveCheckerMain_DragDrop;
		base.KeyDown += IPTVLiveCheckerMain_KeyDown;
		HttpClientHandler handler = new HttpClientHandler
		{
			MaxConnectionsPerServer = 32,
			AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
			UseCookies = false,
			AllowAutoRedirect = true,
			MaxAutomaticRedirections = 5
		};
		httpClient = new HttpClient((HttpMessageHandler)(object)handler);
		httpClient.Timeout = TimeSpan.FromSeconds(120.0);
		((HttpHeaders)httpClient.DefaultRequestHeaders).Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/126.0.0.0 Safari/537.36");
		((HttpHeaders)httpClient.DefaultRequestHeaders).Add("Accept", "*/*");
		((HttpHeaders)httpClient.DefaultRequestHeaders).Add("Accept-Language", "zh-CN,zh;q=0.9");
		((HttpHeaders)httpClient.DefaultRequestHeaders).Add("Connection", "keep-alive");
		SystemEvents.UserPreferenceChanged += delegate
		{
			if (themePreference == "跟随系统")
			{
				theme = MakeEffectiveTheme();
				ApplyTheme();
			}
		};
		FindFFplay();
	}

	private void ApplyTheme()
	{
		SuspendLayout();
		try
		{
			BackColor = theme.Border;
			if (outerWrap != null)
			{
				outerWrap.BackColor = theme.Border;
			}
			if (titleBarPanel != null)
			{
				titleBarPanel.BackColor = theme.Bg;
				Color titleBtnHover = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235));
				if (btnThemeToggle != null)
				{
					btnThemeToggle.BackColor = theme.Bg;
					btnThemeToggle.FlatAppearance.MouseOverBackColor = titleBtnHover;
				}
				if (btnMin != null)
				{
					btnMin.BackColor = theme.Bg;
					btnMin.FlatAppearance.MouseOverBackColor = titleBtnHover;
				}
				if (btnMax != null)
				{
					btnMax.BackColor = theme.Bg;
					btnMax.FlatAppearance.MouseOverBackColor = titleBtnHover;
				}
				if (btnClose != null)
				{
					btnClose.BackColor = theme.Bg;
					btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
					btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
				}
				Color navBtnText = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.White : Color.Black);
				navBtnHoverBg = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230));
				if (btnNavDetect != null)
				{
					btnNavDetect.ForeColor = navBtnText;
				}
				if (btnNavSearch != null)
				{
					btnNavSearch.ForeColor = navBtnText;
				}
				if (btnNavSettings != null)
				{
					btnNavSettings.ForeColor = navBtnText;
				}
				if (btnNavAbout != null)
				{
					btnNavAbout.ForeColor = navBtnText;
				}
				if (btnNavFile != null)
				{
					btnNavFile.ForeColor = navBtnText;
				}
				ApplyMenuTheme(fileMenu);
			}
			if (bottomBarRef != null)
			{
				bottomBarRef.BackColor = theme.Bg;
			}
			if (mainArea != null)
			{
				mainArea.BackColor = theme.BgAlt;
				foreach (Control c in mainArea.Controls)
				{
					ApplyThemeToControl(c);
				}
			}
			if (actionArea != null)
			{
				actionArea.BackColor = theme.BgAlt;
				foreach (Control c2 in actionArea.Controls)
				{
					ApplyThemeToControl(c2);
				}
			}
			if (actionSepRef != null)
			{
				actionSepRef.BackColor = theme.Border;
			}
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
			if (darkVScrollBar != null)
			{
				darkVScrollBar.TrackColor = theme.BgAlt;
				darkVScrollBar.ThumbColor = theme.TextSecondary;
				darkVScrollBar.ThumbHoverColor = theme.TextPrimary;
				darkVScrollBar.ThumbPressedColor = theme.Primary;
				darkVScrollBar.BackColor = darkVScrollBar.TrackColor;
				darkVScrollBar.Invalidate();
			}
			if (dgvData != null)
			{
				dgvData.Invalidate();
			}
			if (statusBarContainer != null)
			{
				statusBarContainer.BackColor = theme.Bg;
			}
			if (statusBarRef != null)
			{
				statusBarRef.BackColor = theme.StatusBarBg;
				foreach (Control c3 in statusBarRef.Controls)
				{
					ApplyThemeToControl(c3);
				}
				UpdateStatusBarRegion();
			}
			if (searchPanelRef != null)
			{
				searchPanelRef.BackColor = theme.BgAlt;
				foreach (Control c4 in searchPanelRef.Controls)
				{
					ApplyThemeToControl(c4);
				}
				if (searchBoxHostRef != null)
				{
					searchBoxHostRef.BackColor = theme.Surface;
				}
				if (cboGroupHost != null)
				{
					cboGroupHost.BackColor = theme.BgAlt;
				}
			}
			if (toolbarRef != null)
			{
				toolbarRef.BackColor = theme.BgAlt;
				if (importHost != null)
				{
					importHost.BackColor = theme.BgAlt;
					foreach (Control c5 in importHost.Controls)
					{
						ApplyThemeToControl(c5);
					}
				}
			}
			ThemePreviewPanel();
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
			if (emptyLabel != null)
			{
				emptyLabel.ForeColor = theme.TextSecondary;
			}
		if (dataGridViewContextMenu != null)
		{
			ApplyMenuTheme(dataGridViewContextMenu);
		}
		if (txtSearchBox != null && txtSearchBox.ContextMenuStrip != null)
		{
			ApplyMenuTheme(txtSearchBox.ContextMenuStrip);
		}
		if (fileMenu != null)
		{
			ApplyMenuTheme(fileMenu);
		}
		if (themeMenuStrip != null)
		{
			ApplyMenuTheme(themeMenuStrip);
		}
			LayoutStatusBar(statusBarRef);
			RestoreLabelColors();
			SelectNavItem(currentView);
			SetFormDarkModeTitleBar(this, theme != null && DrawingUtils.IsDarkColor(theme.Bg));
			UpdateScrollBarTheme(mainArea);
			UpdateActionButtonsVisibility();
			SyncPaletteFromTheme();
			ApplyGlassFx();
			ThemeFx.ApplyThemeFx(this, theme);
			// 预览播放器随主题刷新
			try { channelPlayer?.ApplyTheme(theme); } catch { }
			// WebView2 搜索窗口的玻璃导航栏随主题刷新
			try { ApplyWebViewNavTheme(); } catch { }
		}
		finally
		{
			ResumeLayout(performLayout: false);
			if (titleBarPanel != null) titleBarPanel.Invalidate();
			if (mainArea != null) mainArea.Invalidate();
			if (actionArea != null) actionArea.Invalidate();
			if (statusBarRef != null) statusBarRef.Invalidate();
			if (toolbarRef != null) toolbarRef.Invalidate();
			if (searchPanelRef != null) searchPanelRef.Invalidate();
			if (gridContainerRef != null) gridContainerRef.Invalidate();
		}
	}

	/// <summary>
	/// 玻璃/动效主题的"铬"面板半透明化：让背后流动的极光透出，形成毛玻璃质感。
	/// 仅当主题启用 GlassEnabled 或声明了 AnimationType 时生效；否则恢复不透明（保证切回内置主题正常）。
	/// </summary>
	private void ApplyGlassFx()
	{
		if (theme == null)
		{
			return;
		}
		bool immersive = theme.GlassEnabled || !string.IsNullOrEmpty(theme.AnimationType);
		// 软件图标始终不透明，避免背景透显
		if (titleIconRef != null)
		{
			titleIconRef.BackColor = theme.Bg;
		}
		if (immersive)
		{
			// 毛玻璃模式：outerWrap 透明，让极光背景穿过缝隙可见
			// 所有内部面板使用不透明色（WinForms 半透明 BackColor 不支持真正混合）
			if (outerWrap != null) outerWrap.BackColor = Color.Transparent;
			if (titleBarPanel != null) titleBarPanel.BackColor = theme.Bg;
			if (toolbarRef != null) toolbarRef.BackColor = theme.BgAlt;
			if (searchPanelRef != null) searchPanelRef.BackColor = theme.BgAlt;
			if (mainArea != null)
			{
				mainArea.BackColor = theme.Surface;
				mainArea.Padding = new Padding(16, 8, 16, 8);
			}
			if (actionArea != null) actionArea.BackColor = theme.BgAlt;
			if (gridContainerRef != null) gridContainerRef.BackColor = theme.BgAlt;
			if (statusBarContainer != null) statusBarContainer.BackColor = theme.Bg;
			if (statusBarRef != null) statusBarRef.BackColor = theme.StatusBarBg;
			if (bottomBarRef != null) bottomBarRef.BackColor = theme.Bg;
		}
		else
		{
			if (titleBarPanel != null) titleBarPanel.BackColor = theme.Bg;
			if (mainArea != null)
			{
				mainArea.BackColor = theme.BgAlt;
				mainArea.Padding = new Padding(16, 0, 16, 0);
			}
			if (actionArea != null) actionArea.BackColor = theme.BgAlt;
			if (searchPanelRef != null) searchPanelRef.BackColor = theme.BgAlt;
			if (toolbarRef != null) toolbarRef.BackColor = theme.BgAlt;
			if (statusBarContainer != null) statusBarContainer.BackColor = theme.Bg;
			if (statusBarRef != null) statusBarRef.BackColor = theme.StatusBarBg;
			if (bottomBarRef != null) bottomBarRef.BackColor = theme.Bg;
			if (outerWrap != null) outerWrap.BackColor = theme.Border;
		}
	}

	private void SyncPaletteFromTheme()
	{
		try
		{
			config.Window?.Color?.SyncFromTheme(theme);
			config.TitleBar?.Color?.SyncFromTheme(theme);
			config.Navigation?.Color?.SyncFromTheme(theme);
			config.SearchPanel?.Color?.SyncFromTheme(theme);
			config.ActionArea?.Color?.SyncFromTheme(theme);
			config.DataGrid?.Color?.SyncFromTheme(theme);
			config.StatusBar?.Color?.SyncFromTheme(theme);
			config.Pill?.Color?.SyncFromTheme(theme);
			config.DataGridButton?.Color?.SyncFromTheme(theme);
			config.Dialog?.Color?.SyncFromTheme(theme);
			config.StepIndicator?.Color?.SyncFromTheme(theme);
			config.Toast?.Color?.SyncFromTheme(theme);
			config.EmptyState?.Color?.SyncFromTheme(theme);
			config.ContextMenu?.Color?.SyncFromTheme(theme);
			config.ToggleSwitch?.Color?.SyncFromTheme(theme);
		}
		catch
		{
		}
	}

	private void ApplyMenuTheme(ContextMenuStrip cms)
	{
		if (cms == null)
		{
			return;
		}
		(cms.Renderer as AnimatedMenuRenderer)?.Dispose();
		cms.Renderer = null;
		AnimatedMenuRenderer renderer = new AnimatedMenuRenderer(theme);
		cms.Renderer = renderer;
		renderer.Register(cms);
		cms.BackColor = theme.Surface;
		cms.ForeColor = theme.TextPrimary;
		foreach (ToolStripItem item in cms.Items)
		{
			item.ForeColor = theme.TextPrimary;
			if (item is ToolStripMenuItem mi && mi.DropDownItems.Count > 0)
			{
				mi.DropDown.ForeColor = theme.TextPrimary;
				mi.DropDown.BackColor = theme.Surface;
			}
		}
	}

	/// <summary>
	/// 递归刷新设置窗口 Body 内所有控件的颜色，使"恢复默认"后设置窗口与主窗口主题同步。
	/// </summary>
	private void RefreshDialogControlColors(Control parent, AppTheme theme, NeonPalette pal)
	{
		Color cardBg = ControlPaint.Light(theme.Surface, 0.06f);
		Color textColor = theme.TextPrimary;
		Color borderColor = pal.Border;
		foreach (Control c in parent.Controls)
		{
			if (c is Label lbl)
			{
				// 跳过固定颜色的圆点标签
				if (lbl.Text == "●") { }
				else
				{
					lbl.ForeColor = textColor;
					lbl.BackColor = cardBg;
				}
			}
			else if (c is RadioButton rb)
			{
				rb.ForeColor = textColor;
				rb.BackColor = cardBg;
			}
			else if (c is CheckBox cb)
			{
				cb.ForeColor = textColor;
				cb.BackColor = cardBg;
			}
			else if (c is Button btn)
			{
				btn.ForeColor = textColor;
			}
			else if (c is TextBox tb)
			{
				tb.ForeColor = textColor;
				tb.BackColor = cardBg;
			}
			else if (c is ComboBox cmb)
			{
				cmb.ForeColor = textColor;
				cmb.BackColor = cardBg;
			}
			else if (c is Panel pnl)
			{
				// 高度≤1 的为分隔线
				if (pnl.Height <= 1 || pnl.Width <= 1)
				{
					pnl.BackColor = borderColor;
				}
				else
				{
					pnl.BackColor = cardBg;
				}
			}
			// 递归子控件
			if (c.HasChildren)
			{
				RefreshDialogControlColors(c, theme, pal);
			}
		}
	}

	private void ApplyThemeToControl(Control ctrl)
	{
		if (ctrl == null)
		{
			return;
		}
		if (ctrl is Panel p)
		{
			if (p.Parent == statusBarRef)
			{
				if (p.Height <= 1 || p.Width <= 1)
				{
					p.BackColor = theme.Border;
				}
			}
			else if (p.Parent == actionArea && p.Name != null && p.Name.Contains("sep"))
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
			}
			{
				foreach (Control child in p.Controls)
				{
					ApplyThemeToControl(child);
				}
				return;
			}
		}
		if (ctrl is DarkTabControl dtc)
		{
			dtc.ApplyTheme(theme);
			return;
		}
		if (ctrl is Label lbl)
		{
			if (lbl == lblPercent)
			{
				lbl.ForeColor = theme.Primary;
			}
			else if (lbl.Parent == statusBarRef)
			{
				lbl.ForeColor = theme.TextPrimary;
			}
			else if (lbl.Parent == searchPanelRef)
			{
				lbl.ForeColor = theme.TextPrimary;
			}
		}
		if (ctrl is Button btn)
		{
			if (btn.FlatStyle != FlatStyle.Flat)
			{
				if (btn.BackColor == Color.FromArgb(148, 95, 205) || (btn.BackColor.R > 180 && btn.BackColor.G < 150))
				{
					btn.BackColor = theme.Primary;
				}
				else if (btn.BackColor == Color.FromArgb(255, 85, 140))
				{
					btn.BackColor = theme.Accent;
				}
				else if (btn.FlatAppearance.BorderColor == Color.FromArgb(148, 95, 205))
				{
					btn.BackColor = theme.BgAlt;
					btn.ForeColor = theme.PrimaryDark;
				}
				btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, btn.BackColor.R + 15), Math.Min(255, btn.BackColor.G + 15), Math.Min(255, btn.BackColor.B + 15));
			}
			else if (btn.Tag is string tag && tag.StartsWith("sr:"))
			{
				switch (tag.Substring(3))
				{
				case "primary":
					btn.BackColor = theme.Primary;
					break;
				case "accent":
					btn.BackColor = theme.Accent;
					break;
				case "export":
					btn.BackColor = Color.FromArgb(255, 0, 255);
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
				{
					Color cur = btn.BackColor;
					int dPrim = Math.Abs(cur.R - ColorPurple.R) + Math.Abs(cur.G - ColorPurple.G) + Math.Abs(cur.B - ColorPurple.B);
					int dAcc = Math.Abs(cur.R - ColorPink.R) + Math.Abs(cur.G - ColorPink.G) + Math.Abs(cur.B - ColorPink.B);
					btn.BackColor = ((dPrim <= dAcc) ? theme.Primary : theme.Accent);
					break;
				}
				case "parse":
					btn.BackColor = theme.Primary;
					btn.ForeColor = Color.White;
					break;
				}
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
		if (ctrl is ToggleSwitch ts)
		{
			ts.OnColor = theme.SuccessColor;
			ts.OffColor = DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 62, 72) : Color.FromArgb(218, 222, 228);
			ts.Invalidate();
		}
		foreach (Control child2 in ctrl.Controls)
		{
			ApplyThemeToControl(child2);
		}
	}

	private void ThemePreviewPanel()
	{
		if (previewPanel != null)
		{
			previewPanel.BackColor = theme.BgAlt;
			Walk(previewPanel);
		}
		void Walk(Control ctrl)
		{
			if (ctrl is ChannelPlayer)
			{
				return;
			}
			if (ctrl is Panel p)
			{
				if (p.Tag is string tagStr && tagStr == "__gap")
				{
					p.BackColor = theme.BgAlt;
				}
				else if ((p.Dock == DockStyle.Left && p.Width <= 2) || p.Width <= 1)
				{
					p.BackColor = theme.Border;
				}
				else if (p.Dock == DockStyle.Top && p.Height <= 40)
				{
					p.BackColor = theme.Bg;
				}
				else if (p.Dock == DockStyle.Bottom && p.Height <= 48)
				{
					p.BackColor = theme.Bg;
				}
				else
				{
					p.BackColor = theme.BgAlt;
				}
			}
			else if (ctrl is Label lbl)
			{
				lbl.ForeColor = (lbl.Font.Bold ? theme.TextPrimary : theme.TextSecondary);
			}
			else if (ctrl is Button { FlatStyle: FlatStyle.Flat } btn && btn.ForeColor == Color.White)
			{
				btn.BackColor = theme.Surface;
				btn.ForeColor = Color.White;
			}
			foreach (Control child in ctrl.Controls)
			{
				Walk(child);
			}
		}
	}

	private AppTheme MakeEffectiveTheme()
	{
		if (!AppTheme.IsExternalThemesLoaded())
		{
			AppTheme.LoadExternalThemes();
		}
		AppTheme baseTheme;
		if (AppTheme.ExternalThemes.TryGetValue(themePreference, out AppTheme ext))
		{
			baseTheme = ext;
		}
		else if (themePreference == "跟随系统")
		{
			baseTheme = AppTheme.GetAutoTheme();
		}
		else
		{
			// 数据驱动：从内置主题注册表按名查找
			baseTheme = null;
			foreach (AppTheme b in AppTheme.Builtins)
			{
				if (string.Equals(b.Name, themePreference, StringComparison.OrdinalIgnoreCase))
				{
					baseTheme = b;
					break;
				}
			}
			if (baseTheme == null)
			{
				baseTheme = AppTheme.MintCeladon;
			}
		}
		AppTheme effective = baseTheme.Clone();
		if (AnimationSettings.HighContrast)
		{
			effective.ApplyHighContrast();
		}
		return effective;
	}

	private void SetTheme(AppTheme newTheme)
	{
		if (_applyingTheme)
		{
			return;
		}
		_applyingTheme = true;
		try
		{
			theme = MakeEffectiveTheme();
			ApplyTheme();
			// 预览播放器随主题刷新
			try { channelPlayer?.ApplyTheme(theme); } catch { }
			// WebView2 搜索窗口的玻璃导航栏随主题刷新
			try { ApplyWebViewNavTheme(); } catch { }
		}
		finally
		{
			_applyingTheme = false;
		}
	}

	private void UpdateStatusBar()
	{
		if (lblDetected != null && lblAvailable != null && lblPercent != null && statusBarRef != null)
		{
			lblDetected.Text = $"已检测: {detectedCount}/{totalCount}";
			lblAvailable.Text = $"可用: {availableCount}";
			double pct = ((totalCount > 0) ? ((double)detectedCount / (double)totalCount * 100.0) : 0.0);
			lblPercent.Text = $"{pct:F2}%";
			if (totalCount > 0)
			{
				progressBarWidth = (int)((double)statusBarRef.ClientSize.Width * pct / 100.0);
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
		if (theme != null)
		{
			if (lblDetected != null)
			{
				lblDetected.ForeColor = ((lblDetected.Location.X + lblDetected.Width / 2 < progressBarWidth) ? Color.White : theme.TextPrimary);
			}
			if (lblAvailable != null)
			{
				lblAvailable.ForeColor = ((lblAvailable.Location.X + lblAvailable.Width / 2 < progressBarWidth) ? Color.White : theme.TextPrimary);
			}
			if (lblProgressText != null)
			{
				lblProgressText.ForeColor = ((lblProgressText.Location.X + lblProgressText.Width / 2 < progressBarWidth) ? Color.White : theme.TextPrimary);
			}
			if (lblPercent != null)
			{
				lblPercent.ForeColor = ((lblPercent.Location.X + lblPercent.Width / 2 < progressBarWidth) ? Color.White : theme.Primary);
			}
			if (lblStreamInfo != null && lblStreamInfo.Visible)
			{
				lblStreamInfo.ForeColor = ((lblStreamInfo.Location.X + lblStreamInfo.Width / 2 < progressBarWidth) ? Color.White : theme.TextSecondary);
			}
		}
	}

	private void RestoreLabelColors()
	{
		if (theme != null)
		{
			if (lblDetected != null)
			{
				lblDetected.ForeColor = theme.TextPrimary;
			}
			if (lblAvailable != null)
			{
				lblAvailable.ForeColor = theme.TextPrimary;
			}
			if (lblProgressText != null)
			{
				lblProgressText.ForeColor = theme.TextPrimary;
			}
			if (lblPercent != null)
			{
				lblPercent.ForeColor = theme.Primary;
			}
			if (lblStreamInfo != null)
			{
				lblStreamInfo.ForeColor = theme.TextSecondary;
			}
		}
	}

	private void SearchChannels(string keyword)
	{
		if (string.IsNullOrWhiteSpace(keyword) || keyword == "输入搜索内容，按下回车键搜索")
		{
			foreach (ChannelInfo allChannel in allChannels)
			{
				allChannel.Visible = true;
			}
			RefreshGrid();
		}
		else
		{
			RefreshGrid();
			if (dgvData.Rows.Count == 0)
			{
				DarkMessageBox.Show("未找到包含 \"" + keyword + "\" 的频道", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
	}

	private (int newItems, int dupItems) ImportFromFile(string filePath, HashSet<string> existingUrls = null)
	{
		int newItems = 0;
		int dupItems = 0;
		_isImporting = true;
		try
		{
			if (existingUrls == null)
			{
				existingUrls = new HashSet<string>(allChannels.Select((ChannelInfo c) => c.Url.ToLowerInvariant()));
			}
			string[] array = File.ReadAllLines(filePath, Encoding.UTF8);
			string currentName = "";
			string currentGroup = "";
			string[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				string line = array2[num].Trim();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
				{
					int gi = line.IndexOf("group-title=\"", StringComparison.OrdinalIgnoreCase);
					if (gi >= 0)
					{
						int eq = line.IndexOf('"', gi + 13);
						currentGroup = ((eq <= gi) ? "" : line.Substring(gi + 13, eq - gi - 13));
					}
					else
					{
						currentGroup = "";
					}
					int ci = line.LastIndexOf(',');
					if (ci >= 0)
					{
						currentName = ChannelLogoHelper.StandardNameCctvOnly(line.Substring(ci + 1).Trim());
					}
				}
				else
				{
					if (line.StartsWith("#"))
					{
						continue;
					}
					bool added = false;
					string urlToAdd = null;
					string nameToAdd = null;
					string groupToAdd = null;
					if (line.Contains(","))
					{
					int commaIdx = line.IndexOf(',');
					string n = ChannelLogoHelper.StandardNameCctvOnly(line.Substring(0, commaIdx).Trim().Trim('"', ' ', '`'));
					string u = line.Substring(commaIdx + 1).Trim().Trim('"', ' ', '`');
						if (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) || u.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || u.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
						{
							if (string.IsNullOrWhiteSpace(n))
							{
								n = "未命名频道";
							}
							if (u.Contains("#"))
							{
							string[] array3 = u.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
							for (int num2 = 0; num2 < array3.Length; num2++)
							{
								string trimmedUrl = array3[num2].Trim();
								int trailingLogo = trimmedUrl.IndexOf(',');
								if (trailingLogo >= 0)
								{
									trimmedUrl = trimmedUrl.Substring(0, trailingLogo).Trim();
								}
								if (!string.IsNullOrWhiteSpace(trimmedUrl))
									{
										string urlKey = trimmedUrl.ToLowerInvariant();
										if (existingUrls.Contains(urlKey))
										{
											dupItems++;
											continue;
										}
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
						int li = urlToAdd.IndexOf(',');
						if (li >= 0)
						{
							urlToAdd = urlToAdd.Substring(0, li).Trim();
						}
						nameToAdd = n;
							groupToAdd = currentGroup;
							currentName = "";
							added = true;
						}
					}
					if (!added && (line.StartsWith("http", StringComparison.OrdinalIgnoreCase) || line.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || line.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase)))
					{
						if (string.IsNullOrWhiteSpace(currentName))
						{
							currentName = "未命名频道";
						}
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
							nameToAdd = ChannelLogoHelper.StandardNameCctvOnly(p[0].Trim());
							groupToAdd = ((p.Length > 5) ? p[5].Trim() : currentGroup);
							added = true;
						}
					}
					if (added && urlToAdd != null)
					{
						string urlKey2 = urlToAdd.ToLowerInvariant();
						if (existingUrls.Contains(urlKey2))
						{
							dupItems++;
							continue;
						}
						allChannels.Add(new ChannelInfo
						{
							Name = nameToAdd,
							Url = urlToAdd,
							Location = "",
							Resolution = "",
							Speed = "",
							Group = (groupToAdd ?? ""),
							Status = "未检测",
							Visible = true
						});
						existingUrls.Add(urlKey2);
						newItems++;
					}
				}
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("导入文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_isImporting = false;
		}
		return (newItems: newItems, dupItems: dupItems);
	}

	private void PasteFromClipboard()
	{
		_isImporting = true;
		try
		{
			string text = Clipboard.GetText();
			if (string.IsNullOrWhiteSpace(text))
			{
				DarkMessageBox.Show("剪贴板为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			int added = 0;
			int dupCount = 0;
			HashSet<string> existingUrls = new HashSet<string>(allChannels.Select((ChannelInfo c) => c.Url.ToLowerInvariant()));
			string pendingName = null;
			string pendingGroup = "";
			string[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				string line = array2[num].Trim();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
				{
					string info = line.Substring(8);
					int commaIdx = info.LastIndexOf(',');
					string attrs = ((commaIdx >= 0) ? info.Substring(0, commaIdx) : info);
					string chName = ((commaIdx >= 0) ? info.Substring(commaIdx + 1).Trim().Trim('"', ' ', '`') : "");
					Match grpMatch = Regex.Match(attrs, "group-title\\s*=\\s*\"([^\"]*)\"");
					string grp = (grpMatch.Success ? grpMatch.Groups[1].Value.Trim() : "");
					Match tvgNameMatch = Regex.Match(attrs, "tvg-name\\s*=\\s*\"([^\"]*)\"");
					if (tvgNameMatch.Success && !string.IsNullOrWhiteSpace(tvgNameMatch.Groups[1].Value))
					{
						string tn = tvgNameMatch.Groups[1].Value.Trim().Trim('"', ' ', '`');
						if (!string.IsNullOrWhiteSpace(tn) && string.IsNullOrWhiteSpace(chName))
						{
							chName = tn;
						}
					}
					if (string.IsNullOrWhiteSpace(chName))
					{
						chName = "粘贴链接";
					}
					pendingName = ChannelLogoHelper.StandardNameCctvOnly(chName);
					pendingGroup = grp;
				}
				else
				{
					if (line.StartsWith("#"))
					{
						continue;
					}
					bool pasted = false;
					bool isUrl = line.StartsWith("http", StringComparison.OrdinalIgnoreCase) || line.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || line.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
					bool hasBtUrl = Regex.IsMatch(line, "`https?://");
					string btUrlOnly = null;
					if (!isUrl && hasBtUrl)
					{
						Match btm = Regex.Match(line, "`([^`]+)`");
						if (btm.Success)
						{
							string bu = btm.Groups[1].Value.Trim();
							if (bu.StartsWith("http", StringComparison.OrdinalIgnoreCase) || bu.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || bu.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
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
					int commaIdx2 = line.IndexOf(',');
					string n = ChannelLogoHelper.StandardNameCctvOnly(line.Substring(0, commaIdx2).Trim().Trim('"', ' ', '`'));
						string afterComma = line.Substring(commaIdx2 + 1);
						string u2 = afterComma.Trim().Trim('"', ' ', '`');
						bool uValid = u2.StartsWith("http", StringComparison.OrdinalIgnoreCase) || u2.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || u2.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase);
						if (!uValid)
						{
							Match btMatch = Regex.Match(afterComma, "`([^`]+)`");
							if (btMatch.Success)
							{
								string btUrl = btMatch.Groups[1].Value.Trim().Trim('"', ' ', '`');
								if (btUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || btUrl.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || btUrl.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
								{
									u2 = btUrl;
									uValid = true;
								}
							}
						}
						if (!uValid)
						{
							Match urlMatch = Regex.Match(afterComma, "(https?://[^\\s`\"<>]+)");
							if (urlMatch.Success)
							{
								u2 = urlMatch.Groups[1].Value.Trim();
								uValid = true;
							}
						}
						if (uValid)
						{
							if (string.IsNullOrWhiteSpace(n))
							{
								n = "粘贴链接";
							}
							if (u2.Contains("#"))
							{
								string[] array3 = u2.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
								for (int num2 = 0; num2 < array3.Length; num2++)
								{
									string trimmedUrl = array3[num2].Trim();
									if (!string.IsNullOrWhiteSpace(trimmedUrl))
									{
										string singleUrlKey = trimmedUrl.ToLowerInvariant();
										if (existingUrls.Contains(singleUrlKey))
										{
											dupCount++;
											continue;
										}
										allChannels.Add(new ChannelInfo
										{
											Name = n,
											Url = trimmedUrl,
											Status = "未检测",
											Visible = true
										});
										existingUrls.Add(singleUrlKey);
										added++;
									}
								}
								pasted = true;
								continue;
							}
							string urlKey2 = u2.ToLowerInvariant();
							if (existingUrls.Contains(urlKey2))
							{
								dupCount++;
							}
							else
							{
								allChannels.Add(new ChannelInfo
								{
									Name = n,
									Url = u2,
									Status = "未检测",
									Visible = true
								});
								existingUrls.Add(urlKey2);
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
							Match btUrlMatch = Regex.Match(line, "`([^`]+)`");
							if (btUrlMatch.Success)
							{
								string bu2 = btUrlMatch.Groups[1].Value.Trim();
								if (bu2.StartsWith("http", StringComparison.OrdinalIgnoreCase))
								{
									cleanedUrl = bu2;
								}
							}
						}
						string urlKey3 = cleanedUrl.ToLowerInvariant();
						if (existingUrls.Contains(urlKey3))
						{
							dupCount++;
						}
						else
						{
							allChannels.Add(new ChannelInfo
							{
								Name = "粘贴链接",
								Url = cleanedUrl,
								Status = "未检测",
								Visible = true
							});
							existingUrls.Add(urlKey3);
							added++;
						}
						pasted = true;
					}
					if (pasted || !line.Contains("|"))
					{
						continue;
					}
					string[] p = line.Split('|');
					if (p.Length > 1 && !string.IsNullOrWhiteSpace(p[1]))
					{
						string urlKey4 = p[1].Trim().ToLowerInvariant();
						if (existingUrls.Contains(urlKey4))
						{
							dupCount++;
							continue;
						}
						allChannels.Add(new ChannelInfo
						{
							Name = p[0].Trim(),
							Url = p[1].Trim(),
							Location = ((p.Length > 2) ? p[2].Trim() : ""),
							Resolution = ((p.Length > 3) ? p[3].Trim() : ""),
							Speed = ((p.Length > 4) ? p[4].Trim() : ""),
							Group = ((p.Length > 5) ? p[5].Trim() : ""),
							Status = "未检测",
							Visible = true
						});
						existingUrls.Add(urlKey4);
						added++;
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
				if (dupCount > 0)
				{
					msg += $"\n跳过重复链接 {dupCount} 条";
				}
				DarkMessageBox.Show(msg, "粘贴成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("粘贴失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			_isImporting = false;
		}
	}

	private async Task<bool> DetectSingleChannel(ChannelInfo ch, int timeout, CancellationToken token)
	{
		Stopwatch sw = Stopwatch.StartNew();
		bool ok = false;
		string speed = "";
		string resolution = "";
		string location = ch.Location;
		Task<string> ipLocTask = null;
		string ipHost = "";
		string domainHost = "";
		try
		{
			string h0 = new Uri(ch.Url).Host;
			if (IPAddress.TryParse(h0, out var tip) && tip.GetAddressBytes().Length == 4)
			{
				ipHost = h0;
			}
			else
			{
				domainHost = h0;
			}
		}
		catch
		{
		}
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
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
				using (CancellationTokenSource ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
				{
					using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token);
					HttpResponseMessage resp = await httpClient.SendAsync(request, (HttpCompletionOption)1, linkedCts.Token);
					sw.Stop();
					int statusCode = (int)resp.StatusCode;
					ok = statusCode >= 200 && statusCode < 400;
					if (!ok && statusCode == 416)
					{
						request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
						resp = await httpClient.SendAsync(request, (HttpCompletionOption)1, linkedCts.Token);
						sw.Stop();
						statusCode = (int)resp.StatusCode;
						ok = statusCode >= 200 && statusCode < 400;
					}
					if (!ok && (statusCode == 403 || statusCode == 405))
					{
						ok = true;
					}
					if (ok)
					{
						MediaTypeHeaderValue contentType = resp.Content.Headers.ContentType;
						string contentType2 = ((contentType != null) ? contentType.MediaType : null) ?? "";
						if (contentType2.Contains("mpegurl") || contentType2.Contains("x-mpegurl") || ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							try
							{
								byte[] buf = new byte[16384];
								using Stream stream = await resp.Content.ReadAsStreamAsync();
								int totalRead = 0;
								while (totalRead < buf.Length)
								{
									int n = await stream.ReadAsync(buf, totalRead, buf.Length - totalRead, linkedCts.Token);
									if (n <= 0)
									{
										break;
									}
									totalRead += n;
									string s = Encoding.UTF8.GetString(buf, 0, totalRead);
									if (totalRead > 2048 && !s.Contains("#EXT-X-STREAM-INF") && s.Contains("#EXTINF"))
									{
										break;
									}
								}
								string snippet = Encoding.UTF8.GetString(buf, 0, totalRead);
								if (snippet.IndexOf("#EXTM3U", StringComparison.Ordinal) >= 0 || snippet.IndexOf("#EXTINF", StringComparison.Ordinal) >= 0 || snippet.IndexOf("#EXT-X-", StringComparison.Ordinal) >= 0 || snippet.IndexOf(".ts", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("mpegurl", StringComparison.Ordinal) >= 0)
								{
									ok = true;
								}
								else if (contentType2.IndexOf("mpegurl", StringComparison.Ordinal) < 0 && statusCode == 200)
								{
									ok = true;
								}
								MatchCollection matchCollection = RxResolution.Matches(snippet);
								int maxW = 0;
								int maxH = 0;
								foreach (Match m in matchCollection)
								{
									if (int.TryParse(m.Groups[1].Value, out var w) && int.TryParse(m.Groups[2].Value, out var h1) && w * h1 > maxW * maxH)
									{
										maxW = w;
										maxH = h1;
									}
								}
								if (maxW > 0 && maxH > 0)
								{
									resolution = $"{maxW}x{maxH}";
								}
							}
							catch
							{
								ok = statusCode >= 200 && statusCode < 400;
							}
						}
						else if (contentType2.Contains("video") || contentType2.Contains("flv") || contentType2.Contains("mp4") || contentType2.Contains("octet-stream") || contentType2.Contains("audio"))
						{
							try
							{
								byte[] buf = new byte[8192];
								using Stream stream = await resp.Content.ReadAsStreamAsync();
								int n2 = await stream.ReadAsync(buf, 0, buf.Length, linkedCts.Token);
								if (!Encoding.ASCII.GetString(buf, 0, Math.Min(n2, 16)).StartsWith("FLV"))
								{
									ok = n2 <= 11 || buf[4] != 102 || buf[5] != 116 || buf[6] != 121 || buf[7] != 112 || true;
								}
								else
								{
									ok = true;
									if (n2 > 11)
									{
										int width = 0;
										int height = 0;
										for (int i = 11; i < n2 - 18; i++)
										{
											if (buf[i] == 9 && i + 15 < n2)
											{
												width = (buf[i + 13] << 8) | buf[i + 14];
												height = (buf[i + 14] << 8) | buf[i + 15];
												break;
											}
										}
										if (width > 0 && height > 0)
										{
											resolution = $"{width}x{height}";
										}
									}
								}
							}
							catch
							{
								ok = true;
							}
						}
						else if (ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							ok = true;
						}
						speed = $"{sw.ElapsedMilliseconds}ms";
						if (string.IsNullOrEmpty(location))
						{
							location = ExtractLocationFromUrl(ch.Url);
						}
					}
					resp.Dispose();
				}
				if (ok && string.IsNullOrEmpty(resolution))
				{
					resolution = await GetResolutionWithFallback(ch.Url, token);
				}
				if (ok && string.IsNullOrEmpty(resolution))
				{
					resolution = "直播";
				}
			}
			else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
			{
				ok = true;
				sw.Stop();
				speed = $"{sw.ElapsedMilliseconds}ms";
				resolution = "直播";
				if (string.IsNullOrEmpty(location))
				{
					location = ExtractLocationFromUrl(ch.Url);
				}
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
				if (!string.IsNullOrEmpty(ipLoc))
				{
					location = ipLoc;
				}
			}
			catch
			{
			}
		}
		if (string.IsNullOrEmpty(location))
		{
			location = ExtractLocationFromUrl(ch.Url);
		}
		ch.Status = (ok ? "可用" : "不可用");
		ch.Speed = speed;
		ch.Resolution = resolution;
		ch.Location = location;
		return ok;
	}

	private async Task StartDetection()
	{
		cts = new CancellationTokenSource();
		CancellationToken token = cts.Token;
		if (btnStopDetect != null)
		{
			btnStopDetect.Enabled = true;
		}
		detectedCount = 0;
		availableCount = 0;
		totalCount = allChannels.Count;
		Parallel.ForEach(allChannels, delegate(ChannelInfo ch)
		{
			ch.Status = "未检测";
			ch.Speed = "";
			if (string.IsNullOrWhiteSpace(ch.Location))
			{
				ch.Location = ExtractLocationFromUrl(ch.Url);
			}
			if (string.IsNullOrWhiteSpace(ch.Resolution))
			{
				ch.Resolution = "";
			}
		});
		if (lblProgressText != null && !lblProgressText.IsDisposed)
		{
			lblProgressText.Text = "检测进度:";
		}
		RefreshGrid();
		UpdateStatusBar();
		int concurrency = Math.Min(detectConcurrency, allChannels.Count);
		int uiUpdateCounter = 0;
		int uiRefreshNeeded = 0;
		int lastRefreshPercent = -1;
		System.Windows.Forms.Timer uiRefreshTimer = new System.Windows.Forms.Timer
		{
			Interval = 500
		};
		uiRefreshTimer.Tick += delegate
		{
			if (Interlocked.Exchange(ref uiRefreshNeeded, 0) == 1)
			{
				int num = ((totalCount > 0) ? ((int)((double)detectedCount / (double)totalCount * 100.0)) : 0);
				if (num != lastRefreshPercent || detectedCount == totalCount)
				{
					lastRefreshPercent = num;
					RefreshGrid();
				}
				UpdateStatusBar();
			}
		};
		uiRefreshTimer.Start();
		try
		{
			SemaphoreSlim sem = new SemaphoreSlim(concurrency, concurrency);
			try
			{
				IEnumerable<Task> tasks = ((IEnumerable<ChannelInfo>)allChannels).Select((Func<ChannelInfo, Task>)async delegate(ChannelInfo ch)
				{
					await sem.WaitAsync(token);
					Task<string> ipLocTask = null;
					try
					{
						while (isPaused && !token.IsCancellationRequested)
						{
							await Task.Delay(100);
						}
						token.ThrowIfCancellationRequested();
						ch.Status = "检测中";
						Stopwatch sw = Stopwatch.StartNew();
						bool ok = false;
						string speed = "";
						string resolution = "";
						string location = ch.Location;
						string ipHost = "";
						string domainHost = "";
						try
						{
							string h0 = new Uri(ch.Url).Host;
							if (IPAddress.TryParse(h0, out var tip) && tip.GetAddressBytes().Length == 4)
							{
								ipHost = h0;
							}
							else
							{
								domainHost = h0;
							}
						}
						catch
						{
						}
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
								using CancellationTokenSource ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
								using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token);
								bool isUrlM3u8 = ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0;
								bool isUrlFlv = ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0;
								bool isUrlMp4 = ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0;
								bool isUrlTs = ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0;
								HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
								HttpResponseMessage resp = await httpClient.SendAsync(request, (HttpCompletionOption)1, linkedCts.Token);
								try
								{
									sw.Stop();
									int statusCode = (int)resp.StatusCode;
									ok = statusCode >= 200 && statusCode < 400;
									if (!ok && (statusCode == 403 || statusCode == 405 || statusCode == 416))
									{
										ok = true;
									}
									if (ok)
									{
										MediaTypeHeaderValue contentType = resp.Content.Headers.ContentType;
										string contentType2 = ((contentType != null) ? contentType.MediaType : null) ?? "";
										bool isContentTypeVideo = contentType2.IndexOf("video", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("flv", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("mp4", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("octet-stream", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("audio", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("mpegurl", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("x-mpegurl", StringComparison.Ordinal) >= 0;
										if (isUrlM3u8 || isContentTypeVideo)
										{
											if (isUrlM3u8)
											{
												try
												{
													byte[] buf = new byte[16384];
													using Stream stream = await resp.Content.ReadAsStreamAsync();
													int totalRead = 0;
													while (totalRead < buf.Length)
													{
														int n = await stream.ReadAsync(buf, totalRead, buf.Length - totalRead, linkedCts.Token);
														if (n <= 0)
														{
															break;
														}
														totalRead += n;
														string s = Encoding.UTF8.GetString(buf, 0, totalRead);
														if (totalRead > 2048 && !s.Contains("#EXT-X-STREAM-INF") && s.Contains("#EXTINF"))
														{
															break;
														}
													}
													string snippet = Encoding.UTF8.GetString(buf, 0, totalRead);
													if (snippet.IndexOf("#EXTM3U", StringComparison.Ordinal) >= 0 || snippet.IndexOf("#EXTINF", StringComparison.Ordinal) >= 0 || snippet.IndexOf("#EXT-X-", StringComparison.Ordinal) >= 0 || snippet.IndexOf(".ts", StringComparison.Ordinal) >= 0 || contentType2.IndexOf("mpegurl", StringComparison.Ordinal) >= 0)
													{
														ok = true;
													}
													else if (!contentType2.Contains("mpegurl") && statusCode == 200)
													{
														ok = true;
													}
													MatchCollection matchCollection = RxResolution.Matches(snippet);
													int maxW = 0;
													int maxH = 0;
													foreach (Match m in matchCollection)
													{
														if (int.TryParse(m.Groups[1].Value, out var w) && int.TryParse(m.Groups[2].Value, out var h1) && w * h1 > maxW * maxH)
														{
															maxW = w;
															maxH = h1;
														}
													}
													if (maxW > 0 && maxH > 0)
													{
														resolution = $"{maxW}x{maxH}";
													}
												}
												catch
												{
													ok = statusCode >= 200 && statusCode < 400;
												}
											}
										}
										else if (isUrlFlv || isUrlMp4 || isUrlTs)
										{
											ok = true;
										}
										speed = $"{sw.ElapsedMilliseconds}ms";
										if (string.IsNullOrEmpty(location))
										{
											location = ExtractLocationFromUrl(ch.Url);
										}
									}
								}
								finally
								{
									((IDisposable)resp)?.Dispose();
								}
							}
							catch (HttpRequestException)
							{
								ok = false;
							}
							catch (OperationCanceledException)
							{
								ok = false;
							}
							catch
							{
								ok = false;
							}
						}
						else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
						{
							ok = true;
							sw.Stop();
							speed = $"{sw.ElapsedMilliseconds}ms";
							resolution = "直播";
							if (string.IsNullOrEmpty(location))
							{
								location = ExtractLocationFromUrl(ch.Url);
							}
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
							catch
							{
							}
						}
						if (ok && string.IsNullOrEmpty(resolution))
						{
							resolution = "直播";
						}
						if (ipLocTask != null && ok)
						{
							try
							{
								string ipLoc = await ipLocTask;
								if (!string.IsNullOrEmpty(ipLoc))
								{
									location = ipLoc;
								}
							}
							catch
							{
							}
						}
						if (string.IsNullOrEmpty(location))
						{
							location = ExtractLocationFromUrl(ch.Url);
						}
						ch.Status = (ok ? "可用" : "不可用");
						ch.Speed = speed;
						ch.Resolution = resolution;
						ch.Location = location;
						Interlocked.Increment(ref detectedCount);
						if (ok)
						{
							Interlocked.Increment(ref availableCount);
						}
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
					await Task.WhenAll(tasks);
				}
				catch (OperationCanceledException)
				{
				}
			}
			finally
			{
				if (sem != null)
				{
					((IDisposable)sem).Dispose();
				}
			}
			List<ChannelInfo> failedChannels = allChannels.Where((ChannelInfo c) => c.Status == "不可用").ToList();
			if (failedChannels.Count > 0 && !token.IsCancellationRequested)
			{
				int fallbackConcurrency = Math.Max(1, Math.Min(5, concurrency / 2));
				int fallbackTimeout = timeoutSeconds * 2;
				SemaphoreSlim sem2 = new SemaphoreSlim(fallbackConcurrency, fallbackConcurrency);
				try
				{
					IEnumerable<Task> fallbackTasks = ((IEnumerable<ChannelInfo>)failedChannels).Select((Func<ChannelInfo, Task>)async delegate(ChannelInfo ch)
					{
						await sem2.WaitAsync(token);
						try
						{
							while (isPaused && !token.IsCancellationRequested)
							{
								await Task.Delay(100);
							}
							token.ThrowIfCancellationRequested();
							ch.Status = "复检中";
							Interlocked.Exchange(ref uiRefreshNeeded, 1);
							Task<string> ipLocTask = null;
							Stopwatch sw = Stopwatch.StartNew();
							bool ok = false;
							string speed = "";
							string resolution = "";
							string location = ch.Location;
							string ipHost = "";
							string domainHost = "";
							try
							{
								string h0 = new Uri(ch.Url).Host;
								if (IPAddress.TryParse(h0, out var tip) && tip.GetAddressBytes().Length == 4)
								{
									ipHost = h0;
								}
								else
								{
									domainHost = h0;
								}
							}
							catch
							{
							}
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
										HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
										using CancellationTokenSource ctsInner = new CancellationTokenSource(TimeSpan.FromSeconds(fallbackTimeout));
										using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctsInner.Token);
										HttpResponseMessage resp = await httpClient.SendAsync(request, (HttpCompletionOption)1, linkedCts.Token);
										sw.Stop();
										int statusCode = (int)resp.StatusCode;
										ok = statusCode >= 200 && statusCode < 400;
										if (!ok && statusCode == 416)
										{
											request = new HttpRequestMessage(HttpMethod.Get, ch.Url);
											resp = await httpClient.SendAsync(request, (HttpCompletionOption)1, linkedCts.Token);
											sw.Stop();
											statusCode = (int)resp.StatusCode;
											ok = statusCode >= 200 && statusCode < 400;
										}
										if (!ok && (statusCode == 403 || statusCode == 405))
										{
											ok = true;
										}
										if (ok)
										{
											MediaTypeHeaderValue contentType = resp.Content.Headers.ContentType;
											string contentType2 = ((contentType != null) ? contentType.MediaType : null) ?? "";
											if (contentType2.Contains("mpegurl") || contentType2.Contains("x-mpegurl") || ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0)
											{
												try
												{
													byte[] buf = new byte[16384];
													using Stream stream = await resp.Content.ReadAsStreamAsync();
													int totalRead = 0;
													while (totalRead < buf.Length)
													{
														int n = await stream.ReadAsync(buf, totalRead, buf.Length - totalRead, linkedCts.Token);
														if (n <= 0)
														{
															break;
														}
														totalRead += n;
														string s = Encoding.UTF8.GetString(buf, 0, totalRead);
														if (totalRead > 2048 && !s.Contains("#EXT-X-STREAM-INF") && s.Contains("#EXTINF"))
														{
															break;
														}
													}
													string snippet = Encoding.UTF8.GetString(buf, 0, totalRead);
													if (snippet.IndexOf("#EXTM3U", StringComparison.Ordinal) < 0 && snippet.IndexOf("#EXTINF", StringComparison.Ordinal) < 0 && snippet.IndexOf("#EXT-X-", StringComparison.Ordinal) < 0 && snippet.IndexOf(".ts", StringComparison.Ordinal) < 0 && contentType2.IndexOf("mpegurl", StringComparison.Ordinal) < 0 && contentType2.IndexOf("mpegurl", StringComparison.Ordinal) < 0 && statusCode != 200)
													{
														ok = false;
													}
													MatchCollection matchCollection = RxResolution.Matches(snippet);
													int maxW = 0;
													int maxH = 0;
													foreach (Match m in matchCollection)
													{
														if (int.TryParse(m.Groups[1].Value, out var w) && int.TryParse(m.Groups[2].Value, out var h1) && w * h1 > maxW * maxH)
														{
															maxW = w;
															maxH = h1;
														}
													}
													if (maxW > 0 && maxH > 0)
													{
														resolution = $"{maxW}x{maxH}";
													}
												}
												catch
												{
													ok = statusCode >= 200 && statusCode < 400;
												}
											}
											else if (contentType2.Contains("video") || contentType2.Contains("flv") || contentType2.Contains("mp4") || contentType2.Contains("octet-stream") || contentType2.Contains("audio"))
											{
												try
												{
													byte[] buf = new byte[8192];
													using Stream stream = await resp.Content.ReadAsStreamAsync();
													int n2 = await stream.ReadAsync(buf, 0, buf.Length, linkedCts.Token);
													if (!Encoding.ASCII.GetString(buf, 0, Math.Min(n2, 16)).StartsWith("FLV"))
													{
														ok = n2 <= 11 || buf[4] != 102 || buf[5] != 116 || buf[6] != 121 || buf[7] != 112 || true;
													}
													else
													{
														ok = true;
														if (n2 > 11)
														{
															int width = 0;
															int height = 0;
															for (int i = 11; i < n2 - 18; i++)
															{
																if (buf[i] == 9 && i + 15 < n2)
																{
																	width = (buf[i + 13] << 8) | buf[i + 14];
																	height = (buf[i + 14] << 8) | buf[i + 15];
																	break;
																}
															}
															if (width > 0 && height > 0)
															{
																resolution = $"{width}x{height}";
															}
														}
													}
												}
												catch
												{
													ok = true;
												}
											}
											else if (ch.Url.IndexOf(".m3u8", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".flv", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".mp4", StringComparison.OrdinalIgnoreCase) >= 0 || ch.Url.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0)
											{
												ok = true;
											}
											speed = $"{sw.ElapsedMilliseconds}ms";
											if (string.IsNullOrEmpty(location))
											{
												location = ExtractLocationFromUrl(ch.Url);
											}
										}
										resp.Dispose();
									}
									catch (HttpRequestException)
									{
										ok = false;
									}
									catch (OperationCanceledException)
									{
										ok = false;
									}
									catch
									{
										ok = false;
									}
									if (ok && string.IsNullOrEmpty(resolution) && detectEngine == "FFMPEG")
									{
										try
										{
											resolution = await GetResolutionWithFallback(ch.Url, token);
										}
										catch
										{
										}
									}
									if (ok && string.IsNullOrEmpty(resolution))
									{
										resolution = "直播";
									}
								}
								else if (ch.Url.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase) || ch.Url.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
								{
									ok = true;
									sw.Stop();
									speed = $"{sw.ElapsedMilliseconds}ms";
									resolution = "直播";
									if (string.IsNullOrEmpty(location))
									{
										location = ExtractLocationFromUrl(ch.Url);
									}
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
									if (!string.IsNullOrEmpty(ipLoc))
									{
										location = ipLoc;
									}
								}
								catch
								{
								}
							}
							if (string.IsNullOrEmpty(location))
							{
								location = ExtractLocationFromUrl(ch.Url);
							}
							ch.Status = (ok ? "可用" : "不可用");
							ch.Speed = speed;
							ch.Resolution = resolution;
							ch.Location = location;
							if (ok)
							{
								Interlocked.Increment(ref availableCount);
							}
							Interlocked.Exchange(ref uiRefreshNeeded, 1);
						}
						catch
						{
						}
						finally
						{
							sem2.Release();
						}
					});
					try
					{
						await Task.WhenAll(fallbackTasks);
					}
					catch (OperationCanceledException)
					{
					}
				}
				finally
				{
					if (sem2 != null)
					{
						((IDisposable)sem2).Dispose();
					}
				}
			}
		}
		finally
		{
			uiRefreshTimer.Stop();
			uiRefreshTimer.Dispose();
			if (btnStopDetect != null)
			{
				btnStopDetect.Enabled = false;
			}
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
			int failedCount = allChannels.Count((ChannelInfo c) => c.Status == "不可用");
			string msg = $"检测完成！\n已检测: {detectedCount}/{totalCount}\n可用: {availableCount}";
			if (failedCount > 0)
			{
				msg += $"\n不可用: {failedCount}（已进行二次复检）";
			}
			DarkMessageBox.Show(msg, "检测完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private void IPTVLiveCheckerMain_Load(object sender, EventArgs e)
	{
		if (persistList)
		{
			LoadChannelList();
		}
		try
		{
			base.Icon = LoadIconFromResources();
		}
		catch
		{
			base.Icon = GenerateAppIcon();
		}
		PerformFirstRunVlcCheck();
		BuildUI();
		ApplyWindowShape();
		Func<Task> initTask = async delegate
		{
			await Task.Delay(300);
		};
		BeginInvoke((Action)delegate
		{
			initTask();
			PromptVlcIfNeeded();
		});
	}

	private void PerformFirstRunVlcCheck()
	{
		if (VlcSetup.IsFirstRunChecked())
		{
			return;
		}
		if (VlcSetup.IsLibVlcReady())
		{
			VlcSetup.TouchInitFlag();
			return;
		}
		if (VlcDetector.IsVlcInstalled())
		{
			try
			{
				VlcSetup.CopyFromInstalledVlc();
			}
			catch
			{
			}
		}
	}

	private void PromptVlcIfNeeded()
	{
		if (_vlcPromptShown)
		{
			return;
		}
		if (totalCount <= 0)
		{
			return;
		}
		if (VlcSetup.IsLibVlcReady())
		{
			return;
		}
		_vlcPromptShown = true;
		if (VlcDetector.IsVlcInstalled())
		{
			bool copied = false;
			try
			{
				copied = VlcSetup.CopyFromInstalledVlc();
			}
			catch
			{
			}
			if (copied)
			{
				DarkMessageBox.Show(this, "已检测到系统安装的 VLC，已自动复制必要文件到 libvlc 文件夹。预览功能现已就绪。", "VLC 检测", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}
		}
		string msg = "检测到尚未安装 VLC 播放器。\n\n本软件的频道预览功能需要 VLC 支持，为了获得完整的功能体验，建议下载安装 VLC。\n\n是否立即下载并自动安装到 libvlc 文件夹？";
		DialogResult dr = DarkMessageBox.Show(this, msg, "VLC 检测", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
		if (dr != DialogResult.Yes)
		{
			return;
		}
		DownloadAndInstallVlcAsync();
	}

	private void QueueVlcCheckIfDataPresent()
	{
		if (_vlcCheckQueued || _vlcPromptShown || totalCount <= 0 || VlcSetup.IsLibVlcReady())
		{
			return;
		}
		_vlcCheckQueued = true;
		BeginInvoke((Action)delegate
		{
			_vlcCheckQueued = false;
			PromptVlcIfNeeded();
		});
	}

	private async void DownloadAndInstallVlcAsync()
	{
		Form progressDlg = CreateDownloadProgressDlg();
		progressDlg.Show(this);
		IProgress<(int, string)> progress = new Progress<(int, string)>(p =>
		{
			UpdateDownloadProgressDlg(progressDlg, p.Item1, p.Item2);
		});
		try
		{
			bool ok = await VlcSetup.DownloadAndInstallAsync(progress);
			progressDlg.Close();
			if (ok)
			{
				DarkMessageBox.Show(this, "VLC 安装完成，预览功能已就绪。", "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			else
			{
				DialogResult r = DarkMessageBox.Show(this, "VLC 自动安装失败。\n\n是否手动打开下载页面？", "安装失败", MessageBoxButtons.YesNo, MessageBoxIcon.Hand);
				if (r == DialogResult.Yes)
				{
					try
					{
						Process.Start(new ProcessStartInfo(VlcDetector.DownloadUrl)
						{
							UseShellExecute = true
						});
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			progressDlg.Close();
			DarkMessageBox.Show(this, "安装过程中出现错误：" + ex.Message, "安装错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private Form CreateDownloadProgressDlg()
	{
		NeonPalette pal = NeonPalette.Create(theme, AnimationSettings.HighContrast);
		Form dlg = new Form
		{
			Text = "正在安装 VLC",
			StartPosition = FormStartPosition.CenterParent,
			FormBorderStyle = FormBorderStyle.None,
			MaximizeBox = false,
			MinimizeBox = false,
			ControlBox = false,
			ShowInTaskbar = false,
			TopMost = true,
			ClientSize = new Size(SX(420), SY(190)),
			BackColor = pal.FormBg,
			ForeColor = pal.GhostText,
			Font = GetFont(SF(9.5f))
		};
		var ctx = NeonChrome.Apply(dlg, pal, "正在安装 VLC", dpiScale);
		Point At(int x, int yy) => new Point(x, yy);
		Label lblStatus = new Label
		{
			Name = "lblStatus",
			Text = "正在准备...",
			Location = At(SX(20), SY(15)),
			Size = new Size(SX(380), SY(22)),
			ForeColor = pal.GhostText,
			BackColor = pal.PanelBg
		};
		ProgressBar pb = new ProgressBar
		{
			Name = "pb",
			Minimum = 0,
			Maximum = 100,
			Value = 0,
			Location = At(SX(20), SY(45)),
			Size = new Size(SX(380), SY(24))
		};
		Label lblHint = new Label
		{
			Text = "请勿关闭窗口，安装完成后将自动继续...",
			Location = At(SX(20), SY(78)),
			Size = new Size(SX(380), SY(20)),
			ForeColor = pal.Muted,
			BackColor = pal.PanelBg,
			Font = GetFont(SF(8f))
		};
		ctx.Body.Controls.Add(lblStatus);
		ctx.Body.Controls.Add(pb);
		ctx.Body.Controls.Add(lblHint);
		return dlg;
	}

	private void UpdateDownloadProgressDlg(Form dlg, int percent, string status)
	{
		if (dlg == null || dlg.IsDisposed)
		{
			return;
		}
		Label lbl = dlg.Controls.Find("lblStatus", true).FirstOrDefault() as Label;
		if (lbl != null)
		{
			lbl.Text = status;
		}
		ProgressBar pb = dlg.Controls.Find("pb", true).FirstOrDefault() as ProgressBar;
		if (pb != null)
		{
			pb.Value = Math.Max(0, Math.Min(100, percent));
		}
	}

	private void ApplyWindowShape()
	{
		if (base.IsDisposed)
		{
			return;
		}
		if (base.IsHandleCreated)
		{
			SetFormDarkModeTitleBar(this, theme != null && DrawingUtils.IsDarkColor(theme.Bg));
			SetWindowRoundedCorners(base.Handle, 2);
		}
		int r = SX(_windowRadius);
		if (base.WindowState == FormWindowState.Maximized)
		{
			base.Region = null;
			if (_borderOverlay != null)
			{
				_borderOverlay.Visible = false;
			}
		}
		else
		{
			if (!base.IsHandleCreated)
			{
				return;
			}
			using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, base.Width, base.Height), r))
			{
				base.Region = new Region(path);
			}
			if (_borderOverlay != null)
			{
				_borderOverlay.Visible = true;
				int bw = Math.Max(1, (int)Math.Round(1.5 * (double)dpiScale));
				GraphicsPath outer = RoundedRectPath(new Rectangle(0, 0, base.Width, base.Height), r);
				GraphicsPath inner = RoundedRectPath(new Rectangle(bw, bw, base.Width - 2 * bw, base.Height - 2 * bw), Math.Max(0, r - bw));
				Region reg = new Region(outer);
				using (Region innerReg = new Region(inner))
				{
					reg.Exclude(innerReg);
				}
				_borderOverlay.Region = reg;
				outer.Dispose();
				inner.Dispose();
				bool dark = DrawingUtils.IsDarkColor(theme.Bg);
			_borderOverlay.BackColor = (dark ? ControlPaint.Light(theme.Bg, 0.38f) : ControlPaint.Dark(theme.Bg, 0.3f));
			_borderOverlay.BringToFront();
			}
		}
	}

	// 可复用的窗口边框：为无边框(FormBorderStyle.None)窗口添加圆角 + 1px 内描边，
	// 主窗体已自带等价逻辑，这里抽出供“直播源生成器”等自定义窗口复用，保持边界感一致。
	private static void ApplyWindowChrome(Form f, int radius = 12)
	{
		if (f == null || f.IsDisposed)
		{
			return;
		}
		Panel overlay = f.Tag as Panel;
		if (overlay == null)
		{
			overlay = new Panel
			{
				Dock = DockStyle.Fill,
				Enabled = false,
				Visible = false
			};
			f.Controls.Add(overlay);
			f.Tag = overlay;
			f.HandleCreated += (s, e) => ShapeWindowChrome(f, radius);
			f.ResizeEnd += (s, e) => ShapeWindowChrome(f, radius);
		}
		ShapeWindowChrome(f, radius);
	}

	private static void ShapeWindowChrome(Form f, int radius)
	{
		if (f == null || f.IsDisposed || !f.IsHandleCreated)
		{
			return;
		}
		Panel overlay = f.Tag as Panel;
		if (overlay == null)
		{
			return;
		}
		try
		{
			SetWindowRoundedCorners(f.Handle, 2);
		}
		catch
		{
		}
		if (f.WindowState == FormWindowState.Maximized)
		{
			f.Region = null;
			overlay.Visible = false;
			return;
		}
		int bw = Math.Max(1, radius / 8);
		using (GraphicsPath outer = CreateRoundedRectPath(new Rectangle(0, 0, f.Width, f.Height), radius))
		{
			f.Region = new Region(outer);
		}
		using (GraphicsPath outer2 = CreateRoundedRectPath(new Rectangle(0, 0, f.Width, f.Height), radius))
		using (GraphicsPath inner = CreateRoundedRectPath(new Rectangle(bw, bw, f.Width - 2 * bw, f.Height - 2 * bw), Math.Max(0, radius - bw)))
		{
			Region reg = new Region(outer2);
			using (Region innerReg = new Region(inner))
			{
				reg.Exclude(innerReg);
			}
			overlay.Region = reg;
		}
		Color bg = f.BackColor;
		bool dark = (bg.R + bg.G + bg.B) / 3 < 128;
		overlay.BackColor = dark ? ControlPaint.Light(bg, 0.5f) : ControlPaint.Dark(bg, 0.3f);
		overlay.BringToFront();
		overlay.Visible = true;
	}

	private void CreateTitleBar()
	{
		titleBarPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(40),
			BackColor = theme.Bg
		};
		typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(titleBarPanel, true, null);
		PictureBox titleIcon = (titleIconRef = new PictureBox
		{
			Size = new Size(SX(22), SY(22)),
			Location = At(SX(12), SY(9)),
			BackColor = theme.Bg,
			SizeMode = PictureBoxSizeMode.CenterImage
		});
		titleIcon.Paint += delegate(object s, PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush = new SolidBrush(theme.Primary);
			using (GraphicsPath path = RoundedRectPath(new Rectangle(SX(2), SY(4), SX(18), SY(13)), SX(3)))
			{
				graphics.FillPath(brush, path);
			}
			using (SolidBrush brush2 = new SolidBrush(theme.Bg))
			{
				graphics.FillRectangle(brush2, new Rectangle(SX(4), SY(6), SX(14), SY(9)));
			}
			graphics.FillRectangle(brush, SX(7), SY(17), SX(7), SY(2));
			graphics.FillRectangle(brush, SX(5), SY(19), SX(12), SY(2));
		};
		titleBarPanel.Controls.Add(titleIcon);
		int btnSize = SY(40);
		Color titleBtnBg = theme.Bg;
		Color titleBtnHover = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(230, 230, 235));
		Color closeBtnHover = Color.FromArgb(232, 17, 35);
		Color closeBtnFg = Color.White;
		int navBtnWidth = SX(75);
		int navBtnHeight = (int)((double)titleBarPanel.Height * 0.6);
		int navBtnY = (titleBarPanel.Height - navBtnHeight) / 2;
		int navBtnGap = 1;
		int navBtnRadius = 4;
		Color navBtnText = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.White : Color.Black);
		navBtnHoverBg = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230));
		fileMenu = new ContextMenuStrip
		{
			Font = GetFont(SF(9f))
		};
		AnimatedMenuRenderer fileMenuRenderer = new AnimatedMenuRenderer(theme);
		fileMenu.Renderer = fileMenuRenderer;
		fileMenuRenderer.Register(fileMenu);
		fileMenu.BackColor = theme.Surface;
		fileMenu.ForeColor = theme.TextPrimary;
		fileMenu.Items.Add(new ToolStripMenuItem("导入 m3u/txt…", null, delegate(object s, EventArgs e)
		{
			BtnSelectFile_Click(s, e);
		}));
		fileMenu.Items.Add(new ToolStripSeparator());
		fileMenu.Items.Add(new ToolStripMenuItem("导出为 m3u…", null, delegate(object s, EventArgs e)
		{
			BtnExport_Click(s, e);
		}));
		fileMenu.Items.Add(new ToolStripSeparator());
		fileMenu.Items.Add(new ToolStripMenuItem("直播源生成器…", null, delegate
		{
			ShowScanSourceDialog();
		}));
		fileMenu.Items.Add(new ToolStripSeparator());
		fileMenu.Items.Add(new ToolStripMenuItem("退出", null, delegate
		{
			Close();
		}));
		btnNavFile = new Button
		{
			Text = "文件",
			Size = new Size(navBtnWidth + 20, navBtnHeight),
			Location = At(SX(42), navBtnY),
			BackColor = Color.Transparent,
			ForeColor = navBtnText,
			Font = GetFont(SF(9f), FontStyle.Regular),
			Cursor = Cursors.Hand,
			Tag = "nav:文件",
			TabStop = false
		};
		AttachNavButtonEvents(btnNavFile);
		btnNavFile.Click += delegate
		{
			fileMenu.Show(btnNavFile, new Point(0, btnNavFile.Height));
		};
		titleBarPanel.Controls.Add(btnNavFile);
		btnNavDetect = new Button
		{
			Text = "解析 (P)",
			Size = new Size(navBtnWidth + 20, navBtnHeight),
			Location = At(SX(42) + (navBtnWidth + 20) + navBtnGap, navBtnY),
			BackColor = Color.Transparent,
			ForeColor = navBtnText,
			Font = GetFont(SF(9f), FontStyle.Regular),
			Cursor = Cursors.Hand,
			Tag = "nav:解析",
			TabStop = false
		};
		AttachNavButtonEvents(btnNavDetect);
		btnNavDetect.Click += delegate
		{
			ShowIptvParserDialog();
		};
		titleBarPanel.Controls.Add(btnNavDetect);
		btnNavSearch = new Button
		{
			Text = "搜索 (F)",
			Size = new Size(navBtnWidth + 20, navBtnHeight),
			Location = At(SX(42) + (navBtnWidth + 20) * 2 + navBtnGap * 2, navBtnY),
			BackColor = Color.Transparent,
			ForeColor = navBtnText,
			Font = GetFont(SF(9f), FontStyle.Regular),
			Cursor = Cursors.Hand,
			Tag = "nav:搜索",
			TabStop = false,
			Visible = showSearchButton
		};
		AttachNavButtonEvents(btnNavSearch);
		btnNavSearch.Click += delegate
		{
			if (watchSearchWindow)
			{
				string s2 = "title=\"IPTV\" || title=\"直播\"";
				string fileName = "https://fofa.info/result?qbase64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s2));
				try
				{
					Process.Start(new ProcessStartInfo(fileName)
					{
						UseShellExecute = true
					});
					return;
				}
				catch
				{
					return;
				}
			}
			ShowSearchEngineDialog();
		};
		titleBarPanel.Controls.Add(btnNavSearch);
		btnNavSettings = new Button
		{
			Text = "设置 (S)",
			Size = new Size(navBtnWidth + 20, navBtnHeight),
			Location = At(SX(42) + (navBtnWidth + 20) * 3 + navBtnGap * 3, navBtnY),
			BackColor = Color.Transparent,
			ForeColor = navBtnText,
			Font = GetFont(SF(9f), FontStyle.Regular),
			Cursor = Cursors.Hand,
			TabStop = false
		};
		AttachNavButtonEvents(btnNavSettings);
		btnNavSettings.Click += delegate
		{
			ShowSettingsDialog();
		};
		titleBarPanel.Controls.Add(btnNavSettings);
		btnNavAbout = new Button
		{
			Text = "关于 (A)",
			Size = new Size(navBtnWidth + 20, navBtnHeight),
			Location = At(SX(42) + (navBtnWidth + 20) * 4 + navBtnGap * 4, navBtnY),
			BackColor = Color.Transparent,
			ForeColor = navBtnText,
			Font = GetFont(SF(9f), FontStyle.Regular),
			Cursor = Cursors.Hand,
			Tag = "nav:关于",
			TabStop = false
		};
		AttachNavButtonEvents(btnNavAbout);
		btnNavAbout.Click += delegate
		{
			ShowAboutDialog();
		};
		titleBarPanel.Controls.Add(btnNavAbout);
		btnThemeToggle = CreateTitleButton();
		btnThemeToggle.Tag = "theme";
		btnThemeToggle.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Color color = (btnThemeToggle.ClientRectangle.Contains(btnThemeToggle.PointToClient(Cursor.Position)) ? theme.TextPrimary : theme.TextSecondary);
			int num = btnThemeToggle.Width;
			int num2 = btnThemeToggle.Height;
			int num3 = num / 2;
			int num4 = num2 / 2;
			using Pen pen = new Pen(color, 1.4f);
			using SolidBrush brush = new SolidBrush(color);
			int w = 8;
			int h = 6;
			e.Graphics.FillPolygon(brush, new[]
			{
				new Point(num3 - w, num4 - h),
				new Point(num3 + w, num4 - h),
				new Point(num3, num4 + h)
			});
			e.Graphics.DrawPolygon(pen, new[]
			{
				new Point(num3 - w, num4 - h),
				new Point(num3 + w, num4 - h),
				new Point(num3, num4 + h)
			});
		};
		ContextMenuStrip themeMenu = new ContextMenuStrip();
		themeMenuStrip = themeMenu;
		AnimatedMenuRenderer themeMenuRenderer = new AnimatedMenuRenderer(theme);
		themeMenu.Renderer = themeMenuRenderer;
		themeMenuRenderer.Register(themeMenu);
		themeMenu.BackColor = theme.Surface;
		themeMenu.ForeColor = theme.TextPrimary;
		// 数据驱动：从内置主题注册表自动生成菜单项（新增内置主题只需加到 Builtins 数组）
		foreach (AppTheme b in AppTheme.Builtins)
		{
			string themeName = b.Name;
			themeMenu.Items.Add(themeName).Click += delegate
			{
				themePreference = themeName;
				SetTheme(b);
			};
		}
		if (!AppTheme.IsExternalThemesLoaded())
		{
			AppTheme.LoadExternalThemes();
		}
		if (AppTheme.ExternalThemes.Count > 0)
		{
			themeMenu.Items.Add(new ToolStripSeparator());
			foreach (AppTheme ex in AppTheme.ExternalThemes.Values)
			{
				string exName = ex.Name;
				themeMenu.Items.Add(exName).Click += delegate
				{
					themePreference = exName;
					SetTheme(ex);
				};
			}
		}
		btnThemeToggle.Click += delegate
		{
			Point pt = btnThemeToggle.PointToScreen(new Point(0, btnThemeToggle.Height));
			themeMenu.Show(pt);
		};
		titleBarPanel.Controls.Add(btnThemeToggle);
		btnMin = CreateTitleButton();
		btnMin.Paint += delegate(object s, PaintEventArgs e)
		{
			Color color = (btnMin.ClientRectangle.Contains(btnMin.PointToClient(Cursor.Position)) ? theme.TextPrimary : theme.TextSecondary);
			int num = btnMin.Width;
			int num2 = btnMin.Height;
			int num3 = num / 2;
			int num4 = num2 / 2;
			using Pen pen = new Pen(color, 1.5f);
			e.Graphics.DrawLine(pen, num3 - 8, num4, num3 + 8, num4);
		};
		btnMin.Click += delegate
		{
			base.WindowState = FormWindowState.Minimized;
		};
		titleBarPanel.Controls.Add(btnMin);
		btnMax = CreateTitleButton();
		btnMax.Paint += delegate(object s, PaintEventArgs e)
		{
			Color color = (btnMax.ClientRectangle.Contains(btnMax.PointToClient(Cursor.Position)) ? theme.TextPrimary : theme.TextSecondary);
			bool flag = base.WindowState == FormWindowState.Maximized;
			int num = btnMax.Width;
			int num2 = btnMax.Height;
			int num3 = num / 2;
			int num4 = num2 / 2;
			using Pen pen = new Pen(color, 1.5f);
			if (flag)
			{
				e.Graphics.DrawRectangle(pen, num3 - 7, num4 - 5, 9, 9);
				e.Graphics.DrawRectangle(pen, num3 - 4, num4 - 8, 9, 9);
			}
			else
			{
				e.Graphics.DrawRectangle(pen, num3 - 7, num4 - 7, 14, 14);
			}
		};
		btnMax.Click += delegate
		{
			if (base.WindowState == FormWindowState.Maximized)
			{
				base.WindowState = FormWindowState.Normal;
			}
			else
			{
				base.WindowState = FormWindowState.Maximized;
			}
			btnMax.Invalidate();
		};
		titleBarPanel.Controls.Add(btnMax);
		btnClose = CreateTitleButton();
		btnClose.FlatAppearance.MouseOverBackColor = closeBtnHover;
		btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
		btnClose.Paint += delegate(object s, PaintEventArgs e)
		{
			bool flag = btnClose.ClientRectangle.Contains(btnClose.PointToClient(Cursor.Position));
			bool flag2 = Control.MouseButtons == MouseButtons.Left && flag;
			int num = btnClose.Width;
			int num2 = btnClose.Height;
			int num3 = num / 2;
			int num4 = num2 / 2;
			Color color = (flag2 ? Color.FromArgb(200, 15, 30) : (flag ? closeBtnHover : btnClose.BackColor));
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, num, num2), 8))
			{
				using SolidBrush brush = new SolidBrush(color);
				e.Graphics.FillPath(brush, path);
			}
			Color color2 = (flag ? closeBtnFg : theme.TextSecondary);
			int num5 = (flag2 ? 1 : 0);
			using Pen pen = new Pen(color2, 1.6f);
			e.Graphics.DrawLine(pen, num3 - 7 + num5, num4 - 7 + num5, num3 + 7 + num5, num4 + 7 + num5);
			e.Graphics.DrawLine(pen, num3 + 7 + num5, num4 - 7 + num5, num3 - 7 + num5, num4 + 7 + num5);
		};
		btnClose.MouseEnter += delegate
		{
			btnClose.Invalidate();
		};
		btnClose.MouseLeave += delegate
		{
			btnClose.Invalidate();
		};
		btnClose.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				btnClose.Invalidate();
			}
		};
		btnClose.MouseUp += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				btnClose.Invalidate();
			}
		};
		btnClose.Click += delegate
		{
			Close();
		};
		titleBarPanel.Controls.Add(btnClose);
		titleBarPanel.Resize += delegate
		{
			UpdateTitleAndNav();
		};
		titleBarPanel.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(base.Handle, 161, 2, 0);
			}
		};
		titleIcon.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(base.Handle, 161, 2, 0);
			}
		};
		titleBarPanel.DoubleClick += delegate
		{
			if (base.WindowState == FormWindowState.Maximized)
			{
				base.WindowState = FormWindowState.Normal;
			}
			else
			{
				base.WindowState = FormWindowState.Maximized;
			}
			btnMax.Text = ((base.WindowState == FormWindowState.Maximized) ? "❐" : "☐");
		};
		void AttachNavButtonEvents(Button btn)
		{
			btn.FlatStyle = FlatStyle.Flat;
			btn.FlatAppearance.BorderSize = 0;
			btn.FlatAppearance.MouseOverBackColor = Color.Empty;
			btn.FlatAppearance.MouseDownBackColor = Color.Empty;
			btn.Region?.Dispose();
			using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), navBtnRadius))
			{
				btn.Region = new Region(path);
			}
			btn.Paint += PaintNavButton;
			btn.MouseEnter += delegate
			{
				btn.Invalidate();
			};
			btn.MouseLeave += delegate
			{
				btn.Invalidate();
			};
			btn.Resize += delegate
			{
				btn.Region?.Dispose();
				using (GraphicsPath path2 = RoundedRectPath(new Rectangle(0, 0, btn.Width, btn.Height), navBtnRadius))
				{
					btn.Region = new Region(path2);
				}
				btn.Invalidate();
			};
		}
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
			using (GraphicsPath path = RoundedRectPath(new Rectangle(0, 0, btnSize, btnSize), 8))
			{
				b.Region = new Region(path);
			}
			b.Resize += delegate(object s, EventArgs e)
			{
				Button button = (Button)s;
				button.Region?.Dispose();
				using GraphicsPath path2 = RoundedRectPath(new Rectangle(0, 0, button.Width, button.Height), 8);
				button.Region = new Region(path2);
			};
			return b;
		}
		void PaintNavButton(object sender, PaintEventArgs e)
		{
			Button btn = (Button)sender;
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			// 用不透明实色填充按钮背景，避免标题栏（半透明）背后的动态极光透显到按钮上造成"遮挡组件"。
			Color navSolidBg = Color.FromArgb(255, theme.Bg.R, theme.Bg.G, theme.Bg.B);
			using (SolidBrush clearBrush = new SolidBrush(navSolidBg))
			{
				g.FillRectangle(clearBrush, 0, 0, btn.Width, btn.Height);
			}
			if (btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)))
			{
				Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
				using GraphicsPath path = RoundedRectPath(rect, navBtnRadius);
				using SolidBrush bgBrush = new SolidBrush(navBtnHoverBg);
				g.FillPath(bgBrush, path);
			}
			Rectangle textRect = new Rectangle(8, 0, btn.Width - 16, btn.Height);
			using SolidBrush textBrush = new SolidBrush(btn.ForeColor);
			using StringFormat sf = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			g.DrawString(btn.Text, btn.Font, textBrush, textRect, sf);
		}
		void UpdateTitleAndNav()
		{
			RefreshNavButtonSizes();
			int num = titleBarPanel.ClientSize.Width;
			int btnGap = navBtnGap;
			if (btnNavDetect != null && btnNavSearch != null && btnNavSearch.Visible)
			{
				btnGap = btnNavSearch.Left - btnNavDetect.Right;
			}
			int currentX = SX(42);
			if (btnNavFile != null)
			{
				btnNavFile.Left = currentX;
				currentX = btnNavFile.Right + btnGap;
			}
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
			int startX = num - totalBtnsWidth;
			btnThemeToggle.Left = startX;
			btnMin.Left = startX + btnSize;
			btnMax.Left = startX + btnSize * 2;
			btnClose.Left = startX + btnSize * 3;
		}
	}

	private void CreateBottomBar()
	{
		bottomBarRef = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 32,
			BackColor = theme.Bg
		};
		bottomBarRef.MouseDown += delegate(object s, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(base.Handle, 161, 2, 0);
			}
		};
		bottomBarRef.DoubleClick += delegate
		{
			if (base.WindowState == FormWindowState.Maximized)
			{
				base.WindowState = FormWindowState.Normal;
			}
			else
			{
				base.WindowState = FormWindowState.Maximized;
			}
		};
	}

	private void g_drawSunIcon(Graphics g, Rectangle rect, Pen pen, Color fillColor)
	{
		int cx = rect.X + rect.Width / 2;
		int cy = rect.Y + rect.Height / 2;
		int r = rect.Width / 2 - 2;
		using (SolidBrush br = new SolidBrush(fillColor))
		{
			g.FillEllipse(br, cx - r + 1, cy - r + 1, r * 2 - 2, r * 2 - 2);
		}
		g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
		for (int i = 0; i < 8; i++)
		{
			double angle = (double)i * Math.PI / 4.0;
			int x1 = cx + (int)((double)(r + 2) * Math.Cos(angle));
			int y1 = cy + (int)((double)(r + 2) * Math.Sin(angle));
			int x2 = cx + (int)((double)(r + 4) * Math.Cos(angle));
			int y2 = cy + (int)((double)(r + 4) * Math.Sin(angle));
			g.DrawLine(pen, x1, y1, x2, y2);
		}
	}

	private void g_drawMoonIcon(Graphics g, Rectangle rect, Pen pen, Color fillColor)
	{
		int cx = rect.X + rect.Width / 2;
		int cy = rect.Y + rect.Height / 2;
		int r = rect.Width / 2 - 1;
		using SolidBrush br = new SolidBrush(fillColor);
		g.FillEllipse(br, cx - r, cy - r, r * 2, r * 2);
		using SolidBrush bgBr = new SolidBrush((titleBarPanel != null) ? titleBarPanel.BackColor : Color.White);
		g.FillEllipse(bgBr, cx - r + 4, cy - r - 1, r * 2, r * 2);
	}

	private byte[] MakeSimpleIcon(byte[] pngData, int w, int h)
	{
		using MemoryStream ms = new MemoryStream();
		using BinaryWriter bw = new BinaryWriter(ms);
		bw.Write((ushort)0);
		bw.Write((ushort)1);
		bw.Write((ushort)1);
		bw.Write((byte)((w < 256) ? ((uint)w) : 0u));
		bw.Write((byte)((h < 256) ? ((uint)h) : 0u));
		bw.Write((byte)0);
		bw.Write((byte)0);
		bw.Write((ushort)1);
		bw.Write((ushort)32);
		bw.Write((uint)pngData.Length);
		bw.Write(22u);
		bw.Write(pngData);
		return ms.ToArray();
	}

	private void SaveMultiSizeIcon(string path)
	{
		int[] sizes = new int[7] { 16, 32, 48, 64, 128, 256, 512 };
		List<Bitmap> bitmaps = new List<Bitmap>();
		try
		{
			using (Bitmap master = GenerateAppIconBitmap(512))
			{
				int[] array = sizes;
				foreach (int s in array)
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
			foreach (Bitmap item in bitmaps)
			{
				item.Dispose();
			}
		}
	}

	private Icon GenerateAppIcon(int size = 512)
	{
		using Bitmap bmp = GenerateAppIconBitmap(size);
		IntPtr hicon = bmp.GetHicon();
		Icon obj = Icon.FromHandle(hicon);
		Icon result = (Icon)obj.Clone();
		obj.Dispose();
		DestroyIcon(hicon);
		return result;
	}

	private Icon LoadIconFromResources()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string iconResourceName = assembly.GetManifestResourceNames().FirstOrDefault((string r) => r.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrEmpty(iconResourceName))
		{
			using Stream stream = assembly.GetManifestResourceStream(iconResourceName);
			if (stream != null)
			{
				return new Icon(stream);
			}
		}
		return GenerateAppIcon();
	}

	private Bitmap LoadWechatPromoImage(int maxWidth)
	{
		try
		{
			using MemoryStream ms = new MemoryStream(Convert.FromBase64String(WechatPromoResource.Base64Data));
			Bitmap original = new Bitmap(ms);
			int targetW = ((original.Width <= maxWidth) ? original.Width : maxWidth);
			int targetH = (int)((double)original.Height * (double)targetW / (double)original.Width);
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
		catch
		{
		}
		return null;
	}

	private Bitmap GenerateAppIconBitmap(int size)
	{
		Bitmap bmp = new Bitmap(size, size);
		using Graphics g = Graphics.FromImage(bmp);
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.TextRenderingHint = TextRenderingHint.AntiAlias;
		g.Clear(Color.Transparent);
		float scale = (float)size / 512f;
		int margin = (int)(30f * scale);
		int cardSize = size - margin * 2;
		int cornerR = (int)(90f * scale);
		int innerR = (int)(82f * scale);
		using (GraphicsPath cardPath = GetRoundedPath(new Rectangle(margin, margin, cardSize, cardSize), cornerR))
		{
			using (LinearGradientBrush cardBrush = new LinearGradientBrush(new Rectangle(margin, margin, cardSize, cardSize), Color.FromArgb(155, 105, 220), Color.FromArgb(115, 65, 185), LinearGradientMode.ForwardDiagonal))
			{
				g.FillPath(cardBrush, cardPath);
			}
			using GraphicsPath innerGlow = GetRoundedPath(new Rectangle(margin + (int)(8f * scale), margin + (int)(8f * scale), cardSize - (int)(16f * scale), cardSize - (int)(16f * scale)), innerR);
			using LinearGradientBrush glow = new LinearGradientBrush(new Rectangle(margin, margin, cardSize, cardSize / 2), Color.FromArgb(60, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), LinearGradientMode.Vertical);
			g.FillPath(glow, innerGlow);
		}
		int tvW = (int)(310f * scale);
		int tvH = (int)(230f * scale);
		int tvX = (size - tvW) / 2;
		int tvY = (int)(120f * scale);
		int tvR = (int)(28f * scale);
		using (GraphicsPath tvBody = GetRoundedPath(new Rectangle(tvX, tvY, tvW, tvH), tvR))
		{
			using (SolidBrush tvBrush = new SolidBrush(Color.FromArgb(255, 250, 252)))
			{
				g.FillPath(tvBrush, tvBody);
			}
			using Pen tvPen = new Pen(Color.FromArgb(80, 50, 130), Math.Max(2f, 3f * scale));
			g.DrawPath(tvPen, tvBody);
		}
		int screenPad = (int)(22f * scale);
		Rectangle screenRect = new Rectangle(tvX + screenPad, tvY + screenPad, tvW - screenPad * 2, tvH - screenPad * 2 - (int)(20f * scale));
		int screenR = (int)(14f * scale);
		using (GraphicsPath screenPath = GetRoundedPath(screenRect, screenR))
		{
			using LinearGradientBrush screenGrad = new LinearGradientBrush(screenRect, Color.FromArgb(55, 30, 95), Color.FromArgb(85, 45, 140), LinearGradientMode.ForwardDiagonal);
			g.FillPath(screenGrad, screenPath);
		}
		int waveCenterX = screenRect.Left + screenRect.Width / 2;
		int waveCenterY = screenRect.Top + screenRect.Height / 2;
		float waveW1 = 3f * scale;
		float waveW2 = 2.5f * scale;
		float waveW3 = 2f * scale;
		using (Pen wavePen1 = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1.5f, waveW1)))
		{
			using Pen wavePen2 = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1.2f, waveW2));
			using Pen wavePen3 = new Pen(Color.FromArgb(70, 255, 255, 255), Math.Max(1f, waveW3));
			int arc1 = (int)(80f * scale);
			int arc2 = (int)(60f * scale);
			int arc3 = (int)(40f * scale);
			int ah1 = (int)(35f * scale);
			int ah2 = (int)(26f * scale);
			int ah3 = (int)(17f * scale);
			g.DrawArc(wavePen3, waveCenterX - arc1 * 2, waveCenterY - ah1, arc1 * 4, ah1 * 2, 200, 140);
			g.DrawArc(wavePen2, waveCenterX - arc2 * 2, waveCenterY - ah2, arc2 * 4, ah2 * 2, 200, 140);
			g.DrawArc(wavePen1, waveCenterX - arc3 * 2, waveCenterY - ah3, arc3 * 4, ah3 * 2, 200, 140);
		}
		int playOffX = (int)(-12f * scale);
		int playOffY = (int)(4f * scale);
		int pH = (int)(52f * scale);
		Point[] playTriangle = new Point[3]
		{
			new Point(waveCenterX + playOffX, waveCenterY - pH / 2 + playOffY),
			new Point(waveCenterX + playOffX, waveCenterY + pH / 2 + playOffY),
			new Point(waveCenterX + pH / 2 + (int)(8f * scale), waveCenterY + playOffY)
		};
		using (SolidBrush playBrush = new SolidBrush(Color.FromArgb(245, 245, 250)))
		{
			g.FillPolygon(playBrush, playTriangle);
		}
		int standW = (int)(70f * scale);
		int standH = (int)(20f * scale);
		int num = (size - standW) / 2;
		int standY = tvY + tvH - (int)(5f * scale);
		using (GraphicsPath standPath = GetRoundedPath(new Rectangle(num, standY, standW, standH), (int)(6f * scale)))
		{
			using SolidBrush standBrush = new SolidBrush(Color.FromArgb(235, 230, 245));
			g.FillPath(standBrush, standPath);
		}
		int baseW = (int)(160f * scale);
		int baseH = (int)(16f * scale);
		int num2 = (size - baseW) / 2;
		int baseY = standY + standH - (int)(4f * scale);
		using (GraphicsPath basePath = GetRoundedPath(new Rectangle(num2, baseY, baseW, baseH), (int)(8f * scale)))
		{
			using SolidBrush baseBrush = new SolidBrush(Color.FromArgb(220, 215, 235));
			g.FillPath(baseBrush, basePath);
		}
		float wtvFontSize = Math.Max(10f, 38f * scale);
		float proFontSize = Math.Max(6f, 14f * scale);
		using (Font wtvFont = new Font("Arial Black", wtvFontSize, FontStyle.Bold))
		{
			string wtv = "WTV";
			SizeF wtvSize = g.MeasureString(wtv, wtvFont);
			using SolidBrush wtvBrush = new SolidBrush(Color.White);
			g.DrawString(wtv, wtvFont, wtvBrush, ((float)size - wtvSize.Width) / 2f, tvY + tvH + (int)(22f * scale));
		}
		using Font proFont = new Font("Arial", proFontSize, FontStyle.Bold);
		string proLabel = "工具箱 PRO";
		if (size < 64)
		{
			proLabel = "PRO";
		}
		else if (size < 128)
		{
			proLabel = "工具箱";
		}
		SizeF proSize = g.MeasureString(proLabel, proFont);
		using SolidBrush proBrush = new SolidBrush(Color.FromArgb(220, 210, 240));
		g.DrawString(proLabel, proFont, proBrush, ((float)size - proSize.Width) / 2f, tvY + tvH + (int)(72f * scale));
		return bmp;
	}

	private void SaveIcon(string path, List<Bitmap> bitmaps)
	{
		using FileStream fs = new FileStream(path, FileMode.Create);
		using BinaryWriter bw = new BinaryWriter(fs);
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
				bmp.Save(ms, ImageFormat.Png);
				pngData = ms.ToArray();
			}
			pngDatas.Add(pngData);
			bw.Write((byte)((bmp.Width < 256) ? ((uint)bmp.Width) : 0u));
			bw.Write((byte)((bmp.Height < 256) ? ((uint)bmp.Height) : 0u));
			bw.Write((byte)0);
			bw.Write((byte)0);
			bw.Write((ushort)1);
			bw.Write((ushort)32);
			bw.Write((uint)pngData.Length);
			bw.Write((uint)offset);
			offset += pngData.Length;
		}
		foreach (byte[] data in pngDatas)
		{
			bw.Write(data);
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyIcon(IntPtr hIcon);

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool EnumWindows(WndEnumProc lpEnumFunc, IntPtr lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


	private void ResetStreamInfoFields()
	{
		_currentCodec = "";
		_currentResolution = "";
		_currentFps = "";
		_currentBitrate = "";
		_currentAudioChannels = "";
		_currentAudioSampleRate = "";
		_currentDelay = "";
		_currentFrameCount = "";
		_currentTime = "";
		_currentSpeed = "";
		_currentBuffer = "";
		_currentSar = "";
		_currentDar = "";
		_currentAudioBitdepth = "";
		_currentSize = "";
		_currentPixFmt = "";
		_currentLevel = "";
		_currentColorSpace = "";
		_currentColorRange = "";
		_currentColorPrimaries = "";
		_currentColorTransfer = "";
		_currentFormat = "";
		_currentDuration = "";
		_droppedFrames = 0;
		_totalFrames = 0;
		_currentDecodedFrames = "";
		_currentDisplayedFrames = "";
		_lastStreamTimeMs = 0L;
	}

	private string SimplifyCodecDisplay(string codec)
	{
		if (string.IsNullOrWhiteSpace(codec))
		{
			return codec;
		}
		string[] array = codec.Split(new string[1] { " + " }, StringSplitOptions.RemoveEmptyEntries);
		List<string> simplified = new List<string>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string p = array2[i].Trim();
			string profile = "";
			int openIdx = p.IndexOf('(');
			if (openIdx >= 0)
			{
				int closeIdx = p.IndexOf(')', openIdx);
				if (closeIdx > openIdx)
				{
					profile = p.Substring(openIdx + 1, closeIdx - openIdx - 1).Trim();
					p = (p.Substring(0, openIdx) + p.Substring(closeIdx + 1)).Trim();
				}
			}
			string simple = DetectCodecShortName(p);
			if (!string.IsNullOrEmpty(profile) && !IsCommonProfile(profile))
			{
				string simpleProfile = DetectCodecShortName(profile);
				if (!string.IsNullOrEmpty(simpleProfile) && !simpleProfile.Equals(simple, StringComparison.OrdinalIgnoreCase) && !simpleProfile.Equals(profile, StringComparison.OrdinalIgnoreCase))
				{
					simple = simple + "(" + simpleProfile + ")";
				}
			}
			if (!string.IsNullOrEmpty(simple))
			{
				simplified.Add(simple);
			}
		}
		if (simplified.Count <= 0)
		{
			return codec;
		}
		return string.Join(" + ", simplified);
	}

	private string DetectCodecShortName(string name)
	{
		string lower = name.ToLowerInvariant();
		if (lower.Contains("h.264") || lower.Contains("avc") || lower.Contains("mpeg-4 avc"))
		{
			return "H.264";
		}
		if (lower.Contains("h.265") || lower.Contains("hevc"))
		{
			return "HEVC";
		}
		if (lower.Contains("av1"))
		{
			return "AV1";
		}
		if (lower.Contains("vp9"))
		{
			return "VP9";
		}
		if (lower.Contains("vp8"))
		{
			return "VP8";
		}
		if (lower.Contains("mpeg-4 part 2") || lower.Contains("mpeg-4 visual"))
		{
			return "MPEG-4";
		}
		if ((lower.Contains("mpeg-1") || lower.Contains("mpeg-2")) && lower.Contains("video"))
		{
			return "MPEG-2";
		}
		if (lower.Contains("aac"))
		{
			return "AAC";
		}
		if (lower.Contains("mp3") || lower.Contains("mpeg audio layer 3"))
		{
			return "MP3";
		}
		if (lower.Contains("mp2") || lower.Contains("mpeg audio layer 2"))
		{
			return "MP2";
		}
		if (lower.Contains("e-ac-3") || lower.Contains("eac3"))
		{
			return "EAC3";
		}
		if (lower.Contains("ac-3") || lower.Contains("ac3"))
		{
			return "AC3";
		}
		if (lower.Contains("dts"))
		{
			return "DTS";
		}
		if (lower.Contains("flac"))
		{
			return "FLAC";
		}
		if (lower.Contains("opus"))
		{
			return "Opus";
		}
		if (lower.Contains("vorbis"))
		{
			return "Vorbis";
		}
		if (lower.Contains("pcm"))
		{
			return "PCM";
		}
		return name;
	}

	private bool IsCommonProfile(string profile)
	{
		if (!profile.Equals("Main", StringComparison.OrdinalIgnoreCase) && !profile.Equals("Baseline", StringComparison.OrdinalIgnoreCase) && !profile.Equals("High", StringComparison.OrdinalIgnoreCase) && !profile.Equals("LC", StringComparison.OrdinalIgnoreCase))
		{
			return profile.Equals("Main 10", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private string ExtractLocationFallback(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return "";
		}
		try
		{
			return new Uri(url).Host;
		}
		catch
		{
			return "";
		}
	}

	private string FormatBytes(long bytes)
	{
		if (bytes < 1024)
		{
			return $"{bytes} B";
		}
		if (bytes < 1048576)
		{
			return $"{(double)bytes / 1024.0:F1} KB";
		}
		if (bytes < 1073741824)
		{
			return $"{(double)bytes / 1048576.0:F1} MB";
		}
		return $"{(double)bytes / 1073741824.0:F2} GB";
	}

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
				Location = At(0, 0)
			};
			_streamInfoOverlayForm.Controls.Add(_streamInfoLabel);
			_streamInfoOverlayForm.Show();
			_streamInfoOverlayForm.KeyPreview = true;
			_streamInfoOverlayForm.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					_showStreamInfoOverlay = false;
					StopStreamInfoOverlay();
				}
			};
			_streamInfoOverlayTimer = new System.Windows.Forms.Timer
			{
				Interval = 100
			};
			_streamInfoOverlayTimer.Tick += delegate
			{
				UpdateStreamInfoOverlay();
			};
			_streamInfoOverlayTimer.Start();
		}
		catch
		{
		}
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
		catch
		{
		}
		try
		{
			if (_streamInfoOverlayForm != null && !_streamInfoOverlayForm.IsDisposed)
			{
				_streamInfoOverlayForm.Close();
				_streamInfoOverlayForm.Dispose();
				_streamInfoOverlayForm = null;
			}
		}
		catch
		{
		}
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
			if (!GetWindowRect(targetHwnd, out var rect))
			{
				_streamInfoOverlayForm.Visible = false;
				return;
			}
			StringBuilder info = new StringBuilder();
			if (!string.IsNullOrEmpty(_currentChannelName))
			{
				info.Append("名称: " + _currentChannelName + "\n");
			}
			if (!string.IsNullOrEmpty(_currentFormat))
			{
				info.Append("格式: " + _currentFormat + "\n");
			}
			if (!string.IsNullOrEmpty(_currentCodec))
			{
				info.Append("编码: " + _currentCodec + "\n");
			}
			if (!string.IsNullOrEmpty(_currentResolution))
			{
				info.Append("分辨率: " + _currentResolution + "\n");
			}
			if (!string.IsNullOrEmpty(_currentSar))
			{
				info.Append("SAR: " + _currentSar + "\n");
			}
			if (!string.IsNullOrEmpty(_currentDar))
			{
				info.Append("DAR: " + _currentDar + "\n");
			}
			if (!string.IsNullOrEmpty(_currentFps))
			{
				info.Append("帧率: " + _currentFps + "\n");
			}
			if (!string.IsNullOrEmpty(_currentPixFmt))
			{
				info.Append("像素格式: " + _currentPixFmt + "\n");
			}
			if (!string.IsNullOrEmpty(_currentLevel))
			{
				info.Append("级别: " + _currentLevel + "\n");
			}
			if (!string.IsNullOrEmpty(_currentColorSpace))
			{
				info.Append("色彩空间: " + _currentColorSpace + "\n");
			}
			if (!string.IsNullOrEmpty(_currentColorPrimaries))
			{
				info.Append("色基: " + _currentColorPrimaries + "\n");
			}
			if (!string.IsNullOrEmpty(_currentColorTransfer))
			{
				info.Append("传递函数: " + _currentColorTransfer + "\n");
			}
			if (!string.IsNullOrEmpty(_currentAudioChannels))
			{
				info.Append("声道: " + _currentAudioChannels + "\n");
			}
			if (!string.IsNullOrEmpty(_currentAudioSampleRate))
			{
				info.Append("采样率: " + _currentAudioSampleRate + "\n");
			}
			if (!string.IsNullOrEmpty(_currentAudioBitdepth))
			{
				info.Append("位深: " + _currentAudioBitdepth + "\n");
			}
			if (!string.IsNullOrEmpty(_currentBitrate))
			{
				info.Append("码率: " + _currentBitrate + "\n");
			}
			if (!string.IsNullOrEmpty(_currentDuration))
			{
				info.Append("时长: " + _currentDuration + "\n");
			}
			if (!string.IsNullOrEmpty(_currentDelay))
			{
				info.Append("延时: " + _currentDelay + "\n");
			}
			if (!string.IsNullOrEmpty(_currentTime))
			{
				info.Append("时间: " + _currentTime + "\n");
			}
			if (!string.IsNullOrEmpty(_currentSpeed))
			{
				info.Append("速度: " + _currentSpeed + "\n");
			}
			if (!string.IsNullOrEmpty(_currentFrameCount))
			{
				info.Append("帧计数: " + _currentFrameCount + "\n");
			}
			if (!string.IsNullOrEmpty(_currentDecodedFrames))
			{
				info.Append(_currentDecodedFrames + "\n");
			}
			if (!string.IsNullOrEmpty(_currentDisplayedFrames))
			{
				info.Append(_currentDisplayedFrames + "\n");
			}
			if (!string.IsNullOrEmpty(_currentBuffer))
			{
				info.Append("缓冲: " + _currentBuffer + "\n");
			}
			if (_droppedFrames > 0)
			{
				info.Append($"丢帧: {_droppedFrames}/{_totalFrames}");
			}
			_streamInfoLabel.Text = info.ToString();
			_streamInfoLabel.Location = At(0, 0);
			_streamInfoOverlayForm.Location = At(rect.Left + 26, rect.Top + 60);
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
			EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
			{
				if (!IsWindowVisible(hWnd))
				{
					return true;
				}
				GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
				if (lpdwProcessId != targetPid)
				{
					return true;
				}
				StringBuilder stringBuilder = new StringBuilder(256);
				GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
				string text = stringBuilder.ToString().ToLower();
				StringBuilder stringBuilder2 = new StringBuilder(512);
				GetWindowText(hWnd, stringBuilder2, stringBuilder2.Capacity);
				string text2 = stringBuilder2.ToString().ToLower();
				if (text.Contains("sdl") || text.Contains("ffplay") || text.Contains("potplayer") || text.Contains("vlc") || text.Contains("mpv") || text.Contains("wxwidgets") || text2.Contains("potplayer") || text2.Contains("vlc") || text2.Contains("mpv"))
				{
					targetHwnd = hWnd;
					return false;
				}
				return true;
			}, IntPtr.Zero);
		}
		else if (previewProcess != null && !previewProcess.HasExited)
		{
			uint targetPid2 = (uint)previewProcess.Id;
			EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
			{
				if (!IsWindowVisible(hWnd))
				{
					return true;
				}
				GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
				if (lpdwProcessId != targetPid2)
				{
					return true;
				}
				StringBuilder stringBuilder = new StringBuilder(256);
				GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
				string text = stringBuilder.ToString().ToLower();
				StringBuilder stringBuilder2 = new StringBuilder(512);
				GetWindowText(hWnd, stringBuilder2, stringBuilder2.Capacity);
				string text2 = stringBuilder2.ToString().ToLower();
				if (text.Contains("sdl") || text.Contains("ffplay") || text.Contains("potplayer") || text.Contains("vlc") || text.Contains("mpv") || text.Contains("wxwidgets") || text2.Contains("potplayer") || text2.Contains("vlc") || text2.Contains("mpv"))
				{
					targetHwnd = hWnd;
					return false;
				}
				return true;
			}, IntPtr.Zero);
		}
		return targetHwnd;
	}

	private void LoadSelectedChannelToPreview()
	{
		if (_isImporting)
		{
			return;
		}
		if (channelPlayer == null || previewPanel == null || !previewPanel.Visible || dgvData.SelectedRows.Count == 0)
		{
			return;
		}
		ResetStreamInfoFields();
		DataGridViewRow row = dgvData.SelectedRows[0];
		object urlCell = row.Cells["colUrl"].Value;
		if (urlCell == null)
		{
			return;
		}
		string url = urlCell.ToString();
		string name = row.Cells["colName"].Value?.ToString() ?? "";
		// 仅更新流媒体信息栏，不自动播放；双击行触发播放
		_detailUrlText = url;
		_detailGroupText = row.Cells["colGroup"].Value?.ToString() ?? "未分组";
		string dispUrl = ((url.Length > 46) ? (url.Substring(0, 46) + "…") : url);
		if (_detailName != null)
		{
			_detailName.Text = "名称: " + name;
		}
		if (_detailUrl != null)
		{
			_detailUrl.Text = "地址: " + dispUrl;
		}
		if (_detailResolution != null)
		{
			_detailResolution.Text = "分辨率: " + (row.Cells["colResolution"].Value?.ToString() ?? "—");
		}
		if (_detailGroup != null)
		{
			_detailGroup.Text = "分组: " + _detailGroupText;
		}
		if (_detailStatus != null)
		{
			_detailStatus.Text = "状态: " + (row.Cells["colStatus"].Value?.ToString() ?? "—");
		}
		if (_detailBitrate != null)
		{
			_detailBitrate.Text = "码率: —";
		}
		_previewChannelName = name;
		_previewChannelLocation = row.Cells["colLocation"].Value?.ToString() ?? "";
		// 左侧信息栏无归属地信息时，自动查询归属地并更新到流媒体信息栏
		if (string.IsNullOrWhiteSpace(_previewChannelLocation))
		{
			string queryUrl = url;
			Task.Run(async delegate
			{
				try
				{
					string host = "";
					try { host = new Uri(queryUrl).Host; } catch { }
					if (string.IsNullOrEmpty(host))
					{
						return;
					}
					string ipHost = "";
					string domainHost = host;
					if (System.Net.IPAddress.TryParse(host, out var tip) && tip.GetAddressBytes().Length == 4)
					{
						ipHost = host;
						domainHost = "";
					}
					string loc = "";
					if (!string.IsNullOrEmpty(ipHost))
					{
						loc = await QueryIpLocationAsync(ipHost, CancellationToken.None);
					}
					else if (!string.IsNullOrEmpty(domainHost))
					{
						loc = await QueryDomainLocationAsync(domainHost, CancellationToken.None);
					}
					if (!string.IsNullOrWhiteSpace(loc))
					{
						// 确保仍是当前选中行，避免快速切换时显示旧数据
						if (!base.IsDisposed && IsHandleCreated)
						{
							BeginInvoke((Action)delegate
							{
								if (base.IsDisposed) return;
								DataGridViewRow curRow = (dgvData.SelectedRows.Count > 0) ? dgvData.SelectedRows[0] : null;
								if (curRow != null && curRow.Cells["colUrl"].Value?.ToString() == queryUrl)
								{
									// 仅更新流媒体信息栏，不回填左侧 colLocation 单元格
									if (_previewChannelLocation == "" || _previewChannelLocation == ExtractLocationFallback(queryUrl))
									{
										_previewChannelLocation = loc;
									}
								}
							});
						}
					}
				}
				catch
				{
				}
			});
		}
		string probeUrl = url;
		Task.Run(delegate
		{
			try
			{
				GetFullStreamInfoWithFfprobeSync(probeUrl);
			}
			catch
			{
			}
		});
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(3000);
				if (channelPlayer != null && !string.IsNullOrEmpty(channelPlayer.CurrentUrl) && channelPlayer.CurrentUrl == probeUrl)
				{
					try
					{
						GetFullStreamInfoWithFfprobeSync(probeUrl);
						return;
					}
					catch
					{
						return;
					}
				}
			}
			catch
			{
			}
		});
	}

	private static string FormatClock(long ms)
	{
		long num = Math.Max(0L, ms) / 1000;
		long h = num / 3600;
		long m = num % 3600 / 60;
		long s = num % 60;
		if (h <= 0)
		{
			return $"{m:00}:{s:00}";
		}
		return $"{h}:{m:00}:{s:00}";
	}

	private void TogglePreviewPanel()
	{
		if (previewPanel != null && channelPlayer != null)
		{
			bool show = !previewPanel.Visible;
			if (!show)
			{
				previewPanel.Visible = false;
				channelPlayer.StopAsync();
			}
			else
			{
				previewPanel.Visible = true;
			}
			if (btnTogglePreview != null)
			{
				btnTogglePreview.BackColor = (show ? theme.Primary : theme.Surface);
				btnTogglePreview.ForeColor = (show ? Color.White : theme.Primary);
			}
			if (show)
			{
				LoadSelectedChannelToPreview();
			}
			BeginInvoke((Action)delegate
			{
				ApplyColumnWidthsManual();
			});
		}
	}

	private void BuildUI()
	{
		AnimationSettings.Load();
		theme = MakeEffectiveTheme();
		Text = "";
		base.AutoScaleMode = AutoScaleMode.None;
		using (Graphics g = CreateGraphics())
		{
			dpiScale = g.DpiX / 96f;
		}
		config.Initialize(dpiScale);
		DarkMessageBox.DpiScale = dpiScale;
		int screenW = Screen.PrimaryScreen.WorkingArea.Width;
		int screenH = Screen.PrimaryScreen.WorkingArea.Height;
		int winW = Math.Max(1280, (int)((double)screenW * 0.88));
		int winH = Math.Max(800, (int)((double)screenH * 0.88));
		base.Size = new Size(winW, winH);
		base.StartPosition = FormStartPosition.Manual;
		base.Location = At((screenW - winW) / 2, (screenH - winH) / 2);
		Font = GetFont(SF(11f));
		MinimumSize = new Size(SX(900), SY(600));
		BackColor = theme.Border;
		outerWrap = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = theme.Border,
			Padding = new Padding(1)
		};
		base.Controls.Add(outerWrap);
		_borderOverlay = new Panel
		{
			Dock = DockStyle.Fill,
			Enabled = false,
			Visible = false
		};
		base.Controls.Add(_borderOverlay);
		base.HandleCreated += delegate
		{
			ApplyWindowShape();
		};
		base.Resize += delegate
		{
			if (base.WindowState != _lastWindowState)
			{
				_lastWindowState = base.WindowState;
				ApplyWindowShape();
			}
		};
		base.ResizeEnd += delegate
		{
			ApplyWindowShape();
		};
		CreateTitleBar();
		mainArea = new DoubleBufferedPanel
		{
			Dock = DockStyle.Fill,
			BackColor = theme.BgAlt
		};
		mainArea.Resize += delegate
		{
			UpdateScrollBarTheme(mainArea);
		};
		gridContainerRef = new DoubleBufferedPanel
		{
			Dock = DockStyle.Fill,
			BackColor = theme.BgAlt
		};
		dgvData = new DataGridView();
		dgvData.Dock = DockStyle.Fill;
		dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
		dgvData.ScrollBars = ScrollBars.None;
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
		typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(dgvData, true, null);
		dgvData.Font = GetFont(SF(6.7f));
		dgvData.RowTemplate.Height = SY(42);
		dgvData.CellDoubleClick += DgvData_CellDoubleClick;
		dgvData.CellEndEdit += DgvData_CellEndEdit;
		dgvData.KeyDown += delegate(object s, KeyEventArgs e)
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
		DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
		headerStyle.BackColor = theme.HeaderBg;
		headerStyle.ForeColor = theme.TextSecondary;
		headerStyle.Font = GetFont(SF(9f));
		headerStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
		headerStyle.Padding = new Padding(SX(10), 0, 0, 0);
		headerStyle.SelectionBackColor = theme.HeaderBg;
		headerStyle.SelectionForeColor = theme.TextSecondary;
		dgvData.ColumnHeadersDefaultCellStyle = headerStyle;
		DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
		rowStyle.BackColor = theme.Surface;
		rowStyle.ForeColor = theme.TextPrimary;
		rowStyle.SelectionBackColor = theme.SelectRow;
		rowStyle.SelectionForeColor = theme.SelectRowText;
		rowStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0));
		rowStyle.Font = GetFont(SF(6.7f));
		dgvData.RowsDefaultCellStyle = rowStyle;
		DataGridViewCellStyle altStyle = new DataGridViewCellStyle();
		altStyle.BackColor = theme.Surface;
		altStyle.ForeColor = theme.TextPrimary;
		altStyle.SelectionBackColor = theme.SelectRow;
		altStyle.SelectionForeColor = theme.SelectRowText;
		altStyle.Padding = new Padding(SX(10), SY(0), SX(6), SY(0));
		altStyle.Font = GetFont(SF(6.7f));
		dgvData.AlternatingRowsDefaultCellStyle = altStyle;
		dgvData.Columns.Add("colName", "名称");
		dgvData.Columns.Add("colUrl", "链接");
		dgvData.Columns.Add("colLocation", "归属地");
		dgvData.Columns.Add("colResolution", "分辨率");
		dgvData.Columns.Add("colSpeed", "响应速度");
		dgvData.Columns.Add("colGroup", "分组");
		dgvData.Columns.Add("colStatus", "状态");
		dgvData.Columns.Add("colAction", "操作");
		dgvData.Columns["colSpeed"].HeaderText = "响应";
		dgvData.CellClick += DgvData_CellClick;
		dgvData.ColumnHeaderMouseClick += DgvData_ColumnHeaderMouseClick;
		dgvData.CellPainting += DgvData_CellPainting;
		dgvData.Paint += DgvData_Paint;
		dgvData.CellFormatting += DgvData_CellFormatting;
		dgvData.CellMouseMove += DgvData_CellMouseMove;
		dgvData.CellMouseDown += DgvData_CellMouseDown;
		dgvData.CellMouseUp += DgvData_CellMouseUp;
		dgvData.ShowCellToolTips = true;
		dgvData.CellToolTipTextNeeded += delegate(object s, DataGridViewCellToolTipTextNeededEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
			{
				switch (dgvData.Columns[e.ColumnIndex].Name)
				{
				case "colName":
				case "colUrl":
				case "colLocation":
				case "colResolution":
				case "colGroup":
				{
					string text = dgvData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
					if (!string.IsNullOrWhiteSpace(text))
					{
						e.ToolTipText = text;
					}
					break;
				}
				}
			}
		};
		dgvData.MouseLeave += delegate
		{
			if (_hoverRow != -1)
			{
				_hoverRow = -1;
				_hoverBtn = -1;
				dgvData.Invalidate();
			}
		};
		dgvData.SelectionChanged += delegate
		{
			if (previewPanel != null && previewPanel.Visible)
			{
				// 防抖：连续切换行时延时加载，避免每经过一行都触发一次 VLC 加载导致卡死
				if (_previewDebounceTimer == null)
				{
					_previewDebounceTimer = new System.Windows.Forms.Timer();
					_previewDebounceTimer.Interval = 250;
				_previewDebounceTimer.Tick += delegate
				{
					_previewDebounceTimer.Stop();
					if (_isImporting)
					{
						// 导入中：暂停加载，稍后重试，避免与阻塞的 UI 线程争用 VLC 视频输出导致卡死
						_previewDebounceTimer.Start();
						return;
					}
					if (previewPanel != null && previewPanel.Visible)
					{
						LoadSelectedChannelToPreview();
					}
				};
				}
				_previewDebounceTimer.Stop();
				_previewDebounceTimer.Start();
			}
		};
		dgvData.Columns["colName"].FillWeight = 35f;
		dgvData.Columns["colUrl"].FillWeight = 120f;
		dgvData.Columns["colLocation"].FillWeight = 55f;
		dgvData.Columns["colResolution"].FillWeight = 32f;
		dgvData.Columns["colSpeed"].FillWeight = 25f;
		dgvData.Columns["colGroup"].FillWeight = 28f;
		dgvData.Columns["colStatus"].FillWeight = 30f;
		dgvData.Columns["colAction"].FillWeight = 30f;
		dgvData.Columns["colName"].MinimumWidth = SX(80);
		dgvData.Columns["colUrl"].MinimumWidth = SX(80);
		dgvData.Columns["colLocation"].MinimumWidth = SX(55);
		dgvData.Columns["colResolution"].MinimumWidth = SX(32);
		dgvData.Columns["colSpeed"].MinimumWidth = SX(20);
		dgvData.Columns["colGroup"].MinimumWidth = SX(28);
		dgvData.Columns["colStatus"].MinimumWidth = SX(30);
		dgvData.Columns["colAction"].MinimumWidth = SX(100);
		dgvData.Resize += delegate
		{
			ApplyColumnWidthsManual();
		};
		foreach (DataGridViewColumn column in dgvData.Columns)
		{
			column.SortMode = DataGridViewColumnSortMode.Programmatic;
			column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
		}
		dgvData.Columns["colName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
		dgvData.Columns["colUrl"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
		dgvData.Columns["colLocation"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
		dgvData.Columns["colResolution"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
		dgvData.Columns["colGroup"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
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
		sortedColumn = "colName";
		sortDirection = SortOrder.Ascending;
		gridContainerRef.Controls.Add(dgvData);
		darkVScrollBar = new DarkScrollBar
		{
			Dock = DockStyle.Right,
			Width = SystemInformation.VerticalScrollBarWidth,
			Visible = false
		};
		bool gridDark = theme != null && DrawingUtils.IsDarkColor(theme.Bg);
		darkVScrollBar.TrackColor = (gridDark ? theme.BgAlt : Color.FromArgb(240, 240, 240));
		darkVScrollBar.ThumbColor = (gridDark ? theme.TextSecondary : Color.FromArgb(120, 120, 120));
		darkVScrollBar.ThumbHoverColor = (gridDark ? theme.TextPrimary : Color.FromArgb(100, 100, 100));
		darkVScrollBar.ThumbPressedColor = theme.Primary;
		darkVScrollBar.BackColor = darkVScrollBar.TrackColor;
		darkVScrollBar.ValueChanged += delegate
		{
			if (dgvData != null && dgvData.IsHandleCreated && dgvData.RowCount != 0)
			{
				int value = darkVScrollBar.Value;
				if (value >= 0 && value < dgvData.RowCount)
				{
					try
					{
						dgvData.FirstDisplayedScrollingRowIndex = value;
					}
					catch
					{
					}
				}
			}
		};
		gridContainerRef.Controls.Add(darkVScrollBar);
		darkVScrollBar.BringToFront();
		dgvData.Scroll += delegate
		{
			SyncGridScrollBar();
		};
		dgvData.RowsAdded += delegate
		{
			UpdateGridScrollBar();
			QueueVlcCheckIfDataPresent();
		};
		dgvData.RowsRemoved += delegate
		{
			UpdateGridScrollBar();
		};
		dgvData.Resize += delegate
		{
			UpdateGridScrollBar();
		};
		dgvData.MouseWheel += delegate(object s, MouseEventArgs e)
		{
			if (darkVScrollBar != null && darkVScrollBar.Visible)
			{
				int num = SystemInformation.MouseWheelScrollLines;
				if (num == 0)
				{
					num = 3;
				}
				int num2 = ((e.Delta > 0) ? (-num) : num);
				int val = darkVScrollBar.Value + num2;
				val = Math.Max(darkVScrollBar.Minimum, Math.Min(darkVScrollBar.Maximum, val));
				if (val != darkVScrollBar.Value)
				{
					darkVScrollBar.Value = val;
					if (dgvData != null && dgvData.IsHandleCreated && val >= 0 && val < dgvData.RowCount)
					{
						try
						{
							dgvData.FirstDisplayedScrollingRowIndex = val;
						}
						catch
						{
						}
					}
				}
			}
		};
		emptyStatePanel = new Panel
		{
			BackColor = Color.Transparent,
			Size = new Size(SX(140), SY(110))
		};
		PictureBox emptyIconBox = new PictureBox
		{
			Size = new Size(SX(56), SY(56)),
			Location = At(SX(42), SY(0)),
			BackColor = Color.Transparent,
			SizeMode = PictureBoxSizeMode.CenterImage
		};
		emptyIconBox.Paint += EmptyIcon_Paint;
		emptyLabel = new Label
		{
			Text = "无效站",
			Font = GetFont(SF(11f)),
			ForeColor = Color.FromArgb(180, 180, 180),
			AutoSize = true,
			TextAlign = ContentAlignment.MiddleCenter
		};
		emptyStatePanel.Controls.Add(emptyIconBox);
		emptyStatePanel.Controls.Add(emptyLabel);
		gridContainerRef.Controls.Add(emptyStatePanel);
		emptyStatePanel.BringToFront();
		gridContainerRef.Resize += delegate
		{
			CenterEmptyState();
		};
		previewPanel = new Panel
		{
			Dock = DockStyle.Right,
			Width = SX(360),
			BackColor = theme.BgAlt,
			Visible = false
		};
		Panel previewSep = new Panel
		{
			Dock = DockStyle.Left,
			Width = 1,
			BackColor = theme.Border
		};
		previewPanel.Controls.Add(previewSep);
		// 预览窗容器：带线框边框
		Panel playerFrame = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(270),
			BackColor = theme.BgAlt,
			Padding = new Padding(SX(4), 1, 1, 1)
		};
		playerFrame.Paint += delegate(object s, PaintEventArgs pe)
		{
			using (Pen pen = new Pen(theme.Border, 1))
			{
				Rectangle r = playerFrame.ClientRectangle;
				r.Inflate(-1, -1);
				pe.Graphics.DrawRectangle(pen, r);
			}
		};
		channelPlayer = new ChannelPlayer
		{
			Dock = DockStyle.Fill
		};
		// 精确 16:9：按视频区实际宽度反推 playerFrame 高度，消除上下黑边
		// 视频宽 = 预览窗宽 − 左分隔线1 − Padding左SX(4) − Padding右1
		int previewVideoW = SX(360) - 1 - SX(4) - 1;
		playerFrame.Height = (int)Math.Round((double)previewVideoW * 9.0 / 16.0) + channelPlayer.ControlBarHeight + 2;
		channelPlayer.OpenExternalRequested += delegate
		{
			if (!string.IsNullOrWhiteSpace(channelPlayer.CurrentUrl))
			{
				// 使用 ExternalPlayerHelper 自动选择最佳播放器（PotPlayer > VLC > MPV > FFplay > 自定义 > 系统默认）
				if (!ExternalPlayerHelper.TryPlayFallback(channelPlayer.CurrentUrl))
				{
					// 兜底：用系统默认方式打开
					try
					{
						Process.Start(new ProcessStartInfo(channelPlayer.CurrentUrl)
						{
							UseShellExecute = true
						});
					}
					catch
					{
					}
				}
			}
		};
		// 间距面板：预览窗与流媒体信息栏之间的间隔
		Panel playerGap = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(16),
			BackColor = theme.BgAlt
		};
		playerGap.Tag = "__gap";
		Panel linkStatusPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(280),
			BackColor = theme.BgAlt,
			Padding = new Padding(SX(6), SY(4), SX(6), SY(4))
		};
		_linkStatusPanel = linkStatusPanel;
		linkStatusPanel.Paint += delegate(object s, PaintEventArgs pe)
		{
			pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			Rectangle r = linkStatusPanel.ClientRectangle;
			r.Inflate(-1, -1);
			int radius = 6;
			int d = radius * 2;
			using (var path = new System.Drawing.Drawing2D.GraphicsPath())
			{
				path.AddArc(r.X, r.Y, d, d, 180f, 90f);
				path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
				path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
				path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
				path.CloseFigure();
				using (var pen = new Pen(theme.Border, 1))
				{
					pe.Graphics.DrawPath(pen, path);
				}
			}
		};
		Label linkStatusTitle = new Label
		{
			Text = "当前链接状态 · 实时",
			Font = GetFont(SF(10f), FontStyle.Bold),
			ForeColor = theme.TextPrimary,
			AutoSize = true,
			Location = At(SX(12), SY(8))
		};
		linkStatusPanel.Controls.Add(linkStatusTitle);
		int lsy = SY(34);
		int lsRow = SY(20);
		_statusStreamInfo = MakeStatusRow("流媒体信息: 未播放");
		_statusName = MakeStatusRow("频道名称: —");
		_statusResolution = MakeStatusRow("分辨率: —");
		_statusLocation = MakeStatusRow("归属地: —");
		_statusCodec = MakeStatusRow("编码: —");
		_statusFps = MakeStatusRow("帧率: —");
		_statusBitrate = MakeStatusRow("速率: —");
		_statusChannels = MakeStatusRow("声道: —");
		_statusSampleRate = MakeStatusRow("采样率: —");
		_statusSent = MakeStatusRow("已传输: —");
		_statusSpeed = MakeStatusRow("倍速: —");
		_statusTime = MakeStatusRow("时间: —");
		_linkStatusTimer = new System.Windows.Forms.Timer
		{
			Interval = 500
		};
		_linkStatusTimer.Tick += delegate
		{
			if (previewPanel != null && previewPanel.Visible && _statusStreamInfo != null && channelPlayer != null)
			{
				// 切台进行中：UI 线程完全不访问 libvlc，避免与后台 Stop 的视频输出拆除死锁卡死。
				// 此期间状态栏短暂不刷新（属预期），切台完成后自动恢复，软件主窗口始终可响应。
				if (channelPlayer.IsSwitching)
				{
					return;
				}
				bool isPlaying;
				long timeMs;
				long lengthMs;
				bool num = channelPlayer.TryGetLiveState(out isPlaying, out timeMs, out lengthMs);
				TryUpdateStreamInfoFromVlc();
				bool flag = false;
				if (num)
				{
					flag = isPlaying;
					if (!flag && timeMs > 0)
					{
						flag = timeMs != _lastStreamTimeMs;
					}
					_lastStreamTimeMs = timeMs;
				}
				string text = "未播放";
				if (num && flag)
				{
					text = "● 直播中";
					if (!string.IsNullOrEmpty(_currentResolution))
					{
						text = text + " · " + _currentResolution;
					}
					if (!string.IsNullOrEmpty(_currentBitrate))
					{
						text = text + " · " + _currentBitrate;
					}
				}
				else if (!string.IsNullOrWhiteSpace(channelPlayer.CurrentUrl))
				{
					text = "已加载(未播放)";
				}
				_statusStreamInfo.Text = "流媒体信息: " + text;
				_statusName.Text = "频道名称: " + V(_previewChannelName);
				_statusResolution.Text = "分辨率: " + V(_currentResolution);
				string text2 = _previewChannelLocation;
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = ExtractLocationFallback(channelPlayer.CurrentUrl);
				}
				_statusLocation.Text = "归属地: " + V(text2);
				_statusCodec.Text = "编码: " + V(SimplifyCodecDisplay(_currentCodec));
				_statusFps.Text = "帧率: " + V(_currentFps);
				_statusBitrate.Text = "速率: " + V(_currentBitrate);
				_statusChannels.Text = "声道: " + V(_currentAudioChannels);
				_statusSampleRate.Text = "采样率: " + V(_currentAudioSampleRate);
				if (channelPlayer.TryGetPlayerStats(out var readBytes, out var inputBitrate, out var _, out var _))
				{
					_statusSent.Text = "已传输: " + FormatBytes(readBytes);
					_statusSpeed.Text = "倍速: " + ((channelPlayer.TryGetRate(out var rate) && rate > 0f) ? $"{rate:F2}x" : "—");
				}
				else
				{
					_statusSent.Text = "已传输: " + V(_currentSize);
					_statusSpeed.Text = "倍速: " + V(_currentSpeed);
				}
				string text3 = ((!num || timeMs <= 0) ? V(_currentTime) : (FormatClock(timeMs) + ((lengthMs > 0) ? (" / " + FormatClock(lengthMs)) : "")));
				_statusTime.Text = "时间: " + text3;
			}
		};
		_linkStatusTimer.Start();
		Panel detailPanel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = theme.BgAlt
		};
		Panel detailHeader = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(30),
			BackColor = theme.Bg
		};
		Label detailTitle = new Label
		{
			Text = "频道详情",
			Font = GetFont(SF(10f), FontStyle.Bold),
			ForeColor = theme.TextPrimary,
			AutoSize = true,
			Location = At(SX(12), SY(7))
		};
		detailHeader.Controls.Add(detailTitle);
		detailPanel.Controls.Add(detailHeader);
		int dx = SX(12);
		int dy = SY(40);
		int dRow = SY(22);
		_detailName = MakeDetailRow("名称: ");
		_detailUrl = MakeDetailRow("地址: ");
		_detailResolution = MakeDetailRow("分辨率: ");
		_detailGroup = MakeDetailRow("分组: ");
		_detailStatus = MakeDetailRow("状态: ");
		_detailBitrate = MakeDetailRow("码率: ");
		playerFrame.Controls.Add(channelPlayer);
		previewPanel.Controls.Add(playerFrame);
		previewPanel.Controls.Add(playerGap);
		previewPanel.Controls.Add(linkStatusPanel);
		previewPanel.Controls.Add(detailPanel);
		gridContainerRef.Controls.Add(previewPanel);
		searchPanelRef = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = theme.BgAlt
		};
		Label lblSearch = new Label
		{
			Text = "搜 索 :",
			Font = GetFont(SF(8.5f)),
			ForeColor = theme.TextPrimary,
			Location = At(0, SY(0)),
			AutoSize = true
		};
		lblSearch.Height = SY(26);
		lblSearch.Top = (SY(38) - SY(26)) / 2;
		searchPanelRef.Controls.Add(lblSearch);
		cboGroupHost = new Panel
		{
			BackColor = theme.BgAlt,
			Visible = false,
			Anchor = (AnchorStyles.Top | AnchorStyles.Left),
			Location = At(0, (SY(38) - SY(26)) / 2),
			Size = new Size(110, SY(26))
		};
		DarkComboBox darkCbo = new DarkComboBox
		{
			Font = GetFont(SF(8.5f)),
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
		Label lblGroup = new Label
		{
			Text = "分组:",
			Font = GetFont(SF(8.5f)),
			ForeColor = theme.TextPrimary,
			AutoSize = true,
			Anchor = (AnchorStyles.Top | AnchorStyles.Left),
			Visible = false,
			TextAlign = ContentAlignment.MiddleRight
		};
		lblGroup.Height = SY(26);
		lblGroup.Top = (SY(38) - SY(26)) / 2;
		lblGroupFilter = lblGroup;
		searchPanelRef.Controls.Add(lblGroup);
		Panel searchBoxHost = new Panel
		{
			Location = At(98, (SY(38) - SY(26)) / 2),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left),
			Size = new Size(40, SY(26)),
			BackColor = theme.Surface
		};
		searchBoxHostRef = searchBoxHost;
		searchPanelRef.Controls.Add(searchBoxHost);
		TextBox txtSearch = new TextBox
		{
			Font = GetFont(SF(8f)),
			BorderStyle = BorderStyle.None,
			Location = At(18, SY(2)),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right),
			Width = searchBoxHost.Width - 20,
			Height = SY(18),
			Text = "输入搜索内容，按下回车键搜索",
			ForeColor = theme.TextSecondary,
			BackColor = theme.Surface
		};
		txtSearchBox = txtSearch;
		searchBoxHost.Controls.Add(txtSearch);
		cboGroup.HandleCreated += delegate
		{
			SetWindowTheme(cboGroup.Handle, "", "");
		};
		bool searchFocus = false;
		txtSearch.GotFocus += delegate
		{
			searchFocus = true;
			searchBoxHost.Invalidate();
			if (txtSearch.Text == "输入搜索内容，按下回车键搜索")
			{
				txtSearch.Text = "";
				txtSearch.ForeColor = theme.TextPrimary;
			}
		};
		txtSearch.LostFocus += delegate
		{
			searchFocus = false;
			searchBoxHost.Invalidate();
			if (string.IsNullOrWhiteSpace(txtSearch.Text))
			{
				txtSearch.Text = "输入搜索内容，按下回车键搜索";
				txtSearch.ForeColor = theme.TextSecondary;
			}
		};
		searchBoxHost.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle rect = new Rectangle(0, 0, searchBoxHost.Width - 1, searchBoxHost.Height - 1);
			Color color = (searchFocus ? theme.Primary : theme.Border);
			using GraphicsPath path = GetRoundedPath(rect, 6);
			using (SolidBrush brush = new SolidBrush(theme.Surface))
			{
				e.Graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(color, searchFocus ? 1.5f : 1f);
			e.Graphics.DrawPath(pen, path);
		};
		UpdateSearchBoxRegion();
		UpdateCboGroupRegion();
		searchPanelRef.Resize += delegate
		{
			int num = (cboGroupHost.Visible ? 328 : 15);
			int num2 = 98;
			searchBoxHost.Left = num2;
			searchBoxHost.Width = Math.Max(100, searchPanelRef.ClientSize.Width - num2 - num);
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
		txtSearch.KeyDown += delegate(object s, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				SearchChannels(txtSearch.Text);
				e.SuppressKeyPress = true;
			}
		};
		ContextMenuStrip txtMenu = new ContextMenuStrip
		{
			Font = GetFont(SF(10f)),
			ShowImageMargin = true,
			BackColor = theme.Surface
		};
		AnimatedMenuRenderer txtMenuRenderer = new AnimatedMenuRenderer(theme);
		txtMenu.Renderer = txtMenuRenderer;
		txtMenuRenderer.Register(txtMenu);
		txtMenu.ForeColor = theme.TextPrimary;
		ToolStripMenuItem miCut = new ToolStripMenuItem("剪切", null, delegate
		{
			txtSearch.Cut();
		})
		{
			ShortcutKeyDisplayString = "Ctrl+X"
		};
		ToolStripMenuItem miCopy = new ToolStripMenuItem("复制", null, delegate
		{
			txtSearch.Copy();
		})
		{
			ShortcutKeyDisplayString = "Ctrl+C"
		};
		ToolStripMenuItem miPaste = new ToolStripMenuItem("粘贴", null, delegate
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
		{
			ShortcutKeyDisplayString = "Ctrl+V"
		};
		ToolStripMenuItem miClear = new ToolStripMenuItem("清空", null, delegate
		{
			txtSearch.Clear();
			txtSearch.ForeColor = theme.TextSecondary;
		});
		txtMenu.Items.AddRange(new ToolStripItem[5]
		{
			miCut,
			miCopy,
			miPaste,
			new ToolStripSeparator(),
			miClear
		});
		txtMenu.Opening += delegate
		{
			bool flag = txtSearch.SelectionLength > 0;
			bool enabled = !string.IsNullOrEmpty(txtSearch.Text) && txtSearch.Text != "输入搜索内容，按下回车键搜索";
			miCut.Enabled = flag && !txtSearch.ReadOnly;
			miCopy.Enabled = flag;
			miPaste.Enabled = Clipboard.ContainsText() && !txtSearch.ReadOnly;
			miClear.Enabled = enabled;
		};
		txtSearch.ContextMenuStrip = txtMenu;
		Panel searchSep = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 1,
			BackColor = ColorBorder
		};
		searchPanelRef.Controls.Add(searchSep);
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
			BackColor = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(60, 60, 70) : Color.FromArgb(200, 200, 210))
		};
		statusBarRef.Paint += delegate(object s, PaintEventArgs e)
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
		statusBarRef.Resize += delegate
		{
			LayoutStatusBar(statusBarRef);
			UpdateStatusBarRegion();
		};
		statusBarContainer.Controls.Add(statusBarRef);
		LayoutStatusBar(statusBarRef);
		mainArea.Controls.Add(gridContainerRef);
		mainArea.Controls.Add(statusBarContainer);
		toolbarRef = new Panel
		{
			Dock = DockStyle.Top,
			Height = SY(42),
			BackColor = theme.BgAlt
		};
		importHost = new Panel
		{
			Dock = DockStyle.Left,
			BackColor = theme.BgAlt
		};
		btnTbImport = new Button
		{
			Size = new Size(SX(96), SY(30)),
			Location = At(SX(10), SY(6)),
			FlatStyle = FlatStyle.Flat,
			Text = "导入M3U",
			Font = GetFont(SF(9.5f), FontStyle.Bold),
			ForeColor = Color.White,
			BackColor = theme.Primary,
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter
		};
		btnTbImport.Click += delegate(object s, EventArgs e)
		{
			BtnSelectFile_Click(s, e);
		};
		importHost.Controls.Add(btnTbImport);
		StyleRoundButton(btnTbImport, 8, theme.Primary, 1);
		toolbarRef.Controls.Add(searchPanelRef);
		toolbarRef.Controls.Add(importHost);
		mainArea.Controls.Add(toolbarRef);
		actionArea = new Panel
		{
			Dock = DockStyle.Left,
			Width = SX(150),
			BackColor = theme.BgAlt,
			AutoScroll = true
		};
		actionArea.ControlAdded += delegate(object s, ControlEventArgs e)
		{
			if (e.Control is VScrollBar || e.Control is HScrollBar)
			{
				UpdateScrollBarTheme(actionArea);
			}
		};
		actionArea.HandleCreated += delegate
		{
			UpdateScrollBarTheme(actionArea);
		};
		int ay = SY(14);
		int btnW = SX(126);
		int leftX = SX(12);
		Button btnSelectFile = new Button
		{
			Text = "选择m3u/txt",
			Location = At(leftX, ay),
			Size = new Size(btnW, SY(32)),
			FlatStyle = FlatStyle.Flat,
			BackColor = theme.Surface,
			ForeColor = theme.Primary,
			Font = GetFont(SF(8.5f)),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter,
			ImageAlign = ContentAlignment.MiddleLeft
		};
		btnSelectFile.Paint += delegate(object s, PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			int num = 16;
			int num2 = (btnSelectFile.Height - 12) / 2;
			using (Pen pen = new Pen(theme.Primary, 1.5f))
			{
				graphics.DrawRectangle(pen, num, num2, 12, 10);
				graphics.DrawLine(pen, num, num2 + 3, num + 12, num2 + 3);
			}
			using SolidBrush brush = new SolidBrush(theme.Primary);
			graphics.FillRectangle(brush, num + 2, num2 + 1, 3, 2);
			graphics.FillRectangle(brush, num + 7, num2 + 1, 3, 2);
		};
		btnSelectFile.FlatAppearance.MouseOverBackColor = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.FromArgb(55, 55, 65) : Color.FromArgb(248, 242, 255));
		btnSelectFile.FlatAppearance.BorderColor = theme.Primary;
		btnSelectFile.FlatAppearance.BorderSize = 1;
		btnSelectFile.Click += BtnSelectFile_Click;
		StyleRoundButton(btnSelectFile, 8, theme.Primary, 1, "border");
		ay += SY(32) + SY(10);
		btnStartDetect = new Button
		{
			Text = "开始检测",
			Location = At(btnTbImport.Right + SX(8), SY(6)),
			Size = new Size(SX(84), SY(30)),
			FlatStyle = FlatStyle.Flat,
			BackColor = theme.InfoColor,
			ForeColor = Color.White,
			Font = GetFont(SF(9.5f), FontStyle.Bold),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter,
			ImageAlign = ContentAlignment.MiddleLeft,
			Visible = false
		};
		btnStartDetect.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, theme.InfoColor.R + 20), Math.Min(255, theme.InfoColor.G + 20), Math.Min(255, theme.InfoColor.B + 20));
		btnStartDetect.FlatAppearance.BorderColor = theme.InfoColor;
		btnStartDetect.FlatAppearance.BorderSize = 0;
		btnStartDetect.Click += BtnStartDetect_Click;
		importHost.Controls.Add(btnStartDetect);
		StyleRoundButton(btnStartDetect, 8, theme.InfoColor, 1, "info");
		ay += SY(36) + SY(6);
		btnStopDetect = new Button
		{
			Text = "停止检测",
			Location = At(btnStartDetect.Right + SX(8), SY(6)),
			Size = new Size(SX(64), SY(30)),
			FlatStyle = FlatStyle.Flat,
			BackColor = theme.ErrorColor,
			ForeColor = Color.White,
			Font = GetFont(SF(8.5f), FontStyle.Bold),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter,
			Visible = false,
			Enabled = false
		};
		btnStopDetect.EnabledChanged += delegate
		{
			btnStopDetect.Invalidate();
		};
		btnStopDetect.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, theme.ErrorColor.R + 20), Math.Min(255, theme.ErrorColor.G + 20), Math.Min(255, theme.ErrorColor.B + 20));
		btnStopDetect.FlatAppearance.BorderSize = 0;
		btnStopDetect.Click += delegate
		{
			if (DarkMessageBox.Show("确定要停止检测吗？已检测的数据将被保留。", "停止检测", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				cts?.Cancel();
				isDetecting = false;
				isPaused = false;
				btnStartDetect.Text = "开始检测";
				btnStartDetect.BackColor = theme.InfoColor;
				btnStartDetect.ForeColor = Color.White;
				btnStopDetect.Enabled = false;
				if (btnScanSource != null)
				{
					btnScanSource.Enabled = true;
				}
			}
		};
		importHost.Controls.Add(btnStopDetect);
		StyleRoundButton(btnStopDetect, 8, theme.ErrorColor, 0, "error");
		ay += SY(32) + SY(10);
		Color exportBtnColor = Color.FromArgb(255, 0, 255);
		btnExport = new Button
		{
			Text = "合并导出",
			Location = At(btnStopDetect.Right + SX(8), SY(6)),
			Size = new Size(SX(96), SY(30)),
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
		StyleRoundButton(btnExport, 8, null, 0, "export");
		importHost.Controls.Add(btnExport);
		btnScanSource = new Button
		{
			Text = "源生成器",
			Location = At(btnExport.Right + SX(8), SY(6)),
			Size = new Size(SX(96), SY(30)),
			FlatStyle = FlatStyle.Flat,
			BackColor = theme.SuccessColor,
			ForeColor = Color.White,
			Font = GetFont(SF(9f), FontStyle.Bold),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter
		};
		btnScanSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, theme.SuccessColor.R + 20), Math.Min(255, theme.SuccessColor.G + 20), Math.Min(255, theme.SuccessColor.B + 20));
		btnScanSource.FlatAppearance.BorderSize = 0;
		btnScanSource.Click += delegate
		{
			ShowScanSourceDialog();
		};
		importHost.Controls.Add(btnScanSource);
		StyleRoundButton(btnScanSource, 8, theme.SuccessColor, 0, "success");
		ay += SY(34) + SY(10);
		ay += SY(34) + SY(6);
		btnParseLink = new Button
		{
			Text = "解析链接",
			Location = At(leftX, ay),
			Size = new Size(btnW, SY(34)),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(147, 51, 234),
			ForeColor = Color.White,
			Font = GetFont(SF(9f), FontStyle.Bold),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter,
			Visible = false
		};
		btnParseLink.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, 167), Math.Min(255, 71), Math.Min(255, 254));
		btnParseLink.FlatAppearance.BorderSize = 0;
		bool parseIsRunning = false;
		bool parseIsPaused = false;
		int parseSuccessCount = 0;
		int parseTotalCount = 0;
		CancellationTokenSource parseCts = null;
		btnParseLink.Click += async delegate
		{
			if (!parseIsRunning && !parseIsPaused)
			{
				List<ChannelInfo> pendingChannels = allChannels.Where((ChannelInfo c) => c.Group == "解析待处理" && c.Status == "待解析").ToList();
				if (pendingChannels.Count == 0)
				{
					DarkMessageBox.Show("没有待解析的链接", "解析链接", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				else
				{
					parseIsRunning = true;
					parseIsPaused = false;
					parseSuccessCount = 0;
					parseTotalCount = pendingChannels.Count;
					parseCts = new CancellationTokenSource();
					btnParseLink.Text = "暂停";
					btnParseLink.BackColor = Color.FromArgb(251, 146, 60);
					DateTime parseTime = DateTime.Now;
					List<ChannelInfo> failedChannels = new List<ChannelInfo>();
					foreach (ChannelInfo channel in pendingChannels)
					{
						if (parseIsPaused)
						{
							while (parseIsPaused && !parseCts.Token.IsCancellationRequested)
							{
								await Task.Delay(100);
							}
						}
						if (parseCts.Token.IsCancellationRequested)
						{
							break;
						}
						bool parsedSuccess;
						if (!(channel.Status != "待解析"))
						{
							parsedSuccess = false;
							try
							{
								if (Uri.IsWellFormedUriString(channel.Url, UriKind.Absolute))
								{
									using (CancellationTokenSource ctsParse = new CancellationTokenSource(TimeSpan.FromSeconds(8.0)))
									{
										HttpResponseMessage resp = await httpClient.GetAsync(channel.Url, ctsParse.Token);
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
									goto IL_03a2;
								}
							}
							catch
							{
								goto IL_03a2;
							}
						}
						continue;
						IL_03a2:
						if (!parsedSuccess)
						{
							failedChannels.Add(channel);
						}
						btnParseLink.Text = $"暂停 ({parseSuccessCount}/{parseTotalCount})";
					}
					foreach (ChannelInfo failed in failedChannels)
					{
						allChannels.Remove(failed);
					}
					parseIsRunning = false;
					parseIsPaused = false;
					if (parseCts != null)
					{
						parseCts.Dispose();
					}
					parseCts = null;
					btnParseLink.Text = "解析链接";
					btnParseLink.BackColor = Color.FromArgb(147, 51, 234);
					RefreshGrid();
					UpdateEmptyState();
					UpdateActionButtonsVisibility();
				}
			}
			else if (parseIsRunning && !parseIsPaused)
			{
				parseIsPaused = true;
				btnParseLink.Text = "停止";
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
				btnParseLink.Text = "解析链接";
				btnParseLink.BackColor = Color.FromArgb(147, 51, 234);
				RefreshGrid();
				UpdateEmptyState();
				UpdateActionButtonsVisibility();
			}
		};
		StyleRoundButton(btnParseLink, 8, Color.FromArgb(147, 51, 234), 0, "parse");
		ay += SY(34) + SY(26);
		int tipW = btnW;
		int tipRadius = 8;
		string tipText = "1. 列表位置，点击右键发现更多功能\r\n2. 双击名称，重命名，双击链接，修复直播源。\r\n3. 打开设置发现更多功能。";
		Font tipContentFont = GetFont(9f);
		SizeF tipTextSize;
		using (Graphics g2 = CreateGraphics())
		{
			tipTextSize = g2.MeasureString(tipText, tipContentFont, tipW - 24);
		}
		int tipContentHeight = (int)Math.Ceiling(tipTextSize.Height);
		int tipBoxHeight = 38 + tipContentHeight + 10;
		tipBox = new Panel
		{
			Location = At(leftX, ay),
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
			Location = At(SX(12), SY(10)),
			BackColor = Color.Transparent
		};
		tipBox.Controls.Add(tipTitle);
		Label tipContent = new Label
		{
			Text = tipText,
			Font = tipContentFont,
			ForeColor = theme.TextSecondary,
			AutoSize = false,
			Location = At(12, 38),
			Size = new Size(tipW - 24, tipContentHeight),
			BackColor = Color.Transparent
		};
		tipBox.Controls.Add(tipContent);
		tipBox.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle r = new Rectangle(0, 0, tipBox.Width - 1, tipBox.Height - 1);
			using GraphicsPath path = RoundedRectPath(r, tipRadius);
			using (SolidBrush brush = new SolidBrush(theme.TipBg))
			{
				e.Graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(theme.Border);
			e.Graphics.DrawPath(pen, path);
		};
		tipBox.Resize += delegate
		{
			Label label = null;
			foreach (Control control in tipBox.Controls)
			{
				if (control is Label && control != tipTitle)
				{
					label = (Label)control;
					break;
				}
			}
			if (label == null)
			{
				return;
			}
			using Graphics graphics = tipBox.CreateGraphics();
			int num = (int)Math.Ceiling(graphics.MeasureString(label.Text, label.Font, tipBox.Width - 24).Height);
			int num2 = 38 + num + 10;
			if (tipBox.Height != num2)
			{
				tipBox.Height = num2;
				label.Height = num;
				UpdateActionButtonsVisibility();
			}
		};
		ay += tipBoxHeight + SY(10);
		btnTogglePreview = new Button
		{
			Text = "预览窗",
			Location = At(leftX, ay),
			Size = new Size(btnW, SY(34)),
			FlatStyle = FlatStyle.Flat,
			BackColor = theme.Surface,
			ForeColor = theme.Primary,
			Font = GetFont(SF(9f), FontStyle.Bold),
			Cursor = Cursors.Hand,
			TextAlign = ContentAlignment.MiddleCenter
		};
		btnTogglePreview.FlatAppearance.BorderColor = theme.Primary;
		btnTogglePreview.FlatAppearance.BorderSize = 1;
		btnTogglePreview.Click += delegate
		{
			TogglePreviewPanel();
		};
		StyleRoundButton(btnTogglePreview, 8, theme.Primary, 1, "border");
		ay += SY(34) + SY(10);
		UpdateActionButtonsVisibility();
		actionSepRef = new Panel
		{
			Dock = DockStyle.Left,
			Width = 1,
			BackColor = ColorBorder
		};
		outerWrap.Controls.Add(mainArea);
		CreateBottomBar();
		outerWrap.Controls.Add(bottomBarRef);
		outerWrap.Controls.Add(titleBarPanel);
		dataGridViewContextMenu = new ContextMenuStrip();
		dataGridViewContextMenu.Font = GetFont(SF(9f));
		AnimatedMenuRenderer gridMenuRenderer = new AnimatedMenuRenderer(theme);
		dataGridViewContextMenu.Renderer = gridMenuRenderer;
		gridMenuRenderer.Register(dataGridViewContextMenu);
		dataGridViewContextMenu.ForeColor = theme.TextPrimary;
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
		ToolStripMenuItem pasteItem = new ToolStripMenuItem("从剪贴板粘贴链接", icoPaste, delegate
		{
			PasteFromClipboard();
		});
		pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
		dataGridViewContextMenu.Items.Add(pasteItem);
		ToolStripMenuItem detectMenuItem = new ToolStripMenuItem("检测模式", icoDetect);
		ToolStripMenuItem modeNormal = new ToolStripMenuItem("普通模式(逐个检测)");
		ToolStripMenuItem modeFast = new ToolStripMenuItem("极速模式(5并发)");
		ToolStripMenuItem modeConcurrent = new ToolStripMenuItem("并发模式(10并发)");
		modeNormal.Click += delegate
		{
			detectConcurrency = 1;
			modeNormal.Checked = true;
			modeFast.Checked = false;
			modeConcurrent.Checked = false;
		};
		modeFast.Click += delegate
		{
			detectConcurrency = 5;
			modeNormal.Checked = false;
			modeFast.Checked = true;
			modeConcurrent.Checked = false;
		};
		modeConcurrent.Click += delegate
		{
			detectConcurrency = 10;
			modeNormal.Checked = false;
			modeFast.Checked = false;
			modeConcurrent.Checked = true;
		};
		modeConcurrent.Checked = true;
		detectMenuItem.DropDownItems.Add(modeNormal);
		detectMenuItem.DropDownItems.Add(modeFast);
		detectMenuItem.DropDownItems.Add(modeConcurrent);
		dataGridViewContextMenu.Items.Add(detectMenuItem);
		ToolStripMenuItem sortMenuItem = new ToolStripMenuItem("排序", icoSort);
		sortMenuItem.DropDownItems.Add("按名称排序", null, delegate
		{
			allChannels.Sort((ChannelInfo a, ChannelInfo b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
			RefreshGrid();
		});
		sortMenuItem.DropDownItems.Add("按延迟排序", null, delegate
		{
			allChannels.Sort(delegate(ChannelInfo a, ChannelInfo b)
			{
				int num = ParseSpeed(a.Speed);
				int value = ParseSpeed(b.Speed);
				return num.CompareTo(value);
			});
			RefreshGrid();
		});
		sortMenuItem.DropDownItems.Add("按状态排序", null, delegate
		{
			allChannels.Sort((ChannelInfo a, ChannelInfo b) => string.Compare(a.Status, b.Status, StringComparison.Ordinal));
			RefreshGrid();
		});
		sortMenuItem.DropDownItems.Add("按分组排序", null, delegate
		{
			allChannels.Sort((ChannelInfo a, ChannelInfo b) => string.Compare(a.Group, b.Group, StringComparison.Ordinal));
			RefreshGrid();
		});
		dataGridViewContextMenu.Items.Add(sortMenuItem);
		ToolStripMenuItem playMenuItem = new ToolStripMenuItem("播放", icoPlay);
		playMenuItem.DropDownItems.Add("第三方播放器", null, delegate
		{
			if (dgvData.SelectedRows.Count > 0)
			{
				string text = dgvData.SelectedRows[0].Cells[1].Value?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					PlayChannelCustom(text);
				}
			}
		});
		playMenuItem.DropDownItems.Add(new ToolStripSeparator());
		playMenuItem.DropDownItems.Add("设置第三方播放器路径...", null, delegate
		{
			SetCustomPlayerPath();
		});
		dataGridViewContextMenu.Items.Add(playMenuItem);
		dataGridViewContextMenu.Items.Add(new ToolStripSeparator());
		ToolStripMenuItem copyItem = new ToolStripMenuItem("复制链接", icoCopy, delegate
		{
			CopyLink();
		});
		copyItem.ShortcutKeyDisplayString = "Ctrl+C";
		dataGridViewContextMenu.Items.Add(copyItem);
		ToolStripMenuItem copyAllItem = new ToolStripMenuItem("复制所有链接", icoCopyAll, delegate
		{
			CopyAllLinks();
		});
		copyAllItem.ShortcutKeyDisplayString = "Ctrl+Shift+C";
		dataGridViewContextMenu.Items.Add(copyAllItem);
		ToolStripMenuItem selectAllItem = new ToolStripMenuItem("全选", icoSelectAll, delegate
		{
			SelectAllRows();
		});
		selectAllItem.ShortcutKeyDisplayString = "Ctrl+A";
		dataGridViewContextMenu.Items.Add(selectAllItem);
		ToolStripMenuItem fixUrlMenuItem = new ToolStripMenuItem("修复直播源", icoFixUrl);
		ToolStripMenuItem fixSingleItem = new ToolStripMenuItem("修复当前直播源", null, delegate
		{
			if (dgvData.SelectedRows.Count > 0)
			{
				string text = dgvData.SelectedRows[0].Cells[1].Value?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					ShowReplaceUrlDialog(text);
				}
			}
			else
			{
				DarkMessageBox.Show("请先选中一条直播源！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		});
		fixUrlMenuItem.DropDownItems.Add(fixSingleItem);
		ToolStripMenuItem fixAllItem = new ToolStripMenuItem("一键全部修复", null, delegate
		{
			ReplaceAllUrls();
		});
		fixUrlMenuItem.DropDownItems.Add(fixAllItem);
		dataGridViewContextMenu.Items.Add(fixUrlMenuItem);
		dataGridViewContextMenu.Items.Add(new ToolStripSeparator());
		ToolStripMenuItem renameItem = new ToolStripMenuItem("重命名", icoRename, delegate
		{
			BeginRenameSelected();
		});
		renameItem.ShortcutKeyDisplayString = "F2";
		dataGridViewContextMenu.Items.Add(renameItem);
		ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除此行", icoDelete, delegate
		{
			DeleteRow();
		});
		deleteItem.ShortcutKeyDisplayString = "Del";
		dataGridViewContextMenu.Items.Add(deleteItem);
		ToolStripMenuItem detailItem = new ToolStripMenuItem("查看详情", icoInfo, delegate
		{
			ViewDetails();
		});
		detailItem.ShortcutKeyDisplayString = "Enter";
		dataGridViewContextMenu.Items.Add(detailItem);
		dataGridViewContextMenu.Items.Add(new ToolStripSeparator());
		ToolStripMenuItem clearInvalidItem = new ToolStripMenuItem("清空无效链接", icoClearInv, delegate
		{
			ClearInvalidLinks();
		});
		dataGridViewContextMenu.Items.Add(clearInvalidItem);
		ToolStripMenuItem clearAllItem = new ToolStripMenuItem("清空所有列表", icoClearAll, delegate
		{
			ClearAllLinks();
		});
		dataGridViewContextMenu.Items.Add(clearAllItem);
		dgvData.ContextMenuStrip = dataGridViewContextMenu;
		base.Shown += delegate
		{
			CenterEmptyState();
			int num = ((cboGroupHost != null && cboGroupHost.Visible) ? 328 : 15);
			int num2 = 98;
			searchBoxHost.Left = num2;
			searchBoxHost.Width = searchPanelRef.ClientSize.Width - num2 - num;
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
			ApplyColumnWidthsManual();
			UpdateGridScrollBar();
			Func<Task> refreshTask = async delegate
			{
				try
				{
					await Task.Delay(100);
					ApplyColumnWidthsManual();
					UpdateGridScrollBar();
					dgvData.PerformLayout();
					dgvData.Invalidate();
				}
				catch (Exception)
				{
				}
			};
			BeginInvoke((Action)delegate
			{
				refreshTask();
			});
		};
		base.FormClosing += delegate
		{
			try
			{
				StopMouseHook();
			}
			catch
			{
			}
			try
			{
				_ffplayOutputCts?.Cancel();
			}
			catch
			{
			}
			try
			{
				_ffplayOutputCts?.Dispose();
			}
			catch
			{
			}
			_ffplayOutputCts = null;
			KillRunningPlayer();
			StopPreview();
			if (persistList)
			{
				SaveChannelList();
			}
			SaveConfig();
			HttpClient obj5 = httpClient;
			if (obj5 != null)
			{
				((HttpMessageInvoker)obj5).Dispose();
			}
			CleanupCaches();
		};
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
		if (btnParseLink != null)
		{
			btnParseLink.Visible = false;
		}
		void CleanupCaches()
		{
			if (ipLocationCache.Count > 1000)
			{
				foreach (string key in ipLocationCache.Keys.Take(ipLocationCache.Count - 1000).ToList())
				{
					ipLocationCache.Remove(key);
				}
			}
			if (domainIpCache.Count > 1000)
			{
				foreach (string key2 in domainIpCache.Keys.Take(domainIpCache.Count - 1000).ToList())
				{
					domainIpCache.Remove(key2);
				}
			}
			if (ipLocationFailed.Count > 1000)
			{
				foreach (string key3 in ipLocationFailed.Take(ipLocationFailed.Count - 1000).ToList())
				{
					ipLocationFailed.Remove(key3);
				}
			}
			if (domainIpFailed.Count > 1000)
			{
				foreach (string key4 in domainIpFailed.Take(domainIpFailed.Count - 1000).ToList())
				{
					domainIpFailed.Remove(key4);
				}
			}
		}
		Label MakeDetailRow(string key)
		{
			Label lbl = new Label
			{
				Font = GetFont(SF(9f)),
				ForeColor = theme.TextSecondary,
				AutoSize = true,
				Location = At(dx, dy),
				MaximumSize = new Size(SX(340), 0),
				TextAlign = ContentAlignment.TopLeft
			};
			dy += dRow;
			detailPanel.Controls.Add(lbl);
			return lbl;
		}
		Label MakeStatusRow(string caption)
		{
			Label lbl = new Label
			{
				Text = caption,
				Font = GetFont(SF(8f)),
				ForeColor = theme.TextSecondary,
				AutoSize = true,
				Location = At(SX(10), lsy),
				MaximumSize = new Size(SX(340), 0),
				TextAlign = ContentAlignment.TopLeft
			};
			lsy += lsRow;
			linkStatusPanel.Controls.Add(lbl);
			return lbl;
		}
		void TryUpdateStreamInfoFromVlc()
		{
			if (channelPlayer == null)
			{
				return;
			}
			try
			{
				if (channelPlayer.TryGetStreamInfo(out var vlcCodec, out var vlcResolution, out var vlcFps, out var vlcBitrate, out var vlcChannels, out var vlcSampleRate))
				{
					if (string.IsNullOrEmpty(_currentCodec))
					{
						_currentCodec = vlcCodec;
					}
					if (string.IsNullOrEmpty(_currentResolution))
					{
						_currentResolution = vlcResolution;
					}
					if (string.IsNullOrEmpty(_currentFps) && !string.IsNullOrEmpty(vlcFps))
					{
						_currentFps = vlcFps;
					}
					if (string.IsNullOrEmpty(_currentBitrate))
					{
						_currentBitrate = vlcBitrate;
					}
					if (string.IsNullOrEmpty(_currentAudioChannels))
					{
						_currentAudioChannels = vlcChannels;
					}
					if (string.IsNullOrEmpty(_currentAudioSampleRate))
					{
						_currentAudioSampleRate = vlcSampleRate;
					}
				}
				if (string.IsNullOrEmpty(_currentSpeed) && channelPlayer.TryGetRate(out var rate) && rate > 0f)
				{
					_currentSpeed = $"{rate:F2}x";
				}
			}
			catch
			{
			}
		}
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
		static void UpdateSearchBoxRegion()
		{
		}
		static string V(string v)
		{
			if (!string.IsNullOrWhiteSpace(v))
			{
				return v.Trim();
			}
			return "—";
		}
	}

	private void EmptyIcon_Paint(object sender, PaintEventArgs pe)
	{
		Graphics g = pe.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		int num = pe.ClipRectangle.Width;
		int h = pe.ClipRectangle.Height;
		int size = Math.Min(num, h) - 8;
		int x = (num - size) / 2;
		int y = (h - size) / 2;
		using Pen xPen = new Pen(Color.FromArgb(220, 80, 80), 4f);
		xPen.StartCap = LineCap.Round;
		xPen.EndCap = LineCap.Round;
		int pad = size / 4;
		g.DrawLine(xPen, x + pad, y + pad, x + size - pad, y + size - pad);
		g.DrawLine(xPen, x + size - pad, y + pad, x + pad, y + size - pad);
	}

	private void SelectNavItem(string name)
	{
		Color textColor = (DrawingUtils.IsDarkColor(theme.Bg) ? Color.White : Color.Black);
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
		bool isDetect = name == "检测";
		if (statusBarRef != null)
		{
			statusBarRef.Visible = isDetect;
		}
		if (searchPanelRef != null)
		{
			searchPanelRef.Visible = isDetect;
		}
		if (gridContainerRef != null)
		{
			gridContainerRef.Visible = isDetect;
		}
		if (actionArea != null)
		{
			actionArea.Visible = isDetect;
		}
		if (actionSepRef != null)
		{
			actionSepRef.Visible = isDetect;
		}
		if (actionArea != null)
		{
			foreach (Control c in actionArea.Controls)
			{
				if (c != btnStartDetect && c != btnStopDetect && c != btnExport)
				{
					c.Visible = isDetect;
				}
			}
		}
		mainArea.PerformLayout();
		UpdateScrollBarTheme(mainArea);
	}

	private void LayoutFillPanel(Panel p)
	{
		if (p != null && mainArea != null)
		{
			Rectangle r = mainArea.ClientRectangle;
			p.Bounds = r;
			p.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		}
	}

	private void ForceCreateChildHandles(Control parent)
	{
		if (parent == null)
		{
			return;
		}
		_ = parent.Handle;
		foreach (Control c in parent.Controls)
		{
			ForceCreateChildHandles(c);
		}
	}

	private void UpdateScrollBarTheme(Control parent)
	{
		if (parent == null)
		{
			return;
		}
		int num;
		object obj;
		if (theme != null)
		{
			num = (DrawingUtils.IsDarkColor(theme.Bg) ? 1 : 0);
			if (num != 0)
			{
				obj = "DarkMode_Explorer";
				goto IL_002f;
			}
		}
		else
		{
			num = 0;
		}
		obj = null;
		goto IL_002f;
		IL_002f:
		string themeName = (string)obj;
		int darkMode = ((num != 0) ? 1 : 0);
		foreach (Control c in parent.Controls)
		{
			if (c is ScrollableControl || c is Panel || c is DataGridView || c is TextBox || c is TabControl || c is DarkTabControl || c is ListBox)
			{
				try
				{
					SetWindowTheme(c.Handle, themeName, null);
				}
				catch
				{
				}
				try
				{
					DwmSetWindowAttribute(c.Handle, 20, ref darkMode, 4);
					DwmSetWindowAttribute(c.Handle, 19, ref darkMode, 4);
				}
				catch
				{
				}
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
							DwmSetWindowAttribute(child.Handle, 20, ref darkMode, 4);
							DwmSetWindowAttribute(child.Handle, 19, ref darkMode, 4);
						}
					}
				}
				catch
				{
				}
			}
			if (c is TextBox tb)
			{
				try
				{
					foreach (Control child2 in tb.Controls)
					{
						if (child2 is VScrollBar || child2 is HScrollBar)
						{
							SetWindowTheme(child2.Handle, themeName, null);
							DwmSetWindowAttribute(child2.Handle, 20, ref darkMode, 4);
							DwmSetWindowAttribute(child2.Handle, 19, ref darkMode, 4);
						}
					}
				}
				catch
				{
				}
			}
			if (c is ListBox lb)
			{
				try
				{
					foreach (Control child3 in lb.Controls)
					{
						if (child3 is VScrollBar || child3 is HScrollBar)
						{
							SetWindowTheme(child3.Handle, themeName, null);
							DwmSetWindowAttribute(child3.Handle, 20, ref darkMode, 4);
							DwmSetWindowAttribute(child3.Handle, 19, ref darkMode, 4);
						}
					}
				}
				catch
				{
				}
			}
			if (c is VScrollBar || c is HScrollBar)
			{
				try
				{
					SetWindowTheme(c.Handle, themeName, null);
				}
				catch
				{
				}
				try
				{
					DwmSetWindowAttribute(c.Handle, 20, ref darkMode, 4);
					DwmSetWindowAttribute(c.Handle, 19, ref darkMode, 4);
				}
				catch
				{
				}
			}
			UpdateScrollBarTheme(c);
		}
	}

	private Panel CreateTextBoxWithDarkScrollBar(bool dark, Color foreColor, out TextBox textBox, Font font, bool readOnly = true, bool wordWrap = false, bool acceptsReturn = false)
	{
		Color bg = (dark ? Color.FromArgb(30, 30, 38) : Color.White);
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = bg,
			Padding = Padding.Empty
		};
		textBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Font = font,
			BorderStyle = BorderStyle.None,
			Multiline = true,
			WordWrap = wordWrap,
			AcceptsReturn = acceptsReturn,
			ScrollBars = ScrollBars.None,
			ReadOnly = readOnly,
			BackColor = bg,
			ForeColor = foreColor
		};
		obj.Controls.Add(textBox);
		DarkScrollBar scroll = new DarkScrollBar
		{
			Dock = DockStyle.Right,
			Width = SystemInformation.VerticalScrollBarWidth,
			Visible = false,
			TrackColor = (dark ? Color.FromArgb(45, 45, 55) : Color.FromArgb(240, 240, 240)),
			ThumbColor = (dark ? Color.FromArgb(120, 120, 130) : Color.FromArgb(180, 180, 180)),
			ThumbHoverColor = (dark ? Color.FromArgb(150, 150, 160) : Color.FromArgb(200, 200, 200)),
			ThumbPressedColor = (dark ? Color.FromArgb(170, 170, 180) : Color.FromArgb(220, 220, 220)),
			SmallChange = 1
		};
		obj.Controls.Add(scroll);
		scroll.BringToFront();
		TextBox tb = textBox;
		scroll.ValueChanged += delegate
		{
			UpdateTextBox();
		};
		tb.TextChanged += delegate
		{
			UpdateScrollBar();
		};
		tb.SizeChanged += delegate
		{
			UpdateScrollBar();
		};
		tb.FontChanged += delegate
		{
			UpdateScrollBar();
		};
		tb.HandleCreated += delegate
		{
			UpdateScrollBar();
		};
		tb.GotFocus += delegate
		{
			UpdateScrollBar();
		};
		tb.KeyUp += delegate
		{
			UpdateScrollBar();
		};
		tb.MouseWheel += delegate(object s, MouseEventArgs e)
		{
			int num = e.Delta / SystemInformation.MouseWheelScrollDelta;
			int num2 = Math.Max(0, Math.Min(scroll.Maximum, scroll.Value - num * scroll.SmallChange));
			if (num2 != scroll.Value)
			{
				scroll.Value = num2;
			}
		};
		obj.MouseWheel += delegate(object s, MouseEventArgs e)
		{
			int num = e.Delta / SystemInformation.MouseWheelScrollDelta;
			int num2 = Math.Max(0, Math.Min(scroll.Maximum, scroll.Value - num * scroll.SmallChange));
			if (num2 != scroll.Value)
			{
				scroll.Value = num2;
			}
		};
		EventHandler idle = delegate
		{
			if (!tb.IsDisposed)
			{
				UpdateScrollBar();
			}
		};
		Application.Idle += idle;
		tb.Disposed += delegate
		{
			Application.Idle -= idle;
		};
		return obj;
		int GetFirstVisibleLine()
		{
			if (!tb.IsHandleCreated)
			{
				return 0;
			}
			return SendMessage(tb.Handle, 206, 0, 0);
		}
		int GetLineCount()
		{
			if (!tb.IsHandleCreated)
			{
				return 1;
			}
			return SendMessage(tb.Handle, 186, 0, 0);
		}
		int GetVisibleLines()
		{
			return Math.Max(1, tb.ClientSize.Height / Math.Max(1, tb.Font.Height));
		}
		void UpdateScrollBar()
		{
			if (!tb.IsDisposed && !scroll.IsDisposed && tb.IsHandleCreated)
			{
				int total = GetLineCount();
				int visible = GetVisibleLines();
				int max = Math.Max(0, total - visible);
				if (scroll.Maximum != max)
				{
					scroll.Maximum = max;
				}
				if (scroll.LargeChange != visible)
				{
					scroll.LargeChange = visible;
				}
				int first = GetFirstVisibleLine();
				if (scroll.Value != first)
				{
					scroll.Value = first;
				}
				scroll.Visible = max > 0;
			}
		}
		void UpdateTextBox()
		{
			if (!tb.IsDisposed && !scroll.IsDisposed && tb.IsHandleCreated)
			{
				int first = GetFirstVisibleLine();
				int delta = scroll.Value - first;
				if (delta != 0)
				{
					SendMessage(tb.Handle, 182, 0, delta);
				}
			}
		}
	}

	private Panel CreateListBoxWithDarkScrollBar(bool dark, Color foreColor, out ListBox listBox, Font font)
	{
		Color bg = (dark ? Color.FromArgb(30, 30, 38) : Color.White);
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = bg,
			Padding = Padding.Empty
		};
		listBox = new ListBox
		{
			Dock = DockStyle.Fill,
			Font = font,
			BorderStyle = BorderStyle.None,
			BackColor = bg,
			ForeColor = foreColor,
			HorizontalScrollbar = false,
			IntegralHeight = false
		};
		obj.Controls.Add(listBox);
		DarkScrollBar scroll = new DarkScrollBar
		{
			Dock = DockStyle.Right,
			Width = SystemInformation.VerticalScrollBarWidth,
			Visible = false,
			TrackColor = (dark ? Color.FromArgb(45, 45, 55) : Color.FromArgb(240, 240, 240)),
			ThumbColor = (dark ? Color.FromArgb(120, 120, 130) : Color.FromArgb(180, 180, 180)),
			ThumbHoverColor = (dark ? Color.FromArgb(150, 150, 160) : Color.FromArgb(200, 200, 200)),
			ThumbPressedColor = (dark ? Color.FromArgb(170, 170, 180) : Color.FromArgb(220, 220, 220)),
			SmallChange = 1
		};
		obj.Controls.Add(scroll);
		scroll.BringToFront();
		ListBox lb = listBox;
		scroll.ValueChanged += delegate
		{
			UpdateListBox();
		};
		lb.SizeChanged += delegate
		{
			UpdateScrollBar();
		};
		lb.HandleCreated += delegate
		{
			UpdateScrollBar();
		};
		lb.GotFocus += delegate
		{
			UpdateScrollBar();
		};
		lb.KeyUp += delegate
		{
			UpdateScrollBar();
		};
		lb.SelectedIndexChanged += delegate
		{
			UpdateScrollBar();
		};
		lb.Click += delegate
		{
			UpdateScrollBar();
		};
		lb.MouseWheel += delegate(object s, MouseEventArgs e)
		{
			int num = e.Delta / SystemInformation.MouseWheelScrollDelta;
			int num2 = Math.Max(0, Math.Min(scroll.Maximum, lb.TopIndex - num * scroll.SmallChange));
			if (num2 != scroll.Value)
			{
				scroll.Value = num2;
			}
		};
		EventHandler idle = delegate
		{
			if (!lb.IsDisposed)
			{
				UpdateScrollBar();
			}
		};
		Application.Idle += idle;
		lb.Disposed += delegate
		{
			Application.Idle -= idle;
		};
		return obj;
		void HideNativeScrollBar()
		{
			if (lb.IsHandleCreated)
			{
				ShowScrollBar(lb.Handle, 1, bShow: false);
			}
		}
		void UpdateListBox()
		{
			if (!lb.IsDisposed && !scroll.IsDisposed && lb.TopIndex != scroll.Value)
			{
				lb.TopIndex = scroll.Value;
			}
		}
		void UpdateScrollBar()
		{
			if (!lb.IsDisposed && !scroll.IsDisposed)
			{
				HideNativeScrollBar();
				if (lb.IsHandleCreated)
				{
					int visible = Math.Max(1, lb.ClientSize.Height / Math.Max(1, lb.ItemHeight));
					int max = Math.Max(0, lb.Items.Count - visible);
					if (scroll.Maximum != max)
					{
						scroll.Maximum = max;
					}
					if (scroll.LargeChange != visible)
					{
						scroll.LargeChange = visible;
					}
					if (scroll.Value != lb.TopIndex)
					{
						scroll.Value = lb.TopIndex;
					}
					scroll.Visible = max > 0;
				}
			}
		}
	}

	private void OwnerDrawComboBox(ComboBox combo, bool dark, Color borderColor, Color backColor, Color foreColor)
	{
		if (combo == null)
		{
			return;
		}
		combo.FlatStyle = FlatStyle.Flat;
		combo.BackColor = backColor;
		combo.ForeColor = foreColor;
		combo.DrawMode = DrawMode.OwnerDrawFixed;
		combo.ItemHeight = combo.Font.Height + 4;
		combo.HandleCreated += delegate
		{
			try
			{
				SetWindowTheme(combo.Handle, dark ? "DarkMode_Explorer" : null, null);
			}
			catch
			{
			}
		};
		combo.DrawItem += delegate(object s, DrawItemEventArgs e)
		{
			e.DrawBackground();
			if (e.Index >= 0)
			{
				bool flag = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
				bool flag2 = (e.State & DrawItemState.Focus) == DrawItemState.Focus;
				using (SolidBrush brush = new SolidBrush((!flag) ? backColor : (dark ? Color.FromArgb(80, 80, 90) : SystemColors.Highlight)))
				{
					e.Graphics.FillRectangle(brush, e.Bounds);
				}
				string text = combo.Items[e.Index].ToString();
				Color foreColor2 = ((!flag) ? foreColor : (dark ? Color.White : SystemColors.HighlightText));
				TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, foreColor2, TextFormatFlags.VerticalCenter);
				if (flag2)
				{
					e.DrawFocusRectangle();
				}
			}
		};
		combo.Paint += delegate(object s, PaintEventArgs e)
		{
			ComboBox comboBox = (ComboBox)s;
			using Pen pen = new Pen(borderColor);
			e.Graphics.DrawRectangle(pen, 0, 0, comboBox.Width - 1, comboBox.Height - 1);
		};
	}

	private void CenterEmptyState()
	{
		if (emptyStatePanel == null || emptyLabel == null || dgvData?.Parent == null)
		{
			return;
		}
		int w = dgvData.Parent.ClientSize.Width;
		int h = dgvData.Parent.ClientSize.Height;
		int pw = SX(140);
		int ph = SY(110);
		emptyStatePanel.Location = At((w - pw) / 2, (h - ph) / 2);
		emptyStatePanel.Size = new Size(pw, ph);
		foreach (Control c in emptyStatePanel.Controls)
		{
			if (c is PictureBox)
			{
				c.Location = At((pw - c.Width) / 2, 0);
			}
			else if (c is Label lbl)
			{
				lbl.Location = At((pw - lbl.Width) / 2, SY(66));
			}
		}
	}

	private void UpdateEmptyState()
	{
		if (emptyStatePanel != null && dgvData != null)
		{
			emptyStatePanel.Visible = dgvData.Rows.Count == 0;
			if (emptyStatePanel.Visible)
			{
				emptyStatePanel.BringToFront();
			}
		}
	}

	private void ApplyColumnWidthsManual()
	{
		if (dgvData == null || dgvData.IsDisposed || !dgvData.IsHandleCreated)
		{
			return;
		}
		int availableW = dgvData.DisplayRectangle.Width;
		if (availableW <= 0)
		{
			return;
		}
		double totalWeight = 0.0;
		foreach (DataGridViewColumn col in dgvData.Columns)
		{
			totalWeight += (double)col.FillWeight;
		}
		if (totalWeight <= 0.0)
		{
			return;
		}
		int totalMinW = 0;
		foreach (DataGridViewColumn col2 in dgvData.Columns)
		{
			totalMinW += col2.MinimumWidth;
		}
		dgvData.SuspendLayout();
		try
		{
			if (totalMinW >= availableW)
			{
				foreach (DataGridViewColumn column in dgvData.Columns)
				{
					column.Width = column.MinimumWidth;
				}
				return;
			}
			int extra = availableW - totalMinW;
			int distributed = 0;
			int colCount = dgvData.Columns.Count;
			for (int i = 0; i < colCount; i++)
			{
				DataGridViewColumn col3 = dgvData.Columns[i];
				int w = col3.MinimumWidth;
				if (i < colCount - 1)
				{
					int add = (int)((double)((float)extra * col3.FillWeight) / totalWeight);
					w += add;
					distributed += add;
				}
				else
				{
					w += extra - distributed;
				}
				col3.Width = w;
			}
		}
		finally
		{
			dgvData.ResumeLayout();
		}
	}

	private void AutoFitButtonWidth(Button b, int minW)
	{
		if (b != null && b.IsHandleCreated && b.Font != null)
		{
			int w = TextRenderer.MeasureText(b.Text ?? "", b.Font, new Size(int.MaxValue, b.Height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + SX(20);
			if (w < minW)
			{
				w = minW;
			}
			if (b.Width != w)
			{
				b.Width = w;
			}
		}
	}

	private void UpdateActionButtonsVisibility()
	{
		bool hasData = allChannels != null && allChannels.Count > 0;
		bool canShowParseLink = allChannels != null && allChannels.Any((ChannelInfo c) => c.Group == "解析待处理" && c.Status == "待解析") && hasSearchPlatformData && !autoParseLink;
		if (btnStartDetect != null)
		{
			btnStartDetect.Visible = hasData;
		}
		if (btnStopDetect != null)
		{
			btnStopDetect.Visible = hasData;
		}
		if (btnExport != null)
		{
			btnExport.Visible = hasData;
		}
		if (btnParseLink != null)
		{
			btnParseLink.Visible = canShowParseLink;
		}
		AutoFitButtonWidth(btnTbImport, SX(72));
		AutoFitButtonWidth(btnStartDetect, SX(72));
		AutoFitButtonWidth(btnStopDetect, SX(64));
		AutoFitButtonWidth(btnExport, SX(80));
		AutoFitButtonWidth(btnScanSource, SX(80));
		if (importHost == null)
		{
			return;
		}
		Button[] obj = new Button[5] { btnTbImport, btnStartDetect, btnStopDetect, btnExport, btnScanSource };
		int x = SX(10);
		int maxRight = 0;
		Button[] array = obj;
		foreach (Button b in array)
		{
			if (b != null && b.Visible)
			{
				b.Location = At(x, SY(6));
				x = b.Right + SX(8);
				maxRight = b.Right;
			}
		}
		if (maxRight > 0)
		{
			importHost.Width = maxRight + SX(10);
		}
	}

	private void UpdateTipBoxSize()
	{
		if (tipBox == null)
		{
			return;
		}
		string tipText = "1. 列表位置，点击右键发现更多功能\r\n2. 双击名称，重命名，双击链接，修复直播源。\r\n3. 打开设置发现更多功能。";
		Font tipContentFont = GetFont(9f);
		int tipW = tipBox.Width;
		SizeF tipTextSize;
		using (Graphics g = CreateGraphics())
		{
			tipTextSize = g.MeasureString(tipText, tipContentFont, tipW - 24);
		}
		int tipContentHeight = (int)Math.Ceiling(tipTextSize.Height);
		int tipBoxHeight = 38 + tipContentHeight + 10;
		tipBox.Size = new Size(tipW, tipBoxHeight);
		foreach (Control ctrl in tipBox.Controls)
		{
			if (ctrl is Label label && !ctrl.Text.Equals("提示"))
			{
				label.Size = new Size(tipW - 24, tipContentHeight);
			}
		}
	}

	private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
	{
		return DrawingUtils.RoundedRect(rect, radius);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
            this.SuspendLayout();
            // 
            // IPTVLiveCheckerMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1275, 736);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "IPTVLiveCheckerMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "IPTV直播源检测工具";
            this.Activated += new System.EventHandler(this.IPTVLiveCheckerMain_Activated);
            this.Load += new System.EventHandler(this.IPTVLiveCheckerMain_Load);
            this.ResumeLayout(false);

	}

	private void IPTVLiveCheckerMain_Activated(object sender, EventArgs e)
	{
		if (_isRestoringFromMinimize && WindowState != FormWindowState.Minimized)
		{
			_isRestoringFromMinimize = false;
			Invalidate();
			Update();
		}
	}
}
