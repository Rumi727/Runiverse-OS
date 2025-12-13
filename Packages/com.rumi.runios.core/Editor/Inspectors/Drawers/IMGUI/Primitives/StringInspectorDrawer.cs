#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(string), allowInDebug = true)]
    public class StringInspectorDrawer : GenericInspectorDrawer
    {
        public StringInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.TextField(position, label, (string?)value ?? string.Empty);
    }
}