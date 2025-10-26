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

            label ??= GUIContent.none;
            
            if (inspectableList.parentElement != null)
            {
                if (NullToggleField(inspectableList.parentElement, position, out _, label, flags))
                    return;
            }
            
            if (inspectableList.instancesIsEmpty)
            {
                EditorGUI.LabelField(position, label, new GUIContent(GetTextOrKey("inspector.no_instance")));   
                return;
            }
            
            reorderableList ??= new ReorderableList(inspectableList, elementType, true, false, true, true) { multiSelect = true, };

            reorderableList.drawElementCallback = (rect, index, _, _) => GetElementInspector(index, flags)?.Draw(rect, new GUIContent($"Element {index}"), true);
            reorderableList.elementHeightCallback = index => GetElementInspector(index, flags)?.GetHeight(label, flags, true) ?? EditorGUIUtility.singleLineHeight;
            reorderableList.onCanAddCallback = _ => elementType.HasDefaultConstructor(flags.HasFlagFast(InspectorFlags.NonPublic));
            
            reorderableList.onAddCallback = x =>
            {
                int index = x.selectedIndices.Any() ? (x.selectedIndices.Max() + 1).Min(x.count) : x.count;
                object? value;
                if (inspectableList.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                    value = elementType.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
                else
                    value = elementType.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
                
                inspectableList.Insert(index, value);
                x.Select(index);
            };

            reorderableList.onReorderCallbackWithDetails = (_, oldIndex, newIndex) => inspectableList.OnElementMoved(oldIndex, newIndex);
            
            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            isExpanded = DrawListHeader(position, Enumerable.Repeat(inspectableList, 1), label, isExpanded, isInArray);
            position.y += headHeight + 2;

            position.x += 15;
            position.width -= 15;
            
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

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckInspectableList();
            if (inspectableList.instancesIsEmpty)
                return EditorGUIUtility.singleLineHeight;
            
            float listHeight = reorderableList?.GetHeight() ?? 0;
            float headHeight = GetYSize(label ?? GUIContent.none, EditorStyles.foldoutHeader);

            if (!isInArray)
            {
                animFloat.target = isExpanded ? listHeight + 2 : 0;
                return headHeight + animFloat.value;
            }
            else
                return headHeight + (isExpanded ? listHeight + 2 : 0);
        }

        public Inspector? GetElementInspector(int index, InspectorFlags flags)
        {
            CheckInspectableList();
            
            IInspectorElement? element = inspectableList.GetElement(index, flags);
            if (element is not IInspectorListElement listElement)
                return null;

            elementInspectors.SyncKeysWithList(inspectableList.GetElements(flags), _ => new Inspector(rootInspector));
            if (elementInspectors.TryGetValue(element, out Inspector inspector))
            {
                if (inspector.elements.FirstOrDefault() != listElement || inspector.inspectorFlags != flags)
                    inspector.Rebuild(listElement, flags, true);
                
                return inspector;
            }

            return null;
        }
    }
}