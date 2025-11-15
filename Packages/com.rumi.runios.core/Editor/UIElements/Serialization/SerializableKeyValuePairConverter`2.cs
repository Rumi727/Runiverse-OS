#nullable enable
using Newtonsoft.Json;
using RuniOS.Collections.Generic;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class SerializableKeyValuePairConverter<TKey, TValue> : UxmlAttributeConverter<SerializableKeyValuePair<TKey, TValue>>
    {
        public override SerializableKeyValuePair<TKey, TValue> FromString(string? value)
        {
            try
            {
                return JsonConvert.DeserializeObject<SerializableKeyValuePair<TKey, TValue>>(value ?? string.Empty);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return default;
            }
        }
        
        public override string ToString(SerializableKeyValuePair<TKey, TValue> value) => JsonConvert.SerializeObject(value);
    }
}