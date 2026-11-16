#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using RuniOS.NBS;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    // The wall that Note Block Studio draws at each sound stopper over the layers it stops, over every row of NBSNoteGridView.
    // It sits in the track after the layer, so it is drawn over the block elements of the layer; NBSNoteGridView repaints it when the axis changes, and a new vertical scale repaints it by changing its height.
    internal sealed class NBSStopperGuide : VisualElement
    {
        // The alpha of each pixel column of the wall: it fades in towards the left edge of the tick and ends in two opaque columns.
        static readonly byte[] alphas = [18, 41, 64, 100, 141, 192, 255, 255];

        static readonly CustomStyleProperty<Color> colorProperty = new("--runios-nbs-stopper-guide-color");

        // Shared by every guide; drawing runs on the main thread, one element at a time.
        static readonly List<(Rect rect, Color color)> rects = [];

        readonly GridAxis axis;
        readonly ImmutableArray<NBSSoundStopper> stoppers;
        readonly int layerCount;
        Color color = Color.black;

        public NBSStopperGuide(GridAxis axis, NBSMap map, int layerCount, float left)
        {
            this.axis = axis;
            stoppers = map.stoppers;
            this.layerCount = layerCount;

            name = "stopper-guide";
            pickingMode = PickingMode.Ignore;
            AddToClassList(NBSNoteGridView.stopperGuideUssClassName);
            style.left = left;

            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
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

            // The guide spans every row, so one row is an equal share of its height.
            float rowHeight = contentRect.height / Math.Max(1, layerCount);
            rects.Clear();
            for (int i = low; i < stoppers.Length && stoppers[i].tick <= endTick; i++)
            {
                NBSSoundStopper stopper = stoppers[i];

                // Note Block Studio draws the 16 pixel wall sprite with its origin at the tick minus one pixel; only its first eight pixels are not transparent.
                float left = MathF.Round(axis.ToPixel(stopper.tick)) - 1 - alphas.Length;

                // Each run of stopped rows is one wall.
                for (int start = 0; start < layerCount; start++)
                {
                    if (!stopper.Targets(start))
                        continue;

                    int end = start + 1;
                    while (end < layerCount && stopper.Targets(end))
                        end++;

                    float top = start * rowHeight;
                    float height = (end - start) * rowHeight;
                    for (int column = 0; column < alphas.Length; column++)
                        rects.Add((new Rect(left + column + 1, top, 1, height), color with { a = alphas[column] / 255f }));

                    start = end;
                }
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rects));
            rects.Clear();
        }
    }
}
