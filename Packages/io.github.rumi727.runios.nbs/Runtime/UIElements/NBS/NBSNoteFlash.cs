#nullable enable
using RuniOS.NBS;

namespace RuniOS.UIElements.NBS
{
    // Flashes the blocks of NBSNoteGridView as their notes play, like Note Block Studio: a played block adds up to 0.5 to its alpha, which falls back linearly over one second.
    // The time a note played is kept in NBSNoteGridItem.playedAt, so a block made while its note still flashes continues the flash.
    internal sealed class NBSNoteFlash
    {
        const double duration = 1;

        // Every note sorted by tick, so the notes that play between two ticks are found with a binary search.
        readonly List<NBSNoteGridItem> items = [];

        // The notes whose flash has not ended, so each frame touches only them. A note is in it exactly while now - playedAt < duration.
        readonly List<NBSNoteGridItem> flashing = [];

        public void Clear()
        {
            items.Clear();
            flashing.Clear();
        }

        public void Add(NBSNoteGridItem item) => items.Add(item);

        public void Sort() => items.Sort(static (x, y) => x.note.tick.CompareTo(y.note.tick));

        public static float GetAmount(NBSNoteGridItem item, double now) => (float)Math.Max(0, 1 - ((now - item.playedAt) / duration));

        // Called every frame while playing. Notes on the ticks after fromTick up to toTick play now.
        public void Update(NBSMap map, double fromTick, double toTick, double now)
        {
            // Ended flashes leave first, so a note that plays again below is added back exactly once.
            for (int i = flashing.Count - 1; i >= 0; i--)
            {
                NBSNoteGridItem item = flashing[i];
                float amount = GetAmount(item, now);
                (item.element as NBSNoteBlockElement)?.SetFlash(amount);

                if (amount <= 0)
                {
                    flashing[i] = flashing[^1];
                    flashing.RemoveAt(flashing.Count - 1);
                }
            }

            int low = 0;
            int high = items.Count;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (items[middle].note.tick <= fromTick)
                    low = middle + 1;
                else
                    high = middle;
            }

            for (int i = low; i < items.Count && items[i].note.tick <= toTick; i++)
            {
                NBSNoteGridItem item = items[i];

                // Like Note Block Studio, notes on locked layers do not flash.
                if (map.TryGetLayer(item.note.layer, out NBSLayer layer) && layer.lockState == NBSLayerLock.locked)
                    continue;

                if (now - item.playedAt >= duration)
                    flashing.Add(item);

                item.playedAt = now;
                (item.element as NBSNoteBlockElement)?.SetFlash(1);
            }
        }

        public void Reset()
        {
            foreach (NBSNoteGridItem item in flashing)
            {
                item.playedAt = double.NegativeInfinity;
                (item.element as NBSNoteBlockElement)?.SetFlash(0);
            }

            flashing.Clear();
        }
    }
}
