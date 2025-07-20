#nullable enable
using UnityEditor;

namespace RuniEngine.Editor
{
    [InitializeOnLoad]
    public static class InspectorSettingSetter
    {
        static InspectorSettingSetter()
        {
            if (!APIBridge.UnityEditor.EditorSettings.inspectorUseIMGUIDefaultInspector)
            {
                Debug.Log(EditorTool.TryGetText("internal.auto_setter.property.value_info").Replace("{name}", $"{nameof(APIBridge.UnityEditor.EditorSettings)}.{nameof(APIBridge.UnityEditor.EditorSettings.inspectorUseIMGUIDefaultInspector)}").Replace("{value}", "true"));
                APIBridge.UnityEditor.EditorSettings.inspectorUseIMGUIDefaultInspector = true;
            }
        }
    }
}
