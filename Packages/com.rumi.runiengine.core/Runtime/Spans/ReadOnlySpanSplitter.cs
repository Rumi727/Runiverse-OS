using System;
using System.Runtime.CompilerServices;

namespace RuniEngine.Spans
{
    public readonly ref struct ReadOnlySpanSplitter<T> where T : IEquatable<T>
    {
        readonly ReadOnlySpan<T> _source;
        readonly ReadOnlySpan<T> _separator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSplitter(ReadOnlySpan<T> source, ReadOnlySpan<T> separator)
        {
            if (separator.Length == 0)
                throw new ArgumentException("Requires non-empty value", nameof(separator));

            _source = source;
            _separator = separator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_source, _separator);

        public ref struct Enumerator
        {
            int _nextStartIndex;

            readonly ReadOnlySpan<T> _source;
            readonly ReadOnlySpan<T> _separator;

#pragma warning disable IDE0032 // auto 속성 사용
            ReadOnlySpanSplitValue<T> _current;
#pragma warning restore IDE0032 // auto 속성 사용

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(ReadOnlySpan<T> source, ReadOnlySpan<T> separator)
            {
                if (separator.Length == 0)
                    throw new ArgumentException("Requires non-empty value", nameof(separator));

                _nextStartIndex = 0;

                _source = source;
                _separator = separator;

                _current = new ReadOnlySpanSplitValue<T>();
            }

            public bool MoveNext()
            {
                if (_nextStartIndex > _source.Length)
                    return false;

                ReadOnlySpan<T> nextSource = _source.Slice(_nextStartIndex);

                int foundIndex = nextSource.IndexOf(_separator);
                int length = foundIndex >= 0 ? foundIndex : nextSource.Length;

                _current = new ReadOnlySpanSplitValue<T>(_source, _nextStartIndex, length);
                _nextStartIndex += _separator.Length + _current.Length;

                return true;
            }

#pragma warning disable IDE1006 // 명명 스타일
            public readonly ReadOnlySpanSplitValue<T> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
#pragma warning restore IDE1006 // 명명 스타일
        }
    }
}
