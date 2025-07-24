#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(HexColor))]
    public class HexColorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            HexColor.TryParse(property.stringValue, out Color color);
            color = EditorGUI.ColorField(position, label, color);

            if (EditorGUI.EndChangeCheck())
                property.stringValue = HexColor.ToHex(color);

            EditorGUI.EndProperty();
        }
    }
}
