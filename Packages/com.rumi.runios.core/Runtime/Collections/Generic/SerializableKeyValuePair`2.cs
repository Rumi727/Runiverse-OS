#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/KeyValuePair.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;

namespace RuniOS.Collections.Generic
{
    /// <summary>
    /// 직렬화 가능한 키와 값을 저장하는 제네릭 키-값 쌍 구조체입니다.
    /// <br/>
    /// 유니티 인스펙터에서 표시될 수 있도록 설계되었으며,
    /// 표준 <see cref="KeyValuePair{TKey, TValue}"/> 및 C# 튜플과의 암시적/명시적 변환을 지원합니다.
    /// </summary>
    /// <typeparam name="TKey">키의 타입입니다.</typeparam>
    /// <typeparam name="TValue">값의 타입입니다.</typeparam>
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializableKeyValuePair, ISerializableKeyValuePair<TKey, TValue>
    {
        // 필드랑 프로퍼티 이름 바꾸지 마세요.
        // 직렬화에 사용합니다.
        
        /// <summary>
        /// 지정된 키와 값을 사용하여 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="key">초기화할 키 값입니다.</param>
        /// <param name="value">초기화할 값입니다.</param>
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>
        /// 유니티 직렬화를 위한 키의 내부 필드입니다.
        /// </summary>
        [SerializeField, FieldName("gui.key"), JsonIgnore, DebuggerBrowsable(DebuggerBrowsableState.Never)] TKey key;
        /// <summary>
        /// 유니티 직렬화를 위한 값의 내부 필드입니다.
        /// </summary>
        [SerializeField, FieldName("gui.value"), JsonIgnore, DebuggerBrowsable(DebuggerBrowsableState.Never)] TValue value;

        /// <summary>
        /// 이 키-값 쌍의 키를 가져오거나 설정합니다.
        /// </summary>
        public TKey Key
        {
            readonly get => key;
            set => key = value;
        }
        /// <summary>
        /// 이 키-값 쌍의 값을 가져오거나 설정합니다.
        /// </summary>
        public TValue Value
        {
            readonly get => value;
            set => this.value = value;
        }

        /// <summary>
        /// <see cref="ISerializableKeyValuePair"/> 인터페이스의 명시적 구현입니다.
        /// 키를 <see cref="object"/> 타입으로 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="InvalidCastException">설정할 값이 <typeparamref name="TKey"/> 타입으로 캐스팅될 수 없는 경우 발생합니다.</exception>
        object? ISerializableKeyValuePair.Key
        {
            readonly get => key;
            set
            {
                if (value is TKey result)
                    key = result;
                else
                    throw new InvalidCastException($"Cannot cast value of type '{value?.GetType().FullName ?? "null"}' to '{typeof(TKey).FullName}'.");
            }
        }
        /// <summary>
        /// <see cref="ISerializableKeyValuePair"/> 인터페이스의 명시적 구현입니다.
        /// 값을 <see cref="object"/> 타입으로 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="InvalidCastException">설정할 값이 <typeparamref name="TValue"/> 타입으로 캐스팅될 수 없는 경우 발생합니다.</exception>
        object? ISerializableKeyValuePair.Value
        {
            readonly get => value;
            set
            {
                if (value is TValue result)
                    this.value = result;
                else
                    throw new InvalidCastException($"Cannot cast value of type '{value?.GetType().FullName ?? "null"}' to '{typeof(TValue).FullName}'.");
            }
        }

        /// <summary>
        /// 이 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 인스턴스의 문자열 표현을 반환합니다.
        /// </summary>
        /// <returns>"[Key, Value]" 형식의 문자열입니다.</returns>
        public override readonly string ToString() => $"[{key}, {value}]";

        /// <summary>
        /// 이 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 인스턴스를 분해하여 키와 값을 개별 변수로 할당합니다.
        /// </summary>
        /// <param name="key">분해된 키 값입니다.</param>
        /// <param name="value">분해된 값입니다.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
        
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/>를 <see cref="KeyValuePair{TKey, TValue}"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="pair">변환할 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 값입니다.</param>
        /// <returns>변환된 <see cref="KeyValuePair{TKey, TValue}"/> 값입니다.</returns>
        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValuePair<TKey, TValue> pair) => KeyValuePair.Create(pair.Key, pair.Value);
        
        /// <summary>
        /// <see cref="KeyValuePair{TKey, TValue}"/>를 <see cref="SerializableKeyValuePair{TKey, TValue}"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="pair">변환할 <see cref="KeyValuePair{TKey, TValue}"/> 값입니다.</param>
        /// <returns>변환된 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 값입니다.</returns>
        public static implicit operator SerializableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => SerializableKeyValuePair.Create(pair.Key, pair.Value);
    }
}