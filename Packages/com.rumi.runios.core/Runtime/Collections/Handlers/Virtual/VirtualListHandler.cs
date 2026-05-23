#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual
{
    [CustomCollectionHandler(typeof(IEnumerable))]
    public class VirtualListHandler(IEnumerable targetCollection) : ListHandlerBase(targetCollection)
    {
        protected ArrayList synchronizedList { get; } = new();

        public override object? this[int index]
        {
            get => synchronizedList[index];
            set
            {
                synchronizedList[index] = value;
                UpdateSourceCollections();
            }
        }

        public override int count => synchronizedList.Count;

        public override bool isReadOnly => true;

        public override bool isFixedSize => true;

        public override int Add(object? value)
        {
            int index = synchronizedList.Add(value);
            UpdateSourceCollections();
            return index;
        }

        public override void Insert(int index, object? value)
        {
            synchronizedList.Insert(index, value);
            UpdateSourceCollections();
        }

        public override void Remove(object? value)
        {
            synchronizedList.Remove(value);
            UpdateSourceCollections();
        }

        public override void RemoveAt(int index)
        {
            synchronizedList.RemoveAt(index);
            UpdateSourceCollections();
        }

        public override void Clear()
        {
            synchronizedList.Clear();
            UpdateSourceCollections();
        }

        public override bool Contains(object? value) => synchronizedList.Contains(value);
        public override int IndexOf(object? value) => synchronizedList.IndexOf(value);

        public override void CopyTo(Array array, int index) => synchronizedList.CopyTo(array, index);

        public override IEnumerator GetEnumerator() => synchronizedList.GetEnumerator();

        public override void SynchronizeCollections() => synchronizedList.SyncWithEnumerable(targetCollection);
    }
}