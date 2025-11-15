#nullable enable
using System.Collections;
using System.Reflection;

namespace RuniOS.Collections.Handlers.Generic;

[CustomCollectionHandler(typeof(IDictionary<,>))]
public class IDictionaryHandler : DictionaryHandlerBase
{
    public IDictionaryHandler(IEnumerable targetCollection) : base(targetCollection)
    {
        targetCollection.GetType().IsAssignableToGenericDefinition(typeof(IDictionary<,>), out resolvedTargetType!);
        targetCollection.GetType().IsAssignableToGenericDefinition(typeof(ICollection<>), out resolvedTargetCollectionType!);
    }

    readonly Type resolvedTargetType;
    readonly Type resolvedTargetCollectionType;
        
    public override bool isReadOnly
    {
        get
        {
            isReadOnlyInfo ??= AccessUtility.DeclaredProperty(resolvedTargetCollectionType, nameof(ICollection<int>.IsReadOnly));
            return (bool)isReadOnlyInfo!.GetValue(targetCollection);
        }
    }
    PropertyInfo? isReadOnlyInfo;

    public override bool isFixedSize => isReadOnly;
        
    public override object? this[object key]
    {
        get
        {
            indexerInfo ??= AccessUtility.DeclaredIndexer(resolvedTargetType);
            indexInfoIndex[0] = key;
                
            return indexerInfo!.GetValue(targetCollection, indexInfoIndex);
        }
        set
        {
            indexerInfo ??= AccessUtility.DeclaredIndexer(resolvedTargetType);
            indexInfoIndex[0] = key;
                
            indexerInfo!.SetValue(targetCollection, value, indexInfoIndex);
        }
    }
    readonly object?[] indexInfoIndex = new object?[1];
    PropertyInfo? indexerInfo;
        
    public override ICollection keys
    {
        get
        {
            keysInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(IDictionary<int, int>.Keys));
            return keysCollection ??= FindCollectionHandler((IEnumerable)keysInfo!.GetValue(targetCollection));
        }
    }
    CollectionHandlerBase? keysCollection;
    PropertyInfo? keysInfo;
        
    public override ICollection values
    {
        get
        {
            valuesInfo ??= AccessUtility.DeclaredProperty(resolvedTargetType, nameof(IDictionary<int, int>.Values));
            return valuesCollection ??= FindCollectionHandler((IEnumerable)valuesInfo!.GetValue(targetCollection));
        }
    }
    CollectionHandlerBase? valuesCollection;
    PropertyInfo? valuesInfo;

    public override int count
    {
        get
        {
            countInfo ??= AccessUtility.DeclaredProperty(resolvedTargetCollectionType, nameof(ICollection<int>.Count));
            return (int)countInfo!.GetValue(targetCollection);
        }
    }
    PropertyInfo? countInfo;

    public override void Add(object key, object? value)
    {
        addInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IDictionary<int, int>.Add), resolvedTargetType.GenericTypeArguments);
            
        addInfoParameters[0] = key;
        addInfoParameters[1] = value;
            
        addInfo!.Invoke(targetCollection, addInfoParameters);
    }
    readonly object?[] addInfoParameters = new object?[2];
    MethodInfo? addInfo;
        
    public override void Remove(object key)
    {
        removeInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IDictionary<int, int>.Remove));
        removeInfoParameters[0] = key;
            
        removeInfo!.Invoke(targetCollection, removeInfoParameters);
    }
    readonly object[] removeInfoParameters = new object[1];
    MethodInfo? removeInfo;

    public override void Clear()
    {
        clearMethod ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.Clear));
        clearMethod!.Invoke(targetCollection, null);
    }
    MethodInfo? clearMethod;

    public override bool Contains(object key)
    {
        containsKeyInfo ??= AccessUtility.DeclaredMethod(resolvedTargetType, nameof(IDictionary<int, int>.ContainsKey));
        containsKeyInfoParameters[0] = key;
            
        return (bool)containsKeyInfo!.Invoke(targetCollection, containsKeyInfoParameters);
    }
    readonly object[] containsKeyInfoParameters = new object[1];
    MethodInfo? containsKeyInfo;

    public override void CopyTo(Array array, int index)
    {
        copyToInfo ??= AccessUtility.DeclaredMethod(resolvedTargetCollectionType, nameof(ICollection<int>.CopyTo));
            
        copyToInfoParameters[0] = array;
        copyToInfoParameters[1] = index;
            
        copyToInfo!.Invoke(targetCollection, copyToInfoParameters);
    }
    readonly object[] copyToInfoParameters = new object[2];
    MethodInfo? copyToInfo;
}