using System;
using Newtonsoft.Json;
using RuniEngine.Json.Converters;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    [JsonConverter(typeof(SerializableTypeConverter))]
    public struct SerializableType : IEquatable<SerializableType>, ISerializationCallbackReceiver
    {
        public SerializableType(Type? type)
        {
            value = type;
            _value = string.Empty;
        }

        public Type? value { get; set; }
        [SerializeField, JsonIgnore] string? _value;
        
        public bool Equals(SerializableType other) => this == other;
        public override bool Equals(object? obj) => obj is SerializableType other && Equals(other);
        
        public override int GetHashCode() => value?.GetHashCode() ?? 0;
        
        public static bool operator ==(SerializableType lhs, SerializableType rhs) => lhs.value == rhs.value;
        public static bool operator !=(SerializableType lhs, SerializableType rhs) => !(lhs == rhs);
        
        public static implicit operator SerializableType(Type type) => new SerializableType(type);
        public static implicit operator Type?(SerializableType type) => type.value;

        void ISerializationCallbackReceiver.OnBeforeSerialize() => _value = value?.SerializeToString();
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_value == null)
            {
                value = null;
                return;
            }
            
            value = TypeUtility.DeserializeFromString(_value);
        }
    }
}