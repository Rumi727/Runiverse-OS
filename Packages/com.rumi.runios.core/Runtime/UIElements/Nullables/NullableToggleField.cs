#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Nullables
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
    public partial class NullableToggleField : NullableField<bool>
    {
        public NullableToggleField() : this(string.Empty) { }
        public NullableToggleField(string label, string? nullText = null) : base
        (
            label,
            new FieldDescription<Toggle, bool>
            (
                SerializableNullable.nameOfInternalValue,
                static x => x.HasValue && x.Value,
                static (ref SerializableNullable<bool> nullable, bool fieldValue) => nullable = fieldValue
            ),
            nullText
        )
        { }
    }
}