#nullable enable
using RuniEngine.IO;
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers.IO
{
    [CustomPropertyDrawer(typeof(FileExtension))]
    public class FileExtensionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty valueProperty = property.Copy();
            valueProperty.Next(true);

            EditorGUI.BeginChangeCheck();
            FileExtension value = FileExtensionField(position, label, valueProperty.stringValue); //boxedValue 쓰면 크래시남..
            if (EditorGUI.EndChangeCheck())
                valueProperty.stringValue = value;

            EditorGUI.EndProperty();
        }
    }
}
