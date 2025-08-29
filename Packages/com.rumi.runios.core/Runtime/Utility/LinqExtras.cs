#nullable enable
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class LinqExtras
    {
        public static bool Contains<T>(this IEnumerable<T> first, IEnumerable<T> second) => first.Intersect(second).Any();
        public static bool Contains<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer) => first.Intersect(second, comparer).Any();

        public static bool ContainsAll<T>(this IEnumerable<T> first, IEnumerable<T> second) => !second.Except(first).Any();
        public static bool ContainsAll<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer) => !second.Except(first, comparer).Any();

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

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) => source.Where(static x => x != null)!;
        
        /// <summary>
        /// 지정된 열거형의 순서와 내용을 기반으로 해시코드를 생성합니다.<br/>
        /// 열거형의 요소가 순서까지 같으면 동일한 해시코드를 반환합니다.
        /// </summary>
        /// <param name="list">해시코드를 계산할 리스트 또는 배열입니다.</param>
        /// <typeparam name="T">리스트 또는 배열의 요소 타입입니다.</typeparam>
        /// <returns>생성된 해시코드를 반환합니다.</returns>
        public static int GetSequenceHashCode<T>(this IEnumerable<T>? list)
        {
            if (list == null)
                return 0;
            
            var hash = new HashCode();
            foreach (var item in list)
                hash.Add(item);

            return hash.ToHashCode();
        }
    }
}
