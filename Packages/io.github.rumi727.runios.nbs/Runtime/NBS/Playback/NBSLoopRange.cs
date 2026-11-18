#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the song time range that playback repeats.<br/>
    /// 재생이 반복하는 곡 시간 범위를 저장합니다.
    /// </summary>
    /// <param name="start">
    /// The song time where playback returns to, in seconds.<br/>
    /// 재생이 돌아가는 곡 시간(초)입니다.
    /// </param>
    /// <param name="end">
    /// The song time where playback wraps, in seconds.<br/>
    /// 재생이 되돌아가는 곡 시간(초)입니다.
    /// </param>
    public readonly record struct NBSLoopRange(double start, double end)
    {
        /// <summary>
        /// Gets the length of the range in seconds.<br/>
        /// 범위 길이를 초 단위로 가져옵니다.
        /// </summary>
        public double range => end - start;

        /// <summary>
        /// Creates a range when the values describe a finite range longer than zero.<br/>
        /// 값이 0보다 긴 유한 범위이면 범위를 만듭니다.
        /// </summary>
        /// <param name="enabled">
        /// Whether looping is enabled.<br/>
        /// 반복이 켜져 있는지 여부입니다.
        /// </param>
        /// <param name="start">
        /// The loop start in seconds.<br/>
        /// 초 단위 반복 시작입니다.
        /// </param>
        /// <param name="end">
        /// The loop end in seconds.<br/>
        /// 초 단위 반복 끝입니다.
        /// </param>
        /// <returns>
        /// The range, or <see langword="null"/> when looping is disabled or the range is not usable.<br/>
        /// 범위를 반환하며, 반복이 꺼져 있거나 범위를 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static NBSLoopRange? Create(bool enabled, double start, double end) =>
            enabled && double.IsFinite(start) && double.IsFinite(end) && end > start ? new NBSLoopRange(start, end) : null;

        /// <summary>
        /// Folds a linear position into the range, in the direction of travel.<br/>
        /// 진행 방향을 기준으로 선형 위치를 범위 안으로 접습니다.
        /// </summary>
        /// <param name="time">
        /// The linear position in seconds.<br/>
        /// 초 단위 선형 위치입니다.
        /// </param>
        /// <param name="tempo">
        /// The signed timeline speed that decides which boundary wraps.<br/>
        /// 어느 경계에서 되돌아갈지 정하는 부호 있는 타임라인 속도입니다.
        /// </param>
        /// <returns>
        /// The folded position, in <c>[start, end)</c> while moving forward and in <c>(start, end]</c> while moving backward.
        /// Positions before the start while moving forward, such as an intro, and positions after the end while moving backward stay as they are.<br/>
        /// 접힌 위치를 반환하며, 정방향 진행 중에는 <c>[start, end)</c>, 역방향 진행 중에는 <c>(start, end]</c> 안에 있습니다.
        /// 정방향 진행 중 시작 이전의 위치(인트로 등)와 역방향 진행 중 끝 이후의 위치는 그대로 유지됩니다.
        /// </returns>
        public double Fold(double time, float tempo)
        {
            if (tempo > 0 && time >= end)
                return start + ((time - start) % range);
            if (tempo < 0 && time <= start)
                return end - ((start - time) % range);

            return time;
        }
    }
}
