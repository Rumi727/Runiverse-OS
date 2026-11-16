#nullable enable
using UnityEngine;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A layer whose items take its full height, such as a playhead, loop range, or marker.<br/>
    /// Its height is not set by the layer, so give it one with USS when using it as a track.
    /// <br/><br/>
    /// 아이템이 레이어 높이 전체를 차지하는 레이어입니다. 재생 헤드, 반복 구간, 마커 등에 씁니다.<br/>
    /// 레이어가 높이를 정하지 않으므로, 트랙으로 쓸 때는 USS로 높이를 주세요.
    /// </summary>
    public class SpanGridLayer : GridLayer<GridItem>
    {
        /// <summary>
        /// The USS class name of span grid layers.<br/>
        /// 스팬 그리드 레이어의 USS 클래스 이름입니다.
        /// </summary>
        public new const string ussClassName = "runios-span-grid-layer";

        /// <summary>
        /// Initializes the layer.<br/>
        /// 레이어를 초기화합니다.
        /// </summary>
        public SpanGridLayer() => AddToClassList(ussClassName);

        /// <inheritdoc/>
        protected override void Arrange() { }

        /// <inheritdoc/>
        public override Rect GetItemRect(GridItem item)
        {
            (float x, float width) = GetHorizontalPixels(item);
            return new Rect(x, 0, width, layout.height);
        }

        /// <summary>
        /// Converts a vertical layer pixel to a fraction of the layer height, from <c>0</c> at the top to <c>1</c> at the bottom.<br/>
        /// 레이어의 세로 픽셀을 레이어 높이에 대한 비율로 변환합니다. 위쪽이 <c>0</c>, 아래쪽이 <c>1</c>입니다.
        /// </summary>
        /// <inheritdoc/>
        public override double GetVerticalPosition(float localY) => localY / layout.height;
    }
}
