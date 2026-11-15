#nullable enable
namespace RuniOS.NBS
{
    public sealed partial class NBSMap
    {
        /// <summary>
        /// Gets the special behavior of an instrument index.<br/>
        /// 악기 인덱스의 특수 동작을 가져옵니다.
        /// </summary>
        /// <param name="instrument">
        /// The instrument index of a note.<br/>
        /// 노트의 악기 인덱스입니다.
        /// </param>
        /// <returns>
        /// The function of the custom instrument; <see cref="NBSInstrumentFunction.none"/> for built-in or missing instruments.<br/>
        /// 커스텀 악기의 기능을 반환하며, 기본 악기나 없는 악기이면 <see cref="NBSInstrumentFunction.none"/>을 반환합니다.
        /// </returns>
        public NBSInstrumentFunction GetFunction(int instrument)
        {
            int customIndex = instrument - NBSInstrument.vanillaCount;
            return (uint)customIndex < (uint)functions.Length ? functions[customIndex] : NBSInstrumentFunction.none;
        }

        /// <summary>
        /// Tries to get the index in <see cref="instruments"/> that an instrument index refers to.<br/>
        /// 악기 인덱스가 가리키는 <see cref="instruments"/>의 인덱스를 가져오려고 시도합니다.
        /// </summary>
        /// <param name="instrument">
        /// The instrument index of a note.<br/>
        /// 노트의 악기 인덱스입니다.
        /// </param>
        /// <param name="customIndex">
        /// The index in <see cref="instruments"/> when successful; otherwise, <c>-1</c>.<br/>
        /// 성공하면 <see cref="instruments"/>의 인덱스이고, 그렇지 않으면 <c>-1</c>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the index refers to an existing custom instrument; <see langword="false"/> for built-in or missing instruments.<br/>
        /// 인덱스가 존재하는 커스텀 악기를 가리키면 <see langword="true"/>를 반환하고, 기본 악기나 없는 악기이면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool TryGetCustomInstrumentIndex(int instrument, out int customIndex)
        {
            customIndex = instrument - NBSInstrument.vanillaCount;
            if ((uint)customIndex < (uint)instruments.Length)
                return true;

            customIndex = -1;
            return false;
        }

        /// <summary>
        /// Tries to get the row of a layer.<br/>
        /// 레이어의 행을 가져오려고 시도합니다.
        /// </summary>
        /// <param name="layer">
        /// The zero-based layer, which is also the index in <see cref="layers"/>.<br/>
        /// 0부터 시작하는 레이어이며, <see cref="layers"/>의 인덱스이기도 합니다.
        /// </param>
        /// <param name="row">
        /// The layer row when it exists; otherwise, <see langword="default"/>.<br/>
        /// 행이 있으면 레이어 행이고, 그렇지 않으면 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the layer has a row; otherwise, <see langword="false"/>.<br/>
        /// 레이어에 행이 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool TryGetLayer(int layer, out NBSLayer row)
        {
            if ((uint)layer < (uint)layers.Length)
            {
                row = layers[layer];
                return true;
            }

            row = default;
            return false;
        }

        /// <summary>
        /// Determines whether notes on a layer play under the lock and solo rules of Note Block Studio.<br/>
        /// Note Block Studio의 잠금과 솔로 규칙에서 레이어의 노트가 재생되는지 확인합니다.
        /// </summary>
        /// <param name="layer">
        /// The zero-based layer.<br/>
        /// 0부터 시작하는 레이어입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the layer plays; otherwise, <see langword="false"/>.<br/>
        /// 레이어가 재생되면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <remarks>
        /// When any layer is soloed, only soloed layers play. Otherwise every layer except locked ones plays. Layers without a row play unless a solo exists.<br/>
        /// 솔로 레이어가 하나라도 있으면 솔로 레이어만 재생합니다. 그렇지 않으면 잠긴 레이어를 제외한 모든 레이어가 재생합니다. 행이 없는 레이어는 솔로가 없을 때 재생합니다.
        /// </remarks>
        public bool IsLayerAudible(int layer)
        {
            bool hasRow = TryGetLayer(layer, out NBSLayer row);
            if (hasSolo)
                return hasRow && row.lockState == NBSLayerLock.solo;

            return !hasRow || row.lockState != NBSLayerLock.locked;
        }

