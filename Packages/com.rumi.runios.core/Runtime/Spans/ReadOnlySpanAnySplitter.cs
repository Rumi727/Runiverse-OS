#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Spans;

public readonly ref struct ReadOnlySpanAnySplitter<T> where T : IEquatable<T>
{
    readonly ReadOnlySpan<T> _source;
    readonly ReadOnlySpan<T> _separator;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpanAnySplitter(ReadOnlySpan<T> source, ReadOnlySpan<T> separator)
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
        ReadOnlySpan<T> _current;
#pragma warning restore IDE0032 // auto 속성 사용

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ReadOnlySpan<T> source, ReadOnlySpan<T> separator)
        {
            if (separator.Length == 0)
                throw new ArgumentException("Requires non-empty value", nameof(separator));

            _nextStartIndex = 0;

            _source = source;
            _separator = separator;

            _current = new ReadOnlySpan<T>();
        }

        public bool MoveNext()
        {
            if (_nextStartIndex > _source.Length)
                return false;

            ReadOnlySpan<T> nextSource = _source.Slice(_nextStartIndex);

            int foundIndex = nextSource.IndexOfAny(_separator);
            int length = foundIndex >= 0 ? foundIndex : nextSource.Length;

            _current = _source.Slice(_nextStartIndex, length);
            _nextStartIndex += _current.Length + 1;

            return true;
        }

#pragma warning disable IDE1006 // 명명 스타일
        // ReSharper disable once InconsistentNaming
        public readonly ReadOnlySpan<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
#pragma warning restore IDE1006 // 명명 스타일
    }
}