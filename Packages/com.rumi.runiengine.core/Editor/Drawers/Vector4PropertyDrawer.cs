#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(Vector4))]
    public class Vector4PropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent[] labels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            EditorGUI.MultiPropertyField(position, labels, property, label);
        }
    }
}
