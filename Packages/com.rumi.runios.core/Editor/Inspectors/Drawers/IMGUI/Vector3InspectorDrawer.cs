#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector3))]
    public class Vector3InspectorDrawer : GenericInspectorDrawer
    {
        public Vector3InspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.Vector3Field(position, label, (Vector3)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (EditorGUIUtility.wideMode || !LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
    }
}