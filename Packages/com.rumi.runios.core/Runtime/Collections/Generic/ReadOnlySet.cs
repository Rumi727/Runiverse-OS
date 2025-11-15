using System.Collections;

namespace RuniOS.Collections.Generic
{
    public sealed class ReadOnlySet<T> : ISet<T>, IReadOnlyCollection<T>
    {
        public ReadOnlySet(ISet<T> set) => this.set = set;

        readonly ISet<T> set;

        public int Count => set.Count;
        
        bool ICollection<T>.IsReadOnly => true;

        bool ISet<T>.Add(T item) => throw new NotSupportedException("Collection is read-only.");
        void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only.");
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only.");
        
        void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only.");
        
        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException("Collection is read-only.");
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException("Collection is read-only.");
        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException("Collection is read-only.");
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException("Collection is read-only.");
        
        public bool Contains(T item) => set.Contains(item);
        public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
        
        public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
        
        public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
        
        public void CopyTo(T[] array, int arrayIndex) => set.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}