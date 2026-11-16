#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public partial class GridView
    {
        /// <summary>
        /// The USS class name of <see cref="horizontalScroller"/>.<br/>
        /// <see cref="horizontalScroller"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string horizontalScrollerUssClassName = ussClassName + "__horizontal-scroller";

        // The buttons move as far and wait as long as the buttons of a ScrollView scroller, whose value is in pixels.
        const float scrollerButtonDistance = 20;
        const long scrollerButtonDelay = 250;
        const long scrollerButtonInterval = 30;

        /// <summary>
        /// Gets the horizontal scroll bar below the tracks. It is shown only while <see cref="GridAxis.scrollRange"/> is set.<br/>
        /// Its range grows to hold the visible range, so the thumb stays at the edge while the view is past <see cref="GridAxis.scrollRange"/>, and its buttons keep moving past it.
        /// <br/><br/>
        /// 트랙 아래의 가로 스크롤바를 가져옵니다. <see cref="GridAxis.scrollRange"/>가 설정되어 있을 때만 표시됩니다.<br/>
        /// 범위가 보이는 범위를 포함하도록 늘어나므로, 뷰가 <see cref="GridAxis.scrollRange"/>를 벗어나면 썸이 가장자리에 머물고, 버튼은 범위를 넘어서도 계속 이동합니다.
        /// </summary>
        public Scroller horizontalScroller { get; }

        Scroller CreateHorizontalScroller()
        {
            Scroller scroller = new(0, 0, value => axis.SetView(value, axis.pixelsPerUnit), SliderDirection.Horizontal) { name = "horizontal-scroller" };
            scroller.AddToClassList(horizontalScrollerUssClassName);
            scroller.style.display = DisplayStyle.None;

            // The buttons pan the axis instead of stepping the value, which the slider would keep inside its range.
            scroller.lowButton.SetAction(() => axis.Pan(scrollerButtonDistance), scrollerButtonDelay, scrollerButtonInterval);
            scroller.highButton.SetAction(() => axis.Pan(-scrollerButtonDistance), scrollerButtonDelay, scrollerButtonInterval);
            return scroller;
        }

        void UpdateHorizontalScroller()
        {
            if (axis.scrollRange is not { } range)
            {
                horizontalScroller.style.display = DisplayStyle.None;
                return;
            }

            horizontalScroller.style.display = DisplayStyle.Flex;

            double offset = axis.offset;
            double visibleLength = axis.width / axis.pixelsPerUnit;
            float low = (float)Math.Min(range.start, offset);
            float high = (float)Math.Max(range.end - visibleLength, offset);

            // Setting a bound clamps the value and reports it as changed, which would move the axis.
            // The bounds are widened first, then the value is set, then the bounds are narrowed around it.
            Slider slider = horizontalScroller.slider;
            slider.lowValue = Math.Min(slider.lowValue, low);
            slider.highValue = Math.Max(slider.highValue, high);
            slider.SetValueWithoutNotify((float)offset);
            slider.lowValue = low;
            slider.highValue = high;
            slider.pageSize = (float)visibleLength;

            double totalLength = high - low + visibleLength;
            horizontalScroller.Adjust(totalLength > 0 ? (float)(visibleLength / totalLength) : 1);

            // Adjust disables the scroller once everything fits, but the buttons pan the axis and still move past the range.
            horizontalScroller.SetEnabled(true);
        }
    }
}
