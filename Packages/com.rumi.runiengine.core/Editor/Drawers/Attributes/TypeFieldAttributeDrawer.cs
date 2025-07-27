#nullable enable
using RuniEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(TypeFieldAttribute))]
    public class TypeFieldAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            TypeFieldAttribute attribute = (TypeFieldAttribute)this.attribute;
            TypeField typeField = new TypeField(attribute.baseType)
            {
                label = property.displayName, 
                bindingPath = SerializableTypePropertyDrawer.GetChildProperty(property).propertyPath
            };
            
            typeField.AddToClassList(TypeField.alignedFieldUssClassName);
            typeField.labelElement.AddToClassList(PropertyField.labelUssClassName);
            typeField.visualInput.AddToClassList(PropertyField.inputUssClassName);
            
            return typeField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TypeFieldAttribute attribute = (TypeFieldAttribute)this.attribute;
            
            EditorGUI.BeginProperty(position, label, property);
            SerializableTypePropertyDrawer.Draw(position, property, label, attribute.baseType);
            EditorGUI.EndProperty();
        }
    }
}
