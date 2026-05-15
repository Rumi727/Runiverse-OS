#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    partial record struct IONode
    {
        /// <summary>
        /// Provides file-oriented read operations for an <see cref="IONode"/>.<br/>
        /// <see cref="IONode"/>에 대한 파일 중심 읽기 작업을 제공합니다.
        /// </summary>
        /// <param name="node">
        /// The node whose path is treated as a file.<br/>
        /// 파일로 취급할 경로를 가진 노드입니다.
        /// </param>
        public readonly struct File(IONode node)
        {
            /// <summary>
            /// Gets the file entry represented by this node.<br/>
            /// 이 노드가 나타내는 파일 엔트리를 가져옵니다.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the operation.<br/>
            /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
            /// </param>
            /// <returns>
            /// When the asynchronous operation completes, returns the file entry if this node points to a file; otherwise, <see langword="null"/>.<br/>
            /// 비동기 작업이 완료되면 이 노드가 파일을 가리키는 경우 해당 엔트리를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
            /// </returns>
            public async UniTask<IOEntry?> GetEntry(CancellationToken cancellationToken = default)
            {
                IOEntry? entry = await node.provider.GetEntry(node.path, cancellationToken);
                if (entry.HasValue && !entry.Value.isDirectory)
                    return entry;

                return null;
            }

            /// <summary>
            /// Reads all bytes from the file represented by this node.<br/>
            /// 이 노드가 나타내는 파일에서 모든 바이트를 읽습니다.
            /// </summary>
            /// <returns>
            /// When the asynchronous operation completes, returns the full file contents as a byte array.<br/>
            /// 비동기 작업이 완료되면 파일 전체 내용을 <see cref="byte"/> 배열로 반환합니다.
            /// </returns>
            public UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default) => node.provider.ReadAllBytes(node.path, cancellationToken);

            /// <summary>
            /// Reads all text from the file represented by this node.<br/>
            /// 이 노드가 나타내는 파일에서 모든 텍스트를 읽습니다.
            /// </summary>
            /// <returns>
            /// When the asynchronous operation completes, returns the full file contents as text.<br/>
            /// 비동기 작업이 완료되면 파일 전체 내용을 텍스트로 반환합니다.
            /// </returns>
            public UniTask<string> ReadAllText(CancellationToken cancellationToken = default) => node.provider.ReadAllText(node.path, cancellationToken);

            /// <summary>
            /// Reads the file represented by this node as an asynchronous sequence of lines.<br/>
            /// 이 노드가 나타내는 파일을 줄 단위 비동기 시퀀스로 읽습니다.
            /// </summary>
            /// <returns>
            /// An asynchronous sequence that yields each line from the file.<br/>
            /// 파일의 각 줄을 제공하는 비동기 시퀀스입니다.
            /// </returns>
            public IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default) => node.provider.ReadLines(node.path, cancellationToken);

            /// <summary>
            /// Opens a stream for reading the file represented by this node.<br/>
            /// 이 노드가 나타내는 파일을 읽기 위한 스트림을 엽니다.
            /// </summary>
            /// <returns>
            /// When the asynchronous operation completes, returns a readable <see cref="Stream"/>.<br/>
            /// 비동기 작업이 완료되면 읽을 수 있는 <see cref="Stream"/>을 반환합니다.
            /// </returns>
            public UniTask<Stream> OpenRead(CancellationToken cancellationToken = default) => node.provider.OpenRead(node.path, cancellationToken);
        }
    }
}
