#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.IO;
using System.Threading;

namespace RuniOS.IONew
{
    public interface IIOProvider : IDisposable
    {
        IONode rootNode => new IONode(this);

        /// <summary>
        /// 이 <see cref="IIOEntry"/> 인스턴스가 참조하는 파일 시스템의 구조가 <b>외부 요인에 의해 임의로 변경되지 않는 독립적인 상태</b>인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 핸들러가 가리키는 경로 내부의 구조가 해당 핸들러 또는 개발자에 의해 제어되며, OS나 다른 외부 프로그램에 의해 마음대로 바뀔 수 없는 경우 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <para>이 속성은 <see cref="IIOEntry"/>의 구체적인 구현에 따라 다르게 동작합니다:</para>
        /// <list type="bullet">
        /// <item><description>
        ///   <see langword="true"/>를 반환하는 경우: <see cref="IIOEntry"/>가 에셋 번들, 압축 파일(.zip, .jar 등),
        ///   또는 <see cref="VirtualDirectory"/>와 같이 자체적인 내부 구조를 가지며 외부에서 구조 변경이 어려운 대상을 참조할 때.<br/>
        ///   개발자가 직접 코드를 통해 구조를 정의하거나 변경하는 가상 파일 시스템 또한 여기에 해당합니다.
        /// </description></item>
        /// <item><description>
        ///   <see langword="false"/>를 반환하는 경우: <see cref="IIOEntry"/>가 파일 시스템의 일반적인 경로를 참조할 때.<br/>
        ///   이러한 경로는 OS나 다른 프로그램에 의해 디렉토리 구조나 파일이 임의로 생성, 삭제, 이동될 수 있으므로 독립적이지 않습니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        bool isIndependent { get; }

        UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default);
        IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default);

        UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default);

        async UniTask<byte[]> ReadAllBytes(FilePath path, CancellationToken cancellationToken = default)
        {
            await using Stream? stream = await OpenRead(path, cancellationToken);
            return await stream.ReadToEndAsync(cancellationToken);
        }

        async UniTask<string> ReadAllText(FilePath path, CancellationToken cancellationToken = default)
        {
            await using var stream = await OpenRead(path, cancellationToken);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

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