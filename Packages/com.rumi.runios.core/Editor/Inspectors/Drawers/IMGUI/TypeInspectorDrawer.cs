#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Type))]
    public class TypeInspectorDrawer : GenericInspectorDrawer
    {
        public TypeInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray) => TypeField(position, label, (Type?)value);
    }
}