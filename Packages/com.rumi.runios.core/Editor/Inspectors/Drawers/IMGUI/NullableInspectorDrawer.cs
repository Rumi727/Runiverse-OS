#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Nullable<>))]
    [CustomInspectorDrawer(typeof(ISerializableNullable<>))]
    public class NullableInspectorDrawer : IMGUIInspectorDrawer
    {
        public NullableInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector)
        {
            CheckVariableElement();
            
            valueElement = element.inspectableObjectElement.GetElements(InspectorFlags.All)
                .Where(x => x.name == nameof(Nullable<int>.Value))
                .OfType<IInspectorVariableElement>()
                .First();

            valueElement = new CustomAccessVariableElement(valueElement, null, (_, value) => variableElement.value = Activator.CreateInstance(variableElement.variableType, value));
            
            hasValueElement = element.inspectableObjectElement.GetElements(InspectorFlags.All)
                .Where(x => x.name == nameof(Nullable<int>.HasValue))
                .OfType<IInspectorVariableElement>()
                .First();
            
            hasValueElement = new CustomAccessVariableElement
            (
                hasValueElement,
                // 닷넷의 Nullable<T>를 null로 만들면 구조체이지만 Nullable<T>의 Equals(null)가 true가 되면서 Nullable<T> 인스턴스를 가져오지 못하는 현상이 있습니다.
                x => !x.inspectable.instancesIsEmpty && (bool)x.value!,
                (_, value) =>
                {
                    if (!Equals((bool)hasValueElement.value!, value))
                    {
                        if ((bool)value!)
                            variableElement.value = Activator.CreateInstance(variableElement.variableType, valueElement.variableType.GetDefaultValueNotNull());
                        else
                            variableElement.value = null;
                    }
                }
            );
            
            valueInspector = new Inspector();
            valueInspector.Rebuild(valueElement, InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.Property);
        }

        public string? nullText { get; set; } = null;
        
        public IInspectorVariableElement hasValueElement { get; }
        public IInspectorVariableElement valueElement { get; }

        readonly Inspector valueInspector;
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.None | InspectorFlags.Public | InspectorFlags.Static | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.WriteOnly | InspectorFlags.PublicAccess | InspectorFlags.Property | InspectorFlags.Event | InspectorFlags.Field | InspectorFlags.Method | InspectorFlags.Variable | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            CheckVariableElement();

            Type? underlyingType = variableElement.variableType.GetNullableUnderlyingType();
            if (underlyingType == null)
                throw new InvalidOperationException("It is not of type System.Nullable<T>.");
            
            label ??= GUIContent.none;
            
            float fieldWidth = position.width;
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (fieldWidth - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);
            
            /*if (underlyingType.IsPrimitive)
            {
                if (underlyingType.IsTextField())
                {
                    Rect fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
                    if (toggleRect.Contains(Event.current.mousePosition))
                    {
                        fieldRect = GetPrefixLabelRect(fieldRect, label, out Rect? labelPosition);

                        if (labelPosition != null)
                        {
                            BeginIndentLevel(0);
                            EditorGUI.LabelField(labelPosition.Value, label);
                            EndIndentLevel();
                        }

                        if (value != null)
                            GUI.Box(fieldRect, valueElement.value!.ToString(), EditorStyles.textField);
                        else
                            GUI.Box(fieldRect, nullText, EditorStyles.textField);
                    }
                    else if (value == null)
                    {
                        if (underlyingType.IsText())
                            value = Convert.ChangeType(EditorGUI.TextField(fieldRect, label, nullText), underlyingType);
                        else
                        {
                            value = PrimitiveField(fieldRect, label, underlyingType.GetDefaultValueNotNull());

                            if (!EditorGUIBridge.HasKeyboardFocus(EditorGUIUtilityBridge.s_LastControlID))
                                GUI.Box(GetPrefixLabelRect(fieldRect, label, out _), nullText, EditorStyles.textField);
                            else
                                GUI.Box(Rect.zero, GUIContent.none);
                        }
                    }
                    else
                        value = PrimitiveField(fieldRect, label, valueElement.value!);

                    value = InternalNullableToggleField(value, toggleRect);
                }
                else
                {
                    value = InternalNullableToggleField(value, toggleRect);

                    if (value != null)
                        value = PrimitiveField(position, label, valueElement.value!);
                    else
                        EditorGUI.LabelField(position, label, new GUIContent(nullText));
                }
            }
            else*/
            {
                position.width -= toggleWidth + 4;
                
                EditorGUI.BeginChangeCheck();
                BeginIndentLevel(0);
                bool hasValue = EditorGUI.Toggle(toggleRect, (bool)hasValueElement.value!);
                EndIndentLevel();
                if (EditorGUI.EndChangeCheck())
                    hasValueElement.value = hasValue;
                
                if (hasValue)
                    valueInspector.Draw(position, label, isInArray);
                else
                    EditorGUI.LabelField(position, label, new GUIContent(nullText ?? $"null ({valueElement.variableType.GetTypeDisplayName()})"));
            }
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => (bool)hasValueElement.value! ? valueInspector.GetHeight(label, flags, isInArray) : EditorGUIUtility.singleLineHeight;
    }
}