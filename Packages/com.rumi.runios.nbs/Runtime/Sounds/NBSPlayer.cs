#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.NBS;
using RuniOS.Resource;
using RuniOS.Tasks;
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Plays scoped NBS resources through FMOD from a shared background scheduling worker.<br/>
    /// 공유 백그라운드 예약 워커에서 스코프 기반 NBS 리소스를 FMOD로 재생합니다.
    /// </summary>
    [ExecuteAlways]
    public sealed partial class NBSPlayer : RuniAudioSource, IReloadable
    {
        /// <summary>
        /// Gets or sets the NBS resource reference.
        /// NBS 리소스 참조를 가져오거나 설정합니다.
        /// </summary>
        public AssetRef<NBSFile> nbsFileRef
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    return _nbsFileRef;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    if (_nbsFileRef == value)
                        return;

                    _nbsFileRef = value;
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }
        [SerializeField] AssetRef<NBSFile> _nbsFileRef;

        /// <summary>Gets the currently scoped NBS file, or <see langword="null"/> while unavailable.<br/>현재 스코프된 NBS 파일을 가져오며, 사용할 수 없으면 <see langword="null"/>입니다.</summary>
        public NBSFile? nbsFile
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
        IAssetScope<NBSFile>? nbsScope;
        NBSInstrumentBank? instrumentBank;

        /// <summary>
        /// Gets or sets the transport position in seconds. The value is intentionally not clamped to the song range.<br/>
        /// 초 단위 트랜스포트 위치를 가져오거나 설정합니다. 값은 의도적으로 곡 범위에 제한되지 않습니다.
        /// </summary>
        /// <remarks>
        /// Resource loading never pauses, rewinds, or rebases this transport.<br/>
        /// When resources become available after playback starts, note scheduling begins from the current transport position.
        /// <br/><br/>
        /// 리소스 로딩은 이 트랜스포트를 정지하거나 되감거나 재기준화하지 않습니다.<br/>
        /// 재생 시작 후 리소스가 준비되면 현재 트랜스포트 위치부터 노트 예약을 시작합니다.
        /// </remarks>
        public override double time
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    return base.time;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    CancelFutureReservationsUnsafe();
                    base.time = value;

                    completedFileLoops = 0;
                    ResetCursorUnsafe(value, true);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                NBSPlaybackWorker.Signal();
            }
        }

        /// <summary>
        /// Gets or sets the unbounded logical NBS tick position.<br/>
        /// 범위 제한 없는 논리적 NBS 틱 위치를 가져오거나 설정합니다.
        /// </summary>
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
                NBSFile? file = nbsFile;
                time = file?.tempoMap.TickToTime(value) ?? 0;
            }
        }

        /// <summary>
        /// Gets or sets the nearest active tick-column index. Ties choose the lower tick.<br/>
        /// 가장 가까운 활성 틱 열 인덱스를 가져오거나 설정합니다. 거리가 같으면 더 낮은 틱을 선택합니다.
        /// </summary>
        public int index
        {
            get
            {
                playingLock.EnterReadLock();
                try
                {
                    NBSFile? file = nbsScope?.asset;
                    return file == null ? 0 : FindNearestIndex(file, file.tempoMap.TimeToTick(base.time));
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
            set
            {
                NBSFile? file = nbsFile;
                if (file == null || file.ticks.Count == 0)
                    return;

                int clamped = Math.Clamp(value, 0, file.ticks.Count - 1);
                tick = file.ticks[clamped].tick;
            }
        }

        /// <summary>Gets the logical song length in NBS ticks.<br/>NBS 틱 단위의 논리적 곡 길이를 가져옵니다.</summary>
        public int tickLength => nbsFile?.tickLength ?? 0;

        /// <summary>Gets the number of active tick columns addressable by <see cref="index"/>.<br/><see cref="index"/>로 접근 가능한 활성 틱 열 수를 가져옵니다.</summary>
        public int indexLength => nbsFile?.ticks.Count ?? 0;

        /// <summary>Gets the file tempo in ticks per second at the current position.<br/>현재 위치의 파일 템포를 초당 틱 수로 가져옵니다.</summary>
        public double ticksPerSecond => nbsFile?.tempoMap.GetTicksPerSecond(tick) ?? 0;

        /// <summary>Gets the file tempo in beats per minute, where one beat equals four NBS ticks.<br/>한 박을 NBS 4틱으로 계산한 분당 박자 수를 가져옵니다.</summary>
        public double beatsPerMinute => ticksPerSecond * 15;

        /// <inheritdoc/>
        public override double length => nbsFile?.duration ?? 0;

        /// <summary>
        /// Gets or sets whether the loop flag, start tick, and maximum loop count stored in the file are honored.<br/>
        /// 파일에 저장된 루프 플래그, 시작 틱 및 최대 루프 횟수를 사용할지 가져오거나 설정합니다.
        /// </summary>
        public bool useFileLoopSettings
        {
            get => Volatile.Read(ref _useFileLoopSettings);
            set
            {
                Volatile.Write(ref _useFileLoopSettings, value);
                InvalidateFutureSchedule(false);
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
                    CancelFutureReservationsUnsafe();
                    base.tempo = value;
                    ResetCursorUnsafe(false);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                NBSPlaybackWorker.Signal();
            }
        }

        /// <inheritdoc/>
        public override float pitch
        {
            get => base.pitch;
            set
            {
                base.pitch = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float volume
        {
            get => base.volume;
            set
            {
                base.volume = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float panStereo
        {
            get => base.panStereo;
            set
            {
                base.panStereo = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float spatialBlend
        {
            get => base.spatialBlend;
            set
            {
                base.spatialBlend = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float dopplerLevel
        {
            get => base.dopplerLevel;
            set
            {
                base.dopplerLevel = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float spread
        {
            get => base.spread;
            set
            {
                base.spread = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float minDistance
        {
            get => base.minDistance;
            set
            {
                base.minDistance = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override float maxDistance
        {
            get => base.maxDistance;
            set
            {
                base.maxDistance = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <summary>
        /// Gets or sets the 3D distance attenuation curve used by every note voice.<br/>
        /// 모든 노트 Voice에 사용할 3D 거리 감쇠 곡선을 가져오거나 설정합니다.
        /// </summary>
        public SoundRolloffMode rolloffMode
        {
            get => _rolloffMode;
            set
            {
                playingLock.EnterWriteLock();
                try
                {
                    _rolloffMode = value;
                    UpdateVoiceRolloffModeUnsafe(value);
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }
        [SerializeField] volatile SoundRolloffMode _rolloffMode = SoundRolloffMode.inverse;

        /// <summary>
        /// Gets or sets whether source velocity is estimated from Transform changes when no physics body supplies it.<br/>
        /// 물리 바디가 속도를 제공하지 않을 때 Transform 변화량으로 소스 속도를 추정할지 가져오거나 설정합니다.
        /// </summary>
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
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override double loopStart
        {
            get => base.loopStart;
            set
            {
                base.loopStart = value;
                InvalidateFutureSchedule(false);
            }
        }

        /// <inheritdoc/>
        public override double loopEnd
        {
            get => base.loopEnd;
            set
            {
                base.loopEnd = value;
                InvalidateFutureSchedule(false);
            }
        }

        Dictionary<(int tick, int layer), NBSSpecialEvent> specialEventMap = [];
        int nextTickIndex;
        int completedFileLoops;
        int scheduledFileLoops;
        bool includeCurrentOnUnPause;
        long observedSchedulingRevision = NBSPlaybackSettings.schedulingRevision;
        readonly AsyncReloadGate reloadGate = new AsyncReloadGate();

        /// <summary>
        /// Reloads the NBS scope and all instrument scopes through the resource registries, stopping active voices when replacing them.<br/>
        /// 리소스 레지스트리를 통해 NBS 스코프와 모든 악기 스코프를 다시 로드하며, 교체할 때 재생 중인 Voice를 정지합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that completes after the scope generation has been swapped.<br/>
        /// 스코프 세대 교체가 끝나면 완료되는 비동기 작업입니다.
        /// </returns>
        /// <remarks>
        /// Call this method from the Unity main thread.<br/>
        /// Unity 메인 스레드에서 호출하세요.
        /// </remarks>
        public UniTask Reload() => reloadGate.Run(ReloadCore);

        async UniTask ReloadCore()
        {
            if (this == null || !isActiveAndEnabled)
                return;

            if (nbsFileRef.IsSameTarget(nbsScope))
                return;

            AssetRef<NBSFile> target = nbsFileRef;
            IAssetScope<NBSFile>? newScope = await target.LoadScopeAsync();

            if (this == null || !isActiveAndEnabled)
            {
                DisposeQueue.Enqueue(newScope);
                return;
            }

            NBSInstrumentBank? newBank = null;
            try
            {
                if (newScope != null)
                    newBank = await NBSInstrumentBank.Create(newScope.asset, target.key.assetId);
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

            IAssetScope<NBSFile>? oldScope;
            NBSInstrumentBank? oldBank;
            playingLock.EnterWriteLock();
            try
            {
                StopAllVoicesUnsafe();

                oldScope = nbsScope;
                oldBank = instrumentBank;
                nbsScope = newScope;
                instrumentBank = newBank;
                specialEventMap = newScope?.asset.specialEvents.ToDictionary(x => (x.tick, x.layer)) ?? [];
                completedFileLoops = 0;
                ResetCursorUnsafe(true);
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            DisposeQueue.Enqueue(oldScope);
            DisposeQueue.Enqueue(oldBank);
            NBSPlaybackWorker.Signal();
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

            var transform = this.transform;

            AudioSpatialState initialSpatialState = new AudioSpatialState(transform);
            lock (spatialSnapshotLock)
                spatialSnapshot = initialSpatialState;

            lastSpatialPosition = transform.position;

            ResourceManager.AttachReloadable(this);
            NBSPlaybackWorker.Register(this);

            Reload().Forget();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ResourceManager.DetachReloadable(this);
            NBSPlaybackWorker.Unregister(this);

            IAssetScope<NBSFile>? oldScope;
            NBSInstrumentBank? oldBank;
            playingLock.EnterWriteLock();
            try
            {
                oldScope = nbsScope;
                oldBank = instrumentBank;
                nbsScope = null;
                instrumentBank = null;
                specialEventMap = [];
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
            completedFileLoops = 0;
            includeCurrentOnUnPause = isPaused;
            NBSPlaybackWorker.Signal();
        }

        protected override void OnStop()
        {
            StopAllVoicesUnsafe();
            completedFileLoops = 0;
            includeCurrentOnUnPause = false;
            ResetCursorUnsafe(true);
        }

        protected override void OnPause()
        {
            CancelFutureReservationsUnsafe();
            ResetCursorUnsafe(false);
        }

        protected override void OnUnPause()
        {
            ResetCursorUnsafe(includeCurrentOnUnPause);
            includeCurrentOnUnPause = false;

            NBSPlaybackWorker.Signal();
        }

        void OnValidate() => NBSPlaybackWorker.Signal();

        void InvalidateFutureSchedule(bool includeCurrent)
        {
            playingLock.EnterWriteLock();
            try
            {
                CancelFutureReservationsUnsafe();
                ResetCursorUnsafe(includeCurrent);
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            NBSPlaybackWorker.Signal();
        }

        void ResetCursorUnsafe(bool includeCurrent) => ResetCursorUnsafe(base.time, includeCurrent);

        void ResetCursorUnsafe(double currentTime, bool includeCurrent)
        {
            observedSchedulingRevision = NBSPlaybackSettings.schedulingRevision;
            scheduledFileLoops = completedFileLoops;

            NBSFile? file = nbsScope?.asset;
            if (file == null || file.ticks.Count == 0 || !double.IsFinite(currentTime))
            {
                nextTickIndex = 0;
                return;
            }

            double currentTick = file.tempoMap.TimeToTick(currentTime);
            nextTickIndex = base.tempo < 0
                ? FindReverseCursor(file, currentTick, includeCurrent)
                : FindForwardCursor(file, currentTick, includeCurrent);
        }

        static int FindForwardCursor(NBSFile file, double tick, bool includeCurrent)
        {
            int low = 0;
            int high = file.ticks.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool before = includeCurrent ? file.ticks[middle].tick < tick : file.ticks[middle].tick <= tick;
                if (before)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        static int FindReverseCursor(NBSFile file, double tick, bool includeCurrent)
        {
            int low = 0;
            int high = file.ticks.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool accepted = includeCurrent ? file.ticks[middle].tick <= tick : file.ticks[middle].tick < tick;
                if (accepted)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low - 1;
        }

        static int FindNearestIndex(NBSFile file, double tick)
        {
            if (file.ticks.Count == 0)
                return 0;

            int upper = FindForwardCursor(file, tick, true);
            if (upper <= 0)
                return 0;
            if (upper >= file.ticks.Count)
                return file.ticks.Count - 1;

            double lowerDistance = Math.Abs(file.ticks[upper - 1].tick - tick);
            double upperDistance = Math.Abs(file.ticks[upper].tick - tick);
            return lowerDistance <= upperDistance ? upper - 1 : upper;
        }
    }
}
