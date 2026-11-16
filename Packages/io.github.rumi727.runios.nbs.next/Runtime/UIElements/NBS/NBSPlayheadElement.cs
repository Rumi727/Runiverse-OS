#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.NBS
{
    // The playhead of NBSNoteGridView in the overlay of its grid: a line and the triangle that Note Block Studio draws on its marker, both in one color.
    internal sealed class NBSPlayheadElement : VisualElement
    {
        readonly VisualElement marker;

        // Starts transparent, so the first color is always applied.
        Color color;

        public NBSPlayheadElement()
        {
            name = "playhead";
            pickingMode = PickingMode.Ignore;
            AddToClassList(NBSNoteGridView.playheadUssClassName);

            marker = new VisualElement { name = "marker", pickingMode = PickingMode.Ignore };
            marker.AddToClassList(NBSNoteGridView.playheadMarkerUssClassName);
            marker.generateVisualContent += DrawMarker;
            Add(marker);

            SetColor(NBSVisualEffectOverlay.defaultAccentColor);
        }

        // Called every frame while playing; the marker is drawn again only when the color changes.
        public void SetColor(Color value)
        {
            if (color == value)
                return;

            color = value;
            style.backgroundColor = value;
            marker.MarkDirtyRepaint();
        }

        void DrawMarker(MeshGenerationContext context)
        {
            Rect rect = marker.contentRect;
            Painter2D painter = context.painter2D;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(rect.width, 0));
            painter.LineTo(new Vector2(rect.width * 0.5f, rect.height));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
