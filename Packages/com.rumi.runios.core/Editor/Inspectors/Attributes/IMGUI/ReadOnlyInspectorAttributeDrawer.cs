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

        public override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.None | InspectorFlags.Public | InspectorFlags.Static | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.WriteOnly | InspectorFlags.InstanceAccess | InspectorFlags.PublicAccess | InspectorFlags.Property | InspectorFlags.Event | InspectorFlags.Field | InspectorFlags.Method | InspectorFlags.Variable | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            EditorGUI.BeginDisabledGroup(true);
            drawer.Draw(position, label, flags, isInArray, clipping);
            EditorGUI.EndDisabledGroup();
        }
    }
}