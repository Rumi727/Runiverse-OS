#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector2Int))]
    public class Vector2IntInspectorDrawer : GenericInspectorDrawer
    {
        public Vector2IntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.Vector2IntField(position, label, (Vector2Int)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (EditorGUIUtility.wideMode || !LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
    }
}