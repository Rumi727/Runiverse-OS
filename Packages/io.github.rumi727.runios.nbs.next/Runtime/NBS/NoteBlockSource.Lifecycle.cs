#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    public sealed partial class NoteBlockSource
    {
        Vector3 lastPosition;

        /// <summary>
        /// Loads <see cref="clip"/> and its instruments again when the reference points to a different target.<br/>
        /// 참조가 다른 대상을 가리키면 <see cref="clip"/>과 그 악기를 다시 로드합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that represents completion of the reload.<br/>
        /// 리로드 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        /// <remarks>
        /// Instruments added to the clip after it was loaded stay silent until the next reload.<br/>
        /// 로드한 뒤 클립에 추가된 악기는 다음 리로드 전까지 무음입니다.
        /// </remarks>
        public UniTask Reload() => reloadGate.Run(ReloadCore);

        async UniTask ReloadCore()
        {
            AssetRef<NoteBlockClip> target = _clip;
            if (this == null || !isActiveAndEnabled || target.IsSameTarget(scope))
                return;

            IAssetScope<NoteBlockClip>? newScope = await target.LoadScopeAsync();
            NBSInstrumentBank? newBank = null;
            try
            {
                if (newScope != null && this != null && isActiveAndEnabled)
                    newBank = await NBSInstrumentBank.LoadAsync(newScope.asset.map, GetInstrumentNamespace(target));
            }
            catch
            {
                DisposeQueue.Enqueue(newScope);
                throw;
            }

            if (this == null || !isActiveAndEnabled)
            {
                DisposeQueue.Enqueue(newBank);
                DisposeQueue.Enqueue(newScope);
                return;
            }

            NBSVoiceScheduler<NoteBlockSource>? oldScheduler;
            NBSInstrumentBank? oldBank;
            IAssetScope<NoteBlockClip>? oldScope;

            playingLock.EnterWriteLock();

            try
            {
                oldScheduler = scheduler;
                oldBank = bank;
                oldScope = scope;

                scope = newScope;
                bank = newBank;
                scheduler = newScope != null && newBank != null ? CreateScheduler(newScope.asset, newBank) : null;

                // The loop range can change with the clip, so the current position is wrapped again before the new scheduler reads it.
                SyncInterpolatedTime(time);
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            oldScheduler?.Dispose();
            DisposeQueue.Enqueue(oldBank);
            DisposeQueue.Enqueue(oldScope);
        }

        NBSVoiceScheduler<NoteBlockSource> CreateScheduler(NoteBlockClip loaded, NBSInstrumentBank instruments) => new NBSVoiceScheduler<NoteBlockSource>(loaded, instruments, this)
        {
            reversePitchAlignment = _reversePitchAlignment,
            volume = volume,
            panStereo = panStereo,
            spatialBlend = spatialBlend,
            dopplerLevel = dopplerLevel,
            spread = spread,
            minMaxDistance = (minDistance, maxDistance),
            rolloffMode = _rolloffMode,
            spatialState = new AudioSpatialState(transform)
        };

        string GetInstrumentNamespace(AssetRef<NoteBlockClip> target) => target.mode switch
        {
            AssetRefMode.automatic => target.assetId.nameSpace,
            AssetRefMode.registry => target.resourceKey.assetId.nameSpace,
            _ => _directInstrumentNamespace
        };

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            lastPosition = transform.position;

            ResourceManager.AttachReloadable(this);
            Reload().Forget();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();
            ResourceManager.DetachReloadable(this);

            NBSVoiceScheduler<NoteBlockSource>? oldScheduler;
            NBSInstrumentBank? oldBank;
            IAssetScope<NoteBlockClip>? oldScope;

            playingLock.EnterWriteLock();

            try
            {
                oldScheduler = scheduler;
                oldBank = bank;
                oldScope = scope;
                scheduler = null;
                bank = null;
                scope = null;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            oldScheduler?.Dispose();
            DisposeQueue.Enqueue(oldBank);
            DisposeQueue.Enqueue(oldScope);
        }

        // RuniAudioSource.Play assigns the time, and the time setter restarts the sounds, so OnPlay has nothing left to do.

        /// <inheritdoc/>
        protected override void OnStop() => scheduler?.Stop();

        /// <inheritdoc/>
        protected override void OnPause() => scheduler?.Pause();

        /// <inheritdoc/>
        protected override void OnUnPause() => scheduler?.UnPause();

        void Update()
        {
            Transform current = transform;
            Vector3 position = current.position;
            float deltaTime = Time.deltaTime;
            Vector3 velocity = deltaTime > 0 ? (position - lastPosition) / deltaTime : Vector3.zero;
            lastPosition = position;

            scheduler?.spatialState = new AudioSpatialState(position, velocity, current.rotation);
        }
    }
}
