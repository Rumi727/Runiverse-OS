#nullable enable
using FMOD;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip : RuniAudioClip, IDisposable, ISoundSystemResource
    {
        public static class Unsafe
        {
            public static WaveAudioClip CreateInstance(SoundSystem system, Sound sound, IDisposable? nativeLifetime = null) => new WaveAudioClip(system, sound, nativeLifetime);
        }

        WaveAudioClip(SoundSystem system, Sound sound, IDisposable? nativeLifetime = null)
        {
            this.system = system;

            native = sound;
            this.nativeLifetime = nativeLifetime;

            system.Register(this);

            sound.getDefaults(out float frequency, out _).ThrowIfNotOk();
            this.frequency = frequency;

            sound.getFormat(out SOUND_TYPE type, out SOUND_FORMAT format, out int channel, out int bits).ThrowIfNotOk();
            this.type = (SoundType)type;
            this.channel = channel;
            this.bits = bits;
            pcmFormat = format.ToPCMFormat();

            sound.getMode(out MODE mode).ThrowIfNotOk();
            isStream = mode.HasFlag(MODE.CREATESTREAM);

            keepCompressed = format == SOUND_FORMAT.BITSTREAM;

            sound.getLength(out uint samples, TIMEUNIT.PCM).ThrowIfNotOk();
            this.samples = samples;

            length = samples / frequency;
        }

        public SoundSystem system { get; }

        Sound native { get; }
        readonly ReaderWriterLockSlim nativeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        IDisposable? nativeLifetime;

        public override double length { get; }

        public uint samples { get; }

        public float frequency { get; }

        /// <summary>
        /// Gets the container or codec type determined when this clip was created.<br/>
        /// 이 클립을 만들 때 결정된 컨테이너 또는 코덱 형식을 가져옵니다.
        /// </summary>
        public SoundType type { get; }
        public int channel { get; }
        public int bits { get; }
        public PCMFormat? pcmFormat { get; }

        /// <summary>
        /// Gets whether this clip was created as an FMOD stream.<br/>
        /// 이 클립이 FMOD 스트림으로 생성되었는지 여부를 가져옵니다.
        /// </summary>
        public bool isStream { get; }

        public bool keepCompressed { get; }

        public bool isDisposed => Volatile.Read(ref _isDisposed);
        bool _isDisposed = false;

        /// <summary>
        /// Releases this clip and its owned native lifetime.<br/>
        /// 이 클립과 소유한 네이티브 수명을 해제합니다.
        /// </summary>
        /// <remarks>
        /// Repeated calls are ignored after this clip has been disposed.<br/>
        /// 이 클립이 해제된 뒤의 반복 호출은 무시됩니다.
        /// </remarks>
        public void Dispose() => system.Dispose(this);

        ~WaveAudioClip() => SoundSystem.LogUndisposedResource(this);

        void ISoundSystemResource.ReleaseUnmanagedResources()
        {
            nativeLock.EnterWriteLock();
            IDisposable? lifetime;

            try
            {
                if (_isDisposed)
                    return;

                RESULT result = native.release();

                if (result != RESULT.OK && result != RESULT.ERR_INVALID_HANDLE)
                    result.ThrowIfNotOk();

                _isDisposed = true;

                lifetime = nativeLifetime;
                nativeLifetime = null;
            }
            finally
            {
                nativeLock.ExitWriteLock();
            }

            try
            {
                lifetime?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
