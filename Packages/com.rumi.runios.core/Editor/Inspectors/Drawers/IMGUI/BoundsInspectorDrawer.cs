#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Bounds))]
    public class BoundsInspectorDrawer : GenericInspectorDrawer
    {
        public BoundsInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping) => EditorGUI.BoundsField(position, label, (Bounds)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, bool isInArray, Rect? clipping) => GetMultiRowsFieldHeight(label, 2);
    }
}