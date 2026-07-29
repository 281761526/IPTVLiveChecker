using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Md5Embedder
{
    /// <summary>
    /// 构建期工具：为主程序 exe 追加 80 字节完整性签名尾部。
    /// 尾部布局 = [64 字节 base64(AES-CBC-PKCS7( payload MD5 十六进制串 ))] + [16 字节 ASCII 签名 "IPTV_MD5_V1____"]。
    /// 加密算法、密钥、IV 与 IPTVLiveChecker/Program.cs 中的 AesDecrypt/GetAesKey/GetAesIV 完全一致，
    /// 以保证主程序启动时的完整性校验可通过。
    /// 用法: Md5Embedder.exe <目标exe路径>
    /// </summary>
    internal static class Program
    {
        private const string Md5Signature = "IPTV_MD5_V1____";
        private const int Md5EmbeddedBase64Len = 64;

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: Md5Embedder.exe <targetExePath>");
                return 2;
            }

            string exePath = args[0];
            if (!File.Exists(exePath))
            {
                Console.Error.WriteLine("ERROR: file not found: " + exePath);
                return 2;
            }

            try
            {
                byte[] data = File.ReadAllBytes(exePath);

                // 幂等：若已存在旧签名尾部，先剥离，保证 payload 始终是“纯净”的可执行体。
                if (data.Length >= Md5Signature.Length + Md5EmbeddedBase64Len)
                {
                    string existingSig = Encoding.ASCII.GetString(
                        data, data.Length - Md5Signature.Length, Md5Signature.Length);
                    if (existingSig == Md5Signature)
                    {
                        byte[] stripped = new byte[data.Length - (Md5Signature.Length + Md5EmbeddedBase64Len)];
                        Buffer.BlockCopy(data, 0, stripped, 0, stripped.Length);
                        data = stripped;
                    }
                }

                // 计算 payload（去掉尾部后的全部字节）的 MD5。
                string md5Hex;
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(data);
                    md5Hex = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }

                // AES-CBC-PKCS7 加密 32 字符的 MD5 十六进制串。
                byte[] key = GetAesKey();
                byte[] iv = GetAesIV();
                byte[] plain = Encoding.UTF8.GetBytes(md5Hex);
                byte[] cipher;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plain, 0, plain.Length);
                        cs.FlushFinalBlock();
                        cipher = ms.ToArray();
                    }
                }

                string b64 = Convert.ToBase64String(cipher);
                if (b64.Length != Md5EmbeddedBase64Len)
                {
                    Console.Error.WriteLine(
                        "ERROR: base64 length " + b64.Length + " != expected " + Md5EmbeddedBase64Len);
                    return 1;
                }

                byte[] b64Bytes = Encoding.ASCII.GetBytes(b64);
                byte[] sigBytes = Encoding.ASCII.GetBytes(Md5Signature);

                // 回写：payload + [密文] + [签名]
                using (var outStream = new FileStream(exePath, FileMode.Create, FileAccess.Write))
                {
                    outStream.Write(data, 0, data.Length);
                    outStream.Write(b64Bytes, 0, b64Bytes.Length);
                    outStream.Write(sigBytes, 0, sigBytes.Length);
                }

                Console.WriteLine("[Md5Embedder] OK  file=" + Path.GetFileName(exePath) +
                                  "  payloadMd5=" + md5Hex +
                                  "  size=" + (data.Length + b64Bytes.Length + sigBytes.Length));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.GetType().Name + ": " + ex.Message);
                return 1;
            }
        }

        // 与 Program.cs 中 GetAesKey 完全一致。
        private static byte[] GetAesKey()
        {
            byte[] p1 = Encoding.UTF8.GetBytes("MoreSec");
            byte[] p2 = Encoding.UTF8.GetBytes("retKey12");
            byte[] p3 = Encoding.UTF8.GetBytes("!@#XYZabc");
            byte[] p4 = Encoding.UTF8.GetBytes("12defghi");
            byte[] key = new byte[32];
            Buffer.BlockCopy(p1, 0, key, 0, p1.Length);
            Buffer.BlockCopy(p2, 0, key, p1.Length, p2.Length);
            Buffer.BlockCopy(p3, 0, key, p1.Length + p2.Length, p3.Length);
            Buffer.BlockCopy(p4, 0, key, p1.Length + p2.Length + p3.Length, p4.Length);
            for (int i = 0; i < key.Length; i++)
            {
                key[i] ^= 0x5A;
            }
            return key;
        }

        // 与 Program.cs 中 GetAesIV 完全一致。
        private static byte[] GetAesIV()
        {
            byte[] p1 = Encoding.UTF8.GetBytes("12345678");
            byte[] p2 = Encoding.UTF8.GetBytes("90ABCDEF");
            byte[] iv = new byte[16];
            Buffer.BlockCopy(p1, 0, iv, 0, p1.Length);
            Buffer.BlockCopy(p2, 0, iv, p1.Length, p2.Length);
            for (int i = 0; i < iv.Length; i++)
            {
                iv[i] ^= 0x39;
            }
            return iv;
        }
    }
}
