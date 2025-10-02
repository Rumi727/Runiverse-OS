#nullable enable
using UnityEditor;
using UnityEngine;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(char))]
    public class CharPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label) => property.uintValue = CharField(position, label, (char)property.uintValue);
    }
}
