#nullable enable
namespace RuniOS.Collections.Handlers
{
    public sealed class CustomCollectionHandlerAttribute : TypeHandlerAttribute
    {
        public override bool isSubtypeCompatible => true;

        public CustomCollectionHandlerAttribute(Type targetType) : base(targetType) { }
    }
}