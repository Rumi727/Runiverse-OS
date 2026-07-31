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

        public override double time
        {
            get => base.time;
            set
            {
                lock (playingLock)
                {
                    SyncInterpolatedTime(value);
                    timeSampleDirty = !TryGetAliveChannel(channel => channel.time = value);
                }
            }
        }

        public uint timeSample
        {
            // NONBLOCKING 스트림 시킹 중에는 마지막으로 설정한 보간 시간을 반환합니다.
            get
            {
                try
                {
                    return GetAliveChannelValue(channel => channel.timeSample, 0u);
                }
                catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY)
                {
                    return interpolatedTimeSample;
                }
            }
            set
            {
                lock (playingLock)
                {
                    if (frequency > 0)
                        SyncInterpolatedTime(value / (double)frequency);

                    timeSampleDirty = !TryGetAliveChannel(channel => channel.timeSample = value);
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
                TryGetAliveChannel(UnsafeUpdateChannelMinDistance);
            }
        }

        public override float maxDistance
        {
            get => base.maxDistance;
            set
            {
                base.maxDistance = value;
                TryGetAliveChannel(UnsafeUpdateChannelMaxDistance);
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

        uint interpolatedTimeSample
        {
            get
            {
                double currentTime = time;
                if (!double.IsFinite(currentTime) || frequency <= 0 || samples == 0)
                    return 0;

                return (currentTime * frequency).RoundToUInt().Clamp(0, samples - 1);
            }
        }

#if UNITY_PHYSICS_EXIST
        Rigidbody? rigidbody;
#endif
#if UNITY_PHYSICS2D_EXIST
        Rigidbody2D? rigidbody2D;
#endif

        Vector3 lastPosition;

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

        void Update()
        {
            try
            {
                uint timeSample = GetAliveChannelValue(channel => channel.timeSample, 0u);
                if (timeSampleDirty || lastTimeSamples != timeSample)
                {
                    lastTimeSamples = timeSample;

                    lock (playingLock)
                        UnsafeSyncChannel();
                }
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY)
            {
                // NONBLOCKING stream seek 중에는 position을 읽을 수 없습니다.
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

        protected override void OnDisable()
        {
            base.OnDisable();

            ResourceManager.DetachReloadable(this);

            DisposeQueue.Enqueue(scope);
            scope = null;

            Volatile.Write(ref clipLength, 0);
            clipSamples = 0;
            clipFrequency = 0;
        }

        readonly AsyncReloadGate reloadGate = new();

        /// <remarks>
        /// 메인 스레드에서만 사용해야합니다!
        /// </remarks>
        public UniTask Reload() => reloadGate.Run(ReloadCore);

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
            lock (playingLock)
            {
                StopChannel();

                DisposeQueue.Enqueue(scope);
                scope = newScope;

                Volatile.Write(ref clipLength, scope?.asset.length ?? 0);
                clipSamples = scope?.asset.samples ?? 0;
                clipFrequency = scope?.asset.frequency ?? 0;

                UnsafeSyncChannel();
            }
        }

        bool TryGetAliveChannel(Action<SoundChannel> action)
        {
            SoundChannel? lostChannel = null;
            bool success = false;
            channelLock.EnterReadLock();

            try
            {
                SoundChannel? currentChannel = channel;
                if (currentChannel == null)
                    return false;

                action.Invoke(currentChannel);
                success = true;
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE)
            {
                lostChannel = channel;
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY) { }
            finally
            {
                channelLock.ExitReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);

            return success;
        }

        T GetAliveChannelValue<T>(Func<SoundChannel, T> func, T defaultValue)
        {
            SoundChannel? lostChannel = null;
            T result = defaultValue;
            channelLock.EnterReadLock();

            try
            {
                if (channel == null)
                    return result;

                try
                {
                    result = func.Invoke(channel);
                }
                catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE)
                {
                    lostChannel = channel;
                }
            }
            finally
            {
                channelLock.ExitReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);

            return result;
        }

        public void GetTempoAndPitch(out float tempo, out float pitch)
        {
            lock (tempoAndPitchLock)
            {
                tempo = this.tempo;
                pitch = this.pitch;
            }
        }

        protected override void OnPlay() => UnsafeSyncChannel();
        protected override void OnStop() => UnsafeSyncChannel();

        protected override void OnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);
        protected override void OnUnPause() => TryGetAliveChannel(UnsafeUpdateChannelPause);

        /// <remarks>
        /// 호출 전에 <see cref="RuniAudioSource.playingLock"/>를 먼저 보유야합니다.
        /// </remarks>
        void UnsafeSyncChannel()
        {
            Debug.Assert(Monitor.IsEntered(playingLock), "호출 전에 playingLock를 먼저 보유해야합니다.");

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
                        channel.time = currentTime;
                        timeSampleDirty = false;
                    }
                    else
                        SyncInterpolatedTime(channel.time);
                }
                else
                {
                    channelLock.EnterWriteLock();

                    try
                    {
                        scope.asset.system.Execute(system =>
                        {
                            channel = system.PlaySound(scope.asset, true);
                            channel.onStop += OnChannelStop;

                            UnsafeUpdateChannelProperty(channel);

                            channel.time = currentTime;
                            timeSampleDirty = false;
                        });
                    }
                    finally
                    {
                        channelLock.ExitWriteLock();
                    }
                }
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_INVALID_HANDLE && channel != null) // channel == null에서 ERR_INVALID_HANDLE 에러는 정상 경로가 아님
            {
                lostChannel = channel;
            }
            catch (FMODException exception) when (exception.result == RESULT.ERR_NOTREADY)
            {
                timeSampleDirty = true;
            }
            finally
            {
                channelLock.ExitUpgradeableReadLock();
            }

            if (lostChannel != null)
                HandleChannelLost(lostChannel);
        }

        /// <remarks>
        /// 호출 전에 <see cref="RuniAudioSource.playingLock"/>를 먼저 보유야합니다.
        /// </remarks>
        void StopChannel()
        {
            Debug.Assert(Monitor.IsEntered(playingLock), "호출 전에 playingLock를 먼저 보유해야합니다.");
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
            UnsafeUpdateChannelMinDistance(channel);
            UnsafeUpdateChannelMaxDistance(channel);
            UnsafeUpdateChannelRolloffMode(channel);
            UnsafeUpdateChannelPause(channel);
        }

        void UnsafeUnsafeUpdateChannelLoop(SoundChannel channel)
        {
            channel.loop = loop;

            channel.loopStart = loopStart;
            channel.loopEnd = loopEnd;
        }

        void UnsafeUnsafeUpdateChannelTempoAndPitch(SoundChannel channel)
        {
            channel.frequency = (channel.clip?.frequency ?? channel.frequency) * tempo;
            UnsafeUpdateChannelPitch(channel);
        }

        void UnsafeUpdateChannelPitch(SoundChannel channel)
        {
            float tempo = this.tempo;
            if (tempo == 0)
            {
                UnsafeReleasePitchDSPList(channel);
                return;
            }

            float value = pitch / tempo.Abs();
            if (float.IsNaN(value))
                value = 1;

            value = value.Clamp(0.0001f, 100);
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
            Debug.Log(pitchDSPList.Count);

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

        void UnsafeUpdateChannelMinDistance(SoundChannel channel) => channel.minDistance = minDistance;

        void UnsafeUpdateChannelMaxDistance(SoundChannel channel) => channel.maxDistance = maxDistance;

        void UnsafeUpdateChannelRolloffMode(SoundChannel channel) => channel.rolloffMode = rolloffMode;

        void UnsafeUpdateChannelPause(SoundChannel channel) => channel.isPaused = isPaused;
    }
}
