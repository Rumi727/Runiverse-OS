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
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.corner.header"), boldLabelStyle);
            DrawPropertyLayout("_radius", new GUIContent(GetTextOrKey("inspector.rounded_corners.corner.radius")));
            DrawPropertyLayout("_softness", new GUIContent(GetTextOrKey("inspector.rounded_corners.corner.softness"), GetTextOrKey("inspector.rounded_corners.corner.softness.tooltip")));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.outline.header"), boldLabelStyle);
            DrawPropertyLayout("_outlineColor", new GUIContent(GetTextOrKey("inspector.rounded_corners.outline.outlineColor")));
            DrawPropertyLayout("_outlineWidth", new GUIContent(GetTextOrKey("inspector.rounded_corners.outline.outlineWidth")));
            DrawPropertyLayout("_outlineSoftness", new GUIContent(GetTextOrKey("inspector.rounded_corners.outline.outlineSoftness"), GetTextOrKey("inspector.rounded_corners.outline.outlineSoftness.tooltip")));
            DrawPropertyLayout("_insideOutline", new GUIContent(GetTextOrKey("inspector.rounded_corners.outline.insideOutline")));
        }
    }
}