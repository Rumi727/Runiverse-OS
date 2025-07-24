#nullable enable
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct Version : IEquatable<Version>, IEquatable<VersionRange>, IComparable, IComparable<Version>, ISerializationCallbackReceiver
    {
        public const char separatorChar = '.';
        public const char noneSeparatorChar = '*';

        [JsonIgnore] public static Version all => new Version();
        [JsonIgnore] public static Version zero => new Version(0, 0, 0);
        [JsonIgnore] public static Version one => new Version(1, 0, 0);



        public int? major;
        public int? minor;
        public int? patch;

        [SerializeField, FieldName("gui.major"), NullableField("*")] SerializableNullable<int> _major;
        [SerializeField, FieldName("gui.minor"), NullableField("*")] SerializableNullable<int> _minor;
        [SerializeField, FieldName("gui.patch"), NullableField("*")] SerializableNullable<int> _patch;



        public Version(string? value)
        {
            _major = null;
            _minor = null;
            _patch = null;

            if (value == null)
            {
                major = minor = patch = null;
                return;
            }

            string[]? versions = value.RemoveWhitespace().Split(separatorChar);
            if (versions == null || versions.Length <= 0)
                major = minor = patch = null;
            else if (versions.Length == 1)
            {
                if (int.TryParse(versions[0], out int major))
                    this.major = major;
                else
                    this.major = null;

                minor = null;
                patch = null;
            }
            else if (versions.Length == 2)
            {
                if (int.TryParse(versions[0], out int major))
                    this.major = major;
                else
                    this.major = null;

                if (int.TryParse(versions[1], out int minor))
                    this.minor = minor;
                else
                    this.minor = null;

                patch = null;
            }
            else
            {
                {
                    if (int.TryParse(versions[0], out int major))
                        this.major = major;
                    else
                        this.major = null;
                }

                {
                    if (int.TryParse(versions[1], out int minor))
                        this.minor = minor;
                    else
                        this.minor = null;
                }

                {
                    if (int.TryParse(versions[2], out int patch))
                        this.patch = patch;
                    else
                        this.patch = null;
                }
            }
        }
        public Version(int? major, int? minor, int? patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;

            _major = null;
            _minor = null;
            _patch = null;
        }



        public static bool operator <=(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch <= rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor < rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major < rhs.major)
                return true;

            return false;
        }
        public static bool operator >=(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch >= rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor > rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major > rhs.major)
                return true;

            return false;
        }
        public static bool operator <(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch < rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor < rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major < rhs.major)
                return true;

            return false;
        }
        public static bool operator >(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch > rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor > rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major > rhs.major)
                return true;

            return false;
        }

        public static bool operator ==(Version lhs, Version rhs) => (lhs.major == null || rhs.major == null || lhs.major == rhs.minor) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch == rhs.patch);
        public static bool operator !=(Version lhs, Version rhs) => !(lhs == rhs);

        public static bool operator ==(Version lhs, VersionRange rhs) => lhs == rhs.min && lhs == rhs.max;
        public static bool operator !=(Version lhs, VersionRange rhs) => !(lhs == rhs);

        public static bool operator <(Version lhs, VersionRange rhs) => lhs > rhs.min && lhs > rhs.max;
        public static bool operator >(Version lhs, VersionRange rhs) => lhs > rhs.min && lhs > rhs.max;

        public static Version operator +(Version lhs, Version rhs) => new Version(lhs.major + rhs.major, lhs.minor + rhs.minor, lhs.patch + rhs.patch);
        public static Version operator -(Version lhs, Version rhs) => new Version(lhs.major - rhs.major, lhs.minor - rhs.minor, lhs.patch - rhs.patch);

        public static Version operator +(Version lhs, int rhs) => new Version(lhs.major, lhs.minor, lhs.patch + rhs);
        public static Version operator -(Version lhs, int rhs) => new Version(lhs.major, lhs.minor, lhs.patch - rhs);

        public static Version operator +(int lhs, Version rhs) => new Version(rhs.major, rhs.minor, lhs + rhs.patch);
        public static Version operator -(int lhs, Version rhs) => new Version(rhs.major, rhs.minor, lhs - rhs.patch);



        public static implicit operator string(Version value) => value.ToString();
        public static implicit operator Version(string value) => new Version(value);

        public static implicit operator VersionRange(Version value) => new VersionRange(value);

        public static implicit operator Vector3Int(Version value) => new Vector3Int(value.major ?? 0, value.minor ?? 0, value.patch ?? 0);
        public static implicit operator Version(Vector3Int value) => new Version(value.x, value.y, value.z);

        public static implicit operator Vector3(Version value) => new Vector3(value.major ?? 0, value.minor ?? 0, value.patch ?? 0);
        public static explicit operator Version(Vector3 value) => new Version(value.x.FloorToInt(), value.y.FloorToInt(), value.z.FloorToInt());



        public readonly bool Equals(Version other) => this == other;
        public readonly bool Equals(VersionRange other) => this == other;

        public override readonly bool Equals(object obj)
        {
            if (obj is Version range)
                return Equals(range);
            else if (obj is VersionRange version)
                return Equals(version);

            return false;
        }

        public override readonly int GetHashCode() => HashCode.Combine(major, minor, patch);



        public readonly int CompareTo(object? value)
        {
            if (value == null)
                return 1;
            else if (value is Version version)
                return CompareTo(version);

            throw new InvalidCastException();
        }

        public readonly int CompareTo(Version value)
        {
            if (this < value)
                return -1;
            else if (this > value)
                return 1;
            else
                return 0;
        }



        public override readonly string ToString() => $"{major ?? noneSeparatorChar}{separatorChar}{minor ?? noneSeparatorChar}{separatorChar}{patch ?? noneSeparatorChar}";



        public void OnBeforeSerialize()
        {
            _major = major;
            _minor = minor;
            _patch = patch;
        }

        public void OnAfterDeserialize()
        {
            major = _major;
            minor = _minor;
            patch = _patch;
        }
    }
}
