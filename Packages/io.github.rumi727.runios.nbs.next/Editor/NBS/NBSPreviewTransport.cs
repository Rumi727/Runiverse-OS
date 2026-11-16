#nullable enable
using System.Threading;
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.NBS
{
    /// <summary>
    /// Plays a <see cref="NoteBlockClip"/> in the editor without a scene object, for previews.<br/>
    /// 미리보기를 위해 씬 오브젝트 없이 에디터에서 <see cref="NoteBlockClip"/>을 재생합니다.
    /// </summary>
    /// <remarks>
    /// This is a minimal transport for the inspector: it keeps its own timeline on the DSP clock and drives an <see cref="NBSVoiceScheduler{TTransport}"/>.
    /// The loop follows the NBS header, and <see cref="time"/> advances in mix blocks.<br/>
    /// 인스펙터용 최소 트랜스포트입니다. DSP 클록 위에 자기 타임라인을 가지며 <see cref="NBSVoiceScheduler{TTransport}"/>를 조작합니다.
    /// 반복은 NBS 헤더를 따르며, <see cref="time"/>은 믹스 블록 단위로 진행합니다.
    /// <br/><br/>
    /// The transport does not own the instruments; release them after disposing the transport.<br/>
    /// 트랜스포트는 악기를 소유하지 않으므로 트랜스포트를 해제한 뒤에 악기를 해제해야 합니다.
    /// </remarks>
    public sealed class NBSPreviewTransport : INBSTransport, IPlayControl, IPausable, IStoppable, ISeekable, ILoopControl, ITempoControl, IPitchControl, IDisposable
    {
        /// <summary>
        /// Initializes a stopped transport that plays a clip with loaded instruments.<br/>
        /// 로드된 악기로 클립을 재생하는, 정지된 트랜스포트를 초기화합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip to preview.<br/>
        /// 미리 들을 클립입니다.
        /// </param>
        /// <param name="bank">
        /// The instruments of <paramref name="clip"/>.<br/>
        /// <paramref name="clip"/>의 악기입니다.
        /// </param>
        public NBSPreviewTransport(NoteBlockClip clip, NBSInstrumentBank bank)
        {
            this.clip = clip;
            _loop = clip.header.loop;
            scheduler = new NBSVoiceScheduler<NBSPreviewTransport>(clip, bank, this);
            SoundSystem.main.onResetting += OnSoundSystemResetting;
            SoundSystem.main.onReset += OnSoundSystemReset;
        }

        readonly NoteBlockClip clip;
        readonly Lock gate = new();
        NBSVoiceScheduler<NBSPreviewTransport>? scheduler;

        NBSClockAnchor anchor;
        double resetTime;
        bool _isPlaying;
        bool _isPaused;
        float _tempo = 1;
        float _pitch = 1;
        bool _loop;

        /// <inheritdoc/>
        public NBSClockAnchor clockAnchor
        {
            get
            {
                lock (gate)
                    return anchor;
            }
        }

        /// <inheritdoc/>
        public bool isPlaying => Volatile.Read(ref _isPlaying);

        /// <inheritdoc/>
        public bool isPaused
        {
            get => Volatile.Read(ref _isPaused);
            set
            {
                if (value)
                    Pause();
                else
                    UnPause();
            }
        }

        /// <inheritdoc/>
        public float volume
        {
            get => scheduler?.volume ?? 1;
            set => scheduler?.volume = value;
        }

        /// <inheritdoc/>
        public double time
        {
            get
            {
                SoundSystem.main.Execute(static system => system.GetMasterDSPClock(), out ulong clock);
                return Fold(GetTime(clock));
            }
            set
            {
                Anchor(value);
                scheduler?.Resync();
            }
        }

        /// <inheritdoc/>
        public double length => clip.length;

        /// <inheritdoc/>
        public bool loop
        {
            get => _loop;
            set
            {
                MoveAnchor();
                _loop = value;
                scheduler?.OnLoopChanged();
            }
        }

        /// <summary>
        /// Gets the loop start of the NBS header in seconds. Setting it has no effect.<br/>
        /// NBS 헤더의 반복 시작을 초 단위로 가져옵니다. 설정해도 효과가 없습니다.
        /// </summary>
        public double loopStart
        {
            get => clip.map.tempo.TickToTime(clip.header.loopStartTick);
            set { }
        }

        /// <summary>
        /// Gets the song length, where the NBS loop ends. Setting it has no effect.<br/>
        /// NBS 반복이 끝나는 곡 길이를 가져옵니다. 설정해도 효과가 없습니다.
        /// </summary>
        public double loopEnd
        {
            get => clip.length;
            set { }
        }

        /// <inheritdoc/>
        public NBSLoopRange? loopRange => NBSLoopRange.Create(_loop, loopStart, loopEnd);

        /// <inheritdoc/>
        public float tempo
        {
            get => _tempo;
            set
            {
                MoveAnchor();
                _tempo = value;
                scheduler?.OnRateChanged();
            }
        }

        /// <inheritdoc/>
        public float pitch
        {
            get => _pitch;
            set
            {
                _pitch = value;
                scheduler?.OnRateChanged();
            }
        }

        /// <inheritdoc/>
        public void Play(double startTime = 0)
        {
            lock (gate)
            {
                _isPlaying = true;
                _isPaused = false;
            }

            time = startTime;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            lock (gate)
            {
                _isPlaying = false;
                _isPaused = false;
                anchor = anchor with { time = 0 };
            }

            scheduler?.Stop();
        }

        /// <inheritdoc/>
        public void Pause()
        {
            MoveAnchor();
            lock (gate)
                _isPaused = true;

            scheduler?.Pause();
        }

        /// <inheritdoc/>
        public void UnPause()
        {
            // Moved while still paused, so the held position is kept at the new clock.
            MoveAnchor();
            lock (gate)
                _isPaused = false;

            scheduler?.UnPause();
        }

        /// <summary>
        /// Stops the sounds of the preview. The instruments are not released.<br/>
        /// 미리보기의 소리를 멈춥니다. 악기는 해제하지 않습니다.
        /// </summary>
        public void Dispose()
        {
            SoundSystem.main.onResetting -= OnSoundSystemResetting;
            SoundSystem.main.onReset -= OnSoundSystemReset;
            scheduler?.Dispose();
            scheduler = null;
        }

        /// <summary>Anchors the timeline at the next mix clock. Without a readable clock it holds until the sound system resets.</summary>
        void Anchor(double linearTime)
        {
            ulong clock = NBSClockAnchor.TryGetNextMixClock(SoundSystem.main, out ulong nextMixClock) ? nextMixClock : ulong.MaxValue;
            lock (gate)
                anchor = new NBSClockAnchor(clock, linearTime);
        }

        /// <summary>
        /// Moves the anchor to the next mix clock without moving the timeline. The position is wrapped into the loop range first,
        /// because a later change of the tempo sign wraps only at the other boundary.
        /// </summary>
        void MoveAnchor()
        {
            bool hasClock = NBSClockAnchor.TryGetNextMixClock(SoundSystem.main, out ulong clock);
            lock (gate)
            {
                double linear = hasClock && _isPlaying && !_isPaused ? anchor.GetTime(clock, _tempo, SoundSystem.main.outputSampleRate) : anchor.time;
                anchor = new NBSClockAnchor(hasClock ? clock : ulong.MaxValue, Fold(linear));
            }
        }

        /// <summary>Gets the linear position at a DSP clock; it only advances while playing.</summary>
        double GetTime(ulong clock)
        {
            lock (gate)
                return _isPlaying && !_isPaused ? anchor.GetTime(clock, _tempo, SoundSystem.main.outputSampleRate) : anchor.time;
        }

        double Fold(double linear) => loopRange is { } range ? range.Fold(linear, _tempo) : linear;

        // The clock starts again after a reset, so the position is kept across it and anchored on the new clock.
        void OnSoundSystemResetting(SoundSystem system)
        {
            SoundSystem.main.Execute(static soundSystem => soundSystem.GetMasterDSPClock(), out ulong clock);
            resetTime = GetTime(clock);
        }

        void OnSoundSystemReset(SoundSystem system)
        {
            Anchor(resetTime);
            scheduler?.Resync();
        }
    }
}
