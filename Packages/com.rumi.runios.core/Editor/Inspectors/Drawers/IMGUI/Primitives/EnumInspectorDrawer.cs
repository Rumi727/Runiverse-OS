#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(Enum), true, allowInDebug = true)]
    public class EnumInspectorDrawer : GenericInspectorDrawer
    {
        public EnumInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value)
        {
            Enum enumValue = (Enum)value!;
            if (enumValue.IsFlags())
                return EditorGUI.EnumFlagsField(position, label, enumValue);
            else
                return EditorGUI.EnumPopup(position, label, enumValue);
        }
    }
}