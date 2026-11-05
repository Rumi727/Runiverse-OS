#nullable enable
// Source : https://source.dot.net/#System.Net.Primitives/src/runtime/src/libraries/Common/src/System/Text/StringBuilderCache.cs
using System.Text;

namespace RuniOS
{
    /// <summary>
    /// Provides a cached reusable <see cref="StringBuilder"/> per thread.<br/>
    /// 스레드마다 재사용 가능한 <see cref="StringBuilder"/> 하나를 캐시해서 제공합니다.
    /// </summary>
    public static class StringBuilderCache
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as little memory per thread as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        const int MaxBuilderSize = 360;
        const int DefaultCapacity = 16; // == StringBuilder.DefaultCapacity

        [ThreadStatic]
        static StringBuilder? cachedInstance;

        /// <summary>
        /// Gets a <see cref="StringBuilder"/> with at least the specified capacity.<br/>
        /// If a <see cref="StringBuilder"/> of an appropriate size is cached, it is returned and the cache is emptied.
        /// <br/><br/>
        /// 지정한 용량 이상의 <see cref="StringBuilder"/>를 가져옵니다.<br/>
        /// 적당한 크기의 <see cref="StringBuilder"/>가 캐시되어 있으면 그것을 반환하고 캐시를 비웁니다.
        /// </summary>
        /// <param name="capacity">
        /// The minimum capacity of the returned builder.<br/>
        /// 반환할 빌더의 최소 용량입니다.
        /// </param>
        public static StringBuilder Acquire(int capacity = DefaultCapacity)
        {
            if (capacity <= MaxBuilderSize)
            {
                StringBuilder? sb = cachedInstance;
                if (sb != null)
                {
                    // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                    // when the requested size is larger than the current capacity
                    if (capacity <= sb.Capacity)
                    {
                        cachedInstance = null;
                        sb.Clear();
                        return sb;
                    }
                }
            }

            return new StringBuilder(capacity);
        }

        /// <summary>
        /// Places the specified builder in the cache if it is not too big.<br/>
        /// 지정한 빌더가 너무 크지 않으면 캐시에 넣습니다.
        /// </summary>
        /// <param name="sb">
        /// The builder to cache.<br/>
        /// 캐시할 빌더입니다.
        /// </param>
        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MaxBuilderSize)
                cachedInstance = sb;
        }

        /// <summary>
        /// Calls <see cref="object.ToString"/> on the builder, releases it to the cache, and returns the resulting string.<br/>
        /// 빌더의 <see cref="object.ToString"/>을 호출하고, 캐시에 반환한 뒤 결과 문자열을 돌려줍니다.
        /// </summary>
        /// <param name="sb">
        /// The builder to convert and release.<br/>
        /// 문자열로 만들고 반환할 빌더입니다.
        /// </param>
        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            return result;
        }
    }
}
