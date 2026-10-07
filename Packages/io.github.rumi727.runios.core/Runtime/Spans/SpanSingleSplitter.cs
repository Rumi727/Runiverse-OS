#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Spans
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref struct SpanSingleSplitter<T>(Span<T> source, T separator, StringSplitOptions options = StringSplitOptions.None) where T : IEquatable<T>
    {
        readonly Span<T> _source = source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_source, separator, options);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(Span<T> source, T separator, StringSplitOptions options)
        {
            int _nextStartIndex = 0;

            readonly Span<T> _source = source;

#pragma warning disable IDE0032 // auto 속성 사용
            Span<T> _current = new Span<T>();
#pragma warning restore IDE0032 // auto 속성 사용

            public Enumerator(Span<T> source, T separator) : this(source, separator, StringSplitOptions.None) { }

            public bool MoveNext()
            {
                while (_nextStartIndex <= _source.Length)
                {
                    Span<T> nextSource = _source.Slice(_nextStartIndex);

                    int foundIndex = nextSource.IndexOf(separator);
                    int length = foundIndex >= 0 ? foundIndex : nextSource.Length;

                    _current = _source.Slice(_nextStartIndex, length);
                    _nextStartIndex += _current.Length + 1;

                    if ((options & StringSplitOptions.RemoveEmptyEntries) != 0 && length == 0)
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
