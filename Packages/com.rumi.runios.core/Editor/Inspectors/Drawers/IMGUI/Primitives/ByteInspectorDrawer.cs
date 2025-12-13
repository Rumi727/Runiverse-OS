#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(byte), allowInDebug = true)]
    public class ByteInspectorDrawer : GenericInspectorDrawer
    {
        public ByteInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.IntField(position, label, (byte)value!).ClampToByte();
    }
}