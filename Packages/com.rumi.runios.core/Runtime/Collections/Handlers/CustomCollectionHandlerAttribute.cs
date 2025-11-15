#nullable enable
namespace RuniOS.Collections.Handlers;

public sealed class CustomCollectionHandlerAttribute : CustomAttributeDrawerAttribute
{
    public override bool isSubtypeCompatible => true;

    public CustomCollectionHandlerAttribute(Type targetType) : base(targetType) { }
}