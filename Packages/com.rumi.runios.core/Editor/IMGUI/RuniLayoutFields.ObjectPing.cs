#nullable enable

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static void ObjectPingField(Object? obj) => RuniFields.ObjectPingField(EditorGUILayout.GetControlRect(), GUIContent.none, obj);
        public static void ObjectPingField(string label, Object? obj) => RuniFields.ObjectPingField(EditorGUILayout.GetControlRect(), label, obj);
        public static void ObjectPingField(GUIContent label, Object? obj) => RuniFields.ObjectPingField(EditorGUILayout.GetControlRect(), label, obj);
    }
}
