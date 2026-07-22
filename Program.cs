using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

            const string updateUrl = "https://cdn.jsdelivr.net/gh/281761526/IPTVLiveChecker@master/update.json";
            UpdateConfig config = FetchUpdateConfig(updateUrl);
            if (config == null)
            {
                MessageBox.Show("无法连接更新服务器，请检查网络后重试。\n程序即将退出。", "网络错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(config.Md5Checksum))
            {
                if (!VerifyExeMd5(config.Md5Checksum))
                {
                    MessageBox.Show("程序完整性校验失败！\n文件可能已被篡改或损坏。\n程序即将退出。", "安全警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
            if (!File.Exists(updaterPath))
            {
                MessageBox.Show("更新组件缺失(Updater.exe)，程序无法正常运行。\n请重新安装。", "组件缺失",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(config.LatestVersion))
            {
                string currentVersion = "Beta 1.0";
                if (config.LatestVersion != currentVersion)
                {
                    if (!ShowForcedUpdateDialog(config, currentVersion))
                        return;
                    StartUpdater(config.DownloadUrl);
                    return;
                }
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
                    if (dict.ContainsKey("isForceUpdate")) { bool f; bool.TryParse(dict["isForceUpdate"]?.ToString(), out f); cfg.IsForceUpdate = f; }
                    if (dict.ContainsKey("changelog") && dict["changelog"] is List<object> list)
                        cfg.Changelog = list.Select(x => x?.ToString() ?? "").ToList();
                    return cfg;
                }
            }
            catch { return null; }
        }

        private static bool VerifyExeMd5(string expectedMd5)
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(exePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string actualMd5 = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                    return string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

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
                Text = "当前版本: " + currentVersion + "  ->  最新版本: " + cfg.LatestVersion,
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(160, 168, 185),
                Location = new Point(30, 65),
                AutoSize = true
            });
            string changelogText = "更新内容:";
            if (cfg.Changelog != null && cfg.Changelog.Count > 0)
                changelogText += "\n" + string.Join("\n", cfg.Changelog.Select(x => "  " + x));
            else
                changelogText += "\n  请更新到最新版本以继续使用";
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
                Text = "此更新为强制更新，必须升级后才能继续使用。",
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
            return confirmed;
        }

        private static void StartUpdater(string downloadUrl)
        {
            try
            {
                string updaterPath = Path.Combine(Application.StartupPath, "Updater.exe");
                if (!File.Exists(updaterPath))
                {
                    MessageBox.Show("更新组件缺失，无法完成更新。\n请重新安装程序。", "更新失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var psi = new System.Diagnostics.ProcessStartInfo(updaterPath,
                    "\"" + Application.ExecutablePath + "\" \"" + downloadUrl + "\"")
                { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动更新程序失败: " + ex.Message, "更新失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class UpdateConfig
    {
        public string LatestVersion = "";
        public string DownloadUrl = "";
        public string Md5Checksum = "";
        public bool IsForceUpdate = false;
        public List<string> Changelog = new List<string>();
    }
}
