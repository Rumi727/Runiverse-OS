#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(byte))]
    public class ByteInspectorDrawer : PrimitiveInspectorDrawer
    {
        public ByteInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.IntField(position, label, (byte)value!).ClampToByte();
    }
}