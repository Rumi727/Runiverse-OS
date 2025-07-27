#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Nullable.cs
using Newtonsoft.Json;
using RuniEngine.Json.Converters;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniEngine
{
    /// <summary>
    /// <see cref="Nullable{T}"/>과 유사하지만 유니티에서 직렬화 가능한 제네릭 구조체입니다.
    /// <br/>
    /// <see cref="JsonConverterAttribute"/>를 통해 <see cref="SerializableNullableConverter"/>를 사용하여
    /// JSON 직렬화 및 역직렬화를 처리합니다.
    /// </summary>
    /// <typeparam name="T">값이 될 수 있는 기본 값 타입입니다. 항상 <see langword="struct"/>여야 합니다.</typeparam>
    [Serializable]
    [JsonConverter(typeof(SerializableNullableConverter))]
    public struct SerializableNullable<T> : ISerializableNullable<T>, IEquatable<T>, IEquatable<T?>, IEquatable<SerializableNullable<T>>, ISerializationCallbackReceiver where T : struct
    {
        [SerializeField, JsonIgnore] T value;
        [SerializeField, JsonIgnore] bool hasValue;

        /// <summary>
        /// 지정된 값을 사용하여 <see cref="SerializableNullable{T}"/> 구조체의 새 인스턴스를 초기화합니다.
        /// <br/>
        /// <see cref="HasValue"/>는 <see langword="true"/>로 설정됩니다.
        /// </summary>
        /// <param name="value">초기화할 값입니다.</param>
        public SerializableNullable(T value)
        {
            this.value = value;
            hasValue = true;
        }

        /// <summary>
        /// 표준 nullable 타입 (<see cref="Nullable{T}"/>) 값을 사용하여 <see cref="SerializableNullable{T}"/> 구조체의 새 인스턴스를 초기화합니다.
        /// <br/>
        /// <see cref="HasValue"/>는 입력 값의 <see cref="Nullable{T}.HasValue"/>에 따라 설정됩니다.
        /// </summary>
        /// <param name="value">초기화할 nullable 값입니다.</param>
        public SerializableNullable(T? value)
        {
            this.value = value.GetValueOrDefault();
            hasValue = value.HasValue;
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스에 값이 할당되었는지 여부를 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Value))]
        public readonly bool HasValue => hasValue;

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스에 할당된 값을 가져옵니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>일 경우, 이 속성에 접근하면 <see cref="InvalidOperationException"/>이 발생합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="HasValue"/>가 <see langword="false"/>일 때 발생합니다.</exception>
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

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스에 값이 할당되어 있으면 해당 값을 반환하고,
        /// 값이 없으면 <typeparamref name="T"/>의 기본값을 반환합니다.
        /// </summary>
        /// <returns>값이 있으면 값이고, 그렇지 않으면 <typeparamref name="T"/>의 기본값입니다.</returns>
        public readonly T GetValueOrDefault() => value;

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스에 값이 할당되어 있으면 해당 값을 반환하고,
        /// 값이 없으면 지정된 기본값을 반환합니다.
        /// </summary>
        /// <param name="defaultValue">이 <see cref="SerializableNullable{T}"/> 인스턴스에 값이 없는 경우 반환할 값입니다.</param>
        /// <returns>값이 있으면 값이고, 그렇지 않으면 <paramref name="defaultValue"/>입니다.</returns>
        public readonly T GetValueOrDefault(T defaultValue) => hasValue ? value : defaultValue;

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스가 직렬화되기 전에 호출됩니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>인 경우 내부 <see cref="value"/> 필드를 기본값으로 설정합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!hasValue)
                value = default;
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스가 역직렬화된 후에 호출됩니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>인 경우 내부 <see cref="value"/> 필드를 기본값으로 설정합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!hasValue)
                value = default;
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스와 지정된 개체가 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">현재 개체와 비교할 개체입니다.</param>
        /// <returns>지정된 개체가 현재 개체와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? other)
        {
            if (!hasValue)
                return other == null;
            else if (other == null)
                return false;

            return value.Equals(other);
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스와 지정된 <typeparamref name="T"/> 값이 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <typeparamref name="T"/> 값입니다.</param>
        /// <returns>지정된 값이 현재 인스턴스와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(T other)
        {
            if (!hasValue)
                return false;

            return value.Equals(other);
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스와 지정된 nullable <typeparamref name="T"/> 값이 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 nullable <typeparamref name="T"/> 값입니다.</param>
        /// <returns>지정된 값이 현재 인스턴스와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(T? other)
        {
            if (!hasValue)
                return other == null;
            else if (other == null)
                return false;

            return value.Equals(other);
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스와 다른 <see cref="SerializableNullable{T}"/> 인스턴스가 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <returns>지정된 인스턴스가 현재 인스턴스와 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(SerializableNullable<T> other)
        {
            if (!hasValue)
                return !other.hasValue;
            else if (!other.hasValue)
                return false;

            return value.Equals(other.value);
        }

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스의 해시 코드를 반환합니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>인 경우 0을 반환하고,
        /// 그렇지 않으면 내부 값의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        /// <summary>
        /// 이 <see cref="SerializableNullable{T}"/> 인스턴스의 문자열 표현을 반환합니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>인 경우 빈 문자열을 반환하고,
        /// 그렇지 않으면 내부 값의 <see cref="object.ToString"/> 결과를 반환합니다.
        /// </summary>
        /// <returns>이 인스턴스의 문자열 표현입니다.</returns>
        public override readonly string ToString() => hasValue ? value.ToString() : string.Empty;

        /// <summary>
        /// 두 <see cref="SerializableNullable{T}"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(SerializableNullable<T> lhs, SerializableNullable<T> rhs)
        {
            if (!lhs.hasValue)
                return !rhs.hasValue;
            else if (!rhs.hasValue)
                return false;

            return lhs.value.Equals(rhs.value);
        }
        
        /// <summary>
        /// 두 <see cref="SerializableNullable{T}"/> 인스턴스의 값이 다른지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(SerializableNullable<T> lhs, SerializableNullable<T> rhs) => !(lhs == rhs);

        /// <summary>
        /// <typeparamref name="T"/> 값을 <see cref="SerializableNullable{T}"/>로 암시적으로 변환합니다.
        /// <br/>
        /// 변환된 <see cref="SerializableNullable{T}"/>의 <see cref="HasValue"/>는 <see langword="true"/>입니다.
        /// </summary>
        /// <param name="value">변환할 <typeparamref name="T"/> 값입니다.</param>
        /// <returns>변환된 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</returns>
        public static implicit operator SerializableNullable<T>(T value) => new SerializableNullable<T>(value);
        
        /// <summary>
        /// <see cref="SerializableNullable{T}"/>를 <typeparamref name="T"/>로 명시적으로 변환합니다.
        /// <br/>
        /// 이 변환은 <see cref="HasValue"/>가 <see langword="false"/>일 경우 <see cref="InvalidOperationException"/>을 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <returns>변환된 <typeparamref name="T"/> 값입니다.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="value"/>에 값이 없을 때 발생합니다.</exception>
        public static explicit operator T(SerializableNullable<T> value) => value.Value;

        /// <summary>
        /// 표준 nullable 타입 <typeparamref name="T?"/> 값을 <see cref="SerializableNullable{T}"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 <typeparamref name="T?"/> 값입니다.</param>
        /// <returns>변환된 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</returns>
        public static implicit operator SerializableNullable<T>(T? value) => new SerializableNullable<T>(value);
        
        /// <summary>
        /// <see cref="SerializableNullable{T}"/>를 표준 nullable 타입 <typeparamref name="T?"/>로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="HasValue"/>가 <see langword="false"/>인 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="SerializableNullable{T}"/> 인스턴스입니다.</param>
        /// <returns>변환된 <typeparamref name="T?"/> 값입니다.</returns>
        public static implicit operator T?(SerializableNullable<T> value) => value.hasValue ? new T?(value.value) : null;
    }
}
#pragma warning restore IDE1006 // 명명 스타일