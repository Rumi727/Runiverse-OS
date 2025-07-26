using System;
using UnityEngine;

namespace RuniEngine
{
    public struct SerializableType : IEquatable<SerializableType>
    {
        public Type type { get; set; }
        [SerializeField] string typeAsString;
        
        public bool Equals(SerializableType other) => type.Equals(other.type);
        public override bool Equals(object? obj) => obj is SerializableType other && Equals(other);
        public override int GetHashCode() => type.GetHashCode();
        public static bool operator ==(SerializableType left, SerializableType right) => left.Equals(right);
        public static bool operator !=(SerializableType left, SerializableType right) => !left.Equals(right);
    }
}