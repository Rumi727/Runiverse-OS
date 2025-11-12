#nullable enable
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableShortField : NullableField<short>
    {
        public NullableShortField() : this(string.Empty) { }
        public NullableShortField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<ShortField, short>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : default,
                static (ref SerializableNullable<short> nullable, short fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}