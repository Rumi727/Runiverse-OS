#nullable enable
using RuniOS.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(Vector4))]
    public class Vector4PropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new Vector4Field().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => Draw(position, property, label);
        
        static readonly GUIContent[] labels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());

            property.Next(true);
            EditorGUI.MultiPropertyField(position, labels, property, label);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode || !EditorTool.LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
    }
}
