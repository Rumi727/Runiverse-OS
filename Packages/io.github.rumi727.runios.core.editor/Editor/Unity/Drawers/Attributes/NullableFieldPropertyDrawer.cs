#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.Unity.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(NullableFieldAttribute))]
    public class NullableFieldPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => SerializableNullablePropertyDrawer.CreatePropertyGUI(property.GetPropertyTypeWithoutList(), property, ((NullableFieldAttribute)attribute).customNullText);
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.GetPropertyTypeWithoutList().IsAssignableToGenericDefinition(typeof(ISerializableNullable<>)))
            {
                EditorGUI.BeginProperty(position, label, property);
                
                string? nullText = ((NullableFieldAttribute)attribute).customNullText;
                SerializableNullablePropertyDrawer.Draw(position, property, label, nullText);
                
                EditorGUI.EndProperty();
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Generic && property.GetPropertyTypeWithoutList().IsAssignableToGenericDefinition(typeof(ISerializableNullable<>)))
            {
                (SerializedProperty? field, SerializedProperty? toggle) = SerializableNullablePropertyDrawer.GetChildProperty(property);

                if (field != null && toggle != null && toggle.boolValue)
                    return EditorGUI.GetPropertyHeight(field, label);
                else
                    return EditorGUIUtility.singleLineHeight;
            }
            else
                return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}