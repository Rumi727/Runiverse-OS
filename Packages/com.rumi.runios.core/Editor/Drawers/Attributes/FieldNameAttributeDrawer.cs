#nullable enable
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(FieldNameAttribute))]
    public class FieldNameAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            FieldNameAttribute attribute = (FieldNameAttribute)this.attribute;
            return new PropertyField(property, GetTextOrKey(attribute.name));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FieldNameAttribute attribute = (FieldNameAttribute)this.attribute;
            if (attribute.force || L10n.Tr(property.displayName) == label.text)
            {
                label.text = GetTextOrKey(attribute.name);
                EditorGUI.PropertyField(position, property, label, property.IsGeneric());
            }
            else
                EditorGUI.PropertyField(position, property, label, property.IsGeneric());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label);
    }
}
