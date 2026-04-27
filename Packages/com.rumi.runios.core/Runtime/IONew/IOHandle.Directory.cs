#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.Threading;

namespace RuniOS.IONew
{
    partial record struct IOHandle
    {
        public readonly struct Directory(IOHandle node)
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
            /// 이 핸들러가 나타내는 디렉토리를 삭제합니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            public UniTask Delete(CancellationToken cancellationToken = default) => node.provider.DirectoryDelete(node.path, cancellationToken);
        }
    }
}