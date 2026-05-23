#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static char CharField(char value) => RuniFields.CharField(EditorGUILayout.GetControlRect(), value);
        public static char CharField(string label, char value) => RuniFields.CharField(EditorGUILayout.GetControlRect(), label, value);
        public static char CharField(GUIContent label, char value) => RuniFields.CharField(EditorGUILayout.GetControlRect(), label, value);
    }
}
