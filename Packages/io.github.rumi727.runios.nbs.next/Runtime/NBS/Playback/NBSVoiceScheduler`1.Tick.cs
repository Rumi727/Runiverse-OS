#nullable enable
using System.Threading;

namespace RuniOS.NBS
{
    public sealed partial class NBSVoiceScheduler<TTransport>
    {
        void NBSPlaybackWorker.IClient.Tick()
        {
            // The transport is read before the gate to keep the lock order transport -> scheduler.
            // A command that runs in between bumps the generation, and this pass is skipped.
            long observedGeneration = Volatile.Read(ref generation);
            if (!transport.isPlaying || transport.isPaused)
                return;

            NBSPlaybackRate rate = ReadRate();
            NBSLoopRange? loop = ReadLoop();
            if (!rate.canPlace || !TryTakeClockSnapshot(rate, loop, out ClockSnapshot snapshot))
                return;

            lock (gate)
            {
                if (isDisposed || observedGeneration != generation)
                    return;

                if (!ReferenceEquals(clip.map, timeline.map) || !hasCursor)
                {
                    ResyncUnsafe(rate, loop, snapshot);
                    return;
                }

                double absoluteTempo = Math.Abs((double)rate.tempo);
                double fromDistance = snapshot.ToDistance(cursorClock, absoluteTempo);
                double toDistance = NBSPlaybackSettings.schedulingLookahead * absoluteTempo;
                if (toDistance <= fromDistance)
                    return;

                ScheduleUnsafe(rate, loop, snapshot, fromDistance, false, toDistance);
            }
        }

        void ScheduleUnsafe(in NBSPlaybackRate rate, NBSLoopRange? loop, in ClockSnapshot snapshot, double fromDistance, bool includeFrom, double toDistance)
        {
            planned.Clear();
            timeline.Query(snapshot.position, rate, loop, fromDistance, includeFrom, toDistance, planned);
            for (int i = 0; i < planned.Count; i++)
                CreateVoiceUnsafe(planned[i], rate, snapshot);

            cursorClock = snapshot.ToClock(toDistance, Math.Abs((double)rate.tempo));
            hasCursor = true;
        }

        void ResyncUnsafe(in NBSPlaybackRate rate, NBSLoopRange? loop, in ClockSnapshot snapshot)
        {
            StopAllVoicesUnsafe();
            hasCursor = false;
            committedRate = rate;

            NBSMap map = clip.map;
            if (!ReferenceEquals(map, timeline.map))
                timeline = new NBSTimeline(map, bank);

            if (!transport.isPlaying || transport.isPaused || !rate.canPlace)
                return;

            planned.Clear();
            timeline.QueryActive(snapshot.position, rate, loop, planned);
            for (int i = 0; i < planned.Count; i++)
                CreateVoiceUnsafe(planned[i], rate, snapshot);

            ScheduleUnsafe(rate, loop, snapshot, 0, true, NBSPlaybackSettings.schedulingLookahead * Math.Abs((double)rate.tempo));
        }
    }
}
