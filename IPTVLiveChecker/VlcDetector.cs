using System;
using System.IO;
using Microsoft.Win32;

namespace IPTVLiveChecker;

internal static class VlcDetector
{
	public const string DownloadUrl = "https://www.videolan.org/vlc/";

	public const string DownloadUrlWin64 = "https://get.videolan.org/vlc/3.0.21/win64/vlc-3.0.21-win64.exe";

	public const string DownloadUrlWin32 = "https://get.videolan.org/vlc/3.0.21/win32/vlc-3.0.21-win32.exe";

	public static bool IsVlcInstalled()
	{
		if (CheckRegistryVlc(@"SOFTWARE\VideoLAN\VLC"))
		{
			return true;
		}
		if (CheckRegistryVlc(@"SOFTWARE\WOW6432Node\VideoLAN\VLC"))
		{
			return true;
		}
		string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		string[] array = new string[4]
		{
			Path.Combine(programFiles, "VideoLAN", "VLC", "vlc.exe"),
			Path.Combine(programFilesX86, "VideoLAN", "VLC", "vlc.exe"),
			@"C:\Program Files\VideoLAN\VLC\vlc.exe",
			@"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
		};
		string[] array2 = array;
		foreach (string path in array2)
		{
			if (File.Exists(path))
			{
				return true;
			}
		}
		string pathEnv = Environment.GetEnvironmentVariable("PATH");
		if (!string.IsNullOrEmpty(pathEnv))
		{
			string[] dirs = pathEnv.Split(Path.PathSeparator);
			foreach (string dir in dirs)
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir.Trim('"'), "vlc.exe")))
					{
						return true;
					}
				}
				catch
				{
				}
			}
		}
		return false;
	}

	public static string GetInstalledVlcPath()
	{
		string path = GetRegistryVlcPath(@"SOFTWARE\VideoLAN\VLC");
		if (!string.IsNullOrEmpty(path))
		{
			return path;
		}
		path = GetRegistryVlcPath(@"SOFTWARE\WOW6432Node\VideoLAN\VLC");
		if (!string.IsNullOrEmpty(path))
		{
			return path;
		}
		string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		string[] array = new string[4]
		{
			Path.Combine(programFiles, "VideoLAN", "VLC", "vlc.exe"),
			Path.Combine(programFilesX86, "VideoLAN", "VLC", "vlc.exe"),
			@"C:\Program Files\VideoLAN\VLC\vlc.exe",
			@"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
		};
		foreach (string p in array)
		{
			if (File.Exists(p))
			{
				return p;
			}
		}
		return null;
	}

	private static bool CheckRegistryVlc(string subKey)
	{
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);
			if (key != null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string GetRegistryVlcPath(string subKey)
	{
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);
			if (key != null)
			{
				string installDir = key.GetValue("InstallDir") as string;
				if (!string.IsNullOrEmpty(installDir))
				{
					string vlcPath = Path.Combine(installDir, "vlc.exe");
					if (File.Exists(vlcPath))
					{
						return vlcPath;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
