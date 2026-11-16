#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A popup attached below some items of a <see cref="GridView"/>, centered on them. It follows the items while the view zooms, scrolls, or the items move.<br/>
    /// Input inside the popup does not reach the grid.
    /// <br/><br/>
    /// <see cref="GridView"/>의 아이템들 아래 가운데에 붙는 팝업입니다. 확대·축소, 스크롤, 아이템 이동 중에도 아이템을 따라갑니다.<br/>
    /// 팝업 안의 입력은 그리드에 전달되지 않습니다.
    /// </summary>
    /// <remarks>
    /// While <see cref="GridView.readOnly"/> is <see langword="true"/>, the popup is disabled unless <see cref="enabledWhenReadOnly"/> is <see langword="true"/>.<br/>
    /// <see cref="GridView.readOnly"/>가 <see langword="true"/>인 동안에는 <see cref="enabledWhenReadOnly"/>가 <see langword="true"/>가 아니면 팝업이 비활성화됩니다.
    /// </remarks>
    public class GridPopup : VisualElement
    {
        /// <summary>
        /// The USS class name of grid popups.<br/>
        /// 그리드 팝업의 USS 클래스 이름입니다.
        /// </summary>
        public const string ussClassName = "runios-grid-popup";

        GridItem[] anchorItems = [];

        /// <summary>
        /// Initializes a hidden popup.<br/>
        /// 숨겨진 팝업을 초기화합니다.
        /// </summary>
        public GridPopup()
        {
            AddToClassList(ussClassName);
            RegisterCallback<GeometryChangedEvent>(_ => UpdatePosition());
        }

        /// <summary>
        /// Gets the grid the popup is shown on, or <see langword="null"/> while it is hidden.<br/>
        /// 팝업이 표시된 그리드를 가져옵니다. 숨겨져 있으면 <see langword="null"/>입니다.
        /// </summary>
        public GridView? view { get; private set; }

        /// <summary>
        /// Gets or sets whether the popup stays enabled while <see cref="GridView.readOnly"/> is <see langword="true"/>, for a popup that only shows information. The default is <see langword="false"/>.<br/>
        /// <see cref="GridView.readOnly"/>가 <see langword="true"/>인 동안에도 팝업을 활성 상태로 둘지 여부를 가져오거나 설정하며, 정보만 보여 주는 팝업에 씁니다. 기본값은 <see langword="false"/>입니다.
        /// </summary>
        public bool enabledWhenReadOnly
        {
            get;
            set
            {
                field = value;
                if (view != null)
                    SetEnabled(value || !view.readOnly);
            }
        }

        /// <summary>
        /// Gets the items the popup is attached to.<br/>
        /// 팝업이 붙어 있는 아이템을 가져옵니다.
        /// </summary>
        public IReadOnlyList<GridItem> anchors => anchorItems;

        /// <summary>
        /// Shows <paramref name="content"/> in the popup, below <paramref name="anchors"/> on <paramref name="view"/>. Any previous content is removed.<br/>
        /// <paramref name="view"/>에서 <paramref name="anchors"/> 아래에 <paramref name="content"/>를 팝업으로 표시합니다. 이전 내용은 제거됩니다.
        /// </summary>
        /// <param name="view">
        /// The grid to show the popup on.<br/>
        /// 팝업을 표시할 그리드입니다.
        /// </param>
        /// <param name="anchors">
        /// The items to attach to.<br/>
        /// 붙을 아이템입니다.
        /// </param>
        /// <param name="content">
        /// The content of the popup.<br/>
        /// 팝업의 내용입니다.
        /// </param>
        public void Show(GridView view, IEnumerable<GridItem> anchors, VisualElement content)
        {
            Clear();
            Add(content);

            anchorItems = [.. anchors];
            if (this.view != view)
            {
                RemoveFromHierarchy();
                view.popupLayer.Add(this);
                this.view = view;
            }

            SetEnabled(enabledWhenReadOnly || !view.readOnly);
            UpdatePosition();
        }

        /// <summary>
        /// Hides the popup and removes its content.<br/>
        /// 팝업을 숨기고 내용을 제거합니다.
        /// </summary>
        public void Close()
        {
            RemoveFromHierarchy();
            Clear();

            anchorItems = [];
            view = null;
        }

        /// <summary>
        /// Moves the popup below its anchors. <see cref="GridView"/> calls this on every refresh.<br/>
        /// 팝업을 붙은 아이템 아래로 옮깁니다. <see cref="GridView"/>가 갱신할 때마다 호출합니다.
        /// </summary>
        public void UpdatePosition()
        {
            if (view is not { } gridView)
                return;

            VisualElement popupLayer = gridView.popupLayer;
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.NegativeInfinity;
            foreach (GridItem item in anchorItems)
            {
                if (item.layer is not { } layer || layer.view != gridView)
                    continue;

                Rect rect = layer.ChangeCoordinatesTo(popupLayer, layer.GetItemRect(item));
                left = Math.Min(left, rect.xMin);
                right = Math.Max(right, rect.xMax);
                bottom = Math.Max(bottom, rect.yMax);
            }

            if (left > right)
                return;

            float width = float.IsNaN(resolvedStyle.width) ? 0 : resolvedStyle.width;
            float x = Math.Clamp(((left + right) * 0.5f) - (width * 0.5f), 0, Math.Max(0, popupLayer.layout.width - width));
            style.translate = new Translate(x, bottom);
        }
    }
}
