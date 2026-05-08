#nullable enable
using RuniOS.Collections.Generic;
using System.Diagnostics;

namespace RuniOS.Editor.Localizations
{
    [Serializable]
    struct EditorLanguageSerializableKeyValuePair(string key, string value) : ISerializableKeyValuePair, ISerializableKeyValuePair<string?, string?>
    {

        // 이름은 바꾸지 마세요 (직렬화)
        [SerializeField, FieldName("gui.key"), DebuggerBrowsable(DebuggerBrowsableState.Never)] string? key = key;
        [SerializeField, FieldName("gui.value"), TextArea(0, 1000), DebuggerBrowsable(DebuggerBrowsableState.Never)] string? value = value;

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
        
        readonly ISerializableKeyValuePair<string?, string?> ISerializableKeyValuePair<string?, string?>.CreateInstance(string? key, string? value) => new SerializableKeyValuePair<string?, string?>(key, value);
        readonly ISerializableKeyValuePair ISerializableKeyValuePair.CreateInstance(object? key, object? value)
        {
            ISerializableKeyValuePair copyed = this;
            copyed.Key = key;
            copyed.Value = value;
            return copyed;
        }
    }
}