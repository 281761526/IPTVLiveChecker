using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IPTVLiveChecker;

internal static class ExternalPlayerHelper
{
	public enum PlayerType
	{
		None,
		PotPlayer,
		VLC,
		MPV,
		FFplay,
		Custom,
		SystemDefault
	}

	public class PlayerInfo
	{
		public PlayerType Type;
		public string Path;
		public string DisplayName;
	}

	private static string _customPlayerPath = "";

	public static void SetCustomPlayer(string path)
	{
		_customPlayerPath = path ?? "";
	}

	public static string GetCustomPlayer()
	{
		return _customPlayerPath;
	}

	public static PlayerInfo FindBestPlayer()
	{
		List<PlayerInfo> players = ScanAllPlayers();
		if (players.Count > 0)
		{
			return players[0];
		}
		return new PlayerInfo { Type = PlayerType.SystemDefault, Path = "", DisplayName = "系统默认播放器" };
	}

	public static List<PlayerInfo> ScanAllPlayers()
	{
		List<PlayerInfo> list = new List<PlayerInfo>();

		PlayerInfo pot = FindPotPlayer();
		if (pot != null) list.Add(pot);

		PlayerInfo vlc = FindVLC();
		if (vlc != null) list.Add(vlc);

		PlayerInfo mpv = FindMPV();
		if (mpv != null) list.Add(mpv);

		PlayerInfo ffplay = FindFFplay();
		if (ffplay != null) list.Add(ffplay);

		if (!string.IsNullOrWhiteSpace(_customPlayerPath) && File.Exists(_customPlayerPath))
		{
			list.Add(new PlayerInfo
			{
				Type = PlayerType.Custom,
				Path = _customPlayerPath,
				DisplayName = "自定义: " + Path.GetFileName(_customPlayerPath)
			});
		}

		return list;
	}

	public static PlayerInfo FindPotPlayer()
	{
		string[] regKeys = new string[]
		{
			@"SOFTWARE\Daum\PotPlayer64",
			@"SOFTWARE\Daum\PotPlayer",
			@"SOFTWARE\WOW6432Node\Daum\PotPlayer64",
			@"SOFTWARE\WOW6432Node\Daum\PotPlayer"
		};
		foreach (string keyName in regKeys)
		{
			try
			{
				using RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName);
				if (key != null)
				{
					string programPath = key.GetValue("ProgramPath") as string;
					if (!string.IsNullOrEmpty(programPath) && File.Exists(programPath))
					{
						return new PlayerInfo { Type = PlayerType.PotPlayer, Path = programPath, DisplayName = "PotPlayer" };
					}
				}
			}
			catch { }
		}
		string[] searchPaths = new string[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DAUM", "PotPlayer", "PotPlayerMini64.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "DAUM", "PotPlayer", "PotPlayerMini.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PotPlayer", "PotPlayerMini64.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PotPlayer", "PotPlayerMini.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PotPlayer", "PotPlayerMini64.exe")
		};
		foreach (string p in searchPaths)
		{
			if (File.Exists(p))
			{
				return new PlayerInfo { Type = PlayerType.PotPlayer, Path = p, DisplayName = "PotPlayer" };
			}
		}
		return null;
	}

	public static PlayerInfo FindVLC()
	{
		string path = VlcDetector.GetInstalledVlcPath();
		if (!string.IsNullOrEmpty(path))
		{
			return new PlayerInfo { Type = PlayerType.VLC, Path = path, DisplayName = "VLC" };
		}
		return null;
	}

	public static PlayerInfo FindMPV()
	{
		string[] searchPaths = new string[]
		{
			Path.Combine(Application.StartupPath, "mpv.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "mpv", "mpv.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "mpv", "mpv.exe")
		};
		foreach (string p in searchPaths)
		{
			if (File.Exists(p))
			{
				return new PlayerInfo { Type = PlayerType.MPV, Path = p, DisplayName = "MPV" };
			}
		}
		return null;
	}

	public static PlayerInfo FindFFplay()
	{
		string p = Path.Combine(Application.StartupPath, "ffplay.exe");
		if (File.Exists(p))
		{
			return new PlayerInfo { Type = PlayerType.FFplay, Path = p, DisplayName = "FFplay" };
		}
		return null;
	}

	public static bool Play(string url, PlayerInfo player)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		try
		{
			ProcessStartInfo psi;
			if (player == null || player.Type == PlayerType.SystemDefault || string.IsNullOrEmpty(player.Path))
			{
				psi = new ProcessStartInfo(url) { UseShellExecute = true };
				PlayerLogger.Write("EXT", $"使用系统默认播放器打开: {url}");
			}
			else
			{
				string args = BuildArguments(url, player.Type);
				psi = new ProcessStartInfo
				{
					FileName = player.Path,
					Arguments = args,
					UseShellExecute = false
				};
				PlayerLogger.Write("EXT", $"使用 {player.DisplayName} 打开: {url} | args={args}");
			}
			Process.Start(psi);
			return true;
		}
		catch (Exception ex)
		{
			PlayerLogger.WriteError("EXT", ex);
			return false;
		}
	}

	private static string BuildArguments(string url, PlayerType type)
	{
		switch (type)
		{
			case PlayerType.PotPlayer:
				return "\"" + url + "\" /play";
			case PlayerType.VLC:
				return "\"" + url + "\" --no-video-title-show";
			case PlayerType.MPV:
				return "\"" + url + "\"";
			case PlayerType.FFplay:
				return "-i \"" + url + "\" -autoexit";
			default:
				return "\"" + url + "\"";
		}
	}

	public static bool TryPlayFallback(string url)
	{
		PlayerInfo best = FindBestPlayer();
		return Play(url, best);
	}
}
