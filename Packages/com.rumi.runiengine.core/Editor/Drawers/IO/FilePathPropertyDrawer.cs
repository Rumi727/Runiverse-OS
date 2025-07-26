#nullable enable
using RuniEngine.IO;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers.IO
{
    [CustomPropertyDrawer(typeof(FilePath))]
    public class FilePathPropertyDrawer : PropertyDrawer
    {
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
            string value = EditorGUI.TextField(position, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;
        }

        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            return property;
        }
    }
}
