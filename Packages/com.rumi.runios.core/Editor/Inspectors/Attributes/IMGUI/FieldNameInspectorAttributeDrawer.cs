using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    [CustomInspectorDrawer(typeof(FieldNameAttribute))]
    public class FieldNameInspectorAttributeDrawer(IInspectorAttribute attribute) : IMGUIInspectorAttributeDrawer(attribute)
    {

        protected override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            FieldNameAttribute attribute = (FieldNameAttribute)this.attribute;
            if (attribute.force || label == null)
                label = new GUIContent(GetTextOrKey(attribute.name));
            
            drawer.Draw(position, label, flags, context);
        }
    }
}