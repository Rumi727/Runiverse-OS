#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(nuint), allowInDebug = true)]
    public class NUIntInspectorDrawer : GenericInspectorDrawer
    {
        public NUIntInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.LongField(position, label, ((nuint)value!).ClampToLong()).ClampToNUInt();
    }
}