#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(int))]
    public class IntInspectorDrawer : IMGUIInspectorDrawer
    {
        public IntInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(variableElement.isMixedValue))
            {
                EditorGUI.BeginChangeCheck();
                
                int value = EditorGUI.IntField(position, label ?? GUIContent.none, (int)variableElement.value!);
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value;
            }
        }
    }
}