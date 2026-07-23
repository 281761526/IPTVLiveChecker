using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GenerateEncryptedMd5
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("用法: GenerateEncryptedMd5.exe <目标文件路径> [<解决方案目录>]");
                Console.WriteLine("示例: GenerateEncryptedMd5.exe bin\\x64\\Release\\IPTVLiveChecker.exe .");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            string filePath = args[0];
            string solutionDir = args.Length > 1 ? args[1] : Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(filePath)))));

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误: 文件不存在 - {filePath}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            try
            {
                string md5 = ComputeMd5(filePath);
                Console.WriteLine($"原始 MD5: {md5}");

                string encryptedMd5 = AesEncrypt(md5);
                Console.WriteLine($"加密后: {encryptedMd5}");

                string decryptedMd5 = AesDecrypt(encryptedMd5);
                Console.WriteLine($"解密验证: {decryptedMd5}");

                if (md5.Equals(decryptedMd5, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.WriteLine("加密解密验证通过!");
                    Console.WriteLine();

                    UpdateConfigFile(Path.Combine(solutionDir, "App.config"), encryptedMd5);
                    UpdateConfigFile(Path.Combine(solutionDir, "bin", "x64", "Release", "IPTVLiveChecker.exe.config"), encryptedMd5);
                    UpdateConfigFile(Path.Combine(solutionDir, "publish", "IPTVLiveChecker.exe.config"), encryptedMd5);

                    Console.WriteLine("所有配置文件已自动更新!");
                    Console.WriteLine();
                    Console.WriteLine($"新的加密 MD5: {encryptedMd5}");
                }
                else
                {
                    Console.WriteLine("警告: 加密解密验证失败!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static void UpdateConfigFile(string configPath, string encryptedMd5)
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"跳过: 文件不存在 - {configPath}");
                return;
            }

            try
            {
                string content = File.ReadAllText(configPath);
                string pattern = @"<add key=""ExpectedExeMd5"" value="".*?"" />";
                string replacement = $"<add key=\"ExpectedExeMd5\" value=\"{encryptedMd5}\" />";
                string newContent = System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);

                if (content != newContent)
                {
                    File.WriteAllText(configPath, newContent);
                    Console.WriteLine($"已更新: {configPath}");
                }
                else
                {
                    Console.WriteLine($"无需更新: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新失败: {configPath} - {ex.Message}");
            }
        }

        private static string ComputeMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
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
                key[i] = (byte)(key[i] ^ 0x5A);

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
                iv[i] = (byte)(iv[i] ^ 0x39);

            return iv;
        }

        private static string AesEncrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (var aes = Aes.Create())
            {
                aes.Key = GetAesKey();
                aes.IV = GetAesIV();
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

        private static string AesDecrypt(string cipherTextBase64)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
            using (var aes = Aes.Create())
            {
                aes.Key = GetAesKey();
                aes.IV = GetAesIV();
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
    }
}
