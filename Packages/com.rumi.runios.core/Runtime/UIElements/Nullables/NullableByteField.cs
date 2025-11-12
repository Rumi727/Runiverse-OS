#nullable enable
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
    public partial class NullableByteField : NullableField<byte>
    {
        public NullableByteField() : this(string.Empty) { }
        public NullableByteField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<ByteField, byte>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : default,
                static (ref SerializableNullable<byte> nullable, byte fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}