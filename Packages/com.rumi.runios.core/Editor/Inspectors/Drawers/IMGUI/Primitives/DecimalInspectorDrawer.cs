#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(decimal), allowInDebug = true)]
    public class DecimalInspectorDrawer : GenericInspectorDrawer
    {
        public DecimalInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.DoubleField(position, label, ((decimal)value!).ClampToDouble()).ClampToDecimal();
    }
}