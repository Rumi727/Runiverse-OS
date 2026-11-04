#nullable enable
namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Stores file bytes in fixed-size chunks and supports sparse reads as zero-filled bytes.<br/>
    /// 파일 바이트를 고정 크기 청크에 저장하고 비어 있는 영역을 0으로 읽을 수 있게 합니다.
    /// </summary>
    /// <param name="chunkSize">
    /// The size, in bytes, of each allocated chunk.<br/>
    /// 할당되는 각 청크의 바이트 크기입니다.
    /// </param>
    public sealed class VirtualFileBuffer(int chunkSize = 64 * 1024)
    {
        /// <summary>
        /// Gets or sets the byte at the specified file position.<br/>
        /// 지정된 파일 위치의 바이트를 가져오거나 설정합니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based byte position to access.<br/>
        /// 접근할 0부터 시작하는 바이트 위치입니다.
        /// </param>
        /// <returns>
        /// The stored byte, or <c>0</c> when the chunk has not been allocated.<br/>
        /// 저장된 바이트를 반환하며, 청크가 할당되지 않은 경우 <c>0</c>을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is negative or greater than or equal to <see cref="length"/>.<br/>
        /// <paramref name="index"/>가 음수이거나 <see cref="length"/>보다 크거나 같은 경우 발생합니다.
        /// </exception>
        public byte this[long index]
        {
            get
            {
                if (index < 0 || index >= length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                GetChunkIndex(index, out long chunkIndex, out int offset);
                if (chunks.TryGetValue(chunkIndex, out byte[]? buffer))
                    return buffer[offset];

                return 0;
            }
            set
            {
                if (index < 0 || index >= length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                GetChunkIndex(index, out long chunkIndex, out int chunkOffset);
                if (!chunks.TryGetValue(chunkIndex, out byte[]? buffer))
                {
                    buffer = new byte[chunkSize];
                    chunks[chunkIndex] = buffer;
                }

                buffer[chunkOffset] = value;
            }
        }

        /// <summary>
        /// Gets the logical length of the buffer in bytes.<br/>
        /// 버퍼의 논리적 길이를 바이트 단위로 가져옵니다.
        /// </summary>
        public long length { get; private set; }

        /// <summary>
        /// Gets the size, in bytes, of each allocated chunk.<br/>
        /// 할당되는 각 청크의 바이트 크기를 가져옵니다.
        /// </summary>
        public int chunkSize { get; } = chunkSize > 0
            ? chunkSize
            : throw new ArgumentOutOfRangeException(nameof(chunkSize));

        readonly Dictionary<long, byte[]> chunks = [];

        /// <summary>
        /// Changes the logical length of the buffer.<br/>
        /// 버퍼의 논리적 길이를 변경합니다.
        /// </summary>
        /// <param name="value">
        /// The new length in bytes.<br/>
        /// 새 바이트 길이입니다.
        /// </param>
        /// <remarks>
        /// Shrinking the buffer discards chunks outside the new length and clears bytes in the partially retained tail chunk.<br/>
        /// 버퍼를 줄이면 새 길이 밖의 청크를 제거하고 일부만 유지되는 끝 청크의 남은 바이트를 지웁니다.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is negative.<br/>
        /// <paramref name="value"/>가 음수인 경우 발생합니다.
        /// </exception>
        public void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (value == 0)
                Clear();

            if (value < length)
            {
                GetChunkIndex(value, out long chunkIndex, out int chunkOffset);
                long firstRemoveIndex = chunkOffset == 0 ? chunkIndex : chunkIndex + 1;

                foreach (var index in chunks.Keys.Where(x => x >= firstRemoveIndex).ToArray())
                    chunks.Remove(index);

                if (chunkOffset != 0)
                {
                    if (chunks.TryGetValue(chunkIndex, out byte[]? array))
                        Array.Clear(array, chunkOffset, array.Length - chunkOffset);
                }
            }

            length = value;
        }

        /// <summary>
        /// Copies bytes starting at the specified position into <paramref name="destination"/>.<br/>
        /// 지정된 위치부터 바이트를 <paramref name="destination"/>에 복사합니다.
        /// </summary>
        /// <param name="position">
        /// The zero-based byte position to start reading from.<br/>
        /// 읽기 시작할 0부터 시작하는 바이트 위치입니다.
        /// </param>
        /// <param name="destination">
        /// The span that receives the bytes. Unallocated chunks are read as zeros.<br/>
        /// 바이트를 받을 span입니다. 할당되지 않은 청크는 0으로 읽습니다.
        /// </param>
        /// <returns>
        /// The number of bytes copied, which is less than the length of <paramref name="destination"/> when the end of the buffer is reached.<br/>
        /// 복사한 바이트 수를 반환하며, 버퍼 끝에 닿으면 <paramref name="destination"/>의 길이보다 작습니다.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="position"/> is negative.<br/>
        /// <paramref name="position"/>이 음수인 경우 발생합니다.
        /// </exception>
        public int Read(long position, Span<byte> destination)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            int count = Min(destination.Length, Max(length - position, 0)).ClampToInt();
            for (int copied = 0; copied < count;)
            {
                GetChunkIndex(position + copied, out long chunkIndex, out int chunkOffset);
                int size = Min(chunkSize - chunkOffset, count - copied);

                Span<byte> target = destination.Slice(copied, size);
                if (chunks.TryGetValue(chunkIndex, out byte[]? buffer))
                    buffer.AsSpan(chunkOffset, size).CopyTo(target);
                else
                    target.Clear();

                copied += size;
            }

            return count;
        }

        /// <summary>
        /// Copies <paramref name="source"/> into the buffer starting at the specified position, extending the length when needed.<br/>
        /// <paramref name="source"/>를 지정된 위치부터 버퍼에 복사하며, 필요하면 길이를 늘립니다.
        /// </summary>
        /// <param name="position">
        /// The zero-based byte position to start writing at.<br/>
        /// 쓰기 시작할 0부터 시작하는 바이트 위치입니다.
        /// </param>
        /// <param name="source">
        /// The bytes to write.<br/>
        /// 쓸 바이트입니다.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="position"/> is negative.<br/>
        /// <paramref name="position"/>이 음수인 경우 발생합니다.
        /// </exception>
        public void Write(long position, ReadOnlySpan<byte> source)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            long endPosition = checked(position + source.Length);
            if (endPosition > length)
                SetLength(endPosition);

            for (int written = 0; written < source.Length;)
            {
                GetChunkIndex(position + written, out long chunkIndex, out int chunkOffset);
                int size = Min(chunkSize - chunkOffset, source.Length - written);

                if (!chunks.TryGetValue(chunkIndex, out byte[]? buffer))
                {
                    buffer = new byte[chunkSize];
                    chunks[chunkIndex] = buffer;
                }

                source.Slice(written, size).CopyTo(buffer.AsSpan(chunkOffset, size));
                written += size;
            }
        }

        /// <summary>
        /// Removes all allocated chunks and resets the logical length to zero.<br/>
        /// 할당된 모든 청크를 제거하고 논리적 길이를 0으로 재설정합니다.
        /// </summary>
        public void Clear()
        {
            chunks.Clear();
            length = 0;
        }

        void GetChunkIndex(long position, out long chunkIndex, out int chunkOffset)
        {
            if (position < 0 || position > length)
                throw new ArgumentOutOfRangeException(nameof(position));

            chunkIndex = position / chunkSize;
            chunkOffset = (position % chunkSize).ClampToInt();
        }
    }
}
