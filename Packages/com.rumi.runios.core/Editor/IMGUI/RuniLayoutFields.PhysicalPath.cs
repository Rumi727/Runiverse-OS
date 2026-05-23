#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static PhysicalPath PhysicalPathField(PhysicalPath value, bool isFolder = false) => RuniFields.PhysicalPathField(EditorGUILayout.GetControlRect(), value, isFolder);
        public static PhysicalPath PhysicalPathField(string label, PhysicalPath value, bool isFolder = false) => RuniFields.PhysicalPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);
        public static PhysicalPath PhysicalPathField(GUIContent label, PhysicalPath value, bool isFolder = false) => RuniFields.PhysicalPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);
    }
}
