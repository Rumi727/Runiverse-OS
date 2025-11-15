#nullable enable
using RuniOS.Editor.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables;

[UxmlElement]
public partial class NullableUShortField : NullableField<ushort>
{
    public NullableUShortField() : this(string.Empty) { }
    public NullableUShortField(string label, string? nullText = null) : base
    (
        label,
        new FieldDescription<UShortField, ushort>
        (
            SerializableNullable.nameOfInternalValue,
            static x => x.HasValue ? x.Value : default,
            static (ref SerializableNullable<ushort> nullable, ushort fieldValue) => nullable = fieldValue
        ),
        nullText
    )
    { }
}