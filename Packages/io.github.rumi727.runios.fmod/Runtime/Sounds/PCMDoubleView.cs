#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Sounds
{
    public readonly ref struct PCMDoubleView(ReadOnlySpan<byte> source, PCMFormat format)
    {
        readonly ReadOnlySpan<byte> source = source;
        readonly ReadOnlySpan<float> floatSource = format == PCMFormat.Float ? MemoryMarshal.Cast<byte, float>(source) : default;

        public double this[int index]
        {
            get
            {
                if ((uint)index >= (uint)length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (format == PCMFormat.Float)
                    return floatSource[index];

                return ReadSample(source, checked(index * bytesPerSample), format);
            }
        }

        public int length => source.Length / bytesPerSample;

        readonly int bytesPerSample = format switch
        {
            PCMFormat.PCM8 => 1,
            PCMFormat.PCM16 => 2,
            PCMFormat.PCM24 => 3,
            PCMFormat.PCM32 => 4,
            PCMFormat.Float => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        static double ReadSample(ReadOnlySpan<byte> source, int offset, PCMFormat format)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return format switch
            {
                PCMFormat.PCM8 => unchecked((sbyte)ReadByte(source, offset)) / 128d,
                PCMFormat.PCM16 => ReadInt16(source, offset) / 32768d,
                PCMFormat.PCM24 => ReadInt24(source, offset) / 8388608d,
                PCMFormat.PCM32 => ReadInt32(source, offset) / 2147483648d,
                _ => throw new InvalidOperationException($"FMOD returned unsupported sample format {format}.")
            };
        }

        static byte ReadByte(ReadOnlySpan<byte> source, int offset) => source[offset];

        static int ReadInt16(ReadOnlySpan<byte> source, int offset)
        {
            int value = ReadByte(source, offset) | (ReadByte(source, offset + 1) << 8);
            return unchecked((short)value);
        }

        static int ReadInt24(ReadOnlySpan<byte> source, int offset)
        {
            int value = ReadByte(source, offset) |
                (ReadByte(source, offset + 1) << 8) |
                (ReadByte(source, offset + 2) << 16);

            if ((value & 0b_10000000_00000000_00000000) != 0)
                value |= unchecked((int)0b_11111111_00000000_00000000_00000000);

            return value;
        }

        static int ReadInt32(ReadOnlySpan<byte> source, int offset) =>
            ReadByte(source, offset) |
            (ReadByte(source, offset + 1) << 8) |
            (ReadByte(source, offset + 2) << 16) |
            (ReadByte(source, offset + 3) << 24);
    }
}