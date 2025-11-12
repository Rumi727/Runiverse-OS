#nullable enable
using RuniOS.Editor.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    [UxmlElement]
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