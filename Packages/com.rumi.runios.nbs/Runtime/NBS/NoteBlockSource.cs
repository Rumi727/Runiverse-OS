#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using RuniOS.Sounds;
using RuniOS.Tasks;
using System.Threading;

namespace RuniOS.NBS
{
    /// <summary>
    /// Plays scoped NBS resources through precomputed schedules and a shared background worker.<br/>
    /// 미리 계산된 스케줄과 공유 백그라운드 워커를 통해 스코프 기반 NBS 리소스를 재생합니다.
    /// </summary>
    [ExecuteAlways]
    public sealed partial class NoteBlockSource : RuniAudioSource, IReloadable
    {
        /// <summary>Gets or sets the NBS resource reference.<br/>NBS 리소스 참조를 가져오거나 설정합니다.</summary>
        public AssetRef<NoteBlockClip> nbsFileRef
        {
            get
            {
                nbsFileRefLock.EnterReadLock();
                try
                {
                    return _nbsFileRef;
                }
                finally
                {
                    nbsFileRefLock.ExitReadLock();
                }
            }
            set
            {
                nbsFileRefLock.EnterWriteLock();
                try
                {
                    _nbsFileRef = value;
                }
                finally
                {
                    nbsFileRefLock.ExitWriteLock();
                }
            }
        }
        readonly ReaderWriterLockSlim nbsFileRefLock = new ReaderWriterLockSlim();
        [SerializeField] AssetRef<NoteBlockClip> _nbsFileRef;

        /// <summary>Gets the currently scoped NBS file, or <see langword="null"/> while unavailable.<br/>현재 스코프된 NBS 파일을 가져오며, 사용할 수 없으면 <see langword="null"/>입니다.</summary>
        public NoteBlockClip? nbsFile
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    return nbsScope?.asset;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
        }

        IAssetScope<NoteBlockClip>? nbsScope;
        NoteBlockInstrumentBank? instrumentBank;
        NBSPlaybackSchedule? playbackSchedule;
        NBSPlaybackCursor playbackCursor;
        readonly List<PendingSubmission> pendingSubmissions = [];
        long scheduleGeneration;
        long completedLoops;
        bool restoreSnapshot;
        long playbackRevision;
        long voiceSettingsRevision;
        long observedSchedulingRevision = NoteBlockPlaybackSettings.schedulingRevision;
        readonly AsyncReloadGate reloadGate = new AsyncReloadGate();

