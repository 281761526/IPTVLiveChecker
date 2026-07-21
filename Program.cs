using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPTVLiveChecker
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 启动前自校验：验证主程序完整性，防止被篡改
            if (!VerifySelfIntegrity())
            {
                MessageBox.Show("程序文件已被破坏或篡改，无法继续运行。\n请重新下载安装。", "安全警告",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new IPTVLiveCheckerMain());
        }

        /// <summary>
        /// 自校验主程序 MD5，防止程序被反编译修改后运行
        /// 预期 MD5 存储在 App.config 的 appSettings 中
        /// </summary>
        private static bool VerifySelfIntegrity()
        {
            try
            {
                string expectedMd5 = ConfigurationManager.AppSettings["ExpectedExeMd5"];
                if (string.IsNullOrEmpty(expectedMd5))
                    return true; // 未配置则不校验

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
}
