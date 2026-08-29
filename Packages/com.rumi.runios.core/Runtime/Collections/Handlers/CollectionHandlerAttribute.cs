#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Collections.Handlers
{
    public sealed class CollectionHandlerAttribute(Type targetType) : TypeRegistrationAttribute(targetType);
}