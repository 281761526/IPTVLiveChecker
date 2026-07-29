using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain
{
	private static readonly Dictionary<string, string> CityMap = new Dictionary<string, string>
	{
		{ "beijing", "北京" },
		{ "bj", "北京" },
		{ "shanghai", "上海" },
		{ "sh", "上海" },
		{ "guangzhou", "广州" },
		{ "gz", "广州" },
		{ "gd", "广东" },
		{ "shenzhen", "深圳" },
		{ "sz", "深圳" },
		{ "hangzhou", "杭州" },
		{ "hz", "杭州" },
		{ "zj", "浙江" },
		{ "nanjing", "南京" },
		{ "nj", "南京" },
		{ "js", "江苏" },
		{ "chengdu", "成都" },
		{ "cd", "成都" },
		{ "sc", "四川" },
		{ "wuhan", "武汉" },
		{ "wh", "武汉" },
		{ "hb", "湖北" },
		{ "xian", "西安" },
		{ "xa", "西安" },
		{ "sn", "陕西" },
		{ "chongqing", "重庆" },
		{ "cq", "重庆" },
		{ "tianjin", "天津" },
		{ "tj", "天津" },
		{ "shenyang", "沈阳" },
		{ "sy", "沈阳" },
		{ "ln", "辽宁" },
		{ "qingdao", "青岛" },
		{ "qd", "青岛" },
		{ "sd", "山东" },
		{ "zhengzhou", "郑州" },
		{ "zz", "郑州" },
		{ "hn", "河南" },
		{ "changsha", "长沙" },
		{ "cs", "长沙" },
		{ "hefei", "合肥" },
		{ "hf", "合肥" },
		{ "ah", "安徽" },
		{ "fuzhou", "福州" },
		{ "fz", "福州" },
		{ "fj", "福建" },
		{ "xiamen", "厦门" },
		{ "xm", "厦门" },
		{ "kunming", "昆明" },
		{ "km", "昆明" },
		{ "yn", "云南" },
		{ "guiyang", "贵阳" },
		{ "gy", "贵阳" },
		{ "gz2", "贵州" },
		{ "nanning", "南宁" },
		{ "nn", "南宁" },
		{ "gx", "广西" },
		{ "haikou", "海口" },
		{ "hk", "海口" },
		{ "hi", "海南" },
		{ "harbin", "哈尔滨" },
		{ "heb", "哈尔滨" },
		{ "hlj", "黑龙江" },
		{ "changchun", "长春" },
		{ "cc", "长春" },
		{ "jl", "吉林" },
		{ "huhehot", "呼和浩特" },
		{ "nm", "内蒙古" },
		{ "wulumuqi", "乌鲁木齐" },
		{ "xj", "新疆" },
		{ "nmg", "内蒙古" },
		{ "neimenggu", "内蒙古" },
		{ "cnmg", "内蒙古" },
		{ "lasa", "拉萨" },
		{ "xz", "西藏" },
		{ "lanzhou", "兰州" },
		{ "gs", "甘肃" },
		{ "yinchuan", "银川" },
		{ "nx", "宁夏" },
		{ "xining", "西宁" },
		{ "qh", "青海" },
		{ "nanchang", "南昌" },
		{ "jx", "江西" },
		{ "taiyuan", "太原" },
		{ "ty", "太原" },
		{ "sx", "山西" },
		{ "shijiazhuang", "石家庄" },
		{ "sjz", "石家庄" },
		{ "he", "河北" }
	};

	private static readonly Dictionary<string, string> UrlIspMap = new Dictionary<string, string>
	{
		{ "cmcc", "移动" },
		{ "mobile", "移动" },
		{ "chinamobile", "移动" },
		{ "migu", "移动" },
		{ "unicom", "联通" },
		{ "chinaunicom", "联通" },
		{ "cu", "联通" },
		{ "wo", "联通" },
		{ "telecom", "电信" },
		{ "chinatelecom", "电信" },
		{ "ct", "电信" },
		{ "tianyi", "电信" },
		{ "189", "电信" },
		{ "cernet", "教育网" },
		{ "edu", "教育网" },
		{ "aliyun", "阿里云" },
		{ "ali", "阿里云" },
		{ "alibaba", "阿里云" },
		{ "tencent", "腾讯云" },
		{ "tenc", "腾讯云" },
		{ "qcloud", "腾讯云" },
		{ "wechat", "腾讯云" },
		{ "baidu", "百度云" },
		{ "bce", "百度云" },
		{ "huawei", "华为云" },
		{ "hwcloud", "华为云" },
		{ "aws", "AWS" },
		{ "cloudfront", "AWS" },
		{ "amazon", "AWS" },
		{ "cloudflare", "Cloudflare" },
		{ "cf", "Cloudflare" },
		{ "google", "Google" },
		{ "gstatic", "Google" },
		{ "akamai", "Akamai" },
		{ "akamaized", "Akamai" },
		{ "cdn", "CDN" },
		{ "cache", "CDN" },
		{ "ks3", "CDN" },
		{ "qiniucdn", "CDN" },
		{ "cdnbye", "CDN" }
	};

	private static readonly Dictionary<string, string> CctvMap = new Dictionary<string, string>
	{
		{ "cctv", "CCTV" },
		{ "cntv", "CCTV" },
		{ "cctvnews", "CCTV" },
		{ "cctv5", "CCTV" },
		{ "cmg", "央视" },
		{ "chinacert", "央视" },
		{ "cnr", "央广" },
		{ "cri", "国际台" },
		{ "wasu", "华数" },
		{ "hunan", "湖南" },
		{ "mgtv", "芒果TV" },
		{ "hunantv", "湖南" },
		{ "zhejiang", "浙江卫视" },
		{ "zjstv", "浙江" },
		{ "jiangsu", "江苏卫视" },
		{ "jstv", "江苏" },
		{ "dongfang", "东方" },
		{ "dragon", "东方" },
		{ "beijingtv", "北京卫视" },
		{ "brtn", "北京" },
		{ "shmedia", "上海台" },
		{ "smg", "上海" },
		{ "satv", "深圳卫视" },
		{ "sztv", "深圳" },
		{ "guangdong", "广东卫视" },
		{ "gdtv", "广东" },
		{ "scs", "四川卫视" },
		{ "sctv", "四川" },
		{ "hbtv", "湖北卫视" },
		{ "sdtv", "山东卫视" },
		{ "hntv", "河南卫视" },
		{ "ahtv", "安徽卫视" },
		{ "fjrtv", "福建卫视" },
		{ "fjtv", "东南" }
	};

	private static readonly Dictionary<string, string> ShortenIspMap = new Dictionary<string, string>
	{
		{ "电信", "电信" },
		{ "联通", "联通" },
		{ "移动", "移动" },
		{ "China Telecom", "电信" },
		{ "China Unicom", "联通" },
		{ "China Mobile", "移动" },
		{ "CHINANET", "电信" },
		{ "UNICOM", "联通" },
		{ "CMNET", "移动" },
		{ "阿里云", "阿里云" },
		{ "腾讯云", "腾讯云" },
		{ "华为云", "华为云" },
		{ "Alibaba", "阿里云" },
		{ "Tencent", "腾讯云" },
		{ "Huawei", "华为云" },
		{ "Amazon", "AWS" },
		{ "Cloudflare", "CF" },
		{ "Google", "Google" },
		{ "教育网", "教育网" },
		{ "CERNET", "教育网" },
		{ "广电", "广电" },
		{ "铁通", "铁通" },
		{ "长城", "长城宽带" },
		{ "鹏博士", "鹏博士" }
	};

	private static readonly Dictionary<string, string> SimplifyIspMap = new Dictionary<string, string>
	{
		{ "中国移动", "移动" },
		{ "中国联通", "联通" },
		{ "中国电信", "电信" },
		{ "CHINA MOBILE", "移动" },
		{ "CHINA UNICOM", "联通" },
		{ "CHINA TELECOM", "电信" },
		{ "China Mobile", "移动" },
		{ "China Unicom", "联通" },
		{ "China Telecom", "电信" },
		{ "China Mobile Communications", "移动" },
		{ "China United Network", "联通" },
		{ "China Telecom Group", "电信" },
		{ "CT", "电信" },
		{ "CU", "联通" },
		{ "CM", "移动" }
	};

	private static bool IsPrivateIpv4(byte[] b)
	{
		return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || b[0] == 127 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || (b[0] == 169 && b[1] == 254);
	}

	private static async Task<string> HttpGetBodyAsync(HttpClient client, string url, CancellationToken token, int timeoutMs = 3000)
	{
		using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			cts.CancelAfter(timeoutMs);
			using (var resp = await client.GetAsync(url, cts.Token).ConfigureAwait(false))
			{
				if (!resp.IsSuccessStatusCode) return null;
				string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				return body?.TrimStart().StartsWith("{") == true || body?.TrimStart().StartsWith("<") == true ? body : null;
			}
		}
	}

	private static string FormatLocation(string country, string region, string city, string isp)
	{
		string loc;
		if (!string.IsNullOrEmpty(country) && country.Contains("中国"))
		{
			loc = string.IsNullOrEmpty(city) ? (region ?? "") : city;
		}
		else
		{
			loc = country ?? "";
			if (!string.IsNullOrEmpty(region) && region != country) loc += " " + region;
		}
		string shortIsp = ShortenIsp(isp);
		if (!string.IsNullOrEmpty(shortIsp)) loc = loc.Trim() + " " + shortIsp;
		return loc.Trim();
	}

	private string ExtractLocationFromUrl(string url)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return "";
			}
			string host = "";
			try
			{
				host = new Uri(url).Host;
			}
			catch
			{
				return "";
			}
			if (IPAddress.TryParse(host, out var ip))
			{
				byte[] b = ip.GetAddressBytes();
				if (b.Length == 4)
				{
					if (IsPrivateIpv4(b))
					{
						return "内网";
					}
					return "";
				}
				return "IPv6";
			}
			string lowerHost = host.ToLower();
			string[] parts = lowerHost.Split('.');
			if (parts.Length >= 2)
			{
				string tld = parts[parts.Length - 1];
				string sld = parts[parts.Length - 2];
				string domain = ((parts.Length >= 3) ? parts[parts.Length - 3] : "");
				foreach (KeyValuePair<string, string> kv in CityMap)
				{
					if (sld == kv.Key || domain == kv.Key || lowerHost.Contains(kv.Key))
					{
						return kv.Value;
					}
				}
				foreach (KeyValuePair<string, string> kv2 in CctvMap)
				{
					if (lowerHost.Contains(kv2.Key))
					{
						return kv2.Value;
					}
				}
				foreach (KeyValuePair<string, string> kv3 in UrlIspMap)
				{
					if (sld == kv3.Key || domain == kv3.Key || lowerHost.Contains(kv3.Key))
					{
						return kv3.Value;
					}
				}
				switch (tld)
				{
				case "cn":
				case "com.cn":
				case "net.cn":
				case "org.cn":
				case "gov.cn":
					return "国内";
				case "tv":
				case "live":
				case "me":
				case "cc":
				case "io":
				case "top":
				case "xyz":
					return "海外";
				case "com":
				case "net":
				case "org":
					return (host.Length > 18) ? (host.Substring(0, 15) + "...") : host;
				}
			}
			return (host.Length > 18) ? (host.Substring(0, 15) + "...") : host;
		}
		catch
		{
			return "";
		}
	}

	private string GuessIpLocation(byte a, byte b)
	{
		if (a != 36 && a != 39 && a != 42 && a != 43 && (a != 49 || b < 64 || b > 95))
		{
			switch (a)
			{
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 58:
			case 59:
			case 60:
			case 61:
			case 101:
			case 103:
			case 106:
			case 110:
			case 111:
			case 112:
			case 113:
			case 114:
			case 115:
			case 116:
			case 117:
			case 118:
			case 119:
			case 120:
			case 121:
			case 122:
			case 123:
			case 124:
			case 125:
			case 126:
				break;
			default:
				switch (a)
				{
				case 8:
				case 9:
					return "北美";
				case 23:
				case 24:
				case 25:
				case 26:
				case 27:
				case 28:
				case 29:
				case 30:
				case 31:
				case 32:
				case 33:
					return "北美";
				default:
					if (a >= 64 && a <= 77)
					{
						return "北美";
					}
					if (a >= 96 && a <= 100)
					{
						return "北美";
					}
					if (a >= 128 && a <= 191)
					{
						if (b >= 0 && b <= 99)
						{
							return "北美";
						}
						if (b >= 100 && b <= 159)
						{
							return "欧洲";
						}
						if (b >= 160 && b <= 199)
						{
							return "北美";
						}
						if (b >= 200 && b <= byte.MaxValue)
						{
							return "其他";
						}
					}
					return "";
				}
			}
		}
		return "国内";
	}

	private async Task<string> QueryIpLocationAsync(string ip, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(ip))
		{
			return "";
		}
		lock (ipLocationCache)
		{
			if (ipLocationCache.ContainsKey(ip))
			{
				return ipLocationCache[ip];
			}
			if (ipLocationFailed.Contains(ip))
			{
				return "";
			}
		}
		if (!IPAddress.TryParse(ip, out var addr))
		{
			return "";
		}
		byte[] b = addr.GetAddressBytes();
		bool isV6 = b.Length != 4;
		if (!isV6)
		{
			if (IsPrivateIpv4(b))
			{
				string lan = "内网";
				lock (ipLocationCache)
				{
					ipLocationCache[ip] = lan;
				}
				return lan;
			}
		}
		else if (IPAddress.IsLoopback(addr) || addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal)
		{
			string lan2 = "内网";
			lock (ipLocationCache)
			{
				ipLocationCache[ip] = lan2;
			}
			return lan2;
		}
		string result = "";
		var providers = new Func<string, CancellationToken, Task<string>>[] { QueryIpApiComAsync, QueryPing0CcAsync, QueryIpWhoIsAsync };
		foreach (var provider in providers)
		{
			if (!string.IsNullOrEmpty(result)) break;
			try { result = await provider(ip, token); } catch { }
		}
		if (string.IsNullOrEmpty(result))
		{
			if (!isV6)
			{
				result = GuessIpLocation(b[0], b[1]);
				if (string.IsNullOrEmpty(result))
				{
					result = "海外";
				}
			}
			else
			{
				result = "海外";
			}
		}
		lock (ipLocationCache)
		{
			if (!string.IsNullOrEmpty(result))
			{
				ipLocationCache[ip] = result;
			}
			else
			{
				ipLocationFailed.Add(ip);
			}
		}
		return result;
	}

	private string ExtractJsonField(string json, string key)
	{
		Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
		if (m.Success)
		{
			return m.Groups[1].Value;
		}
		return "";
	}

	private async Task<string> QueryIpApiComAsync(string ip, CancellationToken token)
	{
		try
		{
			string url = "http://ip-api.com/json/" + ip + "?lang=zh-CN&fields=status,message,country,regionName,city,isp";
			string body = await HttpGetBodyAsync(httpClient, url, token);
			if (body == null)
			{
				return "";
			}
			if (ExtractJsonField(body, "status") != "success")
			{
				return "";
			}
			string country = ExtractJsonField(body, "country");
			string region = ExtractJsonField(body, "regionName");
			string city = ExtractJsonField(body, "city");
			string isp = ExtractJsonField(body, "isp");
			if (string.IsNullOrEmpty(country))
			{
				return "";
			}
			return FormatLocation(country, region, city, isp);
		}
		catch
		{
			return "";
		}
	}

	private async Task<string> QueryIpWhoIsAsync(string ip, CancellationToken token)
	{
		try
		{
			string url = "https://ipwho.is/" + ip + "?lang=zh-CN&fields=success,country,region,city,connection";
			string body = await HttpGetBodyAsync(httpClient, url, token);
			if (body == null)
			{
				return "";
			}
			Match successM = Regex.Match(body, "\"success\"\\s*:\\s*(true|false)");
			if (!successM.Success || successM.Groups[1].Value != "true")
			{
				return "";
			}
			string country = ExtractJsonField(body, "country");
			string region = ExtractJsonField(body, "region");
			string city = ExtractJsonField(body, "city");
			string isp = "";
			Match connM = Regex.Match(body, "\"connection\"\\s*:\\s*\\{[^}]*\"isp\"\\s*:\\s*\"([^\"]*)\"");
			if (connM.Success)
			{
				isp = connM.Groups[1].Value;
			}
			Match orgM = Regex.Match(body, "\"connection\"\\s*:\\s*\\{[^}]*\"org\"\\s*:\\s*\"([^\"]*)\"");
			if (string.IsNullOrEmpty(isp) && orgM.Success)
			{
				isp = orgM.Groups[1].Value;
			}
			if (string.IsNullOrEmpty(country))
			{
				return "";
			}
			return FormatLocation(country, region, city, isp);
		}
		catch
		{
			return "";
		}
	}

	private static string ShortenIsp(string isp)
	{
		if (string.IsNullOrEmpty(isp))
		{
			return "";
		}
		foreach (KeyValuePair<string, string> kv in ShortenIspMap)
		{
			if (isp.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return kv.Value;
			}
		}
		return "";
	}

	private async Task<string> ResolveDomainToIpAsync(string host, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			return "";
		}
		lock (domainIpCache)
		{
			if (domainIpCache.ContainsKey(host))
			{
				return domainIpCache[host];
			}
			if (domainIpFailed.Contains(host))
			{
				return "";
			}
		}
		try
		{
			using (CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(3000).Token))
			{
				IPAddress[] array = await Dns.GetHostAddressesAsync(host);
				foreach (IPAddress ip in array)
				{
					if (ip.AddressFamily != AddressFamily.InterNetwork)
					{
						continue;
					}
					byte[] b = ip.GetAddressBytes();
					if (!IsPrivateIpv4(b))
					{
						string ipStr = ip.ToString();
						lock (domainIpCache)
						{
							domainIpCache[host] = ipStr;
						}
						return ipStr;
					}
				}
			}
		}
		catch
		{
		}
		lock (domainIpCache)
		{
			domainIpFailed.Add(host);
		}
		return "";
	}

	private async Task<string> QueryDomainLocationAsync(string host, CancellationToken token)
	{
		try
		{
			string domainLoc = ExtractLocationFromUrl("http://" + host + "/");
			if (!string.IsNullOrWhiteSpace(domainLoc) && domainLoc != "国内" && domainLoc != "海外")
			{
				return domainLoc;
			}
			string ip = await ResolveDomainToIpAsync(host, token);
			if (!string.IsNullOrEmpty(ip))
			{
				string ipLoc = await QueryIpLocationAsync(ip, token);
				if (!string.IsNullOrEmpty(ipLoc))
				{
					return ipLoc;
				}
			}
			return domainLoc;
		}
		catch
		{
			return "";
		}
	}

	private async Task<string> QueryPing0CcAsync(string ip, CancellationToken token)
	{
		try
		{
			string url = "https://ping0.cc/ip/" + ip;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
			{
				cts.CancelAfter(4000);
				using (var req = new HttpRequestMessage(HttpMethod.Get, url))
				{
					req.Headers.Add("User-Agent", AppConstants.UserAgent);
					req.Headers.Add("Accept", "text/html,application/xhtml+xml");
					req.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
					using (var resp = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
					{
						if (!resp.IsSuccessStatusCode)
						{
							return "";
						}
						string html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
						if (string.IsNullOrEmpty(html) || !html.TrimStart().StartsWith("<"))
						{
							return "";
						}
						int idx = html.IndexOf("IP 位置", StringComparison.Ordinal);
						if (idx < 0)
						{
							return "";
						}
						string clean = Regex.Replace(html.Substring(idx, Math.Min(500, html.Length - idx)), "<[^>]+>", "");
						clean = WebUtility.HtmlDecode(clean);
						clean = clean.Replace("IP 位置", "").Replace("错误提交", "").Trim();
						int flagEnd = clean.IndexOfAny(new char[19]
						{
							'中', '美', '日', '韩', '英', '德', '法', '俄', '新', '马',
							'泰', '越', '印', '菲', '加', '澳', '香', '台', '澳'
						});
						if (flagEnd > 0)
						{
							clean = clean.Substring(flagEnd);
						}
						else
						{
							int m = Regex.Match(clean, "[\\u4e00-\\u9fff]").Index;
							if (m > 0)
							{
								clean = clean.Substring(m);
							}
						}
						clean = Regex.Replace(clean, "\\s+", " ").Trim();
						int end = clean.IndexOf("ASN", StringComparison.Ordinal);
						if (end > 0)
						{
							clean = clean.Substring(0, end).Trim();
						}
						if (string.IsNullOrWhiteSpace(clean))
						{
							return "";
						}
						if (clean.Length > 30)
						{
							clean = clean.Substring(0, 30);
						}
						return SimplifyLocation(clean);
					}
				}
			}
		}
		catch
		{
			return "";
		}
	}

	private string SimplifyLocation(string loc)
	{
		if (string.IsNullOrEmpty(loc))
		{
			return "";
		}
		loc = loc.Trim();
		if (loc.StartsWith("中国 "))
		{
			loc = loc.Substring(3).TrimStart();
		}
		else if (loc.StartsWith("中国"))
		{
			loc = loc.Substring(2).TrimStart();
		}
		foreach (KeyValuePair<string, string> kv in SimplifyIspMap)
		{
			int idx = loc.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase);
			if (idx >= 0)
			{
				loc = loc.Substring(0, idx) + " " + kv.Value;
				break;
			}
		}
		loc = loc.Replace("省", "").Replace("市", "").Replace("自治区", "")
			.Replace("特别行政区", "");
		string[] obj = new string[13]
		{
			"移动", "联通", "电信", "教育网", "阿里云", "腾讯云", "华为云", "百度云", "Cloudflare", "Google",
			"AWS", "Akamai", "CDN"
		};
		string isp = "";
		string[] array = obj;
		foreach (string k in array)
		{
			int ki = loc.LastIndexOf(k);
			if (ki >= 0)
			{
				isp = k;
				loc = loc.Substring(0, ki).Trim();
				break;
			}
		}
		loc = Regex.Replace(loc, "[A-Za-z]+", "");
		loc = Regex.Replace(loc, "\\d+", "");
		loc = loc.Replace(",", "").Trim();
		while (loc.Contains("  "))
		{
			loc = loc.Replace("  ", " ");
		}
		loc = loc.Trim();
		if (!string.IsNullOrEmpty(isp) && !loc.Contains(isp))
		{
			loc = loc + " " + isp;
		}
		if (string.IsNullOrWhiteSpace(loc))
		{
			return "";
		}
		return loc.Trim();
	}
}
