#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// 파일 시스템에 대한 읽기 전용 접근을 제공하는 프로바이더의 인터페이스입니다.
    /// 노드 객체들의 실제 I/O 작업을 처리하는 가상 파일 시스템의 기반 역할을 합니다.
    /// </summary>
    public interface IIOProvider : IDisposable
    {
        /// <summary>
        /// 이 프로바이더의 최상위 루트 경로를 가리키는 읽기 전용 노드를 가져옵니다.
        /// </summary>
        IONode rootNode => new IONode(this);

        /// <summary>
        /// 이 <see cref="IIOProvider"/> 인스턴스가 참조하는 파일 시스템의 구조가 <b>외부 요인에 의해 임의로 변경되지 않는 독립적인 상태</b>인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 이 프로바이더가 제공하는 시스템 내부의 구조가 해당 프로바이더 또는 개발자에 의해 제어되며, OS나 다른 외부 프로그램에 의해 마음대로 바뀔 수 없는 경우 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <para>이 속성은 <see cref="IIOProvider"/>의 구체적인 구현에 따라 다르게 동작합니다:</para>
        /// <list type="bullet">
        /// <item><description>
        ///   <see langword="true"/>를 반환하는 경우: 프로바이더가 에셋 번들, 압축 파일(.zip, .jar 등),
        ///   또는 <see cref="VirtualDirectory"/>와 같이 자체적인 내부 구조를 가지며 외부에서 구조 변경이 어려운 대상을 참조할 때.<br/>
        ///   개발자가 직접 코드를 통해 구조를 정의하거나 변경하는 가상 파일 시스템 또한 여기에 해당합니다.
        /// </description></item>
        /// <item><description>
        ///   <see langword="false"/>를 반환하는 경우: 프로바이더가 파일 시스템의 일반적인 경로를 참조할 때.<br/>
        ///   이러한 경로는 OS나 다른 프로그램에 의해 디렉토리 구조나 파일이 임의로 생성, 삭제, 이동될 수 있으므로 독립적이지 않습니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        bool isIndependent { get; }

        /// <summary>
        /// 지정된 경로에 대한 파일 또는 디렉터리 정보(스냅샷)를 가져옵니다.
        /// </summary>
        /// <param name="path">조회할 대상의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>해당 경로에 대상이 존재하면 <see cref="IOEntry"/>를 반환하고, 존재하지 않으면 <see langword="null"/>을 반환합니다.</returns>
        UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 디렉터리 경로 내에 있는 파일 및 하위 디렉터리들의 정보(스냅샷)를 비동기 스트림으로 열거합니다.
        /// </summary>
        /// <param name="path">열거할 기준 디렉터리의 가상 파일 시스템 경로입니다.</param>
        /// <param name="recursive"><see langword="true"/>이면 하위 디렉터리까지 재귀적으로 탐색합니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>검색된 대상들의 <see cref="IOEntry"/> 목록을 제공하는 비동기 스트림입니다.</returns>
        IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로의 파일에서 읽기 위한 스트림을 엽니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일에서 열린 읽기 전용 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default);

        /// <summary>
        /// 지정된 경로의 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/> 배열입니다.</returns>
        async UniTask<byte[]> ReadAllBytes(FilePath path, CancellationToken cancellationToken = default)
        {
            await using Stream? stream = await OpenRead(path, cancellationToken);
            return await stream.ReadToEndAsync(cancellationToken);
        }

        /// <summary>
        /// 지정된 경로의 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        async UniTask<string> ReadAllText(FilePath path, CancellationToken cancellationToken = default)
        {
            await using var stream = await OpenRead(path, cancellationToken);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 지정된 경로의 파일의 모든 줄을 한 줄씩 읽어 비동기 스트림으로 제공합니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 각 줄을 제공하는 비동기 문자열 스트림입니다.</returns>
        IUniTaskAsyncEnumerable<string> ReadLines(FilePath path, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<string>(async (writer, iterationToken) =>
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