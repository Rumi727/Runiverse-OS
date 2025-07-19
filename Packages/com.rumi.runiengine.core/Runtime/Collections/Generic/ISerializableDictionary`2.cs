#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace RuniEngine.Collections.Generic
{
    public interface ISerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        IList<TKey?> serializableKeys { get; }
        IList<TValue?> serializableValues { get; }

        public static void Serialize(ISerializableDictionary<TKey, TValue> dict)
        {
            dict.serializableKeys.Clear();
            dict.serializableValues.Clear();

            foreach (var item in dict)
            {
                dict.serializableKeys.Add(item.Key);
                dict.serializableValues.Add(item.Value);
            }
        }

        public static void Deserialize(ISerializableDictionary<TKey, TValue> dict)
        {
            dict.Clear();
            for (int i = 0; i != dict.serializableKeys.Count.Max(dict.serializableValues.Count); i++)
            {
                TKey? key;
                if (i < dict.serializableKeys.Count)
                    key = dict.serializableKeys[i];
                else
                    key = default;

                key ??= (TKey)typeof(TKey).GetDefaultValueNotNull();

                TValue? value;
                if (i < dict.serializableValues.Count)
                    value = dict.serializableValues[i];
                else
                    value = default;

                value ??= (TValue)typeof(TValue).GetDefaultValueNotNull();

                if (!dict.ContainsKey(key))
                    dict.Add(key, value);
            }
        }
    }
}
