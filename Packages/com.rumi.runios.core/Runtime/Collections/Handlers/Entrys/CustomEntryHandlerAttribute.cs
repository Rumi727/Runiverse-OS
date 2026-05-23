#nullable enable
namespace RuniOS.Collections.Handlers.Entrys
{
    public sealed class CustomEntryHandlerAttribute(Type targetType) : TypeHandlerAttribute(targetType)
    {
        public override bool isSubtypeCompatible => true;
    }
}