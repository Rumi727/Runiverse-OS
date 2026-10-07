#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static FileExtension FileExtensionField(Rect position, FileExtension value) => DoFileExtensionField(position, value);
        public static FileExtension FileExtensionField(Rect position, string label, FileExtension value) => FileExtensionField(position, new GUIContent(label), value);
        public static FileExtension FileExtensionField(Rect position, GUIContent label, FileExtension value) => DoFileExtensionField(EditorGUI.PrefixLabel(position, label), value);

        static FileExtension DoFileExtensionField(Rect position, FileExtension value)
        {
            BeginIndentLevel(0);

            {
                int leftPadding = EditorStyles.textField.padding.left;
                EditorStyles.textField.padding.left = 6;

                string textValue = EditorGUI.TextField(position, value.value.Length > 0 ? value.value.Substring(1) : string.Empty);
                if (textValue.Length > 0)
                    textValue = FileExtension.extensionSeparatorChar + textValue;

                value = (FileExtension)textValue;

                EditorStyles.textField.padding.left = leftPadding;
            }

            EndIndentLevel();

            position.x += 2;
            position.y += 1;

            if (value.value.Length > 0 || EditorGUIBridge.HasKeyboardFocus(EditorGUIUtilityBridge.s_LastControlID))
                GUI.Label(position, FileExtension.extensionSeparatorChar.ToString(), EditorStyles.label);

            return value;
        }
    }
}
