#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Collections.Handlers.Entrys;
using System.Collections;

namespace RuniOS.Collections.Handlers;

public abstract class DictionaryHandlerBase : CollectionHandlerBase, IDictionary
{
    public static DictionaryHandlerBase FindDictionaryHandler(IEnumerable targetCollection)
    {
        Type? handlerType = AttributeDrawer<DictionaryHandlerBase, CustomCollectionHandlerAttribute>.FindDrawerType(targetCollection.GetType());
        if (handlerType != null && typeof(DictionaryHandlerBase).IsAssignableFrom(handlerType))
            return (DictionaryHandlerBase)Activator.CreateInstance(handlerType, targetCollection);

        throw new InvalidOperationException($"{targetCollection} is an invalid dictionary type. An dictionary type with an {nameof(DictionaryHandlerBase)} implementation is required.");
    }
        
    protected DictionaryHandlerBase(IEnumerable targetCollection) : base(targetCollection) { }

    public virtual KeyValuePair<Type, Type>? elementType => CollectionGenericUtility.GetDictionaryElementType(targetCollection.GetType()); 
        
    public abstract object? this[object key] { get; set; }
        
    public abstract ICollection keys { get; }
    public ICollection Keys => keys;
        
    public abstract ICollection values { get; }
    public ICollection Values => values;
        
    public abstract bool isReadOnly { get; }
    bool IDictionary.IsReadOnly => isReadOnly;
        
    public abstract bool isFixedSize { get; }
    bool IDictionary.IsFixedSize => isFixedSize;
        
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;

    public abstract void Add(object key, object? value);
        
    public abstract void Remove(object key);
        
    public abstract void Clear();
        
    public abstract bool Contains(object key);
        
    public new virtual IDictionaryEnumerator GetEnumerator() => new Enumerator(targetCollection);
        
    protected sealed class Enumerator : IEnumerator<DictionaryEntry>, IDictionaryEnumerator
    {
#if ENABLE_IDE_RIDER
        [JetBrains.Annotations.MustDisposeResource]
#endif
        public Enumerator(IEnumerable targetCollection) => this.targetCollection = targetCollection.GetEnumerator();

        readonly IEnumerator targetCollection;
            
        public DictionaryEntry Current { get; set; }
        object IEnumerator.Current => Current;
        DictionaryEntry IDictionaryEnumerator.Entry => Current;
            
        public object Key => Current.Key;
        public object? Value => Current.Value;
            
        public void Reset()
        {
            targetCollection.Reset();
            Current = default;
        }
            
        public bool MoveNext()
        {
            if (!targetCollection.MoveNext())
            {
                Current = default;
                return false;
            }

            KeyValuePair<object?, object?> pair = EntryHandler.FindEntry(targetCollection.Current);
            Current = new DictionaryEntry(pair.Key!, pair.Value);
            return true;
        }
            
        public void Dispose() => (targetCollection as IDisposable)?.Dispose();
    }
}