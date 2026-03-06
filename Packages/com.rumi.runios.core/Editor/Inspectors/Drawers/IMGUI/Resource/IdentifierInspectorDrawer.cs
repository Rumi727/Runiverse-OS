#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(Identifier))]
    public class IdentifierInspectorDrawer : GenericInspectorDrawer
    {
        public IdentifierInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => IdentifierField(position, label, (Identifier)value!);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiColumnsFieldHeight(label);
    }
}