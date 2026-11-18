#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using RuniOS.Sounds.Processing;
using RuniOS.Tasks;
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Plays a <see cref="WaveAudioClip"/> through an FMOD channel and follows the channel position.<br/>
    /// FMOD 채널로 <see cref="WaveAudioClip"/>을 재생하고 채널 위치를 따라갑니다.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Audio/Wave Audio Source")]
    public sealed class WaveAudioSource : RuniAudioSource, IReloadable
    {
        /// <summary>
        /// Gets or sets the reference to the clip that this source plays.<br/>
        /// 이 소스가 재생할 클립의 참조를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// Must be used only from the main thread.<br/>
        /// 메인 스레드에서만 사용해야 합니다.
        /// </remarks>
        public AssetRef<WaveAudioClip> clipRef
        {
            get => _clipRef;
            set => _clipRef = value;
        }
        [SerializeField] AssetRef<WaveAudioClip> _clipRef;
        IAssetScope<WaveAudioClip>? scope;

        /// <inheritdoc/>
        /// <remarks>
        /// It follows the channel position behind the software buffer of the sound system, corrected by the pitch DSP latency like <see cref="time"/>.<br/>
        /// 사운드 시스템의 소프트웨어 버퍼만큼 뒤의 채널 위치를 따라가며, <see cref="time"/>처럼 피치 DSP 지연만큼 보정됩니다.
        /// </remarks>
        public override double outputTime => base.outputTime - pitchDSPTimeOffset;

        /// <inheritdoc/>
        /// <remarks>
        /// The getter delegates to <see cref="RuniAudioSource.time"/> and acquires the read lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// The setter acquires the write lock while synchronizing the interpolated time and seeking the current channel.
        /// <br/><br/>
        /// getter는 <see cref="RuniAudioSource.time"/>에 위임하여 <see cref="RuniAudioSource.playingLock"/>의 읽기 잠금을 획득합니다.<br/>
        /// setter는 보간 시간 동기화 및 현재 채널 탐색 중 쓰기 잠금을 획득합니다.
        /// </remarks>
        public override double time
        {
            get => base.time - pitchDSPTimeOffset;
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    double channelTime = value + (isPlaying ? pitchDSPTimeOffset : 0);
                    SyncInterpolatedTime(channelTime);
                    TryGetAliveChannel(channel => channel.time = channelTime);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback position of the current channel in PCM samples, or zero when there is no channel.<br/>
        /// 현재 채널의 재생 위치를 PCM 샘플 단위로 가져오거나 설정하며, 채널이 없으면 0입니다.
        /// </summary>
        /// <remarks>
        /// The getter does not acquire <see cref="RuniAudioSource.playingLock"/> and reads the current channel through its own channel lock.<br/>
        /// The setter acquires the write lock while synchronizing the interpolated time and seeking the current channel.
        /// <br/><br/>
        /// getter는 <see cref="RuniAudioSource.playingLock"/>을 획득하지 않고 별도의 채널 잠금을 통해 현재 채널을 읽습니다.<br/>
        /// setter는 보간 시간 동기화 및 현재 채널 탐색 중 쓰기 잠금을 획득합니다.
        /// </remarks>
        public uint timeSample
        {
            get => GetAliveChannelValue(channel => channel.timeSample, 0u);
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    if (frequency > 0)
                        SyncInterpolatedTime(value / (double)frequency);

                    if (clipSamples <= 0)
                        return;

                    TryGetAliveChannel(channel => channel.timeSample = value.Clamp(0, clipSamples - 1));
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

        /// <inheritdoc/>
        public override double length => Volatile.Read(ref clipLength);
        double clipLength = 0;

        /// <summary>
        /// Gets the length of the current clip in PCM samples.<br/>
        /// 현재 클립의 길이를 PCM 샘플 단위로 가져옵니다.
        /// </summary>
        public uint samples => clipSamples;
        volatile uint clipSamples = 0;

        /// <summary>
        /// Gets the default sample rate of the current clip in hertz.<br/>
        /// 현재 클립의 기본 샘플 레이트를 헤르츠 단위로 가져옵니다.
        /// </summary>
        public float frequency => clipFrequency;
        volatile float clipFrequency = 0;

        /// <inheritdoc/>
        public override float volume
        {
            get => base.volume;
            set
            {
                base.volume = value;
                TryGetAliveChannel(UnsafeUpdateChannelVolume);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The getter does not acquire <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// The setter delegates to <see cref="RuniAudioSource.tempo"/>, which acquires the write lock, and updates the current channel after that lock is released.
        /// <br/><br/>
        /// getter는 <see cref="RuniAudioSource.playingLock"/>을 획득하지 않습니다.<br/>
        /// setter는 쓰기 잠금을 획득하는 <see cref="RuniAudioSource.tempo"/>에 위임한 후, 해당 잠금이 해제되면 현재 채널을 갱신합니다.
        /// </remarks>
        public override float tempo
        {
            get => base.tempo;
            set
            {
                base.tempo = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelTempoAndPitch);
            }
        }

        /// <inheritdoc/>
        public override float pitch
        {
            get => base.pitch;
            set
            {
                base.pitch = value;
                TryGetAliveChannel(UnsafeUpdateChannelPitch);
            }
        }

        /// <inheritdoc/>
        public override bool loop
        {
            get => base.loop;
            set
            {
                base.loop = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        /// <inheritdoc/>
        public override double loopStart
        {
            get => base.loopStart;
            set
            {
                base.loopStart = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        /// <inheritdoc/>
        public override double loopEnd
        {
            get => base.loopEnd;
            set
            {
                base.loopEnd = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        /// <inheritdoc/>
        public override float panStereo
        {
            get => base.panStereo;
            set
            {
                base.panStereo = value;
                TryGetAliveChannel(UnsafeUpdateChannelPanStereo);
            }
        }

        /// <inheritdoc/>
        public override float spatialBlend
        {
            get => base.spatialBlend;
            set
            {
                base.spatialBlend = value;
                TryGetAliveChannel(UnsafeUpdateChannelSpatialBlend);
            }
        }

        /// <inheritdoc/>
        public override float dopplerLevel
        {
            get => base.dopplerLevel;
            set
            {
                base.dopplerLevel = value;
                TryGetAliveChannel(UnsafeUpdateChannelDopplerLevel);
            }
        }

        /// <inheritdoc/>
        public override float spread
        {
            get => base.spread;
            set
            {
                base.spread = value;
                TryGetAliveChannel(UnsafeUpdateChannelSpread);
            }
        }

        /// <inheritdoc/>
        public override float minDistance
        {
            get => base.minDistance;
            set
            {
                base.minDistance = value;
                TryGetAliveChannel(UnsafeUpdateChannelMinMaxDistance);
            }
        }

        /// <inheritdoc/>
        public override float maxDistance
        {
            get => base.maxDistance;
            set
            {
                base.maxDistance = value;
                TryGetAliveChannel(UnsafeUpdateChannelMinMaxDistance);
            }
        }

        /// <summary>
        /// Gets or sets the 3D distance attenuation curve used by this source.<br/>
        /// 이 소스에 사용할 3D 거리 감쇠 곡선을 가져오거나 설정합니다.
        /// </summary>
        public SoundRolloffMode rolloffMode
        {
            get => _rolloffMode;
            set
            {
                _rolloffMode = value;
                TryGetAliveChannel(UnsafeUpdateChannelRolloffMode);
            }
        }
        [SerializeField] volatile SoundRolloffMode _rolloffMode = SoundRolloffMode.inverse;

        /// <summary>
        /// Gets or sets whether the Doppler velocity is computed from the movement of the transform when there is no rigidbody.<br/>
        /// 리지드바디가 없을 때 트랜스폼의 이동으로 도플러 속도를 계산할지 여부를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// Must be used only from the main thread.<br/>
        /// 메인 스레드에서만 사용해야 합니다.
        /// </remarks>
        public bool nonRigidbodyVelocity
        {
            get => _nonRigidbodyVelocity;
            set => _nonRigidbodyVelocity = value;
        }
        [SerializeField] bool _nonRigidbodyVelocity = false;

        SoundChannel? channel;

        // channel, channelBaseFrequency, pitchDSPList의 수명과 플레이어 소유 FMOD 채널 변경을 보호합니다.
        readonly ReaderWriterLockSlim channelLock = new();

        // pitch DSP는 현재 channel에 부착되므로 반드시 channelLock 안에서 생성, 변경, 해제합니다.
        readonly List<PitchShiftDSP> pitchDSPList = [];
        const float pitchDSPFFTSize = 4096;
        volatile int pitchDSPCount;
        readonly Lock pitchDSPListLock = new();

        double pitchDSPTimeOffset
        {
            get
            {
                float frequency = clipFrequency;
                float tempo = this.tempo;
                if (frequency <= 0 || !float.IsNormal(tempo))
                    return 0;

                double offset = pitchDSPCount * (pitchDSPFFTSize / frequency);
                return tempo < 0 ? -offset : offset;
            }
        }

        volatile uint lastTimeSamples = uint.MaxValue;

#if UNITY_PHYSICS_EXIST
        Rigidbody? rigidbody;
#endif
#if UNITY_PHYSICS2D_EXIST
        Rigidbody2D? rigidbody2D;
#endif

        Vector3 lastPosition;

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="RuniAudioSource.OnEnable"/> temporarily acquires the write lock while setting the active state.<br/>
        /// The remaining initialization runs after that lock is released.
        /// <br/><br/>
        /// <see cref="RuniAudioSource.OnEnable"/>은 활성 상태 설정 중 쓰기 잠금을 일시적으로 획득합니다.<br/>
        /// 나머지 초기화는 해당 잠금이 해제된 후 실행됩니다.
        /// </remarks>
        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_PHYSICS_EXIST
            rigidbody = GetComponent<Rigidbody>();
#endif
#if UNITY_PHYSICS2D_EXIST
            rigidbody2D = GetComponent<Rigidbody2D>();
#endif

            lastPosition = transform.position;

            Reload().Forget();
            ResourceManager.AttachReloadable(this);

            SoundSystem.main.onUpdate += TimeUpdate;
        }

        void TimeUpdate()
        {
            uint timeSample = this.timeSample;
            if (lastTimeSamples != timeSample)
            {
                lastTimeSamples = timeSample;

                playingLock.EnterUpgradeableReadLock();

                try
                {
                    UnsafeSyncChannel(mixerAdvanced: true);
                }
                finally
                {
                    playingLock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        /// Reads the channel position between two reads of the mix stamp, so that the position pairs with the time at which the mixer processed it.<br/>
        /// A stamp written in between fails this read, and the next position change catches up.
        /// <br/><br/>
        /// 믹스 스탬프를 두 번 읽는 사이에 채널 위치를 읽어, 그 위치를 믹서가 처리한 시각과 짝짓습니다.<br/>
        /// 그 사이에 스탬프가 기록되면 이번 읽기는 실패하고, 다음 위치 변경에서 따라잡습니다.
        /// </summary>
        /// <param name="channel">
        /// The channel to read.<br/>
        /// 읽을 채널입니다.
        /// </param>
        /// <param name="time">
        /// The channel position in seconds when successful; otherwise, the position read or zero.<br/>
        /// 성공하면 초 단위 채널 위치이고, 그렇지 않으면 읽은 위치 또는 0입니다.
        /// </param>
        /// <param name="stamp">
        /// The stamp paired with <paramref name="time"/> when successful; otherwise, the first stamp read or <see langword="default"/>.<br/>
        /// 성공하면 <paramref name="time"/>과 짝지은 스탬프이고, 그렇지 않으면 처음 읽은 스탬프 또는 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if both reads returned the same stamp; otherwise, <see langword="false"/>.<br/>
        /// 두 번의 읽기가 같은 스탬프를 반환했으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        static bool TryGetChannelTimeAtMixStamp(SoundChannel channel, out double time, out SoundMixStamp stamp)
        {
            SoundSystem system = channel.system;
            time = 0;

            if (!system.TryGetMixStamp(out stamp))
                return false;

            time = channel.time;
            return system.TryGetMixStamp(out SoundMixStamp after) && stamp == after;
        }

        /// <summary>
        /// Synchronizes the output time from a channel position at the end of the block of <paramref name="stamp"/>.<br/>
        /// The output is behind that position by the software buffer, which is the distance from the stamp clock to its output clock.
        /// <br/><br/>
        /// <paramref name="stamp"/> 블록의 끝에서의 채널 위치로 출력 시간을 동기화합니다.<br/>
        /// 출력은 그 위치보다 소프트웨어 버퍼만큼 뒤에 있으며, 이는 스탬프 클록에서 출력 클록까지의 거리입니다.
        /// </summary>
        /// <param name="channelTime">
        /// The channel position in seconds at the end of the block of <paramref name="stamp"/>.<br/>
        /// <paramref name="stamp"/> 블록의 끝에서의 초 단위 채널 위치입니다.
        /// </param>
        /// <param name="stamp">
        /// The mix stamp that gives the software buffer and the timestamp.<br/>
        /// 소프트웨어 버퍼와 타임스탬프를 제공하는 믹스 스탬프입니다.
        /// </param>
        /// <param name="sampleRate">
        /// The output sample rate of the sound system.<br/>
        /// 사운드 시스템의 출력 샘플 레이트입니다.
        /// </param>
        void SyncOutputTimeToMixStamp(double channelTime, SoundMixStamp stamp, int sampleRate)
        {
            double outputDelay = (stamp.dspClock - stamp.outputClock) / (double)sampleRate;
            SyncInterpolatedOutputTime(channelTime - (outputDelay * tempo), stamp.timestamp);
        }

        /// <summary>
        /// Synchronizes the output time from a channel position set now, using the latest stamp.<br/>
        /// Without a stamp, the output time is left as it is.
        /// <br/><br/>
        /// 최신 스탬프를 사용해 지금 설정한 채널 위치로 출력 시간을 동기화합니다.<br/>
        /// 스탬프가 없으면 출력 시간을 그대로 둡니다.
        /// </summary>
        /// <param name="channel">
        /// The channel whose sound system gives the stamp.<br/>
        /// 스탬프를 제공하는 사운드 시스템의 채널입니다.
        /// </param>
        /// <param name="channelTime">
        /// The channel position in seconds that was set now.<br/>
        /// 지금 설정한 초 단위 채널 위치입니다.
        /// </param>
        void SyncOutputTimeToLatestMixStamp(SoundChannel channel, double channelTime)
        {
            if (channel.system.TryGetMixStamp(out SoundMixStamp stamp))
                SyncOutputTimeToMixStamp(channelTime, stamp, channel.system.outputSampleRate);
        }

        /// <remarks>
        /// Acquires the upgradeable read lock of <see cref="RuniAudioSource.playingLock"/> only while synchronizing the current channel.<br/>
        /// Channel synchronization may upgrade it to the write lock when the interpolated time must be corrected.
        /// <br/><br/>
        /// 현재 채널을 동기화하는 동안에만 <see cref="RuniAudioSource.playingLock"/>의 업그레이드 가능 읽기 잠금을 획득합니다.<br/>
        /// 보간 시간을 보정해야 하는 경우 채널 동기화 과정에서 쓰기 잠금으로 승격할 수 있습니다.
        /// </remarks>
        void Update()
        {
            if (spatialBlend > 0)
            {
                TryGetAliveChannel(channel =>
                {
#if UNITY_PHYSICS_EXIST
                    if (rigidbody != null)
                        channel.spatialState = new AudioSpatialState(transform.position, rigidbody.linearVelocity);
                    else
#endif
#if UNITY_PHYSICS2D_EXIST
                    if (rigidbody2D != null)
                        channel.spatialState = new AudioSpatialState(transform.position, rigidbody2D.linearVelocity);
                    else
#endif
                    if (nonRigidbodyVelocity)
                    {
                        Vector3 velocity = Vector3.zero;
                        if (Kernel.deltaTime != 0)
                        {
                            velocity = (transform.position - lastPosition) / Kernel.deltaTime;
                            velocity = velocity.ClampMagnitude(20);
                        }

                        channel.spatialState = new AudioSpatialState(transform.position, velocity);
                    }
                    else
                        channel.spatialState = new AudioSpatialState(transform.position);
                });
            }

            lastPosition = transform.position;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Acquires the write lock of <see cref="RuniAudioSource.playingLock"/> while disabling playback, detaching reload behavior, and clearing source state.<br/>
        /// The previous asset scope is queued for disposal after the lock is released.
        /// <br/><br/>
        /// 재생 비활성화, 리로드 동작 분리 및 소스 상태 초기화 중 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 획득합니다.<br/>
        /// 이전 에셋 스코프는 잠금이 해제된 후 폐기 큐에 등록합니다.
        /// </remarks>
        protected override void OnDisable()
        {
            IAssetScope<WaveAudioClip>? oldScope;
            playingLock.EnterWriteLock();

            try
            {
                base.OnDisable();

                ResourceManager.DetachReloadable(this);

                oldScope = scope;
                scope = null;

                Volatile.Write(ref clipLength, 0);
                clipSamples = 0;
                clipFrequency = 0;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            DisposeQueue.Enqueue(oldScope);
            SoundSystem.main.onUpdate -= TimeUpdate;
        }

        readonly AsyncReloadGate reloadGate = new();

        /// <summary>
        /// Loads the clip of <see cref="clipRef"/> when it refers to a different target, and synchronizes the channel with it.<br/>
        /// <see cref="clipRef"/>가 다른 대상을 가리키면 그 클립을 로드하고 채널을 동기화합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that represents completion of the reload.<br/>
        /// 리로드 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        /// <remarks>
        /// Must be called only from the main thread.<br/>
        /// The asynchronous load runs without holding <see cref="RuniAudioSource.playingLock"/>; the write lock is acquired only when replacing the scope and synchronizing the channel.
        /// <br/><br/>
        /// 메인 스레드에서만 호출해야 합니다.<br/>
        /// 비동기 로드는 <see cref="RuniAudioSource.playingLock"/>을 보유하지 않고 실행하며, 스코프 교체 및 채널 동기화 시에만 쓰기 잠금을 획득합니다.
        /// </remarks>
        public UniTask Reload() => reloadGate.Run(ReloadCore);

        /// <remarks>
        /// Performs asynchronous loading without holding <see cref="RuniAudioSource.playingLock"/>, then acquires the write lock while replacing the scope and synchronizing the channel.<br/>
        /// <see cref="RuniAudioSource.isActiveAndEnabled"/> checks are thread-safe and acquire the read lock independently.
        /// <br/><br/>
        /// <see cref="RuniAudioSource.playingLock"/>을 보유하지 않고 비동기 로드를 수행한 후, 스코프 교체 및 채널 동기화 중 쓰기 잠금을 획득합니다.<br/>
        /// <see cref="RuniAudioSource.isActiveAndEnabled"/> 확인은 thread-safe하며 독립적으로 읽기 잠금을 획득합니다.
        /// </remarks>
        async UniTask ReloadCore()
        {
            if (this == null || !isActiveAndEnabled)
                return;

            // Reload 메소드에서만 scope를 교채하기 때문에 괜찮습니다.
            if (clipRef.IsSameTarget(scope))
                return;

            IAssetScope<WaveAudioClip>? newScope = await clipRef.LoadScopeAsync();
            if (this == null || !isActiveAndEnabled)
            {
                DisposeQueue.Enqueue(newScope);
                return;
            }

            // 비동기 로드는 lock 밖에서 끝내고, 실제 scope 교체와 채널 재동기화만 하나의 playingLock 구간에서 처리합니다.
            playingLock.EnterWriteLock();

            try
            {
                StopChannel();

                DisposeQueue.Enqueue(scope);
                scope = newScope;

                Volatile.Write(ref clipLength, scope?.asset.length ?? 0);
                clipSamples = scope?.asset.samples ?? 0;
                clipFrequency = scope?.asset.frequency ?? 0;

                UnsafeSyncChannel();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        void TryGetAliveChannel(Action<SoundChannel> action)
        {
            SoundChannel? lostChannel = null;
            channelLock.EnterReadLock();

            try
            {
                if (channel == null)
                    return;

                action.Invoke(channel);
            }
            catch (ObjectDisposedException)
            {
                lostChannel = channel;
            }
            finally
            {
                channelLock.ExitReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);
        }

        T GetAliveChannelValue<T>(Func<SoundChannel, T> func, T defaultValue)
        {
            SoundChannel? lostChannel;
            channelLock.EnterReadLock();

            try
            {
                if (channel == null)
                    return defaultValue;

                return func.Invoke(channel);
            }
            catch (ObjectDisposedException)
            {
                lostChannel = channel;
            }
            finally
            {
                channelLock.ExitReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);

            return defaultValue;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnPlay() => UnsafeSyncChannel();

        /// <inheritdoc/>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnStop() => UnsafeSyncChannel();

        /// <inheritdoc/>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);

        /// <inheritdoc/>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnUnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);

        /// <remarks>
        /// The caller must hold the upgradeable read lock or write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// The method may acquire the write lock through <see cref="RuniAudioSource.SyncInterpolatedTime(double, long)"/> when correcting the interpolated time.
        /// <br/><br/>
        /// 호출자는 <see cref="RuniAudioSource.playingLock"/>의 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 보유해야 합니다.<br/>
        /// 보간 시간을 보정할 때 <see cref="RuniAudioSource.SyncInterpolatedTime(double, long)"/>을 통해 쓰기 잠금을 획득할 수 있습니다.
        /// </remarks>
        /// <param name="mixerAdvanced">
        /// Whether the mixer moved the channel position, which then pairs with the mix stamp instead of the current time.<br/>
        /// A position set through the API is synchronized at the current time.
        /// <br/><br/>
        /// 믹서가 채널 위치를 옮겼는지 여부이며, 그렇다면 위치를 현재 시각 대신 믹스 스탬프와 짝짓습니다.<br/>
        /// API로 설정한 위치는 현재 시각으로 동기화합니다.
        /// </param>
        void UnsafeSyncChannel(bool mixerAdvanced = false)
        {
            Debug.Assert
            (
                playingLock.IsUpgradeableReadLockHeld || playingLock.IsWriteLockHeld,
                "호출 전에 playingLock의 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 먼저 보유해야합니다."
            );

            SoundChannel? lostChannel = null;
            channelLock.EnterUpgradeableReadLock();

            try
            {
                double currentTime = time + pitchDSPTimeOffset;
                if
                (
                    scope == null || !isPlaying || !double.IsFinite(currentTime) ||
                    (!loop && (currentTime < 0 || currentTime > scope.asset.length))
                )
                {
                    StopChannel();
                    return;
                }

                if (channel != null)
                {
                    if (!mixerAdvanced)
                    {
                        double setTime = channel.time;
                        SyncInterpolatedTime(setTime);
                        SyncOutputTimeToLatestMixStamp(channel, setTime);
                    }
                    else if (TryGetChannelTimeAtMixStamp(channel, out double channelTime, out SoundMixStamp stamp))
                    {
                        SyncInterpolatedTime(channelTime, stamp.timestamp);
                        SyncOutputTimeToMixStamp(channelTime, stamp, channel.system.outputSampleRate);
                    }
                }
                else
                {
                    if (scope.asset.system.Execute(system => system.PlaySound(scope.asset, true), out SoundChannel? newChannel) && newChannel != null)
                    {
                        channelLock.EnterWriteLock();

                        try
                        {
                            channel = newChannel;
                            channel.onStop += HandleChannelLost;

                            channel.time = currentTime;

                            UnsafeUpdateChannelProperty(channel);
                        }
                        finally
                        {
                            channelLock.ExitWriteLock();
                        }

                        SyncOutputTimeToLatestMixStamp(newChannel, currentTime);
                    }
                }
            }
            catch (ObjectDisposedException) when (channel != null) // channel == null에서 ERR_INVALID_HANDLE 에러는 정상 경로가 아님
            {
                lostChannel = channel;
            }
            finally
            {
                channelLock.ExitUpgradeableReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);
        }

        // 그냥 Stop 하면 onStop 이벤트가 즉시 호출되기 때문에 미리 해제해주어야합니다.
        void StopChannel()
        {
            Debug.Assert
            (
                !channelLock.IsReadLockHeld && !channelLock.IsWriteLockHeld,
                "channelLock의 읽기 잠금 또는 쓰기 잠금을 보유한 상태로 StopChannel 메소드를 호출하면 안됩니다."
            );

            channelLock.EnterWriteLock();

            try
            {
                if (channel == null)
                    return;

                UnsafeReleasePitchDSPList(channel);

                channel.onStop -= HandleChannelLost;
                channel.Stop();
                channel = null;

                lastTimeSamples = uint.MaxValue;
            }
            finally
            {
                channelLock.ExitWriteLock();
            }
        }

        void HandleChannelLost(SoundChannel lostChannel)
        {
            channelLock.EnterWriteLock();

            try
            {
                // 채널이 새롭게 교채된 후에도 기존 채널이 onStop을 호출할 수 있기 때문에 확인해야합니다.
                if (channel != lostChannel)
                    return;

                UnsafeReleasePitchDSPList(channel);

                channel.onStop -= HandleChannelLost;
                channel = null;

                lastTimeSamples = uint.MaxValue;
            }
            finally
            {
                channelLock.ExitWriteLock();
            }
        }

        // 아래 UpdateChannel* 메서드는 유효한 channel과 channelLock이 확보된 상태에서만 호출합니다.
        // 각 메서드는 자신의 세부 상태만 스냅샷으로 읽고 FMOD 호출을 즉시 끝내야 합니다.
        void UnsafeUpdateChannelProperty(SoundChannel channel)
        {
            UnsafeUnsafeUpdateChannelLoop(channel);
            UnsafeUnsafeUpdateChannelTempoAndPitch(channel);
            UnsafeUpdateChannelVolume(channel);
            UnsafeUpdateChannelPanStereo(channel);
            UnsafeUpdateChannelDopplerLevel(channel);
            UnsafeUpdateChannelSpatialBlend(channel);
            UnsafeUpdateChannelSpread(channel);
            UnsafeUpdateChannelMinMaxDistance(channel);
            UnsafeUpdateChannelRolloffMode(channel);
            UnsafeUpdateChannelPause(channel);
        }

        void UnsafeUnsafeUpdateChannelLoop(SoundChannel channel)
        {
            channel.loop = loop;
            channel.loopRange = (loopStart, loopEnd);
        }

        void UnsafeUnsafeUpdateChannelTempoAndPitch(SoundChannel channel)
        {
            channel.frequency = (channel.clip?.frequency ?? channel.frequency) * tempo;
            UnsafeUpdateChannelPitch(channel);
        }

        void UnsafeUpdateChannelPitch(SoundChannel channel)
        {
            float pitch = this.pitch;
            float tempo = this.tempo;

            if (!float.IsNormal(pitch) || !float.IsNormal(tempo))
            {
                UnsafeReleasePitchDSPList(channel);
                return;
            }

            float value = pitch / tempo.Abs();
            if (float.IsNaN(value))
                value = 1;

            value = value.Clamp(0.01f, 100);
            if (value.Approximately(1))
            {
                UnsafeReleasePitchDSPList(channel);
                return;
            }

            lock (pitchDSPListLock)
            {
                int index = 0;
                while (value < 0.5f)
                {
                    SetPitchDsp(index, 0.5f, channel);

                    index++;
                    value *= 2;
                }

                while (value > 2)
                {
                    SetPitchDsp(index, 2, channel);

                    index++;
                    value *= 0.5f;
                }

                SetPitchDsp(index, value, channel);
                index++;

                for (int i = pitchDSPList.Count - 1; i >= index; i--)
                {
                    PitchShiftDSP dsp = pitchDSPList[i];
                    channel.dsps.Remove(dsp);
                    dsp.Dispose();

                    pitchDSPList.RemoveAt(i);
                }

                void SetPitchDsp(int index, float value, SoundChannel channel)
                {
                    while (pitchDSPList.Count <= index)
                    {
                        PitchShiftDSP pitchDsp = channel.system.CreateDSP<PitchShiftDSP>();
                        pitchDsp.fftSize = pitchDSPFFTSize;
                        pitchDSPList.Add(pitchDsp);
                        channel.dsps.Add(pitchDsp);
                    }

                    pitchDSPList[index].pitch = value;
                }

                pitchDSPCount = pitchDSPList.Count;
            }
        }

        /// <remarks>
        /// channel에 부착된 DSP 목록과 함께 변경되므로 호출자는 channelLock을 보유해야 합니다.
        /// </remarks>
        void UnsafeReleasePitchDSPList(SoundChannel? channel)
        {
            lock (pitchDSPListLock)
            {
                for (int i = 0; i < pitchDSPList.Count; i++)
                {
                    PitchShiftDSP dsp = pitchDSPList[i];

                    try
                    {
                        channel?.dsps.Remove(dsp);
                    }
                    catch (ObjectDisposedException)
                    {
                        channel = null;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    try
                    {
                        dsp.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                pitchDSPList.Clear();
                pitchDSPCount = 0;
            }
        }

        void UnsafeUpdateChannelVolume(SoundChannel channel) => channel.volume = volume;

        void UnsafeUpdateChannelPanStereo(SoundChannel channel) => channel.panStereo = panStereo;

        void UnsafeUpdateChannelSpatialBlend(SoundChannel channel) => channel.spatialBlend = spatialBlend;

        void UnsafeUpdateChannelDopplerLevel(SoundChannel channel) => channel.dopplerLevel = dopplerLevel;

        void UnsafeUpdateChannelSpread(SoundChannel channel) => channel.spread = spread;

        void UnsafeUpdateChannelMinMaxDistance(SoundChannel channel) => channel.minMaxDistance = (minDistance, maxDistance);

        void UnsafeUpdateChannelRolloffMode(SoundChannel channel) => channel.rolloffMode = rolloffMode;

        void UnsafeUpdateChannelPause(SoundChannel channel) => channel.isPaused = isPaused;
    }
}
