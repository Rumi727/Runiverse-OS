#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(TooltipAttribute))]
    public sealed class TooltipAttributeAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TooltipAttribute attribute = (TooltipAttribute)this.attribute;
            label.tooltip = EditorTool.TryGetText(attribute.text);

            EditorGUI.PropertyField(position, property, label, property.IsChildrenIncluded());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label);
    }
}
