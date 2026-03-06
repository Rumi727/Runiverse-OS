#nullable enable
using RuniOS.AnimatedValues;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(CornerRadius))]
    public class CornerRadiusInspectorDrawer : GenericInspectorDrawer
    {
        public CornerRadiusInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        readonly AnimFloat animFloat = new AnimFloat(EditorGUIUtility.singleLineHeight);
        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => CornerRadiusField(position, label, (CornerRadius)value!, animFloat);

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => EditorGUIUtility.singleLineHeight + animFloat.value;
    }
}