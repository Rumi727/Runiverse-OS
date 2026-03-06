#nullable enable
using RuniOS.Inspectors.Attributes;

namespace RuniOS.Utility.Attributes
{
    public sealed class FieldNameAttribute : PropertyAttribute, IInspectorAttribute
    {
        public FieldNameAttribute(string name, bool force = false) : base(true)
        {
            this.name = name;
            this.force = force;
        }

        public string name { get; } = string.Empty;
        public bool force { get; } = false;

        bool IInspectorAttribute.applyToSelf => true;
    }
}