#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace RuniOS.Editor.Installer
{
    [Serializable]
    struct SerializableKeyValuePair<TKey, TValue>
    {
        // 필드랑 프로퍼티 이름 바꾸지 마세요.
        // 직렬화에 사용합니다.

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        [SerializeField] TKey key;
        [SerializeField, TextArea] TValue value;

        public TKey Key
        {
            readonly get => key;
            set => key = value;
        }

        public TValue Value
        {
            readonly get => value;
            set => this.value = value;
        }

        public override readonly string ToString() => $"[{key}, {value}]";

        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }

        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValuePair<TKey, TValue> pair)
            => KeyValuePair.Create(pair.Key, pair.Value);

        public static implicit operator SerializableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
            => new SerializableKeyValuePair<TKey, TValue>(pair.Key, pair.Value);
    }
}
