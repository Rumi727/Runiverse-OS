#nullable enable
namespace RuniOS.Collections.Handlers
{
    public sealed class CustomCollectionHandlerAttribute(Type targetType) : TypeHandlerAttribute(targetType)
    {
        public override bool isSubtypeCompatible => true;
    }
}