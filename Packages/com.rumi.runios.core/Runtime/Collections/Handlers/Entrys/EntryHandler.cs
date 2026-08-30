#nullable enable
using RuniOS.Reflection;

namespace RuniOS.Collections.Handlers.Entrys
{
    public abstract partial class EntryHandler(object targetEntry)
    {
        [GenerateAttributedTypeRegistry]
        public static partial AttributedTypeRegistry<EntryHandlerAttribute> registry { get; }

        public static KeyValuePair<object?, object?> FindEntry(object? targetEntry)
        {
            ExceptionUtility.ThrowIfArgumentNull(targetEntry);
            
            Type? type = registry.Resolve(targetEntry.GetType());
            if (type != null)
                return ((EntryHandler)Activator.CreateInstance(type, targetEntry)).entry;

            throw new InvalidOperationException($"{targetEntry} is an invalid entry type. An entry type with an {nameof(EntryHandler)} implementation is required.");
        }
        
        public static object CreateEntry(Type targetType, object? key, object? value)
        {
            if (registry.TryResolve(targetType, out Type? resolvedTargetType, out Type? drawerType))
            {
                if (!resolvedTargetType.CanGetDefaultValueNotNull())
                    throw new InvalidOperationException($"Cannot create an instance of {targetType}. A public constructor without parameters is required.");
                    
                return ((EntryHandler)Activator.CreateInstance(drawerType, resolvedTargetType.GetDefaultValueNotNull())).CreateInstance(key, value);
            }

            throw new InvalidOperationException($"{targetType} is an invalid entry type. An entry type with an {nameof(EntryHandler)} implementation is required.");
        }

        public object targetEntry { get; private set; } = targetEntry;

        public KeyValuePair<object?, object?> entry => new KeyValuePair<object?, object?>(key, value);

        protected abstract object? key { get; }
        protected abstract object? value { get; }
        
        public abstract object CreateInstance(object? key, object? value);
    }
}
