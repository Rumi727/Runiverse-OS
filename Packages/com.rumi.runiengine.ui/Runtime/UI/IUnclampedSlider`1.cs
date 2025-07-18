#nullable enable
using System;

namespace RuniEngine.UI
{
    public interface IUnclampedSlider<T> : IUnclampedSlider where T : struct, IComparable<T>, IComparable
    {
        new T value { get; set; }

        new T? logicalMinValue { get; set; }
        new T? logicalMaxValue { get; set; }

        T GetClampedValue(T value);
    }
}
