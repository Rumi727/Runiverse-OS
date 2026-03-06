#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Version))]
    public class VersionInspectorDrawer : GenericInspectorDrawer
    {
        public VersionInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => VersionField(position, label, (Version)value!);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiColumnsFieldHeight(label ?? GUIContent.none);
    }
}