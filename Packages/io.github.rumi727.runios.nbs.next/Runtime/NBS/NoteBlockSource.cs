#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;
using RuniOS.Sounds;
using RuniOS.Tasks;

namespace RuniOS.NBS
{
    /// <summary>
    /// Plays a <see cref="NoteBlockClip"/> on its own timeline, as <see cref="AudioSource"/> plays an <see cref="AudioClip"/>.<br/>
    /// <see cref="AudioSource"/>가 <see cref="AudioClip"/>을 재생하듯 자기 타임라인에서 <see cref="NoteBlockClip"/>을 재생합니다.
    /// </summary>
    /// <remarks>
    /// <para>The source owns the timeline. The tempo sign sets the timeline direction and the pitch sign sets the sound direction.</para>
    /// <para>While looping, <see cref="time"/> wraps into the loop range in the direction of travel. With <see cref="useFileLoopSettings"/>, the loop range of the NBS header, from its start tick to the song end, is used when the header loop is on.</para>
    /// <br/><br/>
    /// <para>소스가 타임라인을 소유합니다. tempo 부호는 타임라인 방향을, pitch 부호는 소리 방향을 정합니다.</para>
    /// <para>반복 중에는 <see cref="time"/>이 진행 방향을 기준으로 반복 범위 안으로 되돌아갑니다. <see cref="useFileLoopSettings"/>이면 헤더 반복이 켜져 있을 때 NBS 헤더의 반복 범위(시작 tick부터 곡 끝까지)를 사용합니다.</para>
    /// </remarks>
    [ExecuteAlways]
    [AddComponentMenu("Audio/Note Block Source")]
    public sealed partial class NoteBlockSource : RuniAudioSource, IReloadable
    {
        /// <summary>
        /// Gets or sets the clip reference. Setting it reloads the clip.<br/>
        /// 클립 참조를 가져오거나 설정합니다. 설정하면 클립을 다시 로드합니다.
        /// </summary>
        public AssetRef<NoteBlockClip> clip
        {
            get => _clip;
            set
            {
                _clip = value;
                if (isActiveAndEnabled)
                    Reload().Forget();
            }
        }
        [SerializeField] AssetRef<NoteBlockClip> _clip;

        /// <summary>
        /// Gets or sets the resource namespace of custom instrument sounds when <see cref="clip"/> refers to an asset directly.<br/>
        /// <see cref="clip"/>이 에셋을 직접 참조할 때 커스텀 악기 사운드의 리소스 네임스페이스를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// Registry references use the namespace of the clip identifier instead. This setting is temporary until the rule for imported clips is decided.<br/>
        /// 레지스트리 참조는 대신 클립 식별자의 네임스페이스를 사용합니다. 이 설정은 가져온 클립의 규칙이 정해질 때까지 임시로 둡니다.
        /// </remarks>
        public string directInstrumentNamespace
        {
            get => _directInstrumentNamespace;
            set => _directInstrumentNamespace = value;
        }
        [SerializeField] string _directInstrumentNamespace = Identifier.defaultNamespace;

        /// <summary>
        /// Gets the loaded clip, or <see langword="null"/> while none is loaded.<br/>
        /// 로드된 클립을 가져오며, 로드된 클립이 없으면 <see langword="null"/>입니다.
        /// </summary>
        public NoteBlockClip? loadedClip => scope?.asset;

        /// <summary>
        /// Gets the scheduler that plays the loaded clip, or <see langword="null"/> while none is loaded.<br/>
        /// 로드된 클립을 재생하는 스케줄러를 가져오며, 로드된 클립이 없으면 <see langword="null"/>입니다.
        /// </summary>
        /// <remarks>
        /// The source drives the scheduler. Calling its commands or changing its mix directly makes it disagree with the source until the next change.<br/>
        /// 소스가 스케줄러를 조작합니다. 명령을 직접 호출하거나 믹스를 직접 바꾸면 다음 변경 전까지 소스와 어긋납니다.
        /// </remarks>
        public NBSVoiceScheduler<NoteBlockSource>? scheduler { get; private set; }

