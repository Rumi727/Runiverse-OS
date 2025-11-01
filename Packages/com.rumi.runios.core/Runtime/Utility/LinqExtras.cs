#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace RuniOS.Linq
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
        /// null이 아닌 요소만 포함하는 시퀀스를 반환합니다.<br/>
        /// <see cref="Object.Equals(object)"/> 메소드를 사용하여 <see cref="SerializableNullable{T}"/> 및 <see cref="UnityEngine.Object"/> 등을 지원합니다.
        /// </summary>
        /// <param name="source">필터링할 시퀀스입니다.</param>
        /// <returns>null이 아닌 요소만 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) => source.Where(static x => !x.IsNull())!;

        public static IEnumerable<KeyValuePair<TKey, TElement>> AsDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) => source.Select(item => new KeyValuePair<TKey, TElement>(keySelector(item), elementSelector(item)));

        public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list) => new ReadOnlyCollection<T>(list);

        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary);

        public static bool IsEmpty(this ICollection collection) => collection.Count == 0;
        public static bool Any(this ICollection collection) => collection.Count > 0;

        public static bool SequenceEqual(this IEnumerable first, IEnumerable second) =>
            SequenceEqual(first, second, null);

        public static bool SequenceEqual(this IEnumerable first, IEnumerable second, IEqualityComparer? comparer)
        {
            ExceptionUtility.ThrowIfArgumentNull(first, nameof(first));
            ExceptionUtility.ThrowIfArgumentNull(second, nameof(second));

            if (first is ICollection firstCol && second is ICollection secondCol)
            {
                if (firstCol.Count != secondCol.Count)
                    return false;

                if (firstCol is IList firstList && secondCol is IList secondList)
                {
                    int count = firstCol.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (comparer != null)
                        {
                            if (!comparer.Equals(firstList[i], secondList[i]))
                                return false;
                        }
                        else
                        {
                            if (!Equals(firstList[i], secondList[i]))
                                return false;
                        }
                    }

                    return true;
                }
            }

            IEnumerator e1 = first.GetEnumerator();
            IEnumerator e2 = second.GetEnumerator();

            using var d1 = e1 as IDisposable;
            using var d2 = e2 as IDisposable;

            while (e1.MoveNext())
            {
                if (!e2.MoveNext())
                    return false;

                if (comparer != null)
                {
                    if (!comparer.Equals(e1.Current, e2.Current))
                        return false;
                }
                else
                {
                    if (!Equals(e1.Current, e2.Current))
                        return false;
                }
            }

            return !e2.MoveNext();
        }

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
        
        public static int Count(this IEnumerable source)
        {
            if (source is ICollection collection)
                return collection.Count;
 
            int count = 0;
            IEnumerator e = source.GetEnumerator();
            using var disposable = e as IDisposable;
            checked
            {
                while (e.MoveNext())
                    count++;
            }
 
            return count;
        }
    }
}
