#nullable enable
using RuniOS.Editor.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableDecimalField : NullableField<decimal>
    {
        public NullableDecimalField() : this(string.Empty) { }
        public NullableDecimalField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<DecimalField, decimal>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<decimal> nullable, decimal fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}