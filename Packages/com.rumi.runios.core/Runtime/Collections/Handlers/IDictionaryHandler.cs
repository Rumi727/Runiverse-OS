#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers;

[CustomCollectionHandler(typeof(IDictionary))]
public class IDictionaryHandler : DictionaryHandlerBase
{
    public IDictionaryHandler(IEnumerable targetCollection) : base(targetCollection) { }
        
    public override object? this[object key]
    {
        get => ((IDictionary)targetCollection)[key];
        set => ((IDictionary)targetCollection)[key] = value;
    }

    public override ICollection keys => ((IDictionary)targetCollection).Keys;
    public override ICollection values => ((IDictionary)targetCollection).Values;
        
    public override int count => ((ICollection)targetCollection).Count;
        
    public override bool isReadOnly => ((IDictionary)targetCollection).IsReadOnly;
    public override bool isFixedSize => ((IDictionary)targetCollection).IsFixedSize;

    public override void Add(object key, object? value) => ((IDictionary)targetCollection).Add(key, value);
    public override void Remove(object key) => ((IDictionary)targetCollection).Remove(key);
        
    public override void Clear() => ((IDictionary)targetCollection).Clear();
        
    public override bool Contains(object key) => ((IDictionary)targetCollection).Contains(key);
        
    public override void CopyTo(Array array, int index) => ((ICollection)targetCollection).CopyTo(array, index);

    public override IDictionaryEnumerator GetEnumerator() => ((IDictionary)targetCollection).GetEnumerator();
}