using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace IPTVLiveChecker;

public class AppTheme
{
	// ===== 高对比度护眼主题（柔和低眩光背景 + 强可读性文字） =====

	public static AppTheme WarmCream = new AppTheme
	{
		Name = "暖玉米白",
		Primary = Color.FromArgb(184, 134, 48),
		PrimaryDark = Color.FromArgb(160, 116, 40),
		Accent = Color.FromArgb(196, 72, 96),
		Bg = Color.FromArgb(245, 238, 215),
		BgAlt = Color.FromArgb(238, 229, 203),
		Surface = Color.FromArgb(232, 222, 195),
		Border = Color.FromArgb(206, 194, 165),
		TextPrimary = Color.FromArgb(40, 36, 30),
		TextSecondary = Color.FromArgb(102, 94, 80),
		HeaderBg = Color.FromArgb(236, 226, 200),
		SelectRow = Color.FromArgb(222, 211, 183),
		SelectRowText = Color.FromArgb(40, 36, 30),
		StatusBarBg = Color.FromArgb(226, 215, 188),

		TipBg = Color.FromArgb(232, 222, 195),
		PlayBtnBg = Color.FromArgb(60, 160, 90),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(60, 130, 195),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(228, 218, 192),
		StatusTagBorder = Color.FromArgb(60, 140, 80),
		LinkTextColor = Color.FromArgb(40, 90, 150),
		SuccessColor = Color.FromArgb(50, 150, 80),
		ErrorColor = Color.FromArgb(200, 60, 60),
		WarnColor = Color.FromArgb(200, 140, 40),
		InfoColor = Color.FromArgb(50, 120, 195)
	};

	public static AppTheme MintCeladon = new AppTheme
	{
		Name = "青瓷薄荷",
		Primary = Color.FromArgb(40, 145, 105),
		PrimaryDark = Color.FromArgb(32, 122, 88),
		Accent = Color.FromArgb(200, 78, 120),
		Bg = Color.FromArgb(223, 240, 230),
		BgAlt = Color.FromArgb(212, 232, 220),
		Surface = Color.FromArgb(205, 227, 214),
		Border = Color.FromArgb(180, 210, 194),
		TextPrimary = Color.FromArgb(26, 52, 44),
		TextSecondary = Color.FromArgb(90, 116, 106),
		HeaderBg = Color.FromArgb(214, 233, 222),
		SelectRow = Color.FromArgb(195, 220, 206),
		SelectRowText = Color.FromArgb(26, 52, 44),
		StatusBarBg = Color.FromArgb(206, 228, 216),

		TipBg = Color.FromArgb(205, 227, 214),
		PlayBtnBg = Color.FromArgb(40, 150, 100),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(55, 130, 195),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(200, 224, 210),
		StatusTagBorder = Color.FromArgb(40, 140, 95),
		LinkTextColor = Color.FromArgb(35, 95, 150),
		SuccessColor = Color.FromArgb(40, 150, 95),
		ErrorColor = Color.FromArgb(200, 65, 65),
		WarnColor = Color.FromArgb(200, 145, 45),
		InfoColor = Color.FromArgb(55, 120, 195)
	};

	public static AppTheme MistyBlue = new AppTheme
	{
		Name = "淡雾蓝",
		Primary = Color.FromArgb(45, 115, 175),
		PrimaryDark = Color.FromArgb(38, 98, 150),
		Accent = Color.FromArgb(180, 72, 160),
		Bg = Color.FromArgb(223, 232, 244),
		BgAlt = Color.FromArgb(212, 223, 238),
		Surface = Color.FromArgb(205, 217, 234),
		Border = Color.FromArgb(178, 193, 214),
		TextPrimary = Color.FromArgb(28, 42, 60),
		TextSecondary = Color.FromArgb(88, 104, 124),
		HeaderBg = Color.FromArgb(214, 225, 240),
		SelectRow = Color.FromArgb(196, 209, 228),
		SelectRowText = Color.FromArgb(28, 42, 60),
		StatusBarBg = Color.FromArgb(206, 218, 236),

		TipBg = Color.FromArgb(205, 217, 234),
		PlayBtnBg = Color.FromArgb(45, 150, 100),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(45, 125, 190),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(200, 213, 232),
		StatusTagBorder = Color.FromArgb(45, 140, 95),
		LinkTextColor = Color.FromArgb(35, 90, 160),
		SuccessColor = Color.FromArgb(40, 150, 95),
		ErrorColor = Color.FromArgb(200, 65, 65),
		WarnColor = Color.FromArgb(200, 145, 45),
		InfoColor = Color.FromArgb(45, 115, 180)
	};

