#nullable enable
using ExtendedNumerics;
using System.Collections;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RuniOS.Utility
{
    public static class ListUtility
    {
        public static void Move(this IList list, int oldIndex, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(list);

            object? temp = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, temp);
        }

        public static void Change(this IList list, int oldIndex, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(list);
            
            (list[oldIndex], list[newIndex]) = (list[newIndex], list[oldIndex]);
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public static void Resize(this IList list, int newSize, Func<int, object?>? activator = null)
        {
            if (list.Count == newSize)
                return;
            
            bool add = list.Count < newSize;
            int count = (list.Count - newSize).Abs();

            for (int i = 0; i < count; i++)
            {
                if (add)
                    list.Add(activator?.Invoke(list.Count));
                else
                    list.RemoveAt(list.Count - 1);
            }
        }

        #region Close Value
        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static T CloseValue<T>(this IEnumerable<T> list, T target) where T : INumber<T>
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Any())
                return list.Aggregate((x, y) => CloseDistance(x, target) < CloseDistance(y, target) ? x : y);

            return T.Zero;
        }

        static T CloseDistance<T>(T a, T b) where T : INumber<T> => a > b ? a - b : b - a;

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static BigDecimal CloseValue(this IEnumerable<BigDecimal> list, BigDecimal target)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        #endregion

        #region Close Value Get Number Func
        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static TNumber CloseValue<T, TNumber>(this IEnumerable<T> list, TNumber target, Func<T, TNumber> getNumberFunc) where TNumber : INumber<TNumber>
        {
            ArgumentNullException.ThrowIfNull(list);

            using IEnumerator<T> enumerator = list.GetEnumerator();
            if (!enumerator.MoveNext())
                return TNumber.Zero;

            TNumber val = getNumberFunc(enumerator.Current);
            while (enumerator.MoveNext())
            {
                TNumber currentNumber = getNumberFunc(enumerator.Current);
                val = CloseDistance(val, target) < CloseDistance(currentNumber, target) ? val : currentNumber;
            }

            return val;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static BigDecimal CloseValue<T>(this IEnumerable<T> list, BigDecimal target, Func<T, BigDecimal> getNumberFunc)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                BigDecimal val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    BigDecimal currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        #endregion

        #region Close Value Predicate
        /// <summary>
        /// 조건을 만족하는 항목 중 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">대상 항목을 거르는 조건</param>
        /// <returns></returns>
        public static TNumber CloseValue<T, TNumber>(this IEnumerable<T> list, TNumber target, Func<T, TNumber> getNumberFunc, Predicate<T> predicate) where TNumber : INumber<TNumber>
        {
            ArgumentNullException.ThrowIfNull(list);

            using IEnumerator<T> enumerator = list.GetEnumerator();

            TNumber val = TNumber.Zero;
            bool exists = false;
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    val = getNumberFunc(enumerator.Current);
                    exists = true;
                    break;
                }
            }

            if (!exists)
                return TNumber.Zero;

            while (enumerator.MoveNext())
            {
                if (!predicate(enumerator.Current))
                    continue;

                TNumber currentNumber = getNumberFunc(enumerator.Current);
                val = CloseDistance(val, target) < CloseDistance(currentNumber, target) ? val : currentNumber;
            }

            return val;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static BigDecimal CloseValue<T>(this IEnumerable<T> list, BigDecimal target, Func<T, BigDecimal> getNumberFunc, Predicate<T> predicate)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                BigDecimal val = 0;
                bool exists = false;
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current))
                    {
                        val = getNumberFunc(enumerator.Current);
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    return 0;

                while (enumerator.MoveNext())
                {
                    if (!predicate(enumerator.Current))
                        continue;

                    BigDecimal currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        #endregion

        #region Close Value Index
        public static int CloseValueIndex<T>(this IList<T> list, T target) where T : INumber<T>
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count > 0)
                return list.IndexOf(list.CloseValue(target));

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾고 인덱스를 반환합니다
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int CloseValueIndex(this IList<BigDecimal> list, BigDecimal target)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count > 0)
                return list.IndexOf(list.CloseValue(target));

            return 0;
        }

        #endregion

        #region Close Value Index Binary Search
        public static int CloseValueIndexBinarySearch<T>(this List<T> list, T target) where T : INumber<T>
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count > 0)
                return list.BinarySearch(list.CloseValue(target));

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾고 이진 검색으로 인덱스를 반환합니다
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int CloseValueIndexBinarySearch(this List<BigDecimal> list, BigDecimal target)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count > 0)
                return list.BinarySearch(list.CloseValue(target));

            return 0;
        }

        #endregion

        #region Deduplicate
        public static void Deduplicate<T>(this IList<T> values, T delta) where T : INumber<T>
        {
            int index = 0;
            while (index < values.Count)
            {
                T value = values[index];

                int i = index + 1;
                while (i < values.Count)
                {
                    if (T.Abs(value - values[i]) < delta)
                        values.RemoveAt(i);
                    else
                        i++;
                }

                index++;
            }
        }

        public static void Deduplicate<T>(this IList<T> values, T delta, T setValue) where T : INumber<T>
        {
            int index = 0;
            while (index < values.Count)
            {
                T value = values[index];

                int i = index + 1;
                while (i < values.Count)
                {
                    if (T.Abs(value - values[i]) < delta)
                        values[i] = setValue;

                    i++;
                }

                index++;
            }
        }

        public static void Deduplicate(this IList<BigDecimal> values, BigDecimal delta)
        {
            int index = 0;
            while (index < values.Count)
            {
                BigDecimal value = values[index];

                int i = index + 1;
                while (i < values.Count)
                {
                    if ((value - values[i]).Abs() < delta)
                        values.RemoveAt(i);
                    else
                        i++;
                }

                index++;
            }
        }

        public static void Deduplicate(this IList<BigDecimal> values, BigDecimal delta, BigDecimal setValue)
        {
            int index = 0;
            while (index < values.Count)
            {
                BigDecimal value = values[index];

                int i = index + 1;
                while (i < values.Count)
                {
                    if ((value - values[i]).Abs() < delta)
                        values[i] = setValue;

                    i++;
                }

                index++;
            }
        }
        #endregion

        static readonly Regex digitRegex = new Regex(@"\d+", RegexOptions.Compiled);

        public static IEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> sources, Func<T, string> selector) => sources.OrderByAlphaNumeric(selector, StringComparer.CurrentCulture);

        public static IEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> sources, Func<T, string> selector, StringComparer comparer)
        {
            int maxDigits = sources
                .SelectMany(x => digitRegex.Matches(selector(x)).Select(digitChunk => (int?)digitChunk.Value.Length))
                .Max() ?? 0;

            return sources.OrderBy(x => digitRegex.Replace(selector(x), match => match.Value.PadLeft(maxDigits, '0')), comparer);
        }

        public static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey fromKey, TKey toKey)
        {
            TValue value = dic[fromKey];

            dic.Remove(fromKey);
            dic[toKey] = value;
        }

        #region Array
        public static Array Add(this Array array, object? item)
        {
            array = array.Resize(array.Length + 1);
            array.SetValue(item, array.Length - 1);

            return array;
        }

        public static T[] Add<T>(this T[] array, T item)
        {
            Array.Resize(ref array, array.Length + 1);
            array[^1] = item;

            return array;
        }

        public static Array Insert(this Array array, int index, object? item)
        {
            array = array.Resize(array.Length + 1);
            Array.Copy(array, index, array, index + 1, array.Length - index - 1);

            array.SetValue(item, index);

            return array;
        }

        public static T[] Insert<T>(this T[] array, int index, T item)
        {
            Array.Resize(ref array, array.Length + 1);
            Array.Copy(array, index, array, index + 1, array.Length - index - 1);

            array[index] = item;

            return array;
        }

        public static Array Remove(this Array array, object? item) => array.RemoveAt(Array.IndexOf(array, item));
        public static T[] Remove<T>(this T[] array, T? item) => array.RemoveAt(Array.IndexOf(array, item));

        public static Array RemoveAt(this Array array, int index)
        {
            Array.Copy(array, index + 1, array, index, array.Length - index - 1);
            array = array.Resize(array.Length - 1);

            return array;
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            Array.Copy(array, index + 1, array, index, array.Length - index - 1);
            Array.Resize(ref array, array.Length - 1);

            return array;
        }
        
        public static Array RemoveAll(this Array array) => Array.CreateInstance(array.GetType().GetElementType() ?? throw new InvalidOperationException(), 0);
        
        public static Array Move(this Array list, int oldIndex, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(list);

            object? temp = list.GetValue(oldIndex);
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, temp);

            return list;
        }
        
        public static T[] Move<T>(this T[] list, int oldIndex, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(list);

            T temp = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, temp);

            return list;
        }
        
        public static Array Resize(this Array array, int newSize, Func<int, object?>? activator = null)
        {
            if (array.Length == newSize)
                return array;
            
            Array newArray = Array.CreateInstance(array.GetType().GetElementType() ?? throw new InvalidOperationException(), newSize);
            Array.Copy(array, 0, newArray, 0, (array.Length > newSize) ? newSize : array.Length);

            if (activator != null && array.Length < newSize)
            {
                int count = newSize - array.Length;
                for (int i = 0; i < count; i++)
                    array.SetValue(activator.Invoke(array.Length + i), array.Length + i);
            }

            return newArray;
        }
        
        public static T[] Resize<T>(this T[] array, int newSize, Func<int, T> activator)
        {
            if (array.Length == newSize)
                return array;
            
            T[] newArray = new T[newSize];
            Array.Copy(array, 0, newArray, 0, (array.Length > newSize) ? newSize : array.Length);

            if (array.Length < newSize)
            {
                int count = newSize - array.Length;
                for (int i = 0; i < count; i++)
                    array[array.Length + i] = activator.Invoke(array.Length + i);
            }

            return newArray;
        }

        public static Array Copy(this Array array)
        {
            return (Array)array.Clone();
        }

        public static T[] Copy<T>(this T[] array) => (T[])array.Clone();
        #endregion
        
        /// <summary>
        /// 대상 Dictionary의 키들을 주어진 List의 아이템들과 동기화합니다.<br/>
        /// List에 없는 키는 제거되고, Dictionary에 없는 키는 valueFactory를 통해 추가됩니다.<br/>
        /// 기존 키에 대한 값(Value)은 유지됩니다.
        /// </summary>
        /// <typeparam name="TKey">키 타입입니다.</typeparam>
        /// <typeparam name="TValue">값 타입입니다.</typeparam>
        /// <param name="targetDictionary">키를 동기화할 대상 Dictionary입니다.</param>
        /// <param name="source">동기화의 기준이 되는 List입니다.</param>
        /// <param name="valueFactory">새로운 키가 추가될 때 사용할 기본 값을 생성하는 함수입니다.</param>
        /// <remarks>
        /// Add, Remove, Clear 메소드들을 <b>이벤트로 사용하는</b> 인스펙터 시스템 때문에 Clear 후에 Add 하는 것과 지금의 방식과 <b>결과 값이 달라집니다.</b><br/>
        /// 이 코드는 <b>동기화</b>가 목적이기 때문에 Add, RemoveAt를 각각 따로 해줘야 올바르게 동작합니다.<br/><br/>
        /// 또한, 기존에 있는 값은 그 인덱스도 유지해야하기 때문에 이런 보수적인 코드로 가야만 올바르게 동작합니다.
        /// </remarks>
        public static void SyncKeysWithEnumerable<TKey, TValue>(this IDictionary<TKey, TValue> targetDictionary, IEnumerable<TKey> source, Func<TKey, TValue>? valueFactory = null) where TKey : notnull
        {
            // 1. 제거: List에는 없지만 Dictionary에는 있는 키 제거
            // Dictionary의 Keys 컬렉션은 순회 중 수정할 수 없으므로, Except 결과를 List로 만듭니다.
            IEnumerable<TKey> keysToRemoveEnumerable = targetDictionary.Keys.Except(source);
            if (keysToRemoveEnumerable.Any())
            {
                TKey[] keysToRemove = keysToRemoveEnumerable.ToArray();
                foreach (var key in keysToRemove)
                    targetDictionary.Remove(key);
            }

            // 2. 추가: List에는 있지만 Dictionary에는 없는 키 추가
            IEnumerable<TKey> keysToAddEnumerable = source.Except(targetDictionary.Keys);
            if (keysToAddEnumerable.Any())
            {
                TKey[] keysToAdd = keysToAddEnumerable.ToArray();
                foreach (var key in keysToAdd)
                    targetDictionary.Add(key, valueFactory != null ? valueFactory.Invoke(key) : Activator.CreateInstance<TValue>());
            }
        }

        /// <remarks>
        /// Add, RemoveAt, Clear 메소드들을 <b>이벤트로 사용하는</b> 인스펙터 시스템 때문에 Clear 후에 Add 하는 것과 지금의 방식과 <b>결과 값이 달라집니다.</b><br/>
        /// 이 코드는 <b>동기화</b>가 목적이기 때문에 Add, RemoveAt를 각각 따로 해줘야 올바르게 동작합니다.
        /// </remarks>
        public static void SyncWithEnumerable(this IList target, IEnumerable source)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (index < target.Count)
                    target[index] = item;
                else
                    target.Add(item);
                
                index++;
            }
            
            while (index < target.Count)
                target.RemoveAt(index);
        }

        /// <remarks>
        /// Add, RemoveAt, Clear 메소드들을 <b>이벤트로 사용하는</b> 인스펙터 시스템 때문에 Clear 후에 Add 하는 것과 지금의 방식과 <b>결과 값이 달라집니다.</b><br/>
        /// 이 코드는 <b>동기화</b>가 목적이기 때문에 Add, RemoveAt를 각각 따로 해줘야 올바르게 동작합니다.
        /// </remarks>
        public static void SyncWithEnumerable<T>(this IList<T> target, IEnumerable<T> source)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (index < target.Count)
                    target[index] = item;
                else
                    target.Add(item);
                
                index++;
            }
            
            while (index < target.Count)
                target.RemoveAt(index);
        }
    }
}