#nullable enable
using RuniOS.Inspectors;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Primitives
{
    public abstract class PrimitiveInspectorDrawer : IMGUIInspectorDrawer
    {
        protected PrimitiveInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        public sealed override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            using (new EditorGUI.MixedValueScope(!variableElement.IsReadable(flags) || variableElement.isMixedValue))
            {
                EditorGUI.BeginDisabledGroup(!variableElement.IsWritable(flags));
                EditorGUI.BeginChangeCheck();
                object value = DrawField(position, label ?? GUIContent.none, variableElement.IsReadable(flags) ? variableElement.value : variableElement.variableType.GetDefaultValue());
                if (EditorGUI.EndChangeCheck())
                    variableElement.value = value;
                EditorGUI.EndDisabledGroup();
            }
        }

        protected abstract object DrawField(Rect position, GUIContent label, object? value);
    }
}