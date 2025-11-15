#nullable enable
using System.Collections;

namespace RuniOS.Collections.Generic;

public sealed class ReadOnlyStack<T> : ICollection, IReadOnlyCollection<T>
{
    public ReadOnlyStack(Stack<T> stack) => this.stack = stack;

    readonly Stack<T> stack;

    public int Count => stack.Count;
        
    bool ICollection.IsSynchronized => ((ICollection)stack).IsSynchronized;
    object ICollection.SyncRoot => ((ICollection)stack).SyncRoot;

    public bool Contains(T item) => stack.Contains(item);
        
    public void CopyTo(T[] array, int arrayIndex) => stack.CopyTo(array, arrayIndex);
    void ICollection.CopyTo(Array array, int index) => ((ICollection)stack).CopyTo(array, index);

    public T Peek() => stack.Peek();
    public T[] ToArray() => stack.ToArray();

    public bool TryPeek(out T result) => stack.TryPeek(out result);
        
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)stack).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)stack).GetEnumerator();
}