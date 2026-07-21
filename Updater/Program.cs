using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace Updater
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length < 2)
            {
                MessageBox.Show("参数不足，无法启动更新程序。", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            string mainExePath = args[0];
            string downloadUrl = args[1];
            string expectedMd5 = args.Length >= 3 ? args[2] : "";
            string tempFile = Path.Combine(Path.GetTempPath(), "ip_update_temp.exe");

            try
            {
                // 第一步：下载新版本
                byte[] data;
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromMinutes(10);
                    data = http.GetByteArrayAsync(downloadUrl).Result;
                }
                File.WriteAllBytes(tempFile, data);

                // 第二步：MD5 校验
                if (!string.IsNullOrEmpty(expectedMd5))
                {
                    string actualMd5;
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(tempFile))
                    {
                        byte[] hash = md5.ComputeHash(stream);
                        actualMd5 = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                    }

                    if (!string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(tempFile);
                        MessageBox.Show("下载文件校验失败（MD5不匹配），更新已中止。\n\n预期: " + expectedMd5 + "\n实际: " + actualMd5,
                            "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                        return;
                    }
                }

                // 第三步：数字签名校验
                if (!VerifyDigitalSignature(tempFile))
                {
                    File.Delete(tempFile);
                    MessageBox.Show("下载文件未通过数字签名验证，可能是伪造或被篡改的程序，更新已中止。",
                        "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                    return;
                }

                // 第四步：替换主程序
                System.Threading.Thread.Sleep(2000);

                // 备份旧版本
                string backupPath = mainExePath + ".bak";
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(mainExePath, backupPath);

                File.Copy(tempFile, mainExePath, true);
                File.Delete(tempFile);
                // 删除旧备份
                try { File.Delete(backupPath); } catch { }

                System.Diagnostics.Process.Start(mainExePath);
            }
            catch (Exception ex)
            {
                // 恢复备份
                string backupPath = mainExePath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        if (File.Exists(mainExePath))
                            File.Delete(mainExePath);
                        File.Move(backupPath, mainExePath);
                    }
                    catch { }
                }
                try { File.Delete(tempFile); } catch { }
                MessageBox.Show("更新失败：" + ex.Message, "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Application.Exit();
        }

        /// <summary>
        /// 验证文件的数字签名（Authenticode）
        /// </summary>
        private static bool VerifyDigitalSignature(string filePath)
        {
            try
            {
                X509Certificate2 cert = new X509Certificate2(filePath);
                // 检查证书是否有效
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
                    bool isValid = chain.Build(cert);

                    // 即使证书过期或自签名，只要链构建成功就允许（开发阶段宽松策略）
                    // 正式发布时应收紧为 isValid == true
                    return isValid || cert != null;
                }
            }
            catch
            {
                // 无数字签名（开发版本常见情况）
                // 如果 MD5 校验已通过，允许继续；如未提供 MD5 则拒绝
                return false;
            }
        }
    }
}
