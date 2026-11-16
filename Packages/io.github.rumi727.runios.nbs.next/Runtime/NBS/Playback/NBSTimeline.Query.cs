#nullable enable
namespace RuniOS.NBS
{
    public sealed partial class NBSTimeline
    {
        /// <summary>
        /// Finds the sounds that start within a range of travel distance from a position.<br/>
        /// 위치에서 진행 거리 범위 안에 시작하는 소리를 찾습니다.
        /// </summary>
        /// <param name="position">
        /// The current song time in seconds.<br/>
        /// 초 단위 현재 곡 시간입니다.
        /// </param>
        /// <param name="rate">
        /// The playback rate. Nothing is found when <see cref="NBSPlaybackRate.canPlace"/> is <see langword="false"/>.<br/>
        /// 재생 속도입니다. <see cref="NBSPlaybackRate.canPlace"/>가 <see langword="false"/>이면 아무것도 찾지 않습니다.
        /// </param>
        /// <param name="loop">
        /// The loop range, or <see langword="null"/> without looping.<br/>
        /// 반복 범위이며, 반복하지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <param name="fromDistance">
        /// The start of the distance range in timeline seconds. A negative value also finds starts that are slightly behind.<br/>
        /// 타임라인 초 단위 거리 범위의 시작입니다. 음수이면 약간 지난 시작도 찾습니다.
        /// </param>
        /// <param name="includeFrom">
        /// Whether a start exactly at <paramref name="fromDistance"/> is included.<br/>
        /// 정확히 <paramref name="fromDistance"/>에 있는 시작을 포함할지 여부입니다.
        /// </param>
        /// <param name="toDistance">
        /// The inclusive end of the distance range in timeline seconds.<br/>
        /// 포함되는 타임라인 초 단위 거리 범위의 끝입니다.
        /// </param>
        /// <param name="output">
        /// The list that receives the sounds, in no particular order.<br/>
        /// 소리를 받을 리스트이며 순서는 정해져 있지 않습니다.
        /// </param>
        public void Query(double position, in NBSPlaybackRate rate, NBSLoopRange? loop, double fromDistance, bool includeFrom, double toDistance, List<NBSPlannedVoice> output)
        {
            ArgumentNullException.ThrowIfNull(output);
            if (!rate.canPlace || toDistance < fromDistance)
                return;

            bool forward = rate.tempo > 0;
            Window window = new Window(rate, loop, fromDistance, includeFrom, toDistance, rate.timelineScale * maximumBase);

            if (loop is not { } range || (forward ? position >= range.end : position < range.start))
            {
                QuerySegment(window, position, forward ? double.PositiveInfinity : double.NegativeInfinity, 0, true, output);
                return;
            }

            double segmentStart = position;
            double offset = 0;
            for (int segment = 0; offset <= toDistance; segment++)
            {
                double boundary = forward ? range.end : range.start;
                QuerySegment(window, segmentStart, boundary, offset, segment == 0, output);

                offset += Math.Abs(boundary - segmentStart);
                segmentStart = forward ? range.start : range.end;
            }
        }

        /// <summary>
        /// Finds the sounds that are already playing at a position, entered before it and ending after it.<br/>
        /// 위치에서 이미 재생 중인 소리, 즉 그 이전에 진입하고 그 이후에 끝나는 소리를 찾습니다.
        /// </summary>
        /// <param name="position">
        /// The current song time in seconds.<br/>
        /// 초 단위 현재 곡 시간입니다.
        /// </param>
        /// <param name="rate">
        /// The playback rate. Nothing is found when <see cref="NBSPlaybackRate.canPlace"/> is <see langword="false"/>.<br/>
        /// 재생 속도입니다. <see cref="NBSPlaybackRate.canPlace"/>가 <see langword="false"/>이면 아무것도 찾지 않습니다.
        /// </param>
        /// <param name="loop">
        /// The loop range, or <see langword="null"/> without looping.<br/>
        /// 반복 범위이며, 반복하지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <param name="output">
        /// The list that receives the sounds with a distance of <c>0</c> and their offsets.<br/>
        /// 거리 <c>0</c>과 오프셋을 가진 소리를 받을 리스트입니다.
        /// </param>
        /// <remarks>
        /// A sound that starts exactly at <paramref name="position"/> is not included; <see cref="Query"/> finds it with <c>includeFrom</c>.<br/>
        /// 정확히 <paramref name="position"/>에서 시작하는 소리는 포함하지 않으며, <c>includeFrom</c>을 지정한 <see cref="Query"/>가 찾습니다.
        /// </remarks>
        public void QueryActive(double position, in NBSPlaybackRate rate, NBSLoopRange? loop, List<NBSPlannedVoice> output)
        {
            ArgumentNullException.ThrowIfNull(output);
            if (!rate.canPlace)
                return;

            bool forward = rate.tempo > 0;
            double maximumLength = rate.timelineScale * maximumBase;
            bool looping = loop is { } range && (forward ? position < range.end : position >= range.start);

            Domain domain = Domain.Create(loop, looping, forward, true);
            QueryContaining(rate, loop, domain, position, position, 0, maximumLength, output);
        }

