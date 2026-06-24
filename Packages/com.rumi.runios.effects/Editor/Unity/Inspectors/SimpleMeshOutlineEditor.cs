#nullable enable
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.Effects;
using RuniOS.UI.Effects;

namespace RuniOS.Editor.Effects.Unity.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleMeshOutline))]
    public class SimpleMeshOutlineEditor : CustomInspectorBase<RoundedCorners>
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(TrTempContent("inspector.simple_mesh_outline.appearance.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_color", TrTempContent("inspector.simple_mesh_outline.appearance.color"));
            
            Space();
            
            DrawPropertyLayout("_width", TrTempContent("inspector.simple_mesh_outline.appearance.width"));
            DrawPropertyLayout("_useFixedWidth", TrTempContent("inspector.simple_mesh_outline.appearance.useFixedWidth", "inspector.simple_mesh_outline.appearance.useFixedWidth.tooltip"));
            
            Space();
            
            DrawPropertyLayout("_gap", TrTempContent("inspector.simple_mesh_outline.appearance.gap"));
            DrawPropertyLayout("_useFixedGap", TrTempContent("inspector.simple_mesh_outline.appearance.useFixedGap", "inspector.simple_mesh_outline.appearance.useFixedGap.tooltip"));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.simple_mesh_outline.settings.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_outlineVisibility", TrTempContent("inspector.simple_mesh_outline.settings.outlineVisibility", "inspector.simple_mesh_outline.settings.outlineVisibility.tooltip"));
        }
    }
}