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
                if (chunks.TryGetValue(chunkIndex, out byte[] buffer))
                    return buffer[offset];

                return 0;
            }
            set
            {
                if (index < 0 || index >= length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                GetChunkIndex(index, out long chunkIndex, out int chunkOffset);
                if (!chunks.TryGetValue(chunkIndex, out byte[] buffer))
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
                    if (chunks.TryGetValue(chunkIndex, out byte[] array))
                        Array.Clear(array, chunkOffset, array.Length - chunkOffset);
                }
            }

            length = value;
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
