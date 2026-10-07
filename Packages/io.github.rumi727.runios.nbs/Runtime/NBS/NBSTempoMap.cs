#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Converts between NBS ticks and seconds across mid-song tempo changes.<br/>
    /// 곡 도중 템포 변경을 반영하여 NBS 틱과 초를 상호 변환합니다.
    /// </summary>
    public sealed class NBSTempoMap
    {
        /// <summary>
        /// Describes a constant-tempo range beginning at <paramref name="startTick"/>.<br/>
        /// <paramref name="startTick"/>에서 시작하는 고정 템포 구간을 설명합니다.
        /// </summary>
        public readonly record struct Segment(double startTick, double startTime, double ticksPerSecond);

        public NBSTempoMap(double initialTicksPerSecond, IEnumerable<NBSSpecialEvent> events)
        {
            if (!double.IsFinite(initialTicksPerSecond) || initialTicksPerSecond <= 0)
                throw new InvalidDataException($"The NBS header tempo must be finite and greater than zero: {initialTicksPerSecond} ticks per second.");

            IEnumerable<(int tick, double ticksPerSecond)> changes = events
                .Where(x => x.kind == NBSSpecialEventKind.tempoChange && x.tempoBpm > 0)
                .GroupBy(x => x.tick)
                .Select(x => x.OrderByDescending(y => y.layer).First())
                .OrderBy(x => x.tick)
                .Select(x => (x.tick, x.tempoBpm / 15d));

            List<Segment> result = [new Segment(0, 0, initialTicksPerSecond)];
            foreach ((int tick, double ticksPerSecond) in changes.OrderBy(x => x.tick))
            {
                if (tick < 0 || !double.IsFinite(ticksPerSecond) || ticksPerSecond <= 0)
                    continue;

                Segment previous = result[^1];
                double startTime = previous.startTime + ((tick - previous.startTick) / previous.ticksPerSecond);
                Segment segment = new Segment(tick, startTime, ticksPerSecond);

                if (previous.startTick.Approximately(tick))
                    result[^1] = segment;
                else
                    result.Add(segment);
            }

            segments = result.AsReadOnly();
        }

        /// <summary>Gets constant-tempo segments in ascending tick order.<br/>고정 템포 구간을 틱 오름차순으로 가져옵니다.</summary>
        public IReadOnlyList<Segment> segments { get; }

        /// <summary>
        /// Converts an unbounded NBS tick position to seconds.<br/>
        /// 범위 제한 없는 NBS 틱 위치를 초로 변환합니다.
        /// </summary>
        /// <param name="tick">The unbounded NBS tick position.<br/>범위 제한 없는 NBS 틱 위치입니다.</param>
        /// <returns>The corresponding file time in seconds.<br/>해당하는 파일 시간(초)입니다.</returns>
        public double TickToTime(double tick)
        {
            Segment segment = FindByTick(tick);
            return segment.startTime + ((tick - segment.startTick) / segment.ticksPerSecond);
        }

        /// <summary>
        /// Converts an unbounded time in seconds to an NBS tick position.<br/>
        /// 범위 제한 없는 초 단위 시간을 NBS 틱 위치로 변환합니다.
        /// </summary>
        /// <param name="time">The unbounded file time in seconds.<br/>범위 제한 없는 파일 시간(초)입니다.</param>
        /// <returns>The corresponding logical NBS tick position.<br/>해당하는 논리적 NBS 틱 위치입니다.</returns>
        public double TimeToTick(double time)
        {
            Segment segment = FindByTime(time);
            return segment.startTick + ((time - segment.startTime) * segment.ticksPerSecond);
        }

        /// <summary>
        /// Gets the active ticks-per-second value at an NBS tick position.<br/>
        /// NBS 틱 위치에서 활성화된 초당 틱 수를 가져옵니다.
        /// </summary>
        /// <param name="tick">The tick position to query.<br/>조회할 틱 위치입니다.</param>
        /// <returns>The active file tempo in ticks per second.<br/>활성 파일 템포(초당 틱 수)입니다.</returns>
        public double GetTicksPerSecond(double tick) => FindByTick(tick).ticksPerSecond;

        Segment FindByTick(double tick)
        {
            int low = 0;
            int high = segments.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (segments[middle].startTick <= tick)
                    low = middle;
                else
                    high = middle - 1;
            }

            return segments[low];
        }

        Segment FindByTime(double time)
        {
            int low = 0;
            int high = segments.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (segments[middle].startTime <= time)
                    low = middle;
                else
                    high = middle - 1;
            }

            return segments[low];
        }
    }
}
