#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores every field of the NBS file header.<br/>
    /// NBS 파일 헤더의 모든 필드를 저장합니다.
    /// </summary>
    /// <remarks>
    /// Values keep the meaning of the file format. The reader applies only the corrections that the Note Block Studio loader applies.<br/>
    /// 값은 파일 포맷의 의미를 그대로 유지합니다. 리더는 Note Block Studio 로더가 적용하는 보정만 적용합니다.
    /// </remarks>
    [Serializable]
    public record struct NBSHeader
    {
        /// <summary>
        /// Gets or sets the format version. <c>0</c> is the classic format.<br/>
        /// 포맷 버전을 가져오거나 설정합니다. <c>0</c>은 클래식 포맷입니다.
        /// </summary>
        [field: SerializeField] public byte version { get; set; }

        /// <summary>
        /// Gets or sets the number of built-in instruments when the file was saved. It is <c>0</c> for the classic format.<br/>
        /// 파일을 저장할 때의 기본 악기 수를 가져오거나 설정합니다. 클래식 포맷에서는 <c>0</c>입니다.
        /// </summary>
        [field: SerializeField] public byte vanillaInstrumentCount { get; set; }

        /// <summary>
        /// Gets or sets the declared song length in ticks. Note Block Studio does not use it to build the song.<br/>
        /// 선언된 곡 길이를 tick 단위로 가져오거나 설정합니다. Note Block Studio는 곡을 구성할 때 이 값을 사용하지 않습니다.
        /// </summary>
        [field: SerializeField] public short songLength { get; set; }

        /// <summary>
        /// Gets or sets the declared layer count.<br/>
        /// 선언된 레이어 수를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public short layerCount { get; set; }

        /// <summary>
        /// Gets or sets the song name.<br/>
        /// 곡 이름을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public string name { get; set; }

        /// <summary>
        /// Gets or sets the song author.<br/>
        /// 곡 작성자를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public string author { get; set; }

        /// <summary>
        /// Gets or sets the original song author.<br/>
        /// 원곡 작성자를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public string originalAuthor { get; set; }

        /// <summary>
        /// Gets or sets the song description.<br/>
        /// 곡 설명을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public string description { get; set; }

        /// <summary>
        /// Gets or sets the stored tempo, which is ticks per second multiplied by <c>100</c>.<br/>
        /// 저장된 템포를 가져오거나 설정합니다. 초당 tick 수에 <c>100</c>을 곱한 값입니다.
        /// </summary>
        [field: SerializeField] public short rawTempo { get; set; }

        /// <summary>
        /// Gets or sets the tempo in ticks per second through <see cref="rawTempo"/>.<br/>
        /// <see cref="rawTempo"/>를 통해 초당 tick 수 단위의 템포를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// The setter rounds to the nearest <c>0.01</c> and saturates to the range of <see cref="short"/>.<br/>
        /// setter는 가장 가까운 <c>0.01</c> 단위로 반올림하고 <see cref="short"/> 범위로 포화합니다.
        /// </remarks>
        public double ticksPerSecond
        {
            readonly get => rawTempo / 100d;
            set => rawTempo = (short)Math.Clamp(Math.Round(value * 100), short.MinValue, short.MaxValue);
        }

        /// <summary>
        /// Gets or sets the deprecated auto-save flag.<br/>
        /// 더 이상 사용되지 않는 자동 저장 플래그를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public bool autoSave { get; set; }

        /// <summary>
        /// Gets or sets the deprecated auto-save interval in minutes.<br/>
        /// 더 이상 사용되지 않는 자동 저장 간격을 분 단위로 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public byte autoSaveDuration { get; set; }

        /// <summary>
        /// Gets or sets the time signature numerator.<br/>
        /// 박자표 분자를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public byte timeSignature { get; set; }

        /// <summary>
        /// Gets or sets the minutes spent on the project.<br/>
        /// 프로젝트에 사용한 시간을 분 단위로 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public int minutesSpent { get; set; }

        /// <summary>
        /// Gets or sets the left-click count.<br/>
        /// 왼쪽 클릭 수를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public int leftClicks { get; set; }

        /// <summary>
        /// Gets or sets the right-click count.<br/>
        /// 오른쪽 클릭 수를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public int rightClicks { get; set; }

        /// <summary>
        /// Gets or sets the number of note blocks added.<br/>
        /// 추가한 노트 블록 수를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public int blocksAdded { get; set; }

        /// <summary>
        /// Gets or sets the number of note blocks removed.<br/>
        /// 제거한 노트 블록 수를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public int blocksRemoved { get; set; }

        /// <summary>
        /// Gets or sets the name of the MIDI or schematic file the song was imported from.<br/>
        /// 곡을 가져온 MIDI 또는 schematic 파일의 이름을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public string importedFileName { get; set; }

        /// <summary>
        /// Gets or sets whether the song loops.<br/>
        /// 곡을 반복할지 여부를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public bool loop { get; set; }

        /// <summary>
        /// Gets or sets the stored maximum loop count. <c>0</c> means infinite.<br/>
        /// 저장된 최대 반복 횟수를 가져오거나 설정합니다. <c>0</c>은 무한을 의미합니다.
        /// </summary>
        /// <remarks>
        /// The value is preserved but does not affect <see cref="NoteBlockSource"/> playback.<br/>
        /// 값은 보존되지만 <see cref="NoteBlockSource"/> 재생에는 영향을 주지 않습니다.
        /// </remarks>
        [field: SerializeField] public byte maxLoopCount { get; set; }

        /// <summary>
        /// Gets or sets the tick the song loops back to.<br/>
        /// 곡이 반복될 때 돌아가는 tick을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public short loopStartTick { get; set; }

        /// <summary>
        /// Initializes an empty header with the latest format version and the default tempo of 10 ticks per second.<br/>
        /// 최신 포맷 버전과 기본 템포인 초당 10 tick으로 빈 헤더를 초기화합니다.
        /// </summary>
        public NBSHeader()
        {
            version = NBSReader.latestVersion;
            vanillaInstrumentCount = NBSInstrument.vanillaCount;
            name = string.Empty;
            author = string.Empty;
            originalAuthor = string.Empty;
            description = string.Empty;
            rawTempo = 1000;
            timeSignature = 4;
            importedFileName = string.Empty;
        }
    }
}
