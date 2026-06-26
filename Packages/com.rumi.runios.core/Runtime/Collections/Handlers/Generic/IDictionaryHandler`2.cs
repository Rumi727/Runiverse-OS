#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Generic
{
    [CustomCollectionHandler(typeof(IDictionary<,>))]
    public class IDictionaryHandler<TKey, TValue>(IEnumerable targetCollection) : DictionaryHandlerBase(targetCollection)
    {
        public override bool isReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)targetCollection).IsReadOnly;
        public override bool isFixedSize => isReadOnly;

        public override object? this[object key]
        {
            get => ((IDictionary<TKey, TValue>)targetCollection)[(TKey)key];
            set => ((IDictionary<TKey, TValue>)targetCollection)[(TKey)key] = (TValue)value!;
        }

        public override ICollection keys => FindCollectionHandler(((IDictionary<TKey, TValue>)targetCollection).Keys);
        public override ICollection values => FindCollectionHandler(((IDictionary<TKey, TValue>)targetCollection).Values);

        public override int count => ((IDictionary<TKey, TValue>)targetCollection).Count;

        public override void Add(object key, object? value) => ((IDictionary<TKey, TValue>)targetCollection).Add((TKey)key, (TValue)value!);

        public override void Remove(object key) => ((IDictionary<TKey, TValue>)targetCollection).Remove((TKey)key);

        public override void Clear() => ((IDictionary<TKey, TValue>)targetCollection).Clear();

        public override bool Contains(object key) => ((IDictionary<TKey, TValue>)targetCollection).ContainsKey((TKey)key);

        public override void CopyTo(Array array, int index) => ((IDictionary<TKey, TValue>)targetCollection).CopyTo((KeyValuePair<TKey, TValue>[])array, index);
    }
}