        IAssetScope<NoteBlockClip>? scope;
        NBSInstrumentBank? bank;
        readonly AsyncReloadGate reloadGate = new();

        /// <summary>
        /// Gets or sets whether the loop range of the NBS header replaces <see cref="loopStart"/> and <see cref="loopEnd"/> when the header loop is on. <see cref="loop"/> still decides whether playback loops.<br/>
        /// 헤더 반복이 켜져 있을 때 NBS 헤더의 반복 범위로 <see cref="loopStart"/>와 <see cref="loopEnd"/>를 대신할지 가져오거나 설정합니다. 반복 여부는 여전히 <see cref="loop"/>가 정합니다.
        /// </summary>
        public bool useFileLoopSettings
        {
            get => _useFileLoopSettings;
            set => ChangeLoop(() => _useFileLoopSettings = value);
        }
        [SerializeField] bool _useFileLoopSettings = true;

        /// <summary>
        /// Gets or sets where reversed sounds are placed when the pitch is negative.<br/>
        /// pitch가 음수일 때 리버스된 소리를 어디에 배치할지 가져오거나 설정합니다.
        /// </summary>
        public NBSReversePitchAlignment reversePitchAlignment
        {
            get => _reversePitchAlignment;
            set
            {
                _reversePitchAlignment = value;
                scheduler?.reversePitchAlignment = value;
            }
        }
        [SerializeField] NBSReversePitchAlignment _reversePitchAlignment;

        /// <summary>
        /// Gets or sets the distance rolloff of every sound.<br/>
        /// 모든 소리의 거리 감쇠 방식을 가져오거나 설정합니다.
        /// </summary>
        public SoundRolloffMode rolloffMode
        {
            get => _rolloffMode;
            set
            {
                _rolloffMode = value;
                scheduler?.rolloffMode = value;
            }
        }
        [SerializeField] SoundRolloffMode _rolloffMode = SoundRolloffMode.inverse;

