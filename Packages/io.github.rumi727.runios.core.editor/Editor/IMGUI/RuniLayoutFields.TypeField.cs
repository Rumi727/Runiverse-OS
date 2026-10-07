#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Type? TypeField(Type? value, Type? baseType = null) => RuniFields.TypeField(EditorGUILayout.GetControlRect(), value, baseType);
        public static Type? TypeField(string label, Type? value, Type? baseType = null) => RuniFields.TypeField(EditorGUILayout.GetControlRect(), label, value, baseType);
        public static Type? TypeField(GUIContent label, Type? value, Type? baseType = null) => RuniFields.TypeField(EditorGUILayout.GetControlRect(), label, value, baseType);
    }
}
