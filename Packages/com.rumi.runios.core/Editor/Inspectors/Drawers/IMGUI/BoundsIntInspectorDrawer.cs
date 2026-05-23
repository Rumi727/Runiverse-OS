#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(BoundsInt))]
    public class BoundsIntInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => EditorGUI.BoundsIntField(position, label, (BoundsInt)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default) => RuniFields.GetMultiRowsFieldHeight(label, 2);
    }
}