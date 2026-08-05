using System;
using System.Drawing;
using System.Windows.Forms;
using IPTVLiveChecker.Properties;

namespace IPTVLiveChecker
{
    /// <summary>
    /// 基于 libmpv-2.dll 的预览窗播放引擎，作为 ChannelPlayer(VLC) 的对照实现。
    /// 通过 --wid 把 mpv 渲染嵌入一个 WinForms 面板，自带与 ChannelPlayer 类似的控制栏。
    /// 本类实现 IChannelPlayer，因此主窗口可零改动切换（见 UseMpvPreview 开关）。
    /// </summary>
    public class MpvPlayer : UserControl, IChannelPlayer
    {
        private IntPtr _ctx = IntPtr.Zero;
        private bool _available;
        private float _dpiScale = 1f;

        private Panel _host;
        private Panel _controlBar;
        private Button _btnPlay, _btnStop, _btnMute, _btnFs, _btnExt;
        // 音量键字体：正常态 / 静音态（删除线）。预建复用，避免每次切换 new Font 泄漏
        private Font _fontVol, _fontVolMuted;
        private TrackBar _volume, _seek;
        private Label _timeLabel;
        private Panel _fallback;
        private Label _fallbackLabel;

        private string _url, _name;
        private bool _isPlaying;
        private bool _dragging;
        private bool _fullscreenMode;
        private readonly System.Windows.Forms.Timer _poll = new System.Windows.Forms.Timer();
        // HLS 链接速度兜底探针：MPV 原生 input-bitrate 对 HLS 分片流常为 0，
        // 故与 VLC 一致——HLS 源启动独立分片下载探针，用真实收包字节推算链接速度。
        private HlsLinkProbe _hlsProbe;
        // HLS 探针自动停表（暖机结束后释放带宽，见 ScheduleHlsProbeAutoStop）
        private System.Threading.Timer _hlsProbeAutoStop;
        private const int HlsProbeWarmupMs = 6000;
        // 链接速度窗口化平滑：MPV 原生 input-bitrate 是瞬时值、网络抖动下剧烈跳变，
        // 主窗体 0.5 EWMA 压不住尖刺；HLS 下退化为分片探针 Δbytes/Δtime 又受分片间隔突跳影响。
        // 故仿 VLC 的 ReadBytes 三级融合：自行维护窗口化 Δbytes + 内部 EWMA 兜底「上一帧沿用」，
        // 在主窗体之前先平滑一次，使 MPV 链接速度显示与 VLC 同样顺滑。
        private long _statLastReadBytes;
        private long _statLastTickMs;
        private float _statSmoothBytesPerSec;

        public event EventHandler OpenExternalRequested;

        /// <summary>宿主窗口（视频区）双击请求：由外层 ChannelPlayer 订阅并切换全屏，
        /// 与 VLC 模式双击视频窗最大化行为一致（注意不是暂停/播放）。</summary>
        public event EventHandler HostDoubleClick;

