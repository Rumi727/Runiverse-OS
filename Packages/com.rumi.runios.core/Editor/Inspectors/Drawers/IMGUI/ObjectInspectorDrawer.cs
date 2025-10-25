#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(object))]
    public class ObjectInspectorDrawer : IMGUIInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) => inspector = new Inspector(rootInspector);
        
        public Inspector inspector { get; }
        public bool isExpanded
        {
            get => animBool.target > 0;
            set => animBool.target = value ? 1 : 0;
        }

        static float foldoutYSize => GetYSize(EditorStyles.foldout);

        readonly AnimFloat animBool = new AnimFloat(0);
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            position.height = foldoutYSize;
            isExpanded = EditorGUI.Foldout(position, isExpanded, label, true);
            
            if (inspector.inspectable != variableElement.inspectableObjectElement || inspector.inspectorFlags != flags)
                inspector.Rebuild(variableElement.inspectableObjectElement, flags);

            position.y += foldoutYSize + 2;
            position.height = inspector.GetHeight(label, flags, isInArray);
            
            BeginIndentLevel();

            if (!isInArray)
            {
                if (isExpanded || animBool.isAnimating)
                {
                    if (animBool.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + (position.height * animBool.value)));

                    inspector.Draw(position, label, isInArray);

                    if (animBool.isAnimating)
                        GUI.EndClip();

                    if (animBool.isAnimating)
                        RepaintCurrentWindow();
                }
            }
            else if (isExpanded)
                inspector.Draw(position, label, isInArray);

            EndIndentLevel();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            float size = foldoutYSize;
            if (!isInArray && animBool.isAnimating)
                size += ((inspector.GetHeight(label, flags, isInArray) + 2) * animBool.value);
            else
                size += isExpanded ? inspector.GetHeight(label, flags, isInArray) + 2 : 0;

            return size;
        }
    }
}