#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Bounds))]
    public class BoundsInspectorDrawer : GenericInspectorDrawer
    {
        public BoundsInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.BoundsField(position, label, (Bounds)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (!LabelHasContent(label))
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
            else
                return (EditorGUIUtility.singleLineHeight * 3) + 4;
        }
    }
}