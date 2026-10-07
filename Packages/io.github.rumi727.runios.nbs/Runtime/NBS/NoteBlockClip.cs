using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Represents an immutable parsed NBS file and its precomputed timing data.<br/>
    /// 파싱된 불변 NBS 파일과 미리 계산된 타이밍 데이터를 나타냅니다.
    /// </summary>
    public sealed class NoteBlockClip : RuniAudioClip
    {
        internal NoteBlockClip
        (
            NBSHeader header,
            IReadOnlyList<NBSTick> ticks,
            IReadOnlyList<NBSLayer> layers,
            IReadOnlyList<NBSCustomInstrument> customInstruments,
            IReadOnlyList<NBSSpecialEvent> specialEvents,
            int tickLength
        )
        {
            this.header = header;
            this.ticks = ticks;
            this.layers = layers;
            this.customInstruments = customInstruments;
            this.specialEvents = specialEvents;
            this.tickLength = tickLength;

            tempoMap = new NBSTempoMap(header.ticksPerSecond, specialEvents);
            noteMap = new NBSNoteMap(ticks, specialEvents, tempoMap);
            playbackMap = new NBSPlaybackMap(header, layers, customInstruments, noteMap);
            visualEffectMap = new NBSVisualEffectMap(specialEvents);
        }

        /// <summary>Gets the complete file header.<br/>파일 헤더 전체를 가져옵니다.</summary>
        public NBSHeader header { get; }

        /// <summary>Gets active tick columns in ascending order.<br/>활성 틱 열을 오름차순으로 가져옵니다.</summary>
        public IReadOnlyList<NBSTick> ticks { get; }

        /// <summary>Gets layer metadata.<br/>레이어 메타데이터를 가져옵니다.</summary>
        public IReadOnlyList<NBSLayer> layers { get; }

        /// <summary>Gets custom instrument definitions.<br/>커스텀 악기 정의를 가져옵니다.</summary>
        public IReadOnlyList<NBSCustomInstrument> customInstruments { get; }

        /// <summary>Gets every recognized functional event, including editor-only events.<br/>에디터 전용 이벤트를 포함한 인식된 모든 기능성 이벤트를 가져옵니다.</summary>
        public IReadOnlyList<NBSSpecialEvent> specialEvents { get; }

        /// <summary>Gets the tempo map used by transport and scheduling.<br/>트랜스포트와 예약에 사용하는 템포 맵을 가져옵니다.</summary>
        public NBSTempoMap tempoMap { get; }

        /// <summary>Gets the immutable absolute-time note and special-event map.<br/>불변 절대 시간 음표 및 특수 이벤트 맵을 가져옵니다.</summary>
        public NBSNoteMap noteMap { get; }

        /// <summary>Gets the immutable clip-independent playback map.<br/>불변 클립 독립적 재생 맵을 가져옵니다.</summary>
        public NBSPlaybackMap playbackMap { get; }

        /// <summary>
        /// Gets the visual-effect map used by editor previews.<br/>
        /// 에디터 미리보기에 사용하는 시각 효과 맵을 가져옵니다.
        /// </summary>
        public NBSVisualEffectMap visualEffectMap { get; }

        /// <summary>Gets the logical song length in NBS ticks.<br/>NBS 틱 단위의 논리적 곡 길이를 가져옵니다.</summary>
        public int tickLength { get; }

        /// <summary>Gets the song duration after all tempo changes are applied.<br/>모든 템포 변경을 적용한 곡 길이를 가져옵니다.</summary>
        public override double length => tempoMap.TickToTime(tickLength);
    }
}