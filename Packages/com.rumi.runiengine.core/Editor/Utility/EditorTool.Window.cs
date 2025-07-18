#nullable enable
using RuniEngine.Editor.APIBridge.UnityEditor;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static void RepaintCurrentWindow()
        {
            if (GUIView.current?.instance != null)
                GUIView.current.Repaint();
        }
    }
}
