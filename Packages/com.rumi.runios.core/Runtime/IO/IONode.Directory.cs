#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IONode
    {
        /// <summary>
        /// Provides directory-oriented read operations for an <see cref="IONode"/>.<br/>
        /// <see cref="IONode"/>에 대한 디렉터리 중심 읽기 작업을 제공합니다.
        /// </summary>
        /// <param name="node">
        /// The node whose path is treated as a directory.<br/>
        /// 디렉터리로 취급할 경로를 가진 노드입니다.
        /// </param>
        public readonly struct Directory(IONode node)
        {
            public UniTask<bool> Exists(CancellationToken cancellationToken = default) => node.provider.DirectoryExists(node.path, cancellationToken);

            /// <summary>
            /// Gets the directory entry represented by this node.<br/>
            /// 이 노드가 나타내는 디렉터리 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// When the asynchronous operation completes, returns the directory entry if this node points to a directory; otherwise, <see langword="null"/>.<br/>
            /// 비동기 작업이 완료되면 이 노드가 디렉터리를 가리키는 경우 해당 엔트리를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
            /// </returns>
            public async UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default)
            {
                IOEntry? entry = await node.provider.GetEntry(node.path, cancellationToken);
                if (entry.HasValue && entry.Value.isDirectory)
                    return entry;

                return null;
            }

            /// <summary>
            /// Gets direct directory entries under this node.<br/>
            /// 이 노드 아래의 직계 디렉터리 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of direct directory entries.<br/>
            /// 직계 디렉터리 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetDirectories(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, false, cancellationToken).Where(x => x.isDirectory);

            /// <summary>
            /// Gets directory entries under this node recursively.<br/>
            /// 이 노드 아래의 디렉터리 엔트리를 재귀적으로 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of recursive directory entries.<br/>
            /// 재귀적으로 발견된 디렉터리 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllDirectories(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, true, cancellationToken).Where(x => x.isDirectory);

            /// <summary>
            /// Gets direct file entries under this node.<br/>
            /// 이 노드 아래의 직계 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of direct file entries.<br/>
            /// 직계 파일 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, false, cancellationToken).Where(x => !x.isDirectory);

            /// <summary>
            /// Gets direct file entries under this node that match the specified wildcard patterns.<br/>
            /// 이 노드 아래의 직계 파일 엔트리 중 지정된 와일드카드 패턴과 일치하는 항목을 가져옵니다.
            /// </summary>
            /// <param name="wildcardPatterns">
            /// The wildcard patterns used to filter file paths.<br/>
            /// 파일 경로를 필터링하는 데 사용할 와일드카드 패턴입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of matching direct file entries.<br/>
            /// 일치하는 직계 파일 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) =>
                node.provider.EnumerateEntries(node.path, false, cancellationToken)
                    .Where(x => !x.isDirectory && wildcardPatterns.IsMatch(x.path));

            /// <summary>
            /// Gets file entries under this node recursively.<br/>
            /// 이 노드 아래의 파일 엔트리를 재귀적으로 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of recursive file entries.<br/>
            /// 재귀적으로 발견된 파일 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, true, cancellationToken).Where(x => !x.isDirectory);

            /// <summary>
            /// Gets file entries under this node recursively that match the specified wildcard patterns.<br/>
            /// 이 노드 아래의 파일 엔트리 중 지정된 와일드카드 패턴과 일치하는 항목을 재귀적으로 가져옵니다.
            /// </summary>
            /// <param name="wildcardPatterns">
            /// The wildcard patterns used to filter file paths.<br/>
            /// 파일 경로를 필터링하는 데 사용할 와일드카드 패턴입니다.
            /// </param>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// An asynchronous sequence of matching recursive file entries.<br/>
            /// 일치하는 재귀 파일 엔트리를 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) =>
                node.provider.EnumerateEntries(node.path, true, cancellationToken)
                    .Where(x => !x.isDirectory && wildcardPatterns.IsMatch(x.path));
        }
    }
}
