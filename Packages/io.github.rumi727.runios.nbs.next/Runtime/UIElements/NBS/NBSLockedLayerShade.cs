#nullable enable
using System.Runtime.InteropServices;
using RuniOS.NBS;
using RuniOS.UIElements.Grids;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    // Darkens the rows of the locked layers of NBSNoteGridView.
    // It sits in the track before the layer, so the lines and notes of the layer are drawn over it. A new vertical scale repaints it by changing its height.
    internal sealed class NBSLockedLayerShade : VisualElement
    {
        static readonly CustomStyleProperty<Color> colorProperty = new("--runios-nbs-locked-layer-color");

        // Shared by every shade; drawing runs on the main thread, one element at a time.
        static readonly List<(Rect rect, Color color)> rects = [];

        readonly NBSMap map;
        readonly int layerCount;
        Color color = Color.clear;

        public NBSLockedLayerShade(NBSMap map, int layerCount, float left)
        {
            this.map = map;
            this.layerCount = layerCount;

            name = "locked-layer-shade";
            pickingMode = PickingMode.Ignore;
            AddToClassList(NBSNoteGridView.lockedLayerShadeUssClassName);
            style.left = left;

            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public static bool IsNeeded(NBSMap map, int layerCount)
        {
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (IsLocked(map, layer))
                    return true;
            }

            return false;
        }

        static bool IsLocked(NBSMap map, int layer) => map.TryGetLayer(layer, out NBSLayer row) && row.lockState == NBSLayerLock.locked;

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
            // The shade spans every row, so one row is an equal share of its height.
            Rect bounds = contentRect;
            float rowHeight = bounds.height / Math.Max(1, layerCount);
            rects.Clear();

            // Each run of locked rows is one rectangle.
            for (int start = 0; start < layerCount; start++)
            {
                if (!IsLocked(map, start))
                    continue;

                int end = start + 1;
                while (end < layerCount && IsLocked(map, end))
                    end++;

                rects.Add((new Rect(0, start * rowHeight, bounds.width, (end - start) * rowHeight), color));
                start = end;
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rects));
            rects.Clear();
        }
    }
}
