#nullable enable
using RuniOS.Editor.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    [UxmlElement]
    public partial class NullableCharField : NullableField<char>
    {
        public NullableCharField() : this(string.Empty) { }
        public NullableCharField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<CharField, char>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : '\0',
                static (ref SerializableNullable<char> nullable, char fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}