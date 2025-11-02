#nullable enable
using RuniOS.Inspectors;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class GenericInspectorDrawer : IMGUIInspectorDrawer
    {
        protected GenericInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public sealed override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();
            
            BeginWideMode(true);

            bool isReadable = !variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags); 
            using (new EditorGUI.MixedValueScope(!isReadable || variableElement.isMixedValue))
            {
                EditorGUI.BeginDisabledGroup(variableElement.inspectable.instancesIsEmpty || !variableElement.IsWritable(flags));
                EditorGUI.BeginChangeCheck();
                object value = DrawField(position, label ?? GUIContent.none, isReadable ? variableElement.value : variableElement.variableType.GetDefaultValue());
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value;
                EditorGUI.EndDisabledGroup();
            }
            
            EndWideMode();
        }

        protected abstract object DrawField(Rect position, GUIContent label, object? value);
    }
}