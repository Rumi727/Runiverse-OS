#nullable enable
using UnityEngine;

namespace RuniEngine
{
    public sealed class FieldNameAttribute : PropertyAttribute
    {
        public FieldNameAttribute(string name, bool force = false)
        {
            this.name = name;
            this.force = force;
        }

        public string name { get; } = string.Empty;
        public bool force { get; } = false;
    }
}