	public static AppTheme SoftRose = new AppTheme
	{
		Name = "藕粉柔光",
		Primary = Color.FromArgb(186, 96, 124),
		PrimaryDark = Color.FromArgb(162, 80, 108),
		Accent = Color.FromArgb(200, 110, 60),
		Bg = Color.FromArgb(246, 228, 230),
		BgAlt = Color.FromArgb(239, 219, 222),
		Surface = Color.FromArgb(233, 212, 215),
		Border = Color.FromArgb(208, 184, 188),
		TextPrimary = Color.FromArgb(54, 36, 42),
		TextSecondary = Color.FromArgb(116, 96, 102),
		HeaderBg = Color.FromArgb(240, 221, 224),
		SelectRow = Color.FromArgb(226, 204, 208),
		SelectRowText = Color.FromArgb(54, 36, 42),
		StatusBarBg = Color.FromArgb(232, 211, 214),

		TipBg = Color.FromArgb(233, 212, 215),
		PlayBtnBg = Color.FromArgb(60, 155, 100),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(60, 130, 195),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(230, 208, 212),
		StatusTagBorder = Color.FromArgb(60, 140, 85),
		LinkTextColor = Color.FromArgb(120, 70, 120),
		SuccessColor = Color.FromArgb(50, 150, 85),
		ErrorColor = Color.FromArgb(200, 65, 65),
		WarnColor = Color.FromArgb(200, 140, 50),
		InfoColor = Color.FromArgb(60, 125, 190)
	};

	public static AppTheme DeepInk = new AppTheme
	{
		Name = "深空墨蓝",
		Primary = Color.FromArgb(95, 165, 235),
		PrimaryDark = Color.FromArgb(78, 142, 210),
		Accent = Color.FromArgb(232, 142, 175),
		Bg = Color.FromArgb(24, 30, 43),
		BgAlt = Color.FromArgb(32, 39, 54),
		Surface = Color.FromArgb(38, 46, 63),
		Border = Color.FromArgb(60, 70, 90),
		TextPrimary = Color.FromArgb(230, 238, 250),
		TextSecondary = Color.FromArgb(165, 178, 198),
		HeaderBg = Color.FromArgb(32, 39, 54),
		SelectRow = Color.FromArgb(50, 62, 84),
		SelectRowText = Color.FromArgb(230, 238, 250),
		StatusBarBg = Color.FromArgb(32, 39, 54),

		TipBg = Color.FromArgb(32, 39, 54),
		PlayBtnBg = Color.FromArgb(60, 175, 120),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(60, 140, 205),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(40, 52, 66),
		StatusTagBorder = Color.FromArgb(60, 175, 120),
		LinkTextColor = Color.FromArgb(150, 200, 255),
		SuccessColor = Color.FromArgb(70, 195, 130),
		ErrorColor = Color.FromArgb(240, 100, 100),
		WarnColor = Color.FromArgb(240, 180, 60),
		InfoColor = Color.FromArgb(90, 175, 240)
	};

