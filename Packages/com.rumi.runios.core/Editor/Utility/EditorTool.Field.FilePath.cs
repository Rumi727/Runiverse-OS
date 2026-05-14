#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static RuniPath RuniPathFieldLayout(RuniPath value, bool isFolder = false) => RuniPathField(EditorGUILayout.GetControlRect(), value, isFolder);
        public static RuniPath RuniPathFieldLayout(string label, RuniPath value, bool isFolder = false) => RuniPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);
        public static RuniPath RuniPathFieldLayout(GUIContent label, RuniPath value, bool isFolder = false) => RuniPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);

        public static RuniPath RuniPathField(Rect position, RuniPath value, bool isFolder = false) => DoRuniPathField(position, value, isFolder);
        public static RuniPath RuniPathField(Rect position, string label, RuniPath value, bool isFolder = false) => RuniPathField(position, new GUIContent(label), value, isFolder);
        public static RuniPath RuniPathField(Rect position, GUIContent label, RuniPath value, bool isFolder = false)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoRuniPathField(position, value, isFolder);
            EndIndentLevel();
            return value;
        }

        static RuniPath DoRuniPathField(Rect position, RuniPath value, bool isFolder)
        {
            RuniPath path = TextFieldDropDown(position, value.value, out bool isPressed);
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