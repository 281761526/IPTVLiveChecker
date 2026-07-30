using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace IPTVLiveChecker;

public partial class IPTVLiveCheckerMain
{
	internal static Font GetFont(float size)
	{
		return new Font(customFontFamily, size);
	}

	internal static Font GetFont(float size, FontStyle style)
	{
		return new Font(customFontFamily, size, style);
	}

	private void SaveConfig()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("[Settings]");
		sb.AppendLine("CustomFontFamily=" + customFontFamily);
		sb.AppendLine("DetectEngine=" + detectEngine);
		sb.AppendLine($"DetectConcurrency={detectConcurrency}");
		sb.AppendLine($"TimeoutSeconds={timeoutSeconds}");
		sb.AppendLine($"AutoClearInvalid={autoClearInvalid}");
		sb.AppendLine($"PersistList={persistList}");
		sb.AppendLine("CustomPlayerPath=" + customPlayerPath);
		sb.AppendLine($"WatchSearchWindow={watchSearchWindow}");
		sb.AppendLine($"ShowSearchButton={showSearchButton}");
		sb.AppendLine("ThemePreference=" + themePreference);
		sb.AppendLine($"AutoExtractIpPort={autoExtractIpPort}");
		sb.AppendLine($"AutoParseLink={autoParseLink}");
		sb.AppendLine("IptvHistoryIps=" + string.Join("|", iptvHistoryIps));
		sb.AppendLine($"DisclaimerAgreed={disclaimerAgreed}");
		sb.AppendLine($"SkipDisclaimerPrompt={skipDisclaimerPrompt}");
		sb.AppendLine($"AutoSwitchExternal={autoSwitchExternalPlayer}");
		File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);
	}

	private void SaveChannelList()
	{
		try
		{
			StringBuilder sb = new StringBuilder();
			foreach (ChannelInfo allChannel in allChannels)
			{
				string name = allChannel.Name?.Replace(",", "，") ?? "";
				string url = allChannel.Url ?? "";
				string group = allChannel.Group?.Replace(",", "，") ?? "";
				string status = allChannel.Status ?? "";
				string resolution = allChannel.Resolution ?? "";
				string location = allChannel.Location ?? "";
				string speed = allChannel.Speed ?? "";
				sb.AppendLine(name + "," + url + "," + group + "," + status + "," + resolution + "," + location + "," + speed);
			}
			File.WriteAllText(channelListPath, sb.ToString(), Encoding.UTF8);
		}
		catch
		{
		}
	}

	private void LoadChannelList()
	{
		if (!File.Exists(channelListPath))
		{
			return;
		}
		try
		{
			string[] array = File.ReadAllLines(channelListPath, Encoding.UTF8);
			allChannels.Clear();
			string[] array2 = array;
			foreach (string line in array2)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				string[] parts = line.Split(',');
				if (parts.Length >= 2)
				{
					string url = parts[1];
					if (!string.IsNullOrWhiteSpace(url))
					{
						allChannels.Add(new ChannelInfo
						{
							Name = parts[0],
							Url = url,
							Group = ((parts.Length > 2) ? parts[2] : ""),
							Status = ((parts.Length > 3) ? parts[3] : "未检测"),
							Resolution = ((parts.Length > 4) ? parts[4] : ""),
							Location = ((parts.Length > 5) ? parts[5] : ""),
							Speed = ((parts.Length > 6) ? parts[6] : ""),
							Visible = true
						});
					}
				}
			}
			totalCount = allChannels.Count;
		}
		catch
		{
		}
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
			string[] array = File.ReadAllLines(configPath, Encoding.UTF8);
			foreach (string line in array)
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
				{
					continue;
				}
				int idx = line.IndexOf('=');
				if (idx <= 0)
				{
					continue;
				}
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
						iptvHistoryIps = value.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}
					break;
				case "DisclaimerAgreed":
					bool.TryParse(value, out disclaimerAgreed);
					break;
				case "SkipDisclaimerPrompt":
					bool.TryParse(value, out skipDisclaimerPrompt);
					break;
				case "AutoSwitchExternal":
					bool.TryParse(value, out autoSwitchExternalPlayer);
					break;
				}
			}
			ChannelPlayer.EnableAutoSwitchExternal = autoSwitchExternalPlayer;
		}
		catch (IOException)
		{
			try
			{
				SaveConfig();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private int SX(int x)
	{
		return (int)((float)x * dpiScale);
	}

	private int SY(int y)
	{
		return (int)((float)y * dpiScale);
	}

	private float SF(float f)
	{
		return f * dpiScale;
	}
}
