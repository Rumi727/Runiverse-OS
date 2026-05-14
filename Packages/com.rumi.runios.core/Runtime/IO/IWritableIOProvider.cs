#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    public interface IWritableIOProvider : IIOProvider
    {
        /// <summary>
        /// 이 프로바이더의 최상위 루트 경로를 가리키는 쓰기 가능한 노드를 가져옵니다.
        /// </summary>
        new IOWriteNode rootNode => new IOWriteNode(this);
        IONode IIOProvider.rootNode => rootNode;

        /// <summary>
        /// 지정된 경로를 새 루트로 삼는 쓰기 가능 프로바이더를 생성합니다.
        /// </summary>
        new IWritableIOProvider Recreate(RuniPath path);
        IIOProvider IIOProvider.Recreate(RuniPath path) => Recreate(path);

        /// <summary>
        /// 지정된 경로의 파일에 데이터를 쓰기 위한 스트림을 엽니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일에 쓰기 위해 열린 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> OpenWrite(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로에 디렉토리를 만듭니다.
        /// </summary>
        /// <param name="path">만들 디렉토리 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로에 새 파일을 쓰기 위한 스트림을 엽니다. 파일이 이미 존재하면 기존 내용을 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일에 쓰기 위해 열린 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> CreateFile(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로의 디렉토리를 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 디렉토리 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로의 파일을 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 파일 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로의 파일에 지정된 바이트 배열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="bytes">파일에 기록할 바이트 배열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// 지정된 경로의 파일에 지정된 문자열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="text">파일에 기록할 문자열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// 지정된 경로의 파일에 문자열 목록을 한 줄씩 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="lines">파일에 기록할 문자열 목록입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }
    }
}