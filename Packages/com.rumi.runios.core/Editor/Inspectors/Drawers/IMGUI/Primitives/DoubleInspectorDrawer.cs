#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(double), allowInDebug = true)]
    public class DoubleInspectorDrawer : GenericInspectorDrawer
    {
        public DoubleInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.DoubleField(position, label, (double)value!);
    }
}