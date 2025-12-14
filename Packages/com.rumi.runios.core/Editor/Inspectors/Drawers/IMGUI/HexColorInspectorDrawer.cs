#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(HexColor))]
    public class HexColorInspectorDrawer : GenericInspectorDrawer
    {
        public HexColorInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.ColorField(position, label, (HexColor)value!);
    }
}