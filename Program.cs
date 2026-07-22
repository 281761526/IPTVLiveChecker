using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Web.Script.Serialization;


using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPTVLiveChecker
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点�?
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 启动前自校验：验证主程序完整性，防止被篡�?
            string uj = "https://cdn.jsdelivr.net/gh/281761526/IPTVLiveChecker@master/update.json";
            UpdateConfig uc = null;
            try { uc = FetchUpdateConfig(uj); } catch { }

            if (uc != null && !string.IsNullOrEmpty(uc.Md5Checksum))
            {
                if (!VerifyExeMd5AgainstRemote(uc.Md5Checksum))
                { MessageBox.Show("MD5 check failed", "Security", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            }
            else { if (!VerifySelfIntegrity()) { MessageBox.Show("Local check failed!", "Security", MessageBoxButtons.OK, MessageBoxIcon.Error); return; } }

            if (uc != null && !string.IsNullOrEmpty(uc.LatestVersion))
            {
                string cv = "Beta 1.0";
                if (uc.LatestVersion != cv)
                { if (!ShowForcedUpdateDialog(uc, cv)) return; StartUpdater(uc.DownloadUrl); return; }
            }

            using (var mainForm = new IPTVLiveCheckerMain())
            {
                if (!mainForm.ShowDisclaimerBeforeStart())
                    return;
                Application.Run(mainForm);
            }
        }

        /// <summary>
        /// 自校验主程序 MD5，防止程序被反编译修改后运行
        /// 预期 MD5 存储�?App.config �?appSettings �?
        /// </summary>
        private static UpdateConfig FetchUpdateConfig(string url)
        {
            using (var h = new HttpClient()) { h.Timeout = TimeSpan.FromSeconds(15); string json = h.GetStringAsync(url).Result;
            var s = new JavaScriptSerializer(); var o = s.Deserialize<Dictionary<string, object>>(json); if (o == null) return null;
            var cfg = new UpdateConfig();
            if (o.ContainsKey("latestVersion")) cfg.LatestVersion = o["latestVersion"]?.ToString() ?? "";
            if (o.ContainsKey("downloadUrl")) cfg.DownloadUrl = o["downloadUrl"]?.ToString() ?? "";
            if (o.ContainsKey("md5Checksum")) cfg.Md5Checksum = o["md5Checksum"]?.ToString() ?? "";
            if (o.ContainsKey("isForceUpdate")) { bool f; bool.TryParse(o["isForceUpdate"]?.ToString(), out f); cfg.IsForceUpdate = f; }
            if (o.ContainsKey("changelog") && o["changelog"] is List<object> l) cfg.Changelog = l.Select(x => x?.ToString() ?? "").ToList();
            return cfg; }
        }

        private static bool VerifyExeMd5AgainstRemote(string exp)
        { try { string p = Application.ExecutablePath; using (var m = MD5.Create()) using (var fs = File.OpenRead(p))
            { byte[] h = m.ComputeHash(fs); return string.Equals(BitConverter.ToString(h).Replace("-", "").ToUpperInvariant(), exp, StringComparison.OrdinalIgnoreCase); } } catch { return false; } }

        private static bool ShowForcedUpdateDialog(UpdateConfig cfg, string cv)
        {
            bool ok = false;
            var d = new Form { Text = "Update Required", StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ControlBox = true, ShowInTaskbar = true, TopMost = true, ClientSize = new System.Drawing.Size(460, 350), BackColor = System.Drawing.Color.FromArgb(28, 32, 42), ForeColor = System.Drawing.Color.FromArgb(220, 225, 235), Font = new System.Drawing.Font("Microsoft YaHei", 10f) };
            d.Controls.Add(new Label { Text = "New Version Available", Font = new System.Drawing.Font("Microsoft YaHei", 16f, FontStyle.Bold), ForeColor = System.Drawing.Color.FromArgb(64, 158, 255), Location = new System.Drawing.Point(30, 25), AutoSize = true });
            d.Controls.Add(new Label { Text = "Current: " + cv + "  ->  Latest: " + cfg.LatestVersion, Font = new System.Drawing.Font("Microsoft YaHei", 10f), ForeColor = System.Drawing.Color.FromArgb(160, 168, 185), Location = new System.Drawing.Point(30, 65), AutoSize = true });
            string cl = "Changelog:"; if (cfg.Changelog != null && cfg.Changelog.Count > 0) cl += "\n" + string.Join("\n", cfg.Changelog.Select(x => "* " + x)); else cl += "\n* Please update to continue";
            d.Controls.Add(new Label { Text = cl, Font = new System.Drawing.Font("Microsoft YaHei", 9f), ForeColor = System.Drawing.Color.FromArgb(180, 188, 200), Location = new System.Drawing.Point(30, 100), Size = new System.Drawing.Size(400, 130), AutoSize = false });
            d.Controls.Add(new Label { Text = "Mandatory update. Close = exit.", Font = new System.Drawing.Font("Microsoft YaHei", 8.5f), ForeColor = System.Drawing.Color.FromArgb(255, 150, 50), Location = new System.Drawing.Point(30, 240), Size = new System.Drawing.Size(400, 30), AutoSize = false });
            var b = new Button { Text = "Update Now", Font = new System.Drawing.Font("Microsoft YaHei", 11f, FontStyle.Bold), Location = new System.Drawing.Point(140, 285), Size = new System.Drawing.Size(180, 40), FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(64, 158, 255), ForeColor = System.Drawing.Color.White, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; b.Click += (s, e) => { ok = true; d.Close(); }; d.Controls.Add(b);
            d.ShowDialog(); return ok;
        }

        private static void StartUpdater(string dl)
        { try { string up = Path.Combine(Application.StartupPath, "Updater.exe"); if (!File.Exists(up)) { MessageBox.Show("Updater.exe not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            var psi = new System.Diagnostics.ProcessStartInfo(up, "\"" + Application.ExecutablePath + "\" \"" + dl + "\"") { UseShellExecute = true }; System.Diagnostics.Process.Start(psi); }
            catch (Exception ex) { MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } }

        private static bool VerifySelfIntegrity()
        {
            try
            {
                string expectedMd5 = ConfigurationManager.AppSettings["ExpectedExeMd5"];
                if (string.IsNullOrEmpty(expectedMd5))
                    return true; // 未配置则不校�?

                string exePath = Application.ExecutablePath;
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(exePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string actualMd5 = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                    return string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
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
