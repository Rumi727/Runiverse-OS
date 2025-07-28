#nullable enable
using System;
using Newtonsoft.Json;
using RuniEngine.Json.Converters;
using UnityEngine;

namespace RuniEngine
{
    /// <summary>
    /// 유니티에서 직렬화 가능한 <see cref="Type"/>을 나타내는 구조체입니다.
    /// <br/>
    /// <see cref="JsonConverterAttribute"/>를 통해 <see cref="SerializableTypeConverter"/>를 사용하여
    /// JSON 직렬화 및 역직렬화를 처리합니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(SerializableTypeConverter))]
    public struct SerializableType : IEquatable<SerializableType>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// 지정된 <see cref="Type"/> 값을 사용하여 <see cref="SerializableType"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="type">초기화할 <see cref="Type"/> 값입니다. <see langword="null"/>일 수 있습니다.</param>
        public SerializableType(Type? type)
        {
            value = type;
            _value = string.Empty;
        }

        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스가 나타내는 <see cref="Type"/> 객체를 가져오거나 설정합니다.
        /// </summary>
        public Type? value { get; set; }
        
        /// <summary>
        /// 유니티 직렬화를 위한 <see cref="Type"/>의 문자열 표현을 저장하는 내부 필드입니다.
        /// </summary>
        [SerializeField, Delayed, JsonIgnore] string? _value;
        
        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스와 다른 지정된 <see cref="SerializableType"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="SerializableType"/>입니다.</param>
        /// <returns>지정된 <see cref="SerializableType"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public bool Equals(SerializableType other) => this == other;

        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스와 지정된 <see cref="object"/>의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 <see cref="object"/>입니다.</param>
        /// <returns>지정된 <see cref="object"/>가 <see cref="SerializableType"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override bool Equals(object? obj) => obj is SerializableType other && Equals(other);
        
        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스의 해시 코드를 반환합니다.
        /// <br/>
        /// 내부 <see cref="Type"/> 값이 <see langword="null"/>인 경우 0을 반환하고,
        /// 그렇지 않으면 내부 <see cref="Type"/> 값의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
        public override int GetHashCode() => value?.GetHashCode() ?? 0;
        
        /// <summary>
        /// 두 <see cref="SerializableType"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="SerializableType"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="SerializableType"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(SerializableType lhs, SerializableType rhs) => lhs.value == rhs.value;
        
        /// <summary>
        /// 두 <see cref="SerializableType"/> 인스턴스의 값이 다른지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="SerializableType"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="SerializableType"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(SerializableType lhs, SerializableType rhs) => !(lhs == rhs);
        
        /// <summary>
        /// <see cref="Type"/>을 <see cref="SerializableType"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="type">변환할 <see cref="Type"/> 값입니다.</param>
        /// <returns>변환된 <see cref="SerializableType"/> 인스턴스입니다.</returns>
        public static implicit operator SerializableType(Type type) => new SerializableType(type);
        
        /// <summary>
        /// <see cref="SerializableType"/>를 <see cref="Type"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="type">변환할 <see cref="SerializableType"/> 값입니다.</param>
        /// <returns>변환된 <see cref="Type"/> 값입니다.</returns>
        public static implicit operator Type?(SerializableType type) => type.value;

        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스가 직렬화되기 전에 호출됩니다.
        /// <br/>
        /// 내부 <see cref="Type"/> 값을 문자열 필드(<see cref="_value"/>)로 변환하여 동기화합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize() => _value = value?.SerializeToString();
        
        /// <summary>
        /// 이 <see cref="SerializableType"/> 인스턴스가 역직렬화된 후에 호출됩니다.
        /// <br/>
        /// 문자열 필드(<see cref="_value"/>)로부터 <see cref="Type"/> 값을 파싱하여 동기화합니다.
        /// </summary>
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