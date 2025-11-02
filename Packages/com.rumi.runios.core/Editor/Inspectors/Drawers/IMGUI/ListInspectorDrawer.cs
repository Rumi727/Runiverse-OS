#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(ICollection))]
    [CustomInspectorDrawer(typeof(ICollection<>))]
    public class ListInspectorDrawer : IMGUIInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ListInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        public ReorderableList? reorderableList { get; private set; }
        
        public bool isExpanded { get; set; } = false;

        readonly AnimFloat animFloat = new AnimFloat(0);
        readonly ConditionalWeakTable<IInspectorListElement, Inspector> elementInspectors = new();
        
        object? CreateElementItem(InspectorFlags flags, Type? elementType)
        {
            if (elementType == null)
                throw new NullReferenceException($"{nameof(elementType)} is null");
            
            CheckInspectableList();
            if (inspectableList.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                return elementType.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
            else
                return elementType.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckInspectableList();
            inspectableList.TryGetInspectionElementType(out Type? elementType);

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

            reorderableList ??= new ReorderableList(inspectableList, elementType ?? typeof(object), true, false, true, true) { multiSelect = true, };

            reorderableList.drawElementCallback = (rect, index, _, _) => GetElementInspector(index, flags)?.Draw(rect, new GUIContent($"Element {index}"), true);
            reorderableList.elementHeightCallback = index => GetElementInspector(index, flags)?.GetHeight(label, flags, true) ?? EditorGUIUtility.singleLineHeight;
            reorderableList.onCanAddCallback = _ =>
                (!inspectableList.isArray || (inspectableList.parentElement != null && inspectableList.parentElement.IsWritable(flags))) &&
                !inspectableList.IsFixedSize &&
                elementType != null && elementType.HasDefaultConstructor(flags.HasFlagFast(InspectorFlags.NonPublic));
            reorderableList.onCanRemoveCallback = _ => !inspectableList.IsFixedSize;
            
            reorderableList.onAddCallback = x =>
            {
                int index = x.selectedIndices.Any() ? (x.selectedIndices.Max() + 1).Min(x.count) : x.count;   
                inspectableList.Insert(index, CreateElementItem(flags, elementType));
                x.Select(index);
            };

            reorderableList.onReorderCallbackWithDetails = (_, oldIndex, newIndex) => inspectableList.OnElementMoved(oldIndex, newIndex);
            reorderableList.onChangedCallback = _ => inspectableList.UpdateSourceCollections();
            
            inspectableList.SynchronizeCollections();
            
            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            EditorGUI.BeginChangeCheck();
            isExpanded = DrawListHeader(position, Enumerable.Repeat(inspectableList, 1), label, isExpanded, elementType != null ? (_ => CreateElementItem(flags, elementType)) : null, isInArray);
            position.y += headHeight + 2;
            if (EditorGUI.EndChangeCheck() && !inspectableList.IsFixedSize)
                inspectableList.UpdateSourceCollections();
            
            BeginIndentLevel();

            position.x += 15 * EditorGUI.indentLevel;
            position.width -= 15 * EditorGUI.indentLevel;
            
            if (!isInArray)
            {
                if (isExpanded || animFloat.isAnimating)
                {
                    if (animFloat.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + animFloat.value));

                    EditorGUI.BeginChangeCheck();
                    reorderableList.DoList(position);
                    if (EditorGUI.EndChangeCheck() && !inspectableList.IsFixedSize)
                        inspectableList.UpdateSourceCollections();

                    if (animFloat.isAnimating)
                        GUI.EndClip();
                }

                if (animFloat.isAnimating)
                    RepaintCurrentWindow();
            }
            else if (isExpanded)
                reorderableList.DoList(position);
            
            EndIndentLevel();
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
            
            IInspectorListElement? element = inspectableList.GetElement(index, flags);
            if (element == null)
                return null;

            Inspector inspector = elementInspectors.GetOrCreateValue(element);
            if (inspector.element != element || inspector.inspectorFlags != flags)
                inspector.Rebuild(element, flags, true);
                
            return inspector;
        }
    }
}