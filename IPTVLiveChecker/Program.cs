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
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IPTVLiveChecker;

internal static class Program
{
	private const string Md5Signature = "IPTV_MD5_V1____";

	private const int Md5EmbeddedBase64Len = 64;

	private static readonly int Md5SignatureLen = "IPTV_MD5_V1____".Length;

	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		DarkMessageBox.IsDarkProvider = IsSystemDarkMode;
		DarkMessageBox.ThemeProvider = () => AppTheme.GetAutoTheme();
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
			UpdateConfig config = null;
			try
			{
				string[] array = AppConstants.UpdateMirrors;
				for (int i = 0; i < array.Length; i++)
				{
					config = FetchUpdateConfig(array[i], 8);
					if (config != null)
					{
						break;
					}
				}
				if (config != null && config.VersionCode > AppConstants.CurrentVersionCode)
				{
					if (!File.Exists(Path.Combine(Application.StartupPath, "Updater.exe")))
					{
						DarkMessageBox.Show(BuildUpdateMessage(config) + "\n\n注意: 更新程序(Updater.exe)已丢失，无法自动更新。\n请重新下载完整版本后再使用。", "发现新版本", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
					if (config.IsForceUpdate)
					{
						ShowForcedUpdateDialog(config, AppConstants.CurrentVersion);
						return;
					}
					if (DarkMessageBox.Show(BuildUpdateMessage(config) + "\n\n是否更新？", "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
					{
						StartUpdater(config.DownloadUrl, config.Md5Checksum);
						return;
					}
				}
			}
			catch
			{
			}
			using IPTVLiveCheckerMain mainForm = new IPTVLiveCheckerMain();
			if (mainForm.ShowDisclaimerBeforeStart())
			{
				Application.Run(mainForm);
			}
			return;
		}
#if DEBUG
		using IPTVLiveCheckerMain mainForm2 = new IPTVLiveCheckerMain();
		if (mainForm2.ShowDisclaimerBeforeStart())
		{
			Application.Run(mainForm2);
		}
#else
		DarkMessageBox.Show("程序文件不完整，缺少完整性校验数据。", "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Hand);
#endif
	}

	internal static UpdateConfig FetchUpdateConfig(string url, int timeoutSeconds = 15)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		try
		{
			HttpClient client = new HttpClient();
			try
			{
				client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
				string json = client.GetStringAsync(url).Result;
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

	private static bool ShowForcedUpdateDialog(UpdateConfig cfg, string currentVersion)
	{
		bool confirmed = false;
		Form dlg = new Form
		{
			Text = "检测到更新",
			StartPosition = FormStartPosition.CenterScreen,
			FormBorderStyle = FormBorderStyle.FixedDialog,
			MaximizeBox = false,
			MinimizeBox = false,
			ControlBox = false,
			ShowInTaskbar = true,
			TopMost = true,
			ClientSize = new Size(460, 380),
			BackColor = Color.FromArgb(28, 32, 42),
			ForeColor = Color.FromArgb(220, 225, 235),
			Font = new Font("Microsoft YaHei", 10f),
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
		};
		dlg.Controls.Add(new Label
		{
			Text = "发现新版本",
			Font = new Font("Microsoft YaHei", 16f, FontStyle.Bold),
			ForeColor = Color.FromArgb(64, 158, 255),
			Location = new Point(30, 25),
			AutoSize = true
		});
		dlg.Controls.Add(new Label
		{
			Text = "当前版本: " + currentVersion + "  ->  新版本: " + cfg.LatestVersion,
			Font = new Font("Microsoft YaHei", 10f),
			ForeColor = Color.FromArgb(160, 168, 185),
			Location = new Point(30, 65),
			AutoSize = true
		});
		string changelogText = "更新内容:";
		changelogText = ((cfg.Changelog == null || cfg.Changelog.Count <= 0) ? (changelogText + "\n  暂无详细更新说明") : (changelogText + "\n" + string.Join("\n", cfg.Changelog.Select((string x) => "  " + x))));
		dlg.Controls.Add(new Label
		{
			Text = changelogText,
			Font = new Font("Microsoft YaHei", 9f),
			ForeColor = Color.FromArgb(180, 188, 200),
			Location = new Point(30, 100),
			Size = new Size(400, 150),
			AutoSize = false
		});
		dlg.Controls.Add(new Label
		{
			Text = "检测到新版本，请点击更新后再使用。",
			Font = new Font("Microsoft YaHei", 8.5f),
			ForeColor = Color.FromArgb(255, 150, 50),
			Location = new Point(30, 260),
			Size = new Size(400, 30),
			AutoSize = false
		});
		Button btnUpdate = new Button
		{
			Text = "立即更新",
			Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold),
			Location = new Point(140, 305),
			Size = new Size(180, 40),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(64, 158, 255),
			ForeColor = Color.White,
			Cursor = Cursors.Hand
		};
		btnUpdate.FlatAppearance.BorderSize = 0;
		btnUpdate.Click += delegate
		{
			confirmed = true;
			dlg.Close();
		};
		dlg.Controls.Add(btnUpdate);
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
