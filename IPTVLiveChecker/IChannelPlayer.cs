using System;

namespace IPTVLiveChecker
{
    /// <summary>
    /// 预览窗播放引擎统一契约。VLC(ChannelPlayer) 与 mpv(MpvPlayer) 都实现它，
    /// 因此主窗口只需持有 IChannelPlayer，即可在两种引擎间切换做对照，互不耦合。
    /// </summary>
    public interface IChannelPlayer : IDisposable
    {
        event EventHandler OpenExternalRequested;

        string CurrentUrl { get; }
        string CurrentName { get; }
        bool IsSwitching { get; }
        /// <summary>当前是否处于播放中（线程安全镜像）。主窗体据此在播放期停止竞争型测速探针，把带宽让给播放、根治高码率卡顿。</summary>
        bool IsPlaying { get; }
        /// <summary>当前是否已有 HLS 测量在运行（本地代理或分片探针）。主窗体据此停止 Tier A 独立探针，避免对 HLS 重复拉流。</summary>
        bool IsHlsMeasurementActive { get; }
        int ControlBarHeight { get; }
        /// <summary>底部控制栏面板。主窗体将其移出播放器、放到播放窗外侧(下方)独立承载，避免遮挡画面。</summary>
        System.Windows.Forms.Panel ControlBar { get; }
        /// <summary>控制栏显隐状态变化时通知（主窗体据此同步外部容器高度）。</summary>
        event EventHandler ControlBarVisibilityChanged;

        void LoadChannel(string url, string name);
        void Stop();
        void StopAsync();
        void ApplyTheme(AppTheme theme);
        void SetMute(bool mute);
        void SetVolume(int volume);

        bool TryGetLiveState(out bool isPlaying, out long timeMs, out long lengthMs);
        bool TryGetStreamInfo(out string codec, out string resolution, out string fps, out string bitrate, out string audioChannels, out string audioSampleRate);
        bool TryGetRate(out float rate);
        bool TryGetPlayerStats(out long readBytes, out float inputBitrate, out int decodedFrames, out int displayedFrames);
        /// <summary>尝试获取缓冲时长（秒）。MPV 通过 demuxer-cache-duration；VLC 优先用 knownBps(实时传输速率 bps) 估算
        /// 缓冲领先量（ReadBytes*8/bps - 已播放秒数），knownBps 缺省为 0 时回退到旧 DemuxReadBytes/InputBitrate 估算。</summary>
        bool TryGetBufferDuration(out float seconds, float knownBps = 0f);

        bool TryGetDroppedFrames(out int droppedFrames, out int totalFrames);

        bool TryGetLiveLatency(out float seconds);
    }
}
