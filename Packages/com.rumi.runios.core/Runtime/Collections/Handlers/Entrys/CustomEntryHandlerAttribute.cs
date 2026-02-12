#nullable enable
namespace RuniOS.Collections.Handlers.Entrys
{
    public sealed class CustomEntryHandlerAttribute : TypeHandlerAttribute
    {
        public override bool isSubtypeCompatible => true;
        
        public CustomEntryHandlerAttribute(Type targetType) : base(targetType) { }
    }
}