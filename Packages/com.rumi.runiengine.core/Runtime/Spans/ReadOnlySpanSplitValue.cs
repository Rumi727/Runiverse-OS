using System;

namespace RuniEngine.Spans
{
    public readonly ref struct ReadOnlySpanSplitValue<T> where T : IEquatable<T>
    {
        public ReadOnlySpanSplitValue(ReadOnlySpan<T> source, int startIndex, int length)
        {
            Source = source;

            StartIndex = startIndex;
            Length = length;
        }

        public ReadOnlySpan<T> Source { get; }

        public int StartIndex { get; }
        public int Length { get; }

        public ReadOnlySpan<T> AsSpan() => Source.Slice(StartIndex, Length);

        public static implicit operator ReadOnlySpan<T>(ReadOnlySpanSplitValue<T> value) => value.AsSpan();
    }
}
