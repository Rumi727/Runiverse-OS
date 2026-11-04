#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Provides a <see cref="Stream"/> view over a <see cref="VirtualFileBuffer"/>.<br/>
    /// <see cref="VirtualFileBuffer"/>에 대한 <see cref="Stream"/> 보기를 제공합니다.
    /// </summary>
    /// <param name="fileBuffer">
    /// The buffer used as the stream storage.<br/>
    /// 스트림 저장소로 사용할 버퍼입니다.
    /// </param>
    /// <param name="access">
    /// The read and write access allowed for the stream.<br/>
    /// 스트림에 허용할 읽기 및 쓰기 접근 권한입니다.
    /// </param>
    public sealed class VirtualFileBufferStream(VirtualFileBuffer fileBuffer, FileAccess access) : Stream
    {
        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanRead => access.HasFlag(FileAccess.Read);
        
        /// <inheritdoc/>
        public override bool CanWrite => access.HasFlag(FileAccess.Write);

        /// <inheritdoc/>
        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _position = value;
            }
        }
        long _position;

        /// <inheritdoc/>
        public override long Length => fileBuffer.length;

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (newPosition < 0)
                throw new IOException("Cannot seek before beginning.");

            Position = newPosition;
            return Position;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            return Read(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            if (!CanRead)
                throw new NotSupportedException();

            int read = fileBuffer.Read(Position, buffer);
            Position += read;
            return read;
        }

        // 메모리 안의 작업이라 기다릴 것이 없으므로, 기본 구현처럼 스레드 풀로 보내지 않고 바로 끝냅니다.
        // 스레드 풀로 보내면 메인 스레드에서 기다릴 때마다 다음 업데이트까지 미뤄집니다.

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<int>(Read(buffer.Span));
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));

            Write(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite)
                throw new NotSupportedException();

            fileBuffer.Write(Position, buffer);
            Position += buffer.Length;
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return default;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            if (!CanWrite)
                throw new NotSupportedException();

            fileBuffer.SetLength(value);
        }

        /// <inheritdoc/>
        public override void Flush() { }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
