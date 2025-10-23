#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    [CustomInspectorDrawer(typeof(ulong))]
    public class ULongInspectorDrawer : IMGUIInspectorDrawer
    {
        public ULongInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(variableElement.isMixedValue))
            {
                EditorGUI.BeginChangeCheck();
                
                long value = EditorGUI.LongField(position, label ?? GUIContent.none, ((ulong)variableElement.value!).ClampToLong());
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value.ClampToULong();
            }
        }
    }
}