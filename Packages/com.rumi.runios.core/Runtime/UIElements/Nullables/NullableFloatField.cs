#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
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