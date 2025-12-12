#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(long), allowInDebug = true)]
    public class LongInspectorDrawer : GenericInspectorDrawer
    {
        public LongInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.LongField(position, label, (long)value!);
    }
}