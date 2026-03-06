#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector4))]
    public class Vector4InspectorDrawer : GenericInspectorDrawer
    {
        public Vector4InspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => EditorGUI.Vector4Field(position, label, (Vector4)value!);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => GetMultiColumnsFieldHeight(label);
    }
}