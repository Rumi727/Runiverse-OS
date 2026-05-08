using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    [CustomInspectorDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyInspectorAttributeDrawer(IInspectorAttribute attribute) : IMGUIInspectorAttributeDrawer(attribute)
    {
        protected override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            EditorGUI.BeginDisabledGroup(true);
            drawer.Draw(position, label, flags, context);
            EditorGUI.EndDisabledGroup();
        }
    }
}