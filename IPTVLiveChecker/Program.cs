using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IPTVLiveChecker;

internal static class Program
{
	private const string Md5Signature = "IPTV_MD5_V1____";

	private const int Md5EmbeddedBase64Len = 64;

	private static readonly int Md5SignatureLen = "IPTV_MD5_V1____".Length;

	private const string SingleInstanceMutexName = "Global\\IPTVLiveChecker_SingleInstance_7C3F9A21";

	private static Mutex _singleInstanceMutex;

	[STAThread]
	private static void Main()
	{
		// 单实例限制：同一时间仅允许一个进程运行，降低内存占用
		try
		{
			_singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
			if (!createdNew)
			{
				MessageBox.Show("程序已在运行中，请勿重复启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
		}
		catch
		{
			// 互斥体创建失败（如权限不足）时不阻止启动
		}

		// 初始化播放器诊断日志
		PlayerLogger.Initialize();
		PlayerLogger.Write("SYSTEM", $"程序启动 | StartupPath={Application.StartupPath}");
		// 在任何 LibVLCSharp 代码之前设置 VLC 运行时环境变量。
		// LibVLCSharp 的 LibVLC 静态构造函数会读取 LIBVLC_WIN32_LIBVLC_PATH 来定位 libvlc.dll，
		// 必须在首次引用 LibVLC 类型之前就设置好，否则 DllImport 找不到子目录中的 libvlc.dll。
		try
		{
			string libvlcPath = Path.Combine(Application.StartupPath, "libvlc");
			if (Directory.Exists(libvlcPath))
			{
				Environment.SetEnvironmentVariable("LIBVLC_WIN32_LIBVLC_PATH", libvlcPath);
				string pluginPath = Path.Combine(libvlcPath, "plugins");
				if (Directory.Exists(pluginPath))
				{
					Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginPath);
				}
				string path = Environment.GetEnvironmentVariable("PATH") ?? "";
				if (!path.Contains(libvlcPath))
				{
					Environment.SetEnvironmentVariable("PATH", libvlcPath + Path.PathSeparator + path);
				}
			}
		}
		catch
		{
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		DarkMessageBox.IsDarkProvider = IsSystemDarkMode;
		DarkMessageBox.ThemeProvider = () => AppTheme.ResolveFollowSystem(AppConstants.DefaultThemeName);
		try
		{
			DarkMessageBox.IconProvider = () => Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
		string encryptedMd5 = ReadEmbeddedMd5();
		if (!string.IsNullOrEmpty(encryptedMd5))
		{
			try
			{
				string expectedMd5 = AesDecrypt(encryptedMd5);
				if (!string.Equals(ComputeExeMd5(), expectedMd5, StringComparison.OrdinalIgnoreCase))
				{
					DarkMessageBox.Show("程序文件已被修改，请重新下载官方版本。", "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			catch (FormatException)
			{
				DarkMessageBox.Show("程序完整性校验失败，配置数据格式错误。", "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			catch (CryptographicException)
			{
				DarkMessageBox.Show("程序完整性校验失败，验证数据损坏。", "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			catch (Exception ex3)
			{
				DarkMessageBox.Show("程序文件完整性校验失败：" + ex3.Message, "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			using (IPTVLiveCheckerMain mainForm = new IPTVLiveCheckerMain())
			{
				if (mainForm.ShowDisclaimerBeforeStart())
				{
					// 更新检查移至后台线程，避免同步网络请求（8s×N 镜像超时）在主窗口创建前阻塞启动
					mainForm.Load += (s, e) => BeginUpdateCheck(mainForm);
					Application.Run(mainForm);
				}
			}
			// P0 清理：显式释放单实例互斥体，避免快速重启 / 崩溃重开时 1~5 秒"已在运行"的误判。
			// 操作系统会在进程终止时回收互斥体，但 Win32 Mutex 的 Global\\ 命名槽位在 .NET Finalizer 排队时可能留一段短暂窗口期，
			// 这里用 ReleaseMutex + Dispose 确保立即归还，完全关闭重开的体验间隙。
			try
			{
				if (_singleInstanceMutex != null)
				{
					_singleInstanceMutex.ReleaseMutex();
					_singleInstanceMutex.Dispose();
					_singleInstanceMutex = null;
				}
			}
			catch
			{
				// 未持有 / 已释放的异常都忽略，不影响正常退出
			}
			return;
		}
#if DEBUG
		using (IPTVLiveCheckerMain mainForm2 = new IPTVLiveCheckerMain())
		{
			if (mainForm2.ShowDisclaimerBeforeStart())
			{
				Application.Run(mainForm2);
			}
		}
		try
		{
			if (_singleInstanceMutex != null)
			{
				_singleInstanceMutex.ReleaseMutex();
				_singleInstanceMutex.Dispose();
				_singleInstanceMutex = null;
			}
		}
		catch { }
#else
		DarkMessageBox.Show("程序文件不完整，缺少完整性校验数据。", "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
#endif
	}

	internal static UpdateConfig FetchUpdateConfig(string url, int timeoutSeconds = 15)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing request)
		//IL_0006: Expected O, but got Unknown
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
				// 使用 GetAwaiter().GetResult() 避免在 STAThread 上同步阻塞异步方法导致的死锁
				string json = client.GetStringAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
				Dictionary<string, object> dict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
				if (dict == null)
				{
					return null;
				}
				UpdateConfig cfg = new UpdateConfig();
				if (dict.ContainsKey("latestVersion"))
				{
					cfg.LatestVersion = dict["latestVersion"]?.ToString() ?? "";
				}
				if (dict.ContainsKey("downloadUrl"))
				{
					cfg.DownloadUrl = dict["downloadUrl"]?.ToString() ?? "";
				}
				if (dict.ContainsKey("md5Checksum"))
				{
					cfg.Md5Checksum = dict["md5Checksum"]?.ToString() ?? "";
				}
				if (dict.ContainsKey("versionCode"))
				{
					int.TryParse(dict["versionCode"]?.ToString(), out var vc);
					cfg.VersionCode = vc;
				}
				if (dict.ContainsKey("isForceUpdate"))
				{
					bool.TryParse(dict["isForceUpdate"]?.ToString(), out var f);
					cfg.IsForceUpdate = f;
				}
				if (dict.ContainsKey("changelog") && dict["changelog"] is ArrayList arr)
				{
					cfg.Changelog = (from object x in arr
						select x?.ToString() ?? "").ToList();
				}
				return cfg;
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch
		{
			return null;
		}
	}

	private static string ComputeExeMd5()
	{
		string exePath = Application.ExecutablePath;
		using MD5 md5 = MD5.Create();
		using FileStream stream = File.OpenRead(exePath);
		long totalLen = stream.Length;
		long payloadLen = totalLen;
		byte[] sig = new byte[Md5SignatureLen];
		if (totalLen >= Md5SignatureLen + 64)
		{
			stream.Seek(totalLen - Md5SignatureLen, SeekOrigin.Begin);
			stream.Read(sig, 0, Md5SignatureLen);
			if (Encoding.ASCII.GetString(sig) == "IPTV_MD5_V1____")
			{
				payloadLen = totalLen - Md5SignatureLen - 64;
			}
		}
		stream.Seek(0L, SeekOrigin.Begin);
		byte[] buf = new byte[8192];
		long remaining = payloadLen;
		while (remaining > 0)
		{
			int toRead = (int)Math.Min(buf.Length, remaining);
			int read = stream.Read(buf, 0, toRead);
			if (read <= 0)
			{
				break;
			}
			md5.TransformBlock(buf, 0, read, buf, 0);
			remaining -= read;
		}
		md5.TransformFinalBlock(new byte[0], 0, 0);
		return BitConverter.ToString(md5.Hash).Replace("-", "").ToUpperInvariant();
	}

	private static string ReadEmbeddedMd5()
	{
		try
		{
			using FileStream fs = File.OpenRead(Application.ExecutablePath);
			long totalLen = fs.Length;
			if (totalLen < Md5SignatureLen + 64)
			{
				return null;
			}
			fs.Seek(totalLen - Md5SignatureLen, SeekOrigin.Begin);
			byte[] sig = new byte[Md5SignatureLen];
			fs.Read(sig, 0, Md5SignatureLen);
			if (Encoding.ASCII.GetString(sig) != "IPTV_MD5_V1____")
			{
				return null;
			}
			fs.Seek(totalLen - Md5SignatureLen - 64, SeekOrigin.Begin);
			byte[] b64 = new byte[64];
			fs.Read(b64, 0, 64);
			return Encoding.ASCII.GetString(b64).Trim();
		}
		catch
		{
			return null;
		}
	}

	private static byte[] GetAesKey()
	{
		byte[] part1 = Encoding.UTF8.GetBytes("MoreSec");
		byte[] part2 = Encoding.UTF8.GetBytes("retKey12");
		byte[] part3 = Encoding.UTF8.GetBytes("!@#XYZabc");
		byte[] part4 = Encoding.UTF8.GetBytes("12defghi");
		byte[] key = new byte[32];
		Buffer.BlockCopy(part1, 0, key, 0, part1.Length);
		Buffer.BlockCopy(part2, 0, key, part1.Length, part2.Length);
		Buffer.BlockCopy(part3, 0, key, part1.Length + part2.Length, part3.Length);
		Buffer.BlockCopy(part4, 0, key, part1.Length + part2.Length + part3.Length, part4.Length);
		for (int i = 0; i < key.Length; i++)
		{
			key[i] ^= 0x5A;
		}
		return key;
	}

	private static byte[] GetAesIV()
	{
		byte[] part1 = Encoding.UTF8.GetBytes("12345678");
		byte[] part2 = Encoding.UTF8.GetBytes("90ABCDEF");
		byte[] iv = new byte[16];
		Buffer.BlockCopy(part1, 0, iv, 0, part1.Length);
		Buffer.BlockCopy(part2, 0, iv, part1.Length, part2.Length);
		for (int i = 0; i < iv.Length; i++)
		{
			iv[i] ^= 0x39;
		}
		return iv;
	}

	private static string AesDecrypt(string cipherTextBase64)
	{
		byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
		using Aes aes = Aes.Create();
		aes.Key = GetAesKey();
		aes.IV = GetAesIV();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform decryptor = aes.CreateDecryptor();
		using MemoryStream ms = new MemoryStream(cipherBytes);
		using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
		using StreamReader sr = new StreamReader(cs, Encoding.UTF8);
		return sr.ReadToEnd();
	}

	private static bool IsSystemDarkMode()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			if (key != null)
			{
				object value = key.GetValue("AppsUseLightTheme");
				if (value != null)
				{
					return (int)value == 0;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	[System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	private static bool ShowForcedUpdateDialog(UpdateConfig cfg, string currentVersion)
	{
		bool confirmed = false;

		// DPI 缩放
		float dpiScale = 1f;
		using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
		{
			dpiScale = g.DpiX / 96f;
		}
		int SX(int v) => (int)Math.Round(v * dpiScale);

		// 从 AppTheme 获取主题（ActiveTheme 会自动检测系统明暗模式）
		AppTheme theme = AppTheme.ActiveTheme;
		bool isDark = DrawingUtils.IsDarkColor(theme.Bg);

		// 主题色派生
		Color bg = theme.Bg;
		Color textPrimary = theme.TextPrimary;
		Color textSecondary = theme.TextSecondary;
		Color accent = theme.Primary;
		Color accentText = DrawingUtils.IsDarkColor(theme.Primary) ? Color.White : Color.Black;
		Color btnSecondary = theme.Surface;
		Color btnSecondaryText = theme.TextPrimary;
		Color warnColor = theme.WarnColor;
		string fontFamily = theme.FontFamily ?? "Microsoft YaHei UI";

		Font titleFont = new Font(fontFamily, 15f * dpiScale, FontStyle.Bold);
		Font versionFont = new Font(fontFamily, 9.5f * dpiScale);
		Font changelogFont = new Font(fontFamily, 9f * dpiScale);
		Font warningFont = new Font(fontFamily, 8.5f * dpiScale);
		Font btnFont = new Font(fontFamily, 10.5f * dpiScale, FontStyle.Bold);
		Font btnExitFont = new Font(fontFamily, 10.5f * dpiScale);

		int dlgW = SX(460);
		int dlgH = SX(390);
		int padX = SX(28);

		Form dlg = new Form
		{
			Text = "发现新版本 " + cfg.LatestVersion,
			StartPosition = FormStartPosition.CenterScreen,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			MaximizeBox = false,
			MinimizeBox = false,
			ControlBox = false,
			ShowInTaskbar = true,
			TopMost = true,
			ClientSize = new Size(dlgW, dlgH),
			BackColor = bg,
			ForeColor = textPrimary,
			Font = new Font(fontFamily, 9.5f * dpiScale),
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
		};

		// DWM: 圆角 + 深色/浅色标题栏
		try
		{
			int cornerPref = 2; // DWMWCP_ROUND
			DwmSetWindowAttribute(dlg.Handle, 33, ref cornerPref, 4);
			int darkVal = isDark ? 1 : 0;
			DwmSetWindowAttribute(dlg.Handle, 20, ref darkVal, 4);
		}
		catch { }

		int y = SX(22);
		dlg.Controls.Add(new Label
		{
			Text = "发现新版本",
			Font = titleFont,
			ForeColor = accent,
			Location = new Point(padX, y),
			AutoSize = true
		});
		y += SX(38);
		dlg.Controls.Add(new Label
		{
			Text = "当前版本: " + currentVersion + "  →  新版本: " + cfg.LatestVersion,
			Font = versionFont,
			ForeColor = textSecondary,
			Location = new Point(padX, y),
			AutoSize = true
		});
		y += SX(30);

		string changelogText = "更新内容:";
		if (cfg.Changelog != null && cfg.Changelog.Count > 0)
			changelogText += "\n" + string.Join("\n", cfg.Changelog.Select(x => "  " + x));
		else
			changelogText += "\n  暂无详细更新说明";
		dlg.Controls.Add(new Label
		{
			Text = changelogText,
			Font = changelogFont,
			ForeColor = textPrimary,
			Location = new Point(padX, y),
			Size = new Size(dlgW - padX * 2, SX(155))
		});
		y += SX(165);

		dlg.Controls.Add(new Label
		{
			Text = "检测到新版本，请点击更新后再使用。",
			Font = warningFont,
			ForeColor = warnColor,
			Location = new Point(padX, y),
			Size = new Size(dlgW - padX * 2, SX(30))
		});
		y += SX(40);

		int btnW = SX(155);
		int btnH = SX(40);
		int btnGap = SX(14);
		int totalBtnW = btnW * 2 + btnGap;
		int btnStartX = (dlgW - totalBtnW) / 2;

		Button btnUpdate = new Button
		{
			Text = "立即更新",
			Font = btnFont,
			Location = new Point(btnStartX, y),
			Size = new Size(btnW, btnH),
			FlatStyle = FlatStyle.Flat,
			BackColor = accent,
			ForeColor = accentText,
			Cursor = Cursors.Hand
		};
		btnUpdate.FlatAppearance.BorderSize = 0;
		btnUpdate.Click += delegate { confirmed = true; dlg.Close(); };

		Button btnExit = new Button
		{
			Text = "退出程序",
			Font = btnExitFont,
			Location = new Point(btnStartX + btnW + btnGap, y),
			Size = new Size(btnW, btnH),
			FlatStyle = FlatStyle.Flat,
			BackColor = btnSecondary,
			ForeColor = btnSecondaryText,
			Cursor = Cursors.Hand
		};
		btnExit.FlatAppearance.BorderSize = 0;
		btnExit.Click += delegate { confirmed = false; dlg.Close(); };

		dlg.Controls.Add(btnUpdate);
		dlg.Controls.Add(btnExit);
		dlg.ShowDialog();
		if (confirmed)
		{
			StartUpdater(cfg.DownloadUrl, cfg.Md5Checksum);
		}
		return confirmed;
	}

	internal static string BuildUpdateMessage(UpdateConfig config)
	{
		return $"发现新版本 {config.LatestVersion}\n\n当前版本: {AppConstants.CurrentVersion}\n\n更新内容:\n" + string.Join("\n", config.Changelog.Select(c => "  " + c));
	}

	/// <summary>在后台线程执行版本检查，完全不阻塞主窗口启动；发现新版本时回到 UI 线程弹窗。</summary>
	internal static void BeginUpdateCheck(Form owner)
	{
		Task.Run(() =>
		{
			try
			{
				UpdateConfig config = null;
				foreach (string url in AppConstants.UpdateMirrors)
				{
					config = FetchUpdateConfig(url, 8);
					if (config != null) break;
				}
				if (config == null || config.VersionCode <= AppConstants.CurrentVersionCode) return;
				if (!File.Exists(Path.Combine(Application.StartupPath, "Updater.exe")))
				{
					owner.Invoke(new Action(() =>
						DarkMessageBox.Show(BuildUpdateMessage(config) + "\n\n注意: 更新程序(Updater.exe)已丢失，无法自动更新。\n请重新下载完整版本后再使用。", "发现新版本", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)));
					return;
				}
				if (config.IsForceUpdate)
				{
					owner.Invoke(new Action(() => ShowForcedUpdateDialog(config, AppConstants.CurrentVersion)));
					return;
				}
				DialogResult r = (DialogResult)owner.Invoke(new Func<DialogResult>(() =>
					DarkMessageBox.Show(BuildUpdateMessage(config) + "\n\n是否更新？", "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk)));
				if (r == DialogResult.Yes)
				{
					owner.Invoke(new Action(() => StartUpdater(config.DownloadUrl, config.Md5Checksum)));
				}
			}
			catch
			{
			}
		});
	}

	internal static void StartUpdater(string downloadUrl, string md5 = "")
	{
		try
		{
			string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
			if (!File.Exists(updaterPath))
			{
				DarkMessageBox.Show("更新器丢失，无法升级。\n请重新安装。", "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			Process.Start(new ProcessStartInfo(updaterPath, "\"" + Application.ExecutablePath + "\" \"" + downloadUrl + "\" \"" + md5 + "\"")
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			DarkMessageBox.Show("启动更新程序失败: " + ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}
}
