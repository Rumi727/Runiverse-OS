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

            using MemoryStream result = new MemoryStream();
            stream.CopyTo(result, 81920);
            return result.ToArray();
        }

        public static async UniTask<byte[]> ReadToEndAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            using MemoryStream result = new MemoryStream();
            await stream.CopyToAsync(result, 81920, cancellationToken);
            return result.ToArray();
        }
    }
}