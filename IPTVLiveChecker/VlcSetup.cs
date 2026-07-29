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
		if (Directory.Exists(LibVlcPath))
		{
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
		}
	}

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
			CopyFileSafe(Path.Combine(vlcDir, "libvlc.dll"), Path.Combine(LibVlcPath, "libvlc.dll"));
			CopyFileSafe(Path.Combine(vlcDir, "libvlccore.dll"), Path.Combine(LibVlcPath, "libvlccore.dll"));
			CopyFileSafe(Path.Combine(vlcDir, "vlc.exe"), Path.Combine(LibVlcPath, "vlc.exe"));
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
