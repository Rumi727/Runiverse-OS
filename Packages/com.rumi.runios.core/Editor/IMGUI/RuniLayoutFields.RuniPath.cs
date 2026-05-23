#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static RuniPath RuniPathField(RuniPath value) => RuniFields.RuniPathField(EditorGUILayout.GetControlRect(), value);
        public static RuniPath RuniPathField(string label, RuniPath value) => RuniFields.RuniPathField(EditorGUILayout.GetControlRect(), label, value);
        public static RuniPath RuniPathField(GUIContent label, RuniPath value) => RuniFields.RuniPathField(EditorGUILayout.GetControlRect(), label, value);
    }
}
