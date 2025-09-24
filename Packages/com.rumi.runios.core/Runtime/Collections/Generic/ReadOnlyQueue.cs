#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace RuniOS.Collections.Generic
{
    public sealed class ReadOnlyQueue<T> : IReadOnlyCollection<T>
    {
        public ReadOnlyQueue(Queue<T> queue) => this.queue = queue;

        readonly Queue<T> queue;

        public int Count => queue.Count;

        public bool Contains(T item) => queue.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => queue.CopyTo(array, arrayIndex);

        public T Peek() => queue.Peek();
        public T[] ToArray() => queue.ToArray();

        public bool TryPeek(out T result) => queue.TryPeek(out result);
        
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)queue).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)queue).GetEnumerator();
    }
}