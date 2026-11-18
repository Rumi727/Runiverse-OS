#nullable enable
using System.Diagnostics;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    public sealed partial class NoteBlockSource
    {
        /// <summary>
        /// Gets the DSP clock anchor that defines the timeline the sounds follow.<br/>
        /// 소리가 따라가는 타임라인을 정의하는 DSP 클록 기준점을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// <see cref="time"/> follows this timeline from the moment the mixer processed each block, and holds before the mixer reaches the anchor.<br/>
        /// <see cref="time"/>은 믹서가 각 블록을 처리한 순간을 기준으로 이 타임라인을 따라가며, 믹서가 기준점에 도달하기 전에는 위치를 유지합니다.
        /// </remarks>
        public NBSClockAnchor clockAnchor
        {
            get
            {
                playingLock.EnterReadLock();

                try
                {
                    return _clockAnchor;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
        }
        NBSClockAnchor _clockAnchor;

        /// <summary>
        /// Whether the timeline advances from <see cref="_clockAnchor"/>, which is decided when it is anchored.<br/>
        /// 타임라인이 <see cref="_clockAnchor"/>에서 진행하는지 여부이며, 기준점을 정할 때 결정됩니다.
        /// </summary>
        bool anchorAdvances;

        /// <summary>
        /// Anchors the timeline at the next mix clock.<br/>
        /// The caller holds the write lock of <see cref="RuniAudioSource.playingLock"/>.
        /// <br/><br/>
        /// 다음 믹스 클록에 타임라인의 기준점을 정합니다.<br/>
        /// 호출자는 <see cref="RuniAudioSource.playingLock"/>의 쓰기 잠금을 보유합니다.
        /// </summary>
        /// <param name="linearTime">
        /// The unwrapped timeline position at the anchor.<br/>
        /// 기준점에서의 접히지 않은 타임라인 위치입니다.
        /// </param>
        void AnchorUnsafe(double linearTime) => SetAnchorUnsafe(NBSClockAnchor.TryGetNextMixClock(SoundSystem.main, out ulong clock) ? clock : ulong.MaxValue, linearTime);

        /// <summary>
        /// Moves the anchor to the next mix clock without moving the timeline.<br/>
        /// The position is wrapped into the current loop range first, because a later change of the tempo sign wraps only at the other boundary.
        /// <br/><br/>
        /// 타임라인을 움직이지 않고 기준점을 다음 믹스 클록으로 옮깁니다.<br/>
        /// 나중에 템포 부호가 바뀌면 반대쪽 경계에서만 접히므로, 위치를 먼저 현재 반복 범위 안으로 접습니다.
        /// </summary>
        void MoveAnchorUnsafe()
        {
            bool hasClock = NBSClockAnchor.TryGetNextMixClock(SoundSystem.main, out ulong clock);
            double linear = hasClock && anchorAdvances ? _clockAnchor.GetTime(clock, base.tempo, SoundSystem.main.outputSampleRate) : _clockAnchor.time;
            SetAnchorUnsafe(hasClock ? clock : ulong.MaxValue, FoldTime(linear));
        }

        void SetAnchorUnsafe(ulong clock, double linearTime)
        {
            // Without a readable clock the anchor stays ahead of every clock, so the timeline holds until the sound system resets and anchors it again.
            _clockAnchor = new NBSClockAnchor(clock, linearTime);
            anchorAdvances = isPlaying && !isPaused;
            SyncTimeToAnchorUnsafe();
        }

        /// <summary>
        /// Moves the interpolated time to the new anchor.<br/>
        /// While the timeline advances, the time holds until the mixer reaches the anchor, which is also the case until the mixer stamps its first block.
        /// <br/><br/>
        /// 보간 시간을 새 기준점으로 옮깁니다.<br/>
        /// 타임라인이 진행하는 동안에는 믹서가 기준점에 도달할 때까지 시간을 유지하며, 믹서가 첫 블록을 기록하기 전에도 마찬가지입니다.
        /// </summary>
        void SyncTimeToAnchorUnsafe()
        {
            if (!anchorAdvances)
            {
                SyncInterpolatedTime(_clockAnchor.time);
                return;
            }

            if (SoundSystem.main.Execute(static system => (system.TryGetMixStamp(out SoundMixStamp stamp), stamp), out (bool hasStamp, SoundMixStamp stamp) mix) && mix.hasStamp)
                SyncTimeToStampUnsafe(mix.stamp);
            else
                SyncInterpolatedTime(_clockAnchor.time, long.MaxValue);
        }

        /// <summary>
        /// Moves the interpolated time to where the timeline was when the mixer processed the clock of <paramref name="stamp"/>,<br/>
        /// and the interpolated output time to where it was at the output clock of <paramref name="stamp"/>.
        /// <br/><br/>
        /// 보간 시간을 믹서가 <paramref name="stamp"/>의 클록을 처리했을 때의 타임라인 위치로 옮기고,<br/>
        /// 보간된 출력 시간을 <paramref name="stamp"/>의 출력 클록에서의 위치로 옮깁니다.
        /// </summary>
        /// <param name="stamp">
        /// The mix stamp to synchronize to.<br/>
        /// 동기화할 믹스 스탬프입니다.
        /// </param>
        void SyncTimeToStampUnsafe(SoundMixStamp stamp)
        {
            int sampleRate = SoundSystem.main.outputSampleRate;
            double linearTime = _clockAnchor.GetTime(stamp.dspClock, base.tempo, sampleRate);
            SyncInterpolatedTime(linearTime, GetMixTimestamp(stamp, _clockAnchor.dspClock, sampleRate));

            // An anchor that cannot be reached holds the output time as well.
            if (_clockAnchor.dspClock == ulong.MaxValue)
            {
                SyncInterpolatedOutputTime(_clockAnchor.time, long.MaxValue);
                return;
            }

            // The output clock is behind the mixer and often before the anchor, so the timeline is extrapolated back from the anchor
            // and passes the anchor time when its sound is output.
            long outputOffset = (long)(stamp.outputClock - _clockAnchor.dspClock);
            SyncInterpolatedOutputTime(_clockAnchor.time + ((outputOffset / (double)sampleRate) * base.tempo), stamp.timestamp);
        }

        double FoldTime(double linear) => loopRange is { } range ? range.Fold(linear, base.tempo) : linear;

        ulong lastMixClock;

        /// <summary>
        /// Moves the interpolated time to the timeline whenever the mixer advances, as <see cref="WaveAudioSource"/> follows its channel.<br/>
        /// The time is based on when the mixer processed the clock, not on when this update found it.
        /// <br/><br/>
        /// <see cref="WaveAudioSource"/>가 채널을 따라가듯이, 믹서가 진행할 때마다 보간 시간을 타임라인으로 옮깁니다.<br/>
        /// 시간은 이 갱신이 클록을 발견한 시각이 아니라 믹서가 그 클록을 처리한 시각을 기준으로 합니다.
        /// </summary>
        void SyncTimeToMixer()
        {
            if
            (
                !anchorAdvances ||
                !SoundSystem.main.Execute(static system => (system.TryGetMixStamp(out SoundMixStamp stamp), stamp), out (bool hasStamp, SoundMixStamp stamp) mix) ||
                !mix.hasStamp ||
                mix.stamp.dspClock == lastMixClock
            )
                return;

            // This runs under the sound system lock, which a reset waits for while a command may hold the playing lock and wait for the sound system.
            // So a busy lock skips this mix instead of waiting, and the next one catches up.
            if (!playingLock.TryEnterWriteLock(0))
                return;

            try
            {
                lastMixClock = mix.stamp.dspClock;
                if (anchorAdvances)
                    SyncTimeToStampUnsafe(mix.stamp);
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the timestamp at which the mixer reaches <paramref name="clock"/>, extrapolated from <paramref name="stamp"/>.<br/>
        /// The anchor is usually a block or two ahead of the latest stamp, and the time must hold until the mixer reaches it.
        /// <br/><br/>
        /// <paramref name="stamp"/>에서 외삽하여 믹서가 <paramref name="clock"/>에 도달하는 타임스탬프를 가져옵니다.<br/>
        /// 기준점은 보통 최신 스탬프보다 한두 블록 앞에 있으며, 믹서가 도달할 때까지 시간을 유지해야 합니다.
        /// </summary>
        /// <param name="stamp">
        /// The mix stamp to extrapolate from.<br/>
        /// 외삽의 기준이 되는 믹스 스탬프입니다.
        /// </param>
        /// <param name="clock">
        /// The master DSP clock to reach.<br/>
        /// 도달할 마스터 DSP 클록입니다.
        /// </param>
        /// <param name="sampleRate">
        /// The output sample rate of the sound system.<br/>
        /// 사운드 시스템의 출력 샘플 레이트입니다.
        /// </param>
        /// <returns>
        /// The timestamp of <paramref name="stamp"/> if <paramref name="clock"/> is already reached; otherwise, the extrapolated timestamp,
        /// which saturates at <see cref="long.MaxValue"/> for a clock that cannot be reached, such as <see cref="ulong.MaxValue"/>.
        /// <br/><br/>
        /// <paramref name="clock"/>에 이미 도달했으면 <paramref name="stamp"/>의 타임스탬프를 반환하고, 그렇지 않으면 외삽한 타임스탬프를 반환하며,
        /// <see cref="ulong.MaxValue"/>처럼 도달할 수 없는 클록은 <see cref="long.MaxValue"/>로 포화됩니다.
        /// </returns>
        static long GetMixTimestamp(SoundMixStamp stamp, ulong clock, int sampleRate)
        {
            if (clock <= stamp.dspClock)
                return stamp.timestamp;

            double timestamp = stamp.timestamp + (((clock - stamp.dspClock) / (double)sampleRate) * Stopwatch.Frequency);
            return timestamp >= long.MaxValue ? long.MaxValue : (long)timestamp;
        }

        void OnSoundSystemReset(SoundSystem system)
        {
            playingLock.EnterWriteLock();

            try
            {
                // The new clock starts again, so the timeline is anchored on it from the interpolated time.
                lastMixClock = 0;
                AnchorUnsafe(base.time);
                scheduler?.Resync();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }
    }
}
