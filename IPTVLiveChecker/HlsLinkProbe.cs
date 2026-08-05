using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVLiveChecker
{
    /// <summary>
    /// HLS 智能分片探针：当本地计数代理不可用时（如无法启动端口、源拒绝代理请求头）的兜底方案。
    /// 独立解析 m3u8 清单并持续顺序下载 .ts 分片，按 Δbytes/Δtime 计算实时传输速率。
    /// 会额外拉一条流，因此优先级低于 HlsCountingProxy；仅在代理失败或被禁用时启用。
    /// </summary>
    internal sealed class HlsLinkProbe : IDisposable
    {
        private readonly string _userAgent;
        private CancellationTokenSource _cts;
        private Task _loop;
        private string _m3u8Url;
        private bool _running;
        private long _totalBytesRead;
        private long _windowBytes;
        private long _windowStartMs;
        private float _currentBps;
        private readonly object _lock = new object();

        public HlsLinkProbe(string userAgent = null)
        {
            _userAgent = userAgent ?? "IPTV-HlsProbe/1.0";
        }

        public bool IsRunning => _running;

        public string Url => _m3u8Url;

        public long TotalBytesRead => Interlocked.Read(ref _totalBytesRead);

        public float CurrentBps
        {
            get { lock (_lock) return _currentBps; }
        }

        public bool Start(string m3u8Url)
        {
            Stop();
            if (string.IsNullOrWhiteSpace(m3u8Url)
                || !m3u8Url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                return false;
            _m3u8Url = m3u8Url;
            _cts = new CancellationTokenSource();
            _running = true;
            ResetWindow();
            _loop = Task.Run(() => LoopAsync(_cts.Token));
            return true;
        }

        public void Stop()
        {
            _running = false;
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
                try { _cts.Dispose(); } catch { }
                _cts = null;
            }
            _loop = null;
            _m3u8Url = null;
            lock (_lock)
            {
                _currentBps = 0f;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void ResetWindow()
        {
            long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            lock (_lock)
            {
                _windowBytes = 0;
                _windowStartMs = now;
            }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            try
            {
                string baseUri = GetBaseUri(_m3u8Url);
                var downloadedSegments = new HashSet<string>();
                while (!token.IsCancellationRequested)
                {
                    List<string> segments = null;
                    try
                    {
                        segments = await FetchSegmentsAsync(_m3u8Url, baseUri, token).ConfigureAwait(false);
                    }
                    catch { }
                    if (segments == null || segments.Count == 0)
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                        continue;
                    }
                    foreach (var segUrl in segments)
                    {
                        if (token.IsCancellationRequested) break;
                        if (!downloadedSegments.Add(segUrl)) continue; // 已下载过的分片跳过
                        await DownloadSegmentAsync(segUrl, token).ConfigureAwait(false);
                    }
                    // 直播 HLS：循环刷新 m3u8；点播 HLS：清单不变，继续等待新的直播分片
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                _running = false;
            }
        }

        private async Task<List<string>> FetchSegmentsAsync(string m3u8Url, string baseUri, CancellationToken token)
        {
            var req = (HttpWebRequest)WebRequest.Create(m3u8Url);
            req.UserAgent = _userAgent;
            req.Timeout = 10000;
            using (var resp = (HttpWebResponse)await req.GetResponseAsync().ConfigureAwait(false))
            using (var stream = resp.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var list = new List<string>();
                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (token.IsCancellationRequested) break;
                    string trim = line.Trim();
                    if (string.IsNullOrEmpty(trim) || trim.StartsWith("#")) continue;
                    string segUrl = ResolveUrl(trim, baseUri);
                    list.Add(segUrl);
                }
                return list;
            }
        }

        private async Task DownloadSegmentAsync(string segUrl, CancellationToken token)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(segUrl);
                req.UserAgent = _userAgent;
                req.AllowReadStreamBuffering = false;
                req.Timeout = 15000;
                using (var resp = (HttpWebResponse)await req.GetResponseAsync().ConfigureAwait(false))
                using (var stream = resp.GetResponseStream())
                {
                    byte[] buf = new byte[32768];
                    int n;
                    while ((n = await stream.ReadAsync(buf, 0, buf.Length, token).ConfigureAwait(false)) > 0)
                    {
                        Interlocked.Add(ref _totalBytesRead, n);
                        long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                        lock (_lock)
                        {
                            _windowBytes += n;
                            long windowMs = now - _windowStartMs;
                            if (windowMs >= 250)
                            {
                                double bps = ((double)_windowBytes * 8.0 * 1000.0) / windowMs;
                                _currentBps = _currentBps <= 0f ? (float)bps : _currentBps * 0.5f + (float)bps * 0.5f;
                                _windowBytes = 0;
                                _windowStartMs = now;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static string ResolveUrl(string line, string baseUri)
        {
            if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return line;
            return new Uri(new Uri(baseUri), line).AbsoluteUri;
        }

        private static string GetBaseUri(string url)
        {
            int lastSlash = url.LastIndexOf('/');
            return lastSlash >= 0 ? url.Substring(0, lastSlash + 1) : url;
        }
    }
}
