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

        // One rectangle per pixel column, row, width, and selection state is enough when items are packed tighter than pixels.
        readonly HashSet<(int x, int y, int width, bool selected)> denseCells = [];

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
                scale.GetLines(range, axis.pixelsPerUnit, lineBuffer);
                for (int i = 0; i < lineBuffer.Count; i++)
                {
                    GridLine line = lineBuffer[i];
                    float x = MathF.Round(axis.ToPixel(line.position));
                    rectBuffer.Add((new Rect(x, visibleTop, 1, height), lineColors[Math.Clamp(line.level, 0, lineColors.Length - 1)]));
                }
            }

            if (isDense)
            {
                denseCells.Clear();
                for (int i = 0; i < visibleList.Count; i++)
                {
                    GridItem item = visibleList[i];
                    Rect rect = GetItemRect(item);
                    bool selected = item.selected;
                    if (!denseCells.Add(((int)MathF.Floor(rect.x), (int)MathF.Floor(rect.y), (int)rect.width, selected)))
                        continue;

                    rectBuffer.Add((rect, selected ? selectedItemColor : GetDenseColor(item) ?? itemColor));
                }

                denseCells.Clear();
            }

            context.DrawRects(CollectionsMarshal.AsSpan(rectBuffer));
        }
    }
}
