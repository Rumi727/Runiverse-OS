#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [InspectorDrawer(typeof(Enum), true, allowInDebug = true)]
    public class EnumInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object? DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default)
        {
            Enum enumValue = (Enum)value!;
            if (enumValue.IsFlags())
                return EditorGUI.EnumFlagsField(position, label, enumValue);
            else
                return EditorGUI.EnumPopup(position, label, enumValue);
        }
    }
}