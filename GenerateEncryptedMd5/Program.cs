using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GenerateEncryptedMd5
{
    class Program
    {
        private const string Md5Signature = "IPTV_MD5_V1____";
        private const int Md5EmbeddedBase64Len = 64;
        private static readonly int Md5SignatureLen = Md5Signature.Length;

        static void Main(string[] args)
        {
            bool interactive = !Console.IsInputRedirected;

            if (args.Length == 0)
            {
                Console.WriteLine("用法: GenerateEncryptedMd5.exe <目标文件路径> [<解决方案目录>]");
                Console.WriteLine("示例: GenerateEncryptedMd5.exe bin\\x64\\Release\\IPTVLiveChecker.exe .");
                Console.WriteLine();
                if (interactive)
                {
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                }
                return;
            }

            string filePath = args[0];
            string solutionDir = args.Length > 1 ? args[1] : Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(filePath)))));

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误: 文件不存在 - {filePath}");
                if (interactive)
                {
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                }
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

                    EmbedMd5ToExe(filePath, encryptedMd5);

                    Console.WriteLine();
                    Console.WriteLine($"新的加密 MD5: {encryptedMd5}");
                    Console.WriteLine("已嵌入到 EXE 文件末尾!");
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

            if (interactive)
            {
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }

        private static void EmbedMd5ToExe(string exePath, string encryptedMd5)
        {
            // 先移除旧的嵌入数据（如果有）
            RemoveOldEmbeddedMd5(exePath);

            // 重新计算移除旧数据后的 MD5
            string newMd5 = ComputeMd5(exePath);
            string newEncryptedMd5 = AesEncrypt(newMd5);
            Console.WriteLine($"移除旧数据后 MD5: {newMd5}");

            // 准备要嵌入的数据：64字节 base64（不足补空格） + 16字节签名
            string b64Padded = newEncryptedMd5.PadRight(Md5EmbeddedBase64Len, ' ');
            if (b64Padded.Length > Md5EmbeddedBase64Len)
            {
                Console.WriteLine($"警告: 加密后 MD5 长度 {b64Padded.Length} 超过 {Md5EmbeddedBase64Len} 字节");
                b64Padded = b64Padded.Substring(0, Md5EmbeddedBase64Len);
            }

            byte[] b64Bytes = Encoding.ASCII.GetBytes(b64Padded);
            byte[] sigBytes = Encoding.ASCII.GetBytes(Md5Signature);

            using (var fs = new FileStream(exePath, FileMode.Append, FileAccess.Write))
            {
                fs.Write(b64Bytes, 0, b64Bytes.Length);
                fs.Write(sigBytes, 0, sigBytes.Length);
            }

            Console.WriteLine($"已嵌入 {b64Bytes.Length + sigBytes.Length} 字节数据到 EXE 末尾");
        }

        private static void RemoveOldEmbeddedMd5(string exePath)
        {
            using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.ReadWrite))
            {
                long totalLen = fs.Length;
                if (totalLen < Md5SignatureLen + Md5EmbeddedBase64Len)
                    return;

                fs.Seek(totalLen - Md5SignatureLen, SeekOrigin.Begin);
                byte[] sig = new byte[Md5SignatureLen];
                fs.Read(sig, 0, Md5SignatureLen);
                string sigStr = Encoding.ASCII.GetString(sig);
                if (sigStr != Md5Signature)
                    return;

                long newLen = totalLen - Md5SignatureLen - Md5EmbeddedBase64Len;
                fs.SetLength(newLen);
                Console.WriteLine($"已移除旧的嵌入数据，文件大小从 {totalLen} 变为 {newLen}");
            }
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
