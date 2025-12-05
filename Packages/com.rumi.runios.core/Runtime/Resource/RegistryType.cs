#nullable enable
using Newtonsoft.Json;

namespace RuniOS.Resource
{
    [Serializable]
    [JsonConverter(typeof(RegistryType))]
    public struct RegistryType : IEquatable<RegistryType>
    {
        public RegistryType(string name) => _name = name;

        public string name
        {
            readonly get => _name ?? string.Empty;
            set => _name = value;
        }
        [SerializeField] string? _name;

        public override string ToString() => name;

        public bool Equals(RegistryType other) => name == other.name;
    
        public override bool Equals(object? obj) => obj is RegistryType other && Equals(other);
        public override int GetHashCode() => name.GetHashCode();
    
        public static bool operator ==(RegistryType left, RegistryType right) => left.Equals(right);
        public static bool operator !=(RegistryType left, RegistryType right) => !left.Equals(right);

        public static implicit operator string(RegistryType type) => type.name;
        public static implicit operator RegistryType(string? name) => new RegistryType(name ?? string.Empty);
    }
}