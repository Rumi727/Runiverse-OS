#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IOWriteNode
    {
        /// <summary>
        /// Provides file-oriented read and write operations for an <see cref="IOWriteNode"/>.<br/>
        /// <see cref="IOWriteNode"/>에 대한 파일 중심 읽기 및 쓰기 작업을 제공합니다.
        /// </summary>
        /// <param name="node">
        /// The node whose path is treated as a file.<br/>
        /// 파일로 취급할 경로를 가진 노드입니다.
        /// </param>
        public readonly struct File(IOWriteNode node)
        {
            IONode.File readOnlyFile => ((IONode)node).file;

            /// <inheritdoc cref="IONode.File.GetEntry(CancellationToken)"/>
            public UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default) => readOnlyFile.GetEntry(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadAllBytes(CancellationToken)"/>
            public UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default) => readOnlyFile.ReadAllBytes(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadAllText(CancellationToken)"/>
            public UniTask<string> ReadAllText(CancellationToken cancellationToken = default) => readOnlyFile.ReadAllText(cancellationToken);

            /// <inheritdoc cref="IONode.File.ReadLines(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default) => readOnlyFile.ReadLines(cancellationToken);

            /// <inheritdoc cref="IONode.File.OpenRead(CancellationToken)"/>
            public UniTask<Stream> OpenRead(CancellationToken cancellationToken = default) => readOnlyFile.OpenRead(cancellationToken);

            /// <summary>
            /// Writes the specified bytes to the file represented by this node, overwriting existing contents.<br/>
            /// 지정된 바이트 배열을 이 노드가 나타내는 파일에 쓰며, 기존 내용은 덮어씁니다.
            /// </summary>
            /// <param name="bytes">
            /// The bytes to write.<br/>
            /// 쓸 바이트 배열입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask WriteAllBytes(byte[] bytes, CancellationToken cancellationToken = default) => node.provider.WriteAllBytes(node.path, bytes, cancellationToken);

            /// <summary>
            /// Writes the specified text to the file represented by this node, overwriting existing contents.<br/>
            /// 지정된 문자열을 이 노드가 나타내는 파일에 쓰며, 기존 내용은 덮어씁니다.
            /// </summary>
            /// <param name="text">
            /// The text to write.<br/>
            /// 쓸 문자열입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask WriteAllText(string text, CancellationToken cancellationToken = default) => node.provider.WriteAllText(node.path, text, cancellationToken);

            /// <summary>
            /// Writes the specified lines to the file represented by this node, overwriting existing contents.<br/>
            /// 지정된 문자열 시퀀스를 이 노드가 나타내는 파일에 줄 단위로 쓰며, 기존 내용은 덮어씁니다.
            /// </summary>
            /// <param name="lines">
            /// The lines to write.<br/>
            /// 쓸 문자열 시퀀스입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask WriteLines(IEnumerable<string> lines, CancellationToken cancellationToken = default) => node.provider.WriteLines(node.path, lines, cancellationToken);

            /// <summary>
            /// Opens a stream for writing to the file represented by this node.<br/>
            /// 이 노드가 나타내는 파일에 쓰기 위한 스트림을 엽니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// When the asynchronous operation completes, returns a writable <see cref="Stream"/>.<br/>
            /// 비동기 작업이 완료되면 쓸 수 있는 <see cref="Stream"/>을 반환합니다.
            /// </returns>
            public UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default) => node.provider.OpenWrite(node.path, cancellationToken);

            /// <summary>
            /// Deletes the file represented by this node.<br/>
            /// 이 노드가 나타내는 파일을 삭제합니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask Delete(CancellationToken cancellationToken = default) => node.provider.DeleteFile(node.path, cancellationToken);
        }
    }
}
