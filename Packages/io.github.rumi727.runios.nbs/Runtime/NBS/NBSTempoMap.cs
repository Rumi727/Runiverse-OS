#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RuniOS.NBS
{
    /// <summary>
    /// Converts between NBS ticks and song time across tempo changes.<br/>
    /// 템포 변경을 반영하여 NBS tick과 곡 시간을 서로 변환합니다.
    /// </summary>
    /// <remarks>
    /// The map is immutable and can be read from any thread. Positions before the first segment or after the last one are extrapolated with the nearest segment.<br/>
    /// 맵은 불변이며 어느 스레드에서든 읽을 수 있습니다. 첫 구간 이전이나 마지막 구간 이후의 위치는 가장 가까운 구간으로 외삽합니다.
    /// </remarks>
    public sealed class NBSTempoMap
    {
        /// <summary>
        /// Initializes a tempo map from an initial tempo and tempo changes.<br/>
        /// 초기 템포와 템포 변경으로 템포 맵을 초기화합니다.
        /// </summary>
        /// <param name="initialTicksPerSecond">
        /// The tempo at tick <c>0</c> in ticks per second.<br/>
        /// tick <c>0</c>의 템포(초당 tick 수)입니다.
        /// </param>
        /// <param name="changes">
        /// The tempo changes as tick and ticks-per-second pairs, in ascending tick order. When several changes share a tick, the last one wins.<br/>
        /// tick과 초당 tick 수 쌍으로 된 템포 변경이며 tick 오름차순입니다. 같은 tick에 여러 변경이 있으면 마지막 변경이 적용됩니다.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a tempo is not finite or not greater than zero, or when ticks are not in ascending order.<br/>
        /// 템포가 유한하지 않거나 0보다 크지 않은 경우, 또는 tick이 오름차순이 아닌 경우 발생합니다.
        /// </exception>
        public NBSTempoMap(double initialTicksPerSecond, ReadOnlySpan<(int tick, double ticksPerSecond)> changes)
        {
            ThrowIfInvalidTempo(initialTicksPerSecond);

            NBSTempoChange[] result = new NBSTempoChange[changes.Length + 1];
            result[0] = new NBSTempoChange(0, initialTicksPerSecond, 0);

            int count = 1;
            for (int i = 0; i < changes.Length; i++)
            {
                (int tick, double ticksPerSecond) = changes[i];
                ThrowIfInvalidTempo(ticksPerSecond);

                NBSTempoChange previous = result[count - 1];
                if (tick < previous.tick)
                    throw new ArgumentOutOfRangeException(nameof(changes), tick, "Tempo changes must be in ascending tick order.");

                NBSTempoChange change = new NBSTempoChange(tick, ticksPerSecond, previous.time + ((tick - previous.tick) / previous.ticksPerSecond));
                if (tick == previous.tick)
                    result[count - 1] = change;
                else
                    result[count++] = change;
            }

            Array.Resize(ref result, count);
            this.changes = ImmutableCollectionsMarshal.AsImmutableArray(result);
        }

        /// <summary>
        /// Gets the constant-tempo segments in ascending tick order. The first segment always starts at tick <c>0</c>.<br/>
        /// 일정한 템포 구간을 tick 오름차순으로 가져옵니다. 첫 구간은 항상 tick <c>0</c>에서 시작합니다.
        /// </summary>
        public ImmutableArray<NBSTempoChange> changes { get; }

        /// <summary>
        /// Converts a tick position to song time in seconds.<br/>
        /// tick 위치를 초 단위 곡 시간으로 변환합니다.
        /// </summary>
        /// <param name="tick">
        /// The tick position, which may be fractional or outside the song.<br/>
        /// 소수이거나 곡 밖일 수 있는 tick 위치입니다.
        /// </param>
        /// <returns>
        /// The song time in seconds.<br/>
        /// 초 단위 곡 시간을 반환합니다.
        /// </returns>
        public double TickToTime(double tick)
        {
            NBSTempoChange change = changes[FindByTick(tick)];
            return change.time + ((tick - change.tick) / change.ticksPerSecond);
        }

        /// <summary>
        /// Converts song time in seconds to a tick position.<br/>
        /// 초 단위 곡 시간을 tick 위치로 변환합니다.
        /// </summary>
        /// <param name="time">
        /// The song time in seconds, which may be outside the song.<br/>
        /// 곡 밖일 수 있는 초 단위 곡 시간입니다.
        /// </param>
        /// <returns>
        /// The fractional tick position.<br/>
        /// 소수 tick 위치를 반환합니다.
        /// </returns>
        public double TimeToTick(double time)
        {
            NBSTempoChange change = changes[FindByTime(time)];
            return change.tick + ((time - change.time) * change.ticksPerSecond);
        }

        /// <summary>
        /// Gets the tempo segment active at a tick position.<br/>
        /// tick 위치에서 활성화된 템포 구간을 가져옵니다.
        /// </summary>
        /// <param name="tick">
        /// The tick position to query.<br/>
        /// 조회할 tick 위치입니다.
        /// </param>
        /// <returns>
        /// The active segment.<br/>
        /// 활성 구간을 반환합니다.
        /// </returns>
        public NBSTempoChange GetChangeAtTick(double tick) => changes[FindByTick(tick)];

        /// <summary>
        /// Gets the tempo segment active at a song time.<br/>
        /// 곡 시간에서 활성화된 템포 구간을 가져옵니다.
        /// </summary>
        /// <param name="time">
        /// The song time in seconds to query.<br/>
        /// 조회할 초 단위 곡 시간입니다.
        /// </param>
        /// <returns>
        /// The active segment.<br/>
        /// 활성 구간을 반환합니다.
        /// </returns>
        public NBSTempoChange GetChangeAtTime(double time) => changes[FindByTime(time)];

        int FindByTick(double tick)
        {
            ReadOnlySpan<NBSTempoChange> span = changes.AsSpan();
            int low = 0;
            int high = span.Length - 1;
            while (low < high)
            {
                int middle = (low + high + 1) >> 1;
                if (span[middle].tick <= tick)
                    low = middle;
                else
                    high = middle - 1;
            }

            return low;
        }

        int FindByTime(double time)
        {
            ReadOnlySpan<NBSTempoChange> span = changes.AsSpan();
            int low = 0;
            int high = span.Length - 1;
            while (low < high)
            {
                int middle = (low + high + 1) >> 1;
                if (span[middle].time <= time)
                    low = middle;
                else
                    high = middle - 1;
            }

            return low;
        }

        static void ThrowIfInvalidTempo(double ticksPerSecond)
        {
            if (!double.IsFinite(ticksPerSecond) || ticksPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "A tempo must be finite and greater than zero.");
        }
    }
}
