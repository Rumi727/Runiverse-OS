#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [InspectorDrawer(typeof(Vector3Int))]
    public class Vector3IntInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => EditorGUI.Vector3IntField(position, label, (Vector3Int)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default) => RuniFields.GetMultiColumnsFieldHeight(label);
    }
}