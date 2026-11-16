#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// An interactive grid that runs horizontally, made of rulers at the top, a header column on the left, tracks that scroll vertically, an overlay, popups, and a horizontal scroll bar.<br/>
    /// The horizontal unit is not fixed; <see cref="axis"/> maps any <see cref="double"/> position to pixels.
    /// <br/><br/>
    /// 가로로 진행하는 상호작용 그리드입니다. 위쪽 눈금자, 왼쪽 헤더 열, 세로로 스크롤되는 트랙, 오버레이, 팝업, 가로 스크롤바로 이루어집니다.<br/>
    /// 가로 단위는 정해져 있지 않으며, <see cref="axis"/>가 임의의 <see cref="double"/> 위치를 픽셀로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Add rows with <see cref="AddTrack"/> and rulers with <see cref="AddRuler"/>. Pointer input is handled by <see cref="panZoom"/> and <see cref="selector"/>, which can be removed from <see cref="viewport"/> and replaced.<br/>
    /// Changes to the axis, items, and selection are applied together on the next frame.
    /// </para>
    /// <br/><br/>
    /// <para>
    /// 줄은 <see cref="AddTrack"/>으로, 눈금자는 <see cref="AddRuler"/>로 추가합니다. 포인터 입력은 <see cref="panZoom"/>과 <see cref="selector"/>가 처리하며, <see cref="viewport"/>에서 제거하고 바꿀 수 있습니다.<br/>
    /// 축, 아이템, 선택의 변경은 다음 프레임에 한꺼번에 반영됩니다.
    /// </para>
    /// </remarks>
    [UxmlElement]
    public partial class GridView : VisualElement
    {
        /// <summary>
        /// The USS class name of grid views.<br/>
        /// 그리드 뷰의 USS 클래스 이름입니다.
        /// </summary>
        public const string ussClassName = "runios-grid-view";

        /// <summary>
        /// The USS class name of <see cref="viewport"/>.<br/>
        /// <see cref="viewport"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string viewportUssClassName = ussClassName + "__viewport";

        /// <summary>
        /// The USS class name of the row that holds <see cref="corner"/> and <see cref="rulerContainer"/>.<br/>
        /// <see cref="corner"/>와 <see cref="rulerContainer"/>를 담는 줄의 USS 클래스 이름입니다.
        /// </summary>
        public const string topUssClassName = ussClassName + "__top";

        /// <summary>
        /// The USS class name of <see cref="corner"/>.<br/>
        /// <see cref="corner"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string cornerUssClassName = ussClassName + "__corner";

        /// <summary>
        /// The USS class name of <see cref="rulerContainer"/>.<br/>
        /// <see cref="rulerContainer"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string rulersUssClassName = ussClassName + "__rulers";

        /// <summary>
        /// The USS class name of <see cref="scrollView"/>.<br/>
        /// <see cref="scrollView"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string scrollViewUssClassName = ussClassName + "__scroll-view";

        /// <summary>
        /// The USS class name of <see cref="overlay"/>.<br/>
        /// <see cref="overlay"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string overlayUssClassName = ussClassName + "__overlay";

        /// <summary>
        /// The USS class name of <see cref="marquee"/>.<br/>
        /// <see cref="marquee"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string marqueeUssClassName = ussClassName + "__marquee";

        /// <summary>
        /// The USS class name added to <see cref="marquee"/> while it is shown.<br/>
        /// <see cref="marquee"/>가 표시되는 동안 추가되는 USS 클래스 이름입니다.
        /// </summary>
        public const string marqueeActiveUssClassName = marqueeUssClassName + "--active";

        /// <summary>
        /// The USS class name of <see cref="popupLayer"/>.<br/>
        /// <see cref="popupLayer"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string popupLayerUssClassName = ussClassName + "__popup-layer";

        /// <summary>
        /// The USS class name added to the grid while <see cref="readOnly"/> is <see langword="true"/>.<br/>
        /// <see cref="readOnly"/>가 <see langword="true"/>인 동안 그리드에 추가되는 USS 클래스 이름입니다.
        /// </summary>
        public const string readOnlyUssClassName = ussClassName + "--read-only";

        readonly Action refreshAction;

        /// <summary>
        /// Initializes an empty grid.<br/>
        /// 빈 그리드를 초기화합니다.
        /// </summary>
        public GridView()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            refreshAction = ApplyChanges;

            viewport = new VisualElement { name = "viewport", focusable = true };
            viewport.AddToClassList(viewportUssClassName);
            hierarchy.Add(viewport);

            VisualElement top = new() { name = "top" };
            top.AddToClassList(topUssClassName);
            viewport.Add(top);

            corner = new VisualElement { name = "corner" };
            corner.AddToClassList(cornerUssClassName);
            corner.style.width = headerWidth;
            top.Add(corner);

            rulerContainer = new VisualElement { name = "rulers" };
            rulerContainer.AddToClassList(rulersUssClassName);
            top.Add(rulerContainer);

            scrollView = new ScrollView(ScrollViewMode.Vertical) { name = "scroll-view", horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scrollView.AddToClassList(scrollViewUssClassName);
            scrollView.verticalScroller.valueChanged += _ => ScheduleTrackLayersRefresh();
            viewport.Add(scrollView);

            overlay = new SpanGridLayer { name = "overlay", pickingMode = PickingMode.Ignore, drawScaleLines = false };
            overlay.AddToClassList(overlayUssClassName);
            viewport.Add(overlay);

            marquee = new VisualElement { name = "marquee", pickingMode = PickingMode.Ignore };
            marquee.AddToClassList(marqueeUssClassName);
            viewport.Add(marquee);

            // Outside the viewport, so presses on it do not reach the manipulators and the overlay does not cover it.
            horizontalScroller = CreateHorizontalScroller();
            hierarchy.Add(horizontalScroller);

            popupLayer = new VisualElement { name = "popup-layer", pickingMode = PickingMode.Ignore };
            popupLayer.AddToClassList(popupLayerUssClassName);
            hierarchy.Add(popupLayer);

            selectionPopup = new GridPopup();

            axis.changed += ScheduleRefresh;
            axis.changed += UpdateHorizontalScroller;
            selection.changed += OnSelectionChanged;
            viewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateLayout());
            viewport.RegisterCallback<PointerMoveEvent>(evt => pointer = CreatePointerArgs(evt.position, -1, 0, evt.modifiers, evt.actionKey), TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerLeaveEvent>(_ => pointer = null);

            panZoom = new GridPanZoomManipulator(this);
            viewport.AddManipulator(panZoom);

            selector = new GridSelectManipulator(this);
            viewport.AddManipulator(selector);
        }

        /// <summary>
        /// Gets the horizontal axis.<br/>
        /// 가로축을 가져옵니다.
        /// </summary>
        public GridAxis axis { get; } = new();

        /// <summary>
        /// Gets the selected items.<br/>
        /// 선택된 아이템을 가져옵니다.
        /// </summary>
        public GridSelection selection { get; } = new();

        /// <summary>
        /// Gets the element that holds the rulers, tracks, and overlay, and receives pointer and keyboard input.<br/>
        /// 눈금자, 트랙, 오버레이를 담고 포인터와 키보드 입력을 받는 요소를 가져옵니다.
        /// </summary>
        public VisualElement viewport { get; }

        /// <summary>
        /// Gets the empty area above the header column, left of the rulers.<br/>
        /// 헤더 열 위, 눈금자 왼쪽의 빈 영역을 가져옵니다.
        /// </summary>
        public VisualElement corner { get; }

        /// <summary>
        /// Gets the element that holds the rulers from top to bottom.<br/>
        /// 눈금자를 위에서 아래로 담는 요소를 가져옵니다.
        /// </summary>
        public VisualElement rulerContainer { get; }

        /// <summary>
        /// Gets the vertical scroll view that holds the tracks.<br/>
        /// 트랙을 담는 세로 스크롤 뷰를 가져옵니다.
        /// </summary>
        public ScrollView scrollView { get; }

        /// <summary>
        /// Gets the layer drawn over all tracks and rulers, for things such as a playhead or loop range. It does not receive pointer input.<br/>
        /// 모든 트랙과 눈금자 위에 그려지는 레이어를 가져옵니다. 재생 헤드나 반복 구간 등에 쓰며, 포인터 입력은 받지 않습니다.
        /// </summary>
        public SpanGridLayer overlay { get; }

        /// <summary>
        /// Gets the box shown while selecting by dragging.<br/>
        /// 끌어서 선택하는 동안 표시되는 상자를 가져옵니다.
        /// </summary>
        public VisualElement marquee { get; }

        /// <summary>
        /// Gets the element that holds popups over the grid. It is outside <see cref="viewport"/>, so input in popups does not reach the grid.<br/>
        /// 그리드 위에 팝업을 담는 요소를 가져옵니다. <see cref="viewport"/> 밖에 있으므로 팝업 안의 입력은 그리드에 전달되지 않습니다.
        /// </summary>
        public VisualElement popupLayer { get; }

        /// <summary>
        /// Gets the popup shown below the selection; see <see cref="makeSelectionPopup"/>.<br/>
        /// 선택 아래에 표시되는 팝업을 가져옵니다. <see cref="makeSelectionPopup"/>을 참고하세요.
        /// </summary>
        public GridPopup selectionPopup { get; }

        /// <summary>
        /// Gets the manipulator that zooms and pans the grid.<br/>
        /// 그리드를 확대·축소하고 이동하는 조작기를 가져옵니다.
        /// </summary>
        public GridPanZoomManipulator panZoom { get; }

        /// <summary>
        /// Gets the manipulator that selects, moves, and clicks items.<br/>
        /// 아이템을 선택하고 옮기고 클릭하는 조작기를 가져옵니다.
        /// </summary>
        public GridSelectManipulator selector { get; }

        /// <summary>
        /// Gets what is under the pointer, or <see langword="null"/> while the pointer is outside the grid. Useful for pasting at the pointer.<br/>
        /// 포인터 아래에 있는 것을 가져옵니다. 포인터가 그리드 밖에 있으면 <see langword="null"/>입니다. 포인터 위치에 붙여 넣을 때 유용합니다.
        /// </summary>
        public GridPointerArgs? pointer { get; private set; }

        /// <summary>
        /// Gets the tracks, from top to bottom.<br/>
        /// 트랙을 위에서 아래 순서로 가져옵니다.
        /// </summary>
        public IEnumerable<GridTrack> tracks => scrollView.contentContainer.Children().OfType<GridTrack>();

        /// <summary>
        /// Gets the rulers, from top to bottom.<br/>
        /// 눈금자를 위에서 아래 순서로 가져옵니다.
        /// </summary>
        public IEnumerable<GridRuler> rulers => rulerContainer.Children().OfType<GridRuler>();

        /// <summary>
        /// Gets <see cref="overlay"/> followed by the layers of all tracks.<br/>
        /// <see cref="overlay"/>와 그 뒤로 모든 트랙의 레이어를 가져옵니다.
        /// </summary>
        public IEnumerable<GridLayer> layers => tracks.Select(static x => x.layer).Prepend(overlay);

        /// <inheritdoc/>
        public override VisualElement contentContainer => scrollView.contentContainer;

        /// <summary>
        /// Gets or sets whether editing is blocked. Moving items, popups, deleting, and pasting are blocked; zooming, panning, selecting, and pointer events still work.<br/>
        /// 편집을 막는지 여부를 가져오거나 설정합니다. 아이템 이동, 팝업, 삭제, 붙여넣기를 막으며, 확대·축소, 이동, 선택, 포인터 이벤트는 그대로 동작합니다.
        /// </summary>
        [UxmlAttribute]
        public bool readOnly
        {
            get;
            set
            {
                field = value;
                EnableInClassList(readOnlyUssClassName, value);

                for (int i = 0; i < popupLayer.childCount; i++)
                {
                    if (popupLayer[i] is GridPopup popup)
                        popup.SetEnabled(popup.enabledWhenReadOnly || !value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the header column in pixels, shared by <see cref="corner"/> and every <see cref="GridTrack.header"/>.<br/>
        /// <see cref="corner"/>와 모든 <see cref="GridTrack.header"/>가 함께 쓰는 헤더 열의 픽셀 폭을 가져오거나 설정합니다.
        /// </summary>
        [UxmlAttribute]
        public float headerWidth
        {
            get;
            set
            {
                field = value;
                corner.style.width = value;
                foreach (GridTrack track in tracks)
                    track.header.style.width = value;

                UpdateLayout();
            }
        }

        /// <summary>
        /// Gets or sets the modifier key that turns snapping off while it is held, for example while dragging items. <see cref="EventModifiers.None"/> always snaps.<br/>
        /// 누르고 있는 동안 아이템 끌기 등에서 맞춤(자석)을 끄는 보조 키를 가져오거나 설정합니다. <see cref="EventModifiers.None"/>이면 항상 맞춥니다.
        /// </summary>
        /// <remarks>
        /// Only horizontal snapping is affected. Vertical snapping such as <see cref="PlaneGridLayer.verticalSnap"/> keeps the data valid, so it always applies.<br/>
        /// 가로 맞춤에만 적용됩니다. <see cref="PlaneGridLayer.verticalSnap"/> 같은 세로 맞춤은 데이터를 올바르게 유지하므로 항상 적용됩니다.
        /// </remarks>
        public EventModifiers snapBypassModifier { get; set; } = EventModifiers.Shift;

        /// <summary>
        /// Snaps <paramref name="position"/> with <see cref="GridAxis.Snap"/>, unless <paramref name="modifiers"/> include <see cref="snapBypassModifier"/>.<br/>
        /// <paramref name="modifiers"/>에 <see cref="snapBypassModifier"/>가 없으면 <paramref name="position"/>을 <see cref="GridAxis.Snap"/>으로 맞춥니다.
        /// </summary>
        /// <param name="position">
        /// The axis position.<br/>
        /// 축 위치입니다.
        /// </param>
        /// <param name="modifiers">
        /// The modifier keys held down.<br/>
        /// 눌려 있는 보조 키입니다.
        /// </param>
        /// <returns>
        /// The snapped position, or <paramref name="position"/> unchanged while the bypass key is held.<br/>
        /// 맞춘 위치를 반환하며, 해제 키를 누르고 있으면 <paramref name="position"/>을 그대로 반환합니다.
        /// </returns>
        public double Snap(double position, EventModifiers modifiers)
        {
            if (snapBypassModifier != EventModifiers.None && (modifiers & snapBypassModifier) != 0)
                return position;

            return axis.Snap(position);
        }

        /// <summary>
        /// Gets or sets the function that makes the content of <see cref="selectionPopup"/> whenever the selection changes. Return <see langword="null"/> to show no popup.<br/>
        /// The popup closes when nothing is selected.
        /// <br/><br/>
        /// 선택이 바뀔 때마다 <see cref="selectionPopup"/>의 내용을 만드는 함수를 가져오거나 설정합니다. <see langword="null"/>을 반환하면 팝업을 표시하지 않습니다.<br/>
        /// 선택이 비면 팝업이 닫힙니다.
        /// </summary>
        public Func<GridSelection, VisualElement?>? makeSelectionPopup
        {
            get;
            set
            {
                field = value;
                ScheduleSelectionPopup();
            }
        }

        /// <summary>
        /// Adds a ruler that shows <paramref name="scale"/> below the existing rulers.<br/>
        /// <paramref name="scale"/>을 보여 주는 눈금자를 기존 눈금자 아래에 추가합니다.
        /// </summary>
        /// <param name="scale">
        /// The scale to show.<br/>
        /// 보여 줄 스케일입니다.
        /// </param>
        /// <returns>
        /// The added ruler.<br/>
        /// 추가한 눈금자를 반환합니다.
        /// </returns>
        public GridRuler AddRuler(GridScale scale)
        {
            GridRuler ruler = new(scale);
            rulerContainer.Add(ruler);
            return ruler;
        }

        /// <summary>
        /// Adds a track that shows <paramref name="layer"/> below the existing tracks. Remove it with <see cref="VisualElement.RemoveFromHierarchy"/>.<br/>
        /// <paramref name="layer"/>를 보여 주는 트랙을 기존 트랙 아래에 추가합니다. 제거하려면 <see cref="VisualElement.RemoveFromHierarchy"/>를 호출하세요.
        /// </summary>
        /// <param name="layer">
        /// The layer of the track.<br/>
        /// 트랙의 레이어입니다.
        /// </param>
        /// <returns>
        /// The added track.<br/>
        /// 추가한 트랙을 반환합니다.
        /// </returns>
        public GridTrack AddTrack(GridLayer layer)
        {
            GridTrack track = new(layer);
            Add(track);
            return track;
        }

        bool refreshScheduled;

        // The axis changed or a whole refresh was asked for, so every layer and ruler is refreshed on the next pass, not only the layers that marked themselves.
        bool refreshAll;

        /// <summary>
        /// Refreshes the whole grid on the next frame: every layer, ruler, and popup. Calling it many times in one frame refreshes once.<br/>
        /// Changes to the axis call this. Changes to the items of a layer, the selection, and vertical scrolling refresh only the layers they affect without it.
        /// <br/><br/>
        /// 다음 프레임에 그리드 전체를 갱신합니다. 모든 레이어, 눈금자, 팝업을 갱신합니다. 한 프레임에 여러 번 호출해도 한 번만 갱신합니다.<br/>
        /// 축이 바뀌면 이 메서드가 호출됩니다. 레이어의 아이템, 선택, 세로 스크롤의 변경은 이 메서드 없이 영향을 받는 레이어만 갱신합니다.
        /// </summary>
        public void ScheduleRefresh()
        {
            refreshAll = true;
            ScheduleChanges();
        }

        // Applies the pending changes on the next frame, once however many times it is called.
        internal void ScheduleChanges()
        {
            if (refreshScheduled)
                return;

            refreshScheduled = true;
            schedule.Execute(refreshAction);
        }

        bool selectionPopupDirty;

        /// <summary>
        /// Refreshes the whole grid now: every layer, ruler, and popup.<br/>
        /// 지금 그리드 전체를 갱신합니다. 모든 레이어, 눈금자, 팝업을 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            refreshAll = true;
            ApplyChanges();
        }

        void ApplyChanges()
        {
            refreshScheduled = false;
            bool all = refreshAll;
            refreshAll = false;

            // This runs every frame while something moves, so the children are walked by index instead of through the LINQ properties.
            if (all || overlay.refreshPending)
                overlay.Refresh();

            VisualElement trackContainer = scrollView.contentContainer;
            for (int i = 0; i < trackContainer.childCount; i++)
            {
                if (trackContainer[i] is GridTrack track && (all || track.layer.refreshPending))
                    track.layer.Refresh();
            }

            // The rulers show only the axis, so they are repainted only with the whole grid.
            if (all)
            {
                for (int i = 0; i < rulerContainer.childCount; i++)
                    rulerContainer[i].MarkDirtyRepaint();
            }

            if (selectionPopupDirty)
            {
                selectionPopupDirty = false;
                if (selection.Count > 0 && makeSelectionPopup?.Invoke(selection) is { } content)
                    selectionPopup.Show(this, selection, content);
                else
                    selectionPopup.Close();
            }

            for (int i = 0; i < popupLayer.childCount; i++)
            {
                if (popupLayer[i] is GridPopup popup)
                    popup.UpdatePosition();
            }
        }

        // Any layer can hold the items that joined or left the selection, so every layer is refreshed, but not the rulers.
        void OnSelectionChanged()
        {
            overlay.ScheduleRefresh();
            ScheduleTrackLayersRefresh();
            ScheduleSelectionPopup();
        }

        void ScheduleSelectionPopup()
        {
            selectionPopupDirty = true;
            ScheduleChanges();
        }

        // Vertical scrolling moves the tracks without changing their geometry, so the layers do not notice it by themselves.
        void ScheduleTrackLayersRefresh()
        {
            VisualElement trackContainer = scrollView.contentContainer;
            for (int i = 0; i < trackContainer.childCount; i++)
            {
                if (trackContainer[i] is GridTrack track)
                    track.layer.ScheduleRefresh();
            }
        }

        void UpdateLayout()
        {
            overlay.style.left = headerWidth;
            horizontalScroller.style.marginLeft = headerWidth;

            float width = viewport.layout.width;
            if (!float.IsNaN(width))
                axis.width = Math.Max(0, width - headerWidth);
        }
    }
}
