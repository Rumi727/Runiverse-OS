#nullable enable
using System.Reflection;

namespace RuniOS.Collections.Handlers.Entrys.Generic
{
    [CustomEntryHandler(typeof(KeyValuePair<,>))]
    public class KeyValuePairHandler : EntryHandler
    {
        public KeyValuePairHandler(object targetEntry) : base(targetEntry) => targetEntry.GetType().IsAssignableToGenericDefinition(typeof(KeyValuePair<,>), out resolvedTargetType!);

        readonly Type resolvedTargetType;
        
        protected override object? key
        {
            get
            {
                keyInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(KeyValuePair<int, int>.Key));
                return keyInfo!.GetValue(targetEntry);
            }
        }
        PropertyInfo? keyInfo;
        
        protected override object? value
        {
            get
            {
                valueInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(KeyValuePair<int, int>.Value));
                return valueInfo!.GetValue(targetEntry);
            }
        }
        PropertyInfo? valueInfo;

        public override object CreateInstance(object? key, object? value) => Activator.CreateInstance(resolvedTargetType, key, value);
    }
}