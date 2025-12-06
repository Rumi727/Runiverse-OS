#nullable enable
using System.Collections;

namespace RuniOS.Collections.Generic
{
    public sealed class ReadOnlyQueue<T> : ICollection, IReadOnlyCollection<T>
    {
        public static ReadOnlyQueue<T> empty { get; } = new ReadOnlyQueue<T>(new Queue<T>());
        
        public ReadOnlyQueue(Queue<T> queue) => this.queue = queue;

        readonly Queue<T> queue;

        public int Count => queue.Count;
        
        bool ICollection.IsSynchronized => ((ICollection)queue).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)queue).SyncRoot;

        public bool Contains(T item) => queue.Contains(item);
        
        public void CopyTo(T[] array, int arrayIndex) => queue.CopyTo(array, arrayIndex);
        void ICollection.CopyTo(Array array, int index) => ((ICollection)queue).CopyTo(array, index);

        public T Peek() => queue.Peek();
        public T[] ToArray() => queue.ToArray();

        public bool TryPeek(out T result) => queue.TryPeek(out result);
        
        public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}