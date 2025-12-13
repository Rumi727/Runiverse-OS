#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(nint), allowInDebug = true)]
    public class NIntInspectorDrawer : GenericInspectorDrawer
    {
        public NIntInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.LongField(position, label, ((nint)value!).ClampToLong()).ClampToNInt();
    }
}