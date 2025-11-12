#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
    public partial class NullableLongField : NullableField<long>
    {
        public NullableLongField() : this(string.Empty) { }
        public NullableLongField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<LongField, long>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<long> nullable, long fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}