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

        protected override void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, bool isInArray, Rect? clipping)
        {
            FieldNameAttribute attribute = (FieldNameAttribute)this.attribute;
            if (attribute.force || label == null)
                label = new GUIContent(GetTextOrKey(attribute.name));
            
            drawer.Draw(position, label, flags, isInArray, clipping);
        }
    }
}