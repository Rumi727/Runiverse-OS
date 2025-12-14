#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Bounds))]
    public class BoundsInspectorDrawer : GenericInspectorDrawer
    {
        public BoundsInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.BoundsField(position, label, (Bounds)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiRowsFieldHeight(label, 2);
    }
}