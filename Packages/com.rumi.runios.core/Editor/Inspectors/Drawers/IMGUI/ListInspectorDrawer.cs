#nullable enable
using RuniOS.Inspectors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public class ListInspectorDrawer : IMGUIInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ListInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        public ReorderableList? reorderableList;
        public bool isExpanded { get; set; } = false;

        readonly AnimFloat animFloat = new AnimFloat(0);

        readonly Dictionary<IInspectorElement, Inspector> elementInspectors = new();
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags inspectorFlags = InspectorFlags.All, bool isInArray = false)
        {
            label ??= new GUIContent(element?.displayName ?? inspectable.inspectionDisplayName);
            if (inspectableList == null)
                throw new InvalidOperationException($"{nameof(inspectableList)} is null");

            if (!inspectableList.TryGetInspectionElementType(out Type? elementType) || elementType == null)
                throw new InvalidOperationException($"Cannot get managed type of {nameof(inspectableList)}");

            elementInspectors.SyncKeysWithList(inspectableList.GetElements(inspectorFlags), _ => new Inspector(rootInspector));
            
            reorderableList ??= new ReorderableList(inspectableList, elementType, true, false, true, true)
            {
                multiSelect = true,
                drawElementCallback = (rect, index, _, _) => GetElementInspector(index, inspectorFlags)?.Draw(rect, null, true),
                elementHeightCallback = index => GetElementInspector(index, inspectorFlags)?.GetHeight() ?? EditorGUIUtility.singleLineHeight
            };
            
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
        
        public Inspector? GetElementInspector(int index, InspectorFlags inspectorFlags)
        {
            IInspectorElement? element = inspectableList?.GetElement(index, inspectorFlags);
            if (element is not IInspectorListElement listElement)
                return null;
                    
            Inspector inspector = elementInspectors[element];
            inspector.targetElement = listElement;
            inspector.inspectorFlags = inspectorFlags;

            return inspector;
        }
    }
}