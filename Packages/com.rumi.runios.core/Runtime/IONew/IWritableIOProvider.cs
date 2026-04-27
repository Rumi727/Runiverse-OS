#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using System.Threading;

namespace RuniOS.IONew
{
    public interface IWritableIOProvider : IIOProvider
    {
        new IOHandle rootNode => new IOHandle(this);

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 데이터를 쓰기 위한 스트림을 엽니다. 파일이 이미 존재하면 기존 내용을 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일에 쓰기 위해 열린 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> OpenWrite(FilePath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리를 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 디렉토리 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        UniTask DirectoryDelete(FilePath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이 핸들러가 나타내는 파일을 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 파일 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        UniTask FileDelete(FilePath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 지정된 바이트 배열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="bytes">파일에 기록할 바이트 배열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteAllBytes(FilePath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await OpenWrite(path, cancellationToken);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 지정된 문자열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="text">파일에 기록할 문자열입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteAllText(FilePath path, string text, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await OpenWrite(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 문자열 목록을 한 줄씩 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="path">기록할 파일 경로입니다.</param>
        /// <param name="lines">파일에 기록할 문자열 목록입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        async UniTask WriteLines(FilePath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await OpenWrite(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }
    }
}