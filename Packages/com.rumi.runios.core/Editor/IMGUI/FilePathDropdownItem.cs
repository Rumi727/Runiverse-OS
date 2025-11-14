#nullable enable
using RuniOS.IO;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public class FilePathDropdownItem : AdvancedDropdownItem
    {
        public FilePath path { get; }

        public FilePathDropdownItem(FilePath path, string name) : base(name) => this.path = path;
    }
}