        /// <summary>
        /// Gets the volume, stereo position, and fine pitch that Note Block Studio uses for a note.<br/>
        /// Note Block Studio가 노트에 사용하는 음량, 스테레오 위치, 미세 피치를 가져옵니다.
        /// </summary>
        /// <param name="note">
        /// The note to evaluate.<br/>
        /// 계산할 노트입니다.
        /// </param>
        /// <returns>
        /// The combined values of the note and its layer.<br/>
        /// 노트와 레이어를 합친 값을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Like Note Block Studio, a note on a layer without a row uses a volume of <c>1</c>% hard left with no fine pitch.<br/>
        /// Note Block Studio와 같이 행이 없는 레이어의 노트는 음량 <c>1</c>%, 완전 왼쪽, 미세 피치 없음으로 재생됩니다.
        /// </remarks>
        public NBSNoteMix GetMix(in NBSNote note)
        {
            if (!TryGetLayer(note.layer, out NBSLayer row))
                return new NBSNoteMix(0.01f, -1, 0);

            float volume = (row.volume / 100f) * (note.velocity / 100f);
            float panning = row.panning == 100 ? note.panning : (row.panning + note.panning) * 0.5f;
            return new NBSNoteMix(volume, (panning - 100) / 100f, note.pitch);
        }

        /// <summary>
        /// Tries to get the position in <see cref="notes"/> of the note that was at a source index when the map was created.<br/>
        /// 맵을 만들 때 원본 인덱스에 있던 노트의 <see cref="notes"/> 위치를 가져오려고 시도합니다.
        /// </summary>
        /// <param name="sourceIndex">
        /// The index in the note list the map was created from.<br/>
        /// 맵을 만든 노트 리스트의 인덱스입니다.
        /// </param>
        /// <param name="mapIndex">
        /// The index in <see cref="notes"/> when successful; otherwise, <c>-1</c>.<br/>
        /// 성공하면 <see cref="notes"/>의 인덱스이고, 그렇지 않으면 <c>-1</c>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="sourceIndex"/> is in range; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="sourceIndex"/>가 범위 안이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <remarks>
        /// Source indexes refer to the list as it was when the map was created. Adding or removing notes afterwards shifts them until the map is created again.<br/>
        /// 원본 인덱스는 맵을 만들 때의 리스트를 기준으로 합니다. 이후 노트를 추가하거나 제거하면 맵을 다시 만들 때까지 어긋납니다.
        /// </remarks>
        public bool TryGetMapIndex(int sourceIndex, out int mapIndex)
        {
            if ((uint)sourceIndex < (uint)sourceToMap.Length)
            {
                mapIndex = sourceToMap[sourceIndex];
                return true;
            }

            mapIndex = -1;
            return false;
        }

        /// <summary>
        /// Finds the index of the first column whose tick is greater than or equal to a tick position.<br/>
        /// tick이 지정한 tick 위치 이상인 첫 열의 인덱스를 찾습니다.
        /// </summary>
        /// <param name="tick">
        /// The tick position.<br/>
        /// tick 위치입니다.
        /// </param>
        /// <returns>
        /// The column index, or the column count when every column is before <paramref name="tick"/>.<br/>
        /// 열 인덱스를 반환하며, 모든 열이 <paramref name="tick"/> 이전이면 열 개수를 반환합니다.
        /// </returns>
        public int FindColumnAtOrAfter(double tick)
        {
            ReadOnlySpan<NBSMapColumn> span = columns.AsSpan();
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle].tick < tick)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        /// <summary>
        /// Tries to get the column at a tick.<br/>
        /// tick의 열을 가져오려고 시도합니다.
        /// </summary>
        /// <param name="tick">
        /// The tick of the column.<br/>
        /// 열의 tick입니다.
        /// </param>
        /// <param name="column">
        /// The column when it exists; otherwise, <see langword="default"/>.<br/>
        /// 열이 있으면 해당 열이고, 그렇지 않으면 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the tick has notes; otherwise, <see langword="false"/>.<br/>
        /// tick에 노트가 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool TryGetColumn(int tick, out NBSMapColumn column)
        {
            int index = FindColumnAtOrAfter(tick);
            if (index < columns.Length && columns[index].tick == tick)
            {
                column = columns[index];
                return true;
            }

            column = default;
            return false;
        }

