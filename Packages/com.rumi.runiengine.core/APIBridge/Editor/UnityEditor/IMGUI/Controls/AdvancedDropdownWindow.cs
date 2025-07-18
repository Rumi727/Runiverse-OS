#nullable enable
using System;

namespace RuniEngine.Editor.APIBridge.UnityEditor.IMGUI.Controls
{
    public class AdvancedDropdownWindow : EditorWindow
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_CoreModule.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdownWindow");

        public static AdvancedDropdownWindow CreateInstance() => new AdvancedDropdownWindow((EditorWindow?)Activator.CreateInstance(type));
        public static AdvancedDropdownWindow GetInstance(EditorWindow? instance) => new AdvancedDropdownWindow(instance);

        AdvancedDropdownWindow(EditorWindow? instance) => this.instance = instance;

        public EditorWindow? instance { get; }

        public override string ToString() => instance?.ToString() ?? "Null";
    }
}
