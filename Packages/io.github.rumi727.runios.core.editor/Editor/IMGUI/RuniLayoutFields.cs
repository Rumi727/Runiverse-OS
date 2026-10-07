#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Rect GetMultiColumnsControlRect(GUIContent label, int rows = 1) => EditorGUILayout.GetControlRect(LabelHasContent(label), RuniFields.GetMultiColumnsFieldHeight(label, rows));
        public static Rect GetMultiRowsControlRect(GUIContent label, int rows) => EditorGUILayout.GetControlRect(LabelHasContent(label), RuniFields.GetMultiRowsFieldHeight(label, rows));
    }
}