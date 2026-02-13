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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            object? temp = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, temp);
        }

        public static void Change(this IList list, int oldIndex, int newIndex)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            
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
        public static sbyte CloseValue(this IEnumerable<sbyte> list, sbyte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static byte CloseValue(this IEnumerable<byte> list, byte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target) < (y - target) ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static short CloseValue(this IEnumerable<short> list, short target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static ushort CloseValue(this IEnumerable<ushort> list, ushort target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target) < (y - target) ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static int CloseValue(this IEnumerable<int> list, int target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static uint CloseValue(this IEnumerable<uint> list, uint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target) < (y - target) ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static long CloseValue(this IEnumerable<long> list, long target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static ulong CloseValue(this IEnumerable<ulong> list, ulong target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target) < (y - target) ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static float CloseValue(this IEnumerable<float> list, float target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static double CloseValue(this IEnumerable<double> list, double target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static decimal CloseValue(this IEnumerable<decimal> list, decimal target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static BigInteger CloseValue(this IEnumerable<BigInteger> list, BigInteger target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static BigDecimal CloseValue(this IEnumerable<BigDecimal> list, BigDecimal target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static nint CloseValue(this IEnumerable<nint> list, nint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target).Abs() < (y - target).Abs() ? x : y);

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <returns></returns>
        public static nuint CloseValue(this IEnumerable<nuint> list, nuint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
                return list.Aggregate((x, y) => (x - target) < (y - target) ? x : y);

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
        public static sbyte CloseValue<T>(this IEnumerable<T> list, sbyte target, Func<T, sbyte> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                sbyte val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    sbyte currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static byte CloseValue<T>(this IEnumerable<T> list, byte target, Func<T, byte> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                byte val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    byte currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target) < (currentNumber - target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static short CloseValue<T>(this IEnumerable<T> list, short target, Func<T, short> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                short val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    short currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static ushort CloseValue<T>(this IEnumerable<T> list, ushort target, Func<T, ushort> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                ushort val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    ushort currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target) < (currentNumber - target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static int CloseValue<T>(this IEnumerable<T> list, int target, Func<T, int> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                int val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    int currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static uint CloseValue<T>(this IEnumerable<T> list, uint target, Func<T, uint> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                uint val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    uint currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target) < (currentNumber - target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static long CloseValue<T>(this IEnumerable<T> list, long target, Func<T, long> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                long val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    long currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static ulong CloseValue<T>(this IEnumerable<T> list, ulong target, Func<T, ulong> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                ulong val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    ulong currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target) < (currentNumber - target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static float CloseValue<T>(this IEnumerable<T> list, float target, Func<T, float> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                float val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    float currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static double CloseValue<T>(this IEnumerable<T> list, double target, Func<T, double> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                double val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    double currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static decimal CloseValue<T>(this IEnumerable<T> list, decimal target, Func<T, decimal> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                decimal val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    decimal currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static BigInteger CloseValue<T>(this IEnumerable<T> list, BigInteger target, Func<T, BigInteger> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                BigInteger val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    BigInteger currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static nint CloseValue<T>(this IEnumerable<T> list, nint target, Func<T, nint> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                nint val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    nint currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target).Abs() < (currentNumber - target).Abs() ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <returns></returns>
        public static nuint CloseValue<T>(this IEnumerable<T> list, nuint target, Func<T, nuint> getNumberFunc)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();
                enumerator.MoveNext();

                nuint val = getNumberFunc(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    nuint currentNumber = getNumberFunc(enumerator.Current);
                    val = (val - target) < (currentNumber - target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }
        #endregion

        #region Close Value Predicate
        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static sbyte CloseValue<T>(this IEnumerable<T> list, sbyte target, Func<T, sbyte> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                sbyte val = 0;
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

                    sbyte currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static byte CloseValue<T>(this IEnumerable<T> list, byte target, Func<T, byte> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                byte val = 0;
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

                    byte currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static short CloseValue<T>(this IEnumerable<T> list, short target, Func<T, short> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                short val = 0;
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

                    short currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static ushort CloseValue<T>(this IEnumerable<T> list, ushort target, Func<T, ushort> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                ushort val = 0;
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

                    ushort currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static int CloseValue<T>(this IEnumerable<T> list, int target, Func<T, int> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                int val = 0;
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

                    int currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static uint CloseValue<T>(this IEnumerable<T> list, uint target, Func<T, uint> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                uint val = 0;
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

                    uint currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static long CloseValue<T>(this IEnumerable<T> list, long target, Func<T, long> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                long val = 0;
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

                    long currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static ulong CloseValue<T>(this IEnumerable<T> list, ulong target, Func<T, ulong> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                ulong val = 0;
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

                    ulong currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static float CloseValue<T>(this IEnumerable<T> list, float target, Func<T, float> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                float val = 0;
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

                    float currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static double CloseValue<T>(this IEnumerable<T> list, double target, Func<T, double> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                double val = 0;
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

                    double currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static decimal CloseValue<T>(this IEnumerable<T> list, decimal target, Func<T, decimal> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                decimal val = 0;
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

                    decimal currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static BigInteger CloseValue<T>(this IEnumerable<T> list, BigInteger target, Func<T, BigInteger> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                BigInteger val = 0;
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

                    BigInteger currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static nint CloseValue<T>(this IEnumerable<T> list, nint target, Func<T, nint> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                nint val = 0;
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

                    nint currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }

        /// <summary>
        /// 가장 가까운 수를 찾습니다
        /// </summary>
        /// <param name="list">리스트</param>
        /// <param name="target">기준</param>
        /// <param name="getNumberFunc">리스트에서 숫자를 가져올 함수</param>
        /// <param name="predicate">조건</param>
        /// <returns></returns>
        public static nuint CloseValue<T>(this IEnumerable<T> list, nuint target, Func<T, nuint> getNumberFunc, Predicate<T> predicate)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Any())
            {
                using IEnumerator<T> enumerator = list.GetEnumerator();

                nuint val = 0;
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

                    nuint currentNumber = getNumberFunc(enumerator.Current);
                    val = val.Distance(target) < currentNumber.Distance(target) ? val : currentNumber;
                }

                return val;
            }

            return 0;
        }
        #endregion

        #region Close Value Index
        /// <summary>
        /// 가장 가까운 수를 찾고 인덱스를 반환합니다
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int CloseValueIndex(this IList<sbyte> list, sbyte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<byte> list, byte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<short> list, short target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<ushort> list, ushort target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<int> list, int target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<uint> list, uint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<long> list, long target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<ulong> list, ulong target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<float> list, float target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<double> list, double target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<decimal> list, decimal target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<BigInteger> list, BigInteger target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<nint> list, nint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndex(this IList<nuint> list, nuint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count > 0)
                return list.IndexOf(list.CloseValue(target));

            return 0;
        }
        #endregion

        #region Close Value Index Binary Search
        /// <summary>
        /// 가장 가까운 수를 찾고 이진 검색으로 인덱스를 반환합니다
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int CloseValueIndexBinarySearch(this List<sbyte> list, sbyte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<byte> list, byte target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<short> list, short target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<ushort> list, ushort target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<int> list, int target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<uint> list, uint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<long> list, long target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<ulong> list, ulong target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<float> list, float target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<double> list, double target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<decimal> list, decimal target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<BigInteger> list, BigInteger target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<nint> list, nint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
        public static int CloseValueIndexBinarySearch(this List<nuint> list, nuint target)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count > 0)
                return list.BinarySearch(list.CloseValue(target));

            return 0;
        }
        #endregion

        #region Deduplicate
        public static void Deduplicate(this IList<float> values, float delta)
        {
            int index = 0;
            while (index < values.Count)
            {
                float value = values[index];

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

        public static void Deduplicate(this IList<float> values, float delta, float setValue)
        {
            int index = 0;
            while (index < values.Count)
            {
                float value = values[index];

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

        public static void Deduplicate(this IList<double> values, double delta)
        {
            int index = 0;
            while (index < values.Count)
            {
                double value = values[index];

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

        public static void Deduplicate(this IList<double> values, double delta, double setValue)
        {
            int index = 0;
            while (index < values.Count)
            {
                double value = values[index];

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

        public static void Deduplicate(this IList<decimal> values, decimal delta)
        {
            int index = 0;
            while (index < values.Count)
            {
                decimal value = values[index];

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

        public static void Deduplicate(this IList<decimal> values, decimal delta, decimal setValue)
        {
            int index = 0;
            while (index < values.Count)
            {
                decimal value = values[index];

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

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using IEnumerator<TSource> sourceIterator = source.GetEnumerator();
            if (!sourceIterator.MoveNext())
                throw new InvalidOperationException("Empty sequence");

            var comparer = Comparer<TKey>.Default;
            TSource min = sourceIterator.Current;
            TKey minKey = selector(min);

            while (sourceIterator.MoveNext())
            {
                TSource current = sourceIterator.Current;
                TKey currentKey = selector(current);

                if (comparer.Compare(currentKey, minKey) >= 0)
                    continue;

                min = current;
                minKey = currentKey;
            }

            return min;
        }
        
        public static IEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> sources, Func<T, string> selector) => sources.OrderByAlphaNumeric(selector, StringComparer.CurrentCulture);

        public static IEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> sources, Func<T, string> selector, StringComparer comparer)
        {
            var regex = new Regex(@"\d+", RegexOptions.Compiled);

            int maxDigits = sources
                .SelectMany(x => regex.Matches(selector(x)).Select(digitChunk => (int?)digitChunk.Value.Length))
                .Max() ?? 0;

            return sources.OrderBy(x => regex.Replace(selector(x), match => match.Value.PadLeft(maxDigits, '0')), comparer);
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
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            object? temp = list.GetValue(oldIndex);
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, temp);

            return list;
        }
        
        public static T[] Move<T>(this T[] list, int oldIndex, int newIndex)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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
            Array result = Array.CreateInstance(array.GetType().GetElementType() ?? throw new InvalidOperationException(), array.Length);
            Array.Copy(array, result, array.Length);

            return result;
        }

        public static T[] Copy<T>(this T[] array)
        {
            T[] result = new T[array.Length];
            Array.Copy(array, result, array.Length);

            return result;
        }
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