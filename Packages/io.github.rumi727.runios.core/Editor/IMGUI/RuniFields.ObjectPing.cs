#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static void ObjectPingField(Rect position, Object? obj) => ObjectPingField(position, GUIContent.none, obj);
        public static void ObjectPingField(Rect position, string label, Object? obj) => ObjectPingField(position, new GUIContent(label), obj);
        public static void ObjectPingField(Rect position, GUIContent label, Object? obj)
        {
            GUIContent content = EditorGUIUtility.ObjectContent(obj, typeof(Object));

            position = EditorGUI.PrefixLabel(position, label);

            BeginIndentLevel(0);
            EditorGUI.LabelField(position, content, EditorStyles.objectField);
            EndIndentLevel();

            if (position.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (obj != null)
                    EditorGUIUtility.PingObject(obj);

                Event.current.Use();
            }
        }
    }
}
