#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives;

[CustomInspectorDrawer(typeof(ulong), allowInDebug = true)]
public class ULongInspectorDrawer : GenericInspectorDrawer
{
    public ULongInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

    protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.LongField(position, label, ((ulong)value!).ClampToLong()).ClampToULong();
}