#nullable enable
using FMOD;
using System.Collections.Concurrent;
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Wraps an FMOD playback channel created by <see cref="SoundSystem"/>.<br/>
    /// <see cref="SoundSystem"/>에서 생성한 FMOD 재생 채널을 래핑합니다.
    /// </summary>
    /// <remarks>
    /// FMOD owns the native channel lifetime. Native calls generally pass through without synchronization and may fail after the channel stops or is stolen.<br/>
    /// Compound wrapper operations use dedicated state-domain locks. Direct access through <see cref="native"/> bypasses that synchronization.<br/>
    /// FMOD invalidates a stopped channel handle, so callers that need to identify that expected condition must catch <see cref="FMODException"/> and check whether <see cref="FMODException.result"/> is <c>RESULT.ERR_INVALID_HANDLE</c>.<br/>
    /// When FMOD reports that the channel has stopped, this wrapper detaches its managed handle, unregisters itself, and invokes <see cref="onStop"/> once.<br/><br/>
    /// FMOD가 네이티브 채널 수명을 소유합니다. 네이티브 호출은 일반적으로 동기화 없이 전달되며 채널이 중지되거나 steal된 뒤 실패할 수 있습니다.<br/>
    /// 복합 래퍼 연산은 전용 상태 도메인 락으로 동기화됩니다. <see cref="native"/>에 직접 접근하면 이 동기화를 우회합니다.<br/>
    /// FMOD는 중지된 채널 핸들을 무효화하므로, 이 예상 상태를 구분해야 하는 호출자는 <see cref="FMODException"/>을 잡고 <see cref="FMODException.result"/>가 <c>RESULT.ERR_INVALID_HANDLE</c>인지 확인해야 합니다.<br/>
    /// FMOD가 채널 종료를 보고하면 이 래퍼는 관리 핸들을 분리하고 등록을 해제한 뒤 <see cref="onStop"/>을 한 번 호출합니다.
    /// </remarks>
    public sealed partial class SoundChannel : ISoundSystemResource
    {
        static readonly ConcurrentDictionary<IntPtr, SoundChannel> channelLists = [];
        static readonly CHANNELCONTROL_CALLBACK nativeCallback = OnNativeCallback;

        internal SoundChannel(SoundSystem system, Channel channel, WaveAudioClip clip)
        {
            this.system = system;
            this.clip = clip;

            native = channel;

            reverbWetLevel = new ReverbWetLevel(this);

            channel.setCallback(nativeCallback).ThrowIfNotOk();

            channelLists[native.handle] = this;
            system.Register(this);
        }

        /// <summary>
        /// Gets the sound system that created this channel.<br/>
        /// 이 채널을 생성한 사운드 시스템을 가져옵니다.
        /// </summary>
        public SoundSystem system { get; }

        /// <summary>
        /// Gets the clip played by this channel.<br/>
        /// 이 채널이 재생하는 클립을 가져옵니다.
        /// </summary>
        public WaveAudioClip? clip { get; }

        /// <summary>
        /// Gets the wrapped FMOD channel handle.<br/>
        /// 래핑된 FMOD 채널 핸들을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// This property becomes the default value when this wrapper detaches. A copied handle may return an FMOD error after the native channel is no longer available.<br/><br/>
        /// 이 래퍼가 분리되면 이 속성은 기본값이 됩니다. 복사한 핸들은 네이티브 채널을 더 이상 사용할 수 없을 때 FMOD 오류를 반환할 수 있습니다.
        /// </remarks>
        public Channel native { get; private set; }
        readonly ReaderWriterLockSlim modeLock = new();

        /// <summary>
        /// Occurs once when this wrapper detaches from its FMOD channel.<br/>
        /// 이 래퍼가 FMOD 채널에서 분리될 때 한 번 발생합니다.
        /// </summary>
        /// <remarks>
        /// The event is invoked after <see cref="native"/> is cleared. Handlers must not rely on the native channel remaining valid.<br/><br/>
        /// 이 이벤트는 <see cref="native"/>가 비워진 뒤 호출됩니다. 처리기는 네이티브 채널이 계속 유효하다고 가정하면 안 됩니다.
        /// </remarks>
        public event Action<SoundChannel>? onStop
        {
            add
            {
                lock (onStopLock)
                    _onStop += value;
            }
            remove
            {
                lock (onStopLock)
                    _onStop -= value;
            }
        }
        Action<SoundChannel>? _onStop;
        readonly object onStopLock = new();
        int detached;

        public static SoundChannel? GetManaged(IntPtr handle) => channelLists.GetValueOrDefault(handle);

        void ISoundSystemResource.ReleaseUnmanagedResources()
        {
            if (!TryDetach(out Channel channel))
                return;

            RESULT result = channel.stop();
            if (result != RESULT.OK && result != RESULT.ERR_INVALID_HANDLE)
                result.ThrowIfNotOk();

            InvokeStopHandlers();
        }

        [AOT.MonoPInvokeCallback(typeof(CHANNELCONTROL_CALLBACK))]
        static RESULT OnNativeCallback
        (
            IntPtr channelControl,
            CHANNELCONTROL_TYPE controlType,
            CHANNELCONTROL_CALLBACK_TYPE callbackType,
            IntPtr commandData1,
            IntPtr commandData2
        )
        {
            if (controlType != CHANNELCONTROL_TYPE.CHANNEL || callbackType != CHANNELCONTROL_CALLBACK_TYPE.END)
                return RESULT.OK;

            SoundChannel? channel = GetManaged(channelControl);
            if (channel == null)
                return RESULT.OK;

            if (!channel.TryDetach(out _))
                return RESULT.OK;

            channel.system.Dispose(channel);
            channel.InvokeStopHandlers();

            return RESULT.OK;
        }

        bool TryDetach(out Channel channel)
        {
            if (Interlocked.CompareExchange(ref detached, 1, 0) != 0)
            {
                channel = default;
                return false;
            }

            channel = native;
            if (channel.hasHandle())
                channelLists.TryRemove(channel.handle, out _);

            native = default;
            return true;
        }

        void InvokeStopHandlers()
        {
            Action<SoundChannel>? handlers;
            lock (onStopLock)
                handlers = _onStop;

            handlers.SafeInvoke(this);
        }
    }
}
