#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Rect GetPrefixLabelRect(Rect totalPosition, GUIContent? label, out Rect? labelPosition)
        {
            if (label != null && label.text == string.Empty && label.image == null)
            {
                labelPosition = null;
                return EditorGUI.IndentedRect(totalPosition);
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel * 15;

            labelPosition = new Rect(totalPosition.x + indentLevel, totalPosition.y, labelWidth - indentLevel, EditorGUIUtility.singleLineHeight);

            return new Rect(totalPosition.x + labelWidth + 2f, totalPosition.y, totalPosition.width - labelWidth - 2f, totalPosition.height);
        }

        public static Rect GetMultiControlRect(int yCount = 1) => EditorGUILayout.GetControlRect(true, EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight : (EditorGUIUtility.singleLineHeight * 2) + (yCount * 2));
    }
}
