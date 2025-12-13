#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(nuint), allowInDebug = true)]
    public class NUIntInspectorDrawer : GenericInspectorDrawer
    {
        public NUIntInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.LongField(position, label, ((nuint)value!).ClampToLong()).ClampToNUInt();
    }
}