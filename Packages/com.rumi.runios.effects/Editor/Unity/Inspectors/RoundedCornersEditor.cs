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
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.corner.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_radius", TrTempContent("inspector.rounded_corners.corner.radius"));
            DrawPropertyLayout("_softness", TrTempContent("inspector.rounded_corners.corner.softness", "inspector.rounded_corners.corner.softness.tooltip"));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.outline.header"), EditorStyles.boldLabel);
            DrawPropertyLayout("_outlineColor", TrTempContent("inspector.rounded_corners.outline.outlineColor"));
            DrawPropertyLayout("_outlineWidth", TrTempContent("inspector.rounded_corners.outline.outlineWidth"));
            DrawPropertyLayout("_outlineSoftness", TrTempContent("inspector.rounded_corners.outline.outlineSoftness", "inspector.rounded_corners.outline.outlineSoftness.tooltip"));
            DrawPropertyLayout("_insideOutline", TrTempContent("inspector.rounded_corners.outline.insideOutline"));
        }
    }
}