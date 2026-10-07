#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual
{
    public abstract class VirtualDictionaryHandler(IEnumerable targetCollection) : DictionaryHandlerBase(targetCollection)
    {
        protected Hashtable synchronizedTable { get; } = new();

        public override object? this[object key]
        {
            get => synchronizedTable[key];
            set
            {
                synchronizedTable[key] = value;
                UpdateSourceCollections();
            }
        }

        public override int count => synchronizedTable.Count;

        public override bool isReadOnly => true;

        public override bool isFixedSize => true;

        public override ICollection keys => synchronizedTable.Keys;
        public override ICollection values => synchronizedTable.Values;

        public override void Add(object key, object? value)
        {
            synchronizedTable.Add(key, value);
            UpdateSourceCollections();
        }
        public override void Remove(object key)
        {
            synchronizedTable.Remove(key);
            UpdateSourceCollections();
        }

        public override void Clear()
        {
            synchronizedTable.Clear();
            UpdateSourceCollections();
        }

        public override bool Contains(object key) => synchronizedTable.Contains(key);

        public override void CopyTo(Array array, int index) => synchronizedTable.CopyTo(array, index);

        public override IDictionaryEnumerator GetEnumerator() => synchronizedTable.GetEnumerator();

        public override void SynchronizeCollections()
        {
            synchronizedTable.Clear();
            
            using Enumerator enumerator = new Enumerator(targetCollection);
            while (enumerator.MoveNext())
                synchronizedTable.Add(enumerator.Key, enumerator.Value);
        }
    }
}