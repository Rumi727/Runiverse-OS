#nullable enable
using RuniOS.IO;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public class RuniPathDropdownItem(RuniPath path, string name) : AdvancedDropdownItem(name)
    {
        public RuniPath path { get; } = path;
    }
}