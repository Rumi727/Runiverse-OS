#nullable enable
using RuniOS.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
// ReSharper disable StaticMemberInGenericType

namespace RuniOS
{
    /// <summary>
    /// This abstract class serves as a registry and resolver for types that implement specific attribute-based logic.
    /// <br/>
    /// It automatically discovers classes derived from <see cref="TBase"/> that are decorated with <see cref="TAttribute"/> and manages them for lookup.
    /// <br/><br/>
    /// 이 추상 클래스는 특정 특성 기반 로직을 구현하는 타입들을 위한 레지스트리 및 리졸버(해결자) 역할을 합니다.
    /// <br/>
    /// <see cref="TAttribute"/>가 지정된 <see cref="TBase"/>의 파생 클래스를 자동으로 발견하고 조회할 수 있도록 관리합니다.
    /// </summary>
    /// <typeparam name="TBase">
    /// The base type of the handler system (formerly Drawer). Only classes derived from this type are managed.
    /// <br/>
    /// 핸들러 시스템의 기반 타입입니다. 이 타입을 상속받는 클래스들만 관리 대상이 됩니다.
    /// </typeparam>
    /// <typeparam name="TAttribute">
    /// The attribute type used to map a target type to a handler type.
    /// <br/>
    /// 대상 타입과 핸들러 타입을 매핑하는 데 사용되는 특성 타입입니다.
    /// </typeparam>
    public abstract class AttributeTypeResolver<TBase, TAttribute> where TAttribute : TypeHandlerAttribute
    {
        /// <summary>
        /// Initializes the static members of the <see cref="AttributeTypeResolver{TBase,TAttribute}"/> class.
        /// <br/>
        /// This process subscribes to the <see cref="ReflectionUtility.onListUpdate"/> event and immediately performs the initial discovery of drawer types.
        /// <br/>
        /// <see cref="AttributeTypeResolver{TBase,TAttribute}"/> 클래스의 정적 멤버를 초기화합니다.
        /// <br/>
        /// 이 과정은 <see cref="ReflectionUtility.onListUpdate"/> 이벤트에 구독하고 드로어 타입의 초기 발견을 즉시 수행합니다.
        /// </summary>
        static AttributeTypeResolver()
        {
            ReflectionUtility.onListUpdate += Update;
            Update();
            
            static void Update()
            {
                cachedDrawerTypes.Clear();
                lock (drawerTypesLock)
                {
                    drawerTypes =
                    [
                        ..ReflectionUtility.types
                            .Where
                            (
                                x =>
                                    x.IsDefined(typeof(TAttribute)) &&
                                    ((!typeof(TBase).IsAbstract && typeof(TBase) == x) || x.IsSubclassOf(typeof(TBase)))
                            )
                            .SelectMany
                            (
                                type => type.GetCustomAttributes<TAttribute>()
                                    .Select(attribute => (type, attribute))
                            )
                            .OrderByDescending
                            (
                                x =>
                                {
                                    Type targetType = x.attribute.targetType;
                                
                                    // 1. 1차 정렬 키: targetType이 인터페이스가 아닌지 여부 (bool)
                                    //    - 인터페이스가 아니면 (클래스/구조체): true (높은 값)
                                    //    - 인터페이스이면: false (낮은 값)
                                    //    -> OrderByDescending이므로 클래스/구조체가 인터페이스보다 앞에 위치
                                    bool isNotInterface = !x.attribute.targetType.IsInterface;

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
                                        x.attribute.priority
                                    );
                                }
                            )
                    ];
                }
            }
        }
        
        /// <summary>
        /// Gets a read-only list of all discovered drawer types derived from <see cref="TBase"/> and their associated <see cref="TAttribute"/>.
        /// <br/>
        /// The list is ordered by the hierarchy depth of the target type in descending order, ensuring that more specific drawers are prioritized.
        /// <br/><br/>
        /// <see cref="TBase"/>에서 파생된, 발견된 모든 드로어 타입과 관련 <see cref="TAttribute"/>의 읽기 전용 목록을 가져옵니다.
        /// <br/>
        /// 이 목록은 대상 타입의 계층 깊이(내림차순)에 따라 정렬되어, 더 구체적인 서랍이 우선적으로 처리되도록 합니다.
        /// <br/><br/>
        /// 이 속성은 <b>스레드에 안전</b>합니다. 내부적으로 잠금(<see langword="lock"/>)을 사용하여 <see cref="ReflectionUtility.onListUpdate"/> 이벤트 발생 시 데이터를 갱신합니다.
        /// </summary>
        public static ImmutableArray<(Type type, TAttribute attribute)> drawerTypes { get; private set; }
        static readonly object drawerTypesLock = new();

        static readonly ConcurrentDictionary<Type, (Type resolvedTargetType, Type drawerType)> cachedDrawerTypes = new();


