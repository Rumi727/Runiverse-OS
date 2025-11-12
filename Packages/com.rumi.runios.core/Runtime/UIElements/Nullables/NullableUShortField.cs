#nullable enable
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
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
}