#nullable enable
using System.Diagnostics;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Pairs a master DSP clock with the <see cref="Stopwatch"/> timestamp at which the mixer processed that clock.<br/>
    /// 마스터 DSP 클록과 믹서가 그 클록을 처리한 시각의 <see cref="Stopwatch"/> 타임스탬프를 한 쌍으로 묶습니다.
    /// </summary>
    /// <param name="dspClock">
    /// The master DSP clock in output samples at the end of the block the mixer processed, which is the master clock once that block is mixed.<br/>
    /// 믹서가 처리한 블록의 끝에 해당하는 출력 샘플 단위 마스터 DSP 클록이며, 그 블록이 믹스된 뒤의 마스터 클록과 같습니다.
    /// </param>
    /// <param name="timestamp">
    /// The <see cref="Stopwatch.GetTimestamp"/> value taken on the mixer thread while it processed <paramref name="dspClock"/>.<br/>
    /// 믹서 스레드가 <paramref name="dspClock"/>을 처리하는 동안 얻은 <see cref="Stopwatch.GetTimestamp"/> 값입니다.
    /// </param>
    public readonly record struct SoundMixStamp(ulong dspClock, long timestamp);
}
