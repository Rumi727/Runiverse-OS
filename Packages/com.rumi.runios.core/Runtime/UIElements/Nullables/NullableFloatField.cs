#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableFloatField : NullableField<float>
    {
        public NullableFloatField() : this(string.Empty) { }
        public NullableFloatField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<FloatField, float>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<float> nullable, float fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}