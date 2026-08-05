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
	// ===== 内置主题注册表（精简为 2 个基调：浅色青瓷薄荷 + 深色深空墨蓝；其余动效主题由外部 themes/*.json 提供） =====

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

	// ===== 字体自定义（可选；留空则使用主程序全局字体设置）=====
	/// <summary>字体族名称（如 "Microsoft YaHei", "Segoe UI"）。留空则继承用户设置。</summary>
	public string FontFamily { get; set; }

	/// <summary>基础字号缩放因子（1.0 = 默认大小，1.1 = 放大 10%）。仅当 FontFamily 非空时生效。</summary>
	public double FontScale { get; set; } = 1.0;

	// ===== 布局参数（可选；0 或负值表示使用主程序默认值）=====
	/// <summary>圆角半径（px，DPI 缩放前）。0 = 直角，6 = 默认圆角，-1 = 使用默认。</summary>
	public int CornerRadius { get; set; } = -1;

	/// <summary>控件间距缩放因子（1.0 = 默认间距）。用于微调按钮/面板之间的留白。</summary>
	public double SpacingScale { get; set; } = 1.0;

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

	// ===== "跟随系统" 解析 =====
	// 选择"跟随系统"时，按系统明暗自动回退到内置 Light/Dark 主题（外部主题已移除）。
	/// <summary>
	/// 解析"跟随系统"时应生效的主题：按系统明暗回退内置 Light/Dark。
	/// </summary>
	public static AppTheme ResolveFollowSystem(string defaultLightName)
	{
		return GetAutoTheme();
	}

	// 当前实际生效的主题（由 ApplyTheme 写入），供静态样式方法（如 StyleRoundButton）在无法访问实例字段时读取。
	private static AppTheme _activeTheme;
	public static AppTheme ActiveTheme
	{
		get => _activeTheme ??= GetAutoTheme();
		set => _activeTheme = value;
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
		MintCeladon, DeepInk
	};

	/// <summary>内置主题只读列表（不含"跟随系统"）。</summary>
	public static IReadOnlyList<AppTheme> Builtins => _builtinThemes;

	private static readonly HashSet<string> BuiltinThemeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"青瓷薄荷", "深空墨蓝", "跟随系统"
	};

	// ===== 外部主题库（已移除）=====
	// 外部主题（JSON / 嵌入 DLL / 派生 DLL）已全部删除，仅保留内置主题。
	// 保留空字典与接口，避免调用方改动；ExternalThemes 恒为空。
	private static readonly Dictionary<string, AppTheme> _externalThemes = new Dictionary<string, AppTheme>(StringComparer.OrdinalIgnoreCase);
	private static bool _externalThemesLoaded;

	public static IReadOnlyDictionary<string, AppTheme> ExternalThemes => _externalThemes;

	public static bool IsExternalThemesLoaded()
	{
		return _externalThemesLoaded;
	}

	/// <summary>
	/// 加载外部主题（已移除）。保留为 no-op 以维持调用点兼容；ExternalThemes 始终为空。
	/// </summary>
	public static void LoadExternalThemes()
	{
		_externalThemes.Clear();
		_externalThemesLoaded = true;
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
