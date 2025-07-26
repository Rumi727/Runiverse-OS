#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Nullable.cs
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct SerializableNullable<T> : ISerializableNullable<T>, IEquatable<T>, IEquatable<T?>, IEquatable<SerializableNullable<T>>, ISerializationCallbackReceiver where T : struct
    {
        [SerializeField, AnimFolder] T value;
        [SerializeField] bool hasValue;

        public SerializableNullable(T value)
        {
            this.value = value;
            hasValue = true;
        }

        public SerializableNullable(T? value)
        {
            this.value = value.GetValueOrDefault();
            hasValue = value.HasValue;
        }

        [MemberNotNullWhen(true, nameof(Value))]
        public readonly bool HasValue => hasValue;

        // ReSharper disable once RedundantNullableFlowAttribute
        [MaybeNull]
        public readonly T Value
        {
            get
            {
                if (!hasValue)
                    throw new InvalidOperationException("Nullable object must have a value.");

                return value;
            }
        }

        public readonly T GetValueOrDefault() => value;

        public readonly T GetValueOrDefault(T defaultValue) => hasValue ? value : defaultValue;



        public void OnBeforeSerialize()
        {
            if (!hasValue)
                value = default;
        }

        public readonly void OnAfterDeserialize() { }



        public override readonly bool Equals(object? other)
        {
            if (!hasValue)
                return other == null;
            else if (other == null)
                return false;

            return value.Equals(other);
        }

        public readonly bool Equals(T other)
        {
            if (!hasValue)
                return false;

            return value.Equals(other);
        }

        public readonly bool Equals(T? other)
        {
            if (!hasValue)
                return other == null;
            else if (other == null)
                return false;

            return value.Equals(other);
        }

        public readonly bool Equals(SerializableNullable<T> other)
        {
            if (!hasValue)
                return other == null;
            else if (other == null)
                return false;

            return value.Equals(other.value);
        }

        public override readonly int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        public override readonly string ToString() => hasValue ? value.ToString() : string.Empty;



        public static bool operator ==(SerializableNullable<T> lhs, SerializableNullable<T> rhs)
        {
            if (!lhs.hasValue)
                return !rhs.hasValue;
            else if (!rhs.hasValue)
                return false;

            return lhs.value.Equals(rhs.value);
        }
        public static bool operator !=(SerializableNullable<T> lhs, SerializableNullable<T> rhs) => !(lhs == rhs);



        public static implicit operator SerializableNullable<T>(T value) => new SerializableNullable<T>(value);
        public static explicit operator T(SerializableNullable<T> value) => value.Value;



        public static implicit operator SerializableNullable<T>(T? value) => new SerializableNullable<T>(value);
        public static implicit operator T?(SerializableNullable<T> value) => value.hasValue ? new T?(value.value) : null;
    }
}
#pragma warning restore IDE1006 // 명명 스타일