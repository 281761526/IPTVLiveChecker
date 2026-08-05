using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVLiveChecker
{
    /// <summary>
    /// HLS 本地计数代理：零额外带宽精确测量链接速度。
    ///
    /// 工作原理：
    /// 1. 在 127.0.0.1 的某个可用端口启动一个微型 HTTP 代理；
    /// 2. ChannelPlayer 让 VLC 指向本地代理 URL（如 http://127.0.0.1:xxxxx/index.m3u8?hlsorigin=...）；
    /// 3. 代理回源原始 m3u8 清单，改写其中的子清单/分片/key/init URL 为本地代理 URL；
    /// 4. VLC 请求子清单/分片/key/init 时，代理再次回源并把字节流经过计数器后返回给 VLC；
    /// 5. TotalBytesRead 随 VLC 实际消费的字节持续累加，主窗体据此算出实时链接速度。
    ///
    /// 改写规则：
    /// - 普通 URL 行（媒体播放列表中的分片、key、init 等）：走 /segment?hlsorigin=...（计数并流式转发）；
    /// - .m3u8 URL 行（主播放列表中的子清单、#EXT-X-KEY/MAP/MEDIA/I-FRAME-STREAM-INF 中的 URI）：走 /index.m3u8?hlsorigin=...（递归代理并改写）。
    ///
    /// 优点：单连接（与 VLC 同一条消费路径），不额外拉流，零额外带宽；
    /// 局限：VLC 的请求头/Referer/Cookie 由代理重新构造，不携带原播放器 HTTP 选项，
    ///       对强依赖特殊请求头的源可能失败（此时回退到原始 URL + 智能分片探针兜底）。
    /// </summary>
    internal sealed class HlsCountingProxy : IDisposable
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _loop;
        private readonly string _originBaseUri; // 顶层 m3u8 的基础 URL（备用）
        private readonly string _originM3u8Url; // 顶层 m3u8 完整 URL
        private readonly string _userAgent;
        private long _totalBytesRead;
        private string _localUrl;
        private bool _disposed;

        public HlsCountingProxy(string originM3u8Url, string userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(originM3u8Url))
                throw new ArgumentNullException(nameof(originM3u8Url));
            _originM3u8Url = originM3u8Url;
            _originBaseUri = GetBaseUri(originM3u8Url);
            _userAgent = userAgent ?? "IPTV-HlsProxy/1.0";
        }

        /// <summary>本地代理 URL，传给 VLC 播放。</summary>
        public string LocalUrl => _localUrl;

        /// <summary>累计已传输给 VLC 的字节数（分片/key/init 等二进制资源，m3u8 清单文本本身不计入）。</summary>
        public long TotalBytesRead => Interlocked.Read(ref _totalBytesRead);

        public bool IsRunning => _listener != null && _listener.IsListening;

        /// <summary>尝试启动代理并返回本地 URL。失败时返回 null，调用方应回退到原始 URL。</summary>
        public string Start()
        {
            Stop();
            if (!TryCreateListener(out var prefix, out var listener))
                return null;
            _listener = listener;
            _cts = new CancellationTokenSource();
            _localUrl = prefix + "index.m3u8?hlsorigin=" + Uri.EscapeDataString(_originM3u8Url);
            _loop = Task.Run(() => LoopAsync(_cts.Token));
            return _localUrl;
        }

        public void Stop()
        {
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
                try { _cts.Dispose(); } catch { }
                _cts = null;
            }
            if (_listener != null)
            {
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
                _listener = null;
            }
            _loop = null;
            _localUrl = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
                if (ctx == null) continue;
                // 每个请求独立处理，不阻塞 listener 接收循环
#pragma warning disable CS4014
                Task.Run(() => HandleRequestAsync(ctx), token);
