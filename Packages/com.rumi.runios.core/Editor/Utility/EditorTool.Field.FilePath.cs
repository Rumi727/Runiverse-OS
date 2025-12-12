#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static FilePath FilePathFieldLayout(FilePath value, bool isFolder = false) => FilePathField(EditorGUILayout.GetControlRect(), value, isFolder);
        public static FilePath FilePathFieldLayout(string label, FilePath value, bool isFolder = false) => FilePathField(EditorGUILayout.GetControlRect(), label, value, isFolder);
        public static FilePath FilePathFieldLayout(GUIContent label, FilePath value, bool isFolder = false) => FilePathField(EditorGUILayout.GetControlRect(), label, value, isFolder);

        public static FilePath FilePathField(Rect position, FilePath value, bool isFolder = false) => DoFilePathField(position, value, isFolder);
        public static FilePath FilePathField(Rect position, string label, FilePath value, bool isFolder = false) => FilePathField(position, new GUIContent(label), value, isFolder);
        public static FilePath FilePathField(Rect position, GUIContent label, FilePath value, bool isFolder = false)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoFilePathField(position, value, isFolder);
            EndIndentLevel();
            return value;
        }

        static FilePath DoFilePathField(Rect position, FilePath value, bool isFolder)
        {
            FilePath path = TextFieldDropDown(position, value.value, out bool isPressed);
            if (isPressed)
            {
                string panelValue;
                if (isFolder)
                    panelValue = EditorUtility.OpenFolderPanel(GetTextOrKey("file_path.open_folder.title"), string.Empty, string.Empty);
                else
                    panelValue = EditorUtility.OpenFilePanel(GetTextOrKey("file_path.open_file.title"), string.Empty, string.Empty);
                
                if (!string.IsNullOrEmpty(panelValue))
                    path = panelValue;
            }

            return path;
        }
    }
}