	public static AppTheme ForestNight = new AppTheme
	{
		Name = "墨绿夜",
		Primary = Color.FromArgb(95, 185, 145),
		PrimaryDark = Color.FromArgb(78, 160, 122),
		Accent = Color.FromArgb(232, 150, 175),
		Bg = Color.FromArgb(24, 39, 33),
		BgAlt = Color.FromArgb(32, 50, 42),
		Surface = Color.FromArgb(38, 58, 49),
		Border = Color.FromArgb(58, 82, 70),
		TextPrimary = Color.FromArgb(226, 240, 230),
		TextSecondary = Color.FromArgb(167, 190, 178),
		HeaderBg = Color.FromArgb(32, 50, 42),
		SelectRow = Color.FromArgb(52, 76, 64),
		SelectRowText = Color.FromArgb(226, 240, 230),
		StatusBarBg = Color.FromArgb(32, 50, 42),

		TipBg = Color.FromArgb(32, 50, 42),
		PlayBtnBg = Color.FromArgb(55, 175, 120),
		PlayBtnText = Color.White,
		CopyBtnBg = Color.FromArgb(60, 140, 200),
		CopyBtnText = Color.White,
		StatusTagBg = Color.FromArgb(40, 60, 51),
		StatusTagBorder = Color.FromArgb(55, 175, 120),
		LinkTextColor = Color.FromArgb(150, 210, 180),
		SuccessColor = Color.FromArgb(70, 195, 135),
		ErrorColor = Color.FromArgb(240, 105, 105),
		WarnColor = Color.FromArgb(240, 185, 65),
		InfoColor = Color.FromArgb(90, 180, 235)
	};

	public static AppTheme Light => MintCeladon;

	public static AppTheme Dark => DeepInk;

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

	// ===== 效果元数据（仅外部主题可选声明；内置主题默认关闭，向后兼容）=====
	public bool GlassEnabled { get; set; }

	public int GlassOpacity { get; set; } = 210;

	public bool GlassBlur { get; set; }

	public string AnimationType { get; set; } = "";

	public double AnimationSpeed { get; set; } = 1.0;

	public List<Color> GradientStops { get; set; }

	/// <summary>
	/// 派生主题可覆写此方法执行复杂初始化（派生颜色、读取资源、条件逻辑等）。
	/// 内置主题不需要覆写；外部 DLL 主题可在构造函数或此方法中设置全部字段。
	/// 加载器会在创建实例后调用一次。
	/// </summary>
	public virtual void Initialize()
	{
	}

