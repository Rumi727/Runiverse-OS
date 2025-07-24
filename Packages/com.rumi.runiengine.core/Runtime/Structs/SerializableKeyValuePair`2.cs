#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/KeyValuePair.cs
using System;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializableKeyValuePair<TKey, TValue>
    {
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        // 이름은 바꾸지 마세요 (직렬화)
        [SerializeField, FieldName("gui.key"), DebuggerBrowsable(DebuggerBrowsableState.Never)] TKey key;
        [SerializeField, FieldName("gui.value"), DebuggerBrowsable(DebuggerBrowsableState.Never)] TValue value;

        public readonly TKey Key => key;
        public readonly TValue Value => value;

        public override readonly string ToString() => $"[{key}, {value}]";

        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일