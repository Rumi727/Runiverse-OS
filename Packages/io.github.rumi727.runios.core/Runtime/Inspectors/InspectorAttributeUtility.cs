using RuniOS.Inspectors.Attributes;
using RuniOS.Linq;
using System.Collections.Concurrent;
using System.Reflection;

namespace RuniOS.Inspectors
{
    public static class InspectorAttributeUtility
    {
        /// <summary>
        /// 지정된 부모 요소로부터 상속 가능한 어트리뷰트를 가져와 현재 목록과 병합합니다.
        /// </summary>
        public static IEnumerable<IInspectorAttribute> InheritFrom(this IEnumerable<IInspectorAttribute> attributes, IInspectorVariableElement? parent)
        {
            // 부모가 없으면 상속받을 게 없으므로 내 것(attributes)만 반환
            if (parent == null) 
                return attributes;

            // 로직: 부모의 속성 중 상속 가능한 것 필터링 -> 내 속성으로 덮어쓰기(Override)
            return parent.attributes.FilterInheritable().OverrideBy(attributes);
        }

        /// <summary>
        /// 지정된 부모 인스펙터로부터 상속 가능한 어트리뷰트를 가져와 현재 목록과 병합합니다.
        /// </summary>
        public static IEnumerable<IInspectorAttribute> InheritFrom(this IEnumerable<IInspectorAttribute> attributes, IInspectable parentInspectable)
        {
            // InspectableObject는 null일 수 없으므로 바로 처리
            return parentInspectable.attributes.FilterInheritable().OverrideBy(attributes);
        }

        /// <summary>
        /// 어트리뷰트 목록에서 자식에게 상속 가능한 것만 골라냅니다.
        /// </summary>
        /// <param name="source">어트리뷰트를 필터링할 원본 소스입니다.</param>
        /// <returns>상속 가능한 어트리뷰트 목록을 반환합니다.</returns>
        /// <exception cref="ArgumentNullException">소스가 null 값일 때 발생합니다.</exception>
        public static IEnumerable<IInspectorAttribute> FilterInheritable(this IEnumerable<IInspectorAttribute> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            
            return source.Where(x => x.inheritToChildren && !x.applyToSelf);
        }

        /// <summary>
        /// 2개의 어트리뷰트 목록을 합칩니다.<br/>
        /// 중복 불가 어트리뷰트는 <paramref name="mine"/>이 오버라이드합니다.
        /// </summary>
        public static IEnumerable<IInspectorAttribute> OverrideBy(this IEnumerable<IInspectorAttribute> inherited, IEnumerable<IInspectorAttribute> mine)
        {
            if (inherited.IsEmpty()) return mine;
            if (mine.IsEmpty()) return inherited;

            // 내 어트리뷰트 중 '중복 불가능(AllowMultiple=false)'한 타입 식별
            HashSet<Type> mySingleUseTypes = new HashSet<Type>
            (
                mine
                    .Select(attr => attr.GetType())
                    .Where(t => !IsAllowMultiple(t))
            );

            // 오버라이드 당하는 부모 속성 제외
            var filteredFirst = inherited
                .Where(parentAttr => !mySingleUseTypes.Contains(parentAttr.GetType()));

            // 상속받은 걸 먼저, 내 걸 나중에 배치
            return filteredFirst.Concat(mine);
        }

        // AttributeUsage 캐싱 (리플렉션 비용 절약)
        static readonly ConcurrentDictionary<Type, bool> allowMultipleCache = new();

        /// <summary>
        /// 지정한 어트리뷰트가 중복 적용이 허용되는지 여부를 가져옵니다.
        /// </summary>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        static bool IsAllowMultiple(Type attributeType)
        {
            return allowMultipleCache.GetOrAdd(attributeType, type =>
            {
                AttributeUsageAttribute? usage = type.GetCustomAttribute<AttributeUsageAttribute>(true);
                return usage?.AllowMultiple ?? false;
            });
        }
    }
}