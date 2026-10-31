#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
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

        /*/// <summary>
        /// Creates a writable provider rooted at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로를 새 루트로 삼는 쓰기 가능 프로바이더를 생성합니다.
        /// </summary>
        new IWritableIOProvider Recreate(RuniPath path);
        IIOProvider IIOProvider.Recreate(RuniPath path) => Recreate(path);*/

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
        /// Moves the file at the specified provider-relative path to another path in this provider.<br/>
        /// 지정된 프로바이더 기준 경로의 파일을 이 프로바이더 안의 다른 경로로 이동합니다.
        /// </summary>
        /// <remarks>
        /// When <paramref name="sourcePath"/> and <paramref name="destinationPath"/> refer to the same file, such as a case-only rename on a case-insensitive file system, the file is renamed regardless of <paramref name="overwrite"/>.
        /// <br/><br/>
        /// The default implementation copies the file and then deletes the source.<br/>
        /// It treats different paths as different files, so providers whose different paths can refer to the same file must override this method.
        /// <br/><br/>
        /// <paramref name="sourcePath"/>와 <paramref name="destinationPath"/>가 대소문자를 구분하지 않는 파일 시스템에서 대소문자만 다른 이름처럼 같은 파일을 가리키면, <paramref name="overwrite"/>와 관계없이 파일 이름을 바꿉니다.
        /// <br/><br/>
        /// 기본 구현은 파일을 복사한 뒤 원본을 삭제합니다.<br/>
        /// 서로 다른 경로를 서로 다른 파일로 취급하므로, 다른 경로가 같은 파일을 가리킬 수 있는 프로바이더는 이 메서드를 재정의해야 합니다.
        /// </remarks>
        /// <param name="sourcePath">
        /// The provider-relative path of the file to move.<br/>
        /// 이동할 파일의 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="destinationPath">
        /// The provider-relative path the file is moved to.<br/>
        /// 파일을 이동할 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="overwrite">
        /// <see langword="true"/> to replace an existing file at <paramref name="destinationPath"/>; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="destinationPath"/>에 있는 기존 파일을 교체하려면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <exception cref="IOException">
        /// Thrown when <paramref name="overwrite"/> is <see langword="false"/> and another file or directory already exists at <paramref name="destinationPath"/>.<br/>
        /// <paramref name="overwrite"/>가 <see langword="false"/>이고 <paramref name="destinationPath"/>에 다른 파일이나 디렉터리가 이미 있는 경우 발생합니다.
        /// </exception>
        async UniTask MoveFile(RuniPath sourcePath, RuniPath destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            // 같은 파일에 복사하면 원본을 읽기 전에 비워 버리므로 아무것도 하지 않습니다.
            if (sourcePath == destinationPath)
                return;

            if (!overwrite && await GetEntry(destinationPath, cancellationToken) != null)
                throw new IOException($"The destination '{destinationPath}' already exists.");

            await using (Stream source = await OpenRead(sourcePath, cancellationToken))
            await using (Stream destination = await CreateFile(destinationPath, cancellationToken))
                await source.CopyToAsync(destination, 81920, cancellationToken);

            await DeleteFile(sourcePath, cancellationToken);
        }

        /// <summary>
        /// Moves the directory at the specified provider-relative path, including its contents, to another path in this provider.<br/>
        /// 지정된 프로바이더 기준 경로의 디렉터리를 내용과 함께 이 프로바이더 안의 다른 경로로 이동합니다.
        /// </summary>
        /// <remarks>
        /// The default implementation creates the destination directory, moves each child with <see cref="MoveFile"/> and <see cref="MoveDirectory"/>, and then deletes the source.
        /// <br/><br/>
        /// 기본 구현은 대상 디렉터리를 만들고 각 자식을 <see cref="MoveFile"/>과 <see cref="MoveDirectory"/>로 이동한 뒤 원본을 삭제합니다.
        /// </remarks>
        /// <param name="sourcePath">
        /// The provider-relative path of the directory to move.<br/>
        /// 이동할 디렉터리의 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="destinationPath">
        /// The provider-relative path the directory is moved to.<br/>
        /// 디렉터리를 이동할 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <exception cref="IOException">
        /// Thrown when a file or directory already exists at <paramref name="destinationPath"/>, when <paramref name="destinationPath"/> is <paramref name="sourcePath"/> or one of its descendants, or when <paramref name="sourcePath"/> is the root.<br/>
        /// <paramref name="destinationPath"/>에 파일이나 디렉터리가 이미 있거나, <paramref name="destinationPath"/>가 <paramref name="sourcePath"/> 자신 또는 그 하위 경로이거나, <paramref name="sourcePath"/>가 루트인 경우 발생합니다.
        /// </exception>
        async UniTask MoveDirectory(RuniPath sourcePath, RuniPath destinationPath, CancellationToken cancellationToken = default)
        {
            // 자기 자신이나 하위로 옮기면 옮긴 자식을 다시 옮기며 끝나지 않으므로 거부합니다. 빈 경로(루트)는 모든 경로의 상위입니다.
            if (sourcePath.IsEmpty() || destinationPath.StartsWith(sourcePath))
                throw new IOException($"The directory '{sourcePath}' cannot be moved to itself or its descendant '{destinationPath}'.");

            // 디렉터리를 합칠지는 사용하는 쪽이 정할 일이므로, 대상이 이미 있으면 거부합니다.
            if (await GetEntry(destinationPath, cancellationToken) != null)
                throw new IOException($"The destination '{destinationPath}' already exists.");

            await CreateDirectory(destinationPath, cancellationToken);

            // 이동하면서 원본 디렉터리가 바뀌므로, 자식 목록을 먼저 모두 가져옵니다.
            List<IOEntry> entries = await EnumerateEntries(sourcePath, false, cancellationToken).ToListAsync(cancellationToken);
            foreach (IOEntry entry in entries)
            {
                RuniPath childDestinationPath = destinationPath / entry.path.GetFileName();
                if (entry.isDirectory)
                    await MoveDirectory(entry.path, childDestinationPath, cancellationToken);
                else
                    await MoveFile(entry.path, childDestinationPath, cancellationToken: cancellationToken);
            }

            await DeleteDirectory(sourcePath, cancellationToken);
        }

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
