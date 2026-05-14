#nullable enable
using System.IO;

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

            if (Position >= Length)
                return 0;

            int available = Min(count, Length - Position).ClampToInt();
            if (available <= 0)
                return 0;

            for (int i = 0; i < available; i++)
                buffer[offset + i] = fileBuffer[Position + i];

            Position += available;
            return available;
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

            long endPosition = Position + count;
            if (endPosition > Length)
                fileBuffer.SetLength(endPosition);

            for (int i = 0; i < count; i++)
                fileBuffer[Position + i] = buffer[i + offset];

            Position = endPosition;
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
    }
}
