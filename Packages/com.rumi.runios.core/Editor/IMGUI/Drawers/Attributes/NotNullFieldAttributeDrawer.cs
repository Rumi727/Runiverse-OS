#nullable enable

using UnityEditor;
using UnityEngine;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.IMGUI.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(NotNullFieldAttribute))]
    public class NotNullFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                float fieldWidth = position.width - EditorGUIUtility.labelWidth;
                
                position.width = labelWidth + (fieldWidth * 0.6f);
                EditorGUI.PropertyField(position, property, label);

                position.x += position.width + 4;
                position.width = fieldWidth * 0.4f;
                position.width -= 4;

                EditorGUI.HelpBox(position, GetTextOrKey("gui.field_is_null"), MessageType.Error);
            }
            else
                EditorGUI.PropertyField(position, property, label, property.IsGeneric());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label);
    }
}
