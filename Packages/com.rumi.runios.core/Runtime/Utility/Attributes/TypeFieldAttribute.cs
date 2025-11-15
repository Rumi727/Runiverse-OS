#nullable enable
namespace RuniOS.Utility.Attributes
{
    public sealed class TypeFieldAttribute : PropertyAttribute
    {
        public TypeFieldAttribute(Type baseType) => this.baseType = baseType;
        
        public Type baseType { get; }
    }
}