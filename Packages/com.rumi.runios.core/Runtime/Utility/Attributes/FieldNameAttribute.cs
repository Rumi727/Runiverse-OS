#nullable enable
using RuniOS.Inspectors.Attributes;
using RuniOS.Resource;
using UnityEngine;

namespace RuniOS.Utility.Attributes
{
    public sealed class FieldNameAttribute : PropertyAttribute, IInspectorAttribute
    {
        public FieldNameAttribute(string name, bool force = false) : base(true)
        {
            this.name = name;
            this.force = force;
        }

        public Identifier name { get; } = Identifier.empty;
        public bool force { get; } = false;

        bool IInspectorAttribute.applyToSelf => true;
    }
}