        /// <summary>
        /// Gets the notes at a tick in ascending layer order.<br/>
        /// tick에 있는 노트를 레이어 오름차순으로 가져옵니다.
        /// </summary>
        /// <param name="tick">
        /// The tick to query.<br/>
        /// 조회할 tick입니다.
        /// </param>
        /// <returns>
        /// The notes at <paramref name="tick"/>, which is empty when the tick has none.<br/>
        /// <paramref name="tick"/>에 있는 노트를 반환하며, 없으면 비어 있습니다.
        /// </returns>
        public ReadOnlySpan<NBSMapNote> GetTickNotes(int tick) =>
            TryGetColumn(tick, out NBSMapColumn column) ? notes.AsSpan().Slice(column.noteStart, column.noteCount) : default;

        /// <summary>
        /// Gets the notes inside a tick and layer rectangle, ordered by tick and then by layer.<br/>
        /// tick과 레이어 사각형 안의 노트를 tick, 레이어 순으로 가져옵니다.
        /// </summary>
        /// <param name="startTick">
        /// The first tick, inclusive.<br/>
        /// 포함되는 첫 tick입니다.
        /// </param>
        /// <param name="endTick">
        /// The last tick, exclusive.<br/>
        /// 포함되지 않는 마지막 tick입니다.
        /// </param>
        /// <param name="startLayer">
        /// The first layer, inclusive.<br/>
        /// 포함되는 첫 레이어입니다.
        /// </param>
        /// <param name="endLayer">
        /// The last layer, exclusive.<br/>
        /// 포함되지 않는 마지막 레이어입니다.
        /// </param>
        /// <returns>
        /// An allocation-free enumerator over the matching notes.<br/>
        /// 일치하는 노트를 할당 없이 열거하는 열거자를 반환합니다.
        /// </returns>
        public GridEnumerator GetNotes(int startTick, int endTick, int startLayer, int endLayer) => new GridEnumerator(this, startTick, endTick, startLayer, endLayer);

        /// <summary>
        /// Gets the notes of a layer in ascending tick order.<br/>
        /// 레이어의 노트를 tick 오름차순으로 가져옵니다.
        /// </summary>
        /// <param name="layer">
        /// The zero-based layer.<br/>
        /// 0부터 시작하는 레이어입니다.
        /// </param>
        /// <returns>
        /// An allocation-free enumerator over the notes of <paramref name="layer"/>.<br/>
        /// <paramref name="layer"/>의 노트를 할당 없이 열거하는 열거자를 반환합니다.
        /// </returns>
        public LayerEnumerator GetLayerNotes(int layer)
        {
            if (layer < 0 || layer >= layerStarts.Length - 1)
                return default;

            int start = layerStarts[layer];
            return new LayerEnumerator(notes.AsSpan(), layerNotes.AsSpan(start, layerStarts[layer + 1] - start));
        }

        /// <summary>
        /// Finds the index of the first note whose song time is greater than or equal to a time.<br/>
        /// 곡 시간이 지정한 시간 이상인 첫 노트의 인덱스를 찾습니다.
        /// </summary>
        /// <param name="time">
        /// The song time in seconds.<br/>
        /// 초 단위 곡 시간입니다.
        /// </param>
        /// <returns>
        /// The note index, or the note count when every note is earlier.<br/>
        /// 노트 인덱스를 반환하며, 모든 노트가 더 이르면 노트 개수를 반환합니다.
        /// </returns>
        public int FindNoteAtOrAfter(double time)
        {
            ReadOnlySpan<NBSMapNote> span = notes.AsSpan();
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle].time < time)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        /// <summary>
        /// Finds the song time of the first sound stopper at or after a time that stops a layer.<br/>
        /// 지정한 시간 이후에 레이어를 멈추는 첫 Sound Stopper의 곡 시간을 찾습니다.
        /// </summary>
        /// <param name="layer">
        /// The zero-based layer.<br/>
        /// 0부터 시작하는 레이어입니다.
        /// </param>
        /// <param name="time">
        /// The song time in seconds where the search starts, inclusive.<br/>
        /// 검색을 시작하는 초 단위 곡 시간이며 포함됩니다.
        /// </param>
        /// <returns>
        /// The song time of the stopper, or <see cref="double.PositiveInfinity"/> when there is none.<br/>
        /// Stopper의 곡 시간을 반환하며, 없으면 <see cref="double.PositiveInfinity"/>를 반환합니다.
        /// </returns>
        public double FindStopTimeAtOrAfter(int layer, double time)
        {
            ReadOnlySpan<NBSSoundStopper> span = stoppers.AsSpan();
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle].time < time)
                    low = middle + 1;
                else
                    high = middle;
            }

            for (int i = low; i < span.Length; i++)
            {
                if (span[i].Targets(layer))
                    return span[i].time;
            }

            return double.PositiveInfinity;
        }
    }
}
