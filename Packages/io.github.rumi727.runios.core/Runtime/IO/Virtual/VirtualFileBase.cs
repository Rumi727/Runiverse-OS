#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Defines read and write operations for a virtual file node.<br/>
    /// 가상 파일 노드의 읽기 및 쓰기 작업을 정의합니다.
    /// </summary>
    public abstract class VirtualFileBase : VirtualNode
    {
        /// <summary>
        /// Opens the file for reading.<br/>
        /// 파일을 읽기용으로 엽니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the readable stream.<br/>
        /// 비동기 작업이 완료되면 읽기 가능한 스트림을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this <see cref="VirtualFileBase"/> instance has been deleted.<br/>
        /// 이 <see cref="VirtualFileBase"/> 인스턴스가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenRead(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads all bytes from the file.<br/>
        /// 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns a byte array containing the full file contents.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 포함하는 <see cref="byte"/> 배열을 반환합니다.
        /// </returns>
        public virtual async UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default)
        {
            await using Stream? stream = await OpenRead(cancellationToken);
            return await stream.ReadToEndAsync(cancellationToken);
        }

        /// <summary>
        /// Reads all text from the file.<br/>
        /// 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the full file contents as text.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 텍스트로 반환합니다.
        /// </returns>
        public virtual async UniTask<string> ReadAllText(CancellationToken cancellationToken = default)
        {
            await using var stream = await OpenRead(cancellationToken);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Reads the file as an asynchronous sequence of lines.<br/>
        /// 파일을 줄 단위 비동기 시퀀스로 읽습니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the read operation.<br/>
        /// 읽기 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous sequence that yields each line from the file.<br/>
        /// 파일의 각 줄을 제공하는 비동기 시퀀스입니다.
        /// </returns>
        public virtual IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<string>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            var ct = linkedCTS.Token;

            await using Stream stream = await OpenRead(ct);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 4096);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                await writer.YieldAsync(line);
            }
        });


        /// <summary>
        /// Opens the file for writing without clearing its current contents.<br/>
        /// 현재 내용을 지우지 않고 파일을 쓰기용으로 엽니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the writable stream.<br/>
        /// 비동기 작업이 완료되면 쓰기 가능한 스트림을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this <see cref="VirtualFileBase"/> instance has been deleted.<br/>
        /// 이 <see cref="VirtualFileBase"/> 인스턴스가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or truncates the file and opens it for writing.<br/>
        /// 파일을 만들거나 잘라낸 뒤 쓰기용으로 엽니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the writable stream for the recreated file contents.<br/>
        /// 비동기 작업이 완료되면 새로 만든 파일 내용을 쓰기 위한 스트림을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this <see cref="VirtualFileBase"/> instance has been deleted.<br/>
        /// 이 <see cref="VirtualFileBase"/> 인스턴스가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> Create(CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces the file contents with the specified bytes.<br/>
        /// 파일 내용을 지정된 바이트로 교체합니다.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to write to the file.<br/>
        /// 파일에 기록할 바이트입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the write.<br/>
        /// 쓰기 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        public virtual async UniTask WriteAllBytes(byte[] bytes, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// Replaces the file contents with the specified text.<br/>
        /// 파일 내용을 지정된 텍스트로 교체합니다.
        /// </summary>
        /// <param name="text">
        /// The text to write to the file.<br/>
        /// 파일에 기록할 텍스트입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the write.<br/>
        /// 쓰기 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        public virtual async UniTask WriteAllText(string text, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(text);
        }

        /// <summary>
        /// Replaces the file contents with the specified lines.<br/>
        /// 파일 내용을 지정된 줄 목록으로 교체합니다.
        /// </summary>
        /// <param name="lines">
        /// The lines to write to the file.<br/>
        /// 파일에 기록할 줄 목록입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the write.<br/>
        /// 쓰기 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        public virtual async UniTask WriteLines(IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            await using Stream stream = await Create(cancellationToken);
            await using StreamWriter writer = new StreamWriter(stream);
            foreach (var line in lines)
                await writer.WriteLineAsync(line);
        }
    }
}
