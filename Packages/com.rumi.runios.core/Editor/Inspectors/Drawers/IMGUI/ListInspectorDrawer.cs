#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(IList))]
    public class ListInspectorDrawer : IMGUIInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ListInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        public ReorderableList? reorderableList { get; private set; }
        public bool isExpanded { get; set; } = false;

        readonly AnimFloat animFloat = new AnimFloat(0);

        readonly Dictionary<IInspectorElement, Inspector> elementInspectors = new();
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckInspectableList();
            
            if (!inspectableList.TryGetInspectionElementType(out Type? elementType) || elementType == null)
                throw new InvalidOperationException($"Cannot get managed type of {nameof(inspectableList)}");

            label ??= new GUIContent(element?.displayName ?? inspectable.inspectionDisplayName);
            
            elementInspectors.SyncKeysWithList(inspectableList.GetElements(flags), _ => new Inspector(rootInspector));
            
            reorderableList ??= new ReorderableList(inspectableList, elementType, true, false, true, true) { multiSelect = true, };

            reorderableList.drawElementCallback = (rect, index, _, _) => GetElementInspector(index, flags)?.Draw(rect, null, true);
            reorderableList.elementHeightCallback = index => GetElementInspector(index, flags)?.GetHeight(label, flags, isInArray) ?? EditorGUIUtility.singleLineHeight;
            
            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            isExpanded = DrawListHeader(position, Enumerable.Repeat(inspectableList, 1), label, isExpanded);
            position.y += headHeight + 2;
            
            isExpanded = EditorGUI.Foldout(position, isExpanded, label);
            
            if (!isInArray)
            {
                if (isExpanded || animFloat.isAnimating)
                {
                    if (animFloat.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + animFloat.value));

                    reorderableList.DoList(position);

                    if (animFloat.isAnimating)
                        GUI.EndClip();
                }

                if (animFloat.isAnimating)
                    RepaintCurrentWindow();
            }
            else if (isExpanded)
                reorderableList.DoList(position);
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => reorderableList?.GetHeight() ?? base.GetHeight(label, flags, isInArray);

        public Inspector? GetElementInspector(int index, InspectorFlags flags)
        {
            IInspectorElement? element = inspectableList?.GetElement(index, flags);
            if (element is not IInspectorListElement listElement)
                return null;
                    
            Inspector inspector = elementInspectors[element];
            if (inspector.elements.Length != 1 || inspector.elements[0] == listElement || inspector.inspectorFlags != flags)
                inspector.Rebuild(Enumerable.Repeat(listElement, 1));

            return inspector;
        }
    }
}