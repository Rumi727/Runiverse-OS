#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(NotNullFieldAttribute))]
    public class NotNullFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
            {
                position.width *= 0.6666666666666666666666666666666666666f;
                position.width -= 4;

                EditorGUI.PropertyField(position, property, label);

                position.x += position.width + 4;
                position.width *= 0.5f;
                position.width += 2;

                EditorGUI.HelpBox(position, GetTextOrKey("gui.field_is_null"), MessageType.Error);
            }
            else
                EditorGUI.PropertyField(position, property, label, property.IsChildrenIncluded());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label);
    }
}