        /// <summary>
        /// Gets or sets the loop-relative transport position in seconds.<br/>
        /// 루프 기준 트랜스포트 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        public override double time
        {
            get => base.time;
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    StopAllVoicesUnsafe();
                    pendingSubmissions.Clear();
                    base.time = value;
                    completedLoops = 0;
                    ResetCursorUnsafe(true);
                    restoreSnapshot = isPlaying;
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                NoteBlockPlaybackWorker.Signal();
            }
        }

        /// <summary>Gets or sets the unbounded logical NBS tick position.<br/>범위 제한 없는 논리적 NBS 틱 위치를 가져오거나 설정합니다.</summary>
        public double tick
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    return nbsScope?.asset.tempoMap.TimeToTick(base.time) ?? 0;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
            set
            {
                NoteBlockClip? file = nbsFile;
                time = file?.tempoMap.TickToTime(value) ?? 0;
            }
        }

        /// <summary>Gets or sets the nearest active tick-column index. Ties choose the lower tick.<br/>가장 가까운 활성 틱 열 인덱스를 가져오거나 설정합니다. 거리가 같으면 더 낮은 틱을 선택합니다.</summary>
        public int index
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    NoteBlockClip? file = nbsScope?.asset;
                    return file == null ? 0 : FindNearestIndex(file, file.tempoMap.TimeToTick(base.time));
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
            set
            {
                NoteBlockClip? file = nbsFile;
                if (file == null || file.ticks.Count == 0)
                    return;

                tick = file.ticks[Math.Clamp(value, 0, file.ticks.Count - 1)].tick;
            }
        }

        /// <summary>Gets the logical song length in NBS ticks.<br/>NBS 틱 단위의 논리적 곡 길이를 가져옵니다.</summary>
        public int tickLength => nbsFile?.tickLength ?? 0;

        /// <summary>Gets the number of active tick columns.<br/>활성 틱 열 수를 가져옵니다.</summary>
        public int indexLength => nbsFile?.ticks.Count ?? 0;

        /// <summary>Gets the file tempo in ticks per second at the current position.<br/>현재 위치의 파일 템포를 초당 틱 수로 가져옵니다.</summary>
        public double ticksPerSecond => nbsFile?.tempoMap.GetTicksPerSecond(tick) ?? 0;

        /// <summary>Gets the file tempo in beats per minute, where one beat equals four NBS ticks.<br/>한 박을 NBS 4틱으로 계산한 분당 박자 수를 가져옵니다.</summary>
        public double beatsPerMinute => ticksPerSecond * 15;

        /// <inheritdoc/>
        public override double length => nbsFile?.length ?? 0;

        /// <summary>Gets or sets whether loop metadata stored in the NBS file is used.<br/>NBS 파일에 저장된 루프 메타데이터를 사용할지 가져오거나 설정합니다.</summary>
        public bool useFileLoopSettings
        {
            get => Volatile.Read(ref _useFileLoopSettings);
            set
            {
                Volatile.Write(ref _useFileLoopSettings, value);
                InvalidateLoopSchedule();
            }
        }
        [SerializeField] bool _useFileLoopSettings = true;

        /// <inheritdoc/>
        public override float tempo
        {
            get => base.tempo;
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    if (base.tempo.Equals(value))
                        return;

                    CancelFutureSubmissionsUnsafe();
                    base.tempo = value;
                    RebuildScheduleUnsafe(false, false);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                NoteBlockPlaybackWorker.Signal();
            }
        }

        /// <inheritdoc/>
        public override float pitch
        {
            get => base.pitch;
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    float previous = base.pitch;
                    if (previous.Equals(value))
                        return;

                    CancelFutureSubmissionsUnsafe();
                    base.pitch = value;
                    bool restoringFromZero = previous == 0 && value != 0;
                    if (!float.IsFinite(value) || value == 0)
                        StopAllVoicesUnsafe();
                    else
                        UpdateVoiceFrequenciesUnsafe();

                    RebuildScheduleUnsafe(restoringFromZero, restoringFromZero);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                NoteBlockPlaybackWorker.Signal();
            }
        }

        /// <inheritdoc/>
        public override float volume
        {
            get => base.volume;
            set
            {
                lock (voiceSettingsApplyLock)
                {
                    Voice[] snapshot;
                    playingLock.EnterWriteLock();
                    try
                    {
                        base.volume = value;
                        voiceSettingsRevision++;
                        snapshot = GetVoiceSnapshot();
                    }
                    finally
                    {
                        playingLock.ExitWriteLock();
                    }

                    UpdateVoiceVolumesUnsafe(snapshot, value);
                }
            }
        }

        /// <inheritdoc/>
        public override float panStereo
        {
            get => base.panStereo;
            set
            {
                lock (voiceSettingsApplyLock)
                {
                    Voice[] snapshot;
                    playingLock.EnterWriteLock();
                    try
                    {
                        base.panStereo = value;
                        voiceSettingsRevision++;
                        snapshot = GetVoiceSnapshot();
                    }
                    finally
                    {
                        playingLock.ExitWriteLock();
                    }

                    UpdateVoicePansUnsafe(snapshot, value);
                }
            }
        }

        /// <inheritdoc/>
        public override float spatialBlend
        {
            get => base.spatialBlend;
            set => SetSpatialProperty(value, static (channel, propertyValue) => channel.spatialBlend = propertyValue, static (player, propertyValue) => player.SetBaseSpatialBlend(propertyValue));
        }

        /// <inheritdoc/>
        public override float dopplerLevel
        {
            get => base.dopplerLevel;
            set => SetSpatialProperty(value, static (channel, propertyValue) => channel.dopplerLevel = propertyValue, static (player, propertyValue) => player.SetBaseDopplerLevel(propertyValue));
        }

        /// <inheritdoc/>
        public override float spread
        {
            get => base.spread;
            set => SetSpatialProperty(value, static (channel, propertyValue) => channel.spread = propertyValue, static (player, propertyValue) => player.SetBaseSpread(propertyValue));
        }

        /// <inheritdoc/>
        public override float minDistance
        {
            get => base.minDistance;
            set => SetDistanceProperty(value, true);
        }

        /// <inheritdoc/>
        public override float maxDistance
        {
            get => base.maxDistance;
            set => SetDistanceProperty(value, false);
        }

        /// <summary>Gets or sets the 3D distance attenuation curve used by every Voice.<br/>모든 Voice에 사용할 3D 거리 감쇠 곡선을 가져오거나 설정합니다.</summary>
        public SoundRolloffMode rolloffMode
        {
            get => _rolloffMode;
            set
            {
                lock (voiceSettingsApplyLock)
                {
                    Voice[] snapshot;
                    playingLock.EnterWriteLock();
                    try
                    {
                        _rolloffMode = value;
                        voiceSettingsRevision++;
                        snapshot = GetVoiceSnapshot();
                    }
                    finally
                    {
                        playingLock.ExitWriteLock();
                    }

                    UpdateVoiceRolloffModeUnsafe(snapshot, value);
                }
            }
        }
        [SerializeField] volatile SoundRolloffMode _rolloffMode = SoundRolloffMode.inverse;

        /// <summary>Gets or sets whether Transform changes estimate velocity when no physics body supplies it.<br/>물리 바디가 속도를 제공하지 않을 때 Transform 변화로 속도를 추정할지 가져오거나 설정합니다.</summary>
        public bool nonRigidbodyVelocity
        {
            get => _nonRigidbodyVelocity;
            set => _nonRigidbodyVelocity = value;
        }
        [SerializeField] bool _nonRigidbodyVelocity;

        /// <inheritdoc/>
        public override bool loop
        {
            get => base.loop;
            set
            {
                base.loop = value;
                InvalidateLoopSchedule();
            }
        }

        /// <inheritdoc/>
        public override double loopStart
        {
            get => base.loopStart;
            set
            {
                base.loopStart = value;
                InvalidateLoopSchedule();
            }
        }

        /// <inheritdoc/>
        public override double loopEnd
        {
            get => base.loopEnd;
            set
            {
                base.loopEnd = value;
                InvalidateLoopSchedule();
            }
        }

        /// <summary>Reloads the NBS scope and all unique instrument scopes.<br/>NBS 스코프와 모든 고유 악기 스코프를 다시 로드합니다.</summary>
        /// <returns>An asynchronous operation that represents completion of the generation swap.<br/>세대 교체 완료를 나타내는 비동기 작업입니다.</returns>
        public UniTask Reload() => reloadGate.Run(ReloadCore);

        async UniTask ReloadCore()
        {
            AssetRef<NoteBlockClip> target = nbsFileRef;
            if (this == null || !isActiveAndEnabled || target.IsSameTarget(nbsScope))
                return;

            IAssetScope<NoteBlockClip>? newScope = await target.LoadScopeAsync();
            if (this == null || !isActiveAndEnabled)
            {
                DisposeQueue.Enqueue(newScope);
                return;
            }

            NoteBlockInstrumentBank? newBank = null;
            try
            {
                if (newScope != null)
                    newBank = await NoteBlockInstrumentBank.Create(newScope.asset.playbackMap, target.key.assetId);
            }
            catch
            {
                DisposeQueue.Enqueue(newScope);
                throw;
            }

            if (this == null || !isActiveAndEnabled)
            {
                DisposeQueue.Enqueue(newScope);
                DisposeQueue.Enqueue(newBank);
                return;
            }

            IAssetScope<NoteBlockClip>? oldScope;
            NoteBlockInstrumentBank? oldBank;
            Voice[] oldVoices;
            while (true)
            {
                float preparedTempo = base.tempo;
                float preparedPitch = base.pitch;
                NBSPlaybackSchedule? preparedSchedule;
                try
                {
                    preparedSchedule = newScope != null && newBank != null &&
                        float.IsFinite(preparedTempo) && preparedTempo != 0 &&
                        float.IsFinite(preparedPitch) && preparedPitch != 0
                        ? newScope.asset.playbackMap.CreateSchedule(preparedTempo, preparedPitch, newBank)
                        : null;
                }
                catch
                {
                    DisposeQueue.Enqueue(newScope);
                    DisposeQueue.Enqueue(newBank);
                    throw;
                }

                bool swapped;
                playingLock.EnterWriteLock();
                try
                {
                    if (!base.tempo.Equals(preparedTempo) || !base.pitch.Equals(preparedPitch))
                        continue;

                    oldVoices = DetachAllVoicesUnsafe();
                    pendingSubmissions.Clear();
                    oldScope = nbsScope;
                    oldBank = instrumentBank;
                    nbsScope = newScope;
                    instrumentBank = newBank;
                    completedLoops = 0;
                    scheduleGeneration++;
                    playbackSchedule = preparedSchedule;
                    ResetCursorUnsafe(true);
                    restoreSnapshot = isPlaying;
                    swapped = true;
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                if (swapped)
                    break;
            }

            StopVoices(oldVoices);
            DisposeQueue.Enqueue(oldScope);
            DisposeQueue.Enqueue(oldBank);
            NoteBlockPlaybackWorker.Signal();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_PHYSICS_EXIST
            rigidbody = GetComponent<Rigidbody>();
#endif
#if UNITY_PHYSICS2D_EXIST
            rigidbody2D = GetComponent<Rigidbody2D>();
#endif
            Transform currentTransform = transform;
            lock (spatialSnapshotLock)
                spatialSnapshot = new AudioSpatialState(currentTransform);
            lastSpatialPosition = currentTransform.position;

            ResourceManager.AttachReloadable(this);
            NoteBlockPlaybackWorker.Register(this);
            Reload().Forget();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ResourceManager.DetachReloadable(this);
            NoteBlockPlaybackWorker.Unregister(this);

            IAssetScope<NoteBlockClip>? oldScope;
            NoteBlockInstrumentBank? oldBank;
            playingLock.EnterWriteLock();
            try
            {
                StopAllVoicesUnsafe();
                pendingSubmissions.Clear();
                oldScope = nbsScope;
                oldBank = instrumentBank;
                nbsScope = null;
                instrumentBank = null;
                playbackSchedule = null;
                playbackCursor = default;
                restoreSnapshot = false;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            DisposeQueue.Enqueue(oldScope);
            DisposeQueue.Enqueue(oldBank);
        }

        protected override void OnPlay()
        {
            StopAllVoicesUnsafe();
            pendingSubmissions.Clear();
            completedLoops = 0;
            ResetCursorUnsafe(true);
            restoreSnapshot = true;
            NoteBlockPlaybackWorker.Signal();
        }

        protected override void OnStop()
        {
            StopAllVoicesUnsafe();
            pendingSubmissions.Clear();
            completedLoops = 0;
            restoreSnapshot = false;
            ResetCursorUnsafe(true);
        }

        protected override void OnPause()
        {
            playbackRevision++;
            CancelFutureSubmissionsUnsafe();
            ClearVoiceEndDelaysUnsafe();
            PauseVoicesUnsafe();
        }

        protected override void OnUnPause()
        {
            UnPauseVoicesUnsafe();
            if (!playbackCursor.initialized)
                ResetCursorUnsafe(false);
            NoteBlockPlaybackWorker.Signal();
        }

        void OnValidate() => NoteBlockPlaybackWorker.Signal();

        void InvalidateLoopSchedule()
        {
            playingLock.EnterWriteLock();
            try
            {
                CancelFutureSubmissionsUnsafe();
                scheduleGeneration++;
                completedLoops = 0;
                ResetCursorUnsafe(false);
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            NoteBlockPlaybackWorker.Signal();
        }

        void RebuildScheduleUnsafe(bool includeCurrent, bool includePreviousNotes)
        {
            scheduleGeneration++;
            NoteBlockClip? file = nbsScope?.asset;
            NoteBlockInstrumentBank? bank = instrumentBank;
            float currentTempo = base.tempo;
            float currentPitch = base.pitch;
            playbackSchedule = file != null && bank != null &&
                float.IsFinite(currentTempo) && currentTempo != 0 &&
                float.IsFinite(currentPitch) && currentPitch != 0
                ? file.playbackMap.CreateSchedule(currentTempo, currentPitch, bank)
                : null;
            ResetCursorUnsafe(includeCurrent);
            restoreSnapshot = includePreviousNotes && isPlaying;
        }

        void ResetCursorUnsafe(bool includeCurrent)
        {
            playbackRevision++;
            observedSchedulingRevision = NoteBlockPlaybackSettings.schedulingRevision;
            NBSPlaybackSchedule? schedule = playbackSchedule;
            if (schedule == null || !double.IsFinite(base.time))
            {
                playbackCursor = default;
                return;
            }

            playbackCursor = schedule.CreateCursor
            (
                new NBSPlaybackPosition(base.time, completedLoops),
                GetLoopInfoUnsafe(nbsScope?.asset),
                scheduleGeneration,
                includeCurrent
            );
        }

        static int FindNearestIndex(NoteBlockClip file, double tick)
        {
            if (file.ticks.Count == 0)
                return 0;

            int low = 0;
            int high = file.ticks.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (file.ticks[middle].tick < tick)
                    low = middle + 1;
                else
                    high = middle;
            }

            if (low <= 0)
                return 0;
            if (low >= file.ticks.Count)
                return file.ticks.Count - 1;

            double lowerDistance = Math.Abs(file.ticks[low - 1].tick - tick);
            double upperDistance = Math.Abs(file.ticks[low].tick - tick);
            return lowerDistance <= upperDistance ? low - 1 : low;
        }
    }
}
