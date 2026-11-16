#nullable enable
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
        /// <see cref="time"/> follows this timeline as the mixer advances, so it can lag the anchor by about one mix block.<br/>
        /// <see cref="time"/>은 믹서가 진행할 때마다 이 타임라인을 따라가므로 기준점보다 약 한 믹스 블록 늦을 수 있습니다.
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
        }

        double FoldTime(double linear) => loopRange is { } range ? range.Fold(linear, base.tempo) : linear;

        ulong lastMixClock;

        /// <summary>Moves the interpolated time to the timeline whenever the mixer advances, as <see cref="WaveAudioSource"/> follows its channel.</summary>
        void SyncTimeToMixer()
        {
            if (!anchorAdvances || !SoundSystem.main.Execute(static system => system.GetMasterDSPClock(), out ulong clock) || clock == lastMixClock)
                return;

            // This runs under the sound system lock, which a reset waits for while a command may hold the playing lock and wait for the sound system.
            // So a busy lock skips this mix instead of waiting, and the next one catches up.
            if (!playingLock.TryEnterWriteLock(0))
                return;

            try
            {
                lastMixClock = clock;
                if (anchorAdvances)
                    SyncInterpolatedTime(_clockAnchor.GetTime(clock, base.tempo, SoundSystem.main.outputSampleRate));
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
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
