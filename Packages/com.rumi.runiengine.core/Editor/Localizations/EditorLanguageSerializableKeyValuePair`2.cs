#nullable enable
using System;
using System.Diagnostics;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    [Serializable]
    struct EditorLanguageSerializableKeyValuePair : ISerializableKeyValuePair, ISerializableKeyValuePair<string?, string?>
    {
        public EditorLanguageSerializableKeyValuePair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        // 이름은 바꾸지 마세요 (직렬화)
        [SerializeField, FieldName("gui.key"), DebuggerBrowsable(DebuggerBrowsableState.Never)] string? key;
        [SerializeField, FieldName("gui.value"), TextArea(0, 1000), DebuggerBrowsable(DebuggerBrowsableState.Never)] string? value;

        public string? Key
        {
            readonly get => key;
            set => key = value;
        }
        public string? Value
        {
            readonly get => value;
            set => this.value = value;
        }

        object? ISerializableKeyValuePair.Key
        {
            readonly get => key;
            set
            {
                if (value is string result)
                    key = result;
                
                throw new InvalidCastException();
            }
        }
        object? ISerializableKeyValuePair.Value
        {
            readonly get => value;
            set
            {
                if (value is string result)
                    this.value = result;
                
                throw new InvalidCastException();
            }
        }
    }
}