#nullable enable
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public class TextDropdownItem(string value, string name) : AdvancedDropdownItem(name)
    {
        public string value { get; } = value;
    }
}