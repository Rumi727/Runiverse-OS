#nullable enable
/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Inspectors.Csharp
{
    /// <summary>
    /// 멤버에 속해있지 않은 인스턴스를 나타내는 인스펙터 요소입니다.
    /// </summary>
    public class InstanceElement : IInspectorVariableElement
    {
        public InstanceElement(IInspectorVariableElement targetElement)
        {
            this.targetElement = targetElement;
            this.inspectable = inspectable;

            variableType = targetElement.type;
            nullabilityInfo = targetElement.nullabilityInfo;
            
            _inspectableObjectElement = new InspectableObject(variableType, this);

            if (typeof(IList).IsAssignableFrom(variableType))
                _inspectableList = new InspectableList(variableType, nullabilityInfo?.genericTypeArguments.FirstOrDefault());

            isReadOnly = targetElement.isReadOnly;
        }
        
        public IInspectorVariableElement targetElement { get; }

        public string name => targetElement.name;

        public string displayName
        {
            get => targetElement.displayName;
            set => targetElement.displayName = value;
        }

        public IInspectable inspectable => targetElement.inspectable;

        /// <summary>
        /// 필드의 타입을 가져옵니다.
        /// </summary>
        public Type variableType => targetElement.variableType;

        /// <summary>
        /// 필드의 null 허용 여부 정보를 가져옵니다.
        /// </summary>
        public RuniNullabilityInfo? nullabilityInfo => targetElement.nullabilityInfo;
        
        public bool isPublic => targetElement.isPublic;
        
        public bool isStatic => targetElement.isStatic;

        public object? value
        {
            get => targetElement.value;
            set
            {
                if (targetElement.IsWritable(InspectorFlags.All))
                    targetElement.value = value;
            }
        }

        public bool isMixedValue => targetElement.isMixedValue;
        
        public IInspectableObject inspectableObjectElement => targetElement.inspectableObjectElement;

        public IInspectableList? inspectableElementList => targetElement.inspectableListElement;

        public IEnumerable<object?> GetValues() => targetElement.GetValues();

        public bool HasFlags(InspectorFlags flags) => targetElement.HasFlags(flags);
        
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => targetElement.IsReadable(flags);
        
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => true;
    }
}*/