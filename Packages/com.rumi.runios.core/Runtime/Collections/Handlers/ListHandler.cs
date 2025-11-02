#nullable enable
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(IList))]
    [CustomCollectionHandler(typeof(Array))]
    public class ListHandler : CollectionHandler
    {
        public ListHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }

        public override object? this[int index]
        {
            get => ((IList)targetCollection)[index];
            set => ((IList)targetCollection)[index] = value;
        }

        public override int count => ((ICollection)targetCollection).Count;

        public override bool isReadOnly => ((IList)targetCollection).IsReadOnly;

        public override bool isFixedSize => ((IList)targetCollection).IsFixedSize;

        public override int Add(object? value) => ((IList)targetCollection).Add(value);

        public override void Insert(int index, object value) => ((IList)targetCollection).Insert(index, value);

        public override void Remove(object value) => ((IList)targetCollection).Remove(value);

        public override void RemoveAt(int index) => ((IList)targetCollection).RemoveAt(index);

        public override void Clear() => ((IList)targetCollection).Clear();

        public override bool Contains(object? value) => ((IList)targetCollection).Contains(value);
        public override int IndexOf(object value) => ((IList)targetCollection).IndexOf(value);

        public override void CopyTo(Array array, int index) => ((ICollection)targetCollection).CopyTo(array, index);
    }
}
