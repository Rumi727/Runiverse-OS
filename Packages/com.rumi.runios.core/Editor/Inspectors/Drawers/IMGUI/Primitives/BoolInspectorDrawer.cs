#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(bool))]
    public class BoolInspectorDrawer : IMGUIInspectorDrawer
    {
        public BoolInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(variableElement.isMixedValue))
            {
                EditorGUI.BeginChangeCheck();
                
                bool value = EditorGUI.Toggle(position, label ?? GUIContent.none, (bool)variableElement.value!);
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value;
            }
        }
    }
}