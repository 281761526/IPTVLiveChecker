using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPTVLiveChecker;

internal static class VlcSetup
{
	private static readonly string LibVlcPath = Path.Combine(Application.StartupPath, "libvlc");

	private static readonly string InitFlagFile = Path.Combine(LibVlcPath, ".initialized");

	public static string GetLibVlcPath()
	{
		return LibVlcPath;
	}

	public static bool IsLibVlcReady()
	{
		return File.Exists(Path.Combine(LibVlcPath, "libvlc.dll"))
			&& File.Exists(Path.Combine(LibVlcPath, "libvlccore.dll"))
			&& Directory.Exists(Path.Combine(LibVlcPath, "plugins"));
	}

	public static bool IsFirstRunChecked()
	{
		return File.Exists(InitFlagFile);
	}

	public static void EnsureLibVlcEnvironment()
	{
		PlayerLogger.Write("VLC", $"EnsureLibVlcEnvironment 开始 | LibVlcPath={LibVlcPath} | IsLibVlcReady={IsLibVlcReady()}");
		// 1) 先校验完整运行时是否就绪。未就绪时，尝试从已安装 VLC 补齐。
		if (!IsLibVlcReady() && VlcDetector.IsVlcInstalled())
		{
			PlayerLogger.Write("VLC", "运行时不完整，尝试从已安装VLC复制");
			try
			{
				CopyFromInstalledVlc();
			}
			catch
			{
			}
		}
		if (!IsLibVlcReady())
		{
			PlayerLogger.Write("VLC", "运行时仍不完整，抛出异常");
			throw new InvalidOperationException("libvlc 运行时不完整：缺少 libvlc.dll / libvlccore.dll / plugins 目录。请先安装 VLC 播放器或重新运行 VLC 安装向导。");
		}
		// 2) 把 libvlc 目录加到 PATH，确保依赖 DLL 能被找到
		string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
		if (!path.Contains(LibVlcPath))
		{
			Environment.SetEnvironmentVariable("PATH", LibVlcPath + Path.PathSeparator + path);
		}
		Environment.SetEnvironmentVariable("LIBVLC_WIN32_LIBVLC_PATH", LibVlcPath);
		string pluginPath = Path.Combine(LibVlcPath, "plugins");
		if (Directory.Exists(pluginPath))
		{
			string existing = Environment.GetEnvironmentVariable("VLC_PLUGIN_PATH");
			if (string.IsNullOrEmpty(existing))
			{
				Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginPath);
			}
		}
		// 3) 强制加载核心 dll，验证依赖链是否完整。
		// 如果 LoadLibrary 失败（缺少 MinGW 运行时库等），尝试从已安装 VLC 重新复制所有 DLL。
		IntPtr hCore = LoadLibrary(Path.Combine(LibVlcPath, "libvlccore.dll"));
		PlayerLogger.Write("VLC", $"LoadLibrary(libvlccore.dll) | handle={hCore}");
		if (hCore == IntPtr.Zero && VlcDetector.IsVlcInstalled())
		{
			PlayerLogger.Write("VLC", "libvlccore.dll加载失败，重新尝试从已安装VLC复制");
			try
			{
				CopyFromInstalledVlc();
			}
			catch
			{
			}
			hCore = LoadLibrary(Path.Combine(LibVlcPath, "libvlccore.dll"));
		}
		if (hCore == IntPtr.Zero)
		{
			int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			PlayerLogger.Write("VLC", $"libvlccore.dll最终加载失败 | error={err}");
			throw new InvalidOperationException(
				"无法加载 libvlccore.dll（错误码 " + err + "）。" +
				"可能缺少依赖 DLL（libgcc_s_seh-1.dll / libwinpthread-1.dll / libstdc++-6.dll）。" +
				"请删除 libvlc 文件夹后重启程序，或重新安装 VLC。");
		}
		IntPtr hVlc = LoadLibrary(Path.Combine(LibVlcPath, "libvlc.dll"));
		PlayerLogger.Write("VLC", $"LoadLibrary(libvlc.dll) | handle={hVlc}");
		if (hVlc == IntPtr.Zero)
		{
			int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			PlayerLogger.Write("VLC", $"libvlc.dll加载失败 | error={err}");
			throw new InvalidOperationException(
				"无法加载 libvlc.dll（错误码 " + err + "）。" +
				"可能缺少依赖 DLL。请删除 libvlc 文件夹后重启程序。");
		}
		PlayerLogger.Write("VLC", "EnsureLibVlcEnvironment 完成，所有DLL加载成功");
	}

	[System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr LoadLibrary(string lpFileName);

	public static bool CopyFromInstalledVlc()
	{
		string vlcExe = VlcDetector.GetInstalledVlcPath();
		if (string.IsNullOrEmpty(vlcExe) || !File.Exists(vlcExe))
		{
			return false;
		}
		string vlcDir = Path.GetDirectoryName(vlcExe);
		if (!Directory.Exists(vlcDir))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(LibVlcPath);
			// 复制 VLC 安装目录中的所有 DLL（包括 libvlc.dll、libvlccore.dll
			// 以及它们依赖的 MinGW 运行时库：libgcc_s_seh-1.dll、libwinpthread-1.dll、
			// libstdc++-6.dll 等。缺少这些依赖 DLL 会导致 LoadLibrary 失败，
			// VLC 无法初始化，播放器完全不可用。）
			foreach (string dll in Directory.GetFiles(vlcDir, "*.dll"))
			{
				string name = Path.GetFileName(dll);
				CopyFileSafe(dll, Path.Combine(LibVlcPath, name));
			}
			// 复制 vlc.exe（fallback 外部播放器需要）
			CopyFileSafe(Path.Combine(vlcDir, "vlc.exe"), Path.Combine(LibVlcPath, "vlc.exe"));
			// 复制 plugins 目录（解码器、解复用器、网络协议等）
			CopyDirectory(Path.Combine(vlcDir, "plugins"), Path.Combine(LibVlcPath, "plugins"));
			TouchInitFlag();
			return IsLibVlcReady();
		}
		catch
		{
			return false;
		}
	}

	public static async Task<bool> EnsureLibVlcAsync()
	{
		if (IsLibVlcReady())
		{
			return true;
		}
		if (VlcDetector.IsVlcInstalled())
		{
			return await Task.Run(() => CopyFromInstalledVlc());
		}
		return false;
	}

	public static async Task<bool> DownloadAndInstallAsync(IProgress<(int, string)> progress = null)
	{
		string tempFile = Path.Combine(Path.GetTempPath(), "vlc-installer-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".exe");
		try
		{
			progress?.Report((0, "正在下载 VLC 安装包..."));
			bool ok = await DownloadInstallerAsync(tempFile, progress);
			if (!ok)
			{
				return false;
			}
			progress?.Report((100, "正在安装到 libvlc 文件夹..."));
			bool installed = await Task.Run(() => RunSilentInstall(tempFile));
			if (!installed)
			{
				return false;
			}
			TouchInitFlag();
			progress?.Report((100, "安装完成"));
			return IsLibVlcReady();
		}
		finally
		{
			try
			{
				if (File.Exists(tempFile))
				{
					File.Delete(tempFile);
				}
			}
			catch
			{
			}
		}
	}

	private static async Task<bool> DownloadInstallerAsync(string dstFile, IProgress<(int, string)> progress)
	{
		try
		{
			using HttpClient client = new HttpClient();
			client.Timeout = TimeSpan.FromMinutes(30);
			using HttpResponseMessage response = await client.GetAsync(VlcDetector.DownloadUrlWin64, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();
			long? total = response.Content.Headers.ContentLength;
			using Stream src = await response.Content.ReadAsStreamAsync();
			using FileStream dst = File.Create(dstFile);
			byte[] buf = new byte[81920];
			long read = 0L;
			int n;
			while ((n = await src.ReadAsync(buf, 0, buf.Length)) > 0)
			{
				await dst.WriteAsync(buf, 0, n);
				read += n;
				if (total.HasValue && total.Value > 0)
				{
					int pct = (int)(read * 100L / total.Value);
					progress?.Report((pct, "正在下载 VLC 安装包... " + pct + "%"));
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool RunSilentInstall(string installerPath)
	{
		try
		{
			Directory.CreateDirectory(LibVlcPath);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = installerPath,
				Arguments = "/S /D=" + LibVlcPath,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			Process p = Process.Start(psi);
			if (p == null)
			{
				return false;
			}
			p.WaitForExit(300000);
			return IsLibVlcReady();
		}
		catch
		{
			return false;
		}
	}

	public static void TouchInitFlag()
	{
		try
		{
			Directory.CreateDirectory(LibVlcPath);
			File.WriteAllText(InitFlagFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		}
		catch
		{
		}
	}

	private static void CopyFileSafe(string src, string dst)
	{
		if (File.Exists(src))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(dst));
			File.Copy(src, dst, true);
		}
	}

	private static void CopyDirectory(string srcDir, string dstDir)
	{
		if (!Directory.Exists(srcDir))
		{
			return;
		}
		Directory.CreateDirectory(dstDir);
		DirectoryInfo info = new DirectoryInfo(srcDir);
		FileInfo[] files = info.GetFiles("*", SearchOption.AllDirectories);
		foreach (FileInfo file in files)
		{
			try
			{
				string rel = file.FullName.Substring(info.FullName.Length + 1);
				string dst = Path.Combine(dstDir, rel);
				Directory.CreateDirectory(Path.GetDirectoryName(dst));
				File.Copy(file.FullName, dst, true);
			}
			catch
			{
			}
		}
	}
}
