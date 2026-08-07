#nullable enable
using FMOD;
using System.Collections.Concurrent;
using System.Threading;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns an FMOD DSP created by <see cref="SoundSystem"/>.<br/>
    /// <see cref="SoundSystem"/>에서 생성한 FMOD DSP를 안전하게 소유합니다.
    /// </summary>
    /// <remarks>
    /// Detach a DSP from its channel, channel group, or DSP input before disposing it.<br/>
    /// FMOD reports <see cref="RESULT.ERR_DSP_INUSE"/> when the DSP remains connected.<br/><br/>
    /// DSP를 해제하기 전에 채널, 채널 그룹 또는 DSP 입력에서 분리해야 합니다.<br/>
    /// DSP가 계속 연결되어 있으면 FMOD는 <see cref="RESULT.ERR_DSP_INUSE"/>를 보고합니다.
    /// </remarks>
    public abstract partial class DSP : IDisposable, ISoundSystemResource
    {
        static readonly ConcurrentDictionary<IntPtr, DSP> dspLists = [];

        protected DSP()
        {
            boolParameters = new BoolParameters(this);
            dataParameters = new DataParameters(this);
            floatParameters = new FloatParameters(this);
            intParameters = new IntParameters(this);
        }

        /// <remarks>
        /// 외부로 반환 전에 인스턴스 생성 즉시 이 메소드가 호출되어야합니다.<br/>
        /// </remarks>
        /// <remarks>
        /// 이 메소드는 생성자라고 생각해야합니다.<br/>
        /// 인스턴스 생성 후 이 메소드가 호출되기전에 다른 작업을 절대로 해선 안됩니다.
        /// </remarks>
        internal void Initialize(SoundSystem system, FMOD.DSP native)
        {
            if (_system != null || _isDisposed)
                throw new InvalidOperationException("The FMOD DSP has already been created.");

            _system = system;

            this.native = native;
            nativeHandle = native.handle;

            system.Register(this);
            dspLists.TryAdd(nativeHandle, this);
        }

        /// <summary>
        /// Gets sound system that created this DSP.<br/>
        /// 이 DSP를 생성한 사운드 시스템을 가져옵니다.
        /// </summary>
        public SoundSystem system => _system ?? throw new InvalidOperationException("The FMOD DSP has not been created by a sound system.");
        SoundSystem? _system;

        internal abstract DSP_TYPE type { get; }

        FMOD.DSP native;
        IntPtr nativeHandle;
        readonly ReaderWriterLockSlim nativeLock = new(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Gets whether this DSP has been disposed.<br/>
        /// 이 DSP가 해제되었는지 여부를 가져옵니다.
        /// </summary>
        public bool isDisposed => Volatile.Read(ref _isDisposed);
        bool _isDisposed;

        /// <summary>
        /// Gets or sets whether this DSP is active.<br/>
        /// 이 DSP가 활성 상태인지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool active
        {
            get => UseNative(dsp =>
            {
                dsp.getActive(out bool active).ThrowIfNotOk();
                return active;
            });
            set => UseNative(dsp => dsp.setActive(value).ThrowIfNotOk());
        }

        /// <summary>
        /// Gets or sets whether this DSP is bypassed.<br/>
        /// 이 DSP를 우회할지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool bypass
        {
            get => UseNative(dsp =>
            {
                dsp.getBypass(out bool bypass).ThrowIfNotOk();
                return bypass;
            });
            set => UseNative(dsp => dsp.setBypass(value).ThrowIfNotOk());
        }

        /// <summary>
        /// Gets or sets pre-wet, post-wet, and dry mix levels.<br/>
        /// 프리 웻, 포스트 웻, 드라이 믹스 레벨을 가져오거나 설정합니다.
        /// </summary>
        public (float preWet, float postWet, float dry) signalMix
        {
            get => UseNative(dsp =>
            {
                dsp.getWetDryMix(out float preWet, out float postWet, out float dry).ThrowIfNotOk();
                return (preWet, postWet, dry);
            });
            set => UseNative(dsp => dsp.setWetDryMix(value.preWet, value.postWet, value.dry).ThrowIfNotOk());
        }

        public static DSP? GetManaged(IntPtr handle) => dspLists.GetValueOrDefault(handle);

        /// <summary>
        /// Requests release of this DSP from <see cref="system"/>.<br/>
        /// <see cref="system"/>에 이 DSP의 해제를 요청합니다.
        /// </summary>
        /// <remarks>
        /// Repeated calls are ignored after native release succeeds.<br/>
        /// When this DSP remains connected to the graph, FMOD reports <see cref="RESULT.ERR_DSP_INUSE"/> and this DSP remains owned for a later call after detachment.<br/><br/>
        /// 네이티브 해제가 성공한 뒤의 반복 호출은 무시됩니다.<br/>
        /// 이 DSP가 그래프에 계속 연결되어 있으면 FMOD는 <see cref="RESULT.ERR_DSP_INUSE"/>를 보고하며, 분리 뒤 다시 호출할 수 있도록 이 DSP의 소유권을 유지합니다.
        /// </remarks>
        public void Dispose() => system.Dispose(this);

        /// <summary>
        /// Adds <paramref name="input"/> as one input of this DSP.<br/>
        /// <paramref name="input"/>을 이 DSP의 입력 하나로 추가합니다.
        /// </summary>
        /// <param name="input">
        /// DSP created by same sound system.<br/>
        /// 같은 사운드 시스템에서 생성한 DSP입니다.
        /// </param>
        /// <returns>
        /// Token that owns only the added connection.<br/>
        /// 추가한 연결만 소유하는 토큰을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="input"/> is <see langword="null"/>.<br/>
        /// <paramref name="input"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="input"/> belongs to another sound system.<br/>
        /// <paramref name="input"/>이 다른 사운드 시스템에 속한 경우 발생합니다.
        /// </exception>
        [Obsolete("CustomDSP has not been tested and is quite complex!")]
        public Custom.DSPConnection AddInput(Custom.CustomDSP input)
        {
            ExceptionUtility.ThrowIfArgumentNull(input, nameof(input));
            if (input.system != system)
                throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(input));

            return UseNativePair(this, input, (outputNative, inputNative) =>
            {
                outputNative.addInput(inputNative, out DSPConnection nativeConnection).ThrowIfNotOk();
                return new Custom.DSPConnection(input, this, nativeConnection);
            });
        }

        void ISoundSystemResource.ReleaseUnmanagedResources()
        {
            nativeLock.EnterWriteLock();

            try
            {
                if (_isDisposed)
                    return;

                dspLists.TryRemove(nativeHandle, out _);

                RESULT result = native.release();
                if (result != RESULT.OK && result != RESULT.ERR_INVALID_HANDLE)
                    result.ThrowIfNotOk();

                native.clearHandle();
                _isDisposed = true;
            }
            finally
            {
                nativeLock.ExitWriteLock();
            }

            OnNativeReleaseAccepted();
        }

        /// <summary>
        /// Runs after FMOD accepts native DSP release.<br/>
        /// FMOD가 네이티브 DSP 해제를 수락한 뒤 실행합니다.
        /// </summary>
        protected virtual void OnNativeReleaseAccepted() { }

        void ThrowIfDisposedUnsafe()
        {
            Debug.Assert(nativeLock.IsReadLockHeld || nativeLock.IsWriteLockHeld, "The DSP native lock must be held.");

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DSP), "The FMOD DSP has already been disposed.");
        }
    }
}
