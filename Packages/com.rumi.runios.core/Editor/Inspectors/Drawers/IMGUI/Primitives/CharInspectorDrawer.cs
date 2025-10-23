#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(char))]
    public class CharInspectorDrawer : IMGUIInspectorDrawer
    {
        public CharInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(variableElement.isMixedValue))
            {
                EditorGUI.BeginChangeCheck();
                
                char value = EditorTool.CharField(position, label ?? GUIContent.none, (char)variableElement.value!);
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value;
            }
        }
    }
}