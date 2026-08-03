#nullable enable
using RuniOS.NBS;
using System.Diagnostics;

namespace RuniOS.Sounds
{
    public sealed partial class NBSPlayer
    {
        sealed class Voice
        (
            SoundChannel channel,
            int layer,
            long deadlineTimestamp,
            ulong startDspClock,
            bool isPendingReservation
        )
        {
            public readonly SoundChannel channel = channel;
            public readonly int layer = layer;
            public readonly long deadlineTimestamp = deadlineTimestamp;
            public readonly ulong startDspClock = startDspClock;
            public bool isPendingReservation = isPendingReservation;
        }

        readonly record struct DSPClockSnapshot(ulong dspClock, int sampleRate);
        readonly record struct DSPClockAnchor(long timestamp, ulong dspClock, int sampleRate);
        readonly record struct LoopInfo
        (
            double startTime,
            double endTime,
            double range,
            double startTick,
            double endTick
        );

        readonly object voiceLock = new object();
        readonly List<Voice> voices = [];

        readonly object spatialSnapshotLock = new object();
        AudioSpatialState spatialSnapshot;

#if UNITY_PHYSICS_EXIST
        Rigidbody? rigidbody;
#endif
#if UNITY_PHYSICS2D_EXIST
        Rigidbody2D? rigidbody2D;
#endif

        Vector3 lastSpatialPosition;

