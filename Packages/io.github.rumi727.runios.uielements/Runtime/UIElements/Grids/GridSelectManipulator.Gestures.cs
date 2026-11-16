#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public partial class GridSelectManipulator
    {
        readonly List<GridMoveEntry> moveEntries = [];
        GridItem? moveAnchor;
        double moveAnchorStart;
        double moveAnchorTop;
        double pressVerticalPosition;
        double moveDelta;
        double moveVerticalDelta;

        void BeginMove(GridItem anchor)
        {
            moveEntries.Clear();
            foreach (GridItem item in view.selection)
                moveEntries.Add(new GridMoveEntry(item, item.start, item is PlaneGridItem planeItem ? planeItem.top : 0));

            GridLayer layer = anchor.layer!;
            moveAnchor = anchor;
            moveAnchorStart = anchor.start;
            moveAnchorTop = anchor is PlaneGridItem planeAnchor ? planeAnchor.top : 0;
            pressVerticalPosition = layer.GetVerticalPosition(layer.WorldToLocal(pressPanelPosition).y);
            moveDelta = 0;
            moveVerticalDelta = 0;

            moveStarted?.Invoke(moveEntries.Select(static x => x.item).ToArray());
        }

        void UpdateMove(Vector2 panelPosition, EventModifiers modifiers)
        {
            if (moveAnchor?.layer is not { } layer)
                return;

            double position = view.axis.ToPosition(view.GetAxisPixel(panelPosition));
            moveDelta = view.Snap(moveAnchorStart + (position - press.position), modifiers) - moveAnchorStart;

            if (moveAnchor is PlaneGridItem)
            {
                double verticalPosition = layer.GetVerticalPosition(layer.WorldToLocal(panelPosition).y);
                moveVerticalDelta = layer.SnapVertical(moveAnchorTop + (verticalPosition - pressVerticalPosition)) - moveAnchorTop;
            }

            ApplyMove(moveDelta, moveVerticalDelta);
        }

        void ApplyMove(double delta, double verticalDelta)
        {
            foreach ((GridItem item, double originalStart, double originalTop) in moveEntries)
            {
                item.start = originalStart + delta;
                if (item is PlaneGridItem planeItem)
                    planeItem.top = originalTop + verticalDelta;
            }
        }

        void EndMove(bool commit)
        {
            if (commit && (moveDelta != 0 || moveVerticalDelta != 0))
                moved?.Invoke(new GridMoveResult(moveEntries.ToArray(), moveDelta, moveVerticalDelta));
            else
            {
                ApplyMove(0, 0);
                moveCanceled?.Invoke();
            }

            moveEntries.Clear();
            moveAnchor = null;
        }

        readonly List<GridItem> marqueeBase = [];
        readonly List<GridItem> marqueeItems = [];

        void BeginMarquee()
        {
            marqueeBase.Clear();
            if (press.actionKey)
                marqueeBase.AddRange(view.selection);
            else
                view.selection.Clear();

            view.marquee.AddToClassList(GridView.marqueeActiveUssClassName);
        }

        void UpdateMarquee(Vector2 panelPosition)
        {
            VisualElement viewport = view.viewport;
            Rect rect = FromPoints(viewport.WorldToLocal(pressPanelPosition), viewport.WorldToLocal(panelPosition));
            view.marquee.style.translate = new Translate(rect.x, rect.y);
            view.marquee.style.width = rect.width;
            view.marquee.style.height = rect.height;

            marqueeItems.Clear();
            marqueeItems.AddRange(marqueeBase);
            // This runs on every pointer move, so the tracks are walked by index instead of through GridView.tracks.
            VisualElement content = view.scrollView.contentContainer;
            for (int i = 0; i < content.childCount; i++)
            {
                if (content[i] is not GridTrack track)
                    continue;

                GridLayer layer = track.layer;
                layer.Overlap(FromPoints(layer.WorldToLocal(pressPanelPosition), layer.WorldToLocal(panelPosition)), marqueeItems);
            }

            view.selection.Set(marqueeItems);
        }

        void EndMarquee()
        {
            view.marquee.RemoveFromClassList(GridView.marqueeActiveUssClassName);
            marqueeBase.Clear();
            marqueeItems.Clear();
        }

        static Rect FromPoints(Vector2 a, Vector2 b) => Rect.MinMaxRect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Max(a.x, b.x), Math.Max(a.y, b.y));
    }
}
