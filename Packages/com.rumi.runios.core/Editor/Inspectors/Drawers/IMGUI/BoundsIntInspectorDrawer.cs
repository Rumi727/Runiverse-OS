#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(BoundsInt))]
    public class BoundsIntInspectorDrawer : GenericInspectorDrawer
    {
        public BoundsIntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.BoundsIntField(position, label, (BoundsInt)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (!LabelHasContent(label))
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
            else
                return (EditorGUIUtility.singleLineHeight * 3) + 4;
        }
    }
}