#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace RuniOS.Reflection
{
    public static class ReflectionUtility
    {
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        static ReflectionUtility() => Refresh();

        /// <summary>
        /// 현재 로드된 모든 어셈블리 목록입니다.<br/>
        /// 이 목록은 <see cref="Refresh"/> 메서드에 의해 업데이트됩니다.
        /// <br/><br/>
        /// 이 속성은 스레드에 안전합니다.
        /// </summary>
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        public static ImmutableArray<Assembly> assemblies { get; private set; } = ImmutableArray<Assembly>.Empty;
        static readonly object assembliesLock = new();

        /// <summary>
        /// 현재 로드된 모든 형식(<see cref="Type"/>) 목록입니다.<br/>
        /// 이 목록은 <see cref="Refresh"/> 메서드에 의해 업데이트됩니다.
        /// <br/><br/>
        /// 이 속성은 스레드에 안전합니다.
        /// </summary>
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        public static ImmutableArray<Type> types { get; private set; } = ImmutableArray<Type>.Empty;



        /// <summary>
        /// <see cref="assemblies"/> 또는 <see cref="types"/> 목록이 <see cref="Refresh"/> 메서드를 통해
        /// 업데이트되었을 때 발생합니다.<br/>
        /// 이 이벤트 핸들러 추가/제거 및 호출은 내부적으로 잠금(<see langword="lock"/>)을 사용하여
        /// 스레드에 안전하게 보호됩니다.
        /// </summary>
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        public static event Action? onListUpdate
        {
            add
            {
                lock (onListUpdateLock)
                    _onListUpdate += value;
            }
            remove
            {
                lock (onListUpdateLock)
                    _onListUpdate -= value;
            }
        }
        static Action? _onListUpdate;
        static readonly object onListUpdateLock = new();



        public static bool IsAsyncMethod(this MethodBase methodBase) => methodBase.IsDefined(typeof(AsyncStateMachineAttribute));

        public static bool IsCompilerGenerated(this Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute));
        public static bool IsCompilerGenerated(this MemberInfo memberInfo) => memberInfo.IsDefined(typeof(CompilerGeneratedAttribute));



        /// <summary>
        /// 현재 애플리케이션 도메인에 로드된 어셈블리와 형식 목록을 새로고침(업데이트)합니다.<br/>
        /// 이 메서드는 내부적으로 잠금(<see langword="lock"/>)을 사용하여 스레드에 안전하게 데이터를 갱신합니다.
        /// </summary>
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        public static void Refresh()
        {
            lock (assembliesLock)
            {
                try
                {
#if UNITY_6000_6_OR_NEWER
                    assemblies = [..UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()];
#else
                    assemblys = AppDomain.CurrentDomain.GetAssemblies().ToImmutableArray();
#endif
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                types = [
                    ..assemblies
#if UNITY_EDITOR
                        .Where(x => 
                            /* 브릿지 코드 제외 */ !x.FullName.StartsWith("RuniOS.Editor.APIBridge", StringComparison.Ordinal) &&
                            /* 병신; */ !x.FullName.StartsWith(nameof(JetBrains), StringComparison.Ordinal))
#endif
                        .SelectMany(static x =>
                        {
                            try
                            {
                                return x.GetTypes();
                            }
                            catch (ReflectionTypeLoadException e)
                            {
                                Debug.LogException(e);
                                return e.Types.Where(static x => x != null);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }

                            return [];
                        })
                ];
            }
            
            lock (onListUpdateLock)
                _onListUpdate.SafeInvoke();
        }

        /// <summary>
        /// <typeparamref name="T"/> 어트리뷰트가 정의된 모든 정적이며 매개변수가 없는(parameterless)
        /// 메소드들을 리플렉션을 사용하여 비동기적으로 순회하며 호출합니다.<br/>
        /// <b>메소드의 매개변수 개수 체크</b>와 경고 메시지 출력은
        /// <b>개발 빌드나 에디터 환경</b>에서만 이루어집니다.<br/>
        /// 메소드 탐색은 백그라운드 스레드풀에서 수행됩니다.
        /// </summary>
        /// <typeparam name="T">찾을 메소드에 정의된 <see cref="PreserveAttribute"/> 타입입니다.</typeparam>
        [Obsolete("Global type discovery is deprecated. Use explicit registration.")]
        public static async UniTask InvokeDefinedMethods<T>() where T : PreserveAttribute
        {
            // Linq 쓰면 코드가 몇배는 깔끔해지겠지만, 메소드 호출이 너무 길어져 대략 2배에서 심하면 10배까지도 성능적인 차이가 나는것을 확인했습니다.
            // 소스 제너레이터를 사용해도 되지만, 모딩 환경을 고려하여 리플렉션으로 결정했습니다.
            List<MethodInfo> methods = [];

#if UNITY_EDITOR || ENABLE_PROFILER
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            await UniTask.RunOnThreadPool(() =>
            {
                for (int i = 0; i < types.Length; i++)
                {
                    foreach (var method in types[i].GetRuntimeMethods())
                    {
                        if (!method.IsStatic || method.IsSpecialName || method.IsAbstract)
                            return;

                        if (!method.IsDefined(typeof(T)))
                            continue;
                        
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
                        if (!method.GetParameters().IsEmpty())
                        {
                            Debug.RuntimeLogWarning
                            (
                                $"A method {method.DeclaringType?.Name}.{method.Name} defined as an attribute has been found with a non-zero number of parameters.\n" +
                                "This is currently ignored, but the built program will not check the number of parameters, so this will cause problems!"
                            );
                            return;
                        }
#endif
                        methods.Add(method);
                    }
                }
            });

#if UNITY_EDITOR || ENABLE_PROFILER
            Debug.RuntimeLog($"It took {stopwatch.Elapsed.TotalSeconds} seconds to create a list of methods that match the condition.", $"{nameof(ReflectionUtility)}.{nameof(InvokeDefinedMethods)}<{typeof(T).Name}>");
#endif

            foreach (var item in methods)
            {
                try
                {
                    item.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                await UniTask.Yield();
            }
        }
        
        public static MethodInfo GetMethodInfo(Delegate method) => method.Method;
        
        /// <summary>
        /// 문자열과 배열은 포함되지 않습니다!
        /// </summary>
        public static bool HasDefaultConstructor(this Type t, bool includeNonPublic = false)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
                flags |= BindingFlags.NonPublic; 
            
            return t.IsValueType || t.GetConstructor(flags, null, Type.EmptyTypes, null) != null;
        }
        
        public static bool IsFlags(this Enum value) => value.GetType().IsDefined(typeof(FlagsAttribute));

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