        /// <summary>
        /// Finds the most specific drawer <see cref="Type"/> registered for the given target <see cref="Type"/>.
        /// <br/>
        /// This is a simplified overload that only returns the <see cref="Type"/> of the found drawer.
        /// <br/><br/>
        /// 주어진 대상 <see cref="Type"/>에 등록된 가장 구체적인 드로어 <see cref="Type"/>을 찾습니다.
        /// <br/>
        /// 이는 발견된 드로어의 <see cref="Type"/>만을 반환하는 간소화된 오버로드입니다.
        /// </summary>
        /// <param name="targetType">The type for which to find an associated drawer.</param>
        /// <param name="predicate">An optional filter to apply to the sorted list of drawer types before searching.
        /// <br/>
        /// 검색 전에 정렬된 드로어 타입 목록에 적용할 수 있는 선택적 필터입니다.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/> of the most specific drawer found, or <see langword="null"/> if no matching drawer is registered.
        /// </returns>
        public static Type? FindDrawerType(Type targetType, Func<(Type type, TAttribute attribute), bool>? predicate = null)
        {
            FindDrawerType(targetType, out _, out Type? type, predicate);
            return type;
        }

        /// <summary>
        /// Finds the most specific drawer <see cref="Type"/> registered for the given target <see cref="Type"/>.
        /// <br/>
        /// The search prioritizes drawers registered for the exact type, and then checks drawers that have <see cref="TypeHandlerAttribute.isSubtypeCompatible"/> set to <see langword="true"/> for assignable types.
        /// <br/><br/>
        /// 주어진 대상 <see cref="Type"/>에 등록된 가장 구체적인 드로어 <see cref="Type"/>을 찾습니다.
        /// <br/>
        /// 검색은 정확히 일치하는 타입에 등록된 드로어를 우선하며, 이후 <see cref="TypeHandlerAttribute.isSubtypeCompatible"/>이 <see langword="true"/>로 설정된 드로어에 대해서 할당 가능한 타입인지 확인하여 적용합니다.
        /// </summary>
        /// <param name="targetType">The type for which to find an associated drawer.</param>
        /// <param name="resolvedTargetType">
        /// When the method returns <see langword="true"/>, contains the **closest resolved assignable type** that matches the drawer's target.
        /// <br/><br/>
        /// For example, if <paramref name="targetType"/> is <c>List&lt;int&gt;</c> and the drawer targets <c>IList&lt;&gt;</c>, this will return <c>IList&lt;int&gt;</c>.
        /// <br/><br/>
        /// 메서드가 <see langword="true"/>를 반환할 때, 드로어의 대상과 일치하는 **가장 가까운 해결된 할당 가능 타입**을 포함합니다.
        /// <br/><br/>
        /// 예를 들어, <paramref name="targetType"/>이 <c>List&lt;int&gt;</c>이고 드로어가 <c>IList&lt;&gt;</c>를 대상으로 할 경우, 이 값은 <c>IList&lt;int&gt;</c>가 반환됩니다.
        /// </param>
        /// <param name="drawerType">When the method returns <see langword="true"/>, contains the <see cref="Type"/> of the most specific drawer found.
        /// <br/><br/>
        /// 메서드가 <see langword="true"/>를 반환할 때, 발견된 가장 구체적인 드로어의 <see cref="Type"/>을 포함합니다.
        /// </param>
        /// <param name="predicate">An optional filter to apply to the sorted list of drawer types before searching.
        /// <br/>
        /// 검색 전에 정렬된 드로어 타입 목록에 적용할 수 있는 선택적 필터입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a matching drawer is found; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool FindDrawerType(Type targetType, [MaybeNullWhen(false)] out Type resolvedTargetType, [MaybeNullWhen(false)] out Type drawerType, Func<(Type type, TAttribute attribute), bool>? predicate = null)
        {
            IEnumerable<(Type type, TAttribute attribute)> enumerable = drawerTypes;
            if (predicate != null)
                enumerable = enumerable.Where(predicate);
            else if (cachedDrawerTypes.TryGetValue(targetType, out (Type resolvedTargetType, Type drawerType) value))
            {
                resolvedTargetType = value.resolvedTargetType;
                drawerType = value.drawerType;
                
                return true;
            }
            
            foreach ((Type type, TAttribute attribute) in enumerable)
            {
                resolvedTargetType = targetType;
                
                if (targetType == attribute.targetType || (attribute.isSubtypeCompatible && targetType.IsAssignableToAny(attribute.targetType, out resolvedTargetType)))
                {
                    drawerType = type;
                    cachedDrawerTypes.TryAdd(drawerType, (resolvedTargetType, drawerType));
                    
                    return true;
                }
            }

            resolvedTargetType = null;
            drawerType = null;
            
            return false;
        }
    }
}