#nullable enable
using System.IO;

namespace RuniOS.IO.Virtual
{
    public sealed class VirtualFileBufferStream(VirtualFileBuffer fileBuffer, FileAccess access) : Stream
    {
        public override bool CanSeek => true;

        public override bool CanRead => access.HasFlag(FileAccess.Read);
        public override bool CanWrite => access.HasFlag(FileAccess.Write);

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

        public override long Length => fileBuffer.length;

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

        public override void SetLength(long value)
        {
            if (!CanWrite)
                throw new NotSupportedException();

            fileBuffer.SetLength(value);
        }

        public override void Flush() { }
    }
}