#nullable enable
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RuniOS.IO
{
    public static class WildcardUtility
    {
        static readonly ConcurrentDictionary<string, Regex> _regexCache = new();
        
        /// <summary>
        /// 와일드카드 패턴에 따라 문자열이 일치하는지 확인합니다.
        /// '*'는 0개 이상의 문자와 일치하고, '?'는 1개의 문자와 일치합니다.
        /// </summary>
        /// <param name="text">검색할 문자열.</param>
        /// <param name="pattern">와일드카드 패턴.</param>
        /// <param name="ignoreCase">대소문자를 무시할지 여부.</param>
        /// <returns>문자열이 패턴과 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>.</returns>
        public static bool IsMatch(string text, string pattern, bool ignoreCase = false)
        {
            // 캐시 키 생성 (패턴 + 대소문자 옵션)
            string cacheKey = pattern + (ignoreCase ? ":i" : ":s");

            // 캐시에 있으면 가져오고, 없으면 새로 생성 (스레드 안전)
            Regex regex = _regexCache.GetOrAdd(cacheKey, _ =>
            {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                
                RegexOptions options = RegexOptions.Compiled | RegexOptions.ExplicitCapture;
                if (ignoreCase)
                    options |= RegexOptions.IgnoreCase;

                return new Regex(regexPattern, options);
            });

            return regex.IsMatch(text);
        }

        /// <summary>
        /// 와일드카드 패턴에 따라 문자열이 일치하는지 확인합니다.
        /// '*'는 0개 이상의 문자와 일치하고, '?'는 1개의 문자와 일치합니다.
        /// </summary>
        /// <param name="text">검색할 문자열.</param>
        /// <param name="patterns">와일드카드 패턴.</param>
        /// <param name="ignoreCase">대소문자를 무시할지 여부.</param>
        /// <returns>문자열이 패턴과 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>.</returns>
        public static bool IsMatch(string text, WildcardPatterns patterns, bool ignoreCase = false)
        {
            for (int i = 0; i < patterns.patterns.Length; i++)
            {
                if (IsMatch(text, patterns[i], ignoreCase))
                    return true;
            }

            return false;
        }
    }
}