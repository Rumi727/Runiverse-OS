#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RuniOS.NBS
{
    /// <summary>
    /// Provides an immutable, time-mapped view of an NBS song for playback, editors, and visualization.<br/>
    /// 재생, 에디터, 시각화를 위해 NBS 곡을 시간에 맞춰 배치한 불변 뷰를 제공합니다.
    /// </summary>
    /// <remarks>
    /// The map copies its input, so later changes to the source lists do not affect it. It can be read from any thread.<br/>
    /// Notes are ordered by tick and then by layer. Notes at the same tick and layer keep no particular order.
    /// <br/><br/>
    /// 맵은 입력을 복사하므로 이후 원본 리스트를 바꿔도 영향을 받지 않습니다. 어느 스레드에서든 읽을 수 있습니다.<br/>
    /// 노트는 tick, 레이어 순으로 정렬됩니다. tick과 레이어가 같은 노트 사이의 순서는 정해져 있지 않습니다.
    /// </remarks>
    public sealed partial class NBSMap
    {
        /// <summary>
        /// Initializes a map from the parts of an NBS song.<br/>
        /// NBS 곡의 구성 요소로 맵을 초기화합니다.
        /// </summary>
        /// <param name="header">
        /// The song header.<br/>
        /// 곡 헤더입니다.
        /// </param>
        /// <param name="notes">
        /// The notes in any order.<br/>
        /// 순서와 관계없는 노트입니다.
        /// </param>
        /// <param name="layers">
        /// The layer rows.<br/>
        /// 레이어 행입니다.
        /// </param>
        /// <param name="instruments">
        /// The custom instruments.<br/>
        /// 커스텀 악기입니다.
        /// </param>
        public NBSMap(NBSHeader header, ReadOnlySpan<NBSNote> notes, ReadOnlySpan<NBSLayer> layers, ReadOnlySpan<NBSInstrument> instruments)
        {
            this.header = header;
            this.layers = ImmutableArray.Create(layers);
            this.instruments = ImmutableArray.Create(instruments);

            functions = new NBSInstrumentFunction[instruments.Length];
            for (int i = 0; i < instruments.Length; i++)
                functions[i] = instruments[i].function;

            for (int i = 0; i < layers.Length; i++)
                hasSolo |= layers[i].lockState == NBSLayerLock.solo;

            NBSMapNote[] sorted = new NBSMapNote[notes.Length];
            for (int i = 0; i < notes.Length; i++)
                sorted[i] = new NBSMapNote(notes[i], i, 0, double.PositiveInfinity);

            SortByTickAndLayer(sorted);

            tempo = CreateTempoMap(header, sorted);
            columns = AssignTimes(sorted, out ImmutableArray<NBSSoundStopper> soundStoppers);
            stoppers = soundStoppers;
            AssignNextStopTimes(sorted, soundStoppers);

            sourceToMap = new int[sorted.Length];
            for (int i = 0; i < sorted.Length; i++)
                sourceToMap[sorted[i].sourceIndex] = i;

            CreateLayerIndex(sorted, out layerStarts, out layerNotes);

            tickLength = sorted.Length > 0 ? sorted[^1].tick + 1 : 0;
            length = tempo.TickToTime(tickLength);

            this.notes = new NoteCollection(this, ImmutableCollectionsMarshal.AsImmutableArray(sorted));
            visualEffects = new NBSVisualEffectMap(sorted, instruments);
        }

        readonly NBSInstrumentFunction[] functions;
        readonly bool hasSolo;
        readonly int[] sourceToMap;
        readonly int[] layerStarts;
        readonly int[] layerNotes;

        /// <summary>
        /// Gets the song header.<br/>
        /// 곡 헤더를 가져옵니다.
        /// </summary>
        public NBSHeader header { get; }

        /// <summary>
        /// Gets the layer rows.<br/>
        /// 레이어 행을 가져옵니다.
        /// </summary>
        public ImmutableArray<NBSLayer> layers { get; }

        /// <summary>
        /// Gets the custom instruments.<br/>
        /// 커스텀 악기를 가져옵니다.
        /// </summary>
        public ImmutableArray<NBSInstrument> instruments { get; }

        /// <summary>
        /// Gets the notes sorted by tick and then by layer, with their song times.<br/>
        /// tick, 레이어 순으로 정렬되고 곡 시간이 붙은 노트를 가져옵니다.
        /// </summary>
        public NoteCollection notes { get; }

        /// <summary>
        /// Gets the tick columns that contain notes, in ascending tick order.<br/>
        /// 노트가 있는 tick 열을 tick 오름차순으로 가져옵니다.
        /// </summary>
        public ImmutableArray<NBSMapColumn> columns { get; }

        /// <summary>
        /// Gets the sound stoppers in ascending tick and layer order.<br/>
        /// Sound Stopper를 tick, 레이어 오름차순으로 가져옵니다.
        /// </summary>
        public ImmutableArray<NBSSoundStopper> stoppers { get; }

        /// <summary>
        /// Gets the tempo map built from the header tempo and tempo changers.<br/>
        /// 헤더 템포와 Tempo Changer로 만든 템포 맵을 가져옵니다.
        /// </summary>
        public NBSTempoMap tempo { get; }

        /// <summary>
        /// Gets the visual effect map.<br/>
        /// 시각 효과 맵을 가져옵니다.
        /// </summary>
        public NBSVisualEffectMap visualEffects { get; }

        /// <summary>
        /// Gets the song length in ticks, which is the last note tick plus one, or <c>0</c> without notes.<br/>
        /// 곡 길이를 tick 단위로 가져옵니다. 마지막 노트 tick에 1을 더한 값이며, 노트가 없으면 <c>0</c>입니다.
        /// </summary>
        public int tickLength { get; }

        /// <summary>
        /// Gets the song length in seconds at <see cref="tickLength"/>.<br/>
        /// <see cref="tickLength"/> 위치의 곡 길이를 초 단위로 가져옵니다.
        /// </summary>
        public double length { get; }
    }
}
