#nullable enable
using System.Collections.Generic;

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
    }
}
