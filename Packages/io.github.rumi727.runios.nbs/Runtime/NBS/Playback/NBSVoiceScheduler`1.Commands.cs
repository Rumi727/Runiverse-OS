#nullable enable
namespace RuniOS.NBS
{
    public sealed partial class NBSVoiceScheduler<TTransport>
    {
        /// <summary>
        /// Stops every sound and restarts from the current transport position, restoring the sounds that should already be ringing there.<br/>
        /// 모든 소리를 멈추고 현재 트랜스포트 위치에서 다시 시작하며, 그 위치에서 이미 울리고 있어야 하는 소리를 복원합니다.
        /// </summary>
        /// <remarks>
        /// Call this after a seek, after playback starts, and after the clip map changes.<br/>
        /// seek 후, 재생 시작 후, 클립 맵이 바뀐 후에 호출합니다.
        /// </remarks>
        public void Resync()
        {
            NBSPlaybackRate rate = ReadRate();
            NBSLoopRange? loop = ReadLoop();
            TryTakeClockSnapshot(rate, loop, out ClockSnapshot snapshot);

            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                if (snapshot.sampleRate > 0)
                    ResyncUnsafe(rate, loop, snapshot);
                else
                {
                    StopAllVoicesUnsafe();
                    hasCursor = false;
                }
            }

            NBSPlaybackWorker.Signal();
        }

        /// <summary>
        /// Applies a tempo, pitch, or <see cref="reversePitchAlignment"/> change.<br/>
        /// tempo, pitch, 또는 <see cref="reversePitchAlignment"/> 변경을 적용합니다.
        /// </summary>
        /// <remarks>
        /// <para>Sounds that already started keep playing at their sound position; only their frequency and stop point follow the new rate.</para>
        /// <para>Scheduled sounds that have not started are moved in time when their start does not depend on the note length. Otherwise they are scheduled again.</para>
        /// <para>A pitch of <c>0</c> stops every sound, and leaving it restores the current position as <see cref="Resync"/> does. A tempo of <c>0</c> only stops new scheduling.</para>
        /// <br/><br/>
        /// <para>이미 시작한 소리는 소리 위치를 유지한 채 계속 재생되며, frequency와 멈추는 지점만 새 속도를 따릅니다.</para>
        /// <para>아직 시작하지 않은 예약 소리는 시작 시점이 노트 길이에 의존하지 않으면 시간만 옮기고, 그렇지 않으면 다시 예약합니다.</para>
        /// <para>pitch가 <c>0</c>이면 모든 소리를 멈추고, 0에서 벗어나면 <see cref="Resync"/>처럼 현재 위치를 복원합니다. tempo가 <c>0</c>이면 새 예약만 멈춥니다.</para>
        /// </remarks>
        public void OnRateChanged()
        {
            NBSPlaybackRate rate = ReadRate();
            NBSLoopRange? loop = ReadLoop();
            if (!TryTakeClockSnapshot(rate, loop, out ClockSnapshot snapshot))
                return;

            // The transport moved its anchor to where the tempo changed, so the sounds after it are retimed from there.
            // Without a tempo change, the scale below is 1 and the reference does not matter.
            ulong changeClock = Math.Min(transport.clockAnchor.dspClock, snapshot.clock);

            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                NBSPlaybackRate previous = committedRate;
                committedRate = rate;

                if (rate.pitch == 0 || !float.IsFinite(rate.pitch))
                {
                    StopAllVoicesUnsafe();
                    hasCursor = false;
                    return;
                }

                if (previous.pitch == 0 || !float.IsFinite(previous.pitch))
                {
                    ResyncUnsafe(rate, loop, snapshot);
                    return;
                }

                double previousTempo = Math.Abs((double)previous.tempo);
                double tempo = Math.Abs((double)rate.tempo);
                bool keepsSchedule = previousTempo > 0 && tempo > 0 &&
                    Math.Sign(previous.tempo) == Math.Sign(rate.tempo) &&
                    Math.Sign(previous.pitch) == Math.Sign(rate.pitch) &&
                    previous.alignment == rate.alignment &&
                    !rate.entryDependsOnLength;

                // While paused, stop points stay released until UnPause.
                bool paused = transport.isPaused;

                List<Voice> snapshotVoices = SnapshotVoicesUnsafe();
                for (int i = 0; i < snapshotVoices.Count; i++)
                {
                    Voice voice = snapshotVoices[i];
                    try
                    {
                        if (voice.startClock > snapshot.clock && !keepsSchedule)
                        {
                            StopVoice(voice);
                            continue;
                        }

                        SetFrequency(voice, rate.pitch);
                        if (voice.startClock > snapshot.clock)
                            RetimePendingVoice(voice, rate, changeClock, snapshot.sampleRate, previousTempo, tempo);
                        else if (!paused)
                            RetimeStopPoint(voice, changeClock, snapshot.sampleRate, previousTempo, tempo);
                    }
                    catch (ObjectDisposedException)
                    {
                        RemoveVoice(voice.channel);
                    }
                }

                if (tempo == 0)
                    hasCursor = false;
                else if (keepsSchedule && hasCursor)
                    cursorClock = changeClock + (((cursorClock - changeClock) * previousTempo) / tempo);
                else
                {
                    cursorClock = snapshot.clock;
                    hasCursor = true;
                }
            }

            NBSPlaybackWorker.Signal();
        }

        /// <summary>
        /// Pauses the sounds at their current sound position and cancels the scheduled sounds that have not started.<br/>
        /// 소리를 현재 소리 위치에서 일시 정지하고 아직 시작하지 않은 예약 소리를 취소합니다.
        /// </summary>
        public void Pause()
        {
            if (!TryTakeClockSnapshot(ReadRate(), ReadLoop(), out ClockSnapshot snapshot))
                return;

            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                double tempo = Math.Abs((double)committedRate.tempo);

                List<Voice> snapshotVoices = SnapshotVoicesUnsafe();
                for (int i = 0; i < snapshotVoices.Count; i++)
                {
                    Voice voice = snapshotVoices[i];
                    try
                    {
                        if (voice.startClock > snapshot.clock)
                        {
                            StopVoice(voice);
                            continue;
                        }

                        voice.channel.Pause();

                        // The DSP clock keeps running while a channel is paused, so a pending stop point would fire during the pause.
                        if (voice.endClock != 0)
                        {
                            voice.heldEndDistance = tempo > 0 ? (((double)voice.endClock - snapshot.clock) / snapshot.sampleRate) * tempo : voice.heldEndDistance;
                            voice.endClock = 0;
                            voice.channel.SetDelay(voice.startClock);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        RemoveVoice(voice.channel);
                    }
                }

                hasCursor = false;
            }
        }

        /// <summary>
        /// Resumes the paused sounds from their sound position and continues scheduling from the current transport position.<br/>
        /// 일시 정지된 소리를 소리 위치에서 재개하고 현재 트랜스포트 위치부터 예약을 이어 갑니다.
        /// </summary>
        public void UnPause()
        {
            if (!TryTakeClockSnapshot(ReadRate(), ReadLoop(), out ClockSnapshot snapshot))
                return;

            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                double tempo = Math.Abs((double)committedRate.tempo);

                List<Voice> snapshotVoices = SnapshotVoicesUnsafe();
                for (int i = 0; i < snapshotVoices.Count; i++)
                {
                    Voice voice = snapshotVoices[i];
                    try
                    {
                        if (tempo > 0 && double.IsFinite(voice.heldEndDistance))
                        {
                            voice.endClock = ToClock(snapshot.clock + ((voice.heldEndDistance / tempo) * snapshot.sampleRate));
                            voice.heldEndDistance = double.NaN;
                            voice.channel.SetDelay(voice.startClock, voice.endClock);
                        }

                        voice.channel.UnPause();
                    }
                    catch (ObjectDisposedException)
                    {
                        RemoveVoice(voice.channel);
                    }
                }

                // Scheduling resumes from where the transport anchored the resume.
                cursorClock = Math.Min(transport.clockAnchor.dspClock, snapshot.clock);
                hasCursor = tempo > 0;
            }

            NBSPlaybackWorker.Signal();
        }

        /// <summary>
        /// Stops every sound.<br/>
        /// 모든 소리를 멈춥니다.
        /// </summary>
        public void Stop()
        {
            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                StopAllVoicesUnsafe();
                hasCursor = false;
            }
        }

        /// <summary>
        /// Applies a loop change. Sounds that already started keep ringing, and the sounds that have not started are scheduled again.<br/>
        /// 반복 변경을 적용합니다. 이미 시작한 소리는 계속 울리고, 아직 시작하지 않은 소리는 다시 예약합니다.
        /// </summary>
        public void OnLoopChanged()
        {
            if (!TryTakeClockSnapshot(ReadRate(), ReadLoop(), out ClockSnapshot snapshot))
                return;

            lock (gate)
            {
                if (isDisposed)
                    return;

                generation++;
                // The new range applies from where the transport anchored the change.
                ulong changeClock = Math.Min(transport.clockAnchor.dspClock, snapshot.clock);
                CancelPendingVoicesUnsafe(changeClock);
                cursorClock = changeClock;
                hasCursor = hasCursor && committedRate.tempo != 0;
            }

            NBSPlaybackWorker.Signal();
        }

        /*
         * A sound that has not started keeps its timeline distance to the start, so only the wall time follows the new tempo.
         * Its placement is computed again, because the sound position at the start and the stop point depend on the rate:
         * a sound shortened by a stopper starts at its cut point when reversed.
         */
        void RetimePendingVoice(Voice voice, in NBSPlaybackRate rate, ulong changeClock, int sampleRate, double previousTempo, double tempo)
        {
            double length = timeline.GetLength(voice.mapIndex, rate, out bool stopped);
            if (!(length > 0))
            {
                StopVoice(voice);
                return;
            }

            NBSNotePlacement placement = timeline.GetPlacement(voice.mapIndex, length, rate);
            double sourceTime = placement.GetSourceTime(voice.startPosition);
            double remainingSource = placement.startsAtAttack ? (length * placement.sourceScale) - sourceTime : sourceTime;
            if (!(remainingSource > 0))
            {
                StopVoice(voice);
                return;
            }

            double distance = (((double)voice.startClock - changeClock) / sampleRate) * previousTempo;
            voice.startClock = ToClock(changeClock + ((distance / tempo) * sampleRate));
            voice.endClock = stopped && placement.startsAtAttack
                ? ToClock(voice.startClock + ((Math.Abs(placement.exit - voice.startPosition) / tempo) * sampleRate))
                : 0;

            voice.channel.timeSample = (uint)Math.Clamp(Math.Round(sourceTime * voice.clip.frequency), 0, voice.clip.samples - 1d);
            voice.channel.SetDelay(voice.startClock, voice.endClock);
        }

        void RetimeStopPoint(Voice voice, ulong changeClock, int sampleRate, double previousTempo, double tempo)
        {
            // The stop point is a fixed timeline position, so its remaining distance is kept while the clock time follows the tempo.
            double distance = double.IsFinite(voice.heldEndDistance)
                ? voice.heldEndDistance
                : voice.endClock != 0 && previousTempo > 0 ? (((double)voice.endClock - changeClock) / sampleRate) * previousTempo : double.NaN;

            if (!double.IsFinite(distance))
                return;

            if (tempo == 0)
            {
                voice.heldEndDistance = distance;
                voice.endClock = 0;
            }
            else
            {
                voice.heldEndDistance = double.NaN;
                voice.endClock = ToClock(changeClock + ((distance / tempo) * sampleRate));
            }

            voice.channel.SetDelay(voice.startClock, voice.endClock);
        }
    }
}
