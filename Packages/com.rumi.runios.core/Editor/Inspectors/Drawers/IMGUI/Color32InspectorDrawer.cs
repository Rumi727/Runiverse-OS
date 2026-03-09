#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Color32))]
    public class Color32InspectorDrawer : GenericInspectorDrawer
    {
        public Color32InspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping) => (Color32)EditorGUI.ColorField(position, label, (Color32)value!);
    }
}