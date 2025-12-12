#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(SerializableType))]
    public class SerializableTypeInspectorDrawer : GenericInspectorDrawer
    {
        public SerializableTypeInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => (SerializableType)TypeField(position, label, (SerializableType)value!);
    }
}