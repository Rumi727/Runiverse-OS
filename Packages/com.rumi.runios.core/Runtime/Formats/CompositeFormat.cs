#nullable enable
using System.Text;

namespace RuniOS.Formats
{
    /// <summary>
    /// Parses .NET composite format strings into literal and format hole segments.<br/>
    /// .NET composite format 문자열을 literal 및 format hole 세그먼트로 파싱합니다.
    /// </summary>
    public static class CompositeFormat
    {
        /// <summary>
        /// Parses the specified composite format string into literal or format hole segments.<br/>
        /// 지정된 composite format 문자열을 literal 또는 format hole 세그먼트로 파싱합니다.
        /// </summary>
        /// <param name="format">
        /// The composite format string to parse.<br/>
        /// 파싱할 composite format 문자열입니다.
        /// </param>
        /// <returns>
        /// An array containing the parsed composite format segments.<br/>
        /// 파싱된 composite format 세그먼트를 포함하는 배열을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="format"/> is <see langword="null"/>.<br/>
        /// <paramref name="format"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when a format item in <paramref name="format"/> is invalid.<br/>
        /// <paramref name="format"/>의 format item이 유효하지 않은 경우 발생합니다.
        /// </exception>
        public static CompositeFormatSegment[] Parse(string format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            List<CompositeFormatSegment> segments = [];
            if (!TryParse(format, segments, out int failureOffset, out ParseFailure failure))
                throw new FormatException(GetFailureMessage(failure, failureOffset));

            return segments.ToArray();
        }

        static bool TryParse(string format, List<CompositeFormatSegment> segments, out int failureOffset, out ParseFailure failure)
        {
            StringBuilder literalBuilder = new();
            int pos = 0;

            while (true)
            {
                char ch;
                while (true)
                {
                    int countUntilNextBrace = IndexOfAnyBrace(format, pos);
                    if (countUntilNextBrace < 0)
                    {
                        literalBuilder.Append(format, pos, format.Length - pos);
                        segments.Add(new CompositeFormatSegment(literalBuilder.ToString()));
                        failureOffset = 0;
                        failure = ParseFailure.None;
                        return true;
                    }

                    literalBuilder.Append(format, pos, countUntilNextBrace);
                    pos += countUntilNextBrace;

                    char brace = format[pos];
                    if (!TryMoveNext(format, ref pos, out ch))
                        return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);

                    if (brace == ch)
                    {
                        literalBuilder.Append(ch);
                        pos++;
                        continue;
                    }

                    if (brace != '{')
                        return Fail(ParseFailure.UnexpectedClosingBrace, pos, out failureOffset, out failure);

                    segments.Add(new CompositeFormatSegment(literalBuilder.ToString()));
                    literalBuilder.Length = 0;
                    break;
                }

                int alignment = 0;
                string? itemFormat = null;

                int index = ch - '0';
                if ((uint)index >= 10u)
                    return Fail(ParseFailure.ExpectedAsciiDigit, pos, out failureOffset, out failure);

                if (!TryMoveNext(format, ref pos, out ch))
                    return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);

                if (ch != '}')
                {
                    while (IsAsciiDigit(ch))
                    {
                        index = ((index * 10) + ch) - '0';
                        if (!TryMoveNext(format, ref pos, out ch))
                            return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                    }

                    while (ch == ' ')
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                            return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                    }

                    if (ch == ',')
                    {
                        do
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                        }
                        while (ch == ' ');

                        int leftJustify = 1;
                        if (ch == '-')
                        {
                            leftJustify = -1;
                            if (!TryMoveNext(format, ref pos, out ch))
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                        }

                        alignment = ch - '0';
                        if ((uint)alignment >= 10u)
                            return Fail(ParseFailure.ExpectedAsciiDigit, pos, out failureOffset, out failure);

                        if (!TryMoveNext(format, ref pos, out ch))
                            return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);

                        while (IsAsciiDigit(ch))
                        {
                            alignment = ((alignment * 10) + ch) - '0';
                            if (!TryMoveNext(format, ref pos, out ch))
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                        }

                        alignment *= leftJustify;

                        while (ch == ' ')
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                        }
                    }

                    if (ch != '}')
                    {
                        if (ch != ':')
                            return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);

                        int startingPos = pos;
                        while (true)
                        {
                            if (!TryMoveNext(format, ref pos, out ch))
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);

                            if (ch == '}')
                                break;

                            if (ch == '{')
                                return Fail(ParseFailure.UnclosedFormatItem, pos, out failureOffset, out failure);
                        }

                        startingPos++;
                        itemFormat = format.Substring(startingPos, pos - startingPos);
                    }
                }

                pos++;
                segments.Add(new CompositeFormatSegment(index, alignment, itemFormat));
            }
        }

        static int IndexOfAnyBrace(string format, int startIndex)
        {
            for (int i = startIndex; i < format.Length; i++)
            {
                if (format[i] == '{' || format[i] == '}')
                    return i - startIndex;
            }

            return -1;
        }

        static bool TryMoveNext(string format, ref int pos, out char nextChar)
        {
            pos++;
            if ((uint)pos >= (uint)format.Length)
            {
                nextChar = '\0';
                return false;
            }

            nextChar = format[pos];
            return true;
        }

        static bool IsAsciiDigit(char ch) => (uint)(ch - '0') <= 9u;

        static bool Fail(ParseFailure failure, int offset, out int failureOffset, out ParseFailure failureReason)
        {
            failureOffset = offset;
            failureReason = failure;
            return false;
        }

        static string GetFailureMessage(ParseFailure failure, int offset) => failure switch
        {
            ParseFailure.UnexpectedClosingBrace => $"Unexpected closing brace at offset {offset}.",
            ParseFailure.ExpectedAsciiDigit => $"Expected ASCII digit at offset {offset}.",
            _ => $"Unclosed format item at offset {offset}."
        };

        enum ParseFailure
        {
            None,
            UnexpectedClosingBrace,
            UnclosedFormatItem,
            ExpectedAsciiDigit
        }
    }
}
