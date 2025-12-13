#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Gradient))]
    public class GradientInspectorDrawer : GenericInspectorDrawer
    {
        public GradientInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.GradientField(position, label, (Gradient)value!);
    }
}