#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Is implemented by a transport whose timeline an <see cref="NBSVoiceScheduler{TTransport}"/> follows on the DSP clock.<br/>
    /// <see cref="NBSVoiceScheduler{TTransport}"/>가 DSP 클록 위에서 따라가는 타임라인을 가진 트랜스포트가 구현합니다.
    /// </summary>
    /// <remarks>
    /// The transport defines its timeline with <see cref="clockAnchor"/> instead of a wall clock. Before notifying the scheduler of a seek, a play,
    /// a tempo change, a pause, an unpause, or a loop change, it moves the anchor to <see cref="NBSClockAnchor.TryGetNextMixClock"/> at the position that the change keeps or sets.<br/>
    /// 트랜스포트는 벽시계 대신 <see cref="clockAnchor"/>로 타임라인을 정의합니다. seek, 재생, tempo 변경, 일시 정지, 재개, 반복 변경을 스케줄러에 알리기 전에,
    /// 그 변경이 유지하거나 정하는 위치로 기준점을 <see cref="NBSClockAnchor.TryGetNextMixClock"/>에 옮깁니다.
    /// </remarks>
    public interface INBSTransport
    {
        /// <summary>
        /// Gets the anchor of the timeline. While paused or stopped, its <see cref="NBSClockAnchor.time"/> is the held position.<br/>
        /// 타임라인의 기준점을 가져옵니다. 일시 정지나 정지 중에는 <see cref="NBSClockAnchor.time"/>이 유지되는 위치입니다.
        /// </summary>
        NBSClockAnchor clockAnchor { get; }

        /// <summary>
        /// Gets the song time range that playback repeats, or <see langword="null"/> while it does not loop.<br/>
        /// 재생이 반복하는 곡 시간 범위를 가져오며, 반복하지 않으면 <see langword="null"/>입니다.
        /// </summary>
        NBSLoopRange? loopRange { get; }
    }
}
