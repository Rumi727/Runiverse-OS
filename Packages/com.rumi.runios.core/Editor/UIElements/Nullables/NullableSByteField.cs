#nullable enable
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableSByteField : NullableField<sbyte>
    {
        public NullableSByteField() : this(string.Empty) { }
        public NullableSByteField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<SByteField, sbyte>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : default,
                static (ref SerializableNullable<sbyte> nullable, sbyte fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}