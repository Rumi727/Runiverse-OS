#nullable enable
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The area of a <see cref="GridView"/> where items are placed. Derive from <see cref="GridLayer{TItem}"/> to make a new kind of layout.<br/>
    /// Only items on screen get an element. When too many are on screen, they are drawn as plain rectangles instead.
    /// <br/><br/>
    /// <see cref="GridView"/>에서 아이템이 놓이는 영역입니다. 새로운 배치 방식은 <see cref="GridLayer{TItem}"/>를 상속해 만드세요.<br/>
    /// 화면에 보이는 아이템만 요소를 가집니다. 화면에 너무 많으면 요소 대신 단순한 사각형으로 그립니다.
    /// </summary>
    /// <remarks>
    /// The layer finds its <see cref="GridView"/> when it is attached to a panel, and updates on the frame after something changes.<br/>
    /// Horizontal pixels are measured from the left edge of the layer, which lines up with <see cref="GridAxis"/> pixel <c>0</c>.
    /// <br/><br/>
    /// 레이어는 패널에 붙을 때 자신의 <see cref="GridView"/>를 찾고, 무언가 바뀐 다음 프레임에 갱신합니다.<br/>
    /// 가로 픽셀은 레이어 왼쪽 끝부터 재며, 이 위치는 <see cref="GridAxis"/>의 픽셀 <c>0</c>과 일치합니다.
    /// </remarks>
    public abstract partial class GridLayer : VisualElement
    {
        /// <summary>
        /// The USS class name of grid layers.<br/>
        /// 그리드 레이어의 USS 클래스 이름입니다.
        /// </summary>
        public const string ussClassName = "runios-grid-layer";

        /// <summary>
        /// The USS class name added to the elements of items.<br/>
        /// 아이템 요소에 추가되는 USS 클래스 이름입니다.
        /// </summary>
        public const string itemUssClassName = "runios-grid-item";

        /// <summary>
        /// The USS class name added to the elements of selected items.<br/>
        /// 선택된 아이템 요소에 추가되는 USS 클래스 이름입니다.
        /// </summary>
        public const string selectedItemUssClassName = itemUssClassName + "--selected";

        readonly List<GridItem> sorted = [];
        List<GridItem> visibleList = [];

        // Item values or the item set changed, so the sorted list is sorted and arranged again.
        bool dirty;

        /// <summary>
        /// Initializes the layer.<br/>
        /// 레이어를 초기화합니다.
        /// </summary>
        protected GridLayer()
        {
            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(_ => view?.ScheduleRefresh());
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <summary>
        /// Gets the grid the layer is in, or <see langword="null"/> while it is not in a grid on a panel.<br/>
        /// 레이어가 들어 있는 그리드를 가져옵니다. 패널 위의 그리드에 없으면 <see langword="null"/>입니다.
        /// </summary>
        public GridView? view { get; private set; }

        /// <summary>
        /// Gets the items sorted by <see cref="GridItem.start"/>, as of the last refresh. The order of items with the same start is not specified.<br/>
        /// 마지막 갱신 시점에 <see cref="GridItem.start"/> 순으로 정렬된 아이템을 가져옵니다. 시작이 같은 아이템끼리의 순서는 정해져 있지 않습니다.
        /// </summary>
        public IReadOnlyList<GridItem> sortedItems => sorted;

        /// <summary>
        /// Gets the items on screen, as of the last refresh.<br/>
        /// 마지막 갱신 시점에 화면에 보이는 아이템을 가져옵니다.
        /// </summary>
        public IReadOnlyList<GridItem> visibleItems => visibleList;

        /// <summary>
        /// Gets the top of the part of the layer on screen, in layer pixels.<br/>
        /// 레이어 중 화면에 보이는 부분의 위쪽을 레이어 픽셀로 가져옵니다.
        /// </summary>
        public float visibleTop { get; private set; }

        /// <summary>
        /// Gets the bottom of the part of the layer on screen, in layer pixels.<br/>
        /// 레이어 중 화면에 보이는 부분의 아래쪽을 레이어 픽셀로 가져옵니다.
        /// </summary>
        public float visibleBottom { get; private set; }

        /// <summary>
        /// Gets the largest axis distance an item reaches from its start at the current zoom, as of the last refresh. Range queries look back by this much.<br/>
        /// 마지막 갱신 시점에 현재 확대 수준에서 아이템이 시작 위치부터 닿는 가장 큰 축 거리를 가져옵니다. 범위 질의는 이만큼 뒤까지 살펴봅니다.
        /// </summary>
        /// <remarks>
        /// Computed over all items when they change or the zoom changes.<br/>
        /// 아이템이 바뀌거나 확대 수준이 바뀔 때 모든 아이템을 대상으로 계산합니다.
        /// </remarks>
        public double maximumReach { get; private set; }

        /// <summary>
        /// Gets whether the visible items are drawn as rectangles instead of elements, as of the last refresh.<br/>
        /// 마지막 갱신 시점에 보이는 아이템을 요소 대신 사각형으로 그리고 있는지 여부를 가져옵니다.
        /// </summary>
        public bool isDense { get; private set; }

        /// <summary>
        /// Gets or sets the pixel width assumed for an item whose <see cref="GridItem.GetWidth"/> is <see cref="float.NaN"/> while it has no drawn element. It is never applied to elements.<br/>
        /// Set it to the width the elements get from USS, so that items off screen are culled and stacked the same as those on screen.
        /// <br/><br/>
        /// <see cref="GridItem.GetWidth"/>가 <see cref="float.NaN"/>인 아이템에 그려진 요소가 없는 동안 가정하는 픽셀 폭을 가져오거나 설정합니다. 요소에는 적용하지 않습니다.<br/>
        /// 요소가 USS로 받는 폭과 같게 설정하면, 화면 밖 아이템도 화면 안 아이템과 똑같이 컬링되고 쌓입니다.
        /// </summary>
        public float defaultItemWidth
        {
            get;
            set
            {
                field = value;
                MarkDirty();
            }
        } = 8;

        /// <summary>
        /// Gets or sets how many items may be on screen as elements. Above it, they are drawn as rectangles.<br/>
        /// 요소로 화면에 보일 수 있는 아이템 수를 가져오거나 설정합니다. 이보다 많으면 사각형으로 그립니다.
        /// </summary>
        public int maximumElementCount
        {
            get;
            set
            {
                field = value;
                ScheduleRefresh();
            }
        } = 2000;

        /// <summary>
        /// Gets or sets whether the lines of <see cref="GridAxis.scales"/> are drawn behind the items. <see cref="GridView.overlay"/> turns it off.<br/>
        /// <see cref="GridAxis.scales"/>의 선을 아이템 뒤에 그리는지 여부를 가져오거나 설정합니다. <see cref="GridView.overlay"/>는 이 값을 끕니다.
        /// </summary>
        public bool drawScaleLines
        {
            get;
            set
            {
                field = value;
                MarkDirtyRepaint();
            }
        } = true;

        /// <summary>
        /// Gets whether the layer currently makes elements for its items.<br/>
        /// 레이어가 현재 아이템 요소를 만드는지 여부를 가져옵니다.
        /// </summary>
        protected abstract bool usesElements { get; }

        /// <summary>
        /// Makes the element for <paramref name="item"/>, which just came on screen.<br/>
        /// 방금 화면에 들어온 <paramref name="item"/>의 요소를 만듭니다.
        /// </summary>
        /// <param name="item">
        /// The item.<br/>
        /// 아이템입니다.
        /// </param>
        /// <returns>
        /// The element to show.<br/>
        /// 표시할 요소를 반환합니다.
        /// </returns>
        protected abstract VisualElement MakeElement(GridItem item);

        /// <summary>
        /// Gives back the element of <paramref name="item"/>, which went off screen or was removed. The element is already out of the hierarchy.<br/>
        /// 화면에서 나갔거나 제거된 <paramref name="item"/>의 요소를 돌려줍니다. 요소는 이미 계층에서 빠져 있습니다.
        /// </summary>
        /// <param name="item">
        /// The item.<br/>
        /// 아이템입니다.
        /// </param>
        /// <param name="element">
        /// The element that was shown for the item.<br/>
        /// 아이템에 표시되던 요소입니다.
        /// </param>
        protected abstract void ReleaseElement(GridItem item, VisualElement element);

        /// <summary>
        /// Gets the color used when <paramref name="item"/> is drawn as a rectangle, or <see langword="null"/> for the USS item color.<br/>
        /// <paramref name="item"/>을 사각형으로 그릴 때의 색을 가져옵니다. <see langword="null"/>이면 USS 아이템 색을 씁니다.
        /// </summary>
        /// <param name="item">
        /// The item.<br/>
        /// 아이템입니다.
        /// </param>
        /// <returns>
        /// The color, or <see langword="null"/>.<br/>
        /// 색을 반환하거나 <see langword="null"/>을 반환합니다.
        /// </returns>
        protected virtual Color? GetDenseColor(GridItem item) => null;

        /// <summary>
        /// Gets whether <see cref="Arrange"/> depends on the zoom, so that it is also called when <see cref="GridAxis.pixelsPerUnit"/> changes. The default is <see langword="false"/>.<br/>
        /// <see cref="Arrange"/>가 확대 수준에 따라 달라지는지 여부를 가져옵니다. <see langword="true"/>면 <see cref="GridAxis.pixelsPerUnit"/>이 바뀔 때도 호출됩니다. 기본값은 <see langword="false"/>입니다.
        /// </summary>
        protected virtual bool arrangesWithZoom => false;

        /// <summary>
        /// Places the items after they change, for example by stacking them into lanes, and sets the height of the layer. <see cref="sortedItems"/> is sorted when this is called.<br/>
        /// 아이템이 바뀐 뒤 레인에 쌓는 등 아이템을 배치하고 레이어 높이를 정합니다. 호출될 때 <see cref="sortedItems"/>는 정렬되어 있습니다.
        /// </summary>
        protected abstract void Arrange();

        /// <summary>
        /// Gets the rectangle of <paramref name="item"/> in layer pixels.<br/>
        /// <paramref name="item"/>의 사각형을 레이어 픽셀로 가져옵니다.
        /// </summary>
        /// <param name="item">
        /// An item of this layer.<br/>
        /// 이 레이어의 아이템입니다.
        /// </param>
        /// <returns>
        /// The rectangle.<br/>
        /// 사각형을 반환합니다.
        /// </returns>
        public abstract Rect GetItemRect(GridItem item);

        /// <summary>
        /// Converts a vertical layer pixel to the vertical unit of the layer, such as a lane or row.<br/>
        /// 레이어의 세로 픽셀을 레인이나 행 같은 레이어의 세로 단위로 변환합니다.
        /// </summary>
        /// <param name="localY">
        /// The vertical pixel measured from the top of the layer.<br/>
        /// 레이어 위쪽부터 잰 세로 픽셀입니다.
        /// </param>
        /// <returns>
        /// The vertical position.<br/>
        /// 세로 위치를 반환합니다.
        /// </returns>
        public abstract double GetVerticalPosition(float localY);

        /// <summary>
        /// Snaps a vertical position of the layer. Used when items are dragged vertically. The default returns it unchanged.<br/>
        /// 레이어의 세로 위치를 맞춥니다. 아이템을 세로로 끌 때 사용합니다. 기본 구현은 그대로 반환합니다.
        /// </summary>
        /// <param name="position">
        /// The vertical position.<br/>
        /// 세로 위치입니다.
        /// </param>
        /// <returns>
        /// The snapped vertical position.<br/>
        /// 맞춘 세로 위치를 반환합니다.
        /// </returns>
        public virtual double SnapVertical(double position) => position;

        /// <summary>
        /// Gets the horizontal pixel span of <paramref name="item"/>. The width is <see cref="GridItem.GetWidth"/>, or the drawn width of its element, or <see cref="defaultItemWidth"/>, in that order.<br/>
        /// <paramref name="item"/>의 가로 픽셀 구간을 가져옵니다. 폭은 <see cref="GridItem.GetWidth"/>, 그려진 요소의 폭, <see cref="defaultItemWidth"/> 순서로 정합니다.
        /// </summary>
        /// <param name="item">
        /// The item.<br/>
        /// 아이템입니다.
        /// </param>
        /// <returns>
        /// The left pixel and the width, or zeros while the layer is not in a grid.<br/>
        /// 왼쪽 픽셀과 폭을 반환하며, 레이어가 그리드에 없으면 0을 반환합니다.
        /// </returns>
        public (float x, float width) GetHorizontalPixels(GridItem item)
        {
            if (view is not { } gridView)
                return (0, 0);

            GridAxis axis = gridView.axis;
            return (axis.ToPixel(item.start), GetItemWidth(item, axis));
        }

        float GetItemWidth(GridItem item, GridAxis axis)
        {
            float width = item.GetWidth(axis);
            if (!float.IsNaN(width))
                return width;

            if (item.element is { } element && element.layout.width is var drawnWidth && !float.IsNaN(drawnWidth))
                return drawnWidth;

            return defaultItemWidth;
        }

        /// <summary>
        /// Gets the drawn height of the element of <paramref name="item"/>, or <paramref name="fallback"/> while it has no drawn element. The grid never sets element heights.<br/>
        /// <paramref name="item"/> 요소의 그려진 높이를 가져오며, 그려진 요소가 없으면 <paramref name="fallback"/>을 반환합니다. 그리드는 요소 높이를 정하지 않습니다.
        /// </summary>
        /// <param name="item">
        /// The item.<br/>
        /// 아이템입니다.
        /// </param>
        /// <param name="fallback">
        /// The height to use without a drawn element, usually the height of a lane or row.<br/>
        /// 그려진 요소가 없을 때 쓸 높이이며, 보통 레인이나 행의 높이입니다.
        /// </param>
        /// <returns>
        /// The height in pixels.<br/>
        /// 픽셀 높이를 반환합니다.
        /// </returns>
        public float GetItemHeight(GridItem item, float fallback)
        {
            if (item.element is { } element && element.layout.height is var drawnHeight && !float.IsNaN(drawnHeight))
                return drawnHeight;

            return fallback;
        }

        /// <summary>
        /// Adds the items whose horizontal span reaches into <paramref name="range"/> to <paramref name="result"/>, in start order.<br/>
        /// The cost depends on the number of found items, not on the total.
        /// <br/><br/>
        /// 가로 구간이 <paramref name="range"/>에 닿는 아이템을 시작 순서로 <paramref name="result"/>에 추가합니다.<br/>
        /// 비용은 전체 수가 아니라 찾은 아이템 수에 따라 정해집니다.
        /// </summary>
        /// <param name="range">
        /// The axis range.<br/>
        /// 축 범위입니다.
        /// </param>
        /// <param name="result">
        /// The list to add to. It is not cleared by this method.<br/>
        /// 추가할 목록입니다. 이 메서드는 목록을 비우지 않습니다.
        /// </param>
        /// <remarks>
        /// Uses <see cref="sortedItems"/>, so it reflects the last refresh.<br/>
        /// <see cref="sortedItems"/>를 사용하므로 마지막 갱신 상태를 반영합니다.
        /// </remarks>
        public void CollectRange(GridRange range, List<GridItem> result)
        {
            if (view is not { } gridView)
                return;

            GridAxis axis = gridView.axis;
            ReadOnlySpan<GridItem> items = CollectionsMarshal.AsSpan(sorted);

            int index = LowerBound(items, range.start - maximumReach);
            for (; index < items.Length && items[index].start <= range.end; index++)
            {
                // Items removed since the last refresh are still in the sorted list until the next one.
                GridItem item = items[index];
                if (item.layer == this && item.start + (GetItemWidth(item, axis) / axis.pixelsPerUnit) >= range.start)
                    result.Add(item);
            }
        }

        static int LowerBound(ReadOnlySpan<GridItem> items, double start)
        {
            int low = 0;
            int high = items.Length;
            while (low < high)
            {
                int middle = (low + high) >>> 1;
                if (items[middle].start < start)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        /// <summary>
        /// Adds the items on screen to <paramref name="result"/>. The default uses <see cref="CollectRange"/> and keeps the items that reach between <paramref name="top"/> and <paramref name="bottom"/>.<br/>
        /// 화면에 보이는 아이템을 <paramref name="result"/>에 추가합니다. 기본 구현은 <see cref="CollectRange"/>를 쓰고 <paramref name="top"/>과 <paramref name="bottom"/> 사이에 닿는 아이템만 남깁니다.
        /// </summary>
        /// <param name="range">
        /// The visible axis range.<br/>
        /// 보이는 축 범위입니다.
        /// </param>
        /// <param name="top">
        /// The top of the visible part in layer pixels.<br/>
        /// 보이는 부분의 위쪽 레이어 픽셀입니다.
        /// </param>
        /// <param name="bottom">
        /// The bottom of the visible part in layer pixels.<br/>
        /// 보이는 부분의 아래쪽 레이어 픽셀입니다.
        /// </param>
        /// <param name="result">
        /// The list to add to.<br/>
        /// 추가할 목록입니다.
        /// </param>
        protected virtual void CollectVisible(GridRange range, float top, float bottom, List<GridItem> result)
        {
            int start = result.Count;
            CollectRange(range, result);

            int count = start;
            for (int i = start; i < result.Count; i++)
            {
                Rect rect = GetItemRect(result[i]);
                if (rect.yMax >= top && rect.yMin <= bottom)
                    result[count++] = result[i];
            }

            CollectionsMarshal.SetCount(result, count);
        }

        readonly List<GridItem> queryBuffer = [];

        /// <summary>
        /// Finds the item at <paramref name="localPosition"/>. When items overlap, the one later in start order wins.<br/>
        /// Only the items near the position are checked, so the cost does not depend on the total.
        /// <br/><br/>
        /// <paramref name="localPosition"/>에 있는 아이템을 찾습니다. 아이템이 겹치면 시작 순서상 뒤의 아이템이 우선합니다.<br/>
        /// 위치 근처의 아이템만 확인하므로 비용이 전체 수에 따라 늘지 않습니다.
        /// </summary>
        /// <param name="localPosition">
        /// The position in layer pixels.<br/>
        /// 레이어 픽셀 위치입니다.
        /// </param>
        /// <returns>
        /// The item if found; otherwise, <see langword="null"/>.<br/>
        /// 찾은 경우 아이템을 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public virtual GridItem? HitTest(Vector2 localPosition)
        {
            if (view is not { } gridView)
                return null;

            double position = gridView.axis.ToPosition(localPosition.x);
            queryBuffer.Clear();
            CollectRange(new GridRange(position, position), queryBuffer);

            GridItem? result = null;
            for (int i = queryBuffer.Count - 1; i >= 0; i--)
            {
                if (GetItemRect(queryBuffer[i]).Contains(localPosition))
                {
                    result = queryBuffer[i];
                    break;
                }
            }

            queryBuffer.Clear();
            return result;
        }

        /// <summary>
        /// Adds the items whose rectangle overlaps <paramref name="localRect"/> to <paramref name="result"/>. Only the items in the horizontal range of the rectangle are checked.<br/>
        /// 사각형이 <paramref name="localRect"/>와 겹치는 아이템을 <paramref name="result"/>에 추가합니다. 사각형의 가로 범위 안 아이템만 확인합니다.
        /// </summary>
        /// <param name="localRect">
        /// The rectangle in layer pixels.<br/>
        /// 레이어 픽셀 사각형입니다.
        /// </param>
        /// <param name="result">
        /// The list to add to.<br/>
        /// 추가할 목록입니다.
        /// </param>
        public virtual void Overlap(Rect localRect, List<GridItem> result)
        {
            if (view is not { } gridView)
                return;

            GridAxis axis = gridView.axis;
            queryBuffer.Clear();
            CollectRange(new GridRange(axis.ToPosition(localRect.xMin), axis.ToPosition(localRect.xMax)), queryBuffer);

            for (int i = 0; i < queryBuffer.Count; i++)
            {
                if (GetItemRect(queryBuffer[i]).Overlaps(localRect))
                    result.Add(queryBuffer[i]);
            }

            queryBuffer.Clear();
        }

        /// <summary>
        /// Marks the items as changed so that they are sorted and arranged again on the next refresh.<br/>
        /// 아이템이 바뀌었다고 표시해 다음 갱신 때 다시 정렬하고 배치하게 합니다.
        /// </summary>
        public void MarkDirty()
        {
            dirty = true;
            ScheduleRefresh();
        }

        // Set when something only this layer shows changed, so GridView refreshes this layer on its next pass without refreshing the whole grid.
        internal bool refreshPending { get; private set; }

        // Refreshes this layer on the next frame without sorting or arranging the items again.
        internal void ScheduleRefresh()
        {
            refreshPending = true;
            view?.ScheduleChanges();
        }

        // Added and removed items are applied to the sorted list on the next refresh, keeping its previous order so the adaptive sort stays cheap.
        // Removed items also leave the selection in one batch, so removing many items raises one selection change.
        readonly List<GridItem> addedItems = [];
        readonly HashSet<GridItem> addedSet = [];
        readonly List<GridItem> removedItems = [];

        internal void OnItemAdded(GridItem item)
        {
            addedItems.Add(item);
            dirty = true;
            ScheduleRefresh();
        }

        internal void OnItemRemoved(GridItem item)
        {
            if (item.element != null)
                ReleaseItemElement(item);

            removedItems.Add(item);
            dirty = true;
            ScheduleRefresh();
        }

        List<GridItem> nextVisible = [];
        readonly HashSet<GridItem> nextVisibleSet = [];
        double arrangedPixelsPerUnit = double.NaN;

        /// <summary>
        /// Brings the layer up to date now: sorts and arranges changed items, finds the items on screen, and makes or gives back their elements.<br/>
        /// <see cref="GridView"/> calls this once per frame when needed, so it rarely needs to be called directly.
        /// <br/><br/>
        /// 레이어를 지금 최신 상태로 만듭니다. 바뀐 아이템을 정렬·배치하고, 화면에 보이는 아이템을 찾아 요소를 만들거나 돌려줍니다.<br/>
        /// 필요할 때 <see cref="GridView"/>가 프레임마다 한 번 호출하므로 직접 호출할 일은 드뭅니다.
        /// </summary>
        public void Refresh()
        {
            // Cleared first, so a change raised during the refresh, such as the selection losing removed items, refreshes again on the next pass.
            refreshPending = false;
            if (view is not { } gridView)
                return;

            if (addedItems.Count > 0 || removedItems.Count > 0)
                ApplyAddedAndRemovedItems(gridView);

            GridAxis axis = gridView.axis;
            bool zoomChanged = !axis.pixelsPerUnit.Approximately(arrangedPixelsPerUnit);
            if (dirty)
                SortByStart(CollectionsMarshal.AsSpan(sorted));

            // Widths can depend on the zoom, so the reach is measured again whenever either changes.
            if (dirty || zoomChanged)
            {
                maximumReach = 0;
                for (int i = 0; i < sorted.Count; i++)
                    maximumReach = Math.Max(maximumReach, GetItemWidth(sorted[i], axis) / axis.pixelsPerUnit);
            }

            if (dirty || (zoomChanged && arrangesWithZoom))
                Arrange();

            dirty = false;
            arrangedPixelsPerUnit = axis.pixelsPerUnit;

            Rect clip = gridView.viewport.worldBound;
            Rect bounds = worldBound;
            float top = Math.Max(0, clip.yMin - bounds.yMin);
            float bottom = Math.Min(bounds.height, clip.yMax - bounds.yMin);

            nextVisible.Clear();
            if (bottom > top)
                CollectVisible(gridView.axis.GetRange(), top, bottom, nextVisible);

            bool useElements = usesElements && nextVisible.Count <= maximumElementCount;
            if (useElements)
            {
                nextVisibleSet.Clear();
                nextVisibleSet.UnionWith(nextVisible);
            }

            for (int i = 0; i < visibleList.Count; i++)
            {
                GridItem item = visibleList[i];
                if (item.element != null && item.layer == this && (!useElements || !nextVisibleSet.Contains(item)))
                    ReleaseItemElement(item);
            }

            if (useElements)
            {
                for (int i = 0; i < nextVisible.Count; i++)
                {
                    GridItem item = nextVisible[i];
                    if (item.element == null)
                        MakeItemElement(item);

                    UpdateItemElement(item);
                }
            }

            (visibleList, nextVisible) = (nextVisible, visibleList);
            nextVisible.Clear();
            nextVisibleSet.Clear();

            isDense = !useElements;
            visibleTop = top;
            visibleBottom = bottom;
            MarkDirtyRepaint();
        }

        void ApplyAddedAndRemovedItems(GridView gridView)
        {
            addedSet.UnionWith(addedItems);

            if (removedItems.Count > 0)
            {
                // Items added again after being removed are also in addedSet; they are dropped here and appended once below.
                sorted.RemoveAll(x => x.layer != this || addedSet.Contains(x));

                // An item moved to another layer of the same grid stays selected.
                gridView.selection.RemoveRange(removedItems.Where(x => x.layer?.view != gridView));
                removedItems.Clear();
            }

            // New items go to the end; the adaptive sort then moves them into place.
            for (int i = 0; i < addedItems.Count; i++)
            {
                GridItem item = addedItems[i];
                if (item.layer == this && addedSet.Remove(item))
                    sorted.Add(item);
            }

            addedItems.Clear();
            addedSet.Clear();
        }

        /*
         * Items are usually already sorted and edits move only a few of them, so an insertion sort finishes in about one pass.
         * Its cost grows with the distance the items move, so once the moves exceed what an introsort would cost, the rest goes to Span.Sort.
         * The insertion sort keeps the order of items with the same start, so unchanged items do not swap places between refreshes.
         */
        static void SortByStart(Span<GridItem> items)
        {
            long budget = (long)items.Length * (System.Numerics.BitOperations.Log2((uint)Math.Max(items.Length, 1)) + 1);
            long moves = 0;

            for (int i = 1; i < items.Length; i++)
            {
                GridItem current = items[i];
                double start = current.start;
                int j = i - 1;
                while (j >= 0 && items[j].start > start)
                {
                    items[j + 1] = items[j];
                    j--;
                    moves++;
                }

                items[j + 1] = current;
                if (moves > budget)
                {
                    items.Sort(static (x, y) => x.start.CompareTo(y.start));
                    return;
                }
            }
        }

        void MakeItemElement(GridItem item)
        {
            VisualElement element = MakeElement(item);
            element.AddToClassList(itemUssClassName);
            if (float.IsNaN(item.GetWidth(view!.axis)))
                element.RegisterCallback<GeometryChangedEvent>(OnItemGeometryChanged);

            item.element = element;
            Add(element);
        }

        // An element sized by USS is measured once it is drawn, which can change culling and stacking.
        void OnItemGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size != evt.newRect.size)
                MarkDirty();
        }

        void UpdateItemElement(GridItem item)
        {
            VisualElement element = item.element!;
            Rect rect = GetItemRect(item);

            element.style.translate = new Translate(rect.x, rect.y);
            if (!float.IsNaN(item.GetWidth(view!.axis)))
                element.style.width = rect.width;

            element.EnableInClassList(selectedItemUssClassName, item.selected);
        }

        void ReleaseItemElement(GridItem item)
        {
            VisualElement element = item.element!;
            item.element = null;

            // Only what the layer set is cleared, so the element keeps its own styles for reuse.
            element.UnregisterCallback<GeometryChangedEvent>(OnItemGeometryChanged);
            element.style.translate = StyleKeyword.Null;
            if (view != null && !float.IsNaN(item.GetWidth(view.axis)))
                element.style.width = StyleKeyword.Null;

            element.RemoveFromHierarchy();
            element.RemoveFromClassList(itemUssClassName);
            element.RemoveFromClassList(selectedItemUssClassName);
            ReleaseElement(item, element);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            view = GetFirstAncestorOfType<GridView>();
            ScheduleRefresh();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt) => view = null;
    }
}
