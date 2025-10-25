#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(char))]
    public class CharInspectorDrawer : PrimitiveInspectorDrawer
    {
        public CharInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorTool.CharField(position, label, (char)value!);
    }
}