        internal void WorkerUpdate()
        {
            playingLock.EnterWriteLock();
            try
            {
                NBSFile? file = nbsScope?.asset;
                NBSInstrumentBank? bank = instrumentBank;
                float transportTempo = base.tempo;
                if (!isActiveAndEnabled || !isPlaying || isPaused || file == null || bank == null ||
                    !float.IsFinite(transportTempo) || transportTempo == 0)
                    return;

                long transportTimestampBefore = Stopwatch.GetTimestamp();
                double currentTime = base.time;
                long transportTimestampAfter = Stopwatch.GetTimestamp();
                if (!double.IsFinite(currentTime))
                    return;

                long transportTimestamp = GetMidpointTimestamp(transportTimestampBefore, transportTimestampAfter);

                long schedulingRevision = NBSPlaybackSettings.schedulingRevision;
                if (observedSchedulingRevision != schedulingRevision)
                {
                    CancelFutureReservationsUnsafe();
                    ResetCursorUnsafe(currentTime, false);
                }

                if (TryApplyLoopUnsafe(file, ref currentTime))
                    transportTimestamp = Stopwatch.GetTimestamp();

                double lateTolerance = NBSPlaybackSettings.lateTolerance;
                CancelStaleReservationsUnsafe(transportTimestamp, lateTolerance);
                ProcessScheduleUnsafe
                (
                    file,
                    bank,
                    currentTime,
                    transportTempo,
                    transportTimestamp,
                    NBSPlaybackSettings.schedulingLookahead,
                    lateTolerance
                );
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        void ProcessScheduleUnsafe
        (
            NBSFile file,
            NBSInstrumentBank bank,
            double currentTime,
            float transportTempo,
            long transportTimestamp,
            double schedulingLookahead,
            double lateTolerance
        )
        {
            int direction = transportTempo < 0 ? -1 : 1;
            bool hasLoop = TryGetLoopInfoUnsafe(file, out LoopInfo loopInfo);
            if (hasLoop && scheduledFileLoops < completedFileLoops)
            {
                scheduledFileLoops = completedFileLoops;
                SetScheduleLoopCursorUnsafe(file, loopInfo, direction);
            }

            DSPClockAnchor dspClockAnchor = default;
            bool hasDspClockAnchor = false;
            int loopSafety = 0;

            while (true)
            {
                if (nextTickIndex < 0 || nextTickIndex >= file.ticks.Count)
                {
                    if (!hasLoop || loopSafety++ >= 1024 ||
                        !TryAdvanceScheduleLoopUnsafe(file, loopInfo, direction))
                        return;

                    continue;
                }

                NBSTick tickColumn = file.ticks[nextTickIndex];
                double tickTime = file.tempoMap.TickToTime(tickColumn.tick);
                if (hasLoop && IsScheduleLoopBoundaryReached(tickColumn.tick, loopInfo, direction))
                {
                    if (loopSafety++ >= 1024 || !TryAdvanceScheduleLoopUnsafe(file, loopInfo, direction))
                        return;

                    continue;
                }

                double scheduleCurrentTime = hasLoop
                    ? currentTime + (((double)completedFileLoops - scheduledFileLoops) * direction * loopInfo.range)
                    : currentTime;
                if (!double.IsFinite(scheduleCurrentTime))
                    return;

                double wallDelay = (tickTime - scheduleCurrentTime) / transportTempo;

                if (wallDelay < -lateTolerance)
                {
                    SkipExpiredTickColumnsUnsafe(file, scheduleCurrentTime, transportTempo, lateTolerance, direction);
                    continue;
                }

                long deadline = AddStopwatchSeconds(transportTimestamp, wallDelay);
                if (wallDelay <= 0)
                {
                    ProcessTickColumnUnsafe(file, bank, tickColumn, deadline, null, lateTolerance);
                    nextTickIndex += direction;
                    continue;
                }

                if (schedulingLookahead <= 0 || wallDelay > schedulingLookahead)
                    return;

                if (!hasDspClockAnchor)
                {
                    if (!TryGetDSPClockAnchor(out dspClockAnchor))
                        return;

                    hasDspClockAnchor = true;
                }

                ulong startDspClock = ConvertToDSPClock(dspClockAnchor, deadline);
                ProcessTickColumnUnsafe(file, bank, tickColumn, deadline, startDspClock, lateTolerance);
                nextTickIndex += direction;
            }
        }

        void SkipExpiredTickColumnsUnsafe
        (
            NBSFile file,
            double currentTime,
            float transportTempo,
            double lateTolerance,
            int direction
        )
        {
            double oldestAllowedTime = currentTime - (lateTolerance * transportTempo);
            double oldestAllowedTick = file.tempoMap.TimeToTick(oldestAllowedTime);
            int oldIndex = nextTickIndex;
            int allowedIndex = direction < 0
                ? FindReverseCursor(file, oldestAllowedTick, true)
                : FindForwardCursor(file, oldestAllowedTick, true);

            nextTickIndex = direction < 0
                ? Math.Min(nextTickIndex, allowedIndex)
                : Math.Max(nextTickIndex, allowedIndex);

            if (nextTickIndex == oldIndex)
                nextTickIndex += direction;
        }

        void ProcessTickColumnUnsafe
        (
            NBSFile file,
            NBSInstrumentBank bank,
            NBSTick tickColumn,
            long deadlineTimestamp,
            ulong? scheduledDspClock,
            double lateTolerance
        )
        {
            foreach (NBSNote note in tickColumn.notes)
            {
                if (specialEventMap.TryGetValue((note.tick, note.layer), out NBSSpecialEvent specialEvent))
                {
                    if (specialEvent.kind == NBSSpecialEventKind.soundStop)
                        ApplySoundStopperUnsafe(specialEvent, scheduledDspClock);

                    continue;
                }

                if (note.velocity == 0 || note.layer < 0 || note.layer >= file.layers.Count ||
                    bank[note.instrument] is not { } binding)
                    continue;

                ScheduleNoteUnsafe(note, file.layers[note.layer], binding, deadlineTimestamp, scheduledDspClock, lateTolerance);
            }
        }

        void ScheduleNoteUnsafe
        (
            NBSNote note,
            NBSLayer layer,
            NBSInstrumentBank.InstrumentBinding binding,
            long deadlineTimestamp,
            ulong? scheduledDspClock,
            double lateTolerance
        )
        {
            long latestStartTimestamp = AddStopwatchSeconds(deadlineTimestamp, lateTolerance);
            if (Stopwatch.GetTimestamp() > latestStartTimestamp)
                return;

            WaveAudioClip clip = binding.clip;
            double semitones = ((note.key + binding.keyOffset) - 45) + (note.pitch / 100d);
            float noteVolume = note.velocity / 100f;
            float layerVolume = layer.volume / 100f;
            float finalVolume = noteVolume * layerVolume * base.volume;

            float combinedNbsPan = layer.panning == 100 ? note.panning : (layer.panning + note.panning) * 0.5f;
            float combinedPan = (combinedNbsPan - 100) / 100f;
            combinedPan = Mathf.Lerp(combinedPan, base.panStereo, Mathf.Abs(base.panStereo));

            SoundChannel? channel = null;
            double frequency = 0;
            try
            {
                bool playable = clip.Execute(validClip =>
                {
                    frequency = validClip.frequency * Math.Pow(2, semitones / 12d) * base.pitch;
                    if (!double.IsFinite(frequency) || frequency == 0 || frequency < float.MinValue || frequency > float.MaxValue)
                        return false;

                    channel = validClip.system.PlaySound(validClip, true);
                    return true;
                }, out bool clipResult) && clipResult;

                if (!playable || channel == null || channel.isDisposed)
                    return;

                SoundChannel activeChannel = channel;
                activeChannel.frequency = (float)frequency;
                activeChannel.volume = finalVolume;
                activeChannel.panStereo = combinedPan;
                activeChannel.spatialBlend = base.spatialBlend;
                activeChannel.dopplerLevel = base.dopplerLevel;
                activeChannel.spread = base.spread;
                activeChannel.minMaxDistance = (base.minDistance, base.maxDistance);
                activeChannel.rolloffMode = rolloffMode;

                lock (spatialSnapshotLock)
                    activeChannel.spatialState = spatialSnapshot;

                ulong startDspClock = GetImmediateParentClock(activeChannel);
                bool isPendingReservation = false;
                if (scheduledDspClock is { } targetDspClock && targetDspClock > startDspClock)
                {
                    startDspClock = targetDspClock;
                    isPendingReservation = true;
                    activeChannel.SetDelay(startDspClock);
                }

                if (activeChannel.isDisposed)
                    return;

                if (Stopwatch.GetTimestamp() > latestStartTimestamp)
                {
                    RemoveAndStopVoice(activeChannel);
                    return;
                }

                Voice voice = new Voice
                (
                    activeChannel,
                    note.layer,
                    deadlineTimestamp,
                    startDspClock,
                    isPendingReservation
                );

                bool attached = true;
                lock (voiceLock)
                {
                    activeChannel.onStop += OnVoiceStopped;
                    voices.Add(voice);

                    if (activeChannel.isDisposed)
                    {
                        voices.Remove(voice);
                        activeChannel.onStop -= OnVoiceStopped;
                        attached = false;
                    }
                }

                if (!attached)
                    return;

                activeChannel.UnPause();
            }
            catch (ObjectDisposedException)
            {
                if (channel != null)
                    RemoveAndStopVoice(channel);
            }
            catch (Exception exception)
            {
                if (channel != null)
                    RemoveAndStopVoice(channel);

                Debug.LogException(exception);
            }
        }

        void ApplySoundStopperUnsafe(NBSSpecialEvent specialEvent, ulong? scheduledDspClock)
        {
            Voice[] targets;
            lock (voiceLock)
            {
                targets = voices
                    .Where(x => x.layer >= specialEvent.startLayer && x.layer <= specialEvent.endLayer)
                    .ToArray();
            }

            foreach (Voice voice in targets)
            {
                try
                {
                    ulong parentClock = GetImmediateParentClock(voice.channel);
                    bool scheduled = scheduledDspClock > parentClock;
                    if (scheduled)
                        voice.channel.SetDelay(voice.startDspClock, scheduledDspClock!.Value);

                    if (!scheduled)
                        RemoveAndStopVoice(voice.channel);
                }
                catch (ObjectDisposedException)
                {
                    RemoveAndStopVoice(voice.channel);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    RemoveAndStopVoice(voice.channel);
                }
            }
        }

        bool TryGetLoopInfoUnsafe(NBSFile file, out LoopInfo loopInfo)
        {
            bool fileLoop = useFileLoopSettings && file.header.loopEnabled;
            if (!base.loop)
            {
                loopInfo = default;
                return false;
            }

            double loopStartTime = fileLoop ? file.tempoMap.TickToTime(file.header.loopStartTick) : base.loopStart;
            double loopEndTime = fileLoop ? file.duration : base.loopEnd.Clamp(loopStartTime, file.duration);
            double range = loopEndTime - loopStartTime;
            if (!double.IsFinite(range) || range <= 0)
            {
                loopInfo = default;
                return false;
            }

            double loopStartTick = fileLoop ? file.header.loopStartTick : file.tempoMap.TimeToTick(loopStartTime);
            double loopEndTick = fileLoop ? file.tickLength : file.tempoMap.TimeToTick(loopEndTime);
            loopInfo = new LoopInfo(loopStartTime, loopEndTime, range, loopStartTick, loopEndTick);
            return true;
        }

        bool TryApplyLoopUnsafe(NBSFile file, ref double currentTime)
        {
            if (!TryGetLoopInfoUnsafe(file, out LoopInfo loopInfo))
                return false;

            bool changed = false;
            int safety = 0;
            if (base.tempo >= 0)
            {
                while (currentTime >= loopInfo.endTime && safety++ < 1024)
                {
                    currentTime = loopInfo.startTime + (currentTime - loopInfo.endTime);
                    completedFileLoops++;
                    changed = true;
                }
            }
            else
            {
                while (currentTime <= loopInfo.startTime && safety++ < 1024)
                {
                    currentTime = loopInfo.endTime - (loopInfo.startTime - currentTime);
                    completedFileLoops++;
                    changed = true;
                }
            }

            if (changed)
                SyncInterpolatedTime(currentTime);

            return changed;
        }

        bool TryAdvanceScheduleLoopUnsafe(NBSFile file, LoopInfo loopInfo, int direction)
        {
            int nextLoopIndex = GetScheduleLoopCursor(file, loopInfo, direction);
            if (nextLoopIndex < 0 || nextLoopIndex >= file.ticks.Count)
                return false;

            if (!IsInsideScheduleLoop(file.ticks[nextLoopIndex].tick, loopInfo, direction))
                return false;

            scheduledFileLoops++;
            nextTickIndex = nextLoopIndex;
            return true;
        }

        void SetScheduleLoopCursorUnsafe(NBSFile file, LoopInfo loopInfo, int direction) =>
            nextTickIndex = GetScheduleLoopCursor(file, loopInfo, direction);

        static int GetScheduleLoopCursor(NBSFile file, LoopInfo loopInfo, int direction) => direction < 0
            ? FindReverseCursor(file, loopInfo.endTick, true)
            : FindForwardCursor(file, loopInfo.startTick, true);

        static bool IsScheduleLoopBoundaryReached(double tick, LoopInfo loopInfo, int direction) => direction < 0
            ? tick <= loopInfo.startTick
            : tick >= loopInfo.endTick;

        static bool IsInsideScheduleLoop(double tick, LoopInfo loopInfo, int direction) => direction < 0
            ? tick > loopInfo.startTick && tick <= loopInfo.endTick
            : tick >= loopInfo.startTick && tick < loopInfo.endTick;

        void CancelFutureReservationsUnsafe()
        {
            Voice[] snapshot;
            lock (voiceLock)
                snapshot = voices.Where(x => x.isPendingReservation).ToArray();

            foreach (Voice voice in snapshot)
            {
                try
                {
                    bool isFuture = !voice.channel.isDisposed &&
                                    voice.startDspClock > GetImmediateParentClock(voice.channel);
                    if (isFuture)
                        RemoveAndStopVoice(voice.channel);
                    else
                        voice.isPendingReservation = false;
                }
                catch (ObjectDisposedException)
                {
                    RemoveAndStopVoice(voice.channel);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        void CancelStaleReservationsUnsafe(long currentTimestamp, double lateTolerance)
        {
            long oldestAllowedTimestamp = AddStopwatchSeconds(currentTimestamp, -lateTolerance);
            List<Voice>? candidates = null;
            lock (voiceLock)
            {
                foreach (Voice voice in voices)
                {
                    if (!voice.isPendingReservation || voice.deadlineTimestamp >= oldestAllowedTimestamp)
                        continue;

                    candidates ??= [];
                    candidates.Add(voice);
                }
            }

            if (candidates == null)
                return;

            foreach (Voice voice in candidates)
            {
                try
                {
                    bool isFuture = !voice.channel.isDisposed &&
                                    voice.startDspClock > GetImmediateParentClock(voice.channel);
                    if (isFuture)
                        RemoveAndStopVoice(voice.channel);
                    else
                        voice.isPendingReservation = false;
                }
                catch (ObjectDisposedException)
                {
                    RemoveAndStopVoice(voice.channel);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        void StopAllVoicesUnsafe()
        {
            SoundChannel[] channels;
            lock (voiceLock)
                channels = voices.Select(x => x.channel).ToArray();

            foreach (SoundChannel channel in channels)
                RemoveAndStopVoice(channel);
        }

        void UpdateVoiceRolloffModeUnsafe(SoundRolloffMode value)
        {
            Voice[] snapshot;
            lock (voiceLock)
                snapshot = voices.ToArray();

            foreach (Voice voice in snapshot)
            {
                try
                {
                    voice.channel.rolloffMode = value;
                }
                catch (ObjectDisposedException)
                {
                    RemoveAndStopVoice(voice.channel);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        void RemoveAndStopVoice(SoundChannel channel)
        {
            lock (voiceLock)
            {
                int index = voices.FindIndex(x => ReferenceEquals(x.channel, channel));
                if (index >= 0)
                {
                    voices.RemoveAt(index);
                    channel.onStop -= OnVoiceStopped;
                }
            }

            try
            {
                channel.Stop();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void OnVoiceStopped(SoundChannel channel)
        {
            lock (voiceLock)
            {
                int index = voices.FindIndex(x => ReferenceEquals(x.channel, channel));
                if (index >= 0)
                {
                    voices.RemoveAt(index);
                    channel.onStop -= OnVoiceStopped;
                }
            }
        }

        void Update()
        {
            Vector3 position = transform.position;
            Vector3 velocity = Vector3.zero;

#if UNITY_PHYSICS_EXIST
            if (rigidbody != null)
                velocity = rigidbody.linearVelocity;
            else
#endif
#if UNITY_PHYSICS2D_EXIST
            if (rigidbody2D != null)
                velocity = rigidbody2D.linearVelocity;
            else
#endif
            if (nonRigidbodyVelocity)
            {
                float deltaTime = Kernel.deltaTime;
                if (float.IsFinite(deltaTime) && deltaTime > 0)
                    velocity = ((position - lastSpatialPosition) / deltaTime).ClampMagnitude(20);
            }

            if (!IsFinite(velocity))
                velocity = Vector3.zero;

            lastSpatialPosition = position;

            AudioSpatialState state = new AudioSpatialState(position, velocity, transform.rotation);
            lock (spatialSnapshotLock)
                spatialSnapshot = state;

            Voice[] snapshot;
            lock (voiceLock)
                snapshot = voices.ToArray();

            foreach (Voice voice in snapshot)
            {
                try
                {
                    voice.channel.spatialState = state;
                }
                catch (ObjectDisposedException)
                {
                    RemoveAndStopVoice(voice.channel);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        static ulong GetImmediateParentClock(SoundChannel channel)
        {
            (_, ulong parentClock) = channel.GetDSPClock();
            return parentClock;
        }

        static bool TryGetDSPClockAnchor(out DSPClockAnchor anchor)
        {
            DSPClockSnapshot snapshot = default;
            long timestampBefore = Stopwatch.GetTimestamp();
            bool success = SoundSystem.main.Execute(system =>
            {
                snapshot = new DSPClockSnapshot(system.GetMasterDSPClock(), system.outputSampleRate);
            });
            long timestampAfter = Stopwatch.GetTimestamp();

            if (!success || snapshot.sampleRate <= 0)
            {
                anchor = default;
                return false;
            }

            anchor = new DSPClockAnchor
            (
                GetMidpointTimestamp(timestampBefore, timestampAfter),
                snapshot.dspClock,
                snapshot.sampleRate
            );
            return true;
        }

        static long GetMidpointTimestamp(long before, long after) => before + ((after - before) / 2);

        static long AddStopwatchSeconds(long timestamp, double seconds)
        {
            double ticks = seconds * Stopwatch.Frequency;
            if (ticks >= (double)long.MaxValue - timestamp)
                return long.MaxValue;
            if (ticks <= (double)long.MinValue - timestamp)
                return long.MinValue;

            return timestamp + (long)Math.Round(ticks);
        }

        static ulong ConvertToDSPClock(DSPClockAnchor anchor, long timestamp)
        {
            double samples = (((double)timestamp - anchor.timestamp) / Stopwatch.Frequency) * anchor.sampleRate;
            if (samples >= ulong.MaxValue - anchor.dspClock)
                return ulong.MaxValue;

            if (samples >= 0)
                return anchor.dspClock + (ulong)Math.Round(samples);

            double magnitude = -samples;
            if (magnitude >= anchor.dspClock)
                return 0;

            return anchor.dspClock - (ulong)Math.Round(magnitude);
        }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
