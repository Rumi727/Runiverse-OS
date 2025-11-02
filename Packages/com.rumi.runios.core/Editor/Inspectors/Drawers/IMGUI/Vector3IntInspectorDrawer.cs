#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector3Int))]
    public class Vector3IntInspectorDrawer : GenericInspectorDrawer
    {
        public Vector3IntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value) => EditorGUI.Vector3IntField(position, label, (Vector3Int)value!);
    }
}