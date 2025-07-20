#nullable enable
using UnityEngine.UIElements;

namespace RuniEngine.UI
{
    [UxmlElement]
    public partial class UnclampedSlider : Slider, IUnclampedSlider<float>
    {
        public const string outOfRangeUssClassName = "runios-unclamped-slider--out-of-range";
        public const string outOfLowUssClassName = "runios-unclamped-slider--out-of-low";
        public const string outOfHighUssClassName = "runios-unclamped-slider--out-of-high";

        public bool isOutOfRange => isOutOfLow || isOutOfHigh;

        public bool isOutOfLow => value < lowValue;
        public bool isOutOfHigh => value > highValue;

        float IUnclampedSlider<float>.value
        {
            get => value;
            set => this.value = value;
        }
        object IUnclampedSlider.value
        {
            get => value;
            set => this.value = value as float? ?? 0;
        }

        [UxmlAttribute, Tooltip("ui.unclamped_slider.tooltip")] public SerializableNullable<float> logicalMinValue { get; set; }
        float? IUnclampedSlider<float>.logicalMinValue
        {
            get => logicalMinValue;
            set => logicalMinValue = value;
        }
        object? IUnclampedSlider.logicalMinValue
        {
            get => logicalMinValue;
            set => logicalMinValue = value as float?;
        }

        [UxmlAttribute, Tooltip("ui.unclamped_slider.tooltip")] public SerializableNullable<float> logicalMaxValue { get; set; }
        float? IUnclampedSlider<float>.logicalMaxValue
        {
            get => logicalMaxValue;
            set => logicalMaxValue = value;
        }
        object? IUnclampedSlider.logicalMaxValue
        {
            get => logicalMaxValue;
            set => logicalMaxValue = value as float?;
        }

        public float GetClampedValue(float value)
        {
            if (logicalMinValue != null && value < logicalMinValue.Value)
                return logicalMinValue.Value;
            else if (logicalMaxValue != null && value > logicalMaxValue.Value)
                return logicalMaxValue.Value;

            return value;
        }

        object IUnclampedSlider.GetClampedValue(object value) => GetClampedValue(value as float? ?? 0);
    }
}
