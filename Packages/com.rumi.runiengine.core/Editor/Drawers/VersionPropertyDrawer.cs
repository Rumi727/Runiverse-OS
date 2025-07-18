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
            property.Next(true);

            int controlID = GUIUtility.GetControlID(APIBridge.UnityEditor.EditorGUI.s_FoldoutHash, FocusType.Keyboard, position);
            position = APIBridge.UnityEditor.EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);
            position.height = 18f;

            float fieldWidth = position.width / 3f;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            
            for (int i = 0; i < 3; i++)
            {
                {
                    position.width = fieldWidth;
                    EditorGUI.PropertyField(position, property, new GUIContent(), false);
                    position.x += position.width;
                }

                property.Next(false);
                position.x += 2;
            }
        }
    }
}
