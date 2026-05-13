#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualFileBase : VirtualNode
    {
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenRead(CancellationToken cancellationToken = default);

        /// <summary>
        /// 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/> 배열입니다.</returns>
        public virtual async UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default)
        {
            await using Stream? stream = await OpenRead(cancellationToken);
            return await stream.ReadToEndAsync(cancellationToken);
        }

        /// <summary>
        /// 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        public virtual async UniTask<string> ReadAllText(CancellationToken cancellationToken = default)
        {
            await using var stream = await OpenRead(cancellationToken);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 파일의 모든 줄을 한 줄씩 읽어 비동기 스트림으로 제공합니다.
        /// </summary>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 각 줄을 제공하는 비동기 문자열 스트림입니다.</returns>
        public virtual IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<string>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            var ct = linkedCTS.Token;

            await using Stream stream = await OpenRead(ct);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                await writer.YieldAsync(line);
            }
        });


        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> Create(CancellationToken cancellationToken = default);

        /// <summary>
        /// 파일에 지정된 바이트 배열을 씁니다.
        /// </summary>
        /// <param name="bytes">파일에 기록할 바이트 배열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        public virtual async UniTask WriteAllBytes(byte[] bytes, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// 파일에 지정된 문자열을 씁니다.
        /// </summary>
        /// <param name="text">파일에 기록할 문자열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        public virtual async UniTask WriteAllText(string text, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// 파일에 문자열 목록을 한 줄씩 씁니다.
        /// </summary>
        /// <param name="lines">파일에 기록할 문자열 목록입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        public virtual async UniTask WriteLines(IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }
    }
}