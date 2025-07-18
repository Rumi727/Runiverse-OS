#nullable enable
using System.Collections.Concurrent;
using System.Text;

namespace RuniEngine
{
    public static class StringBuilderCache
    {
        static readonly ConcurrentBag<StringBuilder> cachedStringBuilders = new ConcurrentBag<StringBuilder>();

        public static StringBuilder Acquire() => Acquire(string.Empty);
        public static StringBuilder Acquire(string value)
        {
            if (cachedStringBuilders.TryTake(out StringBuilder? result))
            {
                if (result != null)
                {
                    result.Clear();
                    return result.Append(value);
                }
            }

            return new StringBuilder(value);
        }

        public static string Release(StringBuilder builder)
        {
            string result = builder.ToString();

            builder.Clear();
            cachedStringBuilders.Add(builder);

            return result;
        }

        public static void Clear() => cachedStringBuilders.Clear();
    }
}
