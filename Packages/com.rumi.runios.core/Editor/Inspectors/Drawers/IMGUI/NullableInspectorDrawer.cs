#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
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
            valueElement = element.inspectableObjectElement.GetElements(InspectorFlags.Public | InspectorFlags.NonPublic | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.Property)
                .Where(x => x.name == nameof(Nullable<int>.Value))
                .OfType<IInspectorVariableElement>()
                .First();
            
            valueElement = new CustomAccessVariableElement.Builder(valueElement)
                .AddWriteAction((_, value) => element.value = Activator.CreateInstance(element.variableType, value))
                .AddSetValuesAction((_, values) => element.SetValues(values.Select(x => Activator.CreateInstance(element.variableType, x))))
                .SetIsWritableFunc((_, flags) => element.IsWritable(flags))
                .Build();
            
            hasValueElement = element.inspectableObjectElement.GetElements(InspectorFlags.Public | InspectorFlags.NonPublic | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.Property)
                .Where(x => x.name == nameof(Nullable<int>.HasValue))
                .OfType<IInspectorVariableElement>()
                .First();
            
            hasValueElement = new CustomAccessVariableElement.Builder(hasValueElement)
                // 닷넷의 Nullable<T>를 null로 만들면 구조체이지만 Nullable<T>의 Equals(null)가 true가 되면서 Nullable<T> 인스턴스를 가져오지 못하는 현상이 있습니다.
                .SetReadFunc(x => !x.inspectable.instancesIsEmpty && (bool)x.value!)
                .AddWriteAction((_, value) =>
                {
                    if (Equals(hasValueElement.value, value))
                        return;
                    
                    if ((bool)value!)
                        element.value = Activator.CreateInstance(element.variableType, valueElement.variableType.GetDefaultValueNotNull());
                    else
                        element.value = null;
                })
                .AddSetValuesAction((_, values) =>
                {
                    element.SetValues(values.Select(x =>
                    {
                        if (Equals(hasValueElement.value, x))
                            return x;
                        
                        if ((bool)x!)
                            return Activator.CreateInstance(element.variableType, valueElement.variableType.GetDefaultValueNotNull());
                        else
                            return null;
                    }));
                })
                .SetIsWritableFunc((_, flags) => element.IsWritable(flags))
                .Build();
            
            valueInspector = new Inspector();
        }

        public string? nullText { get; set; } = null;
        
        public IInspectorVariableElement hasValueElement { get; }
        public IInspectorVariableElement valueElement { get; }

        readonly Inspector valueInspector;
        readonly AnimFloat nullableAnimFloat = new AnimFloat(1);
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.None | InspectorFlags.Public | InspectorFlags.Static | InspectorFlags.Instance | InspectorFlags.ReadOnly | InspectorFlags.WriteOnly | InspectorFlags.PublicAccess | InspectorFlags.Property | InspectorFlags.Event | InspectorFlags.Field | InspectorFlags.Method | InspectorFlags.Variable | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            // TODO : NullToggleField로 통합해서 Read-Only, Write-Only 버그 고쳐라
            CheckVariableElement();

            Type? underlyingType = variableElement.variableType.GetNullableUnderlyingType();
            if (underlyingType == null)
                throw new InvalidOperationException("It is not a nullable type.");
            
            label ??= GUIContent.none;
            
            if (valueInspector.elements.FirstOrDefault() != valueElement || valueInspector.inspectorFlags != flags)
                valueInspector.Rebuild(valueElement, flags, true);
            
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (position.width - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);
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
            {
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label, new GUIContent(nullText ?? $"null ({valueElement.variableType.GetTypeDisplayName()})"));
            }
        }

        float lastInspectorHeight;
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            float height = valueInspector.GetHeight(label, flags, isInArray);
            bool valueIsNull = !(bool)hasValueElement.value!;
            nullableAnimFloat.target = valueIsNull ? 1 : 0;

            if (!isInArray && nullableAnimFloat.isAnimating)
            {
                RepaintCurrentWindow();
                return height.Lerp(EditorGUIUtility.singleLineHeight, nullableAnimFloat.value);
            }

            return !valueIsNull ? valueInspector.GetHeight(label, flags, isInArray) : EditorGUIUtility.singleLineHeight;
        }
    }
}