#nullable enable

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static T PrimitiveField<T>(T value) where T : struct => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), value);
        public static T PrimitiveField<T>(string label, T value) where T : struct => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), label, value);
        public static T PrimitiveField<T>(GUIContent label, T value) where T : struct => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), label, value);

        public static object PrimitiveField(object value) => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), value);
        public static object PrimitiveField(string label, object value) => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), label, value);
        public static object PrimitiveField(GUIContent label, object value) => RuniFields.PrimitiveField(EditorGUILayout.GetControlRect(), label, value);
    }
}
