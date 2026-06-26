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
            DrawPropertyLayout("_useFixedWidth", TrTempContent("inspector.simple_mesh_outline.appearance.use_fixed_width", "inspector.simple_mesh_outline.appearance.use_fixed_width.tooltip"));
            
            Space();
            
            DrawPropertyLayout("_gap", TrTempContent("inspector.simple_mesh_outline.appearance.gap"));
            DrawPropertyLayout("_useFixedGap", TrTempContent("inspector.simple_mesh_outline.appearance.use_fixed_gap", "inspector.simple_mesh_outline.appearance.use_fixed_gap.tooltip"));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.simple_mesh_outline.settings.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_outlineVisibility", TrTempContent("inspector.simple_mesh_outline.settings.outline_visibility", "inspector.simple_mesh_outline.settings.outline_visibility.tooltip"));
        }
    }
}