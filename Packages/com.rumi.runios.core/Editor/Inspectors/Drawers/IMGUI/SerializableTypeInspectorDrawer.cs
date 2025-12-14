#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(SerializableType))]
    public class SerializableTypeInspectorDrawer : GenericInspectorDrawer
    {
        public SerializableTypeInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => (SerializableType)TypeField(position, label, (SerializableType)value!);
    }
}