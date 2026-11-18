#nullable enable
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public abstract partial class GridLayer
    {
        static readonly CustomStyleProperty<Color> majorLineColorProperty = new("--runios-grid-line-color-0");
        static readonly CustomStyleProperty<Color> lineColorProperty = new("--runios-grid-line-color-1");
        static readonly CustomStyleProperty<Color> minorLineColorProperty = new("--runios-grid-line-color-2");
        static readonly CustomStyleProperty<Color> itemColorProperty = new("--runios-grid-item-color");
        static readonly CustomStyleProperty<Color> selectedItemColorProperty = new("--runios-grid-selected-item-color");

        readonly Color[] lineColors = [new(0.5f, 0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.3f), new(0.5f, 0.5f, 0.5f, 0.12f)];
        Color itemColor = new(0.35f, 0.6f, 1, 0.85f);
        Color selectedItemColor = Color.white;

        readonly List<GridLine> lineBuffer = [];
        readonly List<(Rect rect, Color color)> rectBuffer = [];

        // Rectangles are at least this many pixels wide and high, so a small item always covers a pixel wherever it lands between pixels.
        // Items of the same row and color closer than this are drawn as one rectangle. Merging depends only on the distance between items, so scrolling never changes which items merge.
        const float denseMinimumSize = 1;

        // The index in rectBuffer of the rectangle each row is extending, keyed by the top of the row.
        readonly Dictionary<float, int> denseRuns = [];

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle style = evt.customStyle;
            if (style.TryGetValue(majorLineColorProperty, out Color color))
                lineColors[0] = color;
            if (style.TryGetValue(lineColorProperty, out color))
                lineColors[1] = color;
            if (style.TryGetValue(minorLineColorProperty, out color))
                lineColors[2] = color;
            if (style.TryGetValue(itemColorProperty, out color))
                itemColor = color;
            if (style.TryGetValue(selectedItemColorProperty, out color))
                selectedItemColor = color;

            MarkDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (view is not { } gridView || !(visibleBottom > visibleTop))
                return;

            GridAxis axis = gridView.axis;
            GridRange range = axis.GetRange();
            float height = visibleBottom - visibleTop;
            rectBuffer.Clear();

            foreach (GridScale scale in axis.scales)
            {
                if (!drawScaleLines || !scale.drawLines)
                    continue;

                lineBuffer.Clear();
                scale.GetLines(range, axis.pixelsPerUnit, false, lineBuffer);
                for (int i = 0; i < lineBuffer.Count; i++)
                {
                    GridLine line = lineBuffer[i];
                    float x = MathF.Round(axis.ToPixel(line.position));
                    rectBuffer.Add((new Rect(x, visibleTop, 1, height), lineColors[Math.Clamp(line.level, 0, lineColors.Length - 1)]));
                }
            }

            AddRowLines(rectBuffer, visibleTop, visibleBottom);

            if (isDense)
            {
                denseRuns.Clear();
                for (int i = 0; i < visibleList.Count; i++)
                {
                    GridItem item = visibleList[i];
                    Rect rect = GetItemRect(item);
                    rect.width = Math.Max(rect.width, denseMinimumSize);
                    rect.height = Math.Max(rect.height, denseMinimumSize);
                    Color color = item.selected ? selectedItemColor : GetDenseColor(item) ?? itemColor;

                    // Items come in start order, so an item can only extend the last rectangle of its row.
                    if (denseRuns.TryGetValue(rect.y, out int index))
                    {
                        (Rect run, Color runColor) = rectBuffer[index];
                        if (runColor == color && run.height.Approximately(rect.height) && rect.xMin < run.xMax + denseMinimumSize)
                        {
                            run.xMax = Math.Max(run.xMax, rect.xMax);
                            rectBuffer[index] = (run, runColor);
                            continue;
                        }
                    }

                    denseRuns[rect.y] = rectBuffer.Count;
                    rectBuffer.Add((rect, color));
                }

                denseRuns.Clear();
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rectBuffer));
        }

        // Adds the lines between the rows of the layer that fall between top and bottom, drawn over the scale lines and under the items.
        private protected virtual void AddRowLines(List<(Rect rect, Color color)> rects, float top, float bottom) { }
    }
}
