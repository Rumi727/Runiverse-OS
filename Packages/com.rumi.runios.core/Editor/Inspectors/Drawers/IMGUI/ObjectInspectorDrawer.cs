#nullable enable
using RuniOS.Inspectors;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public class ObjectInspectorDrawer : IMGUIInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) => inspector = new Inspector(rootInspector);
        
        public Inspector inspector { get; }
        public bool isExpanded { get; set; } = false;

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags inspectorFlags = InspectorFlags.All, bool isInArray = false)
        {
            if (variableElement == null)
                return;

            isExpanded = EditorGUI.Foldout(position, isExpanded, label);
            
            if (inspector.inspectable != variableElement.inspectableObjectElement || inspector.inspectorFlags != inspectorFlags)
                inspector.Rebuild(variableElement.inspectableObjectElement, inspectorFlags);
            
            BeginIndentLevel();
            inspector.Draw(position, label, isInArray);
            EndIndentLevel();
        }

        public override float GetHeight() => inspector.GetHeight();
    }
}