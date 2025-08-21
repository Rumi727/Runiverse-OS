#nullable enable
using RuniOS.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static void RepaintCurrentWindow()
        {
            if (GUIViewBridge.current?.__instance != null)
                GUIViewBridge.current.Repaint();
        }
    }
}
