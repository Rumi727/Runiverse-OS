#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores a constant-tempo segment that begins at <paramref name="tick"/>.<br/>
    /// <paramref name="tick"/>에서 시작하는 일정한 템포 구간을 저장합니다.
    /// </summary>
    /// <param name="tick">
    /// The tick at which the tempo takes effect.<br/>
    /// 템포가 적용되기 시작하는 tick입니다.
    /// </param>
    /// <param name="ticksPerSecond">
    /// The tempo of the segment in ticks per second.<br/>
    /// 구간의 템포(초당 tick 수)입니다.
    /// </param>
    /// <param name="time">
    /// The song time of <paramref name="tick"/> in seconds.<br/>
    /// <paramref name="tick"/>의 곡 시간(초)입니다.
    /// </param>
    public readonly record struct NBSTempoChange(int tick, double ticksPerSecond, double time)
    {
        /// <summary>
        /// Gets the tempo in beats per minute. One NBS beat is four ticks.<br/>
        /// 템포를 분당 박자 수로 가져옵니다. NBS의 한 박은 4 tick입니다.
        /// </summary>
        public double beatsPerMinute => ticksPerSecond * 15;
    }
}
