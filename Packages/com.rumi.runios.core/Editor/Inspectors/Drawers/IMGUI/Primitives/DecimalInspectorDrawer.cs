#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(decimal), allowInDebug = true)]
    public class DecimalInspectorDrawer : PrimitiveInspectorDrawer
    {
        public DecimalInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.DoubleField(position, label, ((decimal)value!).ClampToDouble()).ClampToDecimal();
    }
}