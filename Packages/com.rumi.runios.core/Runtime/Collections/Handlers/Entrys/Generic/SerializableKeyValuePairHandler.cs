#nullable enable
using RuniOS.Collections.Generic;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Entrys.Generic;

[CustomEntryHandler(typeof(ISerializableKeyValuePair<,>))]
public class ISerializableKeyValuePairHandler : EntryHandler
{
    public ISerializableKeyValuePairHandler(object targetEntry) : base(targetEntry) => targetEntry.GetType().IsAssignableToGenericDefinition(typeof(ISerializableKeyValuePair<,>), out resolvedTargetType!);

    readonly Type resolvedTargetType;
        
    protected override object? key
    {
        get
        {
            keyInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(ISerializableKeyValuePair<int, int>.Key));
            return keyInfo!.GetValue(targetEntry);
        }
    }
    PropertyInfo? keyInfo;
        
    protected override object? value
    {
        get
        {
            valueInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(ISerializableKeyValuePair<int, int>.Value));
            return valueInfo!.GetValue(targetEntry);
        }
    }
    PropertyInfo? valueInfo;

    public override object CreateInstance(object? key, object? value)
    {
        createInstanceInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(ISerializableKeyValuePair<int, int>.CreateInstance));
            
        createInstanceParameters[0] = key;
        createInstanceParameters[1] = value;
            
        return createInstanceInfo!.Invoke(targetEntry, createInstanceParameters);
    }
    readonly object?[] createInstanceParameters = new object?[2];
    MethodInfo? createInstanceInfo;
}