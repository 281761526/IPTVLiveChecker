using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVLiveChecker
{
    /// <summary>
    /// 字节计数 Stream 装饰器（Tier B 精确模式，由 ChannelPlayer.UseMeasuredStreamProbe 控制启用）。
    /// 包装任意 Stream，每次读取累计字节数，供链接速度按字节级精确测量，
    /// 不依赖 LibVLC/VLC 内部统计（VLC 稳定播放后内部统计冻结，本计数器随真实收包持续增长）。
    /// CanSeek=false，适配直播流。
    /// </summary>
    public sealed class MeasuredStream : Stream
    {
        private readonly Stream _inner;
        private long _totalRead;

        public MeasuredStream(Stream inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>累计已读取字节数（线程安全）。</summary>
        public long TotalBytesRead => Interlocked.Read(ref _totalRead);

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("MeasuredStream 只读");
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _inner.Read(buffer, offset, count);
            if (n > 0) Interlocked.Add(ref _totalRead, n);
            return n;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int n = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            if (n > 0) Interlocked.Add(ref _totalRead, n);
            return n;
        }

        public override void Close()
        {
            try { _inner.Close(); }
            catch { }
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _inner.Dispose(); }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
}
