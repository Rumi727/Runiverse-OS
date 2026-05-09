#nullable enable
using Cysharp.Threading.Tasks;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IONode
    {
        /// <summary>
        /// 이 노드가 나타내는 파일의 MD5 해시 값을 계산합니다.
        /// </summary>
        /// <returns>파일의 MD5 해시 문자열입니다.</returns>
        public async UniTask<string> GetFileChecksum()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (provider is IPrecalculatedIOChecksum precalculated)
                return await precalculated.GetPrecalculatedChecksum(path);

            await using Stream stream = await provider.OpenRead(path);

            SynchronizationContext? callerContext = SynchronizationContext.Current;
            await UniTask.SwitchToThreadPool();

            using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);

            try
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    incrementalHash.AppendData(buffer, 0, bytesRead);

                string result = BitConverter.ToString(incrementalHash.GetHashAndReset());
                if (callerContext != null && SynchronizationContext.Current != callerContext)
                    await UniTask.SwitchToSynchronizationContext(callerContext);

                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
