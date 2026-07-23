using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
                MessageBox.Show("参数不足，无法启动更新程序。\n用法: Updater.exe <主程序路径> <下载地址> [MD5]", "更新失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string mainExePath = args[0];
            string downloadUrl = args[1];
            string expectedMd5 = args.Length >= 3 ? args[2] : "";

            try
            {
                Task.Run(async () => await DoUpdate(mainExePath, downloadUrl, expectedMd5)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新失败：" + ex.Message, "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static async Task DoUpdate(string mainExePath, string downloadUrl, string expectedMd5)
        {
            string appDir = Path.GetDirectoryName(mainExePath) ?? ".";
            string tempDownload = Path.Combine(Path.GetTempPath(), "iptv_update_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");
            string tempExtract = Path.Combine(Path.GetTempPath(), "iptv_extract_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string backupDir = Path.Combine(Path.GetTempPath(), "iptv_backup_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            bool success = false;

            try
            {
                // 第一步：下载
                byte[] data;
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromMinutes(10);
                    data = await http.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);
                }
                File.WriteAllBytes(tempDownload, data);

                // 第二步：MD5 校验
                if (!string.IsNullOrEmpty(expectedMd5))
                {
                    string actualMd5;
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(tempDownload))
                    {
                        actualMd5 = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToUpperInvariant();
                    }

                    if (!string.Equals(actualMd5, expectedMd5, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(tempDownload);
                        MessageBox.Show("下载文件校验失败（MD5不匹配），更新已中止。\n\n预期: " + expectedMd5 + "\n实际: " + actualMd5,
                            "安全警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 第三步：判断文件类型并提取
                bool isZip = IsZipFile(tempDownload);
                if (isZip)
                {
                    Directory.CreateDirectory(tempExtract);
                    ZipFile.ExtractToDirectory(tempDownload, tempExtract);
                }

                // 第四步：等待主程序退出
                string processName = Path.GetFileNameWithoutExtension(mainExePath);
                await WaitForProcessExit(processName, 15000);

                // 第五步：备份现有文件
                Directory.CreateDirectory(backupDir);
                if (Directory.Exists(appDir))
                {
                    foreach (var file in Directory.GetFiles(appDir, "*", SearchOption.AllDirectories))
                    {
                        string relPath = GetRelativePath(appDir, file);
                        string backupPath = Path.Combine(backupDir, relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? backupDir);
                        File.Copy(file, backupPath, true);
                    }
                }

                // 第六步：安装新文件（仅覆盖ZIP中包含的文件，保护用户数据）
                if (isZip)
                {
                    // 仅删除ZIP中存在且需要替换的文件，保护用户配置和数据
                    foreach (var newFile in Directory.GetFiles(tempExtract, "*", SearchOption.AllDirectories))
                    {
                        string relPath = GetRelativePath(tempExtract, newFile);
                        string destPath = Path.Combine(appDir, relPath);

                        // 删除旧文件（如果存在）
                        if (File.Exists(destPath))
                        {
                            try { File.Delete(destPath); } catch { }
                        }

                        // 复制新文件
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? appDir);
                        File.Copy(newFile, destPath, true);
                    }
                }
                else
                {
                    // 单个 EXE：直接覆盖
                    string backupFile = mainExePath + ".bak";
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    File.Move(mainExePath, backupFile);
                    File.Copy(tempDownload, mainExePath, true);
                    try { File.Delete(backupFile); } catch { }
                }

                success = true;

                // 第七步：重启主程序
                System.Diagnostics.Process.Start(mainExePath);
            }
            catch (Exception ex)
            {
                // 恢复备份
                if (Directory.Exists(backupDir))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories))
                        {
                            string relPath = GetRelativePath(backupDir, file);
                            string destPath = Path.Combine(appDir, relPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? appDir);
                            File.Copy(file, destPath, true);
                        }
                    }
                    catch { }
                }

                throw new Exception("更新失败: " + ex.Message, ex);
            }
            finally
            {
                // 清理临时文件
                try { File.Delete(tempDownload); } catch { }
                try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
                if (success)
                {
                    try { if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true); } catch { }
                }
            }
        }

        static bool IsZipFile(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    byte[] header = new byte[4];
                    if (fs.Read(header, 0, 4) < 4) return false;
                    // ZIP magic: PK\x03\x04
                    return header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
                }
            }
            catch { return false; }
        }

        static async Task WaitForProcessExit(string processName, int timeoutMs)
        {
            int waited = 0;
            int interval = 500;
            while (waited < timeoutMs)
            {
                var procs = System.Diagnostics.Process.GetProcessesByName(processName);
                if (procs.Length == 0) return;
                await Task.Delay(interval);
                waited += interval;
            }
        }

        static string GetRelativePath(string basePath, string fullPath)
        {
            basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar;
            var uri = new Uri(basePath);
            var fileUri = new Uri(fullPath);
            return Uri.UnescapeDataString(uri.MakeRelativeUri(fileUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
