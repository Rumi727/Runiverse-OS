#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(ResourceKey))]
    public class ResourceKeyInspectorDrawer : GenericInspectorDrawer
    {
        public ResourceKeyInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => ResourceKeyField(position, label, (ResourceKey)value!);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiRowsFieldHeight(label, 2);
    }
}