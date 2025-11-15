#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives;

[CustomInspectorDrawer(typeof(Pointer))]
[CustomInspectorDrawer(typeof(void*), true, allowInDebug = true)]
public class VoidPointerInspectorDrawer : GenericInspectorDrawer
{
    public VoidPointerInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

    protected override object? DrawField(Rect position, GUIContent label, object? value)
    {
        CheckVariableElement();

        string typeName = variableElement.variableType.GetTypeDisplayName();
        if (!EditorGUI.showMixedValue)
        {
            IntPtr pointer = ((Pointer)value!).ToIntPtr();
            EditorGUI.LabelField(position, label, new GUIContent($"0x{pointer.ToString("X" + (IntPtr.Size * 2))} ({pointer}) ({typeName})"));
        }
        else
            EditorGUI.LabelField(position, label,  new GUIContent($"— ({typeName})"));

        return value;
    }
}