	public static bool IsSystemDarkTheme()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			if (key != null)
			{
				object value = key.GetValue("AppsUseLightTheme");
				if (value != null && (int)value == 0)
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

	public static AppTheme GetAutoTheme()
	{
		if (!IsSystemDarkTheme())
		{
			return Light;
		}
		return Dark;
	}

	public AppTheme Clone()
	{
		return new AppTheme
		{
			Name = Name,
			Primary = Primary,
			PrimaryDark = PrimaryDark,
			Accent = Accent,
			Bg = Bg,
			BgAlt = BgAlt,
			Surface = Surface,
			Border = Border,
			TextPrimary = TextPrimary,
			TextSecondary = TextSecondary,
			HeaderBg = HeaderBg,
			SelectRow = SelectRow,
			SelectRowText = SelectRowText,
			StatusBarBg = StatusBarBg,
			TipBg = TipBg,
			PlayBtnBg = PlayBtnBg,
			PlayBtnText = PlayBtnText,
			CopyBtnBg = CopyBtnBg,
			CopyBtnText = CopyBtnText,
			StatusTagBg = StatusTagBg,
			StatusTagBorder = StatusTagBorder,
			LinkTextColor = LinkTextColor,
			SuccessColor = SuccessColor,
			ErrorColor = ErrorColor,
			WarnColor = WarnColor,
			InfoColor = InfoColor,
			GlassEnabled = GlassEnabled,
			GlassOpacity = GlassOpacity,
			GlassBlur = GlassBlur,
			AnimationType = AnimationType,
			AnimationSpeed = AnimationSpeed,
			GradientStops = (GradientStops == null ? null : new List<Color>(GradientStops))
		};
	}

	private bool IsDark()
	{
		return (0.299 * Bg.R + 0.587 * Bg.G + 0.114 * Bg.B) / 255.0 < 0.5;
	}

	public void ApplyHighContrast()
	{
		bool isDark = IsDark();
		Color baseBg = (isDark ? Color.Black : Color.White);
		Color baseFg = (isDark ? Color.White : Color.Black);
		Bg = baseBg;
		BgAlt = baseBg;
		Surface = (isDark ? Color.FromArgb(20, 20, 20) : Color.FromArgb(245, 245, 245));
		HeaderBg = (isDark ? Color.FromArgb(28, 28, 28) : Color.FromArgb(235, 235, 235));
		StatusBarBg = (isDark ? Color.FromArgb(35, 35, 35) : Color.FromArgb(220, 220, 220));
		TipBg = (isDark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(240, 240, 240));
		TextPrimary = baseFg;
		TextSecondary = (isDark ? Color.FromArgb(225, 225, 225) : Color.FromArgb(30, 30, 30));
		SelectRowText = baseFg;
		LinkTextColor = (isDark ? Color.FromArgb(120, 200, 255) : Color.FromArgb(0, 90, 200));
		Border = baseFg;
		SelectRow = (isDark ? Color.FromArgb(70, 70, 0) : Color.FromArgb(220, 210, 0));
		StatusTagBorder = (isDark ? Color.FromArgb(220, 220, 0) : Color.FromArgb(120, 120, 0));
		Primary = (isDark ? Color.FromArgb(180, 150, 255) : Color.FromArgb(90, 40, 200));
		PrimaryDark = Primary;
		Accent = (isDark ? Color.FromArgb(255, 150, 200) : Color.FromArgb(200, 0, 110));
		PlayBtnBg = (isDark ? Color.FromArgb(0, 200, 120) : Color.FromArgb(0, 150, 90));
		CopyBtnBg = (isDark ? Color.FromArgb(0, 150, 230) : Color.FromArgb(0, 100, 200));
		PlayBtnText = baseFg;
		CopyBtnText = baseFg;
		StatusTagBg = Surface;
		SuccessColor = (isDark ? Color.FromArgb(0, 230, 130) : Color.FromArgb(0, 150, 80));
		ErrorColor = (isDark ? Color.FromArgb(255, 90, 90) : Color.FromArgb(200, 0, 0));
		WarnColor = (isDark ? Color.FromArgb(255, 200, 0) : Color.FromArgb(180, 120, 0));
		InfoColor = (isDark ? Color.FromArgb(90, 190, 255) : Color.FromArgb(0, 110, 210));
	}

	// ===== 内置主题注册表（数据驱动，消除菜单与 switch 中的硬编码重复）=====
	private static readonly AppTheme[] _builtinThemes = new AppTheme[]
	{
		WarmCream, MintCeladon, MistyBlue, SoftRose, DeepInk, ForestNight
	};

	/// <summary>内置主题只读列表（不含"跟随系统"）。</summary>
	public static IReadOnlyList<AppTheme> Builtins => _builtinThemes;

	private static readonly HashSet<string> BuiltinThemeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"暖玉米白", "青瓷薄荷", "淡雾蓝", "藕粉柔光", "深空墨蓝", "墨绿夜", "跟随系统"
	};

	// ===== 外部主题库（JSON 文件，运行时从 exe 同级 themes/ 目录加载；缺失则回退内置主题）=====
	private static readonly Dictionary<string, AppTheme> _externalThemes = new Dictionary<string, AppTheme>(StringComparer.OrdinalIgnoreCase);
	private static bool _externalThemesLoaded;

	public static IReadOnlyDictionary<string, AppTheme> ExternalThemes => _externalThemes;

	public static bool IsExternalThemesLoaded()
	{
		return _externalThemesLoaded;
	}

	public static void LoadExternalThemes()
	{
		_externalThemes.Clear();
		_externalThemesLoaded = true;
		try
		{
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
			string dir = Path.Combine(baseDir, "themes");
			if (!Directory.Exists(dir))
			{
				return;
			}
			foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
			{
				try
				{
					AppTheme t = ParseThemeJson(File.ReadAllText(file));
					if (t != null && !string.IsNullOrWhiteSpace(t.Name) && !BuiltinThemeNames.Contains(t.Name.Trim()) && !_externalThemes.ContainsKey(t.Name.Trim()))
					{
						_externalThemes[t.Name.Trim()] = t;
					}
				}
				catch
				{
				}
			}
			// 同时加载 DLL 主题（继承 AppTheme 的派生类，支持复杂逻辑）
			LoadThemeDlls(baseDir, dir);
		}
		catch
		{
		}
	}

