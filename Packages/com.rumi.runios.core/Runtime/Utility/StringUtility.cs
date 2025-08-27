#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RuniOS
{
    public static partial class StringUtility
    {
        public static readonly char[] splitQuotes = { '\'', '"' };

        /*public static string ConstEnvironmentVariable(this string value)
        {
            value = value.Replace("%DataPath%", Application.dataPath);
            value = value.Replace("%StreamingAssetsPath%", Application.streamingAssetsPath);
            value = value.Replace("%PersistentDataPath%", Application.persistentDataPath);

            value = value.Replace("%CompanyName%", Application.companyName);
            value = value.Replace("%ProductName%", Application.productName);
            value = value.Replace("%Version%", Application.version);

            return value;
        }*/

        /// <summary>
        /// 문자열에 대문자를 기준으로 공백을 추가합니다.
        /// <br/>(예: "AddSpacesToSentence" -> "Add Spaces To Sentence")
        /// <exception cref="ArgumentNullException">입력 문자열이 null일 경우 발생합니다.</exception>
        /// </summary>
        /// <param name="text">변환할 문자열입니다.</param>
        /// <param name="preserveAcronyms">
        /// <see langword="true"/>일 경우 약어(준말)를 보존합니다.
        /// <br/>(예: "UnscaledFPSDeltaTime" -> "Unscaled FPS Delta Time")
        /// <br/><see langword="false"/>일 경우 약어를 분리합니다.
        /// <br/>(예: "UnscaledFPSDeltaTime" -> "Unscaled F P S Delta Time")
        /// </param>
        /// <returns>공백이 추가된 문자열입니다.</returns>
        public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (text.Length == 0)
                return string.Empty;

            StringBuilder newText = StringBuilderCache.Acquire(text.Length * 2);
            newText.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                {
                    if (text[i - 1] != ' ')
                    {
                        if (!char.IsUpper(text[i - 1]) || (preserveAcronyms && i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        {
                            newText.Append(' ');
                        }
                    }
                }
                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        /// <summary>
        /// 큰따옴표 또는 작은따옴표로 감싸진 부분을 제외하고 특정 구분자를 기준으로 문자열을 나눕니다.
        /// <br/>또한, 따옴표 안의 이스케이프 문자(예: <c>\"</c>, <c>\'</c>, <c>\\</c>)를 올바르게 처리하며,
        /// 유효하지 않은 이스케이프 문자는 그대로 유지합니다.
        /// <exception cref="ArgumentNullException">입력 문자열이 null일 경우 발생합니다.</exception>
        /// </summary>
        /// <param name="text">분할할 문자열입니다.</param>
        /// <param name="separator">분할에 사용할 구분자입니다.</param>
        /// <param name="trimEntries">분할된 각 항목의 앞뒤 공백을 제거할지 여부입니다.</param>
        /// <returns>분할된 문자열들의 배열을 반환합니다.</returns>
        public static string[] QuotedSplit(this string text, char separator, bool trimEntries = false) => text.EnumerateQuotedSplit(separator, trimEntries).ToArray();

        /// <summary>
        /// 큰따옴표 또는 작은따옴표로 감싸진 부분을 제외하고 특정 구분자를 기준으로 문자열을 나누는 열거자(IEnumerable)를 반환합니다.
        /// <br/>이 메서드는 문자열 전체를 한 번에 메모리에 로드하지 않아 큰 문자열에 효율적입니다.
        /// <br/>또한, 따옴표 안의 이스케이프 문자(예: <c>\"</c>, <c>\'</c>, <c>\\</c>)를 올바르게 처리하며,
        /// 유효하지 않은 이스케이프 문자는 그대로 유지합니다.
        /// <exception cref="ArgumentNullException">입력 문자열이 null일 경우 발생합니다.</exception>
        /// </summary>
        /// <param name="text">분할할 문자열입니다.</param>
        /// <param name="separator">분할에 사용할 구분자입니다.</param>
        /// <param name="trimEntries">분할된 각 항목의 앞뒤 공백을 제거할지 여부입니다.</param>
        /// <returns>분할된 문자열들을 열거하는 <see cref="IEnumerable{T}"/>를 반환합니다.</returns>
        public static IEnumerable<string> EnumerateQuotedSplit(this string text, char separator, bool trimEntries = false)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            StringBuilder currentPart = StringBuilderCache.Acquire();
            char? currentQuote = null;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 따옴표 시작 및 종료
                if (currentQuote == null && (c == '"' || c == '\''))
                {
                    currentQuote = c;
                    continue; // 따옴표는 결과 문자열에 포함시키지 않음
                }
                else if (currentQuote == c)
                {
                    currentQuote = null;
                    continue; // 따옴표는 결과 문자열에 포함시키지 않음
                }

                // 이스케이프 문자 처리
                if (currentQuote != null && c == '\\' && i + 1 < text.Length)
                {
                    char nextChar = text[i + 1];
                    if (nextChar == currentQuote || nextChar == '\\')
                    {
                        currentPart.Append(nextChar);
                        i++;
                        
                        continue;
                    }
                }

                // 구분자 처리
                if (currentQuote == null && c == separator)
                {
                    string result = currentPart.ToString();
                    yield return trimEntries ? result.Trim() : result;
                    currentPart.Clear();
                }
                else
                    currentPart.Append(c);
            }

            string finalResult = currentPart.ToString();
            yield return trimEntries ? finalResult.Trim() : finalResult;
            
            StringBuilderCache.Release(currentPart);
        }

        /// <summary>
        /// 문자열에서 모든 공백 문자(스페이스, 탭 등)를 제거합니다.
        /// </summary>
        /// <param name="text">공백을 제거할 문자열입니다.</param>
        /// <returns>공백이 제거된 문자열을 반환합니다.</returns>
        public static string RemoveAllWhitespace(this string? text)
        {
            if (text == null)
                return string.Empty;

            return new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// 문자열을 줄 단위로 나누어 열거자(IEnumerable)를 반환합니다.
        /// <br/>줄 바꿈 문자는 CRLF(<c>\r\n</c>), CR(<c>\r</c>), LF(<c>\n</c>)를 모두 인식합니다.
        /// </summary>
        /// <param name="text">줄을 읽어올 문자열입니다.</param>
        /// <returns>한 줄씩 읽어오는 <see cref="IEnumerable{T}"/>를 반환합니다.</returns>
        public static IEnumerable<string> GetLines(this string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (text.Length == 0)
                yield break;

            using var reader = new System.IO.StringReader(text);
            while (reader.ReadLine() is { } line)
                yield return line;
        }
    }
}
