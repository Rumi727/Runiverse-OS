#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace RuniEngine.Installer
{
    [Serializable]
    class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue?>, ISerializableDictionary
    {
        public SerializableDictionary() { }
        public SerializableDictionary(int capacity) : base(capacity) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue?>> collection) : base(collection) { }
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue?> dictionary) : base(dictionary) { }
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        public SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public SerializableDictionary(ICollection<KeyValuePair<TKey, TValue?>> collection, IEqualityComparer<TKey?> comparer) : base(collection, comparer) { }
        public SerializableDictionary(IDictionary<TKey, TValue?> dictionary, IEqualityComparer<TKey?> comparer) : base(dictionary, comparer) { }

        [SerializeField] List<TKey?> serializableKeys = new List<TKey?>();
        [SerializeField, TextArea] List<TValue?> serializableValues = new List<TValue?>();

        IList ISerializableDictionary.serializableKeys => serializableKeys;
        IList ISerializableDictionary.serializableValues => serializableValues;

        public Type keyType => typeof(TKey);
        public Type valueType => typeof(TValue);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            serializableKeys.Clear();
            serializableValues.Clear();

            foreach (var item in this)
            {
                serializableKeys.Add(item.Key);
                serializableValues.Add(item.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i != Mathf.Max(serializableKeys.Count, serializableValues.Count); i++)
            {
                TKey? key;
                if (i < serializableKeys.Count)
                    key = serializableKeys[i];
                else
                    key = default;

                key ??= (TKey)GetDefaultValueNotNull(keyType);

                TValue? value;
                if (i < serializableValues.Count)
                    value = serializableValues[i];
                else
                    value = default;

                TryAdd(key, value);
            }
        }

        static object GetDefaultValueNotNull(Type type)
        {
            if (type == typeof(string))
                return string.Empty;

            return Activator.CreateInstance(type);
        }
    }
}
