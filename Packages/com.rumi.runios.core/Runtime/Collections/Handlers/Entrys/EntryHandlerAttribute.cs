#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Collections.Handlers.Entrys
{
    public sealed class EntryHandlerAttribute(Type targetType) : TypeRegistrationAttribute(targetType);
}