#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Color32))]
    public class Color32InspectorDrawer : GenericInspectorDrawer
    {
        public Color32InspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => (Color32)EditorGUI.ColorField(position, label, (Color32)value!);
    }
}