#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RuniEngine
{
    public static partial class StringUtility
    {
        public const char quotedChar = '"';
        public const char anotherQuotedChar = '\'';

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
        /// (text = "AddSpacesToSentence") = "Add Spaces To Sentence"
        /// </summary>
        /// <param name="text">텍스트</param>
        /// <param name="preserveAcronyms">약어(준말) 보존 (true = (UnscaledFPSDeltaTime = Unscaled FPS Delta Time), false = (UnscaledFPSDeltaTime = Unscaled FPSDelta Time))</param>
        /// <returns></returns>
        public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            StringBuilder newText = StringBuilderCache.Acquire();
            newText.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) || (preserveAcronyms && char.IsUpper(text[i - 1]) && i < text.Length - 1 && !char.IsUpper(text[i + 1]))))
                    newText.Append(' ');

                newText.Append(text[i]);
            }

            return StringBuilderCache.Release(newText);
        }

        public static string[] QuotedSplit(this string text, string separator) => text.EnumerateQuotedSplit(separator).ToArray();

        //https://codereview.stackexchange.com/a/166801
        public static IEnumerable<string> EnumerateQuotedSplit(this string text, string separator)
        {
            const char quote = '\"';

            StringBuilder sb = new StringBuilder(text.Length);
            int counter = 0;
            while (counter < text.Length)
            {
                // if starts with delmiter if so read ahead to see if matches
                if (separator[0] == text[counter] && separator.SequenceEqual(ReadNext(text, counter, separator.Length)))
                {
                    yield return sb.ToString();

                    sb.Clear();
                    counter += separator.Length; // Move the counter past the delimiter 
                }
                else if (text[counter] == quote) // if we hit a quote read until we hit another quote or end of string
                {
                    sb.Append(text[counter++]);
                    while (counter < text.Length && text[counter] != quote)
                        sb.Append(text[counter++]);

                    // if not end of string then we hit a quote add the quote
                    if (counter < text.Length)
                        sb.Append(text[counter++]);
                }
                else
                    sb.Append(text[counter++]);
            }

            if (sb.Length > 0)
                yield return sb.ToString();

            static IEnumerable<char> ReadNext(string str, int currentPosition, int count)
            {
                for (var i = 0; i < count; i++)
                {
                    if (currentPosition + i >= str.Length)
                        yield break;
                    else
                        yield return str[currentPosition + i];
                }
            }
        }

        public static string RemoveWhitespace(this string text) => string.Join(string.Empty, text.Split(string.Empty, StringSplitOptions.RemoveEmptyEntries));

        public static IEnumerable<string> ReadLines(this string text)
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            char lastChar = char.MinValue;
            foreach (var item in text)
            {
                if (item == '\r' || item == '\n')
                {
                    if (lastChar == '\r' && item == '\n')
                        continue;

                    yield return stringBuilder.ToString();
                    stringBuilder.Clear();

                    lastChar = item;
                }

                stringBuilder.Append(item);
            };

            yield return StringBuilderCache.Release(stringBuilder);
        }
    }
}
