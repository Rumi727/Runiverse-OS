#nullable enable
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.NBS
{
    /// <summary>
    /// Plays a <see cref="NoteBlockClip"/> in the editor without a scene object, for previews.<br/>
    /// 미리보기를 위해 씬 오브젝트 없이 에디터에서 <see cref="NoteBlockClip"/>을 재생합니다.
    /// </summary>
    /// <remarks>
    /// This is a minimal transport for the inspector: it keeps its own timeline and drives an <see cref="NBSVoiceScheduler{TTransport}"/>.
    /// The loop follows the NBS header.<br/>
    /// 인스펙터용 최소 트랜스포트입니다. 자기 타임라인을 가지며 <see cref="NBSVoiceScheduler{TTransport}"/>를 조작합니다. 반복은 NBS 헤더를 따릅니다.
    /// </remarks>
    public sealed class NBSPreviewTransport : IPlayControl, IPausable, IStoppable, ISeekable, ILoopControl, ITempoControl, IPitchControl, IDisposable
    {
        NBSPreviewTransport(NoteBlockClip clip) => this.clip = clip;

        readonly NoteBlockClip clip;
        readonly Lock gate = new();
        NBSInstrumentBank? bank;
        NBSVoiceScheduler<NBSPreviewTransport>? scheduler;

        double anchorTime;
        long anchorTimestamp;
        bool _isPlaying;
        bool _isPaused;
        float _tempo = 1;
        float _pitch = 1;
        bool _loop;

        /// <summary>
        /// Creates a preview and loads the instruments of a clip.<br/>
        /// 미리보기를 만들고 클립의 악기를 로드합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip to preview.<br/>
        /// 미리 들을 클립입니다.
        /// </param>
        /// <param name="instrumentNamespace">
        /// The resource namespace of custom instrument sounds.<br/>
        /// 커스텀 악기 사운드의 리소스 네임스페이스입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the preview.<br/>
        /// 비동기 작업이 완료되면 미리보기를 반환합니다.
        /// </returns>
        public static async UniTask<NBSPreviewTransport> CreateAsync(NoteBlockClip clip, string instrumentNamespace)
        {
            NBSPreviewTransport transport = new NBSPreviewTransport(clip) { _loop = clip.header.loop };
            transport.bank = await NBSInstrumentBank.LoadAsync(clip.map, instrumentNamespace);
            transport.scheduler = new NBSVoiceScheduler<NBSPreviewTransport>(clip, transport.bank, transport);
            return transport;
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
                lock (gate)
                {
                    double linear = _isPlaying && !_isPaused ? anchorTime + (Stopwatch.GetElapsedTime(anchorTimestamp).TotalSeconds * _tempo) : anchorTime;
                    return NBSLoopRange.Create(_loop, loopStart, loopEnd) is { } range ? range.Fold(linear, _tempo) : linear;
                }
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
                Anchor(time);
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
        public float tempo
        {
            get => _tempo;
            set
            {
                Anchor(time);
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
                anchorTime = 0;
            }

            scheduler?.Stop();
        }

        /// <inheritdoc/>
        public void Pause()
        {
            Anchor(time);
            lock (gate)
                _isPaused = true;

            scheduler?.Pause();
        }

        /// <inheritdoc/>
        public void UnPause()
        {
            Anchor(time);
            lock (gate)
                _isPaused = false;

            scheduler?.UnPause();
        }

        /// <summary>
        /// Stops the preview and releases its instruments.<br/>
        /// 미리보기를 멈추고 악기를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            scheduler?.Dispose();
            bank?.Dispose();
            scheduler = null;
            bank = null;
        }

        void Anchor(double value)
        {
            lock (gate)
            {
                anchorTime = value;
                anchorTimestamp = Stopwatch.GetTimestamp();
            }
        }
    }
}