#pragma warning restore CS4014
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            try
            {
                string rawPath = ctx.Request.RawUrl;
                if (rawPath.StartsWith("/index.m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    await ServeManifestAsync(ctx).ConfigureAwait(false);
                }
                else if (rawPath.StartsWith("/segment", StringComparison.OrdinalIgnoreCase))
                {
                    await ServeSegmentAsync(ctx).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
            }
            catch
            {
                try { ctx.Response.StatusCode = 502; ctx.Response.Close(); }
                catch { }
            }
        }

        private async Task ServeManifestAsync(HttpListenerContext ctx)
        {
            string originUrl = ExtractHlsOrigin(ctx.Request.Url.Query) ?? _originM3u8Url;
            if (string.IsNullOrWhiteSpace(originUrl))
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                return;
            }
            using (var rsp = await FetchOriginAsync(originUrl, ctx.Request).ConfigureAwait(false))
            {
                if (rsp == null || !rsp.IsOK)
                {
                    ctx.Response.StatusCode = rsp?.StatusCode ?? 502;
                    ctx.Response.Close();
                    return;
                }
                byte[] body = await ReadAllBytesAsync(rsp.Stream).ConfigureAwait(false);
                string text = Encoding.UTF8.GetString(body);
                string rewritten = RewriteManifest(text, originUrl);
                byte[] outBytes = Encoding.UTF8.GetBytes(rewritten);
                ctx.Response.ContentType = "application/vnd.apple.mpegurl";
                ctx.Response.ContentLength64 = outBytes.Length;
                await ctx.Response.OutputStream.WriteAsync(outBytes, 0, outBytes.Length).ConfigureAwait(false);
                ctx.Response.Close();
            }
        }

        private async Task ServeSegmentAsync(HttpListenerContext ctx)
        {
            string originUrl = ExtractHlsOrigin(ctx.Request.Url.Query);
            if (string.IsNullOrWhiteSpace(originUrl))
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                return;
            }
            using (var rsp = await FetchOriginAsync(originUrl, ctx.Request).ConfigureAwait(false))
            {
                if (rsp == null || !rsp.IsOK)
                {
                    ctx.Response.StatusCode = rsp?.StatusCode ?? 502;
                    ctx.Response.Close();
                    return;
                }
                ctx.Response.ContentType = GuessContentType(originUrl);
                ctx.Response.SendChunked = true;
                byte[] buf = new byte[65536];
                int n;
                while ((n = await rsp.Stream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
                {
                    await ctx.Response.OutputStream.WriteAsync(buf, 0, n).ConfigureAwait(false);
                    Interlocked.Add(ref _totalBytesRead, n);
                }
                ctx.Response.Close();
            }
        }

        /// <summary>改写 m3u8：把子清单/分片/key/init URL 全部指向本地代理。
        /// manifestUrl 是当前被改写的 m3u8 的原始 URL，用于解析相对路径。</summary>
        private string RewriteManifest(string manifest, string manifestUrl)
        {
            string baseUri = GetBaseUri(manifestUrl);
            var sb = new StringBuilder();
            using (var sr = new StringReader(manifest))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string trim = line.Trim();
                    if (string.IsNullOrEmpty(trim))
                    {
                        sb.AppendLine(line);
                        continue;
                    }
                    if (trim.StartsWith("#"))
                    {
                        // 标签行中可能包含 URI="..." 的资源引用（如 #EXT-X-KEY、#EXT-X-MAP、
                        // #EXT-X-MEDIA、#EXT-X-I-FRAME-STREAM-INF 等）
                        if (trim.IndexOf("URI=\"", StringComparison.OrdinalIgnoreCase) >= 0)
                            sb.AppendLine(RewriteTagUri(line, baseUri));
                        else
                            sb.AppendLine(line);
                        continue;
                    }
                    // 普通 URL 行
                    string resourceUrl = ResolveUrl(trim, baseUri);
                    string proxyUrl = IsM3u8Url(resourceUrl)
                        ? "index.m3u8?hlsorigin=" + Uri.EscapeDataString(resourceUrl)
                        : "segment?hlsorigin=" + Uri.EscapeDataString(resourceUrl);
                    sb.AppendLine(proxyUrl);
                }
            }
            return sb.ToString();
        }

        /// <summary>替换标签行中的 URI="..." 为本地代理 URL。</summary>
        private string RewriteTagUri(string line, string baseUri)
        {
            int start = line.IndexOf("URI=\"", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return line;
            start += 5; // 跳过 URI="
            int end = line.IndexOf("\"", start);
            if (end < 0) return line;
            string uri = line.Substring(start, end - start);
            string absUri = ResolveUrl(uri, baseUri);
            string proxy = IsM3u8Url(absUri)
                ? "index.m3u8?hlsorigin=" + Uri.EscapeDataString(absUri)
                : "segment?hlsorigin=" + Uri.EscapeDataString(absUri);
            return line.Substring(0, start) + proxy + line.Substring(end);
        }

        private static string ExtractHlsOrigin(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            int idx = query.IndexOf("hlsorigin=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int start = idx + "hlsorigin=".Length;
            int end = query.IndexOf('&', start);
            string encoded = end < 0 ? query.Substring(start) : query.Substring(start, end - start);
            try { return Uri.UnescapeDataString(encoded); }
            catch { return null; }
        }

        private static bool IsM3u8Url(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)
                || url.IndexOf(".m3u8?", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveUrl(string line, string baseUri)
        {
            if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return line;
            try
            {
                return new Uri(new Uri(baseUri), line).AbsoluteUri;
            }
            catch { return line; }
        }

        private static string GetBaseUri(string url)
        {
            int lastSlash = url.LastIndexOf('/');
            return lastSlash >= 0 ? url.Substring(0, lastSlash + 1) : url;
        }

        private static string GuessContentType(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "application/octet-stream";
            if (url.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)) return "video/mp2t";
            if (url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                || url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)) return "application/vnd.apple.mpegurl";
            if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) return "video/mp4";
            if (url.EndsWith(".aac", StringComparison.OrdinalIgnoreCase)) return "audio/aac";
            return "application/octet-stream";
        }

        private async Task<OriginResponse> FetchOriginAsync(string url, HttpListenerRequest incoming)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = _userAgent;
                req.AllowReadStreamBuffering = false;
                req.Timeout = 15000;
                // 尽量透传常见头（Referer 对 HLS 防盗链常见）
                string referer = incoming?.Headers["Referer"];
                if (!string.IsNullOrWhiteSpace(referer)) req.Referer = referer;
                var resp = (HttpWebResponse)await req.GetResponseAsync().ConfigureAwait(false);
                return new OriginResponse
                {
                    StatusCode = (int)resp.StatusCode,
                    Stream = resp.GetResponseStream(),
                    Response = resp,
                    IsOK = resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.PartialContent
                };
            }
            catch
            {
                return null;
            }
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                byte[] buf = new byte[8192];
                int n;
                while ((n = await stream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
                    ms.Write(buf, 0, n);
                return ms.ToArray();
            }
        }

        private bool TryCreateListener(out string prefix, out HttpListener listener)
        {
            prefix = null;
            listener = null;
            // 尝试 20 个随机高位端口
            var rnd = new Random();
            for (int i = 0; i < 20; i++)
            {
                int port = rnd.Next(43000, 49000);
                string p = "http://127.0.0.1:" + port + "/";
                var l = new HttpListener();
                l.Prefixes.Add(p);
                try
                {
                    l.Start();
                    prefix = p;
                    listener = l;
                    return true;
                }
                catch { l.Close(); }
            }
            return false;
        }

        private sealed class OriginResponse : IDisposable
        {
            public int StatusCode;
            public Stream Stream;
            public HttpWebResponse Response;
            public bool IsOK;
            public void Dispose()
            {
                try { Stream?.Dispose(); } catch { }
                try { Response?.Close(); } catch { }
            }
        }
    }
}
