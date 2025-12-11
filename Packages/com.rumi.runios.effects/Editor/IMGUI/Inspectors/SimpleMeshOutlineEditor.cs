#nullable enable
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.Effects;
using RuniOS.UI.Effects;

namespace RuniOS.Editor.Effects.IMGUI.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleMeshOutline))]
    public class SimpleMeshOutlineEditor : CustomInspectorBase<RoundedCorners>
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(GetTextOrKey("inspector.simple_mesh_outline.appearance.header"), boldLabelStyle);
            DrawPropertyLayout("_color", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.appearance.color")));
            
            Space();
            
            DrawPropertyLayout("_width", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.appearance.width")));
            DrawPropertyLayout("_useFixedWidth", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.appearance.useFixedWidth"), GetTextOrKey("inspector.simple_mesh_outline.appearance.useFixedWidth.tooltip")));
            
            Space();
            
            DrawPropertyLayout("_gap", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.appearance.gap")));
            DrawPropertyLayout("_useFixedGap", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.appearance.useFixedGap"), GetTextOrKey("inspector.simple_mesh_outline.appearance.useFixedGap.tooltip")));
            
            Space();
            
            GUILayout.Label(GetTextOrKey("inspector.simple_mesh_outline.settings.header"), boldLabelStyle);
            DrawPropertyLayout("_outlineVisibility", new GUIContent(GetTextOrKey("inspector.simple_mesh_outline.settings.outlineVisibility"), GetTextOrKey("inspector.simple_mesh_outline.settings.outlineVisibility.tooltip")));
        }
    }
}