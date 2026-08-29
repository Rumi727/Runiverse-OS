#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Generic
{
    [CollectionHandler(typeof(IList<>), useForChildren = true)]
    public class IListHandler<T>(IEnumerable targetCollection) : ListHandlerBase(targetCollection)
    {
        public override object? this[int index]
        {
            get => ((IList<T>)targetCollection)[index];
            set => ((IList<T>)targetCollection)[index] = (T)value!;
        }

        public override int count => ((ICollection<T>)targetCollection).Count;

        public override bool isReadOnly => ((ICollection<T>)targetCollection).IsReadOnly;
        public override bool isFixedSize => isReadOnly;

        public override int Add(object? value)
        {
            ((ICollection<T>)targetCollection).Add((T)value!);
            return count - 1;
        }

        public override void Insert(int index, object? value) => ((IList<T>)targetCollection).Insert(index, (T)value!);

        public override void Remove(object? value) => ((ICollection<T>)targetCollection).Remove((T)value!);
        public override void RemoveAt(int index) => ((IList<T>)targetCollection).RemoveAt(index);

        public override void Clear() => ((ICollection<T>)targetCollection).Clear();

        public override bool Contains(object? value) => ((ICollection<T>)targetCollection).Contains((T)value!);

        public override int IndexOf(object? value) => ((IList<T>)targetCollection).IndexOf((T)value!);

        public override void CopyTo(Array array, int index) => ((ICollection<T>)targetCollection).CopyTo((T[])array, index);
    }
}