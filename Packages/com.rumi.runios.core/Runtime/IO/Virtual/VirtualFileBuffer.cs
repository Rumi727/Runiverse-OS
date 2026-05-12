#nullable enable
namespace RuniOS.IO.Virtual
{
    public sealed class VirtualFileBuffer(int chunkSize = 64 * 1024)
    {
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

        public long length { get; private set; }

        public int chunkSize { get; } = chunkSize > 0
            ? chunkSize
            : throw new ArgumentOutOfRangeException(nameof(chunkSize));

        readonly Dictionary<long, byte[]> chunks = [];

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