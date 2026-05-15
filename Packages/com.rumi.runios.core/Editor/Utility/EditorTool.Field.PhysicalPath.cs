#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static PhysicalPath PhysicalPathFieldLayout(PhysicalPath value, bool isFolder = false) => PhysicalPathField(EditorGUILayout.GetControlRect(), value, isFolder);
        public static PhysicalPath PhysicalPathFieldLayout(string label, PhysicalPath value, bool isFolder = false) => PhysicalPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);
        public static PhysicalPath PhysicalPathFieldLayout(GUIContent label, PhysicalPath value, bool isFolder = false) => PhysicalPathField(EditorGUILayout.GetControlRect(), label, value, isFolder);

        public static PhysicalPath PhysicalPathField(Rect position, PhysicalPath value, bool isFolder = false) => DoPhysicalPathField(position, value, isFolder);
        public static PhysicalPath PhysicalPathField(Rect position, string label, PhysicalPath value, bool isFolder = false) => PhysicalPathField(position, new GUIContent(label), value, isFolder);
        public static PhysicalPath PhysicalPathField(Rect position, GUIContent label, PhysicalPath value, bool isFolder = false)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoPhysicalPathField(position, value, isFolder);
            EndIndentLevel();
            return value;
        }

        static PhysicalPath DoPhysicalPathField(Rect position, PhysicalPath value, bool isFolder)
        {
            PhysicalPath path = (PhysicalPath)TextFieldDropDown(position, value.value, out bool isPressed);
            if (isPressed)
            {
                string panelValue;
                if (isFolder)
                    panelValue = EditorUtility.OpenFolderPanel(GetTextOrKey("file_path.open_folder.title"), string.Empty, string.Empty);
                else
                    panelValue = EditorUtility.OpenFilePanel(GetTextOrKey("file_path.open_file.title"), string.Empty, string.Empty);
                
                if (!string.IsNullOrEmpty(panelValue))
                    path = (PhysicalPath)panelValue;
            }

            return path;
        }
    }
}