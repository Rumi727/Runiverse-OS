#nullable enable
using UnityEditor;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(Version))]
    public class VersionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());
            
            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);
            position.height = 18f;
            
            property.Next(true);

            BeginIndentLevel(0);
            float fieldWidth = (position.width - (2 * 4) - (4 * 2)) / 3f;

            for (int i = 0; i < 5; i++)
            {
                if (i % 2 == 0)
                {
                    if (i == 2)
                        position.width = fieldWidth.Floor();
                    else if (i == 4)
                        position.width = fieldWidth.Ceil();
                    else
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
                    GUI.Label(position, ".");

                    position.x += position.width;
                    position.width += 4;
                }
            }
            EndIndentLevel();

            EditorGUI.EndProperty();
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
