#nullable enable
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class LinqExtras
    {
        /// <summary>
        /// 첫 번째 시퀀스에 두 번째 시퀀스의 요소가 하나라도 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="first">소스 시퀀스입니다.</param>
        /// <param name="second">포함 여부를 확인할 요소를 가진 시퀀스입니다.</param>
        /// <returns>하나 이상의 요소가 포함되어 있으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool Contains<T>(this IEnumerable<T> first, IEnumerable<T> second) => first.Intersect(second).Any();
        
        /// <summary>
        /// 지정된 비교자를 사용하여 첫 번째 시퀀스에 두 번째 시퀀스의 요소가 하나라도 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="first">소스 시퀀스입니다.</param>
        /// <param name="second">포함 여부를 확인할 요소를 가진 시퀀스입니다.</param>
        /// <param name="comparer">요소를 비교하는 데 사용할 <see cref="IEqualityComparer{T}"/>입니다.</param>
        /// <returns>하나 이상의 요소가 포함되어 있으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool Contains<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer) => first.Intersect(second, comparer).Any();

        /// <summary>
        /// 첫 번째 시퀀스에 두 번째 시퀀스의 모든 요소가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="first">소스 시퀀스입니다.</param>
        /// <param name="second">포함 여부를 확인할 모든 요소를 가진 시퀀스입니다.</param>
        /// <returns>모든 요소가 포함되어 있으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool ContainsAll<T>(this IEnumerable<T> first, IEnumerable<T> second) => !second.Except(first).Any();
        
        /// <summary>
        /// 지정된 비교자를 사용하여 첫 번째 시퀀스에 두 번째 시퀀스의 모든 요소가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="first">소스 시퀀스입니다.</param>
        /// <param name="second">포함 여부를 확인할 모든 요소를 가진 시퀀스입니다.</param>
        /// <param name="comparer">요소를 비교하는 데 사용할 <see cref="IEqualityComparer{T}"/>입니다.</param>
        /// <returns>모든 요소가 포함되어 있으면 true이고, 그렇지 않으면 false입니다.</returns>
        public static bool ContainsAll<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer) => !second.Except(first, comparer).Any();

        /// <summary>
        /// 시퀀스에서 지정된 요소의 첫 번째 인덱스를 반환합니다.
        /// </summary>
        /// <param name="source">검색할 시퀀스입니다.</param>
        /// <param name="item">찾을 요소입니다.</param>
        /// <returns>요소를 찾으면 해당 요소의 첫 번째 인덱스이고, 찾지 못하면 -1입니다.</returns>
        public static int IndexOf<T>(this IEnumerable<T?> source, T? item)
        {
            int index = 0;
            foreach (var item2 in source)
            {
                if (item == null && item2 == null)
                    return index;
                else if (item == null)
                    continue;
                else if (item2 == null)
                    continue;

                if (item.Equals(item2))
                    return index;

                index++;
            }

            return -1;
        }

        /// <summary>
        /// null이 아닌 요소만 포함하는 시퀀스를 반환합니다.
        /// </summary>
        /// <param name="source">필터링할 시퀀스입니다.</param>
        /// <returns>null이 아닌 요소만 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) => source.Where(static x => x != null)!;
        
        /// <summary>
        /// null이 아닌 요소만 포함하는 시퀀스를 반환합니다.
        /// </summary>
        /// <param name="source">필터링할 시퀀스입니다.</param>
        /// <returns>null이 아닌 요소만 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public static IEnumerable<T> WhereNotFakeNull<T>(this IEnumerable<T?> source) where T : UnityEngine.Object => source.Where(static x => x != null)!;

        public static IEnumerable<KeyValuePair<TKey, TElement>> AsDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) => source.Select(item => new KeyValuePair<TKey, TElement>(keySelector(item), elementSelector(item))); 
    }
}
