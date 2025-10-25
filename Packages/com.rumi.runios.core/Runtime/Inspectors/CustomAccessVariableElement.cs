#nullable enable
using System;
using System.Collections.Generic;

namespace RuniOS.Inspectors
{
    /// <summary>
    /// 변수의 읽기/쓰기를 커스텀할 수 있는 인스펙터 요소입니다.<br/>
    /// 변수의 읽기/쓰기를 커스텀할 수 있다는 점을 제외하면 타겟 요소와 동일합니다.
    /// <br/><br/>
    /// 구조체처럼 내부 필드는 읽기 전용이지만 새 구조체를 만드는 것과 같이 값을 바꿀 수는 있을때 사용할 수 있습니다.
    /// </summary>
    public class CustomAccessVariableElement : IInspectorVariableElement
    {
        public delegate object? ReadFunc(IInspectorVariableElement element);
        public delegate void WriteAction(IInspectorVariableElement element, object? value);
        
        public CustomAccessVariableElement(IInspectorVariableElement targetElement, ReadFunc? readEvent = null, WriteAction? writeEvent = null)
        {
            this.targetElement = targetElement;

            this.readEvent = readEvent ?? (x => x.value);
            this.writeEvent = writeEvent ?? ((element, value) => element.value = value);
        }
        
        public IInspectorVariableElement targetElement { get; }

        public string name => targetElement.name;

        public string displayName
        {
            get => targetElement.displayName;
            set => targetElement.displayName = value;
        }

        public IInspectable inspectable => targetElement.inspectable;
        
        public Type variableType => targetElement.variableType;
        
        public RuniNullabilityInfo? nullabilityInfo => targetElement.nullabilityInfo;
        
        public bool isPublic => targetElement.isPublic;
        
        public bool isStatic => targetElement.isStatic;
        
        public object? value
        {
            get => readEvent.Invoke(targetElement);
            set => writeEvent.SafeInvoke(targetElement, value);
        }

        public bool isMixedValue => targetElement.isMixedValue;
        
        public IInspectableObject inspectableObjectElement => targetElement.inspectableObjectElement;
        
        public IInspectableList? inspectableListElement => targetElement.inspectableListElement;
        
        public event ReadFunc readEvent;
        
        public event WriteAction writeEvent;

        public IEnumerable<object?> GetValues() => targetElement.GetValues();
        public void SetValues(IEnumerable<object?> values) => targetElement.SetValues(values);

        public bool HasFlags(InspectorFlags flags) => targetElement.HasFlags(flags);
        
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => targetElement.IsReadable(flags);
        
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => true;

        public void UpdateChildInspectable() => targetElement.UpdateChildInspectable();
    }
}