        /// <summary>
        /// Gets or sets the playback position in seconds. While looping, the getter wraps into the loop range.<br/>
        /// 재생 위치를 초 단위로 가져오거나 설정합니다. 반복 중이면 getter는 반복 범위 안으로 되돌아간 값을 반환합니다.
        /// </summary>
        /// <remarks>
        /// Setting the position restarts the sounds from it, including the sounds that should already be ringing there.<br/>
        /// 위치를 설정하면 그 위치에서 이미 울리고 있어야 하는 소리를 포함하여 소리를 다시 시작합니다.
        /// </remarks>
        public override double time
        {
            get
            {
                double linear = base.time;
                return GetLoopRange() is { } range ? range.Fold(linear, base.tempo) : linear;
            }
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    base.time = value;

                    // RuniAudioSource.Play and Stop assign the time, so this also restarts the sounds for them.
                    scheduler?.Resync();
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback position in NBS ticks through the tempo map of the loaded clip.<br/>
        /// 로드된 클립의 템포 맵을 통해 재생 위치를 NBS tick 단위로 가져오거나 설정합니다.
        /// </summary>
        public double tick
        {
            get => loadedClip is { } current ? current.map.tempo.TimeToTick(time) : 0;
            set
            {
                if (loadedClip is { } current)
                    time = current.map.tempo.TickToTime(value);
            }
        }

        /// <summary>
        /// Gets the song length in seconds, or <c>0</c> while no clip is loaded.<br/>
        /// 곡 길이를 초 단위로 가져오며, 클립이 로드되지 않았으면 <c>0</c>입니다.
        /// </summary>
        public override double length => loadedClip?.length ?? 0;

        /// <inheritdoc/>
        public override float tempo
        {
            get => base.tempo;
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    base.tempo = value;
                    scheduler?.OnRateChanged();
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
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
                    base.pitch = value;
                    scheduler?.OnRateChanged();
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }

        /// <inheritdoc/>
        public override float volume
        {
            get => base.volume;
            set
            {
                base.volume = value;
                scheduler?.volume = value;
            }
        }

        /// <inheritdoc/>
        public override float panStereo
        {
            get => base.panStereo;
            set
            {
                base.panStereo = value;
                scheduler?.panStereo = value;
            }
        }

        /// <inheritdoc/>
        public override float spatialBlend
        {
            get => base.spatialBlend;
            set
            {
                base.spatialBlend = value;
                scheduler?.spatialBlend = value;
            }
        }

        /// <inheritdoc/>
        public override float dopplerLevel
        {
            get => base.dopplerLevel;
            set
            {
                base.dopplerLevel = value;
                scheduler?.dopplerLevel = value;
            }
        }

        /// <inheritdoc/>
        public override float spread
        {
            get => base.spread;
            set
            {
                base.spread = value;
                scheduler?.spread = value;
            }
        }

        /// <inheritdoc/>
        public override float minDistance
        {
            get => base.minDistance;
            set
            {
                base.minDistance = value;
                scheduler?.minMaxDistance = (value, maxDistance);
            }
        }

        /// <inheritdoc/>
        public override float maxDistance
        {
            get => base.maxDistance;
            set
            {
                base.maxDistance = value;
                scheduler?.minMaxDistance = (minDistance, value);
            }
        }

        /// <summary>
        /// Gets or sets the loop start in seconds.<br/>
        /// 반복 시작을 초 단위로 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="RuniAudioSource.loop"/> always decides whether playback loops; this only chooses the range.
        /// While a clip is loaded, the getter returns the loop start of the NBS header when <see cref="useFileLoopSettings"/> is set and the header loop is on,
        /// and otherwise the stored value clamped to the song.<br/>
        /// 반복 여부는 항상 <see cref="RuniAudioSource.loop"/>가 정하고, 이 값은 범위만 고릅니다.
        /// 클립이 로드되어 있으면 getter는 <see cref="useFileLoopSettings"/>가 켜져 있고 헤더 반복이 켜져 있을 때 NBS 헤더의 반복 시작을, 그렇지 않으면 곡 범위로 제한한 저장 값을 반환합니다.
        /// </remarks>
        public override double loopStart
        {
            get
            {
                NoteBlockClip? current = loadedClip;
                if (current == null)
                    return base.loopStart;

                return UsesFileLoop(current) ? current.map.tempo.TickToTime(current.header.loopStartTick) : Math.Clamp(base.loopStart, 0, current.length);
            }
            set => ChangeLoop(() => base.loopStart = value);
        }

        /// <summary>
        /// Gets or sets the loop end in seconds.<br/>
        /// 반복 끝을 초 단위로 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// While a clip is loaded, the getter returns the song length when the loop of the NBS header is used, and otherwise the stored value clamped between <see cref="loopStart"/> and the song length.<br/>
        /// 클립이 로드되어 있으면 getter는 NBS 헤더의 반복을 사용할 때 곡 길이를, 그렇지 않으면 <see cref="loopStart"/>와 곡 길이 사이로 제한한 저장 값을 반환합니다.
        /// </remarks>
        public override double loopEnd
        {
            get
            {
                NoteBlockClip? current = loadedClip;
                if (current == null)
                    return base.loopEnd;

                return UsesFileLoop(current) ? current.length : Math.Clamp(base.loopEnd, Math.Min(loopStart, current.length), current.length);
            }
            set => ChangeLoop(() => base.loopEnd = value);
        }

        /// <inheritdoc/>
        public override bool loop
        {
            get => base.loop;
            set => ChangeLoop(() => base.loop = value);
        }

        bool UsesFileLoop(NoteBlockClip current) => _useFileLoopSettings && current.header.loop;

        NBSLoopRange? GetLoopRange() => NBSLoopRange.Create(loop, loopStart, loopEnd);

        void ChangeLoop(Action change)
        {
            playingLock.EnterWriteLock();

            try
            {
                // Freezes the wrapped position first, so the new range does not move the current position.
                SyncInterpolatedTime(time);
                change();
                scheduler?.OnLoopChanged();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }
    }
}
