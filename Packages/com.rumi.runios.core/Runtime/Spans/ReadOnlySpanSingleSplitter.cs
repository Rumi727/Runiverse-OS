#nullable enable
using System.Runtime.CompilerServices;

namespace RuniOS.Spans
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref struct ReadOnlySpanSingleSplitter<T>(ReadOnlySpan<T> source, T separator) where T : IEquatable<T>
    {
        readonly ReadOnlySpan<T> _source = source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(_source, separator);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref struct Enumerator(ReadOnlySpan<T> source, T separator)
        {
            int _nextStartIndex = 0;

            readonly ReadOnlySpan<T> _source = source;

#pragma warning disable IDE0032 // auto 속성 사용
            ReadOnlySpan<T> _current = new ReadOnlySpan<T>();
#pragma warning restore IDE0032 // auto 속성 사용

            public bool MoveNext()
            {
                if (_nextStartIndex > _source.Length)
                    return false;

                ReadOnlySpan<T> nextSource = _source.Slice(_nextStartIndex);

                int foundIndex = nextSource.IndexOf(separator);
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
}