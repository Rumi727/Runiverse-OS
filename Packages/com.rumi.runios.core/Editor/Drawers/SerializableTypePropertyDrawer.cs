#nullable enable
using System;
using UnityEngine;
using UnityEditor;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Drawers
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
            
            EditorGUI.BeginChangeCheck();
            Type? type = TypeField(position, label, TypeUtility.DeserializeFromString(property.stringValue), baseType);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = type?.SerializeToString() ?? string.Empty;
        }
        
        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            return property;
        }
    }
}
