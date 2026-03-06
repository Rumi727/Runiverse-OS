using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    [CustomInspectorDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyInspectorAttributeDrawer : IMGUIInspectorAttributeDrawer
    {
        public ReadOnlyInspectorAttributeDrawer(IInspectorAttribute attribute) : base(attribute) { }

        protected override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, bool isInArray, Rect? clipping)
        {
            EditorGUI.BeginDisabledGroup(true);
            drawer.Draw(position, label, flags, isInArray, clipping);
            EditorGUI.EndDisabledGroup();
        }
    }
}