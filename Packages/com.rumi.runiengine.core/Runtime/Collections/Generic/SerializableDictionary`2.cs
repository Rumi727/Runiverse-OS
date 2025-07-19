#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RuniEngine.Collections.Generic
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializableDictionary, ISerializableDictionary<TKey, TValue>
    {
        public SerializableDictionary() : base() { }
        public SerializableDictionary(int capacity) : base(capacity) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        public SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }


        [SerializeField] List<TKey?> serializableKeys = new List<TKey?>();
        [SerializeField] List<TValue?> serializableValues = new List<TValue?>();

        IList<TKey?> ISerializableDictionary<TKey, TValue>.serializableKeys => serializableKeys;
        IList<TValue?> ISerializableDictionary<TKey, TValue>.serializableValues => serializableValues;

        IList ISerializableDictionary.serializableKeys => serializableKeys;
        IList ISerializableDictionary.serializableValues => serializableValues;

        public Type keyType => typeof(TKey);
        public Type valueType => typeof(TValue);

        void ISerializationCallbackReceiver.OnBeforeSerialize() => ISerializableDictionary<TKey, TValue>.Serialize(this);
        void ISerializationCallbackReceiver.OnAfterDeserialize() => ISerializableDictionary<TKey, TValue>.Deserialize(this);
    }
}
