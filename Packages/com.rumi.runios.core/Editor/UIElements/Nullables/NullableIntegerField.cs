#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables;

[UxmlElement]
public partial class NullableIntegerField : NullableField<int>
{
    public NullableIntegerField() : this(string.Empty) { }
    public NullableIntegerField(string label, string? nullText = null) : base
    (
        label,
        new FieldDescription<IntegerField, int>
        (
            SerializableNullable.nameOfInternalValue,
            static x => x.HasValue ? x.Value : 0,
            static (ref SerializableNullable<int> nullable, int fieldValue) => nullable = fieldValue
        ),
        nullText
    )
    { }
}