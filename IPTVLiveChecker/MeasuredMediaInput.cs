using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace IPTVLiveChecker
{
    /// <summary>
    /// Tier B 真正形态：零额外带宽注入式精确模式。
    /// 不再另开第二个连接去"测量并丢弃"，而是把上游的【同一个】连接直接注入 VLC 输入——
    /// VLC 通过回调向本类拉取字节，我们每次读取都经 MeasuredStream 精确计数后再交给 VLC。
    /// VLC 实际消费多少字节就计数多少，单连接、零额外带宽，且读数随真实收包持续累加
    /// （彻底摆脱 LibVLC 内部统计在稳定播放后冻结/归零的问题）。
    /// 由 VLC 在其解封装线程回调 Open/Read/Seek/Close，不阻塞 UI 线程。
    /// </summary>
    internal sealed class MeasuredMediaInput : MediaInput
    {
        private readonly Uri _uri;
        private readonly string _userAgent;
        private WebResponse _resp;
        private MeasuredStream _measured;
        private byte[] _buffer;

        public MeasuredMediaInput(Uri uri, string userAgent = null)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _userAgent = userAgent ?? "IPTV-MeasuredInput/1.0";
        }

		/// <summary>累计已读取（即 VLC 实际消费）的字节数，由 TryGetPlayerStats 读取用于链接速度。</summary>
		public long TotalBytesRead => _measured?.TotalBytesRead ?? 0L;

		// 注：LibVLCSharp 3.8.2 的 MediaInput.CanSeek 为非虚属性（默认 false），无需也无法 override；
		// 直播流本就不可定位，基类默认值恰为 false，直接继承即可。

		public override bool Open(out ulong size)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(_uri);
                req.UserAgent = _userAgent;
                req.AllowReadStreamBuffering = false; // 直播流：边收边读，不缓冲到结束
                req.Timeout = 15000;
                _resp = req.GetResponse();
                var raw = _resp.GetResponseStream();
                _measured = new MeasuredStream(raw);
                _buffer = new byte[262144];
                // 直播流长度未知，必须填 ulong.MaxValue，否则 VLC 会按有限长度截断/提前结束
                size = ulong.MaxValue;
                return true;
            }
            catch
            {
                size = 0;
                return false;
            }
        }

        public override int Read(IntPtr buf, uint len)
        {
            var ms = _measured;
            if (ms == null) return 0;
            try
            {
                int toRead = (int)Math.Min(len, (uint)_buffer.Length);
                int n = ms.Read(_buffer, 0, toRead);
                if (n > 0) Marshal.Copy(_buffer, 0, buf, n);
                return n; // 0 = 流结束；>0 = 读取字节数
            }
            catch
            {
                // 上游异常（断流/超时）统一按流结束处理，避免向 VLC 原生层抛托管异常
                return 0;
            }
        }

        public override bool Seek(ulong position) => false;

        public override void Close()
        {
            try { _measured?.Dispose(); }
            catch { }
            try { _resp?.Close(); }
            catch { }
            _measured = null;
            _resp = null;
            _buffer = null;
        }
    }
}
