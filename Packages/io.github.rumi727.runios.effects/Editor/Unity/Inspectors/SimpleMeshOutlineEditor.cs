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
            GUILayout.Label(TrTempContent("runios-editor:inspector.simple_mesh_outline.appearance.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_color", TrTempContent("runios-editor:inspector.simple_mesh_outline.appearance.color"));
            
            Space();
            
            DrawPropertyLayout("_width", TrTempContent("runios-editor:inspector.simple_mesh_outline.appearance.width"));
            DrawPropertyLayout("_usePixelWidth", TrTempContent("runios-editor:inspector.simple_mesh_outline.appearance.use_pixel_width", "runios-editor:inspector.simple_mesh_outline.appearance.use_pixel_width.tooltip"));
            
            Space();
            
            DrawPropertyLayout("_gap", TrTempContent("runios:inspector.simple_mesh_outline.appearance.gap"));
            DrawPropertyLayout("_usePixelGap", TrTempContent("runios:inspector.simple_mesh_outline.appearance.use_pixel_gap", "runios:inspector.simple_mesh_outline.appearance.use_pixel_gap.tooltip"));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("runios:inspector.simple_mesh_outline.settings.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_outlineVisibility", TrTempContent("runios:inspector.simple_mesh_outline.settings.outline_visibility", "runios:inspector.simple_mesh_outline.settings.outline_visibility.tooltip"));
        }
    }
}
