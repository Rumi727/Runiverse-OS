#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
// ReSharper disable StaticMemberInGenericType

namespace RuniOS
{
    /// <summary>
    /// This abstract class provides the core logic for automatically discovering and managing classes that implement specific attribute-based drawing logic.
    /// <br/>
    /// It functions as a **static utility class** for managing drawer types associated with <see cref="TAttribute"/>, and serves as a **base class** for specific drawer implementations.
    /// <br/><br/>
    /// 이 추상 클래스는 특정 특성 기반 드로잉 로직을 구현하는 클래스를 자동으로 발견하고 관리하는 핵심 로직을 제공합니다.
    /// <br/>
    /// 이 클래스는 <see cref="TAttribute"/>와 관련된 드로어 타입을 관리하는 <b>정적 유틸리티 클래스</b> 역할을 하며, 특정 드로어 구현을 위한 <b>기반 클래스</b> 역할도 수행합니다.
    /// </summary>
    /// <typeparam name="TBaseDrawer">The base type of the specific drawer system. This type is used to filter reflection results, ensuring only classes that derive from <see cref="TBaseDrawer"/> (and have <see cref="TAttribute"/>) are managed.
    /// <br/>
    /// 특정 드로어 시스템의 기준이 되는 기반 타입입니다. 이 타입은 리플렉션 결과를 필터링하는 데 사용되며, <see cref="TBaseDrawer"/>를 상속하는 클래스(및 <see cref="TAttribute"/>가 있는 클래스)만이 관리되도록 합니다.
    /// </typeparam>
    /// <typeparam name="TAttribute">The specific attribute type that marks the classes this drawer should manage, which must inherit from <see cref="CustomAttributeDrawerAttribute"/>.
    /// <br/>
    /// 이 드로어가 관리해야 할 클래스를 표시하는 특정 특성 타입이며, <see cref="CustomAttributeDrawerAttribute"/>를 상속해야 합니다.
    /// </typeparam>
    public abstract class AttributeDrawer<TBaseDrawer, TAttribute> where TAttribute : CustomAttributeDrawerAttribute
    {
        /// <summary>
        /// Initializes the static members of the <see cref="AttributeDrawer{TBaseDrawer, TAttribute}"/> class.
        /// <br/>
        /// This process subscribes to the <see cref="ReflectionUtility.onListUpdate"/> event and immediately performs the initial discovery of drawer types.
        /// <br/>
        /// <see cref="AttributeDrawer{TBaseDrawer, TAttribute}"/> 클래스의 정적 멤버를 초기화합니다.
        /// <br/>
        /// 이 과정은 <see cref="ReflectionUtility.onListUpdate"/> 이벤트에 구독하고 드로어 타입의 초기 발견을 즉시 수행합니다.
        /// </summary>
        static AttributeDrawer()
        {
            ReflectionUtility.onListUpdate += Update;
            Update();
            
            static void Update()
            {
                lock (drawerTypesLock)
                {
                    drawerTypes = ReflectionUtility.types
                        .Where
                        (
                            x =>
                                x.IsDefined(typeof(TAttribute)) &&
                                x.IsSubclassOf(typeof(TBaseDrawer))
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
                                    targetType != typeof(Array),
                                    targetType != typeof(ValueType),
                                    targetType != typeof(Enum),
                                    // 2. 클래스 우선
                                    isNotInterface,
                                    // 3. 깊이 가중치 (높을수록 구체적이고 우선순위 높음)
                                    depthWeight
                                );
                            }
                        ).ToImmutableArray();
                }
            }
        }
        
        /// <summary>
        /// Gets a read-only list of all discovered drawer types derived from <see cref="TBaseDrawer"/> and their associated <see cref="TAttribute"/>.
        /// <br/>
        /// The list is ordered by the hierarchy depth of the target type in descending order, ensuring that more specific drawers are prioritized.
        /// <br/><br/>
        /// <see cref="TBaseDrawer"/>에서 파생된, 발견된 모든 드로어 타입과 관련 <see cref="TAttribute"/>의 읽기 전용 목록을 가져옵니다.
        /// <br/>
        /// 이 목록은 대상 타입의 계층 깊이(내림차순)에 따라 정렬되어, 더 구체적인 서랍이 우선적으로 처리되도록 합니다.
        /// <br/><br/>
        /// 이 속성은 <b>스레드에 안전</b>합니다. 내부적으로 잠금(<see langword="lock"/>)을 사용하여 <see cref="ReflectionUtility.onListUpdate"/> 이벤트 발생 시 데이터를 갱신합니다.
        /// </summary>
        public static ImmutableArray<(Type type, TAttribute attribute)> drawerTypes { get; private set; }
        static readonly object drawerTypesLock = new();
        
        
        /// <summary>
        /// Finds the most specific drawer <see cref="Type"/> registered for the given target <see cref="Type"/>.
        /// <br/>
        /// The search prioritizes drawers registered for the exact type, and then checks drawers that have <see cref="CustomAttributeDrawerAttribute.isSubtypeCompatible"/> set to <see langword="true"/> for assignable types.
        /// <br/><br/>
        /// 주어진 대상 <see cref="Type"/>에 등록된 가장 구체적인 드로어 <see cref="Type"/>을 찾습니다.
        /// <br/>
        /// 검색은 정확히 일치하는 타입에 등록된 드로어를 우선하며, 이후 <see cref="CustomAttributeDrawerAttribute.isSubtypeCompatible"/>이 <see langword="true"/>로 설정된 드로어에 대해서 할당 가능한 타입인지 확인하여 적용합니다.
        /// </summary>
        /// <param name="targetType">The type for which to find an associated drawer.</param>
        /// <returns>
        /// The <see cref="Type"/> of the most specific drawer found, or <see langword="null"/> if no matching drawer is registered.
        /// </returns>
        public static Type? FindDrawerType(Type targetType)
        {
            foreach ((Type type, TAttribute attribute) in drawerTypes)
            {
                if (targetType == attribute.targetType || (attribute.isSubtypeCompatible && targetType.IsAssignableToAny(attribute.targetType)))
                    return type;
            }

            return null;
        }
    }
}