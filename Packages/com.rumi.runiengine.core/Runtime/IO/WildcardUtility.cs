#nullable enable
using System.Text.RegularExpressions;

namespace RuniEngine.IO
{
    public static class WildcardUtility
    {
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
            // 와일드카드 패턴을 정규식 패턴으로 변환
            // '.' 문자를 이스케이프하여 리터럴 점으로 취급
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";

            // 정규식 옵션 설정 (대소문자 무시 등)
            RegexOptions options = RegexOptions.None;
            if (ignoreCase)
                options |= RegexOptions.IgnoreCase;

            return Regex.IsMatch(text, regexPattern, options);
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
            for (int i = 0; i < patterns.patterns.Count; i++)
            {
                if (IsMatch(text, patterns[i], ignoreCase))
                    return true;
            }

            return false;
        }
    }
}
