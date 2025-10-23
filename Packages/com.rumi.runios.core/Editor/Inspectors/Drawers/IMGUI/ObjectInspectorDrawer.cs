#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(object), priority = int.MinValue)]
    public class ObjectInspectorDrawer : IMGUIInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) => inspector = new Inspector(rootInspector);
        
        public Inspector inspector { get; }
        public bool isExpanded
        {
            get => animBool.target;
            set => animBool.target = value;
        }

        static float foldoutYSize => GetYSize(EditorStyles.foldout);

        readonly AnimBool animBool = new AnimBool(false);
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            position.height = foldoutYSize;
            animBool.target = EditorGUI.Foldout(position, animBool.target, label, true);
            
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
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + (position.height * animBool.faded)));

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
            if (!isInArray)
                size += (inspector.GetHeight(label, flags, isInArray) * animBool.faded) + 2;
            else
                size += isExpanded ? inspector.GetHeight(label, flags, isInArray) + 2 : 0;

            return size;
        }
    }
}