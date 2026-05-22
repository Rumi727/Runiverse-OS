#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides read-only access to an abstract file-system target.<br/>
    /// Node types delegate their actual read operations to this provider by using provider-relative <see cref="RuniPath"/> values.
    /// <br/><br/>
    /// 추상 파일 시스템 대상에 대한 읽기 전용 접근을 제공합니다.<br/>
    /// 노드 타입은 프로바이더 기준 <see cref="RuniPath"/> 값을 사용해 실제 읽기 작업을 이 프로바이더에 위임합니다.
    /// </summary>
    public interface IIOProvider : IDisposable
    {
        /// <summary>
        /// Gets a read-only node that points to this provider's root path.<br/>
        /// 이 프로바이더의 루트 경로를 가리키는 읽기 전용 노드를 가져옵니다.
        /// </summary>
        IONode rootNode => new IONode(this);

        /// <summary>
        /// Gets a value indicating whether this provider owns a structure that is not expected to change outside the provider.<br/>
        /// Returns <see langword="true"/> for provider-controlled structures such as virtual directories or archives, and <see langword="false"/> for externally mutable targets such as physical directories.
        /// <br/><br/>
        /// 이 프로바이더가 외부에서 임의로 변경되지 않는 구조를 소유하는지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 가상 디렉터리나 압축 파일처럼 프로바이더가 제어하는 구조이면 <see langword="true"/>를 반환하고, 물리 디렉터리처럼 외부에서 변경될 수 있는 대상이면 <see langword="false"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <para>This property depends on the concrete <see cref="IIOProvider"/> implementation.</para>
        /// <list type="bullet">
        /// <item><description>
        /// Returns <see langword="true"/> when the provider refers to a structure controlled by the provider or developer.
        /// </description></item>
        /// <item><description>
        /// Returns <see langword="false"/> when the provider refers to a normal file-system location that may be changed by the OS or other programs.
        /// </description></item>
        /// </list>
        /// <br/><br/>
        /// <para>이 속성은 <see cref="IIOProvider"/>의 구체적인 구현에 따라 다르게 동작합니다.</para>
        /// <list type="bullet">
        /// <item><description>
        /// 프로바이더 또는 개발자가 제어하는 구조를 참조하는 경우 <see langword="true"/>를 반환합니다.
        /// </description></item>
        /// <item><description>
        /// OS나 다른 프로그램이 변경할 수 있는 일반 파일 시스템 위치를 참조하는 경우 <see langword="false"/>를 반환합니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        bool isIndependent { get; }

        /*/// <summary>
        /// Creates a read-only provider rooted at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로를 새 루트로 삼는 읽기 전용 프로바이더를 생성합니다.
        /// </summary>
        IIOProvider Recreate(RuniPath path);*/

        /// <summary>
        /// Determines whether this provider and another provider refer to the same underlying target.<br/>
        /// 이 프로바이더와 다른 프로바이더가 같은 내부 대상을 참조하는지 확인합니다.
        /// </summary>
        bool IsSameTarget(IIOProvider other);

        /// <summary>
        /// Gets a snapshot of the file or directory entry at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일 또는 디렉터리 엔트리 스냅샷을 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative path to inspect.<br/>
        /// 조회할 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the matching <see cref="IOEntry"/> if found; otherwise, <see langword="null"/>.<br/>
        /// 비동기 작업이 완료되면 엔트리를 찾은 경우 해당 <see cref="IOEntry"/>를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates entry snapshots under the specified provider-relative directory path.<br/>
        /// 지정된 프로바이더 기준 디렉터리 경로 아래의 엔트리 스냅샷을 열거합니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative directory path to enumerate.<br/>
        /// 열거할 프로바이더 기준 디렉터리 경로입니다.
        /// </param>
        /// <param name="recursive">
        /// <see langword="true"/> to enumerate descendants recursively; otherwise, <see langword="false"/>.<br/>
        /// 하위 항목을 재귀적으로 열거하려면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous sequence of discovered <see cref="IOEntry"/> values.<br/>
        /// 발견된 <see cref="IOEntry"/> 값을 제공하는 비동기 시퀀스입니다.
        /// </returns>
        IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a stream for reading the file at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일을 읽기 위한 스트림을 엽니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns a readable <see cref="Stream"/>.<br/>
        /// 비동기 작업이 완료되면 읽을 수 있는 <see cref="Stream"/>을 반환합니다.
        /// </returns>
        UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads all bytes from the file at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일에서 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the full file contents as a byte array.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 <see cref="byte"/> 배열로 반환합니다.
        /// </returns>
        async UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default)
        {
            await using Stream? stream = await OpenRead(path, cancellationToken);
            return await stream.ReadToEndAsync(cancellationToken);
        }

        /// <summary>
        /// Reads all text from the file at the specified provider-relative path.<br/>
        /// 지정된 프로바이더 기준 경로의 파일에서 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the full file contents as text.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 텍스트로 반환합니다.
        /// </returns>
        async UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default)
        {
            await using var stream = await OpenRead(path, cancellationToken);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Reads the file at the specified provider-relative path as an asynchronous sequence of lines.<br/>
        /// 지정된 프로바이더 기준 경로의 파일을 줄 단위 비동기 시퀀스로 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous sequence that yields each line from the file.<br/>
        /// 파일의 각 줄을 제공하는 비동기 시퀀스입니다.
        /// </returns>
        IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<string>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            var ct = linkedCTS.Token;

            await using Stream stream = await OpenRead(path, ct);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                await writer.YieldAsync(line);
            }
        });
    }
}
