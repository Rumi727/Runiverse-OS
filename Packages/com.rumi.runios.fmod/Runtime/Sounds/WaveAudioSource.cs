#nullable enable
using Cysharp.Threading.Tasks;
using FMOD;
using RuniOS.Resource;
using RuniOS.Sounds.Processing;
using RuniOS.Tasks;
using System.Threading;

namespace RuniOS.Sounds
{
    [ExecuteAlways]
    public sealed class WaveAudioSource : RuniAudioSource, IReloadable
    {
        /// <remarks>
        /// 메인 스레드에서만 사용해야합니다!
        /// </remarks>
        public AssetRef<WaveAudioClip> clipRef
        {
            get => _clipRef;
            set => _clipRef = value;
        }
        [SerializeField] AssetRef<WaveAudioClip> _clipRef;
        IAssetScope<WaveAudioClip>? scope;

        /// <remarks>
        /// The getter delegates to <see cref="RuniAudioSource.time"/> and acquires the read lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// The setter acquires the write lock while synchronizing the interpolated time and seeking the current channel.
        /// <br/><br/>
        /// getter는 <see cref="RuniAudioSource.time"/>에 위임하여 <see cref="RuniAudioSource.playingLock"/>의 읽기 잠금을 획득합니다.<br/>
        /// setter는 보간 시간 동기화 및 현재 채널 탐색 중 쓰기 잠금을 획득합니다.
        /// </remarks>
        public override double time
        {
            get => base.time;
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    SyncInterpolatedTime(value);
                    timeSampleDirty = !TrySeekAliveChannel(channel => channel.time = value);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

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

                    timeSampleDirty = !TrySeekAliveChannel(channel => channel.timeSample = value.Clamp(0, clipSamples - 1));
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

        public override double length => Volatile.Read(ref clipLength);
        double clipLength = 0;

        public uint samples => clipSamples;
        volatile uint clipSamples = 0;

        public float frequency => clipFrequency;
        volatile float clipFrequency = 0;

        public override float volume
        {
            get => base.volume;
            set
            {
                base.volume = value;
                TryGetAliveChannel(UnsafeUpdateChannelVolume);
            }
        }

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

        public override float pitch
        {
            get => base.pitch;
            set
            {
                base.pitch = value;
                TryGetAliveChannel(UnsafeUpdateChannelPitch);
            }
        }

        readonly object tempoAndPitchLock = new();

        public override bool loop
        {
            get => base.loop;
            set
            {
                base.loop = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        public override double loopStart
        {
            get => base.loopStart;
            set
            {
                base.loopStart = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        public override double loopEnd
        {
            get => base.loopEnd;
            set
            {
                base.loopEnd = value;
                TryGetAliveChannel(UnsafeUnsafeUpdateChannelLoop);
            }
        }

        public override float panStereo
        {
            get => base.panStereo;
            set
            {
                base.panStereo = value;
                TryGetAliveChannel(UnsafeUpdateChannelPanStereo);
            }
        }

        public override float spatialBlend
        {
            get => base.spatialBlend;
            set
            {
                base.spatialBlend = value;
                TryGetAliveChannel(UnsafeUpdateChannelSpatialBlend);
            }
        }

        public override float dopplerLevel
        {
            get => base.dopplerLevel;
            set
            {
                base.dopplerLevel = value;
                TryGetAliveChannel(UnsafeUpdateChannelDopplerLevel);
            }
        }

        public override float spread
        {
            get => base.spread;
            set
            {
                base.spread = value;
                TryGetAliveChannel(UnsafeUpdateChannelSpread);
            }
        }

        public override float minDistance
        {
            get => base.minDistance;
            set
            {
                base.minDistance = value;
                TryGetAliveChannel(UnsafeUpdateChannelMinMaxDistance);
            }
        }

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

        /// <remarks>
        /// 메인 스레드에서만 사용해야합니다!
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
        const float fftSize = 4096;

        volatile uint lastTimeSamples = uint.MaxValue;
        volatile bool timeSampleDirty;

#if UNITY_PHYSICS_EXIST
        Rigidbody? rigidbody;
#endif
#if UNITY_PHYSICS2D_EXIST
        Rigidbody2D? rigidbody2D;
#endif

        Vector3 lastPosition;

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
            uint timeSample = GetAliveChannelValue(channel => channel.timeSample, 0u);
            if (timeSampleDirty || lastTimeSamples != timeSample)
            {
                lastTimeSamples = timeSample;

                playingLock.EnterUpgradeableReadLock();

                try
                {
                    UnsafeSyncChannel();
                }
                finally
                {
                    playingLock.ExitUpgradeableReadLock();
                }
            }

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

                lastPosition = transform.position;
            }
        }

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
        }

        readonly AsyncReloadGate reloadGate = new();

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

        bool TryGetAliveChannel(Action<SoundChannel> action)
        {
            SoundChannel? lostChannel = null;
            bool success = false;
            channelLock.EnterReadLock();

            try
            {
                if (channel == null)
                    return false;

                action.Invoke(channel);
                success = true;
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE)
            {
                lostChannel = channel;
            }
            finally
            {
                channelLock.ExitReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);

            return success;
        }

        bool TrySeekAliveChannel(Action<SoundChannel> action)
        {
            try
            {
                return TryGetAliveChannel(action);
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY)
            {
                return false;
            }
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
            catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE)
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

        public void GetTempoAndPitch(out float tempo, out float pitch)
        {
            lock (tempoAndPitchLock)
            {
                tempo = this.tempo;
                pitch = this.pitch;
            }
        }

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnPlay() => UnsafeSyncChannel();

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnStop() => UnsafeSyncChannel();

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected override void OnUnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);

        /// <remarks>
        /// The caller must hold the upgradeable read lock or write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// The method may acquire the write lock through <see cref="RuniAudioSource.SyncInterpolatedTime"/> when correcting the interpolated time.
        /// <br/><br/>
        /// 호출자는 <see cref="RuniAudioSource.playingLock"/>의 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 보유해야 합니다.<br/>
        /// 보간 시간을 보정할 때 <see cref="RuniAudioSource.SyncInterpolatedTime"/>을 통해 쓰기 잠금을 획득할 수 있습니다.
        /// </remarks>
        void UnsafeSyncChannel()
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
                double currentTime = time;
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
                    if (timeSampleDirty)
                    {
                        try
                        {
                            if (scope.asset.openStates.state != SoundOpenState.SetPosition)
                            {
                                channel.time = currentTime;
                                timeSampleDirty = false;
                            }
                        }
                        catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY) { }
                    }
                    else
                        SyncInterpolatedTime(channel.time);
                }
                else
                {
                    if (scope.asset.system.Execute(system => system.PlaySound(scope.asset, true), out SoundChannel? newChannel) && newChannel != null)
                    {
                        channelLock.EnterWriteLock();

                        try
                        {
                            channel = newChannel;
                            channel.onStop += OnChannelStop;

                            try
                            {
                                if (scope.asset.openStates.state != SoundOpenState.SetPosition)
                                {
                                    channel.time = currentTime;
                                    timeSampleDirty = false;
                                }
                                else
                                    timeSampleDirty = true;
                            }
                            catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY)
                            {
                                timeSampleDirty = true;
                            }

                            UnsafeUpdateChannelProperty(channel);
                        }
                        finally
                        {
                            channelLock.ExitWriteLock();
                        }
                    }
                }
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE && channel != null) // channel == null에서 ERR_INVALID_HANDLE 에러는 정상 경로가 아님
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

        /// <remarks>
        /// The caller must hold the upgradeable read lock or write lock of <see cref="RuniAudioSource.playingLock"/>.<br/>
        /// Channel state is changed while holding the separate channel write lock; this method does not upgrade <see cref="RuniAudioSource.playingLock"/> itself.
        /// <br/><br/>
        /// 호출자는 <see cref="RuniAudioSource.playingLock"/>의 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 보유해야 합니다.<br/>
        /// 채널 상태는 별도의 채널 쓰기 잠금을 보유한 상태에서 변경하며, 이 메서드 자체는 <see cref="RuniAudioSource.playingLock"/>을 승격하지 않습니다.
        /// </remarks>
        void StopChannel()
        {
            Debug.Assert
            (
                playingLock.IsUpgradeableReadLockHeld || playingLock.IsWriteLockHeld,
                "호출 전에 playingLock의 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 먼저 보유해야합니다."
            );
            Debug.Assert(!channelLock.IsReadLockHeld, "업그레이드 락 또는 쓰기 락만 보유해야합니다.");

            channelLock.EnterWriteLock();

            try
            {
                if (channel != null)
                    channel.onStop -= OnChannelStop;

                UnsafeReleasePitchDSPList(channel);

                channel?.Stop();
                channel = null;

                lastTimeSamples = uint.MaxValue;
                timeSampleDirty = false;
            }
            finally
            {
                channelLock.ExitWriteLock();
            }
        }

        void OnChannelStop(SoundChannel disposing) => HandleChannelLost(disposing);

        void HandleChannelLost(SoundChannel lostChannel)
        {
            PitchShiftDSP[] pitchDSPs;

            channelLock.EnterWriteLock();

            try
            {
                if (!ReferenceEquals(channel, lostChannel))
                    return;

                channel.onStop -= OnChannelStop;
                channel = null;

                pitchDSPs = pitchDSPList.ToArray();
                pitchDSPList.Clear();

                lastTimeSamples = uint.MaxValue;
                timeSampleDirty = false;
            }
            finally
            {
                channelLock.ExitWriteLock();
            }

            foreach (PitchShiftDSP dsp in pitchDSPs)
                dsp.Dispose();
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
                if (pitchDSPList.Count > 0)
                    UnsafeReleasePitchDSPList(channel);

                return;
            }

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
                channel.RemoveDSP(dsp);
                dsp.Dispose();

                pitchDSPList.RemoveAt(i);
            }

            void SetPitchDsp(int index, float value, SoundChannel channel)
            {
                while (pitchDSPList.Count <= index)
                {
                    PitchShiftDSP pitchDsp = channel.system.CreateDSP<PitchShiftDSP>();
                    pitchDsp.fftSize = fftSize;
                    pitchDSPList.Add(pitchDsp);
                    channel.AddDSP(pitchDsp);
                }

                pitchDSPList[index].pitch = value;
            }
        }

        /// <remarks>
        /// channel에 부착된 DSP 목록과 함께 변경되므로 호출자는 channelLock을 보유해야 합니다.
        /// </remarks>
        void UnsafeReleasePitchDSPList(SoundChannel? channel)
        {
            for (int i = 0; i < pitchDSPList.Count; i++)
            {
                PitchShiftDSP dsp = pitchDSPList[i];

                try
                {
                    channel?.RemoveDSP(dsp);
                }
                catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE) { }

                dsp.Dispose();
            }

            pitchDSPList.Clear();
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
