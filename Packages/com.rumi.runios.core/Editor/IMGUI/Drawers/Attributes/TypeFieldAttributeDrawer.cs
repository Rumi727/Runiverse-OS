#nullable enable
using RuniOS.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.IMGUI.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(TypeFieldAttribute))]
    public class TypeFieldAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            TypeFieldAttribute attribute = (TypeFieldAttribute)this.attribute;
            TypeField typeField = (TypeField)new TypeField(attribute.baseType).SetProperty(property);
            
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
