#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IOWriteNode
    {
        /// <summary>
        /// Provides directory-oriented read and write operations for an <see cref="IOWriteNode"/>.<br/>
        /// <see cref="IOWriteNode"/>에 대한 디렉터리 중심 읽기 및 쓰기 작업을 제공합니다.
        /// </summary>
        /// <param name="node">
        /// The node whose path is treated as a directory.<br/>
        /// 디렉터리로 취급할 경로를 가진 노드입니다.
        /// </param>
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
            /// Creates the directory represented by this node.<br/>
            /// 이 노드가 나타내는 디렉터리를 만듭니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask Create(CancellationToken cancellationToken = default) => node.provider.CreateDirectory(node.path, cancellationToken);

            /// <summary>
            /// Deletes the directory represented by this node.<br/>
            /// 이 노드가 나타내는 디렉터리를 삭제합니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            public UniTask Delete(CancellationToken cancellationToken = default) => node.provider.DeleteDirectory(node.path, cancellationToken);
        }
    }
}
