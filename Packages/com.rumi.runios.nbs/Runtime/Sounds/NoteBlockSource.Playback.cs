#nullable enable
using RuniOS.NBS;
using System.Diagnostics;

namespace RuniOS.Sounds
{
    public sealed partial class NoteBlockSource
    {
        sealed class Voice(SoundChannel channel, NBSOccurrenceId occurrence, NBSPreparedNote preparedNote, float clipFrequency, ulong startDspClock, bool isPendingStart)
        {

            public readonly SoundChannel channel = channel;
            public readonly NBSOccurrenceId occurrence = occurrence;
            public readonly NBSPreparedNote preparedNote = preparedNote;
            public readonly float clipFrequency = clipFrequency;
            public readonly ulong startDspClock = startDspClock;
            public bool isPendingStart = isPendingStart;
            public Action<SoundChannel>? stopHandler;
        }

        readonly record struct PendingSubmission(ulong targetDspClock, NBSPlaybackCursor cursorBeforeMoment);

        readonly record struct DSPClockSnapshot(ulong dspClock, int sampleRate, long timestamp);

        readonly record struct VoiceCancellationPlan(Voice[]? voicesToStop, Voice[]? voicesToClearEndDelay);

        readonly record struct VoiceSettings
        (
            float pitch,
            float volume,
            float panStereo,
            float spatialBlend,
            float dopplerLevel,
            float spread,
            float minDistance,
            float maxDistance,
            SoundRolloffMode rolloffMode,
            AudioSpatialState spatialState,
            long revision
        );

        readonly object voiceLock = new object();
        readonly object voiceSettingsApplyLock = new object();
        readonly List<Voice> voices = [];
        readonly List<NBSPlaybackCommand> commandBuffer = [];
        readonly List<PendingSubmission> pendingSubmissionBuffer = [];

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
            playingLock.EnterReadLock();
            try
            {
                if (!isActiveAndEnabled || !isPlaying || isPaused || nbsScope == null || instrumentBank == null)
                    return;
            }
            finally
            {
                playingLock.ExitReadLock();
            }

            if (!TryGetDSPClockSnapshot(out DSPClockSnapshot dspSnapshot))
                return;

            NBSInstrumentBank? retainedBank = null;
            NBSFile? capturedFile;
            NBSPlaybackSchedule? capturedSchedule;
            NBSPlaybackCursor capturedCursor = default;
            NBSLoopInfo capturedLoopInfo = default;
            bool includePreviousNotes = false;
            long capturedRevision;
            long capturedSchedulingRevision = 0;
            long capturedCompletedLoops = 0;
            float capturedTempo;
            float capturedPitch;
            double capturedTime = 0;
            long transportTimestampBefore = 0;
            long transportTimestampAfter = 0;
            bool invalidTransport;
            bool invalidTime = false;
            bool scheduleMismatch = false;
            bool pitchChanged = false;
            bool schedulingRevisionChanged = false;
            playingLock.EnterReadLock();
            try
            {
                NBSFile? file = nbsScope?.asset;
                NBSInstrumentBank? bank = instrumentBank;
                float currentTempo = base.tempo;
                float currentPitch = base.pitch;
                if (!isActiveAndEnabled || !isPlaying || isPaused || file == null || bank == null)
                    return;

                NBSPlaybackSchedule? schedule = playbackSchedule;
                capturedFile = file;
                capturedSchedule = schedule;
                capturedRevision = playbackRevision;
                capturedTempo = currentTempo;
                capturedPitch = currentPitch;
                invalidTransport = !float.IsFinite(currentTempo) || currentTempo == 0 ||
                    !float.IsFinite(currentPitch) || currentPitch == 0;
                if (!invalidTransport)
                {
                    if (!bank.TryRetain())
                        return;

                    retainedBank = bank;
                    scheduleMismatch = schedule == null || !schedule.tempo.Equals(currentTempo) || !schedule.pitch.Equals(currentPitch);
                    pitchChanged = schedule != null && !schedule.pitch.Equals(currentPitch);
                    if (!scheduleMismatch)
                    {
                        transportTimestampBefore = Stopwatch.GetTimestamp();
                        capturedTime = base.time;
                        transportTimestampAfter = Stopwatch.GetTimestamp();
                        invalidTime = !double.IsFinite(capturedTime);
                        if (!invalidTime)
                        {
                            capturedCursor = playbackCursor;
                            capturedLoopInfo = GetLoopInfoUnsafe(file);
                            capturedCompletedLoops = completedLoops;
                            includePreviousNotes = restoreSnapshot;
                            capturedSchedulingRevision = NBSPlaybackSettings.schedulingRevision;
                            schedulingRevisionChanged = observedSchedulingRevision != capturedSchedulingRevision;
                        }
                    }
                }
            }
            finally
            {
                playingLock.ExitReadLock();
            }

