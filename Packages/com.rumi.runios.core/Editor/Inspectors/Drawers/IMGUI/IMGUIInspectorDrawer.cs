#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class IMGUIInspectorDrawer : InspectorDrawer
    {
        public static IMGUIInspectorDrawer? FindDrawer(IInspectorVariableElement? element, Inspector? rootInspector = null)
        {
            if (element == null)
                return null;
            
            Type? type = AttributeDrawer<IMGUIInspectorDrawer, CustomInspectorDrawerAttribute>.FindDrawerType(element.variableType);
            if (type == null)
                return null;
            
            return (IMGUIInspectorDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new object?[] { element, rootInspector }, null);
        }
        
        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector? rootInspector { get; }
        
        /// <summary>
        /// UI 요소를 렌더링합니다.
        /// </summary>
        public abstract void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false);

        public virtual float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => EditorGUIUtility.singleLineHeight;

        protected static bool NullToggleField(IInspectorVariableElement variableElement, Rect position, out Rect resultPosition, GUIContent? label, InspectorFlags flags)
        {
            resultPosition = position;
            if (variableElement.variableType.IsValueType)
                return false;
            
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (position.width - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            using (new EditorGUI.DisabledScope(!variableElement.IsWritable(flags)))
            {
                if (!variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags))
                {
                    bool valueIsNull = variableElement.value.IsNull();
                    if (valueIsNull || variableElement.nullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.BeginDisabledGroup(valueIsNull && !variableElement.variableType.HasDefaultConstructor());
                        
                        BeginIndentLevel(0);
                        bool toggleValue = EditorGUI.Toggle(toggleRect, !valueIsNull);
                        EndIndentLevel();
                        
                        EditorGUI.EndDisabledGroup();
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = toggleValue ? variableElement.variableType.GetDefaultValueNotNull() : null;
                    }

                    position.width -= toggleRect.width + 4;
                    resultPosition = position;

                    if (valueIsNull)
                    {
                        position.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.LabelField(position, label ?? GUIContent.none, new GUIContent($"null ({variableElement.variableType.GetTypeDisplayName()})"));
                        
                        return true;
                    }
                }
                else
                {
                    BeginIndentLevel(0);
                    
                    if (variableElement.nullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.Toggle(toggleRect, false);
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = null;
                    }
                    
                    toggleRect.x -= toggleRect.width + 2;
                    position.width -= toggleRect.width + 2;

                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.BeginDisabledGroup(!variableElement.variableType.HasDefaultConstructor());
                        
                        EditorGUI.Toggle(toggleRect, true);
                        
                        EditorGUI.EndDisabledGroup();
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = variableElement.variableType.GetDefaultValueNotNull();

                        position.width -= toggleRect.width + 2;
                    }
                    
                    EndIndentLevel();
                }
            }

            resultPosition = position;
            return false;
        }

        protected IMGUIInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element) => this.rootInspector = rootInspector;
        protected IMGUIInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList) => this.rootInspector = rootInspector;
    }
}