        public MpvPlayer()
        {
            using (Graphics g = CreateGraphics())
            {
                _dpiScale = g.DpiX / 96f;
            }

            _host = new Panel
            {
                BackColor = Color.Black
            };
            _host.DoubleClick += (s, e) => HostDoubleClick?.Invoke(this, EventArgs.Empty);

            BuildControlBar();
            base.Controls.Add(_host);

            // 视频宿主始终只占“纯视频区”：
            // - 被 ChannelPlayer 托管时（自身控制栏隐藏）：_host 铺满整个 _mpv（其高度已被 ChannelPlayer
            //   约束为“纯视频区”，无需再减控制栏高度），使 mpv 渲染窗口比例恒为 16:9，
            //   避免窗口被撑高后 mpv 比例居中导致“上方黑边比下方大”（下方被外部控制栏遮挡）。
            // - 独立/全屏时：_host 占“控件高度 − 控制栏高度”，底部留给自身控制栏。
            // mpv 通过 video-aspect-override=16:9 + panscan=1 拉伸铺满窗口，与 VLC 预览窗一致。
            base.Layout += (s, e) =>
            {
                // 控制栏已移到播放窗外侧下方，视频宿主始终占满整个控件。
                int barH = 0;
                int availW = base.ClientSize.Width;
                int availH = Math.Max(0, base.ClientSize.Height - barH);
                // 防抖：尺寸未变化则跳过 SetBounds，避免 mpv(wid 模式) 反复重建渲染 viewport 造成卡顿
                if (_host.Bounds.Width == availW && _host.Bounds.Height == availH)
                {
                    return;
                }
                _host.SetBounds(0, 0, availW, availH);
            };

            _fallback = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 24, 28),
                Visible = false
            };
            _fallbackLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 56,
                ForeColor = Color.FromArgb(220, 220, 225),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f),
                Text = "mpv 不可用（缺少 libmpv-2.dll 或初始化失败）"
            };
            _fallback.Controls.Add(_fallbackLabel);
            base.Controls.Add(_fallback);

            _poll.Interval = 250;
            _poll.Tick += PollTick;
        }

        // ---------------- 控制栏 ----------------

        private void BuildControlBar()
        {
            int barH = (int)(52f * _dpiScale);
            _controlBar = new Panel
            {
                Dock = DockStyle.None,
                Height = barH,
                BackColor = Color.FromArgb(20, 22, 28)
            };
            // 控制栏不再放在播放器内部(会占用视频高度/被圆角窗裁切)，改由主窗体承载到播放窗外侧下方。
            _controlBar.VisibleChanged += (s, e) => ControlBarVisibilityChanged?.Invoke(this, EventArgs.Empty);

            int btnSize = (int)(34f * _dpiScale);
            int y = (barH - btnSize) / 2;

            _btnPlay = MakeBtn("▶", btnSize, y);
            _btnPlay.Click += (s, e) => TogglePlay();
            _btnStop = MakeBtn("⏹", btnSize, y);
            _btnStop.Click += (s, e) => Stop();
            // 原 "🔊"/"🔇"：U+1F50A/U+1F507 是 emoji 呈现，由 Segoe UI Emoji 以固定彩色位图渲染，
            // 既不随主题变色，在 UseCompatibleTextRendering(GDI+) 下还可能落成豆腐块。
            // 改用单色音符 ♪(U+266A，文本呈现)，静音态以删除线表达 —— 全程跟随 ForeColor，随主题换肤。
            _fontVol = new Font("Segoe UI", 11f * _dpiScale, FontStyle.Regular);
            _fontVolMuted = new Font("Segoe UI", 11f * _dpiScale, FontStyle.Strikeout);
            _btnMute = MakeBtn("♪", btnSize, y);
            _btnMute.Font = _fontVol;
            _btnMute.Click += (s, e) => ToggleMute();

            _volume = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                Width = (int)(80f * _dpiScale),
                Height = btnSize,
                TickStyle = TickStyle.None,
                Left = _btnMute.Right + 6,
                Top = y
            };
            _volume.ValueChanged += (s, e) => SetVolume(_volume.Value);

            _seek = new TrackBar
            {
                Minimum = 0,
                Maximum = 1000,
                Value = 0,
                Height = btnSize,
                TickStyle = TickStyle.None,
                Left = _volume.Right + 8,
                Top = y
            };
            _seek.MouseDown += (s, e) => _dragging = true;
            _seek.MouseUp += (s, e) => _dragging = false;
            _seek.KeyDown += (s, e) => _dragging = true;
            _seek.KeyUp += (s, e) => _dragging = false;
            _seek.Scroll += (s, e) => DoSeek();

            _timeLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 225),
                Font = new Font("Segoe UI", 10f),
                Top = y + 4,
                Text = "00:00 / 00:00"
            };

            _btnFs = MakeBtn("⛶", btnSize, y);
            _btnFs.Click += (s, e) => ToggleFullscreen();
            _btnExt = MakeBtn("↗", btnSize, y);
            _btnExt.Click += (s, e) => OnOpenExternalRequested();

            _controlBar.Controls.Add(_btnPlay);
            _controlBar.Controls.Add(_btnStop);
            _controlBar.Controls.Add(_btnMute);
            _controlBar.Controls.Add(_volume);
            _controlBar.Controls.Add(_seek);
            _controlBar.Controls.Add(_timeLabel);
            _controlBar.Controls.Add(_btnFs);
            _controlBar.Controls.Add(_btnExt);

            // 右侧按钮靠右布局
            base.Layout += (s, e) =>
            {
                int right = _controlBar.ClientSize.Width - 8;
                _btnExt.Left = right - btnSize;
                _btnFs.Left = _btnExt.Left - btnSize - 6;
                _timeLabel.Left = _btnFs.Left - _timeLabel.Width - 12;
                _seek.Width = Math.Max(40, _timeLabel.Left - _seek.Left - 10);
            };
        }

        private Button MakeBtn(string text, int size, int top)
        {
            Button b = new Button
            {
                Text = text,
                Size = new Size(size, size),
                Top = top,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 44, 52),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public int ControlBarHeight => (_controlBar != null) ? _controlBar.Height : ((int)(52f * _dpiScale));

        /// <summary>底部控制栏面板（移到播放窗外侧下方承载）。</summary>
        public Panel ControlBar => _controlBar;

        /// <summary>控制栏显隐状态变化通知，供主窗体同步外部容器高度。</summary>
        public event EventHandler ControlBarVisibilityChanged;

        // 当被 ChannelPlayer 托管时，隐藏自带控制栏，由 ChannelPlayer 统一控制
        public void SetControlBarVisible(bool visible)
        {
            if (_controlBar != null) _controlBar.Visible = visible;
        }

        // ---------------- 引擎生命周期 ----------------

        /// <summary>预热 mpv 上下文（创建+初始化），应尽早调用（如宿主 handle 创建后），
        /// 使首次切到 MPV 引擎时无需再同步初始化、避免切换卡顿。重复调用安全（内部已缓存）。</summary>
        public void Prewarm()
        {
            if (MpvLib.Missing) return; // 缺 dll 时不必尝试
            if (_available || _ctx != IntPtr.Zero) return;
            try { EnsureMpv(); } catch { }
        }

        /// <summary>全屏模式切换：全屏时视频宿主铺满整个窗口（不留控制栏高度），退出时恢复纯视频区。</summary>
        public void SetFullscreenMode(bool on)
        {
            if (_fullscreenMode == on) return;
            _fullscreenMode = on;
            if (_host != null && !_host.IsDisposed)
            {
                // 触发 Layout 重新计算宿主尺寸
                OnLayout(new LayoutEventArgs(this, "fullscreen"));
            }
        }

        private void EnsureMpv()
        {
            if (_available || _ctx != IntPtr.Zero)
            {
                return;
            }
            if (!_host.IsHandleCreated)
            {
                _host.CreateControl();
            }
            try
            {
                _ctx = MpvLib.Create();
                if (_ctx == IntPtr.Zero)
                {
                    if (MpvLib.Missing)
                    {
                        ShowFallback("未找到 libmpv-2.dll，请将它放到程序目录或系统 PATH");
                    }
                    else
                    {
                        ShowFallback("mpv 初始化失败（mpv_create 返回空）");
                    }
                    return;
                }
                MpvLib.SetOption(_ctx, "wid", _host.Handle.ToString());
                MpvLib.SetOption(_ctx, "keep-open", "yes");
                MpvLib.SetOption(_ctx, "keepaspect", "no"); // 不强制保持窗口比例
                // 强制视频按 16:9 拉伸铺满宿主窗口（预览窗本身即 16:9），消除上下/左右黑边，与 VLC 预览窗一致
                MpvLib.SetOption(_ctx, "video-aspect-override", "16:9");
                MpvLib.SetOption(_ctx, "panscan", "1"); // 铺满窗口，避免任何方向残留黑边
                MpvLib.SetOption(_ctx, "osc", "no");
                MpvLib.SetOption(_ctx, "osd-level", "0");
                MpvLib.SetOption(_ctx, "no-terminal", "yes");
                MpvLib.SetOption(_ctx, "idle", "yes");
                MpvLib.SetOption(_ctx, "hwdec", "auto"); // 硬件解码：1080p H.264/H.265 走 GPU，降低软解 CPU 占用与掉帧
                MpvLib.SetOption(_ctx, "hwdec-codecs", "all");
                int r = MpvLib.Initialize(_ctx);
                if (r != 0)
                {
                    ShowFallback("mpv 初始化失败（code " + r + "）");
                    _ctx = IntPtr.Zero;
                    return;
                }
                _available = true;
                _poll.Start();
            }
            catch (DllNotFoundException)
            {
                ShowFallback("未找到 libmpv-2.dll，请将它放到程序目录或系统 PATH");
            }
            catch (Exception ex)
            {
                ShowFallback("mpv 错误：" + ex.Message);
            }
        }

        private void ShowFallback(string msg)
        {
            _available = false;
            _fallbackLabel.Text = msg;
            _fallback.Visible = true;
            _host.Visible = false;
        }

        // ---------------- IChannelPlayer ----------------

        public string CurrentUrl => _url;
        public string CurrentName => _name;
        public bool IsSwitching => false;
        public bool IsPlaying => _isPlaying;
        public bool IsHlsMeasurementActive => _hlsProbe != null;

        public void LoadChannel(string url, string name)
        {
            _url = url;
            _name = name;
            StopHlsProbe();
            // 重置链接速度平滑基准，新频道从 0 重新起步（避免用上一频道的累计字节算出离谱速率）
            _statLastReadBytes = 0L;
            _statLastTickMs = 0L;
            _statSmoothBytesPerSec = 0f;
            // HLS 源：启动独立分片探针兜底链接速度（与 VLC 一致），
            // 同时置 IsHlsMeasurementActive 让主窗体 Tier A 探针让位，避免重复拉流。
            if (ChannelPlayer.UseHlsProbeFallback && ChannelPlayer.IsHlsUrl(url))
            {
                try
                {
                    _hlsProbe = new HlsLinkProbe();
                    _hlsProbe.Start(url);
                    ScheduleHlsProbeAutoStop();
                }
                catch
                {
                    _hlsProbe = null;
                }
            }
            EnsureMpv();
            if (!_available)
            {
                return;
            }
            MpvLib.Command(_ctx, "loadfile", url, "replace");
            _isPlaying = true;
            UpdatePlayIcon();
        }

        public void Stop()
        {
            if (_available && _ctx != IntPtr.Zero)
            {
                MpvLib.Command(_ctx, "stop");
            }
            StopHlsProbe();
            // 停止时清空链接速度平滑基准，下次播放重新起步
            _statLastReadBytes = 0L;
            _statLastTickMs = 0L;
            _statSmoothBytesPerSec = 0f;
            _isPlaying = false;
            UpdatePlayIcon();
        }

        public void StopAsync() => Stop();

        public void SetMute(bool mute) => MpvLib.SetProperty(_ctx, "mute", mute ? "yes" : "no");
        public void SetVolume(int volume) => MpvLib.SetProperty(_ctx, "volume", volume.ToString());

        // 供 ChannelPlayer 在 MPV 引擎下转发的便捷方法（库形态，无需外部进程）
        public bool IsPaused => _available && _ctx != IntPtr.Zero && MpvLib.GetPropertyString(_ctx, "pause") == "yes";
        public void SetPaused(bool paused) => MpvLib.SetProperty(_ctx, "pause", paused ? "yes" : "no");
        public double? GetDuration() => MpvLib.GetPropertyDouble(_ctx, "duration");
        public double? GetTimePos() => MpvLib.GetPropertyDouble(_ctx, "time-pos");
        public void Seek(double sec) => MpvLib.Command(_ctx, "seek", sec.ToString("F3"), "absolute");

        public void ApplyTheme(AppTheme theme)
        {
            if (theme == null || _controlBar == null)
            {
                return;
            }
            _controlBar.BackColor = theme.Surface;
            foreach (Control c in _controlBar.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = theme.PlayBtnBg;
                    b.ForeColor = theme.PlayBtnText;
                }
                else if (c is Label l)
                {
                    l.ForeColor = theme.TextPrimary;
                }
                else if (c is TrackBar tb)
                {
                    tb.BackColor = theme.BgAlt;
                }
            }
            // 修复：fallback 面板和标签跟随主题
            if (_fallback != null)
            {
                _fallback.BackColor = theme.BgAlt;
            }
            if (_fallbackLabel != null)
            {
                _fallbackLabel.ForeColor = theme.TextPrimary;
            }
        }

        public bool TryGetLiveState(out bool isPlaying, out long timeMs, out long lengthMs)
        {
            isPlaying = false; timeMs = 0; lengthMs = 0;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            bool paused = MpvLib.GetPropertyString(_ctx, "pause") == "yes";
            double? pos = MpvLib.GetPropertyDouble(_ctx, "time-pos");
            double? dur = MpvLib.GetPropertyDouble(_ctx, "duration");
            isPlaying = _isPlaying && !paused;
            if (pos.HasValue) timeMs = (long)(pos.Value * 1000);
            if (dur.HasValue && dur.Value > 0 && dur.Value < 1e9) lengthMs = (long)(dur.Value * 1000);
            return true;
        }

        public bool TryGetStreamInfo(out string codec, out string resolution, out string fps, out string bitrate, out string audioChannels, out string audioSampleRate)
        {
            codec = resolution = fps = bitrate = audioChannels = audioSampleRate = null;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            // 视频编码（mpv 给出裸 fourcc，如 "h264"；无 CodecDescription 等价映射，直接展示）
            string vCodec = MpvLib.GetPropertyString(_ctx, "video-codec");
            string aCodec = MpvLib.GetPropertyString(_ctx, "audio-codec");
            // 合并视频+音频编码，与 A（ChannelPlayer）风格一致："h264 + aac"
            if (!string.IsNullOrEmpty(vCodec))
            {
                codec = vCodec;
                if (!string.IsNullOrEmpty(aCodec) && !vCodec.Contains(aCodec))
                {
                    codec = vCodec + " + " + aCodec;
                }
            }
            else if (!string.IsNullOrEmpty(aCodec))
            {
                codec = aCodec;
            }

            double? w = MpvLib.GetPropertyDouble(_ctx, "width");
            double? h = MpvLib.GetPropertyDouble(_ctx, "height");
            if (w.HasValue && h.HasValue && w.Value > 0 && h.Value > 0)
            {
                resolution = $"{(int)w.Value}x{(int)h.Value}";
            }

            double? f = MpvLib.GetPropertyDouble(_ctx, "fps");
            if (f.HasValue && f.Value > 0) fps = f.Value.ToString("F2");

            // 码率（bits/s → kb/s）。优先视频码率，回退音频码率，对齐 A 的 track.Bitrate 文本格式
            double? vBr = MpvLib.GetPropertyDouble(_ctx, "video-bitrate");
            double? aBr = MpvLib.GetPropertyDouble(_ctx, "audio-bitrate");
            double? br = (vBr.HasValue && vBr.Value > 0) ? vBr : aBr;
            if (br.HasValue && br.Value > 0)
            {
                bitrate = $"{br.Value / 1000.0:F1} kb/s";
            }

            // 声道数（如 2.0 → "2 声道"）
            double? ch = MpvLib.GetPropertyDouble(_ctx, "audio-channels");
            if (ch.HasValue && ch.Value > 0)
            {
                audioChannels = $"{(int)Math.Round(ch.Value)} 声道";
            }

            // 音频采样率（Hz）
            double? sr = MpvLib.GetPropertyDouble(_ctx, "audio-samplerate");
            if (sr.HasValue && sr.Value > 0)
            {
                audioSampleRate = $"{(int)sr.Value} Hz";
            }

            return !string.IsNullOrEmpty(codec) || !string.IsNullOrEmpty(resolution) || !string.IsNullOrEmpty(audioChannels);
        }

        public bool TryGetRate(out float rate)
        {
            rate = 0f;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            double? v = MpvLib.GetPropertyDouble(_ctx, "speed");
            if (v.HasValue)
            {
                rate = (float)v.Value;
                return true;
            }
            return false;
        }

        public bool TryGetPlayerStats(out long readBytes, out float inputBitrate, out int decodedFrames, out int displayedFrames)
        {
            readBytes = 0; inputBitrate = 0f; decodedFrames = 0; displayedFrames = 0;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            // 注意：刚开播/缓冲期 read-bytes 可能很小、decoded/displayed 可能为 0，
            // 不能用「任一>0」做门槛，否则瞬时速度采样会反复失败清零，导致"链接速度"永远为 0。
            // 只要 mpv 上下文可用、属性读取接口不报错，就返回 true，交给 UI 层判断。
            long? rb = MpvLib.GetPropertyInt64(_ctx, "read-bytes");
            if (rb.HasValue) readBytes = rb.Value;
            // HLS 源：MPV 原生 read-bytes 对分片流不可靠，改用分片探针真实下载字节，
            // 供主窗体 Δbytes/Δtime 推算出实时链接速度（与 VLC 行为一致）。
            if (_hlsProbe != null) readBytes = _hlsProbe.TotalBytesRead;

            long? dec = MpvLib.GetPropertyInt64(_ctx, "decoded-frames");
            if (dec.HasValue) decodedFrames = (int)dec.Value;

            long? disp = MpvLib.GetPropertyInt64(_ctx, "displayed-frames");
            if (disp.HasValue) displayedFrames = (int)disp.Value;

            // ── 链接速度平滑（仿 VLC ReadBytes 三级融合）──
            // 优先用窗口化 readBytes 差值自算速率：readBytes 为 MPV 原生 read-bytes（直链）
            // 或分片探针真实下载字节（HLS），二者均为累计值，对其做 Δbytes/Δtime 即得真实传输速率。
            long nowMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (_statLastTickMs > 0L && readBytes >= _statLastReadBytes)
            {
                long dBytes = readBytes - _statLastReadBytes;
                long dMs = nowMs - _statLastTickMs;
                if (dMs > 0L && dBytes > 0L)
                {
                    // bytes/ms × 1000 = bytes/s
                    float instBytesPerSec = (float)dBytes / (float)dMs * 1000f;
                    // 内部 EWMA（0.4/0.6）：抑制单点尖刺；无增量（分片间隔平局）时不更新，
                    // 平滑继承上一帧速率而非冻结/突跳。
                    _statSmoothBytesPerSec = _statSmoothBytesPerSec <= 0f
                        ? instBytesPerSec
                        : _statSmoothBytesPerSec * 0.4f + instBytesPerSec * 0.6f;
                    // 仅在真实增量时推进基准：分片间隔被并入窗口，下一拍按「跨间隙累计字节/跨间隙时间」
                    // 计算，得到平滑平均速率而非瞬时空刺。
                    _statLastReadBytes = readBytes;
                    _statLastTickMs = nowMs;
                }
                // 平局期：不推进基准、不更新平滑值 → 平滑继承上一帧速率
            }
            else
            {
                // readBytes 回退（探针重启等）→ 重置基准，避免用陈旧字节算出离谱速率
                _statLastReadBytes = readBytes;
                _statLastTickMs = nowMs;
            }

            // 平滑值可用则直接返回（已是 bytes/s，与契约一致）；否则回退 MPV 原生 input-bitrate。
            // 主窗体还会再做一次 0.5 EWMA，形成两级平滑，与 VLC 显示观感一致。
            if (_statSmoothBytesPerSec > 0f)
            {
                inputBitrate = _statSmoothBytesPerSec;
            }
            else
            {
                double? ibr = MpvLib.GetPropertyDouble(_ctx, "input-bitrate");
                if (ibr.HasValue && ibr.Value > 0.0) inputBitrate = (float)ibr.Value;
            }

            return true;
        }

        public bool TryGetBufferDuration(out float seconds, float knownBps = 0f)
        {
            seconds = 0f;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            // mpv 的 demuxer-cache-duration 属性：当前 demuxer 缓冲的可播放时长（秒）
            double? dur = MpvLib.GetPropertyDouble(_ctx, "demuxer-cache-duration");
            if (dur.HasValue && dur.Value > 0)
            {
                seconds = (float)dur.Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 丢帧数：mpv 的 frame-drop-count（含解码+渲染丢帧）。total 用 estimated-frame-count 估算。
        /// </summary>
        public bool TryGetDroppedFrames(out int droppedFrames, out int totalFrames)
        {
            droppedFrames = 0;
            totalFrames = 0;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                long? drop = MpvLib.GetPropertyInt64(_ctx, "frame-drop-count");
                if (drop.HasValue) droppedFrames = (int)drop.Value;
                long? total = MpvLib.GetPropertyInt64(_ctx, "estimated-frame-count");
                if (total.HasValue) totalFrames = (int)total.Value;
                return true;
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 直播延迟：mpv 下以 demuxer-cache-duration（缓冲领先量）近似，对直播流二者相关性高。
        /// </summary>
        public bool TryGetLiveLatency(out float seconds)
        {
            seconds = 0f;
            if (!_available || _ctx == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                double? dur = MpvLib.GetPropertyDouble(_ctx, "demuxer-cache-duration");
                if (dur.HasValue && dur.Value > 0)
                {
                    seconds = (float)dur.Value;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private void StopHlsProbe()
        {
            try { _hlsProbeAutoStop?.Dispose(); }
            catch { }
            _hlsProbeAutoStop = null;
            var p = _hlsProbe;
            _hlsProbe = null;
            if (p != null)
            {
                try { p.Dispose(); }
                catch { }
            }
        }

        /// <summary>启动一次性计时器：HlsProbeWarmupMs 后自动停止 HLS 分片探针，
        /// 释放被探针占用的第二条连接带宽，根治高码率源播放卡顿。</summary>
        private void ScheduleHlsProbeAutoStop()
        {
            try { _hlsProbeAutoStop?.Dispose(); }
            catch { }
            _hlsProbeAutoStop = new System.Threading.Timer(delegate
            {
                StopHlsProbe();
            }, null, HlsProbeWarmupMs, System.Threading.Timeout.Infinite);
        }

        // ---------------- 交互 ----------------

        private void TogglePlay()
        {
            if (!_available || _ctx == IntPtr.Zero)
            {
                return;
            }
            bool paused = MpvLib.GetPropertyString(_ctx, "pause") == "yes";
            MpvLib.SetProperty(_ctx, "pause", paused ? "no" : "yes");
            _isPlaying = paused; // 若原本暂停则恢复播放
            UpdatePlayIcon();
        }

        private void ToggleMute()
        {
            if (!_available || _ctx == IntPtr.Zero)
            {
                return;
            }
            bool muted = MpvLib.GetPropertyString(_ctx, "mute") == "yes";
            MpvLib.SetProperty(_ctx, "mute", muted ? "no" : "yes");
            // muted 为「切换前」状态：原本静音→现在恢复出声（正常字体），反之加删除线
            _btnMute.Font = (muted ? _fontVol : _fontVolMuted);
        }

        private void ToggleFullscreen()
        {
            if (!_available || _ctx == IntPtr.Zero)
            {
                return;
            }
            bool fs = MpvLib.GetPropertyString(_ctx, "fullscreen") == "yes";
            MpvLib.SetProperty(_ctx, "fullscreen", fs ? "no" : "yes");
        }

        private void DoSeek()
        {
            if (!_available || _ctx == IntPtr.Zero || _dragging)
            {
                return;
            }
            double? dur = MpvLib.GetPropertyDouble(_ctx, "duration");
            if (!dur.HasValue || dur.Value <= 0)
            {
                return;
            }
            double target = _seek.Value / 1000.0 * dur.Value;
            MpvLib.Command(_ctx, "seek", target.ToString("F2"), "absolute");
        }

        private void UpdatePlayIcon()
        {
            if (_btnPlay != null && !_btnPlay.IsDisposed)
            {
                _btnPlay.Text = _isPlaying ? "⏸" : "▶";
            }
        }

        private void PollTick(object sender, EventArgs e)
        {
            if (!_available || _ctx == IntPtr.Zero)
            {
                return;
            }
            double? pos = MpvLib.GetPropertyDouble(_ctx, "time-pos");
            double? dur = MpvLib.GetPropertyDouble(_ctx, "duration");
            bool paused = MpvLib.GetPropertyString(_ctx, "pause") == "yes";
            _isPlaying = !paused;

            if (dur.HasValue && dur.Value > 0 && dur.Value < 1e9)
            {
                _seek.Enabled = true;
                if (!_dragging)
                {
                    _seek.Value = (int)(pos.GetValueOrDefault(0) / dur.Value * 1000);
                }
                _timeLabel.Text = $"{Fmt(pos)}{Fmt(dur)}";
            }
            else
            {
                _seek.Enabled = false;
                _seek.Value = 0;
                _timeLabel.Text = "直播 " + Fmt(pos);
            }
            UpdatePlayIcon();
        }

        private static string Fmt(double? sec)
        {
            double s = sec.GetValueOrDefault(0);
            int m = (int)(s / 60);
            int ss = (int)(s % 60);
            return $"{m:D2}:{ss:D2}";
        }

        protected virtual void OnOpenExternalRequested()
        {
            if (!string.IsNullOrWhiteSpace(_url))
            {
                OpenExternalRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _btnMute?.Dispose();
                _fontVol?.Dispose();
                _fontVolMuted?.Dispose();
                _fontVol = null;
                _fontVolMuted = null;
                _poll.Stop();
                if (_ctx != IntPtr.Zero)
                {
                    try { MpvLib.TerminateDestroy(_ctx); }
                    catch { }
                    _ctx = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }
    }
}
