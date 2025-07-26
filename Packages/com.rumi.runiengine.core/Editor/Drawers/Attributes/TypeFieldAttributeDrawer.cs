#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(TypeFieldAttribute))]
    public class TypeFieldAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TypeFieldAttribute attribute = (TypeFieldAttribute)this.attribute;
            
            EditorGUI.BeginProperty(position, label, property);
            SerializableTypePropertyDrawer.Draw(position, property, label, attribute.baseType);
            EditorGUI.EndProperty();
        }
    }
}
