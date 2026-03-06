using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    [CustomInspectorDrawer(typeof(FieldNameAttribute))]
    public class FieldNameInspectorAttributeDrawer : IMGUIInspectorAttributeDrawer
    {
        public FieldNameInspectorAttributeDrawer(IInspectorAttribute attribute) : base(attribute) { }

        public override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.None | InspectorFlags.Public | InspectorFlags.Static | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.WriteOnly | InspectorFlags.InstanceAccess | InspectorFlags.PublicAccess | InspectorFlags.Property | InspectorFlags.Event | InspectorFlags.Field | InspectorFlags.Method | InspectorFlags.Variable | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            FieldNameAttribute attribute = (FieldNameAttribute)this.attribute;
            if (attribute.force || label == null)
                label = new GUIContent(GetTextOrKey(attribute.name));
            
            drawer.Draw(position, label, flags, isInArray, clipping);
        }
    }
}