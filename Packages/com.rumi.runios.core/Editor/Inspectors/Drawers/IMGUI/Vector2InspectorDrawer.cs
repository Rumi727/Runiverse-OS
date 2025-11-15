#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI;

[CustomInspectorDrawer(typeof(Vector2))]
public class Vector2InspectorDrawer : GenericInspectorDrawer
{
    public Vector2InspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

    protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.Vector2Field(position, label, (Vector2)value!);

    public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
    {
        if (EditorGUIUtility.wideMode || !EditorTool.LabelHasContent(label))
            return EditorGUIUtility.singleLineHeight;
        else
            return (EditorGUIUtility.singleLineHeight * 2) + 2;
    }
}