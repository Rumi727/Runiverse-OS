#nullable enable
using UnityEngine;

namespace RuniEngine
{
    public sealed class FieldNameAttribute : PropertyAttribute
    {
        public FieldNameAttribute(string name) => this.name = name;

        public string name { get; } = "";
    }
}
