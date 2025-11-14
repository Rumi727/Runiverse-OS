#nullable enable

using RuniOS.Editor.UIElements;
using RuniOS.Editor.UIElements.Primitives;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(char))]
    public class CharPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new CharField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label) => property.uintValue = CharField(position, label, (char)property.uintValue);
    }
}
