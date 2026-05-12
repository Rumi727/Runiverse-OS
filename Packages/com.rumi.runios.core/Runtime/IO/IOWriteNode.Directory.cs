#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IOWriteNode
    {
        /// <summary>
        /// 노드를 디렉토리로 취급하여 디렉토리 관련 I/O 작업을 수행할 수 있는 객체입니다.
        /// </summary>
        /// <param name="node">대상 노드입니다.</param>
        public readonly struct Directory(IOWriteNode node)
        {
            IONode.Directory readOnlyDir => ((IONode)node).dir;

            /// <inheritdoc cref="IONode.Directory.GetEntry(CancellationToken)"/>
            public UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default) => readOnlyDir.GetEntry(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetDirectories(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetDirectories(CancellationToken cancellationToken = default) => readOnlyDir.GetDirectories(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllDirectories(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllDirectories(CancellationToken cancellationToken = default) => readOnlyDir.GetAllDirectories(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetFiles(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(CancellationToken cancellationToken = default) => readOnlyDir.GetFiles(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetFiles(WildcardPatterns, CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) => readOnlyDir.GetFiles(wildcardPatterns, cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllFiles(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(CancellationToken cancellationToken = default) => readOnlyDir.GetAllFiles(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllFiles(WildcardPatterns, CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) => readOnlyDir.GetAllFiles(wildcardPatterns, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 디렉토리를 삭제합니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            public UniTask Delete(CancellationToken cancellationToken = default) => node.provider.DeleteDirectory(node.path, cancellationToken);
        }
    }
}