#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuniOS.Reflection
{
    public static class ReflectionUtility
    {
        extension(MethodBase methodBase)
        {
            public bool IsAsyncMethod => methodBase.IsDefined(typeof(AsyncStateMachineAttribute), false);
        }

        extension(MemberInfo memberInfo)
        {
            public bool IsCompilerGenerated => memberInfo.IsDefined(typeof(CompilerGeneratedAttribute), true);
        }

        public static MethodInfo GetMethodInfo(Delegate method) => method.Method;

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

            // Nullable<T>는 default 값을 boxing하면 null이 되므로,
            // non-null 상태인 default(T)를 대신 반환합니다.
            if (Nullable.GetUnderlyingType(type) is { } underlyingType)
                return Activator.CreateInstance(underlyingType, nonPublic)!;

            return Activator.CreateInstance(type, nonPublic) ?? throw new InvalidOperationException($"Could not create a non-null default value for '{type}'.");
        }

        public static bool CanGetDefaultValueNotNull(this Type type, bool nonPublic = false) => type == typeof(string) || type.IsArray || type.HasDefaultConstructor(nonPublic);

        /// <summary>
        /// 문자열과 배열은 포함되지 않습니다!
        /// </summary>
        public static bool HasDefaultConstructor(this Type type, bool nonPublic = false)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flags |= BindingFlags.NonPublic;

            return type.IsValueType || type.GetConstructor(flags, null, Type.EmptyTypes, null) != null;
        }

        [return: NotNullIfNotNull("value")]
        public static object? Cast(this object? value, Type type)
        {
            if (value == null)
                return null;

            ParameterExpression param = Expression.Parameter(typeof(object), nameof(value));
            BlockExpression block = Expression.Block(Expression.Convert(Expression.Convert(param, value.GetType()), type));
            Delegate run = Expression.Lambda(block, param).Compile();
            
            return run.DynamicInvoke(value);
        }

        /// <summary>
        /// Orders the elements of <paramref name="source"/> by the specificity of the <see cref="Type"/> selected for each element.<br/>
        /// 각 요소에서 선택한 <see cref="Type"/>의 구체성에 따라 <paramref name="source"/>의 요소를 정렬합니다.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the elements in <paramref name="source"/>.<br/>
        /// <paramref name="source"/>를 구성하는 요소의 타입입니다.
        /// </typeparam>
        /// <param name="source">
        /// The sequence to order.<br/>
        /// 정렬할 시퀀스입니다.
        /// </param>
        /// <param name="typeSelector">
        /// A function that selects the <see cref="Type"/> used to determine the order of each element.<br/>
        /// 각 요소의 정렬 순서를 결정하는 데 사용할 <see cref="Type"/>을 선택하는 함수입니다.
        /// </param>
        /// <param name="prioritySelector">
        /// An optional function that returns an additional priority for each element. Higher values are ordered first, and an omitted function uses <c>0</c>.<br/>
        /// 각 요소의 추가 우선순위를 반환하는 선택적 함수입니다. 값이 높을수록 먼저 정렬되며, 함수를 생략하면 <c>0</c>을 사용합니다.
        /// </param>
        /// <returns>
        /// An <see cref="IOrderedEnumerable{TSource}"/> that orders the elements according to the selected type and priority.<br/>
        /// 선택한 타입과 우선순위에 따라 요소를 정렬하는 <see cref="IOrderedEnumerable{TSource}"/>입니다.
        /// </returns>
        /// <remarks>
        /// The tuple keys first place types other than <see langword="void"/>, <see langword="object"/>, and <see cref="ValueType"/> ahead of those special types. Among the special types, the order is <see cref="ValueType"/>, <see langword="object"/>, then <see langword="void"/>. Non-interface types are then ordered before interfaces, and greater type depth is ordered first. For interfaces, type depth is the number of inherited interfaces; for other types, it is the length of the base-type hierarchy. <paramref name="prioritySelector"/> is used as the final ordering key.<br/>
        /// 튜플 키는 먼저 <see langword="void"/>, <see langword="object"/>, <see cref="ValueType"/>이 아닌 타입을 해당 특수 타입보다 앞에 정렬합니다. 특수 타입 사이에서는 <see cref="ValueType"/>, <see langword="object"/>, <see langword="void"/> 순으로 정렬합니다. 이후 인터페이스가 아닌 타입을 인터페이스보다 먼저 정렬하고, 타입 깊이가 큰 타입을 먼저 정렬합니다. 인터페이스의 타입 깊이는 상속한 인터페이스 수이며, 그 외 타입의 타입 깊이는 기본 타입 계층의 길이입니다. <paramref name="prioritySelector"/>는 최종 정렬 키로 사용합니다.
        /// <br/><br/>
        /// The returned enumerable uses deferred execution, so the source and selectors are evaluated when the result is enumerated.<br/>
        /// 반환된 열거 가능 컬렉션은 지연 실행되므로, 소스와 선택기 함수는 결과를 열거할 때 평가됩니다.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> is <see langword="null"/>.<br/>
        /// <paramref name="source"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public static IOrderedEnumerable<TSource> OrderByTypes<TSource>(this IEnumerable<TSource> source, Func<TSource, Type> typeSelector, Func<TSource, int>? prioritySelector = null)
        {
            return source.OrderByDescending
            (
                x =>
                {
                    Type targetType = typeSelector.Invoke(x);

                    // 1. 1차 정렬 키: targetType이 인터페이스가 아닌지 여부 (bool)
                    //    - 인터페이스가 아니면 (클래스/구조체): true (높은 값)
                    //    - 인터페이스이면: false (낮은 값)
                    //    -> OrderByDescending이므로 클래스/구조체가 인터페이스보다 앞에 위치
                    bool isNotInterface = !targetType.IsInterface;

                    // 2차 정렬 키: 타입의 깊이 가중치 (int)
                    int depthWeight;
                    if (isNotInterface)
                    {
                        // [클래스/구조체]: GetHierarchy() (상속 체인 길이) 사용
                        depthWeight = targetType.GetHierarchy().Count();
                    }
                    else
                    {
                        // [인터페이스]: 인터페이스가 상속하는 인터페이스의 개수를 사용합니다.
                        // 상속 개수가 많을수록 구체적입니다. OrderByDescending이므로:
                        // - IChild: 2 (높음, 우선순위 높음)
                        // - IBase: 1
                        // - IRoot: 0 (낮음, 우선순위 낮음)
                        depthWeight = targetType.GetInterfaces().Length;
                    }

                    // 최종 정렬 키 튜플
                    // OrderByDescending은 튜플의 요소를 순서대로 비교합니다.
                    return
                    (
                        // 1. 특정 기본 타입 예외 처리 (높은 우선순위)
                        targetType != typeof(void),
                        targetType != typeof(object),
                        targetType != typeof(ValueType),
                        // 2. 클래스 우선
                        isNotInterface,
                        // 3. 깊이 가중치 (높을수록 구체적이고 우선순위 높음)
                        depthWeight,
                        prioritySelector?.Invoke(x) ?? 0
                    );
                }
            );
        }
    }
}
