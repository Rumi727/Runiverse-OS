#nullable enable
using RuniOS.APIBridge.UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace RuniOS.Editor.Serialization
{
    /// <summary>
    /// Provides an abstract base class for converting <see cref="SerializedProperty">serialized properties</see> to and from instances. 
    /// Derived classes define how to read and write property values for specific types.
    /// <br/>
    /// <see cref="SerializedProperty">직렬화된 속성</see>과 인스턴스를 서로 변환하기 위한 추상적인 기본 클래스를 제공합니다.
    /// 파생 클래스는 특정 타입에 대한 속성 값을 읽고 쓰는 방법을 정의합니다.
    /// </summary>
    public abstract class PropertyConverter
    {
        /// <summary>
        /// Gets a read-only list of all discovered <see cref="PropertyConverter"/> types and their associated <see cref="PropertyConverter">CustomPropertyConverterAttributes</see>.
        /// <br/>
        /// The list is ordered by the hierarchy depth of the target type in descending order, ensuring that more specific converters are prioritized.
        /// <br/><br/>
        /// 발견된 모든 <see cref="CustomPropertyConverterAttribute"/> 타입의 읽기 전용 목록을 가져옵니다.
        /// <br/>
        /// 이 목록은 대상 타입의 계층 깊이(내림차순)에 따라 정렬되어, 더 구체적인 컨버터가 우선적으로 처리되도록 합니다.
        /// </summary>
        public static IReadOnlyList<(Type type, CustomPropertyConverterAttribute attribute)> propertyConverterTypes { get; } = ReflectionUtility.types
        .Where
        (
            static x =>
                x.AttributeContains(typeof(CustomPropertyConverterAttribute)) &&
                x.IsSubclassOf(typeof(PropertyConverter)) &&
                x.HasDefaultConstructor()
        )
        .Select(static x => (x, x.GetCustomAttribute<CustomPropertyConverterAttribute>()))
        .OrderByDescending
        (
            static x =>
            {
                if (x.Item2.priority == 0)
                {
                    if (x.Item2.targetType.IsInterface)
                        return 0;
                    
                    return x.Item2.targetType.GetHierarchy().Count() * 100;
                }
                else
                    return x.Item2.priority;
            }
        ).ToArray().AsReadOnly();



        public static PropertyConverter? FindConverter<T>() => FindConverter(typeof(T));

        public static PropertyConverter? FindConverter(SerializedProperty property)
        {
            if (ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(property, out Type type) != null)
                return FindConverter(type);

            return null;
        }
        
        public static PropertyConverter? FindConverter(Type propertyType)
        {
            // 해당 propertyType에 맞는 PropertyConverter를 찾습니다.
            PropertyConverter? converter = null;
            foreach ((Type type, CustomPropertyConverterAttribute attribute) in propertyConverterTypes)
            {
                // propertyType이 컨버터의 targetType과 정확히 일치하거나,
                // 컨버터가 하위 타입 호환성을 지원하고 propertyType이 targetType에 할당 가능한 경우
                if (propertyType == attribute.targetType || (attribute.isSubtypeCompatible && propertyType.IsAssignableToAny(attribute.targetType)))
                {
                    // 해당 PropertyConverter 인스턴스를 생성하고 루프를 종료합니다 (가장 적합한 컨버터 선택).
                    converter = (PropertyConverter)Activator.CreateInstance(type);
                    break;
                }
            }

            return converter;
        }



        /// <summary>
        /// When overridden in a derived class, reads the value from the specified <see cref="SerializedProperty"/> and returns it as an object.
        /// <br/><br/>
        /// 파생 클래스에서 재정의될 때, 지정된 <see cref="SerializedProperty"/>에서 값을 읽어 객체로 반환합니다.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to read from.<br/>값을 읽어올 <see cref="SerializedProperty"/>입니다.</param>
        /// <param name="propertyType">The expected <see cref="Type"/> of the property value.<br/>속성 값의 예상 <see cref="Type"/>입니다.</param>
        /// <returns>The read value from the property, or <see langword="null"/> if the value cannot be read.<br/>속성에서 읽은 값, 또는 값을 읽을 수 없는 경우 <see langword="null"/>입니다.</returns>
        /// <exception cref="Exception">A derived class might throw exceptions based on specific reading logic (e.g., if the property is not in the expected format).<br/>파생 클래스는 특정 읽기 로직에 따라 예외를 발생시킬 수 있습니다 (예: 속성이 예상 형식과 다른 경우).</exception>
        public abstract object? Read(SerializedProperty property, Type propertyType);

        /// <summary>
        /// When overridden in a derived class, writes the specified value to the <see cref="SerializedProperty"/>.
        /// <br/><br/>
        /// 파생 클래스에서 재정의될 때, 지정된 값을 <see cref="SerializedProperty"/>에 씁니다.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to write to.<br/>값을 쓸 <see cref="SerializedProperty"/>입니다.</param>
        /// <param name="propertyType">The <see cref="Type"/> of the property value to write.<br/>쓸 속성 값의 <see cref="Type"/>입니다.</param>
        /// <param name="value">The object value to write to the property.<br/>속성에 쓸 객체 값입니다.</param>
        /// <exception cref="Exception">A derived class might throw exceptions based on specific writing logic (e.g., if the value is not compatible with the property type).<br/>파생 클래스는 특정 쓰기 로직에 따라 예외를 발생시킬 수 있습니다 (예: 값이 속성 타입과 호환되지 않는 경우).</exception>
        public abstract void Write(SerializedProperty property, Type propertyType, object? value);

        /// <summary>
        /// When overridden in a derived class, compares two values for equality.
        /// <br/>
        /// The default implementation uses <see cref="object.Equals(object?, object?)"/>.
        /// <br/><br/>
        /// 파생 클래스에서 재정의될 때, 두 값의 동등성을 비교합니다.
        /// <br/>
        /// 기본 구현은 <see cref="object.Equals(object?, object?)"/>를 사용합니다.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> being compared.<br/>비교 대상인 <see cref="SerializedProperty"/>입니다.</param>
        /// <param name="propertyType">The <see cref="Type"/> of the property values.<br/>속성 값의 <see cref="Type"/>입니다.</param>
        /// <param name="current">The current value.<br/>현재 값입니다.</param>
        /// <param name="valueToCompare">The value to compare against the current value.<br/>현재 값과 비교할 값입니다.</param>
        /// <returns><see langword="true"/> if the values are considered equal; otherwise, <see langword="false"/>.<br/>두 값이 같다고 간주되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public virtual bool Comparer(SerializedProperty property, Type propertyType, object? current, object? valueToCompare) => Equals(current, valueToCompare);
    }
}