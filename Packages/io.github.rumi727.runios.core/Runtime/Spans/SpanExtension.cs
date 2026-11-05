#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Spans
{
    public static class ExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSingleSplitter<T> SplitSpans<T>(this ReadOnlySpan<T> source, T separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new ReadOnlySpanSingleSplitter<T>(source, separator, options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSingleSplitter<T> SplitSpans<T>(this Span<T> source, T separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new SpanSingleSplitter<T>(source, separator, options);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSplitter<T> SplitSpans<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new ReadOnlySpanSplitter<T>(source, separator, options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSplitter<T> SplitSpans<T>(this Span<T> source, ReadOnlySpan<T> separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new SpanSplitter<T>(source, separator, options);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanAnySplitter<T> SplitAnySpans<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new ReadOnlySpanAnySplitter<T>(source, separator, options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanAnySplitter<T> SplitAnySpans<T>(this Span<T> source, ReadOnlySpan<T> separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T> => new SpanAnySplitter<T>(source, separator, options);
    }
}
