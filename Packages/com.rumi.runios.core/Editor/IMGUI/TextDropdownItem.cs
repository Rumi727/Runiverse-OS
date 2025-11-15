#nullable enable
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI;

public class TextDropdownItem : AdvancedDropdownItem
{
    public string value { get; }

    public TextDropdownItem(string value, string name) : base(name) => this.value = value;
}