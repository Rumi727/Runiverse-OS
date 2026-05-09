#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IOWriteNode
    {
        /// <summary>
        /// 노드를 파일로 취급하여 파일 관련 I/O 작업을 수행할 수 있는 객체입니다.
        /// </summary>
        /// <param name="node">대상 노드입니다.</param>
        public readonly struct File(IOWriteNode node)
        {
            IONode.File readOnlyFile => ((IONode)node).file;

            /// <inheritdoc cref="IONode.File.GetEntry(CancellationToken)"/>
            public UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken) => readOnlyFile.GetEntry(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadAllBytes(CancellationToken)"/>
            public UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken) => readOnlyFile.ReadAllBytes(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadAllText(CancellationToken)"/>
            public UniTask<string> ReadAllText(CancellationToken cancellationToken) => readOnlyFile.ReadAllText(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadLines(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken) => readOnlyFile.ReadLines(cancellationToken);

            /// <inheritdoc cref="IONode.File.OpenRead(CancellationToken)"/>
            public UniTask<Stream> OpenRead(CancellationToken cancellationToken) => readOnlyFile.OpenRead(cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일에 지정된 바이트 배열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <param name="bytes">파일에 기록할 바이트 배열입니다.</param>
            public UniTask WriteAllBytes(byte[] bytes, CancellationToken cancellationToken = default) => node.provider.WriteAllBytes(node.path, bytes, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일에 지정된 문자열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <param name="text">파일에 기록할 문자열입니다.</param>
            public UniTask WriteAllText(string text, CancellationToken cancellationToken = default) => node.provider.WriteAllText(node.path, text, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일에 문자열 목록을 한 줄씩 씁니다. 파일이 이미 존재하면 덮어씁니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <param name="lines">파일에 기록할 문자열 목록입니다.</param>
            public UniTask WriteLines(IEnumerable<string> lines, CancellationToken cancellationToken = default) => node.provider.WriteLines(node.path, lines, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일에 데이터를 쓰기 위한 스트림을 엽니다. 파일이 이미 존재하면 기존 내용을 덮어씁니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>파일에 쓰기 위해 열린 <see cref="Stream"/>입니다.</returns>
            public UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default) => node.provider.OpenWrite(node.path, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일을 삭제합니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            public UniTask Delete(CancellationToken cancellationToken = default) => node.provider.FileDelete(node.path, cancellationToken);
        }
    }
}