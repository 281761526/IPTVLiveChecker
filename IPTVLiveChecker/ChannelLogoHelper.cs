using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IPTVLiveChecker;

/// <summary>
/// 频道名称清洗与台标（logo）兜底适配。
/// 导出 m3u/txt 时调用，便于统一管理频道并自动匹配台标链接。
/// </summary>
internal static class ChannelLogoHelper
{
	// 兜底台标源（按优先级排列）。数据来自 GitHub 仓库 fanmingming/live（tv/CCTV1.png 等）。
	// 原站 live.fanmingming.cn / .com 不稳定，改用国内加速/反代源作为主源，原站降为末尾兜底；
	// 另保留 x1ao4/tv-logos 作为跨仓库兜底。GetLogoCandidates 会依次返回这些候选 URL。
	private static readonly string[] LogoSources = new string[]
	{
		"https://cdn.jsdelivr.net/gh/fanmingming/live/tv/{0}.png",
		"https://raw.bgithub.xyz/fanmingming/live/main/tv/{0}.png",
		"https://ghproxy.net/https://raw.githubusercontent.com/fanmingming/live/main/tv/{0}.png",
		"https://raw.gitmirror.com/fanmingming/live/main/tv/{0}.png",
		"https://live.fanmingming.cn/tv/{0}.png",
		"https://live.fanmingming.com/tv/{0}.png",
		"https://raw.githubusercontent.com/x1ao4/tv-logos/main/tv-logos/{0}.png"
	};

