#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using RuniOS.NBS;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    // The wall that Note Block Studio draws at each sound stopper over the layers it stops, for one layer of NBSNoteGridView.
    // It sits in the track after the layer, so it is drawn over the block elements of the layer; NBSNoteGridView repaints it when the axis changes.
    internal sealed class NBSStopperGuide : VisualElement
    {
        // The alpha of each pixel column of the wall: it fades in towards the left edge of the tick and ends in two opaque columns.
        static readonly byte[] alphas = [18, 41, 64, 100, 141, 192, 255, 255];

        static readonly CustomStyleProperty<Color> colorProperty = new("--runios-nbs-stopper-guide-color");

        // Shared by every guide; drawing runs on the main thread, one element at a time.
        static readonly List<(Rect rect, Color color)> rects = [];

        readonly GridAxis axis;
        readonly ImmutableArray<NBSSoundStopper> stoppers;
        readonly int layer;
        Color color = Color.black;

        public NBSStopperGuide(GridAxis axis, NBSMap map, int layer, float left)
        {
            this.axis = axis;
            stoppers = map.stoppers;
            this.layer = layer;

            name = "stopper-guide";
            pickingMode = PickingMode.Ignore;
            AddToClassList(NBSNoteGridView.stopperGuideUssClassName);
            style.left = left;

            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public static bool IsNeeded(NBSMap map, int layer)
        {
            foreach (NBSSoundStopper stopper in map.stoppers)
            {
                if (stopper.Targets(layer))
                    return true;
            }

            return false;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(colorProperty, out Color value) && value != color)
            {
                color = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            GridRange range = axis.GetRange();

            // The wall reaches to the left of its tick, so a stopper just right of the view can still show.
            double startTick = range.start - 1;
            double endTick = range.end + ((alphas.Length + 1) / axis.pixelsPerUnit);

            int low = 0;
            int high = stoppers.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (stoppers[middle].tick < startTick)
                    low = middle + 1;
                else
                    high = middle;
            }

            float height = contentRect.height;
            rects.Clear();
            for (int i = low; i < stoppers.Length && stoppers[i].tick <= endTick; i++)
            {
                if (!stoppers[i].Targets(layer))
                    continue;

                // Note Block Studio draws the 16 pixel wall sprite with its origin at the tick minus one pixel; only its first eight pixels are not transparent.
                float left = MathF.Round(axis.ToPixel(stoppers[i].tick)) - 1 - alphas.Length;
                for (int column = 0; column < alphas.Length; column++)
                    rects.Add((new Rect(left + column + 1, 0, 1, height), color with { a = alphas[column] / 255f }));
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rects));
            rects.Clear();
        }
    }
}
