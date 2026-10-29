#nullable enable
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.UI.Effects;

namespace RuniOS.Editor.Effects.Unity.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RoundedCorners))]
    public class RoundedCornersEditor : CustomInspectorBase<RoundedCorners>
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.rounded_corners.corner.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_radius", TrTempContent("runios-editor:inspector.rounded_corners.corner.radius"));
            DrawPropertyLayout("_softness", TrTempContent("runios-editor:inspector.rounded_corners.corner.softness", "runios-editor:inspector.rounded_corners.corner.softness.tooltip"));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.rounded_corners.outline.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_outlineColor", TrTempContent("runios-editor:inspector.rounded_corners.outline.outline_color"));
            DrawPropertyLayout("_outlineWidth", TrTempContent("runios-editor:inspector.rounded_corners.outline.outline_width"));
            DrawPropertyLayout("_outlineSoftness", TrTempContent("runios-editor:inspector.rounded_corners.outline.outline_softness", "runios-editor:inspector.rounded_corners.outline.outline_softness.tooltip"));
            DrawPropertyLayout("_insideOutline", TrTempContent("runios-editor:inspector.rounded_corners.outline.inside_outline"));
        }
    }
}