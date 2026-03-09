#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(string), allowInDebug = true)]
    public class StringInspectorDrawer : GenericInspectorDrawer
    {
        public StringInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping) => EditorGUI.TextField(position, label, (string?)value ?? string.Empty);
    }
}