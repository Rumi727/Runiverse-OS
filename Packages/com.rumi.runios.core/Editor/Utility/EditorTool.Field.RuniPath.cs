#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static RuniPath RuniPathFieldLayout(RuniPath value) => RuniPathField(EditorGUILayout.GetControlRect(), value);
        public static RuniPath RuniPathFieldLayout(string label, RuniPath value) => RuniPathField(EditorGUILayout.GetControlRect(), label, value);
        public static RuniPath RuniPathFieldLayout(GUIContent label, RuniPath value) => RuniPathField(EditorGUILayout.GetControlRect(), label, value);

        public static RuniPath RuniPathField(Rect position, RuniPath value) => DoRuniPathField(position, value);
        public static RuniPath RuniPathField(Rect position, string label, RuniPath value) => RuniPathField(position, new GUIContent(label), value);
        public static RuniPath RuniPathField(Rect position, GUIContent label, RuniPath value)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoRuniPathField(position, value);
            EndIndentLevel();
            return value;
        }

        static RuniPath DoRuniPathField(Rect position, RuniPath value) => (RuniPath)EditorGUI.TextField(position, value.value);
    }
}