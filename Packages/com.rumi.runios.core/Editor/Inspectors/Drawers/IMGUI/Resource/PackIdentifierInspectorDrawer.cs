#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(PackIdentifier))]
    public class PackIdentifierInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {

        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => RuniFields.PackIdentifierField(position, label, (PackIdentifier)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default) => RuniFields.GetMultiColumnsFieldHeight(label);
    }
}