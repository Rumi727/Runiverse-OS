#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    public partial class GridView
    {
        /// <summary>
        /// The USS class name of <see cref="horizontalRangeSlider"/>.<br/>
        /// <see cref="horizontalRangeSlider"/>의 USS 클래스 이름입니다.
        /// </summary>
        public const string horizontalRangeSliderUssClassName = ussClassName + "__horizontal-range-slider";

        /// <summary>
        /// Gets the slider below the tracks that shows the visible range of <see cref="axis"/>. It is shown only while <see cref="GridAxis.scrollRange"/> is set.<br/>
        /// Dragging the middle pans the axis, and dragging either end moves only that end of the visible range, which zooms the axis while the other end stays.<br/>
        /// Its limits grow to hold the visible range, so the thumb stays at the edge while the view is past <see cref="GridAxis.scrollRange"/>.
        /// <br/><br/>
        /// 트랙 아래에서 <see cref="axis"/>의 보이는 범위를 보여 주는 슬라이더를 가져옵니다. <see cref="GridAxis.scrollRange"/>가 설정되어 있을 때만 표시됩니다.<br/>
        /// 가운데를 끌면 축을 이동하고, 한쪽 끝을 끌면 보이는 범위의 그쪽 끝만 옮기므로 반대쪽 끝을 고정한 채 축을 확대·축소합니다.<br/>
        /// 한계가 보이는 범위를 포함하도록 늘어나므로, 뷰가 <see cref="GridAxis.scrollRange"/>를 벗어나면 썸이 가장자리에 머뭅니다.
        /// </summary>
        public MinMaxSlider horizontalRangeSlider { get; }

        MinMaxSlider CreateHorizontalRangeSlider()
        {
            MinMaxSlider slider = new() { name = "horizontal-range-slider" };
            slider.AddToClassList(horizontalRangeSliderUssClassName);
            slider.style.display = DisplayStyle.None;
            slider.RegisterValueChangedCallback(OnHorizontalRangeChanged);
            return slider;
        }

        void OnHorizontalRangeChanged(ChangeEvent<Vector2> evt)
        {
            Vector2 range = evt.newValue;
            Vector2 previous = evt.previousValue;
            double length = range.y - range.x;

            // The zoom is limited by the axis. When only the low end was dragged, the offset is worked out from the limited zoom so that the high end stays.
            double pixelsPerUnit = (length > 0 ? axis.width / length : double.PositiveInfinity).Clamp(axis.minimumPixelsPerUnit, axis.maximumPixelsPerUnit);
            double offset = !range.x.Approximately(previous.x) && range.y.Approximately(previous.y) ? range.y - (axis.width / pixelsPerUnit) : range.x;
            axis.SetView(offset, pixelsPerUnit);
        }

        void UpdateHorizontalRangeSlider()
        {
            if (axis.scrollRange is not { } range)
            {
                horizontalRangeSlider.style.display = DisplayStyle.None;
                return;
            }

            horizontalRangeSlider.style.display = DisplayStyle.Flex;

            GridRange visible = axis.GetRange();
            float low = Min(range.start, visible.start).ClampToFloat();
            float high = Max(range.end, visible.end).ClampToFloat();

            // Setting a limit clamps the value and reports it as changed, which would move the axis, and a low limit above the high limit throws.
            // The limits are widened first, then the value is set, then the limits are narrowed around it.
            MinMaxSlider slider = horizontalRangeSlider;
            slider.highLimit = Max(slider.highLimit, high);
            slider.lowLimit = Min(slider.lowLimit, low);
            slider.SetValueWithoutNotify(new Vector2((float)visible.start, (float)visible.end));
            slider.lowLimit = low;
            slider.highLimit = high;
        }
    }
}
