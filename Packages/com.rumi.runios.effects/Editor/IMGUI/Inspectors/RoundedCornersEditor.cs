#nullable enable
using RuniOS.Editor.IMGUI.Inspectors;
using RuniOS.UI.Effects;

namespace RuniOS.Editor.Effects.IMGUI.Inspectors
{
    [CustomEditor(typeof(RoundedCorners))]
    public class RoundedCornersEditor : CustomInspectorBase<RoundedCorners>
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.corner.header"), boldLabelStyle);
            DrawPropertyLayout("_radius", new GUIContent(GetTextOrKey("inspector.rounded_corners.corner.radius")));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.render.header"), boldLabelStyle);
            DrawPropertyLayout("_useAntiAliasing", new GUIContent(GetTextOrKey("inspector.rounded_corners.render.useAntiAliasing")));
            
            EditorGUI.BeginDisabledGroup(HasSameValue(x => x.useAntiAliasing) && !target.useAntiAliasing);
            DrawPropertyLayout("_softness", new GUIContent(GetTextOrKey("inspector.rounded_corners.render.softness"), GetTextOrKey("inspector.rounded_corners.render.softness.tooltip")));
            EditorGUI.EndDisabledGroup();
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.rounded_corners.update.header"), boldLabelStyle);
            DrawPropertyLayout("_autoRebuildWithMask", new GUIContent(GetTextOrKey("inspector.rounded_corners.update.autoRebuildWithMask"), GetTextOrKey("inspector.rounded_corners.update.autoRebuildWithMask.tooltip")));
            DrawPropertyLayout("_alwaysRebuildMaterial", new GUIContent(GetTextOrKey("inspector.rounded_corners.update.alwaysRebuildMaterial"), GetTextOrKey("inspector.rounded_corners.update.alwaysRebuildMaterial.tooltip")));
        }
    }
}