            if (invalidTransport)
            {
                HandleInvalidWorkerTransport(capturedRevision, capturedTempo, capturedPitch, dspSnapshot);
                return;
            }
            if (retainedBank == null)
                return;

            try
            {
                if (invalidTime)
                    return;

                if (scheduleMismatch)
                {
                    RebuildWorkerSchedule
                    (
                        capturedFile,
                        retainedBank,
                        capturedSchedule,
                        capturedRevision,
                        capturedTempo,
                        capturedPitch,
                        pitchChanged,
                        dspSnapshot
                    );
                    return;
                }

                if (capturedSchedule == null)
                    return;

                long transportTimestamp = transportTimestampBefore + ((transportTimestampAfter - transportTimestampBefore) / 2);
                dspSnapshot = dspSnapshot with
                {
                    dspClock = GetDSPClockAtTimestamp(dspSnapshot, transportTimestamp),
                    timestamp = transportTimestamp
                };
                TryApplyLoop(ref capturedTime, ref capturedCompletedLoops, capturedTempo, capturedLoopInfo);
                CleanupStartedVoices(dspSnapshot.dspClock);

                if (schedulingRevisionChanged)
                {
                    ResetWorkerSchedulingRevision
                    (
                        retainedBank,
                        capturedSchedule,
                        capturedRevision,
                        dspSnapshot
                    );
                    return;
                }

                commandBuffer.Clear();
                NBSPlaybackQueryContext capturedContext = new NBSPlaybackQueryContext
                (
                    new NBSPlaybackPosition(capturedTime, capturedCompletedLoops),
                    NBSPlaybackSettings.schedulingLookahead,
                    capturedTempo,
                    capturedPitch,
                    capturedLoopInfo
                );
                capturedSchedule.Query
                (
                    capturedCursor,
                    capturedContext,
                    includePreviousNotes,
                    commandBuffer,
                    out NBSPlaybackCursor nextCursor
                );
                pendingSubmissionBuffer.Clear();
                BuildPendingSubmissions
                (
                    commandBuffer,
                    dspSnapshot,
                    capturedSchedule.direction,
                    pendingSubmissionBuffer
                );

                playingLock.EnterWriteLock();
                try
                {
                    if (!IsPlaybackContextCurrentUnsafe(capturedRevision, retainedBank, capturedSchedule) ||
                        NBSPlaybackSettings.schedulingRevision != capturedSchedulingRevision)
                        return;

                    if (completedLoops != capturedCompletedLoops)
                    {
                        completedLoops = capturedCompletedLoops;
                        SyncInterpolatedTime(capturedTime);
                    }
                    CleanupStartedPendingSubmissionsUnsafe(dspSnapshot.dspClock);
                    pendingSubmissions.AddRange(pendingSubmissionBuffer);
                    playbackCursor = nextCursor;
                    restoreSnapshot = false;
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                ProcessCommandsUnsafe(retainedBank, capturedSchedule, capturedRevision, commandBuffer, dspSnapshot);
            }
            finally
            {
                retainedBank.Release();
            }
        }

