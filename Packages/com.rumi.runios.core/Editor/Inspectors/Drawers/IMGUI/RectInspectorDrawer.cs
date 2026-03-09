#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Rect))]
    public class RectInspectorDrawer : GenericInspectorDrawer
    {
        public RectInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping) => EditorGUI.RectField(position, label, (Rect)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, bool isInArray, Rect? clipping) => GetMultiColumnsFieldHeight(label, 2);
    }
}