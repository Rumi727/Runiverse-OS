#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/KeyValuePair.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializableKeyValuePair, ISerializableKeyValuePair<TKey, TValue>
    {
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        // 이름은 바꾸지 마세요 (직렬화)
        [SerializeField, FieldName("gui.key"), DebuggerBrowsable(DebuggerBrowsableState.Never)] TKey key;
        [SerializeField, FieldName("gui.value"), DebuggerBrowsable(DebuggerBrowsableState.Never)] TValue value;

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

        object? ISerializableKeyValuePair.Key
        {
            readonly get => key;
            set
            {
                if (value is TKey result)
                    key = result;
                
                throw new InvalidCastException();
            }
        }
        object? ISerializableKeyValuePair.Value
        {
            readonly get => value;
            set
            {
                if (value is TValue result)
                    this.value = result;
                
                throw new InvalidCastException();
            }
        }

        public override readonly string ToString() => $"[{key}, {value}]";

        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
        
        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValuePair<TKey, TValue> pair) => KeyValuePair.Create(pair.Key, pair.Value);
        public static implicit operator SerializableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => SerializableKeyValuePair.Create(pair.Key, pair.Value);
        
        public static implicit operator (TKey Key, TValue Value)(SerializableKeyValuePair<TKey, TValue> pair) => (pair.Key, pair.Value);
        public static implicit operator SerializableKeyValuePair<TKey, TValue>((TKey Key, TValue Value) pair) => SerializableKeyValuePair.Create(pair.Key, pair.Value);
    }
}