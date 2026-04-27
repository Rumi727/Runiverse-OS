#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.Threading;

namespace RuniOS.IONew
{
    partial record struct IONode
    {
        public readonly struct Directory(IONode node)
        {
            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>디렉터리가 존재하지 않으면 <see langword="null"/>을 반환합니다.</returns>
            public async UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default)
            {
                IOEntry? entry = await node.provider.GetEntry(node.path, cancellationToken);
                if (entry.HasValue && entry.Value.isDirectory)
                    return entry;

                return null;
            }

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 내의 모든 디렉터리 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetDirectories(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, false, cancellationToken).Where(x => x.isDirectory);

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 디렉터리 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>모든 디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllDirectories(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, true, cancellationToken).Where(x => x.isDirectory);

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 내의 모든 파일 이름을 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, false, cancellationToken).Where(x => !x.isDirectory);

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>일치하는 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) =>
                node.provider.EnumerateEntries(node.path, false, cancellationToken)
                    .Where(x => !x.isDirectory && wildcardPatterns.IsMatch(x.path));

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(CancellationToken cancellationToken = default) => node.provider.EnumerateEntries(node.path, true, cancellationToken).Where(x => !x.isDirectory);

            /// <summary>
            /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>일치하는 모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<IOEntry> GetAllFiles(WildcardPatterns wildcardPatterns, CancellationToken cancellationToken = default) =>
                node.provider.EnumerateEntries(node.path, true, cancellationToken)
                    .Where(x => !x.isDirectory && wildcardPatterns.IsMatch(x.path));
        }
    }
}