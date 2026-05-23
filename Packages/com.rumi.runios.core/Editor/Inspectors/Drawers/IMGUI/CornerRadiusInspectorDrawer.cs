#nullable enable
using RuniOS.AnimatedValues;
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(CornerRadius))]
    public class CornerRadiusInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        readonly AnimFloat animFloat = new AnimFloat(EditorGUIUtility.singleLineHeight);
        protected override object DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default) => RuniFields.CornerRadiusField(position, label, (CornerRadius)value!, animFloat);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default) => EditorGUIUtility.singleLineHeight + animFloat.value;
    }
}