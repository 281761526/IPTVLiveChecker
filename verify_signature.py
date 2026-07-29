import hashlib, base64
from Crypto.Cipher import AES

EXE = r"C:/Users/lenovo/Desktop/IPTVLiveChecker/IPTV1.0/bin/Verify/IPTVLiveChecker.exe"
data = open(EXE, 'rb').read()

# 与 Program.cs 一致：签名 15 字节，尾部 = 64 字节 base64 + 15 字节签名 = 79 字节
SIG_LEN = 15
B64_LEN = 64
sig = b"IPTV_MD5_V1____"
print("size:", len(data))
print("last 15 bytes == signature:", data[-SIG_LEN:] == sig)

b64 = data[-(B64_LEN + SIG_LEN):-SIG_LEN]   # 64 字节密文
print("b64 len:", len(b64), "| is clean ascii:", all(0x20 <= c < 0x7F for c in b64))

# 密钥派生（与 Program.cs GetAesKey / GetAesIV 完全一致）
key = bytearray(b"MoreSec" + b"retKey12" + b"!@#XYZabc" + b"12defghi")
while len(key) < 32:
    key.append(0)
key = bytes([b ^ 0x5A for b in key])
iv = bytes([b ^ 0x39 for b in (b"12345678" + b"90ABCDEF")])

cipher = base64.b64decode(b64)
aes = AES.new(key, AES.MODE_CBC, iv)
pt = aes.decrypt(cipher)
pad = pt[-1]
pt = pt[:-pad] if 1 <= pad <= 16 else pt
embedded = pt.decode('ascii')
print("EMBEDDED MD5 (decrypted from tail):", embedded)

payload = data[:-(B64_LEN + SIG_LEN)]
actual = hashlib.md5(payload).hexdigest().upper()
print("RECOMPUTED payload MD5          :", actual)
print(">>> MATCH (Program.cs 完整性校验将 PASS):", embedded.upper() == actual)
