#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static FileExtension FileExtensionField(FileExtension value) => RuniFields.FileExtensionField(EditorGUILayout.GetControlRect(), value);
        public static FileExtension FileExtensionField(string label, FileExtension value) => RuniFields.FileExtensionField(EditorGUILayout.GetControlRect(), label, value);
        public static FileExtension FileExtensionField(GUIContent label, FileExtension value) => RuniFields.FileExtensionField(EditorGUILayout.GetControlRect(), label, value);
    }
}