        void HandleInvalidWorkerTransport
        (
            long revision,
            float tempo,
            float pitch,
            DSPClockSnapshot dspSnapshot
        )
        {
            VoiceCancellationPlan cancellationPlan = default;
            Voice[] voicesToStop = [];
            bool changed;
            playingLock.EnterWriteLock();
            try
            {
                if (playbackRevision != revision || !base.tempo.Equals(tempo) || !base.pitch.Equals(pitch) ||
                    !isActiveAndEnabled || !isPlaying || isPaused)
                    return;
                if (playbackSchedule == null && !playbackCursor.initialized && pendingSubmissions.Count == 0)
                    return;

                if (!float.IsFinite(pitch) || pitch == 0)
                {
                    voicesToStop = DetachAllVoicesUnsafe();
                    pendingSubmissions.Clear();
                }
                else
                {
                    cancellationPlan = PrepareCancelFutureSubmissionsUnsafe(dspSnapshot.dspClock, true);
                }

                scheduleGeneration++;
                playbackSchedule = null;
                ResetCursorUnsafe(false);
                restoreSnapshot = false;
                changed = true;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            ExecuteCancellationPlan(cancellationPlan);
            StopVoices(voicesToStop);
            if (changed)
                NBSPlaybackWorker.Signal();
        }

        void RebuildWorkerSchedule
        (
            NBSFile file,
            NBSInstrumentBank bank,
            NBSPlaybackSchedule? expectedSchedule,
            long revision,
            float tempo,
            float pitch,
            bool pitchChanged,
            DSPClockSnapshot dspSnapshot
        )
        {
            NBSPlaybackSchedule preparedSchedule = file.playbackMap.CreateSchedule(tempo, pitch, bank);
            VoiceCancellationPlan cancellationPlan;
            Voice[] frequencySnapshot = [];
            bool changed;
            playingLock.EnterWriteLock();
            try
            {
                if (playbackRevision != revision || !ReferenceEquals(nbsScope?.asset, file) ||
                    !ReferenceEquals(instrumentBank, bank) || !ReferenceEquals(playbackSchedule, expectedSchedule) ||
                    !base.tempo.Equals(tempo) || !base.pitch.Equals(pitch) ||
                    !isActiveAndEnabled || !isPlaying || isPaused)
                    return;

                cancellationPlan = PrepareCancelFutureSubmissionsUnsafe(dspSnapshot.dspClock, true);
                if (pitchChanged)
                    frequencySnapshot = GetVoiceSnapshot();

                scheduleGeneration++;
                playbackSchedule = preparedSchedule;
                ResetCursorUnsafe(false);
                restoreSnapshot = false;
                changed = true;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            ExecuteCancellationPlan(cancellationPlan);
            if (pitchChanged)
                UpdateVoiceFrequenciesUnsafe(frequencySnapshot, pitch);
            if (changed)
                NBSPlaybackWorker.Signal();
        }

        void ResetWorkerSchedulingRevision
        (
            NBSInstrumentBank bank,
            NBSPlaybackSchedule schedule,
            long revision,
            DSPClockSnapshot dspSnapshot
        )
        {
            VoiceCancellationPlan cancellationPlan;
            bool changed;
            playingLock.EnterWriteLock();
            try
            {
                if (!IsPlaybackContextCurrentUnsafe(revision, bank, schedule) ||
                    observedSchedulingRevision == NBSPlaybackSettings.schedulingRevision)
                    return;

                cancellationPlan = PrepareCancelFutureSubmissionsUnsafe(dspSnapshot.dspClock, true);
                ResetCursorUnsafe(false);
                changed = true;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }

            ExecuteCancellationPlan(cancellationPlan);
            if (changed)
                NBSPlaybackWorker.Signal();
        }

        void ProcessCommandsUnsafe
        (
            NBSInstrumentBank bank,
            NBSPlaybackSchedule schedule,
            long revision,
            List<NBSPlaybackCommand> commands,
            DSPClockSnapshot dspSnapshot
        )
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (!IsPlaybackContextCurrent(revision, bank, schedule))
                    return;

                NBSPlaybackCommand command = commands[i];
                ulong targetClock = AddDSPSeconds(dspSnapshot.dspClock, dspSnapshot.sampleRate, command.wallDelay);
                try
                {
                    if (command.kind == NBSPlaybackCommandKind.note)
                        CreateVoiceUnsafe(bank, schedule, revision, command, dspSnapshot.dspClock, targetClock);
                    else
                        ApplySoundStopperUnsafe(command, dspSnapshot.dspClock, targetClock);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        static void BuildPendingSubmissions
        (
            List<NBSPlaybackCommand> commands,
            DSPClockSnapshot dspSnapshot,
            NBSPlaybackDirection direction,
            List<PendingSubmission> output
        )
        {
            long pendingLoop = long.MinValue;
            int pendingMoment = int.MinValue;
            for (int i = 0; i < commands.Count; i++)
            {
                NBSPlaybackCommand command = commands[i];
                if (command.wallDelay <= 0 ||
                    (command.occurrence.loopIteration == pendingLoop && command.occurrence.momentIndex == pendingMoment))
                    continue;

                NBSPlaybackCursor cursorBefore = new NBSPlaybackCursor
                {
                    momentIndex = command.occurrence.momentIndex,
                    loopIteration = command.occurrence.loopIteration,
                    scheduleGeneration = command.occurrence.scheduleGeneration,
                    direction = direction,
                    initialized = true
                };
                ulong targetClock = AddDSPSeconds(dspSnapshot.dspClock, dspSnapshot.sampleRate, command.wallDelay);
                output.Add(new PendingSubmission(targetClock, cursorBefore));
                pendingLoop = command.occurrence.loopIteration;
                pendingMoment = command.occurrence.momentIndex;
            }
        }

        bool IsPlaybackContextCurrent(long revision, NBSInstrumentBank bank, NBSPlaybackSchedule schedule)
        {
            playingLock.EnterReadLock();
            try
            {
                return IsPlaybackContextCurrentUnsafe(revision, bank, schedule);
            }
            finally
            {
                playingLock.ExitReadLock();
            }
        }

        bool IsPlaybackContextCurrentUnsafe(long revision, NBSInstrumentBank bank, NBSPlaybackSchedule schedule) =>
            playbackRevision == revision && ReferenceEquals(instrumentBank, bank) && ReferenceEquals(playbackSchedule, schedule) &&
            isActiveAndEnabled && isPlaying && !isPaused;

        void CreateVoiceUnsafe
        (
            NBSInstrumentBank bank,
            NBSPlaybackSchedule schedule,
            long revision,
            NBSPlaybackCommand command,
            ulong currentDspClock,
            ulong targetDspClock
        )
        {
            lock (voiceLock)
            {
                for (int i = 0; i < voices.Count; i++)
                {
                    if (voices[i].occurrence == command.occurrence)
                        return;
                }
            }

            NBSPreparedNote note = command.note;
            if (!bank.TryGetClip(note.instrument, out WaveAudioClip clip) || clip.samples == 0)
                return;

            double samplePosition = command.sourceOffset * clip.frequency;
            if (!double.IsFinite(samplePosition))
                return;
            uint sourceSample = samplePosition <= 0
                ? 0
                : samplePosition >= clip.samples - 1d
                    ? clip.samples - 1
                    : (uint)Math.Round(samplePosition);

            if (!clip.system.Execute((system, clip) => system.PlaySound(clip, true), clip, out SoundChannel? channel) || channel == null)
                return;

            try
            {
                channel.timeSample = sourceSample;

                ulong startClock = Math.Max(currentDspClock, targetDspClock);
                channel.SetDelay(startClock);
                Voice voice = new Voice
                (
                    channel,
                    command.occurrence,
                    note,
                    clip.frequency,
                    startClock,
                    startClock > currentDspClock
                );
                Action<SoundChannel> stopHandler = stoppedChannel => OnVoiceStopped(stoppedChannel, command.occurrence);
                voice.stopHandler = stopHandler;

                bool attached = false;
                while (true)
                {
                    if (!TryCaptureVoiceSettings(revision, bank, schedule, out VoiceSettings settings) ||
                        !ApplyVoiceSettings(channel, note, clip.frequency, settings))
                        break;

                    playingLock.EnterReadLock();
                    try
                    {
                        if (!IsPlaybackContextCurrentUnsafe(revision, bank, schedule))
                            break;
                        if (voiceSettingsRevision != settings.revision)
                            continue;

                        attached = true;
                        lock (voiceLock)
                        {
                            for (int i = 0; i < voices.Count; i++)
                            {
                                if (voices[i].occurrence == command.occurrence)
                                {
                                    attached = false;
                                    break;
                                }
                            }

                            if (attached)
                            {
                                channel.onStop += stopHandler;
                                voices.Add(voice);
                                if (channel.isDisposed)
                                {
                                    voices.Remove(voice);
                                    channel.onStop -= stopHandler;
                                    attached = false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        playingLock.ExitReadLock();
                    }

                    break;
                }

                if (!attached)
                {
                    StopChannel(channel);
                    return;
                }

                channel.UnPause();
            }
            catch
            {
                RemoveAndStopVoice(channel, command.occurrence);
                throw;
            }
        }

        bool TryCaptureVoiceSettings
        (
            long revision,
            NBSInstrumentBank bank,
            NBSPlaybackSchedule schedule,
            out VoiceSettings settings
        )
        {
            playingLock.EnterReadLock();
            try
            {
                if (!IsPlaybackContextCurrentUnsafe(revision, bank, schedule))
                {
                    settings = default;
                    return false;
                }

                AudioSpatialState state;
                lock (spatialSnapshotLock)
                    state = spatialSnapshot;
                settings = new VoiceSettings
                (
                    base.pitch,
                    base.volume,
                    base.panStereo,
                    base.spatialBlend,
                    base.dopplerLevel,
                    base.spread,
                    base.minDistance,
                    base.maxDistance,
                    rolloffMode,
                    state,
                    // ReSharper disable once InconsistentlySynchronizedField
                    voiceSettingsRevision
                );
                return true;
            }
            finally
            {
                playingLock.ExitReadLock();
            }
        }

        static bool ApplyVoiceSettings(SoundChannel channel, NBSPreparedNote note, float clipFrequency, VoiceSettings settings)
        {
            double frequency = clipFrequency * note.staticPitchRatio * Math.Abs((double)settings.pitch);
            if (settings.pitch < 0)
                frequency = -frequency;
            if (!double.IsFinite(frequency) || frequency == 0 || frequency < float.MinValue || frequency > float.MaxValue)
                return false;

            channel.frequency = (float)frequency;
            channel.volume = note.staticVolume * settings.volume;
            channel.panStereo = Mathf.Lerp(note.staticPan, settings.panStereo, Mathf.Abs(settings.panStereo));
            channel.spatialBlend = settings.spatialBlend;
            channel.dopplerLevel = settings.dopplerLevel;
            channel.spread = settings.spread;
            channel.minMaxDistance = (settings.minDistance, settings.maxDistance);
            channel.rolloffMode = settings.rolloffMode;
            channel.spatialState = settings.spatialState;
            return true;
        }

        void ApplySoundStopperUnsafe(NBSPlaybackCommand command, ulong currentDspClock, ulong targetDspClock)
        {
            Voice[] targets;
            lock (voiceLock)
            {
                List<Voice> result = [];
                for (int i = 0; i < voices.Count; i++)
                {
                    Voice voice = voices[i];
                    if (voice.occurrence.scheduleGeneration == command.occurrence.scheduleGeneration &&
                        voice.preparedNote.layer >= command.stopStartLayer && voice.preparedNote.layer <= command.stopEndLayer)
                        result.Add(voice);
                }
                targets = result.ToArray();
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Voice voice = targets[i];
                try
                {
                    if (targetDspClock > currentDspClock)
                        voice.channel.SetDelay(voice.startDspClock, targetDspClock);
                    else
                        RemoveAndStopVoice(voice.channel, voice.occurrence);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    RemoveAndStopVoice(voice.channel, voice.occurrence);
                }
            }
        }

        NBSLoopInfo GetLoopInfoUnsafe(NBSFile? file)
        {
            if (file == null || !base.loop)
                return default;

            bool useFileLoop = useFileLoopSettings && file.header.loopEnabled;
            double startTime;
            double endTime;
            if (useFileLoop)
            {
                startTime = file.tempoMap.TickToTime(file.header.loopStartTick);
                endTime = file.duration;
            }
            else
            {
                startTime = Math.Clamp(base.loopStart, 0, file.duration);
                endTime = Math.Clamp(base.loopEnd, startTime, file.duration);
            }

            double range = endTime - startTime;
            return double.IsFinite(startTime) && double.IsFinite(endTime) && range > 0
                ? new NBSLoopInfo(true, startTime, endTime)
                : default;
        }

        static void TryApplyLoop
        (
            ref double currentTime,
            ref long completedLoops,
            float tempo,
            NBSLoopInfo loopInfo
        )
        {
            if (!loopInfo.enabled || loopInfo.range <= 0)
                return;

            int safety = 0;
            if (tempo > 0)
            {
                while (currentTime >= loopInfo.endTime && loopInfo.CanUseIteration(completedLoops + 1) && safety++ < 1024)
                {
                    currentTime = loopInfo.startTime + (currentTime - loopInfo.endTime);
                    completedLoops++;
                }
            }
            else
            {
                while (currentTime <= loopInfo.startTime && loopInfo.CanUseIteration(completedLoops + 1) && safety++ < 1024)
                {
                    currentTime = loopInfo.endTime - (loopInfo.startTime - currentTime);
                    completedLoops++;
                }
            }
        }

        void CleanupStartedPendingSubmissionsUnsafe(ulong currentDspClock)
        {
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < pendingSubmissions.Count; readIndex++)
            {
                PendingSubmission submission = pendingSubmissions[readIndex];
                if (submission.targetDspClock > currentDspClock)
                    pendingSubmissions[writeIndex++] = submission;
            }

            if (writeIndex < pendingSubmissions.Count)
                pendingSubmissions.RemoveRange(writeIndex, pendingSubmissions.Count - writeIndex);
        }

        void CleanupStartedVoices(ulong currentDspClock)
        {
            lock (voiceLock)
            {
                for (int i = 0; i < voices.Count; i++)
                {
                    if (voices[i].isPendingStart && voices[i].startDspClock <= currentDspClock)
                        voices[i].isPendingStart = false;
                }
            }
        }

        void CancelFutureSubmissionsUnsafe()
        {
            bool hasClock = TryGetDSPClockSnapshot(out DSPClockSnapshot snapshot);
            ulong currentClock = hasClock ? snapshot.dspClock : 0;
            CancelFutureSubmissionsUnsafe(currentClock, hasClock);
        }

        void CancelFutureSubmissionsUnsafe(ulong currentClock, bool hasClock)
        {
            VoiceCancellationPlan cancellationPlan = PrepareCancelFutureSubmissionsUnsafe(currentClock, hasClock);
            ExecuteCancellationPlan(cancellationPlan);
        }

        VoiceCancellationPlan PrepareCancelFutureSubmissionsUnsafe(ulong currentClock, bool hasClock)
        {
            NBSPlaybackCursor? rewindCursor = null;
            for (int i = 0; i < pendingSubmissions.Count; i++)
            {
                PendingSubmission submission = pendingSubmissions[i];
                if (hasClock && submission.targetDspClock <= currentClock)
                    continue;

                rewindCursor ??= submission.cursorBeforeMoment;
            }

            Voice[] futureVoices;
            Voice[] activeVoices;
            lock (voiceLock)
            {
                List<Voice> futureResult = [];
                List<Voice> activeResult = [];
                for (int i = 0; i < voices.Count; i++)
                {
                    Voice voice = voices[i];
                    if (voice.isPendingStart && (!hasClock || voice.startDspClock > currentClock))
                    {
                        futureResult.Add(voice);
                        continue;
                    }

                    if (voice.isPendingStart)
                        voice.isPendingStart = false;
                    activeResult.Add(voice);
                }
                futureVoices = futureResult.ToArray();
                activeVoices = activeResult.ToArray();
            }

            pendingSubmissions.Clear();
            if (rewindCursor is { } cursor && cursor.scheduleGeneration == scheduleGeneration)
                playbackCursor = cursor;
            return new VoiceCancellationPlan(futureVoices, activeVoices);
        }

        void ExecuteCancellationPlan(VoiceCancellationPlan cancellationPlan)
        {
            Voice[]? voicesToStop = cancellationPlan.voicesToStop;
            if (voicesToStop != null)
            {
                for (int i = 0; i < voicesToStop.Length; i++)
                    RemoveAndStopVoice(voicesToStop[i].channel, voicesToStop[i].occurrence);
            }

            ClearVoiceEndDelays(cancellationPlan.voicesToClearEndDelay);
        }

        void ClearVoiceEndDelaysUnsafe() => ClearVoiceEndDelays(GetVoiceSnapshot());

        void ClearVoiceEndDelays(Voice[]? snapshot)
        {
            if (snapshot == null)
                return;

            for (int i = 0; i < snapshot.Length; i++)
            {
                Voice voice = snapshot[i];
                try
                {
                    voice.channel.SetDelay(voice.startDspClock);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        void PauseVoicesUnsafe() => ApplyToVoicesUnsafe(static voice => voice.channel.Pause());
        void UnPauseVoicesUnsafe() => ApplyToVoicesUnsafe(static voice => voice.channel.UnPause());

        void StopAllVoicesUnsafe()
        {
            Voice[] snapshot = DetachAllVoicesUnsafe();
            StopVoices(snapshot);
        }

        Voice[] DetachAllVoicesUnsafe()
        {
            lock (voiceLock)
            {
                Voice[] snapshot = voices.ToArray();
                voices.Clear();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    Voice voice = snapshot[i];
                    if (voice.stopHandler != null)
                        voice.channel.onStop -= voice.stopHandler;
                }
                return snapshot;
            }
        }

        static void StopVoices(Voice[] voicesToStop)
        {
            for (int i = 0; i < voicesToStop.Length; i++)
                StopChannel(voicesToStop[i].channel);
        }

        void UpdateVoiceFrequenciesUnsafe()
        {
            float playerPitch = base.pitch;
            UpdateVoiceFrequenciesUnsafe(GetVoiceSnapshot(), playerPitch);
        }

        void UpdateVoiceFrequenciesUnsafe(Voice[] snapshot, float playerPitch)
        {
            ApplyToVoicesUnsafe(snapshot, voice =>
            {
                double frequency = voice.clipFrequency * voice.preparedNote.staticPitchRatio * Math.Abs((double)playerPitch);
                if (playerPitch < 0)
                    frequency = -frequency;
                if (double.IsFinite(frequency) && frequency != 0 && frequency >= float.MinValue && frequency <= float.MaxValue)
                    voice.channel.frequency = (float)frequency;
            });
        }

        void UpdateVoiceVolumesUnsafe(Voice[] snapshot, float value) =>
            ApplyToVoicesUnsafe(snapshot, voice => voice.channel.volume = voice.preparedNote.staticVolume * value);

        void UpdateVoicePansUnsafe(Voice[] snapshot, float value) => ApplyToVoicesUnsafe(snapshot, voice =>
            voice.channel.panStereo = Mathf.Lerp(voice.preparedNote.staticPan, value, Mathf.Abs(value)));

        void UpdateVoiceRolloffModeUnsafe(Voice[] snapshot, SoundRolloffMode value) =>
            ApplyToVoicesUnsafe(snapshot, voice => voice.channel.rolloffMode = value);

        void SetSpatialProperty
        (
            float value,
            Action<SoundChannel, float> channelSetter,
            Action<NoteBlockSource, float> baseSetter
        )
        {
            lock (voiceSettingsApplyLock)
            {
                Voice[] snapshot;
                playingLock.EnterWriteLock();
                try
                {
                    baseSetter(this, value);
                    voiceSettingsRevision++;
                    snapshot = GetVoiceSnapshot();
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                ApplyToVoicesUnsafe(snapshot, voice => channelSetter(voice.channel, value));
            }
        }

        void SetBaseSpatialBlend(float value) => base.spatialBlend = value;
        void SetBaseDopplerLevel(float value) => base.dopplerLevel = value;
        void SetBaseSpread(float value) => base.spread = value;

        void SetDistanceProperty(float value, bool minimum)
        {
            lock (voiceSettingsApplyLock)
            {
                Voice[] snapshot;
                (float min, float max) distance;
                playingLock.EnterWriteLock();
                try
                {
                    if (minimum)
                        base.minDistance = value;
                    else
                        base.maxDistance = value;

                    voiceSettingsRevision++;
                    distance = (base.minDistance, base.maxDistance);
                    snapshot = GetVoiceSnapshot();
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }

                ApplyToVoicesUnsafe(snapshot, voice => voice.channel.minMaxDistance = distance);
            }
        }

        void ApplyToVoicesUnsafe(Action<Voice> action)
        {
            Voice[] snapshot = GetVoiceSnapshot();
            ApplyToVoicesUnsafe(snapshot, action);
        }

        static void ApplyToVoicesUnsafe(Voice[] snapshot, Action<Voice> action)
        {
            for (int i = 0; i < snapshot.Length; i++)
            {
                Voice voice = snapshot[i];
                try
                {
                    action(voice);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        Voice[] GetVoiceSnapshot()
        {
            lock (voiceLock)
                return voices.ToArray();
        }

        void RemoveAndStopVoice(SoundChannel channel, NBSOccurrenceId occurrence)
        {
            RemoveVoice(channel, occurrence);
            StopChannel(channel);
        }

        void RemoveVoice(SoundChannel channel, NBSOccurrenceId occurrence)
        {
            lock (voiceLock)
            {
                int index = voices.FindIndex(x => ReferenceEquals(x.channel, channel) && x.occurrence == occurrence);
                if (index < 0)
                    return;

                Voice voice = voices[index];
                voices.RemoveAt(index);
                if (voice.stopHandler != null)
                    channel.onStop -= voice.stopHandler;
            }
        }

        static void StopChannel(SoundChannel channel)
        {
            if (!channel.isDisposed)
                channel.Stop();
        }

        void OnVoiceStopped(SoundChannel channel, NBSOccurrenceId occurrence) => RemoveVoice(channel, occurrence);

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

            Voice[] snapshot = GetVoiceSnapshot();
            for (int i = 0; i < snapshot.Length; i++)
            {
                Voice voice = snapshot[i];
                if (voice.channel.isDisposed)
                {
                    RemoveVoice(voice.channel, voice.occurrence);
                    continue;
                }

                voice.channel.spatialState = state;
            }
        }

        static bool TryGetDSPClockSnapshot(out DSPClockSnapshot snapshot)
        {
            DSPClockSnapshot result = default;
            long timestampBefore = Stopwatch.GetTimestamp();
            bool success = SoundSystem.main.Execute(system =>
            {
                result = new DSPClockSnapshot(system.GetMasterDSPClock(), system.outputSampleRate, 0);
            });
            long timestampAfter = Stopwatch.GetTimestamp();
            snapshot = result with { timestamp = timestampBefore + ((timestampAfter - timestampBefore) / 2) };
            return success && result.sampleRate > 0;
        }

        static ulong GetDSPClockAtTimestamp(DSPClockSnapshot snapshot, long timestamp)
        {
            double samples = ((timestamp - snapshot.timestamp) / (double)Stopwatch.Frequency) * snapshot.sampleRate;
            if (samples >= 0)
            {
                if (samples >= ulong.MaxValue - snapshot.dspClock)
                    return ulong.MaxValue;
                return snapshot.dspClock + (ulong)Math.Round(samples);
            }

            double magnitude = -samples;
            if (magnitude >= snapshot.dspClock)
                return 0;
            return snapshot.dspClock - (ulong)Math.Round(magnitude);
        }

        static ulong AddDSPSeconds(ulong dspClock, int sampleRate, double seconds)
        {
            if (seconds <= 0)
                return dspClock;

            double samples = seconds * sampleRate;
            if (!double.IsFinite(samples) || samples >= ulong.MaxValue - dspClock)
                return ulong.MaxValue;
            return dspClock + (ulong)Math.Round(samples);
        }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
