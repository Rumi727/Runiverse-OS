#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(short), allowInDebug = true)]
    public class ShortInspectorDrawer : GenericInspectorDrawer
    {
        public ShortInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.IntField(position, label, (short)value!).ClampToShort();
    }
}