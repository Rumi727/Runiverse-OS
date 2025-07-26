#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace RuniEngine.Collections.Generic
{
    /// <summary>
    /// 인스펙터상에 표시되려면 이름의 가진 직렬화 가능 필드가 있어야합니다!
    /// </summary>
    public interface ISerializableDictionary<TKey, TValue, TPair> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver where TPair : ISerializableKeyValuePair<TKey?, TValue?>, new()
    {
        IList<TPair> pairs { get; }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            pairs.Clear();
            
            foreach (var item in this)
            {
                TPair pair = new TPair
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
                TPair pair = pairs[i];
                    
                pair.Key ??= (TKey)typeof(TKey).GetDefaultValueNotNull();
                pair.Value ??= (TValue)typeof(TValue).GetDefaultValueNotNull();
                    
                if (!ContainsKey(pair.Key))
                    Add(pair.Key, pair.Value);
            }
        }
    }
}