	// 常见频道 -> 台标源文件名 token（精确适配；未覆盖的走清洗后的名称兜底）。
	// 卫视使用常见拼音命名，属于 best-effort，某一源 404 时由其余兜底源/清洗名称补充。
	private static readonly Dictionary<string, string> KnownLogoTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		// CCTV 系列（高置信度，覆盖“综合”等写法）
		{ "CCTV1", "CCTV1" }, { "CCTV1综合", "CCTV1" },
		{ "CCTV2", "CCTV2" }, { "CCTV2财经", "CCTV2" },
		{ "CCTV3", "CCTV3" }, { "CCTV3综艺", "CCTV3" },
		{ "CCTV4", "CCTV4" }, { "CCTV4中文国际", "CCTV4" },
		{ "CCTV5", "CCTV5" }, { "CCTV5体育", "CCTV5" },
		{ "CCTV5+", "CCTV5Plus" }, { "CCTV5plus", "CCTV5Plus" },
		{ "CCTV6", "CCTV6" }, { "CCTV6电影", "CCTV6" },
		{ "CCTV7", "CCTV7" }, { "CCTV7军事农业", "CCTV7" },
		{ "CCTV8", "CCTV8" }, { "CCTV8电视剧", "CCTV8" },
		{ "CCTV9", "CCTV9" }, { "CCTV9记录", "CCTV9" },
		{ "CCTV10", "CCTV10" }, { "CCTV10科教", "CCTV10" },
		{ "CCTV11", "CCTV11" }, { "CCTV11戏曲", "CCTV11" },
		{ "CCTV12", "CCTV12" }, { "CCTV12社会与法", "CCTV12" },
		{ "CCTV13", "CCTV13" }, { "CCTV13新闻", "CCTV13" },
		{ "CCTV14", "CCTV14" }, { "CCTV14少儿", "CCTV14" },
		{ "CCTV15", "CCTV15" }, { "CCTV15音乐", "CCTV15" },
		{ "CCTV16", "CCTV16" }, { "CCTV16奥林匹克", "CCTV16" },
		{ "CCTV17", "CCTV17" }, { "CCTV17农业农村", "CCTV17" },
		{ "CCTV4K", "CCTV4K" }, { "CCTV8K", "CCTV8K" },
		// 省级卫视（常见拼音命名，best-effort）
		{ "湖南卫视", "HunanTV" }, { "浙江卫视", "ZhejiangTV" }, { "北京卫视", "BeijingTV" },
		{ "广东卫视", "GuangdongTV" }, { "江苏卫视", "JiangsuTV" }, { "东方卫视", "DragonTV" },
		{ "山东卫视", "ShandongTV" }, { "四川卫视", "SichuanTV" }, { "天津卫视", "TianjinTV" },
		{ "湖北卫视", "HubeiTV" }, { "河南卫视", "HenanTV" }, { "河北卫视", "HebeiTV" },
		{ "安徽卫视", "AnhuiTV" }, { "辽宁卫视", "LiaoningTV" }, { "福建卫视", "FujianTV" },
		{ "深圳卫视", "ShenzhenTV" }, { "重庆卫视", "ChongqingTV" }, { "江西卫视", "JiangxiTV" },
		{ "黑龙江卫视", "HeilongjiangTV" }, { "吉林卫视", "JilinTV" }, { "贵州卫视", "GuizhouTV" },
		{ "云南卫视", "YunnanTV" }, { "山西卫视", "ShanxiTV" }, { "陕西卫视", "ShaanxiTV" },
		{ "甘肃卫视", "GansuTV" }, { "广西卫视", "GuangxiTV" }, { "内蒙古卫视", "NeimengguTV" },
		{ "新疆卫视", "XinjiangTV" }, { "宁夏卫视", "NingxiaTV" }, { "青海卫视", "QinghaiTV" },
		{ "海南卫视", "HainanTV" }, { "西藏卫视", "XizangTV" }, { "三沙卫视", "SanshaTV" },
		{ "海峡卫视", "HaixiaTV" }, { "东南卫视", "DongnanTV" }, { "厦门卫视", "XiamenTV" },
		{ "粤港澳大湾区卫视", "GBATV" }, { "康巴卫视", "KangbaTV" }, { "五星体育", "FiveStarSports" }
	};

	// 常见画质/格式/来源标签，导出清洗时移除（忽略大小写）
	private static readonly string[] QualityTags = new string[]
	{
		"HD", "高清", "标清", "超清", "超高清", "原画", "流畅", "极速", "蓝光",
		"FHD", "UHD", "1080P", "720P", "1080", "720", "HR", "PH",
		"HDR", "杜比", "环绕", "中英", "双语", "多语", "官方", "测试", "TEST",
		"CAM", "TS", "WEB", "源", "线路", "备用", "修复", "新版", "老版"
	};

	/// <summary>
	/// 清洗频道名称：去画质/来源标签、统一 CCTV/卫视写法、规整分隔符。
	/// 仅用于导出，不影响软件内原始列表。
	/// </summary>
	public static string CleanChannelName(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return raw;
		}
		string s = raw.Trim();
		// 全角转半角，避免不同来源符号不一致
		s = s.Replace('（', '(').Replace('）', ')').Replace('：', ':').Replace('　', ' ')
			.Replace('，', ',').Replace('［', '[').Replace('］', ']').Replace('【', '[').Replace('】', ']');
		// 去掉常见画质/格式标签（含可能的括号包裹）
		foreach (string t in QualityTags)
		{
			s = Regex.Replace(s, @"[\(\{\[]?\s*" + Regex.Escape(t) + @"\s*[\}\)\]]?", "", RegexOptions.IgnoreCase);
		}
		// 去掉形如 (1) (2) [12] 的纯数字序号（来源去重标记）
		s = Regex.Replace(s, @"[\(\{\[]\s*\d+\s*[\}\)\]]", "");
		// 去掉残留的来源标记，如 @iptv、_tt
		s = Regex.Replace(s, @"[@_]\S+", "");
		// 统一 CCTV 写法：CCTV-1 / CCTV 1 -> CCTV1
		s = Regex.Replace(s, @"CCTV\s*-?\s*(\d+)", "CCTV$1", RegexOptions.IgnoreCase);
		// 去掉 CCTV 后多余的“综合”等描述，便于统一管理（CCTV1综合 -> CCTV1）
		s = Regex.Replace(s, @"(CCTV\d+(?:\+|plus)?)\s*综合", "$1", RegexOptions.IgnoreCase);
		// 4K/8K 仅作为画质标签时移除，但必须保留 CCTV4K / CCTV8K 的频道写法（负向后查排除 CCTV 前缀）
		s = Regex.Replace(s, @"(?<!CCTV)\s*4K", "", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"(?<!CCTV)\s*8K", "", RegexOptions.IgnoreCase);
		// 规整空白与首尾标点
		s = Regex.Replace(s, @"\s+", " ").Trim();
		s = s.Trim(' ', '-', '_', '|', '·', '•', '—', '~', '-', ',', ':', '：');
		s = s.Trim();
		// 清洗后为空则回退原始名称，避免丢失
		if (string.IsNullOrWhiteSpace(s))
		{
			return raw.Trim();
		}
		return s;
	}

	/// <summary>返回全部兜底台标源模板（{0} 为频道 token）。</summary>
	public static string[] GetLogoSources()
	{
		return (string[])LogoSources.Clone();
	}

	// 官方规范台名（按键匹配），用于导出时统一展示。
	private static readonly Dictionary<string, string> OfficialCctvNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "CCTV1", "CCTV-1 综合频道" }, { "CCTV2", "CCTV-2 财经频道" },
		{ "CCTV3", "CCTV-3 综艺频道" }, { "CCTV4", "CCTV-4 中文国际" },
		{ "CCTV5", "CCTV-5 体育频道" }, { "CCTV5+", "CCTV-5+ 体育赛事" },
		{ "CCTV6", "CCTV-6 电影频道" }, { "CCTV7", "CCTV-7 国防军事" },
		{ "CCTV8", "CCTV-8 电视剧" }, { "CCTV9", "CCTV-9 纪录频道" },
		{ "CCTV10", "CCTV-10 科教频道" }, { "CCTV11", "CCTV-11 戏曲频道" },
		{ "CCTV12", "CCTV-12 社会与法" }, { "CCTV13", "CCTV-13 新闻频道" },
		{ "CCTV14", "CCTV-14 少儿频道" }, { "CCTV15", "CCTV-15 音乐频道" },
		{ "CCTV16", "CCTV-16 奥林匹克" }, { "CCTV17", "CCTV-17 农业农村" },
		{ "CCTV4K", "CCTV-4K 超高清" }, { "CCTV8K", "CCTV-8K 超高清" }
	};

	/// <summary>归一化合并键：把 CCTV-1 综合频道 / CCTV1 / CCTV1综合 等统一为 cctv1，卫视保持原名（去空格）。用于按频道合并。</summary>
	public static string NormalizeKey(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return raw ?? "";
		}
		string s = CleanChannelName(raw);
		// 去掉 CCTV 后描述（综合/财经/.../体育赛事），便于按频道号归一
		s = Regex.Replace(s, @"(CCTV\d+(?:\+|plus|Plus)?)\s*(综合|财经|综艺|中文国际|体育|体育赛事|电影|国防军事|军事农业|电视剧|纪录|记录|科教|戏曲|社会与法|新闻|少儿|音乐|奥林匹克|农业农村)", "$1", RegexOptions.IgnoreCase);
		// 统一 CCTV5+/4K/8K 写法
		s = Regex.Replace(s, @"CCTV\s*5\s*\+", "CCTV5+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"CCTV\s*5\s*plus", "CCTV5+", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"CCTV\s*4\s*K", "CCTV4K", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"CCTV\s*8\s*K", "CCTV8K", RegexOptions.IgnoreCase);
		s = Regex.Replace(s, @"\s+", "").ToLowerInvariant();
		return s;
	}

	/// <summary>返回官方规范台名（如 CCTV-1 综合频道）；卫视等无规范表的返回清洗后的展示名。</summary>
	public static string OfficialName(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return raw ?? "";
		}
		string key = NormalizeKey(raw);
		if (OfficialCctvNames.TryGetValue(key, out string off))
		{
			return off;
		}
		return CleanChannelName(raw);
	}

	/// <summary>仅把 CCTV 系列清洗为官方标准名（CCTV-1 综合频道 …）；其它名称原样返回，不做任何清洗/去标签。</summary>
	public static string StandardNameCctvOnly(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return raw ?? "";
		}
		string key = NormalizeKey(raw);
		if (key.StartsWith("cctv") && OfficialCctvNames.TryGetValue(key, out string off))
		{
			return off;
		}
		return raw;
	}

	// 导出分组顺序（权重）
	private static readonly Dictionary<string, int> CategoryOrderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		{ "央视频道", 1 }, { "卫视频道", 2 }, { "港澳台频道", 3 }, { "其他频道", 9 }
	};

	/// <summary>返回频道的导出分组：央视频道 / 卫视频道 / 港澳台频道 / 其他频道（按名称自动识别）。</summary>
	public static string ClassifyChannel(string rawName)
	{
		if (string.IsNullOrWhiteSpace(rawName))
		{
			return "其他频道";
		}
		string key = NormalizeKey(rawName);
		string clean = CleanChannelName(rawName);
		if (key.StartsWith("cctv"))
		{
			return "央视频道";
		}
		if (Regex.IsMatch(clean, @"(港澳|香港|澳门|臺灣|台湾|凤凰|明珠|翡翠|Jade|Pearl|TVB|纬来|民视|台视|中视|华视|星空|Now|Macau|Taiwan)", RegexOptions.IgnoreCase))
		{
			return "港澳台频道";
		}
		if (clean.Contains("卫视") || rawName.Contains("卫视"))
		{
			return "卫视频道";
		}
		return "其他频道";
	}

	/// <summary>返回分组排序权重，未知分组排最后。</summary>
	public static int CategoryOrder(string category)
	{
		if (!string.IsNullOrWhiteSpace(category) && CategoryOrderMap.TryGetValue(category, out int o))
		{
			return o;
		}
		return 99;
	}

	/// <summary>返回全部候选台标 URL（按优先级）。CCTV 以 51zmt 主源（已验证 tb1/CCTV 路径），卫视等以 fanmingming 为主、51zmt 为补充。</summary>
	public static List<string> GetLogoCandidates(string rawName)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(rawName))
		{
			return list;
		}
		string clean = CleanChannelName(rawName);
		string key = NormalizeKey(rawName);
		string fanToken = GetFanToken(clean, key);
		string safe = fanToken.Replace(" ", "").Replace("（", "").Replace("）", "").Replace("(", "").Replace(")", "");
		bool isCctv = key.StartsWith("cctv");
		string zmt = BuildZmt(clean, key);
		if (isCctv && zmt != null)
		{
			list.Add(zmt);
		}
		foreach (string tpl in LogoSources)
		{
			list.Add(string.Format(tpl, Uri.EscapeDataString(safe)));
		}
		if (!isCctv && zmt != null)
		{
			list.Add(zmt);
		}
		return list.Distinct().ToList();
	}

	/// <summary>解析主台标链接：返回候选列表第一项（最高优先级）。</summary>
	public static string ResolveLogo(string rawName)
	{
		List<string> c = GetLogoCandidates(rawName);
		return c.Count > 0 ? c[0] : "";
	}

	// 台标兜底 token：优先已知映射，CCTV 退化为 CCTV+数字（与 KnownLogoTokens 键一致）
	private static string GetFanToken(string clean, string key)
	{
		if (KnownLogoTokens.TryGetValue(clean, out string mapped))
		{
			return mapped;
		}
		if (key.StartsWith("cctv"))
		{
			Match m = Regex.Match(clean, @"CCTV(\d+)", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				string num = m.Groups[1].Value;
				if (key == "cctv5+")
				{
					return "CCTV5Plus";
				}
				if (key == "cctv4k")
				{
					return "CCTV4K";
				}
				if (key == "cctv8k")
				{
					return "CCTV8K";
				}
				return "CCTV" + num;
			}
		}
		return clean;
	}

	// 51zmt 台标 CDN（用户指定源）：CCTV 走已验证的 tb1/CCTV/{token}.png；卫视按拼音 best-effort
	private static string BuildZmt(string clean, string key)
	{
		if (key.StartsWith("cctv"))
		{
			Match m = Regex.Match(clean, @"CCTV(\d+)", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				string num = m.Groups[1].Value;
				string token;
				if (key == "cctv5+")
				{
					token = "CCTV5Plus";
				}
				else if (key == "cctv4k")
				{
					token = "CCTV4K";
				}
				else if (key == "cctv8k")
				{
					token = "CCTV8K";
				}
				else
				{
					token = "CCTV" + num;
				}
				return "http://epg.51zmt.top:8000/tb1/CCTV/" + token + ".png";
			}
		}
		if (KnownLogoTokens.TryGetValue(clean, out string py))
		{
			return "http://epg.51zmt.top:8000/tb1/%E5%8D%AB%E8%A7%86/" + py + ".png";
		}
		return null;
	}
}
