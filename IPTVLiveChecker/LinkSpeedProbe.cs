using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVLiveChecker
{
    /// <summary>
    /// 引擎无关的独立链接速度探针（Tier A）。
    /// 对频道 URL 发起独立的 GET 请求并持续读取数据，按 Δbytes/Δtime 计算实时传输速率（bps），
    /// 不依赖播放器内部统计。VLC 在稳定播放后内部统计会冻结/归零，本探针从 IO 层直接测量，全程有效。
    /// 仅支持 http/https 直播流；非 http(s) 或请求失败时不启动，由调用方回退到播放器统计。
    /// </summary>
    public sealed class LinkSpeedProbe : IDisposable
    {
        private readonly HttpClient _http;
        private CancellationTokenSource _cts;
        private Task _loop;
        private string _url;
        private readonly object _lock = new object();
        private long _windowBytes;
        private long _windowStartMs;
        private float _currentBps; // 平滑后的字节/s
        private bool _running;

        public LinkSpeedProbe()
        {
            _http = new HttpClient();
            // 直播流是无限流，用 CTS 取消而非 HttpClient 自身超时；避免健康流被 100s 默认超时误杀
            _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "IPTV-LinkProbe/1.0");
        }

        /// <summary>该 URL 是否可被本探针测量（仅 http/https）。</summary>
        public bool IsSupported(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>当前平滑后的实时传输速率（字节/s）。未就绪时为 0。</summary>
        public float CurrentBps
        {
            get { lock (_lock) return _currentBps; }
        }

        public bool IsRunning => _running;

        public string Url => _url;

        /// <summary>启动探针。url 不支持时立即返回 false，不建立任何连接。</summary>
        public bool Start(string url)
        {
            Stop(); // 清理旧连接
            if (!IsSupported(url))
            {
                return false;
            }
            _url = url;
            lock (_lock)
            {
                _windowBytes = 0;
                _windowStartMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                _currentBps = 0f;
            }
            _cts = new CancellationTokenSource();
            _running = true;
            _loop = Task.Run(() => LoopAsync(_cts.Token));
            return true;
        }

        public void Stop()
        {
            _running = false;
            if (_cts != null)
            {
                try { _cts.Cancel(); }
                catch { }
                try { _cts.Dispose(); }
                catch { }
                _cts = null;
            }
            _loop = null;
        }

        private async Task LoopAsync(CancellationToken token)
        {
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, _url))
                {
                    using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                            return;
                        using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            byte[] buf = new byte[32768];
                            while (!token.IsCancellationRequested)
                            {
                                int n = await stream.ReadAsync(buf, 0, buf.Length, token).ConfigureAwait(false);
                                if (token.IsCancellationRequested) break;
                                if (n <= 0)
                                {
                                    // 直播流在两次数据块之间可能暂时返回 0（无新数据），
                                    // 不应视为流结束而退出：短暂等待后继续读取，保持探针持续运行。
                                    // 否则 VLC 稳定播放后内部统计冻结，探针又退出，会导致“链接速度/已传输”静止。
                                    await Task.Delay(50, token).ConfigureAwait(false);
                                    continue;
                                }
                                long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                                lock (_lock)
                                {
                                    _windowBytes += n;
                                    long windowMs = now - _windowStartMs;
                                    if (windowMs >= 250)
                                    {
                                        double bps = ((double)_windowBytes * 8.0 * 1000.0) / windowMs; // bits/s
                                        _currentBps = _currentBps <= 0f ? (float)bps : _currentBps * 0.5f + (float)bps * 0.5f;
                                        _windowBytes = 0;
                                        _windowStartMs = now;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* 探针失败不影响播放；CurrentBps 保持上次值，调用方可回退播放器统计 */ }
            finally
            {
                _running = false;
            }
        }

        public void Dispose()
        {
            Stop();
            try { _http.Dispose(); }
            catch { }
        }
    }
}
