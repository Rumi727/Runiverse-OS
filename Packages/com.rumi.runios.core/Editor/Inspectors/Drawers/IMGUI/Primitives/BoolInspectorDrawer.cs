#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(bool), allowInDebug = true)]
    public class BoolInspectorDrawer : GenericInspectorDrawer
    {
        public BoolInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.Toggle(position, label, (bool)value!);
    }
}