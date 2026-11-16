#nullable enable
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Defines a timeline on the DSP clock of a sound system: the timeline is at <see cref="time"/> at <see cref="dspClock"/>.<br/>
    /// 사운드 시스템의 DSP 클록 위에 타임라인을 정의합니다. 타임라인은 <see cref="dspClock"/>에서 <see cref="time"/>에 있습니다.
    /// </summary>
    /// <param name="dspClock">
    /// The master DSP clock in output samples at which the timeline is at <paramref name="time"/>.<br/>
    /// 타임라인이 <paramref name="time"/>에 있는 출력 샘플 단위의 마스터 DSP 클록입니다.
    /// </param>
    /// <param name="time">
    /// The linear timeline position in seconds before loop wrapping.<br/>
    /// 반복으로 되돌아가기 전의 초 단위 선형 타임라인 위치입니다.
    /// </param>
    /// <remarks>
    /// While playing, the timeline advances by the tempo from the anchor and holds its position before it.
    /// Sounds are scheduled from the anchor with plain arithmetic, so their spacing is exact to the sample.<br/>
    /// 재생 중 타임라인은 기준점부터 tempo만큼 진행하며, 기준점 이전에는 위치를 유지합니다.
    /// 소리는 기준점에서 단순 계산으로 예약하므로 소리 사이 간격이 샘플 단위로 정확합니다.
    /// </remarks>
    public readonly record struct NBSClockAnchor(ulong dspClock, double time)
    {
        /// <summary>
        /// Gets the linear timeline position at a DSP clock while playing.<br/>
        /// 재생 중일 때 DSP 클록에서의 선형 타임라인 위치를 가져옵니다.
        /// </summary>
        /// <param name="clock">
        /// The master DSP clock in output samples.<br/>
        /// 출력 샘플 단위의 마스터 DSP 클록입니다.
        /// </param>
        /// <param name="tempo">
        /// The signed timeline speed.<br/>
        /// 부호 있는 타임라인 속도입니다.
        /// </param>
        /// <param name="sampleRate">
        /// The output sample rate of the sound system.<br/>
        /// 사운드 시스템의 출력 샘플 레이트입니다.
        /// </param>
        /// <returns>
        /// The linear position in seconds; <see cref="time"/> when <paramref name="clock"/> is not after <see cref="dspClock"/>.<br/>
        /// 초 단위 선형 위치를 반환하며, <paramref name="clock"/>이 <see cref="dspClock"/> 이후가 아니면 <see cref="time"/>을 반환합니다.
        /// </returns>
        public double GetTime(ulong clock, float tempo, int sampleRate) => clock <= dspClock ? time : time + (((clock - dspClock) / (double)sampleRate) * tempo);

        /// <summary>
        /// Gets the first DSP clock that the mixer has not started to mix. A sound scheduled at or after it starts exactly at its sample.<br/>
        /// 믹서가 아직 믹스를 시작하지 않은 첫 DSP 클록을 가져옵니다. 이 클록 이후로 예약한 소리는 정확히 그 샘플에서 시작합니다.
        /// </summary>
        /// <param name="system">
        /// The sound system whose master clock is read.<br/>
        /// 마스터 클록을 읽을 사운드 시스템입니다.
        /// </param>
        /// <param name="clock">
        /// The clock when successful; otherwise, <c>0</c>.<br/>
        /// 성공하면 클록이고, 그렇지 않으면 <c>0</c>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the sound system is active; otherwise, <see langword="false"/>.<br/>
        /// 사운드 시스템이 활성 상태이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <remarks>
        /// The master clock that FMOD reports is the start of the block being mixed or of the next one, so one block is added to stay ahead of the mixer.<br/>
        /// FMOD가 알려 주는 마스터 클록은 믹스 중인 블록이나 다음 블록의 시작이므로, 믹서보다 앞서도록 한 블록을 더합니다.
        /// </remarks>
        public static bool TryGetNextMixClock(SoundSystem system, out ulong clock)
        {
            ArgumentNullException.ThrowIfNull(system);

            return system.Execute(static soundSystem => soundSystem.GetMasterDSPClock() + soundSystem.UseNative(static native =>
            {
                native.getDSPBufferSize(out uint blockLength, out _).ThrowIfNotOk();
                return (ulong)blockLength;
            }), out clock);
        }
    }
}
