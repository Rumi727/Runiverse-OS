#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(RectOffset))]
    public class RectOffsetInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => RectOffsetField(position, label, (RectOffset)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default) => GetMultiColumnsFieldHeight(label);
    }
}