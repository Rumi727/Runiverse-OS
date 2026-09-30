#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableUnsignedLongFieldField : NullableField<ulong>
    {
        public NullableUnsignedLongFieldField() : this(string.Empty) { }
        public NullableUnsignedLongFieldField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<UnsignedLongField, ulong>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<ulong> nullable, ulong fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}