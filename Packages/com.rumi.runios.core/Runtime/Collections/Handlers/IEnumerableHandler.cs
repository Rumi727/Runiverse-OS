#nullable enable
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(IEnumerable))]
    public class IEnumerableHandler : CollectionHandler
    {
        public IEnumerableHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }

        protected ArrayList synchronizedList { get; } = new();

        public override object? this[int index]
        {
            get => synchronizedList[index];
            set => synchronizedList[index] = value;
        }

        public override int count => synchronizedList.Count;

        public override bool isReadOnly => true;

        public override bool isFixedSize => true;

        public override int Add(object? value) => synchronizedList.Add(value);

        public override void Insert(int index, object value) => synchronizedList.Insert(index, value);

        public override void Remove(object value) => synchronizedList.Remove(value);

        public override void RemoveAt(int index) => synchronizedList.RemoveAt(index);

        public override void Clear() => synchronizedList.Clear();

        public override bool Contains(object? value) => synchronizedList.Contains(value);
        public override int IndexOf(object value) => synchronizedList.IndexOf(value);

        public override void CopyTo(Array array, int index) => synchronizedList.CopyTo(array, index);

        public override void SynchronizeCollections() => synchronizedList.SyncWithEnumerable(targetCollection);
    }
}
