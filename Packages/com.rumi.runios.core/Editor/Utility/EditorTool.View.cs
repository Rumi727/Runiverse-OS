#nullable enable
using GUIView = RuniOS.Editor.APIBridge.UnityEditor.GUIView;

namespace RuniOS.Editor
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