	/// <summary>
	/// 从 themes/*.dll 加载外部主题：扫描程序集中所有继承 AppTheme 的非抽象类型，
	/// 实例化并调用 Initialize()。与 JSON 主题并存，同名时先到先得。
	/// </summary>
	private static void LoadThemeDlls(string baseDir, string themesDir)
	{
		if (!Directory.Exists(themesDir))
		{
			return;
		}
		foreach (string dll in Directory.GetFiles(themesDir, "*.dll", SearchOption.TopDirectoryOnly))
		{
			try
			{
				Assembly asm = Assembly.LoadFrom(dll);
				foreach (Type t in asm.GetTypes())
				{
					if (t.IsAbstract || !typeof(AppTheme).IsAssignableFrom(t))
					{
						continue;
					}
					try
					{
						AppTheme theme = (AppTheme)Activator.CreateInstance(t);
						theme.Initialize();
						if (theme != null && !string.IsNullOrWhiteSpace(theme.Name)
							&& !BuiltinThemeNames.Contains(theme.Name.Trim())
							&& !_externalThemes.ContainsKey(theme.Name.Trim()))
						{
							_externalThemes[theme.Name.Trim()] = theme;
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
	}

	private static AppTheme ParseThemeJson(string json)
	{
		MatchCollection matches = Regex.Matches(json, "\"(?<k>[^\"]+)\"\\s*:\\s*\"(?<v>[^\"]*)\"");
		if (matches.Count == 0 && !Regex.IsMatch(json, "\"(GradientStops)\"\\s*:\\s*\\["))
		{
			return null;
		}
		AppTheme t = new AppTheme();
		PropertyInfo[] props = typeof(AppTheme).GetProperties();
		// 1) 带引号的字符串 / 颜色值
		foreach (Match m in matches)
		{
			string key = m.Groups["k"].Value;
			string val = m.Groups["v"].Value;
			PropertyInfo p = props.FirstOrDefault((PropertyInfo x) => string.Equals(x.Name, key, StringComparison.OrdinalIgnoreCase));
			if (p == null)
			{
				continue;
			}
			if (p.PropertyType == typeof(string))
			{
				p.SetValue(t, val);
			}
			else if (p.PropertyType == typeof(Color))
			{
				try
				{
					p.SetValue(t, ColorTranslator.FromHtml(val));
				}
				catch
				{
				}
			}
		}
		// 2) 布尔值
		foreach (Match m in Regex.Matches(json, "\"(?<k>[^\"]+)\"\\s*:\\s*(?<v>true|false)", RegexOptions.IgnoreCase))
		{
			PropertyInfo p = props.FirstOrDefault((PropertyInfo x) => string.Equals(x.Name, m.Groups["k"].Value, StringComparison.OrdinalIgnoreCase));
			if (p != null && p.PropertyType == typeof(bool))
			{
				try
				{
					p.SetValue(t, bool.Parse(m.Groups["v"].Value));
				}
				catch
				{
				}
			}
		}
		// 3) 数字（int / double）
		foreach (Match m in Regex.Matches(json, "\"(?<k>[^\"]+)\"\\s*:\\s*(?<v>-?\\d+(?:\\.\\d+)?)"))
		{
			PropertyInfo p = props.FirstOrDefault((PropertyInfo x) => string.Equals(x.Name, m.Groups["k"].Value, StringComparison.OrdinalIgnoreCase));
			if (p == null)
			{
				continue;
			}
			if (p.PropertyType == typeof(int))
			{
				try
				{
					p.SetValue(t, int.Parse(m.Groups["v"].Value));
				}
				catch
				{
				}
			}
			else if (p.PropertyType == typeof(double))
			{
				try
				{
					p.SetValue(t, double.Parse(m.Groups["v"].Value, System.Globalization.CultureInfo.InvariantCulture));
				}
				catch
				{
				}
			}
		}
		// 4) 颜色数组（渐变光斑）
		Match arr = Regex.Match(json, "\"(GradientStops)\"\\s*:\\s*\\[(?<v>[^\\]]*)\\]");
		if (arr.Success)
		{
			List<Color> stops = new List<Color>();
			foreach (Match cm in Regex.Matches(arr.Groups["v"].Value, "\"([^\"]+)\""))
			{
				try
				{
					stops.Add(ColorTranslator.FromHtml(cm.Groups[1].Value));
				}
				catch
				{
				}
			}
			if (stops.Count >= 2)
			{
				t.GradientStops = stops;
			}
		}
		return t;
	}
}
