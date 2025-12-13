#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(HexColor))]
    public class HexColorInspectorDrawer : GenericInspectorDrawer
    {
        public HexColorInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.ColorField(position, label, (HexColor)value!);
    }
}