#nullable enable
using System.Diagnostics;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    public sealed partial class NBSVoiceScheduler<TTransport>
    {
        /// <summary>Records one sound handed to FMOD: what it plays and the clocks it was scheduled with.</summary>
        sealed class Voice(SoundChannel channel, int mapIndex, WaveAudioClip clip, double ratio, ulong startClock, ulong endClock)
        {
            public readonly SoundChannel channel = channel;
            public readonly int mapIndex = mapIndex;
            public readonly WaveAudioClip clip = clip;
            public readonly double ratio = ratio;
            public ulong startClock = startClock;
            public ulong endClock = endClock;

            /// <summary>The timeline distance to the stop point while the end clock is released, or <see cref="double.NaN"/>.</summary>
            public double heldEndDistance = double.NaN;

            public Action<SoundChannel>? stopHandler;
        }

        /// <summary>The DSP clock read once per operation, aligned with the transport position read right after it.</summary>
        readonly record struct ClockSnapshot(ulong clock, int sampleRate, double position, double positionClock)
        {
            public double ToClock(double distance, double absoluteTempo) => positionClock + ((distance / absoluteTempo) * sampleRate);

            public double ToDistance(double clockValue, double absoluteTempo) => ((clockValue - positionClock) / sampleRate) * absoluteTempo;
        }

        bool TryTakeClockSnapshot(out ClockSnapshot snapshot)
        {
            long clockBefore = Stopwatch.GetTimestamp();
            bool success = system.Execute(static soundSystem => (clock: soundSystem.GetMasterDSPClock(), sampleRate: soundSystem.outputSampleRate), out (ulong clock, int sampleRate) result);
            long clockAfter = Stopwatch.GetTimestamp();
            (ulong clock, int sampleRate) = result;

            long positionBefore = Stopwatch.GetTimestamp();
            double position = transport.time;
            long positionAfter = Stopwatch.GetTimestamp();

            double clockTimestamp = (clockBefore + clockAfter) * 0.5;
            double positionTimestamp = (positionBefore + positionAfter) * 0.5;
            double positionClock = clock + (((positionTimestamp - clockTimestamp) / Stopwatch.Frequency) * sampleRate);

            snapshot = new ClockSnapshot(clock, sampleRate, position, positionClock);
            return success && sampleRate > 0 && double.IsFinite(position);
        }

        static ulong ToClock(double value) => value <= 0 ? 0 : value >= ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Round(value);

        void CreateVoiceUnsafe(in NBSPlannedVoice plan, in NBSPlaybackRate rate, in ClockSnapshot snapshot)
        {
            WaveAudioClip? clip = timeline.GetClip(plan.mapIndex);
            if (clip == null || clip.isDisposed)
                return;

            double ratio = timeline.GetRatio(plan.mapIndex);
            double sourceScale = plan.placement.sourceScale;
            bool forwardSource = plan.placement.startsAtAttack;
            double sourceLength = plan.length * sourceScale;

            // A late start skips the part of the sound that should already have been heard.
            double sourceTime = plan.sourceTime;
            if (plan.distance < 0)
                sourceTime += (forwardSource ? -plan.distance : plan.distance) * sourceScale;

            double remainingSource = forwardSource ? sourceLength - sourceTime : sourceTime;
            if (!(remainingSource > 0))
                return;

            double absolutePitch = Math.Abs((double)rate.pitch);
            ulong startClock = Math.Max(snapshot.clock, ToClock(snapshot.ToClock(Math.Max(0, plan.distance), Math.Abs((double)rate.tempo))));
            ulong endClock = plan.stopped && forwardSource
                ? ToClock(startClock + ((remainingSource / (ratio * absolutePitch)) * snapshot.sampleRate))
                : 0;

            if (!system.Execute(static (soundSystem, sound) => soundSystem.PlaySound(sound, true), clip, out SoundChannel? channel) || channel == null)
                return;

            Voice voice = new Voice(channel, plan.mapIndex, clip, ratio, startClock, endClock);
            try
            {
                channel.frequency = (float)(clip.frequency * ratio * absolutePitch * Math.Sign(rate.pitch));
                channel.timeSample = (uint)Math.Clamp(Math.Round(sourceTime * clip.frequency), 0, clip.samples - 1d);
                ApplyMix(voice);
                channel.SetDelay(startClock, endClock);

                voice.stopHandler = stopped => RemoveVoice(stopped);
                lock (voiceLock)
                    voices.Add(voice);

                channel.onStop += voice.stopHandler;
                channel.UnPause();
            }
            catch (ObjectDisposedException)
            {
                RemoveVoice(channel);
            }
        }

        void RemoveVoice(SoundChannel channel)
        {
            lock (voiceLock)
            {
                for (int i = 0; i < voices.Count; i++)
                {
                    if (voices[i].channel == channel)
                    {
                        voices.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        /// <summary>Copies the voices so FMOD calls never run under <see cref="voiceLock"/>, which the stop callback takes.</summary>
        List<Voice> SnapshotVoicesUnsafe()
        {
            voiceSnapshot.Clear();
            lock (voiceLock)
                voiceSnapshot.AddRange(voices);

            return voiceSnapshot;
        }

        void StopVoice(Voice voice)
        {
            if (voice.stopHandler != null)
                voice.channel.onStop -= voice.stopHandler;

            RemoveVoice(voice.channel);
            voice.channel.Stop();
        }

        void StopAllVoicesUnsafe()
        {
            List<Voice> snapshot = SnapshotVoicesUnsafe();
            for (int i = 0; i < snapshot.Count; i++)
                StopVoice(snapshot[i]);
        }

        void CancelPendingVoicesUnsafe(ulong now)
        {
            List<Voice> snapshot = SnapshotVoicesUnsafe();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].startClock > now)
                    StopVoice(snapshot[i]);
            }
        }

        void SetFrequency(Voice voice, float pitch)
        {
            voice.channel.frequency = (float)(voice.clip.frequency * voice.ratio * Math.Abs((double)pitch) * Math.Sign(pitch));
        }
    }
}
