#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static FilePath FilePathFieldLayout(FilePath value) => FilePathField(EditorGUILayout.GetControlRect(), value);
        public static FilePath FilePathFieldLayout(string label, FilePath value) => FilePathField(EditorGUILayout.GetControlRect(), label, value);
        public static FilePath FilePathFieldLayout(GUIContent label, FilePath value) => FilePathField(EditorGUILayout.GetControlRect(), label, value);

        public static FilePath FilePathField(Rect position, FilePath value) => DoFilePathField(position, value);
        public static FilePath FilePathField(Rect position, string label, FilePath value) => FilePathField(position, new GUIContent(label), value);
        public static FilePath FilePathField(Rect position, GUIContent label, FilePath value)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoFilePathField(position, value);
            EndIndentLevel();
            return value;
        }

        static FilePath DoFilePathField(Rect position, FilePath value)
        {
            FilePath path = TextFieldDropDown(position, value.value, out bool isPressed);
            if (isPressed)
            {
                string panelValue = EditorUtility.OpenFolderPanel(GetTextOrKey("pack_identifier.open_folder.title"), string.Empty, string.Empty);
                if (!string.IsNullOrEmpty(panelValue))
                    path = panelValue;
            }

            return path;
        }
    }
}