#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IONode
    {
        /// <summary>
        /// 노드를 파일로 취급하여 파일 관련 읽기 작업을 수행할 수 있는 객체입니다.
        /// </summary>
        /// <param name="node">대상 노드입니다.</param>
        public readonly struct File(IONode node)
        {
            /// <summary>
            /// 이 노드가 나타내는 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
            /// <returns>파일이 존재하지 않으면 <see langword="null"/>을 반환합니다.</returns>
            public async UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default)
            {
                IOEntry? entry = await node.provider.GetEntry(node.path, cancellationToken);
                if (entry.HasValue && !entry.Value.isDirectory)
                    return entry;

                return null;
            }

            /// <summary>
            /// 이 노드가 나타내는 파일의 모든 바이트를 읽습니다.
            /// </summary>
            /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/>[]입니다.</returns>
            public UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken) => node.provider.ReadAllBytes(node.path, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일의 모든 텍스트를 읽습니다.
            /// </summary>
            /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
            public UniTask<string> ReadAllText(CancellationToken cancellationToken) => node.provider.ReadAllText(node.path, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일의 모든 줄을 읽습니다.
            /// </summary>
            /// <returns>파일의 모든 줄을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
            public IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken) => node.provider.ReadLines(node.path, cancellationToken);

            /// <summary>
            /// 이 노드가 나타내는 파일에서 읽기 위한 스트림을 엽니다.
            /// </summary>
            /// <returns>파일에서 열린 <see cref="Stream"/>입니다.</returns>
            public UniTask<Stream> OpenRead(CancellationToken cancellationToken) => node.provider.OpenRead(node.path, cancellationToken);
        }
    }
}