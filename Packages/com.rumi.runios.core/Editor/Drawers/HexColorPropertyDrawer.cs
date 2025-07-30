#nullable enable
using RuniOS.Editor.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(HexColor))]
    public class HexColorPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            HexColorField colorField = new HexColorField
            {
                label = property.displayName,
                bindingPath = property.propertyPath
            };
            
            colorField.AddToClassList(ColorField.alignedFieldUssClassName);
            colorField.labelElement.AddToClassList(PropertyField.labelUssClassName);
            colorField.visualInput.AddToClassList(PropertyField.inputUssClassName);
            
            return colorField;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            property = GetChildProperty(property);
            EditorGUI.BeginChangeCheck();

            HexColor.TryParse(property.stringValue, out Color color);
            color = EditorGUI.ColorField(position, label, color);

            if (EditorGUI.EndChangeCheck())
                property.stringValue = HexColor.ToHex(color);
        }
        
        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            return property;
        }
    }
}
