#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(Pointer))]
    [CustomInspectorDrawer(typeof(void*), true, allowInDebug = true)]
    public class VoidPointerInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object? DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default)
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
}