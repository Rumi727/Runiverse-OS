#nullable enable
using ExtendedNumerics;
using System.Numerics;

namespace RuniOS.Utility
{
    public static class GuidUtility
    {
        static readonly Guid maxGuid = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");

        public static byte ToByte(this Guid value) => (byte)(value.ToBigInteger().Repeat(byte.MaxValue));
        public static ushort ToUInt16(this Guid value) => (ushort)(value.ToBigInteger().Repeat(ushort.MaxValue));
        public static uint ToUInt32(this Guid value) => (uint)(value.ToBigInteger().Repeat(uint.MaxValue));
        public static ulong ToUInt64(this Guid value) => (ulong)(value.ToBigInteger().Repeat(ulong.MaxValue));
        public static BigInteger ToBigInteger(this Guid value)
        {
            Span<byte> bytes = stackalloc byte[16];
            value.TryWriteBytes(bytes);

            return new BigInteger(bytes);
        }
        public static BigDecimal ToBigDecimal(this Guid value) => new BigDecimal(value.ToBigInteger());

        public static Guid ToGuid(this byte value) => FromBytes([value]);
        public static Guid ToGuid(this ushort value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ushort)];
            BitConverter.TryWriteBytes(bytes, value);

            return FromBytes(bytes);
        }
        public static Guid ToGuid(this uint value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            BitConverter.TryWriteBytes(bytes, value);

            return FromBytes(bytes);
        }
        public static Guid ToGuid(this ulong value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            BitConverter.TryWriteBytes(bytes, value);

            return FromBytes(bytes);
        }
        public static Guid ToGuid(this BigInteger value)
        {
            Span<byte> bytes = stackalloc byte[16];
            if (!value.TryWriteBytes(bytes, out int written, isUnsigned: true))
                return maxGuid;

            return FromBytes(bytes[..written]);
        }

        // 바이트 배열을 16바이트의 뒤쪽에 맞춰 Guid로 만듭니다.
        static Guid FromBytes(ReadOnlySpan<byte> bytes)
        {
            Span<byte> buffer = stackalloc byte[16];
            bytes.CopyTo(buffer[(buffer.Length - bytes.Length)..]);

            return new Guid(buffer);
        }
    }
}