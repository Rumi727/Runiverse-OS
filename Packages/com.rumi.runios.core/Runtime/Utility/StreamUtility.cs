#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.Utility
{
    public static class StreamUtility
    {
        public static byte[] ReadToEnd(this Stream stream)
        {
            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            if (stream.CanSeek)
            {
                long length = stream.Length - stream.Position;
                if (length == 0)
                    return [];

                byte[] buffer = new byte[length];
                int totalBytesRead = 0;

                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead = stream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead);
                    if (bytesRead == 0)
                        throw new EndOfStreamException();

                    totalBytesRead += bytesRead;
                }

                return buffer;
            }

            using MemoryStream result = new MemoryStream();
            stream.CopyTo(result, 81920);
            return result.ToArray();
        }

        public static async UniTask<byte[]> ReadToEndAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            if (stream.CanSeek)
            {
                long length = stream.Length - stream.Position;
                if (length == 0)
                    return [];

                byte[] buffer = new byte[length];
                int totalBytesRead = 0;

                while (totalBytesRead < buffer.Length)
                {
                    int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken);
                    if (bytesRead == 0)
                        throw new EndOfStreamException();

                    totalBytesRead += bytesRead;
                }

                return buffer;
            }

            using MemoryStream result = new MemoryStream();
            await stream.CopyToAsync(result, 81920, cancellationToken);
            return result.ToArray();
        }
    }
}