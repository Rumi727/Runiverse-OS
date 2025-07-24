#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace RuniEngine.Collections.Generic
{
    /// <summary>
    /// 인스펙터상에 표시되려면 이름의 가진 직렬화 가능 필드가 있어야합니다!
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface ISerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        IList<TKey?> serializableKeys { get; }
        IList<TValue?> serializableValues { get; }

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
            for (int i = 0; i != serializableKeys.Count.Max(serializableValues.Count); i++)
            {
                TKey? key;
                if (i < serializableKeys.Count)
                    key = serializableKeys[i];
                else
                    key = default;

                key ??= (TKey)typeof(TKey).GetDefaultValueNotNull();

                TValue? value;
                if (i < serializableValues.Count)
                    value = serializableValues[i];
                else
                    value = default;

                value ??= (TValue)typeof(TValue).GetDefaultValueNotNull();

                if (!ContainsKey(key))
                    Add(key, value);
            }
        }
    }
}
