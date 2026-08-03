#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Identifies a half-open range in a precomputed NBS map.<br/>
    /// 미리 계산된 NBS 맵의 반개구간을 식별합니다.
    /// </summary>
    public readonly record struct NBSMapRange(int startIndex, int endIndex)
    {
        /// <summary>Gets the number of entries in the range.<br/>범위에 포함된 항목 수를 가져옵니다.</summary>
        public int count => Math.Max(0, endIndex - startIndex);

        /// <summary>Gets whether the range contains no entries.<br/>범위에 항목이 없는지 여부를 가져옵니다.</summary>
        public bool isEmpty => count == 0;
    }

    /// <summary>
    /// Connects one raw NBS note to its stable identifier and absolute file time.<br/>
    /// 원본 NBS 음표 하나를 안정적인 식별자 및 절대 파일 시간과 연결합니다.
    /// </summary>
    public readonly record struct NBSMappedNote(int id, double time, NBSNote note);

    /// <summary>
    /// Connects one NBS special event to its stable identifier and absolute file time.<br/>
    /// NBS 특수 이벤트 하나를 안정적인 식별자 및 절대 파일 시간과 연결합니다.
    /// </summary>
    public readonly record struct NBSMappedSpecialEvent(int id, double time, NBSSpecialEvent specialEvent);

    /// <summary>
    /// Provides immutable, time-sorted note and special-event data computed from an NBS file.<br/>
    /// NBS 파일에서 계산된 불변 시간순 음표 및 특수 이벤트 데이터를 제공합니다.
    /// </summary>
    public sealed class NBSNoteMap
    {
        internal NBSNoteMap
        (
            IReadOnlyList<NBSTick> ticks,
            IReadOnlyList<NBSSpecialEvent> specialEvents,
            NBSTempoMap tempoMap
        )
        {
            List<NBSMappedNote> mappedNotes = [];
            int noteId = 0;
            for (int tickIndex = 0; tickIndex < ticks.Count; tickIndex++)
            {
                NBSTick tick = ticks[tickIndex];
                double time = tempoMap.TickToTime(tick.tick);
                for (int noteIndex = 0; noteIndex < tick.notes.Count; noteIndex++)
                    mappedNotes.Add(new NBSMappedNote(noteId++, time, tick.notes[noteIndex]));
            }

            mappedNotes.Sort(static (left, right) =>
            {
                int timeComparison = left.time.CompareTo(right.time);
                if (timeComparison != 0)
                    return timeComparison;

                int layerComparison = left.note.layer.CompareTo(right.note.layer);
                return layerComparison != 0 ? layerComparison : left.id.CompareTo(right.id);
            });

            List<NBSMappedSpecialEvent> mappedEvents = new List<NBSMappedSpecialEvent>(specialEvents.Count);
            for (int i = 0; i < specialEvents.Count; i++)
            {
                NBSSpecialEvent specialEvent = specialEvents[i];
                mappedEvents.Add(new NBSMappedSpecialEvent(i, tempoMap.TickToTime(specialEvent.tick), specialEvent));
            }

            mappedEvents.Sort(static (left, right) =>
            {
                int timeComparison = left.time.CompareTo(right.time);
                if (timeComparison != 0)
                    return timeComparison;

                int layerComparison = left.specialEvent.layer.CompareTo(right.specialEvent.layer);
                return layerComparison != 0 ? layerComparison : left.id.CompareTo(right.id);
            });

            notes = mappedNotes.AsReadOnly();
            this.specialEvents = mappedEvents.AsReadOnly();
        }

        /// <summary>Gets notes ordered by time, layer, and stable identifier.<br/>시간, 레이어, 안정적인 식별자순 음표를 가져옵니다.</summary>
        public IReadOnlyList<NBSMappedNote> notes { get; }

        /// <summary>Gets special events ordered by time, layer, and stable identifier.<br/>시간, 레이어, 안정적인 식별자순 특수 이벤트를 가져옵니다.</summary>
        public IReadOnlyList<NBSMappedSpecialEvent> specialEvents { get; }

        /// <summary>
        /// Finds the first note whose time is greater than or equal to <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/> 이상인 첫 음표를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or the note count when no note matches.<br/>일치하는 인덱스이며, 음표가 없으면 음표 수입니다.</returns>
        public int FindFirstNoteAtOrAfter(double time) => FindFirstNote(time, false);

        /// <summary>
        /// Finds the first note whose time is greater than <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/>보다 큰 첫 음표를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or the note count when no note matches.<br/>일치하는 인덱스이며, 음표가 없으면 음표 수입니다.</returns>
        public int FindFirstNoteAfter(double time) => FindFirstNote(time, true);

        /// <summary>
        /// Finds the last note whose time is less than or equal to <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/> 이하인 마지막 음표를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or <c>-1</c> when no note matches.<br/>일치하는 인덱스이며, 음표가 없으면 <c>-1</c>입니다.</returns>
        public int FindLastNoteAtOrBefore(double time) => FindFirstNote(time, true) - 1;

        /// <summary>
        /// Finds the last note whose time is less than <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/>보다 작은 마지막 음표를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or <c>-1</c> when no note matches.<br/>일치하는 인덱스이며, 음표가 없으면 <c>-1</c>입니다.</returns>
        public int FindLastNoteBefore(double time) => FindFirstNote(time, false) - 1;

        /// <summary>
        /// Gets the half-open note index range between two times.<br/>
        /// 두 시간 사이의 음표 인덱스 반개구간을 가져옵니다.
        /// </summary>
        /// <param name="startTime">The lower time boundary.<br/>아래쪽 시간 경계입니다.</param>
        /// <param name="endTime">The upper time boundary.<br/>위쪽 시간 경계입니다.</param>
        /// <param name="includeStart">Whether notes at <paramref name="startTime"/> are included.<br/><paramref name="startTime"/>의 음표를 포함할지 여부입니다.</param>
        /// <param name="includeEnd">Whether notes at <paramref name="endTime"/> are included.<br/><paramref name="endTime"/>의 음표를 포함할지 여부입니다.</param>
        /// <returns>The matching half-open index range.<br/>일치하는 인덱스 반개구간입니다.</returns>
        public NBSMapRange GetNoteRange(double startTime, double endTime, bool includeStart = true, bool includeEnd = true)
        {
            if (endTime < startTime)
                return default;

            int startIndex = FindFirstNote(startTime, !includeStart);
            int endIndex = FindFirstNote(endTime, includeEnd);
            return new NBSMapRange(startIndex, Math.Max(startIndex, endIndex));
        }

        /// <summary>
        /// Finds the first special event whose time is greater than or equal to <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/> 이상인 첫 특수 이벤트를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or the event count when no event matches.<br/>일치하는 인덱스이며, 이벤트가 없으면 이벤트 수입니다.</returns>
        public int FindFirstSpecialEventAtOrAfter(double time) => FindFirstSpecialEvent(time, false);

        /// <summary>
        /// Finds the first special event whose time is greater than <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/>보다 큰 첫 특수 이벤트를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or the event count when no event matches.<br/>일치하는 인덱스이며, 이벤트가 없으면 이벤트 수입니다.</returns>
        public int FindFirstSpecialEventAfter(double time) => FindFirstSpecialEvent(time, true);

        /// <summary>
        /// Finds the last special event whose time is less than or equal to <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/> 이하인 마지막 특수 이벤트를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or <c>-1</c> when no event matches.<br/>일치하는 인덱스이며, 이벤트가 없으면 <c>-1</c>입니다.</returns>
        public int FindLastSpecialEventAtOrBefore(double time) => FindFirstSpecialEvent(time, true) - 1;

        /// <summary>
        /// Finds the last special event whose time is less than <paramref name="time"/>.<br/>
        /// 시간이 <paramref name="time"/>보다 작은 마지막 특수 이벤트를 찾습니다.
        /// </summary>
        /// <returns>The matching index, or <c>-1</c> when no event matches.<br/>일치하는 인덱스이며, 이벤트가 없으면 <c>-1</c>입니다.</returns>
        public int FindLastSpecialEventBefore(double time) => FindFirstSpecialEvent(time, false) - 1;

        /// <summary>
        /// Gets the half-open special-event index range between two times.<br/>
        /// 두 시간 사이의 특수 이벤트 인덱스 반개구간을 가져옵니다.
        /// </summary>
        /// <param name="startTime">The lower time boundary.<br/>아래쪽 시간 경계입니다.</param>
        /// <param name="endTime">The upper time boundary.<br/>위쪽 시간 경계입니다.</param>
        /// <param name="includeStart">Whether events at <paramref name="startTime"/> are included.<br/><paramref name="startTime"/>의 이벤트를 포함할지 여부입니다.</param>
        /// <param name="includeEnd">Whether events at <paramref name="endTime"/> are included.<br/><paramref name="endTime"/>의 이벤트를 포함할지 여부입니다.</param>
        /// <returns>The matching half-open index range.<br/>일치하는 인덱스 반개구간입니다.</returns>
        public NBSMapRange GetSpecialEventRange(double startTime, double endTime, bool includeStart = true, bool includeEnd = true)
        {
            if (endTime < startTime)
                return default;

            int startIndex = FindFirstSpecialEvent(startTime, !includeStart);
            int endIndex = FindFirstSpecialEvent(endTime, includeEnd);
            return new NBSMapRange(startIndex, Math.Max(startIndex, endIndex));
        }

        int FindFirstNote(double time, bool strictlyAfter)
        {
            int low = 0;
            int high = notes.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool before = strictlyAfter ? notes[middle].time <= time : notes[middle].time < time;
                if (before)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        int FindFirstSpecialEvent(double time, bool strictlyAfter)
        {
            int low = 0;
            int high = specialEvents.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool before = strictlyAfter ? specialEvents[middle].time <= time : specialEvents[middle].time < time;
                if (before)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }
    }
}
