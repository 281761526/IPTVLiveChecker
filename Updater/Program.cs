using System;
using System.IO;
using System.Net.Http;
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
            string tempFile = Path.Combine(Path.GetTempPath(), "ip_update_temp.exe");

            try
            {
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromMinutes(10);
                    byte[] data = http.GetByteArrayAsync(downloadUrl).Result;
                    File.WriteAllBytes(tempFile, data);
                }

                System.Threading.Thread.Sleep(2000);

                File.Copy(tempFile, mainExePath, true);
                File.Delete(tempFile);

                System.Diagnostics.Process.Start(mainExePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新失败：" + ex.Message, "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Application.Exit();
        }
    }
}
