#nullable enable
using System;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor
{
    public class EnumDropdownItem : AdvancedDropdownItem
    {
        public Enum enumValue { get; }

        public EnumDropdownItem(Enum enumValue, string name) : base(name) => this.enumValue = enumValue;
    }
}
