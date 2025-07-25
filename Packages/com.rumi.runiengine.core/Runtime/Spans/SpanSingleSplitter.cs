#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace RuniEngine.Spans
{
    public readonly ref struct SpanSingleSplitter<T> where T : IEquatable<T>
    {
        readonly Span<T> _source;
        readonly T _separator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSingleSplitter(Span<T> source, T separator)
        {
            _source = source;
            _separator = separator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_source, _separator);

        public ref struct Enumerator
        {
            int _nextStartIndex;

            readonly Span<T> _source;
            readonly T _separator;

#pragma warning disable IDE0032 // auto 속성 사용
            SpanSplitValue<T> _current;
#pragma warning restore IDE0032 // auto 속성 사용

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Span<T> source, T separator)
            {
                _nextStartIndex = 0;

                _source = source;
                _separator = separator;

                _current = new SpanSplitValue<T>();
            }

            public bool MoveNext()
            {
                if (_nextStartIndex > _source.Length)
                    return false;

                Span<T> nextSource = _source.Slice(_nextStartIndex);

                int foundIndex = nextSource.IndexOf(_separator);
                int length = foundIndex >= 0 ? foundIndex : nextSource.Length;

                _current = new SpanSplitValue<T>(_source, _nextStartIndex, length);
                _nextStartIndex += _current.Length + 1;

                return true;
            }

#pragma warning disable IDE1006 // 명명 스타일
            // ReSharper disable once InconsistentNaming
            public readonly SpanSplitValue<T> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
#pragma warning restore IDE1006 // 명명 스타일
        }
    }
}
