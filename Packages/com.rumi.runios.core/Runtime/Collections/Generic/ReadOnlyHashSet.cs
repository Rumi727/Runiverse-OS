using System.Collections;
using System.Collections.Generic;

namespace RuniOS.Collections.Generic
{
    public sealed class ReadOnlyHashSet<T> : IReadOnlyCollection<T>
    {
        public ReadOnlyHashSet(HashSet<T> hashset) => this.hashset = hashset;

        readonly HashSet<T> hashset;

        public int Count => hashset.Count;

        public bool Contains(T item) => hashset.Contains(item);

        public void CopyTo(T[] array) => hashset.CopyTo(array);
        public void CopyTo(T[] array, int arrayIndex) => hashset.CopyTo(array, arrayIndex);
        public void CopyTo(T[] array, int arrayIndex, int count) => hashset.CopyTo(array, arrayIndex, count);

        public bool Overlaps(IEnumerable<T> other) => hashset.Overlaps(other);
        public bool TryGetValue(T equalValue, out T actualValue) => hashset.TryGetValue(equalValue, out actualValue);

        public IEnumerator<T> GetEnumerator() => hashset.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}