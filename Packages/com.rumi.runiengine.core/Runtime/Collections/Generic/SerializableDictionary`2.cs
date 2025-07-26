#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RuniEngine.Collections.Generic
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializableDictionary<TKey, TValue, SerializableKeyValuePair<TKey?, TValue?>>, ISerializableDictionary
    {
        public SerializableDictionary() { }
        public SerializableDictionary(int capacity) : base(capacity) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        public SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }


        [SerializeField] List<SerializableKeyValuePair<TKey?, TValue?>> pairs = new();

        IList<SerializableKeyValuePair<TKey?, TValue?>> ISerializableDictionary<TKey, TValue, SerializableKeyValuePair<TKey?, TValue?>>.pairs => pairs;
        IList ISerializableDictionary.pairs => pairs;

        public Type keyType => typeof(TKey);
        public Type valueType => typeof(TValue);
    }
}
