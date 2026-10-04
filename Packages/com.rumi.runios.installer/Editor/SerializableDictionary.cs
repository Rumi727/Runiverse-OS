#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;

namespace RuniOS.Editor.Installer
{
    [Serializable]
    class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        // 필드랑 프로퍼티 이름 바꾸지 마세요.
        // 직렬화에 사용합니다.

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

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            pairs.Clear();

            foreach (var item in this)
            {
                SerializableKeyValuePair<TKey?, TValue?> pair = new()
                {
                    Key = item.Key,
                    Value = item.Value
                };

                pairs.Add(pair);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < pairs.Count; i++)
            {
                SerializableKeyValuePair<TKey?, TValue?> pair = pairs[i];

                pair.Key ??= (TKey)GetDefaultValueNotNull(typeof(TKey));
                pair.Value ??= (TValue)GetDefaultValueNotNull(typeof(TValue));

                if (!ContainsKey(pair.Key))
                    Add(pair.Key, pair.Value);
            }
        }

        static object GetDefaultValueNotNull(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType() ?? typeof(object), 0);

            if (Nullable.GetUnderlyingType(type) is { } underlyingType)
                return Activator.CreateInstance(underlyingType)!;

            return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Could not create a non-null default value for '{type}'.");
        }
    }
}
