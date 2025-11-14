#nullable enable

using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(Version))]
    public class VersionPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new VersionField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => Draw(position, property, label);
        
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());
            
            int controlID = GUIUtility.GetControlID(EditorGUIBridge.s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, controlID, label, 4);
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
            if (EditorGUIUtility.wideMode || !LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
        
        public static (SerializedProperty major, SerializedProperty minor, SerializedProperty patch) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            
            property.Next(true);
            SerializedProperty major = property.Copy();
            
            property.Next(false);
            SerializedProperty minor = property.Copy();
            
            property.Next(false);
            SerializedProperty patch = property.Copy();

            return (major, minor, patch);
        }
    }
}
