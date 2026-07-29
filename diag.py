import hashlib
EXE = r"C:/Users/lenovo/Desktop/IPTVLiveChecker/IPTV1.0/bin/Verify/IPTVLiveChecker.exe"
data = open(EXE, 'rb').read()
print("size:", len(data))
sig = b"IPTV_MD5_V1____"
pos = data.rfind(sig)
print("rfind signature at byte offset:", pos, " (size-pos =", len(data)-pos, ")")
print("last 80 bytes hex:", data[-80:].hex())
# candidate base64 region: 64 bytes immediately before the signature
if pos != -1:
    b64_region = data[pos-64:pos]
    print("b64_region repr:", repr(b64_region))
    print("b64_region len:", len(b64_region))
    payload = data[:pos-64]
    print("payload MD5 (before b64):", hashlib.md5(payload).hexdigest().upper())
    print("payload len:", len(payload))
# Also: what does the embedder THINK happened? recompute MD5 of (file minus last 80)
print("MD5 of file[:-80]:", hashlib.md5(data[:-80]).hexdigest().upper())
# show the byte right before the 80-byte tail starts
tail_start = len(data) - 80
print("byte at tail_start-1 (last payload byte):", hex(data[tail_start-1]))
print("byte at tail_start (first tail byte):", hex(data[tail_start]))