        readonly record struct Window(NBSPlaybackRate rate, NBSLoopRange? loop, double from, bool includeFrom, double to, double maximumLength)
        {
            public bool Contains(double distance) => (includeFrom ? distance >= from : distance > from) && distance <= to;
        }

        /// <summary>The range of note times whose attack the traversal reaches.</summary>
        readonly record struct Domain(double low, double high)
        {
            public bool Contains(double time) => time >= low && time < high;

            public static Domain Create(NBSLoopRange? loop, bool looping, bool forward, bool firstPass)
            {
                if (!looping || loop is not { } range)
                    return new Domain(double.NegativeInfinity, double.PositiveInfinity);

                // The first forward pass also plays the intro; later passes and reverse passes stay inside the loop.
                if (firstPass)
                    return forward ? new Domain(double.NegativeInfinity, range.end) : new Domain(range.start, double.PositiveInfinity);

                return new Domain(range.start, range.end);
            }
        }

        void QuerySegment(in Window window, double segmentStart, double boundary, double offset, bool firstSegment, List<NBSPlannedVoice> output)
        {
            bool forward = window.rate.tempo > 0;

            // Late starts are only searched behind the current position, never across a wrap.
            double nearDistance = firstSegment ? window.from - offset : Math.Max(0, window.from - offset);
            double farDistance = window.to - offset;
            double nearPosition = forward ? segmentStart + nearDistance : segmentStart - nearDistance;
            double farPosition = forward ? Math.Min(boundary, segmentStart + farDistance) : Math.Max(boundary, segmentStart - farDistance);
            double low = Math.Min(nearPosition, farPosition);
            double high = Math.Max(nearPosition, farPosition);

            Domain domain = Domain.Create(window.loop, window.loop != null && !double.IsInfinity(boundary), forward, firstSegment);
            ReadOnlySpan<NBSMapNote> notes = map.notes.AsSpan();
            for (int i = map.FindNoteAtOrAfter(low - window.maximumLength); i < notes.Length && notes[i].time <= high + window.maximumLength; i++)
            {
                if (!domain.Contains(notes[i].time) || !TryPlace(i, window.rate, window.loop, out NBSNotePlacement placement, out double length, out bool stopped))
                    continue;

                double entry = placement.entry;
                if (forward ? entry >= boundary || (!firstSegment && entry < segmentStart) : entry < boundary || (!firstSegment && entry > segmentStart))
                    continue;

                double distance = offset + (forward ? entry - segmentStart : segmentStart - entry);
                if (window.Contains(distance))
                    output.Add(new NBSPlannedVoice(i, distance, entry, placement.GetSourceTime(entry), placement, length, stopped));
            }
        }

        void QueryContaining(in NBSPlaybackRate rate, NBSLoopRange? loop, Domain domain, double point, double searchCenter, double distance, double maximumLength, List<NBSPlannedVoice> output)
        {
            ReadOnlySpan<NBSMapNote> notes = map.notes.AsSpan();
            for (int i = map.FindNoteAtOrAfter(searchCenter - maximumLength); i < notes.Length && notes[i].time <= searchCenter + maximumLength; i++)
            {
                if (!domain.Contains(notes[i].time) || !TryPlace(i, rate, loop, out NBSNotePlacement placement, out double length, out bool stopped))
                    continue;

                if (placement.low < point && point < placement.high)
                    output.Add(new NBSPlannedVoice(i, distance, point, placement.GetSourceTime(point), placement, length, stopped));
            }
        }

        bool TryPlace(int mapIndex, in NBSPlaybackRate rate, NBSLoopRange? loop, out NBSNotePlacement placement, out double length, out bool stopped)
        {
            placement = default;
            stopped = false;
            length = double.NaN;
            if (!IsAudible(mapIndex, rate))
                return false;

            length = GetLength(mapIndex, rate, loop, out stopped);
            if (!(length > 0))
                return false;

            placement = GetPlacement(mapIndex, length, rate);
            return true;
        }
    }
}
