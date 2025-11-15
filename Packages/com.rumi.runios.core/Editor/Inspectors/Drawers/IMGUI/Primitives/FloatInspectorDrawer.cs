#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(float), allowInDebug = true)]
    public class FloatInspectorDrawer : GenericInspectorDrawer
    {
        public FloatInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.FloatField(position, label, (float)value!);
    }
}