#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(decimal), allowInDebug = true)]
    public class DecimalInspectorDrawer : GenericInspectorDrawer
    {
        public DecimalInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.DoubleField(position, label, ((decimal)value!).ClampToDouble()).ClampToDecimal();
    }
}