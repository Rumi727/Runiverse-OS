#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static T? NullableField<T>(T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => RuniFields.NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, drawAction, nullText);
        public static T? NullableField<T>(string label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => RuniFields.NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);
        public static T? NullableField<T>(GUIContent label, T? value, Func<Rect, T, T?> drawAction, string? nullText = null) where T : struct => RuniFields.NullableField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, drawAction, nullText);

        public static T? NullablePrimitiveField<T>(T? value, string? nullText = null) where T : struct => RuniFields.NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), value, nullText);
        public static T? NullablePrimitiveField<T>(string label, T? value, string? nullText = null) where T : struct => RuniFields.NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, nullText);
        public static T? NullablePrimitiveField<T>(GUIContent label, T? value, string? nullText = null) where T : struct => RuniFields.NullablePrimitiveField(EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight), label, value, nullText);
    }
}
