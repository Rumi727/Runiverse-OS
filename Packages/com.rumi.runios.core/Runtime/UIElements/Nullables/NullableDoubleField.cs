#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
    public partial class NullableDoubleField : NullableField<double>
    {
        public NullableDoubleField() : this(string.Empty) { }
        public NullableDoubleField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<DoubleField, double>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue ? x.Value : 0,
                static (ref SerializableNullable<double> nullable, double fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}