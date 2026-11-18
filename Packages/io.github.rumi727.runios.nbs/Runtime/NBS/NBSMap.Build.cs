#nullable enable
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RuniOS.NBS
{
    public sealed partial class NBSMap
    {
        /// <summary>The lowest tempo Note Block Studio plays, in ticks per second.</summary>
        const double minimumTicksPerSecond = 0.25;

        readonly struct TickLayerComparer : IComparer<NBSMapNote>
        {
            public int Compare(NBSMapNote x, NBSMapNote y)
            {
                int tick = x.tick.CompareTo(y.tick);
                return tick != 0 ? tick : x.layer.CompareTo(y.layer);
            }
        }

        /*
         * Notes read from a file are already sorted and edits usually move only a few notes,
         * so an insertion sort finishes in about one pass. Its cost grows with the distance the notes move,
         * so once the moves exceed what an introsort would cost, the remaining work switches to Span.Sort.
         */
        static void SortByTickAndLayer(NBSMapNote[] notes)
        {
            TickLayerComparer comparer = default;
            long budget = (long)notes.Length * (BitOperations.Log2((uint)Math.Max(notes.Length, 1)) + 1);
            long moves = 0;

            for (int i = 1; i < notes.Length; i++)
            {
                NBSMapNote current = notes[i];
                int j = i - 1;
                while (j >= 0 && comparer.Compare(notes[j], current) > 0)
                {
                    notes[j + 1] = notes[j];
                    j--;
                    moves++;
                }

                notes[j + 1] = current;
                if (moves > budget)
                {
                    notes.AsSpan().Sort(comparer);
                    return;
                }
            }
        }

        // Follows update_tempo_changes of Note Block Studio: every tempo changer counts, regardless of the layer lock.
        NBSTempoMap CreateTempoMap(NBSHeader header, NBSMapNote[] sorted)
        {
            List<(int tick, double ticksPerSecond)> changes = [];
            for (int i = 0; i < sorted.Length; i++)
            {
                NBSNote note = sorted[i].note;
                if (GetFunction(note.instrument) == NBSInstrumentFunction.tempoChanger)
                    changes.Add((note.tick, Math.Max(minimumTicksPerSecond, Math.Abs((int)note.pitch) / 15d)));
            }

            double initialTicksPerSecond = double.IsFinite(header.ticksPerSecond) ? Math.Max(minimumTicksPerSecond, header.ticksPerSecond) : minimumTicksPerSecond;
            return new NBSTempoMap(initialTicksPerSecond, CollectionsMarshal.AsSpan(changes));
        }

        ImmutableArray<NBSMapColumn> AssignTimes(NBSMapNote[] sorted, out ImmutableArray<NBSSoundStopper> soundStoppers)
        {
            ReadOnlySpan<NBSTempoChange> changes = tempo.changes.AsSpan();
            List<NBSMapColumn> columnList = [];
            List<NBSSoundStopper> stopperList = [];

            int segment = 0;
            int start = 0;
            while (start < sorted.Length)
            {
                int tick = sorted[start].tick;
                while (segment + 1 < changes.Length && changes[segment + 1].tick <= tick)
                    segment++;

                NBSTempoChange change = changes[segment];
                double time = change.time + ((tick - change.tick) / change.ticksPerSecond);

                int end = start;
                while (end < sorted.Length && sorted[end].tick == tick)
                {
                    sorted[end] = sorted[end] with { time = time };
                    if (GetFunction(sorted[end].note.instrument) == NBSInstrumentFunction.soundStopper)
                        stopperList.Add(NBSSoundStopper.FromNote(end, sorted[end].note, time));

                    end++;
                }

                columnList.Add(new NBSMapColumn(tick, time, start, end - start));
                start = end;
            }

            soundStoppers = ImmutableCollectionsMarshal.AsImmutableArray(stopperList.ToArray());
            return ImmutableCollectionsMarshal.AsImmutableArray(columnList.ToArray());
        }

        /*
         * Sweeps the song backwards while remembering, per layer, the closest stopper ahead.
         * Inside a column the sweep goes from the highest layer down: Note Block Studio processes a column
         * from the lowest layer up, so a stopper stops the lower layers of its own column but not the higher ones.
         */
        static void AssignNextStopTimes(NBSMapNote[] sorted, ImmutableArray<NBSSoundStopper> stoppers)
        {
            if (stoppers.IsEmpty)
                return;

            int maximumLayer = 0;
            for (int i = 0; i < sorted.Length; i++)
                maximumLayer = Math.Max(maximumLayer, sorted[i].layer);

            double[] nextByLayer = new double[maximumLayer + 1];
            nextByLayer.AsSpan().Fill(double.PositiveInfinity);
            double nextForAll = double.PositiveInfinity;

            int stopperIndex = stoppers.Length - 1;
            for (int i = sorted.Length - 1; i >= 0; i--)
            {
                if (stopperIndex >= 0 && stoppers[stopperIndex].mapIndex == i)
                {
                    NBSSoundStopper stopper = stoppers[stopperIndex--];
                    if (stopper.startLayer == 0)
                        nextForAll = stopper.time;
                    else
                    {
                        int last = Math.Min(stopper.endLayer - 1, maximumLayer);
                        for (int layer = stopper.startLayer - 1; layer <= last; layer++)
                            nextByLayer[layer] = stopper.time;
                    }

                    continue;
                }

                sorted[i] = sorted[i] with { nextStopTime = Math.Min(nextForAll, nextByLayer[sorted[i].layer]) };
            }
        }

        static void CreateLayerIndex(NBSMapNote[] sorted, out int[] starts, out int[] indexes)
        {
            int maximumLayer = -1;
            for (int i = 0; i < sorted.Length; i++)
                maximumLayer = Math.Max(maximumLayer, sorted[i].layer);

            starts = new int[maximumLayer + 2];
            for (int i = 0; i < sorted.Length; i++)
                starts[sorted[i].layer + 1]++;

            for (int layer = 1; layer < starts.Length; layer++)
                starts[layer] += starts[layer - 1];

            indexes = new int[sorted.Length];
            int[] next = starts.AsSpan(0, starts.Length - 1).ToArray();
            for (int i = 0; i < sorted.Length; i++)
                indexes[next[sorted[i].layer]++] = i;
        }
    }
}
