#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS
{
    public static class TypeUtility
    {
        public static object? GetDefaultValue(this Type type, bool nonPublic = false)
        {
            if (!type.IsValueType)
                return null;

            return Activator.CreateInstance(type, nonPublic);
        }

        public static object GetDefaultValueNotNull(this Type type, bool nonPublic = false)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType() ?? typeof(object), 0);

            return Activator.CreateInstance(type, nonPublic);
        }

        /// <summary>
        /// 주어진 <paramref name="givenType"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
        /// 구현하거나 상속하는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여
        /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
        /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
        /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
        /// </remarks>
        /// <param name="givenType">확인할 대상 <see cref="Type"/>입니다.</param>
        /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
        /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
        /// </exception>
        public static bool IsAssignableToGenericDefinition(this Type givenType, Type genericTypeDefinition) => IsAssignableToGenericDefinition(givenType, genericTypeDefinition, out _);

        /// <summary>
        /// 주어진 <paramref name="givenType"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
        /// 구현하거나 상속하는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여
        /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
        /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
        /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
        /// </remarks>
        /// <param name="givenType">확인할 대상 <see cref="Type"/>입니다.</param>
        /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
        /// <param name="resolvedType">
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하는 경우,
        /// 실제로 발견된 구체적인 제네릭 타입(예: <c>IList&lt;int&gt;</c>)이 반환됩니다.<br/>
        /// 찾지 못한 경우 <see langword="null"/>이 반환됩니다.
        /// <br/><br/>
        /// 인터페이스일 경우엔 찾은 인터페이스 타입을 반환합니다.
        /// </param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
        /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
        /// </exception>
        public static bool IsAssignableToGenericDefinition(this Type givenType, Type genericTypeDefinition, [MaybeNullWhen(false)] out Type resolvedType)
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (givenType == null)
                throw new ArgumentNullException(nameof(givenType), "The given type cannot be null.");
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), "The generic type definition cannot be null.");
            else if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException("The provided genericTypeDefinition must be a valid generic type definition (e.g., typeof(List<>) or typeof(IDictionary<,>)).", nameof(genericTypeDefinition));
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            Type? currentType = givenType;
            while (currentType != null)
            {
                // 인터페이스 확인
                var interfaceTypes = currentType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType && it.GetGenericTypeDefinition() == genericTypeDefinition)
                    {
                        resolvedType = it;
                        return true;
                    }
                }

                // 현재 타입 확인 (직접적인 제네릭 타입 정의 일치)
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    resolvedType = currentType;
                    return true;
                }

                // 기반 클래스 확인
                currentType = currentType.BaseType;
            }

            resolvedType = null;
            return false;
        }

        /// <summary>
        /// 주어진 <paramref name="givenType"/>의 인스턴스가 <paramref name="targetType"/>의 변수에 할당 가능한지 확인합니다.
        /// <paramref name="targetType"/>이 제네릭 타입 정의(<c>List&lt;&gt;</c> 등)인 경우,
        /// <paramref name="givenType"/>이 해당 제네릭 정의를 구현하거나 상속하는지도 함께 검사합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 다음과 같은 경우 <see langword="true"/>를 반환합니다:<br/>
        /// <list type="bullet">
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/>과 동일한 경우.</description></item>
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/>의 서브클래스인 경우.</description></item>
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/> 인터페이스를 구현하는 경우.</description></item>
        ///     <item><description><paramref name="targetType"/>이 제네릭 타입 정의이고, <paramref name="givenType"/>이 해당 정의를 구현하거나 상속하는 경우.</description></item>
        /// </list>
        /// 이 메서드는 <paramref name="targetType"/>이 제네릭 타입 정의인 경우
        /// <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여 일치 여부를 검사합니다.
        /// </remarks>
        /// <param name="givenType">할당될 대상 인스턴스의 <see cref="Type"/>입니다.</param>
        /// <param name="targetType">할당받을 변수의 <see cref="Type"/>입니다. 제네릭 타입 정의일 수 있습니다.</param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="targetType"/>에 할당 가능하면 <see langword="true"/>를 반환하고,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="targetType"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public static bool IsAssignableToAny(this Type givenType, Type targetType) => IsAssignableToAny(givenType, targetType, out _);

        /// <summary>
        /// 주어진 <paramref name="givenType"/>의 인스턴스가 <paramref name="targetType"/>의 변수에 할당 가능한지 확인합니다.
        /// <paramref name="targetType"/>이 제네릭 타입 정의(<c>List&lt;&gt;</c> 등)인 경우,
        /// <paramref name="givenType"/>이 해당 제네릭 정의를 구현하거나 상속하는지도 함께 검사합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 다음과 같은 경우 <see langword="true"/>를 반환합니다:<br/>
        /// <list type="bullet">
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/>과 동일한 경우.</description></item>
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/>의 서브클래스인 경우.</description></item>
        ///     <item><description><paramref name="givenType"/>이 <paramref name="targetType"/> 인터페이스를 구현하는 경우.</description></item>
        ///     <item><description><paramref name="targetType"/>이 제네릭 타입 정의이고, <paramref name="givenType"/>이 해당 정의를 구현하거나 상속하는 경우.</description></item>
        /// </list>
        /// 이 메서드는 <paramref name="targetType"/>이 제네릭 타입 정의인 경우
        /// <paramref name="givenType"/>의 인터페이스 및 상속 계층 구조를 탐색하여 일치 여부를 검사합니다.
        /// </remarks>
        /// <param name="givenType">할당될 대상 인스턴스의 <see cref="Type"/>입니다.</param>
        /// <param name="targetType">할당받을 변수의 <see cref="Type"/>입니다. 제네릭 타입 정의일 수 있습니다.</param>
        /// <param name="resolvedType">
        /// 할당이 가능한 경우, 발견된 구체적인 <see cref="Type"/> (예: <c>List&lt;int&gt;</c>) 또는
        /// <paramref name="targetType"/>이 제네릭 정의가 아닌 경우 <paramref name="targetType"/> 자체가 반환됩니다.
        /// 할당이 불가능한 경우 <see langword="null"/>이 반환됩니다.
        /// </param>
        /// <returns>
        /// <paramref name="givenType"/>이 <paramref name="targetType"/>에 할당 가능하면 <see langword="true"/>를 반환하고,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="givenType"/> 또는 <paramref name="targetType"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public static bool IsAssignableToAny(this Type givenType, Type targetType, [MaybeNullWhen(false)] out Type resolvedType)
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (givenType == null)
                throw new ArgumentNullException(nameof(givenType), "The given type cannot be null.");
            else if (targetType == null)
                throw new ArgumentNullException(nameof(targetType), "The target type cannot be null.");
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            if (targetType == typeof(object))
            {
                resolvedType = targetType;
                return true;
            }

            if (targetType.IsGenericTypeDefinition)
                return givenType.IsAssignableToGenericDefinition(targetType, out resolvedType);
            else if (targetType.IsAssignableFrom(givenType))
            {
                resolvedType = targetType;
                return true;
            }

            resolvedType = null;
            return false;
        }

        /// <summary>
        /// 지정된 타입의 상위 계층(상속 체인)을 열거합니다.<br/>
        /// 이 메서드는 현재 타입부터 시작하여 <see cref="object"/> 타입까지 모든 기본 타입을 반환합니다.
        /// </summary>
        /// <param name="type">상위 계층을 가져올 시작 <see cref="Type"/>입니다.</param>
        /// <returns>
        /// 지정된 타입과 그 상위 기본 타입을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="Type"/> 컬렉션입니다.<br/>
        /// 만약 <paramref name="type"/>이 null인 경우 빈 컬렉션을 반환합니다.
        /// </returns>
        public static IEnumerable<Type> GetHierarchy(this Type? type)
        {
            for (; type != null; type = type.BaseType)
                yield return type;
        }

        public static string GetTypeDisplayName(this Type type) => Unity.Properties.TypeUtility.GetTypeDisplayName(type);

        public static string SerializeToString(this Type type) => type.FullName + ", " + type.Assembly.GetName().Name;
        public static Type? DeserializeFromString(string typeName) => Type.GetType(typeName);



        /// <summary>
        /// 지정된 <see cref="Type"/>이 <see cref="int"/> 타입으로 할당(변환) 가능한 숫자 타입인지 확인합니다.
        /// </summary>
        /// <param name="type">확인할 <see cref="Type"/>입니다.</param>
        /// <returns>
        /// 해당 타입의 값을 <see cref="int"/>(으)로 변환할 때 오버플로우가 발생하지 않으면 <see langword="true"/>,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsAssignableToInt(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                _ => false,
            };
        }

        /// <summary>
        /// 지정된 <see cref="Type"/>이 <see cref="long"/> 타입으로 할당(변환) 가능한 숫자 타입인지 확인합니다.
        /// </summary>
        /// <param name="type">확인할 <see cref="Type"/>입니다.</param>
        /// <returns>
        /// 해당 타입의 값을 <see cref="long"/>(으)로 변환할 때 오버플로우가 발생하지 않으면 <see langword="true"/>,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsAssignableToLong(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                _ => false,
            };
        }

        /// <summary>
        /// 지정된 <see cref="Type"/>이 <see cref="float"/> 타입으로 할당(변환) 가능한 숫자 타입인지 확인합니다.<br/>
        /// 여기서 '할당 가능'은 오버플로우 없이 변환이 가능함을 의미하며,
        /// <see cref="decimal"/>과 같이 명시적 캐스팅을 통해 정밀도 손실이 발생할 수 있는 경우도 포함합니다.
        /// </summary>
        /// <param name="type">확인할 <see cref="Type"/>입니다.</param>
        /// <returns>
        /// 해당 타입의 값을 <see cref="float"/>(으)로 변환할 때 오버플로우가 발생하지 않으면 <see langword="true"/>,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsAssignableToFloat(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                TypeCode.UInt64 => true,
                TypeCode.Single => true,
                TypeCode.Decimal => true,
                _ => false,
            };
        }

        /// <summary>
        /// 지정된 <see cref="Type"/>이 <see cref="double"/> 타입으로 할당(변환) 가능한 숫자 타입인지 확인합니다.<br/>
        /// 여기서 '할당 가능'은 오버플로우 없이 변환이 가능함을 의미하며,
        /// <see cref="decimal"/>과 같이 명시적 캐스팅을 통해 정밀도 손실이 발생할 수 있는 경우도 포함합니다.
        /// </summary>
        /// <param name="type">확인할 <see cref="Type"/>입니다.</param>
        /// <returns>
        /// 해당 타입의 값을 <see cref="double"/>(으)로 변환할 때 오버플로우가 발생하지 않으면 <see langword="true"/>,
        /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsAssignableToDouble(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                TypeCode.UInt64 => true,
                TypeCode.Single => true,
                TypeCode.Double => true,
                TypeCode.Decimal => true,
                _ => false,
            };
        }

        public static bool IsInteger(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                TypeCode.UInt64 => true,
                _ => false,
            };
        }

        public static bool IsFractional(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Single => true,
                TypeCode.Double => true,
                TypeCode.Decimal => true,
                _ => false,
            };
        }

        public static bool IsNumeric(this Type type) => type.IsInteger() || type.IsFractional();
        public static bool IsText(this Type type) => type == typeof(char) || type == typeof(string);
        public static bool IsTextField(this Type type) => type.IsNumeric() || type.IsText();



        /// <summary>
        /// 주어진 타입의 최소값을 가져옵니다.
        /// </summary>
        /// <param name="type">최소값을 가져올 타입입니다.</param>
        /// <returns>타입의 최소값입니다.</returns>
        /// <exception cref="ArgumentException">지원하지 않는 타입인 경우 발생합니다.</exception>
        public static object GetMinValue(this Type type)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => sbyte.MinValue,
                TypeCode.Byte => byte.MinValue,
                TypeCode.Int16 => short.MinValue,
                TypeCode.UInt16 => ushort.MinValue,
                TypeCode.Int32 => int.MinValue,
                TypeCode.UInt32 => uint.MinValue,
                TypeCode.Int64 => long.MinValue,
                TypeCode.UInt64 => ulong.MinValue,
                TypeCode.Single => float.MinValue,
                TypeCode.Double => double.MinValue,
                TypeCode.Decimal => decimal.MinValue,
                _ => throw new ArgumentException(),
            };
        }

        /// <summary>
        /// 주어진 타입의 최대값을 가져옵니다.
        /// </summary>
        /// <param name="type">최대값을 가져올 타입입니다.</param>
        /// <returns>타입의 최대값입니다.</returns>
        /// <exception cref="ArgumentException">지원하지 않는 타입인 경우 발생합니다.</exception>
        public static object GetMaxValue(this Type type)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => sbyte.MaxValue,
                TypeCode.Byte => byte.MaxValue,
                TypeCode.Int16 => short.MaxValue,
                TypeCode.UInt16 => ushort.MaxValue,
                TypeCode.Int32 => int.MaxValue,
                TypeCode.UInt32 => uint.MaxValue,
                TypeCode.Int64 => long.MaxValue,
                TypeCode.UInt64 => ulong.MaxValue,
                TypeCode.Single => float.MaxValue,
                TypeCode.Double => double.MaxValue,
                TypeCode.Decimal => decimal.MaxValue,
                _ => throw new ArgumentException(),
            };
        }
    }
}