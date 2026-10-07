#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the complete Note Block Studio header, including editor-only metadata.<br/>
    /// 에디터 전용 메타데이터를 포함한 Note Block Studio 헤더 전체를 저장합니다.
    /// </summary>
    public readonly record struct NBSHeader
    (
        byte version,
        byte vanillaInstrumentCount,
        ushort declaredSongLength,
        ushort layerCount,
        string songName,
        string author,
        string originalAuthor,
        string description,
        double ticksPerSecond,
        bool autoSave,
        byte autoSaveDuration,
        byte timeSignature,
        int minutesSpent,
        int leftClicks,
        int rightClicks,
        int blocksAdded,
        int blocksRemoved,
        string importedFileName,
        bool loopEnabled,
        byte maxLoopCount,
        ushort loopStartTick
    );

    /// <summary>
    /// Stores a note and its absolute tick/layer coordinates.<br/>
    /// 음표와 절대 틱/레이어 좌표를 저장합니다.
    /// </summary>
    public readonly record struct NBSNote(int tick, int layer, byte instrument, byte key, byte velocity, byte panning, short pitch);

    /// <summary>
    /// Stores one NBS layer definition.<br/>
    /// NBS 레이어 정의 하나를 저장합니다.
    /// </summary>
    public readonly record struct NBSLayer(string name, bool locked, byte volume, byte panning);

    /// <summary>
    /// Stores one custom instrument definition, including its source file name.<br/>
    /// 원본 파일 이름을 포함한 커스텀 악기 정의 하나를 저장합니다.
    /// </summary>
    public readonly record struct NBSCustomInstrument(string name, string soundFile, byte key, bool pressKey)
    {
        public bool IsFunctionalInstrument()
        {
            string name = this.name.Trim();
            return name.Equals("Tempo Changer", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Sound Stopper", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Toggle Rainbow", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Show Save Popup", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Toggle Background Accent", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Change Color to #", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Groups every note located at the same active tick column.<br/>
    /// 같은 활성 틱 열에 위치한 모든 음표를 묶습니다.
    /// </summary>
    public readonly struct NBSTick(int tick, IReadOnlyList<NBSNote> notes)
    {
        /// <summary>
        /// Gets the absolute NBS tick number.<br/>
        /// 절대 NBS 틱 번호를 가져옵니다.
        /// </summary>
        public int tick { get; } = tick;

        /// <summary>
        /// Gets notes ordered from the lowest layer to the highest layer.<br/>
        /// 낮은 레이어부터 높은 레이어 순서로 정렬된 음표를 가져옵니다.
        /// </summary>
        public IReadOnlyList<NBSNote> notes => _notes ?? [];
        readonly IReadOnlyList<NBSNote>? _notes = notes;
    }

    /// <summary>
    /// Identifies functional custom-instrument events emitted by modern NBS editors.<br/>
    /// 최신 NBS 에디터가 내보내는 기능성 커스텀 악기 이벤트를 식별합니다.
    /// </summary>
    public enum NBSSpecialEventKind
    {
        tempoChange,
        soundStop,
        toggleRainbow,
        showSavePopup,
        toggleBackgroundAccent,
        changeMainColor
    }

    /// <summary>
    /// Stores a parsed functional custom-instrument event.<br/>
    /// 파싱된 기능성 커스텀 악기 이벤트를 저장합니다.
    /// </summary>
    public record struct NBSSpecialEvent
    (
        NBSSpecialEventKind kind,
        int tick,
        int layer,
        double tempoBpm = 0,
        int startLayer = 0,
        int endLayer = int.MaxValue,
        HexColor color = default
    );
}
