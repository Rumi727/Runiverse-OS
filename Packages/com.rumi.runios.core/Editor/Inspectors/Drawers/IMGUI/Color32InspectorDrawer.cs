#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Color32))]
    public class Color32InspectorDrawer : GenericInspectorDrawer
    {
        public Color32InspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => (Color32)EditorGUI.ColorField(position, label, (Color32)value!);
    }
}