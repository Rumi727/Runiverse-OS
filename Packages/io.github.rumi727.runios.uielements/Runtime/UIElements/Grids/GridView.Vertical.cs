#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public partial class GridView
    {
        // The scroll offset that ZoomVertical keeps the anchor at, or NaN when there is none.
        // The scroll view limits its offset to the current content height, which the layers change only on the next refresh and layout, so it is applied once the content has its new geometry.
        float pendingVerticalScrollOffset = float.NaN;

        /// <summary>
        /// Gets or sets the vertical zoom of the tracks. Layers that use it, such as <see cref="PlaneGridLayer"/> and <see cref="LaneGridLayer"/>, multiply their heights by it. The default is <c>1</c>.<br/>
        /// The value is kept between <see cref="minimumVerticalScale"/> and <see cref="maximumVerticalScale"/>. Setting it does not move <see cref="ScrollView.scrollOffset"/>; use <see cref="ZoomVertical"/> to keep a point in place.
        /// <br/><br/>
        /// 트랙의 세로 배율을 가져오거나 설정합니다. <see cref="PlaneGridLayer"/>, <see cref="LaneGridLayer"/>처럼 이를 쓰는 레이어는 높이에 이 값을 곱합니다. 기본값은 <c>1</c>입니다.<br/>
        /// 값은 <see cref="minimumVerticalScale"/>과 <see cref="maximumVerticalScale"/> 사이로 유지됩니다. 설정해도 <see cref="ScrollView.scrollOffset"/>은 움직이지 않으며, 한 지점을 고정하려면 <see cref="ZoomVertical"/>을 쓰세요.
        /// </summary>
        public double verticalScale
        {
            get => _verticalScale;
            set
            {
                value = Math.Clamp(value, minimumVerticalScale, maximumVerticalScale);
                if (_verticalScale.Approximately(value))
                    return;

                _verticalScale = value;
                ScheduleRefresh();
                verticalScaleChanged?.Invoke();
            }
        }
        double _verticalScale = 1;

        /// <summary>
        /// Gets or sets the smallest allowed <see cref="verticalScale"/>, which limits zooming out vertically.<br/>
        /// 허용되는 가장 작은 <see cref="verticalScale"/>을 가져오거나 설정합니다. 세로 축소 한계를 정합니다.
        /// </summary>
        public double minimumVerticalScale { get; set; } = 1e-6;

        /// <summary>
        /// Gets or sets the largest allowed <see cref="verticalScale"/>, which limits zooming in vertically.<br/>
        /// 허용되는 가장 큰 <see cref="verticalScale"/>을 가져오거나 설정합니다. 세로 확대 한계를 정합니다.
        /// </summary>
        public double maximumVerticalScale { get; set; } = 1e6;

        /// <summary>
        /// Occurs when <see cref="verticalScale"/> changes. Use it to resize elements that take their height from the scale, because the grid never sets element heights.<br/>
        /// <see cref="verticalScale"/>이 바뀌면 발생합니다. 그리드는 요소 높이를 정하지 않으므로, 배율에 따라 높이가 정해지는 요소의 크기를 바꿀 때 쓰세요.
        /// </summary>
        public event Action? verticalScaleChanged;

        /// <summary>
        /// Multiplies <see cref="verticalScale"/> by <paramref name="factor"/> while keeping the content under <paramref name="anchorY"/> in place.<br/>
        /// The scroll offset is applied after the tracks are laid out with the new scale, because the scroll view cannot scroll past its current content.
        /// <br/><br/>
        /// <paramref name="anchorY"/> 아래의 내용을 고정한 채 <see cref="verticalScale"/>에 <paramref name="factor"/>를 곱합니다.<br/>
        /// 스크롤 뷰는 현재 내용 너머로 스크롤할 수 없으므로, 스크롤 오프셋은 트랙이 새 배율로 배치된 뒤에 적용됩니다.
        /// </summary>
        /// <param name="factor">
        /// The zoom factor. Values above <c>1</c> zoom in.<br/>
        /// 확대 배율입니다. <c>1</c>보다 크면 확대합니다.
        /// </param>
        /// <param name="anchorY">
        /// The vertical pixel in <see cref="ScrollView.contentViewport"/> of <see cref="scrollView"/> that stays on the same content, usually the pointer.<br/>
        /// <see cref="scrollView"/>의 <see cref="ScrollView.contentViewport"/> 안에서 같은 내용에 머무는 세로 픽셀이며, 보통 포인터 위치입니다.
        /// </param>
        public void ZoomVertical(double factor, float anchorY)
        {
            double oldScale = _verticalScale;
            double newScale = Math.Clamp(oldScale * factor, minimumVerticalScale, maximumVerticalScale);
            if (oldScale.Approximately(newScale))
                return;

            // Zooming again before the last offset was applied starts from that offset, so the anchor does not drift.
            float offset = float.IsNaN(pendingVerticalScrollOffset) ? scrollView.scrollOffset.y : pendingVerticalScrollOffset;
            pendingVerticalScrollOffset = (float)(((offset + anchorY) * (newScale / oldScale)) - anchorY);
            verticalScale = newScale;
        }

        // Registered after the scroll view registers its own handler, so its scroller already spans the new content height here.
        void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            if (float.IsNaN(pendingVerticalScrollOffset))
                return;

            float offset = pendingVerticalScrollOffset;
            pendingVerticalScrollOffset = float.NaN;
            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, offset);
        }
    }
}
