#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector4))]
    public class Vector4InspectorDrawer : GenericInspectorDrawer
    {
        public Vector4InspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.Vector4Field(position, label, (Vector4)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (EditorGUIUtility.wideMode || !LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
    }
}