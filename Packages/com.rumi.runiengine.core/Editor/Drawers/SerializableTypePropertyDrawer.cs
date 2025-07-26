#nullable enable
using System;
using UnityEngine;
using UnityEditor;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SerializableType))]
    public class SerializableTypePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label, Type? baseType = null)
        {
            property = GetChildProperty(property);
            TypeField(position, label, TypeUtility.DeserializeFromString(property.stringValue), x => property.stringValue = x?.SerializeToString() ?? string.Empty, baseType);
        }
        
        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            return property;
        }
    }
}
