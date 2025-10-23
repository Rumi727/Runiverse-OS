#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(decimal))]
    public class DecimalInspectorDrawer : IMGUIInspectorDrawer
    {
        public DecimalInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(variableElement.isMixedValue))
            {
                EditorGUI.BeginChangeCheck();
                
                double value = EditorGUI.DoubleField(position, label ?? GUIContent.none, ((decimal)variableElement.value!).ClampToDouble());
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value.ClampToDecimal();
            }
        }
    }
}