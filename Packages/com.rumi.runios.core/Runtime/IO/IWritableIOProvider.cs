#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides writable access to an abstract file-system target.<br/>
    /// 추상 파일 시스템 대상에 대한 쓰기 가능 접근을 제공합니다.
    /// </summary>
    public interface IWritableIOProvider : IIOProvider
    {
        /// <summary>
        /// Gets a writable node that points to this provider's root path.<br/>
        /// 이 프로바이더의 루트 경로를 가리키는 쓰기 가능 노드를 가져옵니다.
        /// </summary>
        new IOWriteNode rootNode => new IOWriteNode(this);
        IONode IIOProvider.rootNode => rootNode;

        /// <summary>
        /// Creates a writable provider rooted at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로를 새 루트로 삼는 쓰기 가능 프로바이더를 생성합니다.
        /// </summary>
        new IWritableIOProvider Recreate(RuniPath path);
        IIOProvider IIOProvider.Recreate(RuniPath path) => Recreate(path);

        /// <summary>
        /// Opens a stream for writing to the file at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일에 쓰기 위한 스트림을 엽니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to write.<br/>
        /// 쓸 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns a writable <see cref="Stream"/>.<br/>
        /// 비동기 작업이 완료되면 쓸 수 있는 <see cref="Stream"/>을 반환합니다.
        /// </returns>
        UniTask<Stream> OpenWrite(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a directory at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로에 디렉터리를 만듭니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative directory path to create.<br/>
        /// 만들 프로바이더 기준 디렉터리 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or overwrites the file at the specified provider-relative path and opens a writable stream.<br/>
        /// 지정된 프로바이더 기준 경로의 파일을 만들거나 덮어쓰고 쓸 수 있는 스트림을 엽니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to create.<br/>
        /// 만들 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns a writable <see cref="Stream"/>.<br/>
        /// 비동기 작업이 완료되면 쓸 수 있는 <see cref="Stream"/>을 반환합니다.
        /// </returns>
        UniTask<Stream> CreateFile(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the directory at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 디렉터리를 삭제합니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative directory path to delete.<br/>
        /// 삭제할 프로바이더 기준 디렉터리 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the file at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일을 삭제합니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to delete.<br/>
        /// 삭제할 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the specified bytes to the file at the specified provider-relative path, overwriting existing contents.<br/>
        /// 지정된 바이트 배열을 프로바이더 기준 경로의 파일에 쓰며, 기존 내용은 덮어씁니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to write.<br/>
        /// 쓸 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="bytes">
        /// The bytes to write.<br/>
        /// 쓸 바이트 배열입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        async UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// Writes the specified text to the file at the specified provider-relative path, overwriting existing contents.<br/>
        /// 지정된 문자열을 프로바이더 기준 경로의 파일에 쓰며, 기존 내용은 덮어씁니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to write.<br/>
        /// 쓸 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="text">
        /// The text to write.<br/>
        /// 쓸 문자열입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        async UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// Writes the specified lines to the file at the specified provider-relative path, overwriting existing contents.<br/>
        /// 지정된 문자열 시퀀스를 프로바이더 기준 경로의 파일에 줄 단위로 쓰며, 기존 내용은 덮어씁니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to write.<br/>
        /// 쓸 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="lines">
        /// The lines to write.<br/>
        /// 쓸 문자열 시퀀스입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        async UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await CreateFile(path, cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }
    }
}
