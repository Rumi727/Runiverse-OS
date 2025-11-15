#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(nint), allowInDebug = true)]
    public class NIntInspectorDrawer : GenericInspectorDrawer
    {
        public NIntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.LongField(position, label, ((nint)value!).ClampToLong()).ClampToNInt();
    }
}