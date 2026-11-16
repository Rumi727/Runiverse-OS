#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The place of one piece of data on a grid. Derive from it to keep a reference to your own data.<br/>
    /// A visual element exists for the item only while it is on screen; see <see cref="GridLayer{TItem}.makeElement"/>.
    /// <br/><br/>
    /// 데이터 하나가 그리드에서 차지하는 자리입니다. 상속해서 자신의 데이터 참조를 담으세요.<br/>
    /// 아이템의 요소는 화면에 보이는 동안에만 존재합니다. <see cref="GridLayer{TItem}.makeElement"/>를 참고하세요.
    /// </summary>
    /// <remarks>
    /// Changing a value updates the grid on the next frame, so values may be changed freely at any time.<br/>
    /// 값을 바꾸면 다음 프레임에 그리드가 갱신되므로, 언제든 자유롭게 바꿔도 됩니다.
    /// </remarks>
    public class GridItem
    {
        /// <summary>
        /// Gets or sets the axis position where the item starts.<br/>
        /// 아이템이 시작하는 축 위치를 가져오거나 설정합니다.
        /// </summary>
        public double start
        {
            get;
            set
            {
                if (field.Approximately(value))
                    return;

                field = value;
                layer?.MarkDirty();
            }
        }

        /// <summary>
        /// Gets the pixel width of the item at the current zoom, or <see cref="float.NaN"/> to leave the width to the element.<br/>
        /// Override it for items that span part of the axis, for example <c>(float)(length * axis.pixelsPerUnit)</c>.
        /// <br/><br/>
        /// 현재 확대 수준에서 아이템의 픽셀 폭을 가져옵니다. 폭을 요소에 맡기려면 <see cref="float.NaN"/>을 반환합니다.<br/>
        /// 축의 일부 구간을 차지하는 아이템은 재정의하세요. 예: <c>(float)(length * axis.pixelsPerUnit)</c>.
        /// </summary>
        /// <param name="axis">
        /// The axis of the grid.<br/>
        /// 그리드의 축입니다.
        /// </param>
        /// <returns>
        /// The width in pixels, or <see cref="float.NaN"/>. The default returns <see cref="float.NaN"/>.<br/>
        /// 픽셀 폭을 반환하거나 <see cref="float.NaN"/>을 반환합니다. 기본 구현은 <see cref="float.NaN"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>A number: the grid sets the element to this width and uses it for culling, stacking, and hit testing.</description></item>
        /// <item><description><see cref="float.NaN"/>: the grid does not touch the element width, and uses the drawn width of the element, or <see cref="GridLayer.defaultItemWidth"/> while there is no element yet.</description></item>
        /// </list>
        /// Call <see cref="MarkDirty"/> when the value this method depends on changes.
        /// <br/><br/>
        /// <list type="bullet">
        /// <item><description>숫자: 그리드가 요소 폭을 이 값으로 정하고 컬링, 쌓기, 히트 테스트에 씁니다.</description></item>
        /// <item><description><see cref="float.NaN"/>: 그리드가 요소 폭을 건드리지 않고, 그려진 요소의 폭을 쓰며 아직 요소가 없으면 <see cref="GridLayer.defaultItemWidth"/>를 씁니다.</description></item>
        /// </list>
        /// 이 메서드가 의존하는 값이 바뀌면 <see cref="MarkDirty"/>를 호출하세요.
        /// </remarks>
        public virtual float GetWidth(GridAxis axis) => float.NaN;

        /// <summary>
        /// Tells the layer that something it uses changed, such as a value that <see cref="GetWidth"/> depends on. The layer updates on the next frame.<br/>
        /// <see cref="GetWidth"/>가 의존하는 값처럼 레이어가 쓰는 무언가가 바뀌었다고 알립니다. 레이어는 다음 프레임에 갱신됩니다.
        /// </summary>
        protected void MarkDirty() => layer?.MarkDirty();

        /// <summary>
        /// Gets the layer that owns the item, or <see langword="null"/> if it is in no layer.<br/>
        /// 아이템을 가진 레이어를 가져옵니다. 어느 레이어에도 없으면 <see langword="null"/>입니다.
        /// </summary>
        /// <remarks>
        /// Set by <see cref="GridItemCollection{TItem}"/> when the item is added or removed.<br/>
        /// 아이템을 추가하거나 제거할 때 <see cref="GridItemCollection{TItem}"/>가 설정합니다.
        /// </remarks>
        public GridLayer? layer { get; internal set; }

        /// <summary>
        /// Gets the element shown for the item, or <see langword="null"/> while it is off screen or drawn without an element.<br/>
        /// 아이템에 표시되는 요소를 가져옵니다. 화면 밖이거나 요소 없이 그려지는 동안에는 <see langword="null"/>입니다.
        /// </summary>
        public VisualElement? element { get; internal set; }

        /// <summary>
        /// Gets whether the item is in the <see cref="GridView.selection"/> of its grid.<br/>
        /// 아이템이 그리드의 <see cref="GridView.selection"/>에 있는지 여부를 가져옵니다.
        /// </summary>
        public bool selected => layer?.view?.selection.Contains(this) == true;
    }
}
