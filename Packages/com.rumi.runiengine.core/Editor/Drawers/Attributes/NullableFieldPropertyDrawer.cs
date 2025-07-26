#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(NullableFieldAttribute))]
    public class NullableFieldPropertyDrawer : PropertyDrawer
    {
        SerializedProperty? field;
        SerializedProperty? toggle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.boxedValue != null && property.boxedValue.GetType().IsAssignableToGenericDefinition(typeof(SerializableNullable<>)))
            {
                field ??= property.FindPropertyRelative("value");
                toggle ??= property.FindPropertyRelative("hasValue");

                string? nullText = ((NullableFieldAttribute)attribute).customNullText;
                SerializableNullablePropertyDrawer.Draw(position, field, toggle, label, nullText);
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                field ??= property.FindPropertyRelative("value");
                toggle ??= property.FindPropertyRelative("hasValue");

                if (field != null && toggle != null && toggle.boolValue)
                    return EditorGUI.GetPropertyHeight(field, label);
                else
                    return EditorGUIUtility.singleLineHeight;
            }
            else
                return EditorGUI.GetPropertyHeight(field, label);
        }
    }
}
