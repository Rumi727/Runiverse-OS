#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives;

[CustomInspectorDrawer(typeof(uint), allowInDebug = true)]
public class UIntInspectorDrawer : GenericInspectorDrawer
{
    public UIntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

    protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.LongField(position, label, (uint)value!).ClampToUInt();
}