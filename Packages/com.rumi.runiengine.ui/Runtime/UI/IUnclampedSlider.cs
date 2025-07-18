#nullable enable
namespace RuniEngine.UI
{
    public interface IUnclampedSlider
    {
        object value { get; set; }

        bool isOutOfRange { get; }

        bool isOutOfLow { get; }
        bool isOutOfHigh { get; }

        object? logicalMinValue { get; set; }
        object? logicalMaxValue { get; set; }

        object GetClampedValue(object value);
    }
}
