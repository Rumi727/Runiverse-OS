#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(Enum), true, allowInDebug = true)]
    public class EnumInspectorDrawer : GenericInspectorDrawer
    {
        public EnumInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray)
        {
            Enum enumValue = (Enum)value!;
            if (enumValue.IsFlags())
                return EditorGUI.EnumFlagsField(position, label, enumValue);
            else
                return EditorGUI.EnumPopup(position, label, enumValue);
        }
    }
}