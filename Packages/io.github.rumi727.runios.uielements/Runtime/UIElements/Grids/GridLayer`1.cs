#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A grid layer that holds items of type <typeparamref name="TItem"/>.<br/>
    /// Set <see cref="makeElement"/> to show items as elements; without it, items are drawn as rectangles.
    /// <br/><br/>
    /// <typeparamref name="TItem"/> 타입 아이템을 가지는 그리드 레이어입니다.<br/>
    /// 아이템을 요소로 보여 주려면 <see cref="makeElement"/>를 설정하세요. 설정하지 않으면 아이템을 사각형으로 그립니다.
    /// </summary>
    /// <typeparam name="TItem">
    /// The item type.<br/>
    /// 아이템 타입입니다.
    /// </typeparam>
    public abstract class GridLayer<TItem> : GridLayer where TItem : GridItem
    {
        /// <summary>
        /// Initializes the layer with an empty item collection.<br/>
        /// 빈 아이템 컬렉션으로 레이어를 초기화합니다.
        /// </summary>
        protected GridLayer() => items = new GridItemCollection<TItem>(this);

        /// <summary>
        /// Gets the items of the layer.<br/>
        /// 레이어의 아이템을 가져옵니다.
        /// </summary>
        public GridItemCollection<TItem> items { get; }

        /// <summary>
        /// Gets or sets the function called when an item comes on screen, which returns the element to show for it.<br/>
        /// Elements are not reused by the layer. Reuse them yourself in <see cref="releaseElement"/> if needed.
        /// <br/><br/>
        /// 아이템이 화면에 들어올 때 호출되어 그 아이템에 표시할 요소를 반환하는 함수를 가져오거나 설정합니다.<br/>
        /// 레이어는 요소를 재사용하지 않습니다. 필요하면 <see cref="releaseElement"/>에서 직접 재사용하세요.
        /// </summary>
        /// <remarks>
        /// The layer sets the position and size of the element and adds <see cref="GridLayer.itemUssClassName"/> to it.<br/>
        /// 레이어가 요소의 위치와 크기를 정하고 <see cref="GridLayer.itemUssClassName"/>을 추가합니다.
        /// </remarks>
        public Func<TItem, VisualElement>? makeElement
        {
            get;
            set
            {
                field = value;
                ScheduleRefresh();
            }
        }

        /// <summary>
        /// Gets or sets the function called when an item goes off screen or is removed, with the element that was shown for it.<br/>
        /// 아이템이 화면에서 나가거나 제거될 때, 그 아이템에 표시되던 요소와 함께 호출되는 함수를 가져오거나 설정합니다.
        /// </summary>
        public Action<TItem, VisualElement>? releaseElement { get; set; }

        /// <summary>
        /// Gets or sets the function that gives the color of an item drawn as a rectangle, or <see langword="null"/> for the USS item color.<br/>
        /// 사각형으로 그리는 아이템의 색을 주는 함수를 가져오거나 설정합니다. <see langword="null"/>이면 USS 아이템 색을 씁니다.
        /// </summary>
        public Func<TItem, Color>? denseColor { get; set; }

        /// <inheritdoc/>
        protected override bool usesElements => makeElement != null;

        /// <inheritdoc/>
        protected override VisualElement MakeElement(GridItem item) => makeElement!((TItem)item);

        /// <inheritdoc/>
        protected override void ReleaseElement(GridItem item, VisualElement element) => releaseElement?.Invoke((TItem)item, element);

        /// <inheritdoc/>
        protected override Color? GetDenseColor(GridItem item) => denseColor?.Invoke((TItem)item);
    }
}
