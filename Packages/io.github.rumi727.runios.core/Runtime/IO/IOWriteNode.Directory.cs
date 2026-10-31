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

            /// <inheritdoc cref="IONode.Directory.Exists(CancellationToken)"/>
            public UniTask<bool> Exists(CancellationToken cancellationToken = default) => readOnlyDir.Exists(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetEntry(CancellationToken)"/>
            public UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default) => readOnlyDir.GetEntry(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetDirectories(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetDirectories(CancellationToken cancellationToken = default) => readOnlyDir.GetDirectories(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllDirectories(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllDirectories(CancellationToken cancellationToken = default) => readOnlyDir.GetAllDirectories(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetFiles(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(CancellationToken cancellationToken = default) => readOnlyDir.GetFiles(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetFiles(IPatternMatcher, CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(IPatternMatcher matcher, CancellationToken cancellationToken = default) => readOnlyDir.GetFiles(matcher, cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllFiles(CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(CancellationToken cancellationToken = default) => readOnlyDir.GetAllFiles(cancellationToken);

            /// <inheritdoc cref="IONode.Directory.GetAllFiles(IPatternMatcher, CancellationToken)"/>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(IPatternMatcher matcher, CancellationToken cancellationToken = default) => readOnlyDir.GetAllFiles(matcher, cancellationToken);

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

            /// <summary>
            /// Moves the directory represented by this node, including its contents, to another path in the same provider.<br/>
            /// 이 노드가 나타내는 디렉터리를 내용과 함께 같은 프로바이더 안의 다른 경로로 이동합니다.
            /// </summary>
            /// <param name="destinationPath">
            /// The provider-relative path the directory is moved to.<br/>
            /// 디렉터리를 이동할 프로바이더 기준 경로입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <exception cref="System.IO.IOException">
            /// Thrown when a file or directory already exists at <paramref name="destinationPath"/>, or when <paramref name="destinationPath"/> is this directory or one of its descendants.<br/>
            /// <paramref name="destinationPath"/>에 파일이나 디렉터리가 이미 있거나, <paramref name="destinationPath"/>가 이 디렉터리 자신 또는 그 하위 경로인 경우 발생합니다.
            /// </exception>
            public UniTask Move(RuniPath destinationPath, CancellationToken cancellationToken = default) => node.provider.MoveDirectory(node.path, destinationPath, cancellationToken);
        }
    }
}
