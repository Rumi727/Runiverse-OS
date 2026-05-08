#nullable enable
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public class EnumDropdownItem(Enum enumValue, string name) : AdvancedDropdownItem(name)
    {
        public Enum enumValue { get; } = enumValue;
    }
}