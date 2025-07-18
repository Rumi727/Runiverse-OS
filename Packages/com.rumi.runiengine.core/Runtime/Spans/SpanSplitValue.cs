using System;

namespace RuniEngine.Spans
{
    public readonly ref struct SpanSplitValue<T> where T : IEquatable<T>
    {
        public SpanSplitValue(Span<T> source, int startIndex, int length)
        {
            Source = source;

            StartIndex = startIndex;
            Length = length;
        }

        public Span<T> Source { get; }

        public int StartIndex { get; }
        public int Length { get; }

        public Span<T> AsSpan() => Source.Slice(StartIndex, Length);

        public static implicit operator Span<T>(SpanSplitValue<T> value) => value.AsSpan();
    }
}
