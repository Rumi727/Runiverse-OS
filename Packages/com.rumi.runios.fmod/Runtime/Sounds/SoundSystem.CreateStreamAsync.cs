#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// Asynchronously opens the encoded audio at the specified <paramref name="node"/> and creates an FMOD stream.<br/>
        /// 지정된 <paramref name="node"/>의 인코딩된 오디오를 비동기로 열어 FMOD 스트림을 만듭니다.
        /// </summary>
        /// <param name="node">
        /// The node that identifies the encoded audio file to open.<br/>
        /// 열 인코딩된 오디오 파일을 식별하는 노드입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created FMOD audio clip.<br/>
        /// 비동기 작업이 완료되면 생성된 FMOD 오디오 클립을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the stream opened from <paramref name="node"/> is not readable, seekable, or representable by FMOD's file-length limit.<br/>
        /// <paramref name="node"/>에서 연 스트림이 읽기 또는 탐색할 수 없거나 FMOD 파일 길이 제한으로 표현할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this method is called while the sound system's native lock is held.<br/>
        /// 사운드 시스템의 네이티브 잠금이 잡힌 상태에서 이 메서드를 호출한 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// The returned clip owns the stream opened from <paramref name="node"/> and disposes it when the clip is disposed.<br/>
        /// 반환된 클립은 <paramref name="node"/>에서 연 스트림을 소유하며 클립을 해제할 때 함께 해제합니다.
        /// </remarks>
        public async UniTask<WaveAudioClip> CreateStreamAsync(IONode node)
        {
            ThrowIfSystemLockHeld();

            Stream stream = await node.file.OpenRead();
            return await CreateStreamAsync(stream);
        }

        /// <summary>
        /// Asynchronously creates an FMOD stream that reads encoded audio directly from the specified <see cref="Stream"/>.<br/>
        /// 지정된 <see cref="Stream"/>에서 인코딩된 오디오를 직접 읽는 FMOD 스트림을 비동기로 만듭니다.
        /// </summary>
        /// <param name="stream">
        /// The readable, seekable stream that supplies the encoded audio data.<br/>
        /// 인코딩된 오디오 데이터를 제공하는 읽기 및 탐색 가능한 스트림입니다.
        /// </param>
        /// <param name="leaveOpen">
        /// <see langword="true"/> to keep <paramref name="stream"/> open when the returned clip is disposed; otherwise, <see langword="false"/>.<br/>
        /// 반환된 클립을 해제할 때 <paramref name="stream"/>을 열어 두려면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created FMOD audio clip.<br/>
        /// 비동기 작업이 완료되면 생성된 FMOD 오디오 클립을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.<br/>
        /// <paramref name="stream"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stream"/> is not readable, seekable, or representable by FMOD's file-length limit.<br/>
        /// <paramref name="stream"/>이 읽기 또는 탐색할 수 없거나 FMOD 파일 길이 제한으로 표현할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this method is called while the sound system's native lock is held.<br/>
        /// 사운드 시스템의 네이티브 잠금이 잡힌 상태에서 이 메서드를 호출한 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// FMOD accesses <paramref name="stream"/> until the returned <see cref="WaveAudioClip"/> is disposed.<br/>
        /// FMOD는 반환된 <see cref="WaveAudioClip"/>이 해제될 때까지 <paramref name="stream"/>에 접근합니다.
        /// </remarks>
        public UniTask<WaveAudioClip> CreateStreamAsync(Stream stream, bool leaveOpen = false)
        {
            ThrowIfSystemLockHeld();
            return UniTask.RunOnThreadPool(() => CreateStream(stream, leaveOpen));
        }
    }
}
