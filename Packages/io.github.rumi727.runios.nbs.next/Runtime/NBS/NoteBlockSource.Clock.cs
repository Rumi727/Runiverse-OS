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

        /// <summary>Whether the timeline advances from <see cref="_clockAnchor"/>, which is decided when it is anchored.</summary>
        bool anchorAdvances;

        /// <summary>Anchors the timeline at the next mix clock. The caller holds the write lock of <see cref="RuniAudioSource.playingLock"/>.</summary>
        void AnchorUnsafe(double linearTime) => SetAnchorUnsafe(NBSClockAnchor.TryGetNextMixClock(SoundSystem.main, out ulong clock) ? clock : ulong.MaxValue, linearTime);

        /// <summary>
        /// Moves the anchor to the next mix clock without moving the timeline. The position is wrapped into the current loop range first,
        /// because a later change of the tempo sign wraps only at the other boundary.
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
        /// Moves the interpolated time to the new anchor. While the timeline advances, the time holds until the mixer reaches the anchor,
        /// which is also the case until the mixer stamps its first block.
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
        /// Moves the interpolated time to where the timeline was when the mixer processed the clock of <paramref name="stamp"/>,
        /// and the interpolated output time to where it was at the output clock.
        /// </summary>
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
        /// Moves the interpolated time to the timeline whenever the mixer advances, as <see cref="WaveAudioSource"/> follows its channel.
        /// The time is based on when the mixer processed the clock, not on when this update found it.
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
        /// Gets the timestamp at which the mixer reaches <paramref name="clock"/>, extrapolated from <paramref name="stamp"/>.
        /// The anchor is usually a block or two ahead of the latest stamp, and the time must hold until the mixer reaches it.
        /// A clock that cannot be reached, such as <see cref="ulong.MaxValue"/>, holds the time indefinitely.
        /// </summary>
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
