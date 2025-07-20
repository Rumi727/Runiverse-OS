#nullable enable
using System;

namespace RuniEngine
{
    [Serializable]
    public struct VersionRange : IEquatable<Version>, IEquatable<VersionRange>
    {
        public const char separatorChar = '~';

        public VersionRange(string? value)
        {
            if (value == null)
            {
                min = max = Version.all;
                return;
            }

            string[]? versions = value.RemoveWhitespace().Split(separatorChar);
            if (versions == null || versions.Length <= 0)
                min = max = Version.all;
            else
            {
                min = new Version(versions[0]);
                max = new Version(versions[versions.Length - 1]);
            }
        }

        public VersionRange(Version version) => min = max = version;

        public VersionRange(Version min, Version max)
        {
            this.min = min;
            this.max = max;
        }

        [FieldName("gui.min")] public Version min;
        [FieldName("gui.max")] public Version max;



        public static implicit operator string(VersionRange value) => value.ToString();
        public static implicit operator VersionRange(string value) => new VersionRange(value);

        public static implicit operator (Version min, Version max)(VersionRange other) => (other.min, other.max);
        public static implicit operator VersionRange((Version min, Version max) other) => new VersionRange(other.min, other.max);

        public static bool operator ==(VersionRange lhs, VersionRange rhs) => lhs.min == rhs.min && lhs.max == rhs.max;
        public static bool operator !=(VersionRange lhs, VersionRange rhs) => !(lhs == rhs);

        public static bool operator ==(VersionRange lhs, Version rhs) => lhs.min == rhs && lhs.max == rhs;
        public static bool operator !=(VersionRange lhs, Version rhs) => !(lhs == rhs);

        public static bool operator <(VersionRange lhs, Version rhs) => lhs.min > rhs && lhs.max > rhs;
        public static bool operator >(VersionRange lhs, Version rhs) => lhs.min > rhs && lhs.max > rhs;



        public readonly bool Contains(Version version) => version >= min && version <= max;
        public readonly bool Contains(VersionRange range) => range.min >= min && range.max <= max;

        public readonly bool Equals(Version other) => min == other && max == other;
        public readonly bool Equals(VersionRange other) => this == other;

        public override readonly bool Equals(object obj)
        {
            if (obj is VersionRange range)
                return Equals(range);
            else if (obj is Version version)
                return Equals(version);

            return false;
        }

        public override readonly int GetHashCode() => HashCode.Combine(min, max);



        public override readonly string ToString() => $"{min}{separatorChar}{max}";
    }
}
