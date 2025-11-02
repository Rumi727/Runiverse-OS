#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Rect))]
    public class RectInspectorDrawer : GenericInspectorDrawer
    {
        public RectInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.RectField(position, label, (Rect)value!);
    }
}