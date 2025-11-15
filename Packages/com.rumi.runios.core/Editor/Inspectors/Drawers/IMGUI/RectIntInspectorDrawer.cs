#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI;

[CustomInspectorDrawer(typeof(RectInt))]
public class RectIntInspectorDrawer : GenericInspectorDrawer
{
    public RectIntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

    protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.RectIntField(position, label, (RectInt)value!);
        
    public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
    {
        if (EditorGUIUtility.wideMode || !LabelHasContent(label))
            return EditorGUIUtility.singleLineHeight;
        else
            return (EditorGUIUtility.singleLineHeight * 3) + 4;
    }
}