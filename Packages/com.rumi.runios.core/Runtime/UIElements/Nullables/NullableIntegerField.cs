#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
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
}