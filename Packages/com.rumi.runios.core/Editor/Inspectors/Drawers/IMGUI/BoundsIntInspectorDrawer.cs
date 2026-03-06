#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(BoundsInt))]
    public class BoundsIntInspectorDrawer : GenericInspectorDrawer
    {
        public BoundsIntInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.BoundsIntField(position, label, (BoundsInt)value!);
        
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiRowsFieldHeight(label, 2);
    }
}