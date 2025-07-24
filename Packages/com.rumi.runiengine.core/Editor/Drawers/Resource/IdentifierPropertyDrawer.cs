#nullable enable
using RuniEngine.Resource;
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(Identifier))]
    public class IdentifierPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());

            /*property.Next(true);
            SerializedProperty nameSpace = property.Copy();
            
            property.Next(false);
            SerializedProperty path = property.Copy();*/

            property.boxedValue = IdentifierField(position, label, (Identifier)property.boxedValue);

            EditorGUI.EndProperty();

            /*BeginIndentLevel(0);
            float fieldWidth = (position.width - (2 * 2) - 4) / 2f;
            for (int i = 0; i < 3; i++)
            {
                if (i % 2 == 0)
                {
                    position.width = fieldWidth;
                    EditorGUI.PropertyField(position, property, new GUIContent(), false);
                    position.x += position.width;

                    property.Next(false);
                    position.x += 4;
                }
                else
                {
                    position.width = 8;
                    position.x -= 4;

                    GUI.Label(position, Identifier.separator.ToString());

                    position.x += position.width;
                    position.width += 4;
                }
            }
            EndIndentLevel();*/
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode)
                return EditorGUIUtility.singleLineHeight;
            else
                return EditorGUIUtility.singleLineHeight * 2;
        }
    }
}
