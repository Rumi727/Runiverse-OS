#nullable enable
namespace RuniOS.Utility.Attributes
{
    public sealed class FieldNameAttribute : PropertyAttribute
    {
        public FieldNameAttribute(string name, bool force = false) : base(true)
        {
            this.name = name;
            this.force = force;
        }

        public string name { get; } = string.Empty;
        public bool force { get; } = false;
    }
}