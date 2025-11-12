#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.Serialization;
using RuniOS.Editor.UIElements;
using RuniOS.Inspectors;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace RuniOS.Editor.Inspectors.Unity
{
    /// <summary>
    /// 직렬화된 프로퍼티를 나타내는 인스펙터 요소입니다.
    /// </summary>
    public class SerializedPropertyElement : IInspectorVariableElement, IInspectorSerializedPropertyElement
    {
        /// <summary>
        /// <see cref="SerializedPropertyElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="property">이 요소가 나타내는 직렬화된 프로퍼티입니다.</param>
        public SerializedPropertyElement(SerializedProperty property)
        {
            displayName = property.GetFieldLabel();
            
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(property, out Type elementType);

            inspectable = new InspectableSerializedObject(property.serializedObject);
            
            this.property = property;
            converter = PropertyConverter.FindConverter(elementType);

            variableType = elementType;

            inspectableObjectElement = new InspectableSerializedObject(property.serializedObject, property);
            if (property.propertyType != SerializedPropertyType.String && property.isArray)
                inspectableListElement = new InspectableSerializedList(property);
        }

        /// <summary>
        /// 이 프로퍼티가 속한 검사 가능한 직렬화된 객체를 가져옵니다.
        /// </summary>
        public InspectableSerializedObject inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        /// <summary>
        /// 이 요소가 나타내는 <see cref="SerializedProperty"/>를 가져옵니다.
        /// </summary>
        public SerializedProperty property { get; }
        /// <summary>
        /// 프로퍼티 값을 변환하는 데 사용되는 <see cref="PropertyConverter"/>를 가져옵니다.
        /// </summary>
        public PropertyConverter? converter { get; }

        /// <summary>
        /// 프로퍼티의 이름을 가져옵니다.
        /// </summary>
        public string name => property.name;
        
        /// <summary>
        /// 프로퍼티의 디스플레이 이름을 가져옵니다.
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// 변수의 타입을 가져옵니다.
        /// </summary>
        public Type variableType { get; }
        /// <summary>
        /// 변수의 null 허용 여부 정보를 가져옵니다. 직렬화된 프로퍼티의 경우 항상 null을 반환합니다.
        /// </summary>
        public RuniNullabilityInfo? nullabilityInfo => null;
        
        /// <summary>
        /// 변수가 공개되어있는지 여부를 나타내는 값을 가져옵니다. 직렬화된 프로퍼티의 경우 항상 true입니다.
        /// </summary>
        public bool isPublic => true;

        /// <summary>
        /// 변수가 정적인지 여부를 나타내는 값을 가져옵니다. 직렬화된 프로퍼티의 경우 항상 false입니다.
        /// </summary>
        public bool isStatic => false;

        /// <summary>
        /// 변수의 값을 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="InspectorElementException">값을 가져오거나 설정하는 데 필요한 변환기가 설정되지 않은 경우 발생합니다.</exception>
        public object? value
        {
            get
            {
                if (converter == null)
                    throw new InspectorElementException($"Cannot get value for property '{name}' because the converter is not set.", name);

                return converter.Read(property, variableType);
            }
            set
            {
                if (converter == null)
                    throw new InspectorElementException($"Cannot convert value for property '{name}' because the converter is not set.", name);

                converter.Write(property, variableType, value);
            }
        }

        /// <summary>
        /// 프로퍼티가 여러 다른 값을 가지고 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isMixedValue => property.hasMultipleDifferentValues;

        /// <summary>
        /// 이 프로퍼티의 값을 나타내는 검사 가능한 객체를 가져옵니다.
        /// </summary>
        public IInspectableObject inspectableObjectElement { get; }
        
        /// <summary>
        /// 이 프로퍼티가 리스트인 경우, 검사 가능한 리스트를 가져옵니다.
        /// </summary>
        public IInspectableList? inspectableListElement { get; }

        /// <summary>
        /// 이 필드가 딕셔너리인 경우, 딕셔너리를 나타내는 <see cref="IInspectableDictionary"/>를 가져옵니다.
        /// </summary>
        public IInspectableDictionary? inspectableDictionaryElement => null;

        /// <summary>
        /// 모든 대상 객체에서 값을 가져옵니다. 직렬화된 프로퍼티에 대해서는 지원되지 않습니다.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">직렬화된 프로퍼티에 대해 항상 발생합니다.</exception>
        public IEnumerable<object?> GetValues() => throw new NotSupportedException("Fetching values for all target objects is not supported for serialized properties.");
        
        public void SetValues(IEnumerable<object?> values) => throw new NotSupportedException("Writing values for all target objects is not supported for serialized properties.");

        public bool HasFlags(InspectorFlags flags) => flags != InspectorFlags.None && flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.Field);
        
        /// <summary>
        /// 변수를 읽을 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => true;
        
        /// <summary>
        /// 변수에 쓸 수 있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => true;
        
        public void UpdateChildInspectable() { }
    }
}