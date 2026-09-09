using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Collections.Immutable;

public readonly struct ImmutableEquatableArray<T>(ImmutableArray<T> array) : IReadOnlyList<T>, IEquatable<ImmutableEquatableArray<T>>
{
    readonly ImmutableArray<T> _array = array;
    public ImmutableArray<T> array => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public T this[int index] => array[index];

    public int length => array.Length;
    int IReadOnlyCollection<T>.Count => array.Length;

    public bool isEmpty => array.IsEmpty;

    public ref readonly T ItemRef(int index) => ref array.ItemRef(index);

    public ImmutableArray<T>.Enumerator GetEnumerator() => array.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)array).GetEnumerator();

    public bool Equals(ImmutableEquatableArray<T> other)
    {
        if (length != other.length)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < length; i++)
        {
            if (!comparer.Equals(array[i], other.array[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is ImmutableEquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
        foreach (T item in array)
            hashCode.Add(item);

        return hashCode.ToHashCode();
    }

    public static bool operator ==(ImmutableEquatableArray<T> left, ImmutableEquatableArray<T> right) => left.Equals(right);
    public static bool operator !=(ImmutableEquatableArray<T> left, ImmutableEquatableArray<T> right) => !left.Equals(right);

    public static implicit operator ImmutableEquatableArray<T>(ImmutableArray<T> array) => new(array);
    public static implicit operator ImmutableArray<T>(ImmutableEquatableArray<T> array) => array._array;
}