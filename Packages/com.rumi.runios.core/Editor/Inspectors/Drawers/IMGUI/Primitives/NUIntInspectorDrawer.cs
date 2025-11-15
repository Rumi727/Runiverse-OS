#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(nuint), allowInDebug = true)]
    public class NUIntInspectorDrawer : GenericInspectorDrawer
    {
        public NUIntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.LongField(position, label, ((nuint)value!).ClampToLong()).ClampToNUInt();
    }
}