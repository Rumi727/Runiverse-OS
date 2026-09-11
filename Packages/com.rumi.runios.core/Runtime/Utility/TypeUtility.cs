#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RuniOS.Utility
{
    public static class TypeUtility
    {
        extension(Enum value)
        {
            public bool IsFlags => value.GetType().IsDefined(typeof(FlagsAttribute), false);
        }

        /// <param name="type">확인할 대상 <see cref="Type"/>입니다.</param>
        extension(Type type)
        {
            public bool IsCompilerGenerated => type.IsDefined(typeof(CompilerGeneratedAttribute), true);

            public bool IsInteger => Type.GetTypeCode(type) switch
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
            } && !type.IsEnum;

            public bool IsFractional => Type.GetTypeCode(type) switch
            {
                TypeCode.Single => true,
                TypeCode.Double => true,
                TypeCode.Decimal => true,
                _ => false,
            };

            public bool IsNumeric => type.IsInteger || type.IsFractional;
            public bool IsText => type == typeof(char) || type == typeof(string);
            public bool IsTextOrNumeric => type.IsNumeric || type.IsText;
            public bool IsEnum => type.IsSubclassOf(typeof(Enum));

            /// <summary>
            /// 이 타입의 모든 값을 <see cref="int"/>에서 정밀도 및 값 손실 없이 표현할 수 있는지 확인합니다.
            /// </summary>
            public bool IsExactlyRepresentableAsInt32 => Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                _ => false,
            };

            /// <summary>
            /// 이 타입의 모든 값을 <see cref="long"/>에서 정밀도 및 값 손실 없이 표현할 수 있는지 확인합니다.
            /// </summary>
            public bool IsExactlyRepresentableAsInt64 => Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                _ => false,
            };

            /// <summary>
            /// 이 타입의 모든 값을 <see cref="float"/>에서 정밀도 및 값 손실 없이 표현할 수 있는지 확인합니다.
            /// </summary>
            public bool IsExactlyRepresentableAsSingle => Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Single => true,
                _ => false,
            };

            /// <summary>
            /// 이 타입의 모든 값을 <see cref="double"/>에서 정밀도 및 값 손실 없이 표현할 수 있는지 확인합니다.
            /// </summary>
            public bool IsExactlyRepresentableAsDouble => Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Single => true,
                TypeCode.Double => true,
                _ => false,
            };

            /// <summary>
            /// 숫자 타입의 최소값을 가져옵니다.
            /// </summary>
            /// <exception cref="ArgumentException">지원하지 않는 숫자 타입인 경우 발생합니다.</exception>
            public object NumericMinValue => Type.GetTypeCode(type) switch
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
                _ => throw new ArgumentException($"'{type}'은(는) 지원하는 숫자 타입이 아닙니다.", nameof(type)),
            };

            /// <summary>
            /// 숫자 타입의 최대값을 가져옵니다.
            /// </summary>
            /// <exception cref="ArgumentException">지원하지 않는 숫자 타입인 경우 발생합니다.</exception>
            public object NumericMaxValue => Type.GetTypeCode(type) switch
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
                _ => throw new ArgumentException($"'{type}'은(는) 지원하는 숫자 타입이 아닙니다.", nameof(type)),
            };

            /// <summary>
            /// 주어진 <paramref name="type"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
            /// 구현하거나 상속하는지 확인합니다.
            /// </summary>
            /// <remarks>
            /// 이 메서드는 <paramref name="type"/>의 인터페이스 및 상속 계층 구조를 탐색하여
            /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
            /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
            /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
            /// </remarks>
            /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
            /// <returns>
            /// <paramref name="type"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
            /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="type"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
            /// </exception>
            public bool IsAssignableToGenericDefinition(Type genericTypeDefinition) => type.IsAssignableToGenericDefinition(genericTypeDefinition, out _);

            /// <summary>
            /// 주어진 <paramref name="type"/>이 특정 제네릭 타입 정의(<paramref name="genericTypeDefinition"/>)를
            /// 구현하거나 상속하는지 확인합니다.
            /// </summary>
            /// <remarks>
            /// 이 메서드는 <paramref name="type"/>의 인터페이스 및 상속 계층 구조를 탐색하여
            /// <paramref name="genericTypeDefinition"/>과 일치하는 제네릭 타입 정의가 있는지 검사합니다.<br/>
            /// 예를 들어, <c>List&lt;int&gt;</c>가 <c>IEnumerable&lt;&gt;</c>를 구현하는지,
            /// 또는 <c>MyDerivedClass&lt;T&gt;</c>가 <c>BaseClass&lt;&gt;</c>로부터 파생되었는지 등을 확인할 수 있습니다.
            /// </remarks>
            /// <param name="genericTypeDefinition">찾으려는 제네릭 타입 정의입니다 (예: <c>typeof(List&lt;&gt;)</c>, <c>typeof(IDictionary&lt;,&gt;)</c>).</param>
            /// <param name="resolvedType">
            /// <paramref name="type"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하는 경우,
            /// 실제로 발견된 구체적인 제네릭 타입(예: <c>IList&lt;int&gt;</c>)이 반환됩니다.<br/>
            /// 찾지 못한 경우 <see langword="null"/>이 반환됩니다.
            /// <br/><br/>
            /// 인터페이스일 경우엔 찾은 인터페이스 타입을 반환합니다.
            /// </param>
            /// <returns>
            /// <paramref name="type"/>이 <paramref name="genericTypeDefinition"/>을 구현하거나 상속하면
            /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="type"/> 또는 <paramref name="genericTypeDefinition"/>이 <see langword="null"/>인 경우 발생합니다.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="genericTypeDefinition"/>이 유효한 제네릭 타입 정의가 아닌 경우 발생할 수 있습니다.
            /// </exception>
            public bool IsAssignableToGenericDefinition(Type genericTypeDefinition, [MaybeNullWhen(false)] out Type resolvedType)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (type == null)
                    throw new ArgumentNullException(nameof(type), "The given type cannot be null.");
                if (genericTypeDefinition == null)
                    throw new ArgumentNullException(nameof(genericTypeDefinition), "The generic type definition cannot be null.");
                else if (!genericTypeDefinition.IsGenericTypeDefinition)
                    throw new ArgumentException("The provided genericTypeDefinition must be a valid generic type definition (e.g., typeof(List<>) or typeof(IDictionary<,>)).", nameof(genericTypeDefinition));
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                Type? currentType = type;
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
            /// 주어진 포인터 타입(<paramref name="type"/>)이 다른 포인터 타입(<paramref name="pointerType"/>)에 할당 가능한지 확인합니다.
            /// </summary>
            /// <remarks>
            /// 이 메서드는 다음과 같은 경우 <see langword="true"/>를 반환합니다:<br/>
            /// <list type="bullet">
            ///     <item><description><paramref name="type"/>이 <paramref name="pointerType"/>과 동일한 포인터 타입인 경우 (예: <c>int*</c>가 <c>int*</c>에 할당).</description></item>
            ///     <item><description><paramref name="type"/>이 포인터 타입이고, <paramref name="pointerType"/>이 <c>void*</c>인 경우 (예: <c>int*</c>가 <c>void*</c>에 할당).</description></item>
            /// </list>
            /// 두 타입 모두 반드시 포인터 타입이어야 합니다.
            /// </remarks>
            /// <param name="pointerType">할당받을 변수의 포인터 <see cref="Type"/>입니다 (예: <c>typeof(void*)</c>).</param>
            /// <returns>
            /// <paramref name="type"/>이 <paramref name="pointerType"/>에 할당 가능하면
            /// <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="type"/> 또는 <paramref name="pointerType"/>이 <see langword="null"/>인 경우 발생합니다.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="pointerType"/>이 유효한 포인터 타입이 아닌 경우 발생할 수 있습니다.
            /// </exception>
            public bool IsAssignableToPointer(Type pointerType)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (type == null)
                    throw new ArgumentNullException(nameof(type), "The given type cannot be null.");
                if (pointerType == null)
                    throw new ArgumentNullException(nameof(pointerType), "The target pointer type cannot be null.");
                else if (!pointerType.IsPointer)
                    throw new ArgumentException("The provided type must be a valid pointer type (e.g., typeof(int*) or typeof(void*)).", nameof(pointerType));
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                return type.IsPointer && (pointerType == typeof(void*) || type == pointerType);
            }

            /// <summary>
            /// 주어진 <paramref name="type"/>의 인스턴스가 <paramref name="targetType"/>의 변수에 할당 가능한지 확인합니다.
            /// <paramref name="targetType"/>이 제네릭 타입 정의(<c>List&lt;&gt;</c> 등)인 경우,
            /// <paramref name="type"/>이 해당 제네릭 정의를 구현하거나 상속하는지도 함께 검사합니다.<br/>
            /// 또한, 포인터 타입(<c>int*</c> 등) 간의 할당 가능성도 확인합니다.
            /// </summary>
            /// <remarks>
            /// 이 메서드는 다음과 같은 경우 <see langword="true"/>를 반환합니다:<br/>
            /// <list type="bullet">
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/>과 동일한 경우.</description></item>
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/>의 서브클래스인 경우.</description></item>
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/> 인터페이스를 구현하는 경우.</description></item>
            ///     <item><description><paramref name="targetType"/>이 제네릭 타입 정의이고, <paramref name="type"/>이 해당 정의를 구현하거나 상속하는 경우.</description></item>
            ///     <item><description>
            ///         <paramref name="targetType"/>이 포인터 타입(<c>T*</c>)이며, <paramref name="type"/>이 동일한 포인터 타입이거나 <c>void*</c>로 할당 가능한 포인터 타입인 경우.
            ///     </description></item>
            /// </list>
            /// 이 메서드는 <paramref name="targetType"/>이 제네릭 타입 정의인 경우
            /// <paramref name="type"/>의 인터페이스 및 상속 계층 구조를 탐색하여 일치 여부를 검사합니다.
            /// </remarks>
            /// <param name="targetType">할당받을 변수의 <see cref="Type"/>입니다. 제네릭 타입 정의이거나 포인터 타입일 수 있습니다.</param>
            /// <returns>
            /// <paramref name="type"/>이 <paramref name="targetType"/>에 할당 가능하면 <see langword="true"/>를 반환하고,
            /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="type"/> 또는 <paramref name="targetType"/>이 <see langword="null"/>인 경우 발생합니다.
            /// </exception>
            public bool IsAssignableToAny(Type targetType) => type.IsAssignableToAny(targetType, out _);

            /// <summary>
            /// 주어진 <paramref name="type"/>의 인스턴스가 <paramref name="targetType"/>의 변수에 할당 가능한지 확인합니다.
            /// <paramref name="targetType"/>이 제네릭 타입 정의(<c>List&lt;&gt;</c> 등)인 경우,
            /// <paramref name="type"/>이 해당 제네릭 정의를 구현하거나 상속하는지도 함께 검사합니다.<br/>
            /// 또한, 포인터 타입(<c>int*</c> 등) 간의 할당 가능성도 확인합니다.
            /// </summary>
            /// <remarks>
            /// 이 메서드는 다음과 같은 경우 <see langword="true"/>를 반환합니다:<br/>
            /// <list type="bullet">
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/>과 동일한 경우.</description></item>
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/>의 서브클래스인 경우.</description></item>
            ///     <item><description><paramref name="type"/>이 <paramref name="targetType"/> 인터페이스를 구현하는 경우.</description></item>
            ///     <item><description><paramref name="targetType"/>이 제네릭 타입 정의이고, <paramref name="type"/>이 해당 정의를 구현하거나 상속하는 경우.</description></item>
            ///     <item><description>
            ///         <paramref name="targetType"/>이 포인터 타입(<c>T*</c>)이며, <paramref name="type"/>이 동일한 포인터 타입이거나 <c>void*</c>로 할당 가능한 포인터 타입인 경우.
            ///     </description></item>
            /// </list>
            /// 이 메서드는 <paramref name="targetType"/>이 제네릭 타입 정의인 경우
            /// <paramref name="type"/>의 인터페이스 및 상속 계층 구조를 탐색하여 일치 여부를 검사합니다.
            /// </remarks>
            /// <param name="targetType">할당받을 변수의 <see cref="Type"/>입니다. 제네릭 타입 정의이거나 포인터 타입일 수 있습니다.</param>
            /// <param name="resolvedType">
            /// 할당이 가능한 경우, 발견된 구체적인 <see cref="Type"/> (예: <c>List&lt;int&gt;</c>) 또는
            /// <paramref name="targetType"/>이 제네릭 정의가 아닌 경우 <paramref name="targetType"/> 자체가 반환됩니다.
            /// <br/>
            /// <paramref name="targetType"/>이 포인터 타입으로 할당 가능할 때, <paramref name="type"/>이 반환됩니다.
            /// <br/>
            /// 할당이 불가능한 경우 <see langword="null"/>이 반환됩니다.
            /// </param>
            /// <returns>
            /// <paramref name="type"/>이 <paramref name="targetType"/>에 할당 가능하면 <see langword="true"/>를 반환하고,
            /// 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="type"/> 또는 <paramref name="targetType"/>이 <see langword="null"/>인 경우 발생합니다.
            /// </exception>
            public bool IsAssignableToAny(Type targetType, [MaybeNullWhen(false)] out Type resolvedType)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (type == null)
                    throw new ArgumentNullException(nameof(type), "The given type cannot be null.");
                else if (targetType == null)
                    throw new ArgumentNullException(nameof(targetType), "The target type cannot be null.");
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                if (targetType.IsPointer && type.IsAssignableToPointer(targetType))
                {
                    resolvedType = type;
                    return true;
                }

                if (targetType == typeof(object))
                {
                    resolvedType = targetType;
                    return true;
                }

                if (targetType.IsGenericTypeDefinition)
                    return type.IsAssignableToGenericDefinition(targetType, out resolvedType);
                else if (targetType.IsAssignableFrom(type))
                {
                    resolvedType = targetType;
                    return true;
                }

                resolvedType = null;
                return false;
            }

            public string GetTypeDisplayName()
            {
                if (type == typeof(void))
                    return "void";

                if (type.IsArray)
                    return (type.GetElementType() ?? typeof(object)).GetTypeDisplayName() + "[]";
                if (type.IsPointer)
                    return (type.GetElementType() ?? typeof(void)).GetTypeDisplayName() + "*";

                return Unity.Properties.TypeUtility.GetTypeDisplayName(type);
            }
        }

        extension(Type? type)
        {
            /// <summary>
            /// 지정된 타입의 상위 계층(상속 체인)을 열거합니다.<br/>
            /// 이 메서드는 현재 타입부터 시작하여 <see cref="object"/> 타입까지 모든 기본 타입을 반환합니다.
            /// </summary>
            /// <returns>
            /// 지정된 타입과 그 상위 기본 타입을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="Type"/> 컬렉션입니다.<br/>
            /// 만약 <paramref name="type"/>이 null인 경우 빈 컬렉션을 반환합니다.
            /// </returns>
            public IEnumerable<Type> GetHierarchy()
            {
                for (; type != null; type = type.BaseType)
                    yield return type;
            }
        }

        // ReSharper disable once MoveToExtensionBlock
        public static string SerializeToString(this Type type) => type.FullName + ", " + type.Assembly.GetName().Name;
        public static Type? DeserializeFromString(string typeName) => Type.GetType(typeName);
    }
}