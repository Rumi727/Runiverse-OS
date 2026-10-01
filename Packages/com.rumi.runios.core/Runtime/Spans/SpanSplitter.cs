#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Spans
{
    public readonly ref struct SpanSplitter<T> where T : IEquatable<T>
    {
        readonly Span<T> _source;
        readonly ReadOnlySpan<T> _separator;
        readonly StringSplitOptions _options;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSplitter(Span<T> source, ReadOnlySpan<T> separator, StringSplitOptions options = StringSplitOptions.None)
        {
            if (separator.Length == 0)
                throw new ArgumentException("Requires non-empty value", nameof(separator));

            _source = source;
            _separator = separator;
            _options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_source, _separator, _options);

        public ref struct Enumerator
        {
            int _nextStartIndex;

            readonly Span<T> _source;
            readonly ReadOnlySpan<T> _separator;
            readonly StringSplitOptions _options;

#pragma warning disable IDE0032 // auto 속성 사용
            Span<T> _current;
#pragma warning restore IDE0032 // auto 속성 사용

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Span<T> source, ReadOnlySpan<T> separator) : this(source, separator, StringSplitOptions.None) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Span<T> source, ReadOnlySpan<T> separator, StringSplitOptions options)
            {
                if (separator.Length == 0)
                    throw new ArgumentException("Requires non-empty value", nameof(separator));

                _nextStartIndex = 0;

                _source = source;
                _separator = separator;
                _options = options;

                _current = new Span<T>();
            }

            public bool MoveNext()
            {
                while (_nextStartIndex <= _source.Length)
                {
                    Span<T> nextSource = _source.Slice(_nextStartIndex);

                    int foundIndex = nextSource.IndexOf(_separator);
                    int length = foundIndex >= 0 ? foundIndex : nextSource.Length;

                    _current = _source.Slice(_nextStartIndex, length);
                    _nextStartIndex += _separator.Length + _current.Length;

                    if ((_options & StringSplitOptions.RemoveEmptyEntries) != 0 && length == 0)
                        continue;

                    return true;
                }

                return false;
            }

#pragma warning disable IDE1006 // 명명 스타일
            // ReSharper disable once InconsistentNaming
            public readonly Span<T> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
#pragma warning restore IDE1006 // 명명 스타일
        }
    }
}
