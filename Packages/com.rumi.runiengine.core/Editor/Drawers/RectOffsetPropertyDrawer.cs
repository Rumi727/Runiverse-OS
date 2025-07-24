#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(RectOffset))]
    public class RectOffsetPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent[] labels = new GUIContent[] { new GUIContent("L"), new GUIContent("R"), new GUIContent("T"), new GUIContent("B") };
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.MultiPropertyField(position, labels, property, label);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode)
                return EditorGUIUtility.singleLineHeight;
            else
                return EditorGUIUtility.singleLineHeight * 2;
        }
    }
}
