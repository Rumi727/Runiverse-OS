#nullable enable
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI;

public interface ISelectableDropdown<out T> : IShowableDropdown where T : AdvancedDropdownItem
{
    public T? selectedItem { get; }
    public event Action<T>? onSelectedItem;
}