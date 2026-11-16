#nullable enable
#if UNITY_EDITOR
using RuniOS.UIElements.Grids;
using RuniOS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GridView를 1D 레인, 100만 개 2D 아이템, 재생 헤드 오버레이로 직접 조작해 보는 테스트 창입니다.
/// </summary>
public sealed class GridViewTestWindow : EditorWindow
{
    [MenuItem("RuniOS/Grid View Test")]
    static void Open() => GetWindow<GridViewTestWindow>("Grid View Test");

    /// <summary>
    /// 길이를 가진 1D 아이템입니다. 길이가 0이면 요소 폭을 그대로 씁니다.
    /// </summary>
    sealed class TestLaneItem : LaneGridItem
    {
        double _length;

        public double length
        {
            get => _length;
            set
            {
                _length = value;
                MarkDirty();
            }
        }

        public override float GetWidth(GridAxis axis) => length > 0 ? (float)(length * axis.pixelsPerUnit) : float.NaN;
    }

    const int planeItemCount = 1_000_000;
    const int planeRowCount = 100;

    readonly Stack<VisualElement> pool = new();
    int madeCount;
    int releasedCount;
    Label? status;

    void CreateGUI()
    {
        Toggle readOnlyToggle = new("Read only");
        status = new Label();

        GridView grid = new() { headerWidth = 120 };
        grid.style.flexGrow = 1;

        LinearGridScale beats = new() { unit = 4, majorInterval = 4 };
        grid.axis.scales.Add(beats);
        grid.AddRuler(beats);
        grid.AddRuler(new LinearGridScale { unit = 1, minimumSpacing = 30, subdivision = 1, drawLines = false, labelFormatter = x => $"{x}t" });

        // 1D: items overlapping on screen stack into lanes, earlier items on top.
        LaneGridLayer lanes = new();
        lanes.makeElement = MakeElement;
        lanes.releaseElement = (_, element) => Release(element);
        System.Random random = new(1);
        for (int i = 0; i < 200; i++)
            lanes.items.Add(new TestLaneItem { start = random.Next(0, 400), length = random.Next(0, 3) == 0 ? 0 : random.Next(1, 16) });

        // Same start, different lengths: the order in the collection decides which one is on top.
        lanes.items.Add(new TestLaneItem { start = -8, length = 1 });
        lanes.items.Add(new TestLaneItem { start = -8, length = 6 });
        lanes.items.Add(new TestLaneItem { start = -8 });

        GridTrack laneTrack = grid.AddTrack(lanes);
        laneTrack.header.Add(new Label("Lanes (1D)"));

        // 2D: one million items, already sorted by start.
        PlaneGridLayer plane = new() { unitHeight = 16, verticalLength = planeRowCount };
        plane.makeElement = MakeElement;
        plane.releaseElement = (_, element) => Release(element);
        PlaneGridItem[] planeItems = new PlaneGridItem[planeItemCount];
        for (int i = 0; i < planeItems.Length; i++)
        {
            // ReSharper disable once PossibleLossOfFraction
            planeItems[i] = new PlaneGridItem { start = i / 10, top = random.Next(0, planeRowCount) };
        }
        plane.items.AddRange(planeItems);

        GridTrack planeTrack = grid.AddTrack(plane);
        planeTrack.header.Add(new Label($"Plane (2D, {planeItemCount:N0})"));

        // Overlay: a playhead that keeps moving.
        GridItem playhead = new();
        grid.overlay.defaultItemWidth = 2;
        grid.overlay.makeElement = _ => new VisualElement { style = { width = 2, backgroundColor = new Color(1, 0.6f, 0.1f) }, pickingMode = PickingMode.Ignore };
        grid.overlay.items.Add(playhead);
        grid.schedule.Execute(() => playhead.start = (playhead.start + 0.25) % 400).Every(16);

        grid.selector.clicking += args =>
        {
            Debug.Log($"Click button={args.button} position={args.position:0.###} snapped={args.snappedPosition:0.###} vertical={args.verticalPosition:0.###} item={args.item != null} ruler={args.ruler != null}");
            if (args.button == 0 && args.item == null && args.layer == lanes && grid.selection.Count == 0 && !grid.readOnly)
            {
                TestLaneItem item = new() { start = args.snappedPosition, length = 4 };
                lanes.items.Add(item);
                grid.selection.Set(item);
                return true;
            }

            return false;
        };
        grid.selector.dragStarting += args => args.ruler != null;
        grid.selector.dragging += args => playhead.start = args.position;
        grid.selector.moved += result => Debug.Log($"Moved {result.entries.Count} items by {result.delta:0.###}, {result.verticalDelta:0.###}");

        GridEditKeyManipulator keys = new(grid);
        keys.deleting += selection =>
        {
            lanes.items.RemoveAll(x => x.selected);
            plane.items.RemoveAll(x => x.selected);
        };
        keys.copying += selection => Debug.Log($"Copy {selection.Count} items in {selection.range}");
        keys.pasting += pointer => Debug.Log($"Paste at {pointer.snappedPosition:0.###} in {pointer.layer?.GetType().Name}");
        grid.viewport.AddManipulator(keys);

        grid.makeSelectionPopup = selection =>
        {
            // Edits apply to every selected item; a field whose values differ shows a mixed value.
            GridItem[] items = selection.ToArray();
            VisualElement content = new();
            content.Add(new Label($"{items.Length} selected"));
            content.Add(MakeField("Start", items, static x => x.start, static (x, value) => x.start = value));
            if (Array.TrueForAll(items, static x => x is TestLaneItem))
                content.Add(MakeField("Length", items, static x => ((TestLaneItem)x).length, static (x, value) => ((TestLaneItem)x).length = Math.Max(0, value)));

            return content;
        };

        readOnlyToggle.RegisterValueChangedCallback(evt => grid.readOnly = evt.newValue);
        grid.schedule.Execute(() =>
        {
            if (status != null)
                status.text = $"made {madeCount}, released {releasedCount}, pooled {pool.Count}, lanes {lanes.laneCount}, plane visible {plane.visibleItems.Count} dense {plane.isDense}";
        }).Every(200);

        rootVisualElement.Add(readOnlyToggle);
        rootVisualElement.Add(status);
        rootVisualElement.Add(grid);
    }

    static DoubleField MakeField(string label, GridItem[] items, Func<GridItem, double> get, Action<GridItem, double> set)
    {
        double first = get(items[0]);
        DoubleField field = new(label) { value = first, showMixedValue = Array.Exists(items, x => !get(x).Approximately(first)) };
        field.RegisterValueChangedCallback(evt =>
        {
            foreach (GridItem item in items)
                set(item, evt.newValue);

            field.showMixedValue = false;
        });

        return field;
    }

    VisualElement MakeElement(GridItem item)
    {
        madeCount++;
        if (!pool.TryPop(out VisualElement? element))
            element = new VisualElement { style = { backgroundColor = new Color(0.35f, 0.6f, 1, 0.85f) } };

        // The element decides its own size; the grid overrides only the width of items that report one.
        element.style.width = 8;
        element.style.height = 14;
        return element;
    }

    void Release(VisualElement element)
    {
        releasedCount++;
        pool.Push(element);
    }
}
#endif
