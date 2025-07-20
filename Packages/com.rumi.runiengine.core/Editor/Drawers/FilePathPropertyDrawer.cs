#nullable enable
using RuniEngine.IO;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    [CustomPropertyDrawer(typeof(FilePath))]
    public class FilePathPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
