using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace Updater
{
    /// <summary>
    /// 独立更新器。由主程序 Program.cs 的 StartUpdater 以如下参数启动：
    ///   Updater.exe "<主程序exe完整路径>" "<下载地址>" "<期望MD5>"
    /// 流程：下载 zip -> 校验 MD5 -> 解压(自动剥离 GitHub 包裹文件夹) ->
    ///       等待主程序退出 -> 拷贝文件到应用目录 -> 自更新 Updater.exe -> 重启主程序。
    /// 设计为 net472 控制台程序，可直接在用户机器(.NET Framework 4.7.2+)上运行。
    /// </summary>
    internal static class Program
    {
        private static string _logFile;

        private static int Main(string[] args)
        {
            _logFile = Path.Combine(Path.GetTempPath(), "IPTVLiveChecker_Updater.log");
            try
            {
                if (args.Length < 3)
                {
                    Log("参数不足。用法: Updater.exe <主exe路径> <下载URL> <md5>");
                    return 1;
                }

                string mainExePath = args[0];
                string downloadUrl = args[1];
                string expectedMd5 = (args[2] ?? "").Trim();

                if (!File.Exists(mainExePath))
                {
                    Log("主程序不存在: " + mainExePath);
                    return 1;
                }

                string appDir = Path.GetDirectoryName(Path.GetFullPath(mainExePath));
                string workDir = Path.Combine(
                    Path.GetTempPath(), "IPTVLiveChecker_Update_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workDir);
                string zipPath = Path.Combine(workDir, "update.zip");

                Log("开始更新。主程序=" + mainExePath);
                Log("下载地址=" + downloadUrl);

                // 1) 下载
                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, zipPath);
                }
                Log("下载完成: " + zipPath);

                // 2) MD5 校验
                if (!string.IsNullOrEmpty(expectedMd5))
                {
                    string actual = ComputeFileMd5(zipPath);
                    if (!string.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase))
                    {
                        Log("MD5 校验失败! 期望=" + expectedMd5 + " 实际=" + actual);
                        Cleanup(workDir);
                        return 2;
                    }
                    Log("MD5 校验通过: " + actual);
                }
                else
                {
                    Log("未提供 MD5，跳过校验。");
                }

                // 3) 解压
                string extractDir = Path.Combine(workDir, "extracted");
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(zipPath, extractDir);
                Log("解压完成。");

                // 处理 GitHub Release 下载的 zip 自带的一层包裹文件夹，例如 IPTVLiveChecker_v1.0-beta/
                string srcRoot = extractDir;
                string[] topDirs = Directory.GetDirectories(extractDir);
                string[] topFiles = Directory.GetFiles(extractDir);
                if (topDirs.Length == 1 && topFiles.Length == 0)
                {
                    srcRoot = topDirs[0];
                    Log("检测到包裹文件夹，使用内部目录: " + srcRoot);
                }

                // 4) 等待主程序退出（最多约 30 秒）
                WaitForMainExit(mainExePath);

                // 5) 拷贝文件
                string selfPath = Process.GetCurrentProcess().MainModule.FileName;
                int copied = 0;
                foreach (string src in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
                {
                    string rel = src.Substring(srcRoot.Length).TrimStart('\\', '/');
                    string dest = Path.Combine(appDir, rel);

                    // 不要覆盖正在运行的 Updater 自身；改为落地为 pending，稍后替换。
                    if (string.Equals(Path.GetFileName(dest), "Updater.exe", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(Path.GetFullPath(dest), Path.GetFullPath(selfPath), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(src, Path.Combine(appDir, "Updater.pending.exe"), true);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(src, dest, true);
                    copied++;
                }
                Log("已更新文件数: " + copied);

                // 6) 自更新 Updater.exe
                string pendingUpdater = Path.Combine(appDir, "Updater.pending.exe");
                if (File.Exists(pendingUpdater))
                {
                    try
                    {
                        string cur = Path.Combine(appDir, "Updater.exe");
                        if (File.Exists(cur)) File.Delete(cur);
                        File.Move(pendingUpdater, Path.Combine(appDir, "Updater.exe"));
                        Log("Updater.exe 自更新完成。");
                    }
                    catch (Exception ex)
                    {
                        Log("Updater 自更新失败(可下次更新时修复): " + ex.Message);
                    }
                }

                // 7) 重启主程序
                Log("重启主程序: " + mainExePath);
                Process.Start(new ProcessStartInfo(mainExePath) { UseShellExecute = true });

                Cleanup(workDir);
                Log("更新成功完成。");
                return 0;
            }
            catch (Exception ex)
            {
                Log("更新失败: " + ex.GetType().Name + ": " + ex.Message);
                return 3;
            }
        }

        private static void WaitForMainExit(string mainExePath)
        {
            string fullPath = Path.GetFullPath(mainExePath);
            string name = Path.GetFileNameWithoutExtension(mainExePath);
            for (int i = 0; i < 60; i++) // 最多约 30 秒
            {
                bool running = false;
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (string.Equals(p.MainModule.FileName, fullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            running = true;
                            break;
                        }
                    }
                    catch
                    {
                        // 进程已退出或无法访问
                    }
                }
                if (!running) return;
                Thread.Sleep(500);
            }
            Log("等待主程序退出超时，继续更新。");
        }

        private static string ComputeFileMd5(string path)
        {
            using (var md5 = MD5.Create())
            using (var fs = File.OpenRead(path))
            {
                byte[] hash = md5.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        private static void Cleanup(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        private static void Log(string line)
        {
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string msg = "[" + ts + "] " + line;
            Console.WriteLine(msg);
            try
            {
                File.AppendAllText(_logFile, msg + Environment.NewLine);
            }
            catch
            {
                // 忽略日志写入失败
            }
        }
    }
}
