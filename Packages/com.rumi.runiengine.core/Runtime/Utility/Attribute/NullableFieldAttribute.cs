#nullable enable
using UnityEngine;

namespace RuniEngine
{
    public sealed class NullableFieldAttribute : PropertyAttribute
    {
        public NullableFieldAttribute() => customNullText = null;
        public NullableFieldAttribute(string customNullText) => this.customNullText = customNullText;

        public string? customNullText { get; } = string.Empty;
    }
}
