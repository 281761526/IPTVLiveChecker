using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IPTVLiveChecker
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string encryptedMd5 = ConfigurationManager.AppSettings["ExpectedExeMd5"]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(encryptedMd5))
            {
                try
                {
                    string expectedMd5 = AesDecrypt(encryptedMd5);
                    string actualMd5 = ComputeExeMd5();
                    if (!string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            "程序文件已被修改，请重新下载官方版本。",
                            "安全警告",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show(
                        "程序文件完整性校验失败，请重新下载官方版本。",
                        "安全警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                    return;
                }
            }

            int localVersionCode = AppVersion.VersionCode;
            string currentVersion = AppVersion.Version;
            UpdateConfig config = null;

            try
            {
                string updateUrl = "https://raw.githubusercontent.com/281761526/IPTVLiveChecker/master/update.json";
                config = FetchUpdateConfig(updateUrl);

                if (config != null)
                {
                    string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
                    if (File.Exists(updaterPath))
                    {
                        if (config.VersionCode > localVersionCode)
                        {
                            if (config.IsForceUpdate)
                            {
                                if (!ShowForcedUpdateDialog(config, currentVersion))
                                    return;
                            }
                            else
                            {
                                var result = MessageBox.Show(
                                    "发现新版本" + config.LatestVersion + "\n\n" +
                                    "当前版本: " + currentVersion + "\n\n" +
                                    "更新内容:\n" + string.Join("\n", (config.Changelog ?? new List<string>()).Select(x => "  " + x)) + "\n\n" +
                                    "是否更新？",
                                    "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (result == DialogResult.Yes)
                                {
                                    StartUpdater(config.DownloadUrl, config.Md5Checksum);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("更新器丢失(Updater.exe)，无法进行更新。\n将继续使用当前版本。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("无法连接到更新服务器，请检查网络连接。\n将继续使用当前版本。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("更新检查失败，将继续使用当前版本。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            using (var mainForm = new IPTVLiveCheckerMain())
            {
                if (!mainForm.ShowDisclaimerBeforeStart())
                    return;
                Application.Run(mainForm);
            }
        }

        private static UpdateConfig FetchUpdateConfig(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string json = client.GetStringAsync(url).Result;
                    var serializer = new JavaScriptSerializer();
                    var dict = serializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict == null) return null;
                    var cfg = new UpdateConfig();
                    if (dict.ContainsKey("latestVersion")) cfg.LatestVersion = dict["latestVersion"]?.ToString() ?? "";
                    if (dict.ContainsKey("downloadUrl")) cfg.DownloadUrl = dict["downloadUrl"]?.ToString() ?? "";
                    if (dict.ContainsKey("md5Checksum")) cfg.Md5Checksum = dict["md5Checksum"]?.ToString() ?? "";
                    if (dict.ContainsKey("versionCode")) { int vc; int.TryParse(dict["versionCode"]?.ToString(), out vc); cfg.VersionCode = vc; }
                    if (dict.ContainsKey("isForceUpdate")) { bool f; bool.TryParse(dict["isForceUpdate"]?.ToString(), out f); cfg.IsForceUpdate = f; }
                    if (dict.ContainsKey("changelog") && dict["changelog"] is System.Collections.ArrayList arr)
                        cfg.Changelog = arr.Cast<object>().Select(x => x?.ToString() ?? "").ToList();
                    return cfg;
                }
            }
            catch { return null; }
        }

        private static string ComputeExeMd5()
        {
            string exePath = Application.ExecutablePath;
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(exePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        // ========== AES 加解密（用于保护 App.config 中的 MD5 值）==========
        // 密钥与 IV（256位密钥 + 128位 IV），破解者需反编译才能获取
        private static readonly byte[] AesKey = new byte[] {
            0x4D, 0x6F, 0x72, 0x65, 0x53, 0x65, 0x63, 0x72,
            0x65, 0x74, 0x4B, 0x65, 0x79, 0x21, 0x40, 0x23,
            0x58, 0x59, 0x7A, 0x61, 0x31, 0x32, 0x62, 0x63,
            0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B
        };
        private static readonly byte[] AesIV = new byte[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x30, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46
        };

        /// <summary>
        /// AES 解密（解密 App.config 中存储的加密 MD5）
        /// </summary>
        private static string AesDecrypt(string cipherTextBase64)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
            using (var aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

#if DEBUG
        /// <summary>
        /// AES 加密（仅 Debug 编译可用，用于本地调试时生成加密 MD5）
        /// 使用方式：在 Main 方法中临时调用 AesEncrypt("你的MD5值") 获取加密字符串
        /// </summary>
        public static string AesEncrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (var aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
#endif

        private static bool ShowForcedUpdateDialog(UpdateConfig cfg, string currentVersion)
        {
            bool confirmed = false;
            var dlg = new Form
            {
                Text = "强制更新",
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
                Font = new Font("Microsoft YaHei", 10f)
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
            if (cfg.Changelog != null && cfg.Changelog.Count > 0)
                changelogText += "\n" + string.Join("\n", cfg.Changelog.Select(x => "  " + x));
            else
                changelogText += "\n  暂无详细更新说明";
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
                Text = "此版本为强制更新，请点击更新后再使用。",
                Font = new Font("Microsoft YaHei", 8.5f),
                ForeColor = Color.FromArgb(255, 150, 50),
                Location = new Point(30, 260),
                Size = new Size(400, 30),
                AutoSize = false
            });
            var btnUpdate = new Button
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
            btnUpdate.Click += (s, e) => { confirmed = true; dlg.Close(); };
            dlg.Controls.Add(btnUpdate);
            dlg.ShowDialog();
            if (confirmed)
                StartUpdater(cfg.DownloadUrl, cfg.Md5Checksum);
            return confirmed;
        }

        private static void StartUpdater(string downloadUrl, string md5 = "")
        {
            try
            {
                string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
                if (!File.Exists(updaterPath))
                {
                    MessageBox.Show("更新器丢失，无法升级。\n请重新安装。", "启动失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var psi = new System.Diagnostics.ProcessStartInfo(updaterPath,
                    "\"" + Application.ExecutablePath + "\" \"" + downloadUrl + "\" \"" + md5 + "\"")
                { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动更新程序失败: " + ex.Message, "启动失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class UpdateConfig
    {
        public string LatestVersion = "";
        public string DownloadUrl = "";
        public string Md5Checksum = "";
        public int VersionCode = 0;
        public bool IsForceUpdate = false;
        public List<string> Changelog = new List<string>();
    }
}