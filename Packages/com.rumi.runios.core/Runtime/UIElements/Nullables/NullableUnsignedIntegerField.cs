#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableUnsignedIntegerField : NullableField<uint>
    {
        public NullableUnsignedIntegerField() : this(string.Empty) { }
        public NullableUnsignedIntegerField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<UnsignedIntegerField, uint>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<uint> nullable, uint fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}