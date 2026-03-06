#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(PackIdentifier))]
    public class PackIdentifierInspectorDrawer : GenericInspectorDrawer
    {
        public PackIdentifierInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => PackIdentifierField(position, label, (PackIdentifier)value!);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiColumnsFieldHeight(label);
    }
}