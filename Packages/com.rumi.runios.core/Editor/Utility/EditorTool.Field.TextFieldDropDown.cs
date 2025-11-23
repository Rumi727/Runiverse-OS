#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static string TextFieldDropDownLayout(string value, out bool isPressed) => TextFieldDropDownLayout(GUIContent.none, value, out isPressed);
        public static string TextFieldDropDownLayout(string label, string value, out bool isPressed) => TextFieldDropDownLayout(new GUIContent(label), value, out isPressed);
        public static string TextFieldDropDownLayout(GUIContent label, string value, out bool isPressed) => TextFieldDropDown(EditorGUILayout.GetControlRect(), label, value, out isPressed);

        public static string TextFieldDropDown(Rect position, string value, out bool isPressed) => TextFieldDropDown(position, GUIContent.none, value, out isPressed);
        public static string TextFieldDropDown(Rect position, string label, string value, out bool isPressed) => TextFieldDropDown(position, new GUIContent(label), value, out isPressed);
        public static string TextFieldDropDown(Rect position, GUIContent label, string value, out bool isPressed)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            isPressed = false;

            Rect fieldRect = position;
            fieldRect.width -= EditorStylesBridge.textFieldDropDown.fixedWidth;

            value = EditorGUI.TextField(fieldRect, label, value, EditorStylesBridge.textFieldDropDownText);

            Rect dropdownRect = position;
            dropdownRect.x += fieldRect.width;
            dropdownRect.width = EditorStylesBridge.textFieldDropDown.fixedWidth;

            if (GUI.Button(dropdownRect, GUIContent.none, EditorStylesBridge.textFieldDropDown))
                isPressed = true;

            return value;
        }
    }
}