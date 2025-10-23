#nullable enable
using RuniOS.Inspectors;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace RuniOS.Editor.Inspectors.Unity
{
    /// <summary>
    /// 직렬화된 프로퍼티의 리스트 요소를 나타내는 인스펙터 요소 클래스입니다.
    /// </summary>
    public class SerializedListElement : IInspectorListElement
    {
        /// <summary>
        /// <see cref="SerializedListElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="inspectable">이 요소가 속한 검사 가능한 리스트입니다.</param>
        /// <param name="property">이 요소에 대한 직렬화된 프로퍼티입니다.</param>
        /// <param name="index">리스트에서 이 요소의 인덱스입니다.</param>
        public SerializedListElement(InspectableSerializedList inspectable, SerializedProperty property, int index)
        {
            displayName = name;
            
            this.inspectable = inspectable;

            this.property = property;
            this.index = index;

            inspectableObjectElement = new InspectableSerializedObject(property.serializedObject, property);
            if (property.isArray)
                inspectableListElement = new InspectableSerializedList(property);
        }

        /// <summary>
        /// 요소의 이름을 가져옵니다.
        /// </summary>
        public string name => property.name;
        
        /// <summary>
        /// 요소의 디스플레이 이름을 가져옵니다.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// 이 요소가 속한 검사 가능한 직렬화된 리스트를 가져옵니다.
        /// </summary>
        public InspectableSerializedList inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        /// <summary>
        /// 변수의 타입을 가져옵니다.
        /// </summary>
        public Type variableType => inspectable.inspectionType;

        /// <summary>
        /// 요소의 타입을 가져옵니다.
        /// </summary>
        public Type elementType => inspectable.inspectionElementType;

        /// <summary>
        /// 변수의 null 허용 여부 정보를 가져옵니다. 직렬화된 프로퍼티의 경우 null을 반환합니다.
        /// </summary>
        public NullabilityInfo? nullabilityInfo => null;

        /// <summary>
        /// 직렬화된 프로퍼티를 가져옵니다.
        /// </summary>
        public SerializedProperty property { get; }

        /// <summary>
        /// 리스트에 있는 요소의 인덱스를 가져옵니다.
        /// </summary>
        public int index { get; }

        /// <summary>
        /// 변수가 공개되어있는지 여부를 나타내는 값을 가져옵니다. 직렬화된 프로퍼티의 경우 항상 true입니다.
        /// </summary>
        public bool isPublic => true;
        
        /// <summary>
        /// 변수가 정적인지 여부를 나타내는 값을 가져옵니다. 직렬화된 프로퍼티의 경우 항상 false입니다.
        /// </summary>
        public bool isStatic => false;

        /// <summary>
        /// 변수를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isReadable => true;
        
        /// <summary>
        /// 변수에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isWritable => true;

        /// <summary>
        /// 변수의 값을 가져오거나 설정합니다.
        /// </summary>
        public object? value
        {
            get
            {
                try
                {
                    return inspectable[index];
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
                }
            }
            set
            {
                try
                {
                    inspectable[index] = value;
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while writing a value to the {name} field.", name, e);
                }
            }
        }

        /// <summary>
        /// 프로퍼티가 여러 다른 값을 가지고 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isMixedValue
        {
            get
            {
                try
                {
                    return property.hasMultipleDifferentValues;
                }
                catch (Exception e)
                {
                    throw new InspectorElementException($"An exception occurred while reading value from {name} field.", name, e);
                }
            }
        }

        /// <summary>
        /// 이 요소에 대한 검사 가능한 객체를 가져옵니다.
        /// </summary>
        public IInspectableObject inspectableObjectElement { get; }
        
        /// <summary>
        /// 이 요소가 배열이나 리스트인 경우, 검사 가능한 리스트를 가져옵니다.
        /// </summary>
        public IInspectableList? inspectableListElement { get; }

        /// <summary>
        /// 모든 대상 객체에서 값을 가져옵니다. 직렬화된 프로퍼티에 대해서는 지원되지 않습니다.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">직렬화된 프로퍼티에 대해 항상 발생합니다.</exception>
        public IEnumerable<object?> GetValues() => throw new NotSupportedException("Fetching values for all target objects is not supported for serialized properties.");

        public bool HasFlags(InspectorFlags flags) => flags != InspectorFlags.None && flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.List);
    }
}