#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.IO;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.IO
{
    [CustomInspectorDrawer(typeof(RuniPath))]
    public class RuniPathInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => RuniFields.RuniPathField(position, label, (RuniPath)value!);
    }
}