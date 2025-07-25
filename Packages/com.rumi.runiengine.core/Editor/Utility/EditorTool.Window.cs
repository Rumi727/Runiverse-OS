#nullable enable

using UnityEditor;
using UnityEngine.UIElements;
using GUIView = RuniEngine.Editor.APIBridge.UnityEditor.GUIView;
using HostView = RuniEngine.Editor.APIBridge.UnityEditor.HostView;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static void RepaintCurrentWindow()
        {
            if (GUIView.current?.instance != null)
            {
                if (HostView.type.IsAssignableFrom(GUIView.current.instance.GetType()))
                {
                    HostView hostView = HostView.GetInstance(GUIView.current.instance);
                    EditorWindow? actualView = hostView.actualView;
                    if (actualView != null)
                    {
                        actualView.Repaint();
                        RuniEngine.APIBridge.UnityEngine.UIElements.VisualElement.GetInstance(actualView.rootVisualElement).IncrementVersion(VersionChangeType.Size);
                    }
                }
                else
                    GUIView.current.Repaint();
            }
        }
    }
}
