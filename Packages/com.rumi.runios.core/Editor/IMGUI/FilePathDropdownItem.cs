#nullable enable
using RuniOS.IO;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public class FilePathDropdownItem(FilePath path, string name) : AdvancedDropdownItem(name)
    {
        public FilePath path { get; } = path;
    }
}