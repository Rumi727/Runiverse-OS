#nullable enable
using GUIView = RuniEngine.Editor.APIBridge.UnityEditor.GUIView;

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
