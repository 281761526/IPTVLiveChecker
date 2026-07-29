namespace IPTVLiveChecker;

/// <summary>
/// 全局常量定义，消除散布在各文件中的重复字符串字面量。
/// </summary>
internal static class AppConstants
{
	// ===== 版本信息 =====
	public const string CurrentVersion = "v1.0.0";
	public const int CurrentVersionCode = 100;

	// ===== 网络 =====
	public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

	// ===== 频道状态 =====
	public const string StatusNotChecked = "未检测";
	public const string StatusAvailable = "可用";
	public const string StatusUnavailable = "不可用";
	public const string StatusChecking = "检测中";
	public const string StatusRechecking = "复检中";
	public const string StatusPendingParse = "待解析";
	public const string StatusParseQueued = "解析待处理";

	// ===== 分组 =====
	public const string GroupNone = "未分组";
	public const string GroupAll = "全部";

	// ===== DataGridView 列名 =====
	public const string ColUrl = "colUrl";
	public const string ColName = "colName";
	public const string ColStatus = "colStatus";
	public const string ColAction = "colAction";
	public const string ColSpeed = "colSpeed";
	public const string ColGroup = "colGroup";
	public const string ColResolution = "colResolution";
	public const string ColLocation = "colLocation";

	// ===== 平台名称 =====
	public const string PlatformZhgx = "智慧光迅";
	public const string PlatformKutv = "智慧桌面";
	public const string PlatformHuashi = "华视美达";

	// ===== 平台 URL 路径 =====
	public const string UrlZhgxPath = "/ZHGXTV/Public/json/live_interface.txt";
	public const string UrlKutvPath = "/iptv/live/1000.json?key=txiptv";
	public const string UrlHuashiPattern = "/newlive/live/hls/{0}/live.m3u8";

	// ===== 字体 =====
	public const string FontFamilyName = "Microsoft YaHei";

	// ===== 主题 =====
	public const string DefaultThemeName = "青瓷薄荷";

	// ===== 流媒体标识 =====
	public const string M3uHeader = "#EXTM3U";

	// ===== UI 提示 =====
	public const string SearchPlaceholder = "输入搜索内容，按下回车键搜索";
	public const string SelectRowFirst = "请先选择一行";

	// ===== 更新系统 =====
	// 更新配置镜像链：GitHub 直连 + 多个国内可用代理/CDN 兜底。
	// 抓取逻辑会按顺序逐个尝试（单镜像超时即跳到下一个），任一可达即可完成更新检查。
	public const string UpdateUrlPrimary = "https://raw.githubusercontent.com/281761526/IPTVLiveChecker/main/update.json";
	public const string UpdateUrlMirror1 = "https://cdn.jsdelivr.net/gh/281761526/IPTVLiveChecker@main/update.json";
	public const string UpdateUrlMirror2 = "https://fastly.jsdelivr.net/gh/281761526/IPTVLiveChecker@main/update.json";
	public const string UpdateUrlMirror3 = "https://gcore.jsdelivr.net/gh/281761526/IPTVLiveChecker@main/update.json";
	public const string UpdateUrlMirror4 = "https://ghproxy.net/https://raw.githubusercontent.com/281761526/IPTVLiveChecker/main/update.json";
	public const string UpdateUrlMirror5 = "https://raw.bgithub.xyz/281761526/IPTVLiveChecker/main/update.json";

	// 顺序说明：国内可靠的 jsdelivr / GitHub 代理前置，GitHub 直连放最后兜底。
	// 这样国内用户能快速命中可用镜像，避免启动时被直连超时拖慢。
	public static readonly string[] UpdateMirrors = {
		UpdateUrlMirror1,   // cdn.jsdelivr.net   (国内最稳)
		UpdateUrlMirror4,   // ghproxy.net        (GitHub 代理)
		UpdateUrlMirror5,   // raw.bgithub.xyz    (GitHub 镜像)
		UpdateUrlMirror3,   // gcore.jsdelivr.net
		UpdateUrlMirror2,   // fastly.jsdelivr.net
		UpdateUrlPrimary    // raw.githubusercontent.com (GitHub 直连，最后兜底)
	};
}
