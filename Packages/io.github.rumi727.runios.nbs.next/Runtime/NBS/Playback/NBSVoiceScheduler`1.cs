#nullable enable
using System.Threading;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Schedules the note sounds of a clip on the FMOD DSP clock while following the timeline of a transport.<br/>
    /// 트랜스포트의 타임라인을 따라가며 클립의 노트 소리를 FMOD DSP 클록에 예약합니다.
    /// </summary>
    /// <typeparam name="TTransport">
    /// The type of the transport that owns the timeline.<br/>
    /// 타임라인을 소유하는 트랜스포트 타입입니다.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// The scheduler has no timeline of its own. The shared <see cref="NBSPlaybackWorker"/> reads the transport every pass and schedules the sounds
    /// that start within <see cref="NBSPlaybackSettings.schedulingLookahead"/>. Only those sounds and the ones still ringing have channels.
    /// </para>
    /// <para>
    /// Start clocks come from <see cref="INBSTransport.clockAnchor"/> by plain arithmetic on the DSP clock, so sounds keep exact sample spacing
    /// however the worker passes fall.
    /// </para>
    /// <para>
    /// The transport owner must call the matching command right after a discontinuity: <see cref="Resync"/> after a seek or play,
    /// <see cref="OnRateChanged"/> after a tempo or pitch change, <see cref="Pause"/>, <see cref="UnPause"/>, <see cref="Stop"/>, and <see cref="OnLoopChanged"/>.
    /// </para>
    /// <para>
    /// The scheduler does not own the clip or the bank; dispose it before releasing them. A map applied later is picked up automatically, but instruments
    /// that the bank did not load stay silent.
    /// </para>
    /// <br/><br/>
    /// <para>
    /// 스케줄러는 자기 타임라인을 갖지 않습니다. 공유 <see cref="NBSPlaybackWorker"/>가 순회마다 트랜스포트를 읽고,
    /// <see cref="NBSPlaybackSettings.schedulingLookahead"/> 안에 시작하는 소리를 예약합니다. 채널은 그 소리와 아직 울리는 소리에만 있습니다.
    /// </para>
    /// <para>
    /// 시작 클록은 <see cref="INBSTransport.clockAnchor"/>에서 DSP 클록 위의 단순 계산으로 얻으므로, 워커 순회 시점과 관계없이 소리 사이 간격이 샘플 단위로 정확합니다.
    /// </para>
    /// <para>
    /// 트랜스포트 주인은 불연속 직후 알맞은 명령을 호출해야 합니다. seek이나 재생 후에는 <see cref="Resync"/>, tempo나 pitch 변경 후에는 <see cref="OnRateChanged"/>,
    /// 그 밖에 <see cref="Pause"/>, <see cref="UnPause"/>, <see cref="Stop"/>, <see cref="OnLoopChanged"/>입니다.
    /// </para>
    /// <para>
    /// 스케줄러는 클립과 bank를 소유하지 않으므로 그것들을 해제하기 전에 스케줄러를 먼저 해제해야 합니다. 나중에 적용된 맵은 자동으로 반영되지만,
    /// bank가 로드하지 않은 악기는 무음입니다.
    /// </para>
    /// </remarks>
    public sealed partial class NBSVoiceScheduler<TTransport> : IDisposable, NBSPlaybackWorker.IClient
        where TTransport : class, INBSTransport, ISeekable, ITempoControl, IPitchControl, ILoopControl, IPausable
    {
        /// <summary>
        /// Initializes a scheduler and registers it with <see cref="NBSPlaybackWorker"/>.<br/>
        /// 스케줄러를 초기화하고 <see cref="NBSPlaybackWorker"/>에 등록합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip to play.<br/>
        /// 재생할 클립입니다.
        /// </param>
        /// <param name="bank">
        /// The instrument sounds of the clip.<br/>
        /// 클립의 악기 사운드입니다.
        /// </param>
        /// <param name="transport">
        /// The transport whose timeline the scheduler follows.<br/>
        /// 스케줄러가 따라갈 타임라인을 가진 트랜스포트입니다.
        /// </param>
        /// <param name="system">
        /// The sound system that plays the sounds, or <see langword="null"/> for <see cref="SoundSystem.main"/>.<br/>
        /// 소리를 재생할 사운드 시스템이며, <see langword="null"/>이면 <see cref="SoundSystem.main"/>입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="clip"/>, <paramref name="bank"/>, or <paramref name="transport"/> is <see langword="null"/>.<br/>
        /// <paramref name="clip"/>, <paramref name="bank"/>, <paramref name="transport"/> 중 하나가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public NBSVoiceScheduler(NoteBlockClip clip, NBSInstrumentBank bank, TTransport transport, SoundSystem? system = null)
        {
            ArgumentNullException.ThrowIfNull(clip);
            ArgumentNullException.ThrowIfNull(bank);
            ArgumentNullException.ThrowIfNull(transport);

            this.clip = clip;
            this.bank = bank;
            this.transport = transport;
            this.system = system ?? SoundSystem.main;

            timeline = new NBSTimeline(clip.map, bank);
            committedRate = ReadRate();

            NBSPlaybackWorker.Register(this);
        }

        readonly NoteBlockClip clip;
        readonly NBSInstrumentBank bank;
        readonly TTransport transport;
        readonly SoundSystem system;

        readonly Lock gate = new();
        readonly Lock voiceLock = new();
        readonly List<Voice> voices = [];
        readonly List<Voice> voiceSnapshot = [];
        readonly List<NBSPlannedVoice> planned = [];

        NBSPlaybackRate committedRate;
        double cursorClock;
        bool hasCursor;
        long generation;
        bool isDisposed;

        /// <summary>
        /// Gets the timeline that the scheduler currently places notes with.<br/>
        /// 스케줄러가 현재 노트를 배치하는 데 사용하는 타임라인을 가져옵니다.
        /// </summary>
        public NBSTimeline currentTimeline
        {
            get
            {
                lock (gate)
                    return timeline;
            }
        }
        NBSTimeline timeline;

        /// <summary>
        /// Gets or sets where reversed sounds are placed. Changing it reschedules the sounds that have not started.<br/>
        /// 리버스된 소리를 어디에 배치할지 가져오거나 설정합니다. 바꾸면 아직 시작하지 않은 소리를 다시 예약합니다.
        /// </summary>
        public NBSReversePitchAlignment reversePitchAlignment
        {
            get => _reversePitchAlignment;
            set
            {
                if (_reversePitchAlignment == value)
                    return;

                _reversePitchAlignment = value;
                OnRateChanged();
            }
        }
        volatile NBSReversePitchAlignment _reversePitchAlignment;

        /// <summary>
        /// Releases every sound and unregisters the scheduler from <see cref="NBSPlaybackWorker"/>.<br/>
        /// 모든 소리를 해제하고 <see cref="NBSPlaybackWorker"/>에서 스케줄러 등록을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            NBSPlaybackWorker.Unregister(this);

            lock (gate)
            {
                if (isDisposed)
                    return;

                isDisposed = true;
                generation++;
                StopAllVoicesUnsafe();
            }
        }

        NBSPlaybackRate ReadRate() => new NBSPlaybackRate(transport.tempo, transport.pitch, _reversePitchAlignment);

        NBSLoopRange? ReadLoop() => transport.loopRange;
    }
}
