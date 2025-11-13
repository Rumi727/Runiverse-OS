#nullable enable
using System;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor
{
    public interface ISelectableDropdown<out T> : IShowableDropdown where T : AdvancedDropdownItem
    {
        public T? selectedItem { get; }
        public event Action<T>? onSelectedItem;
    }
}
