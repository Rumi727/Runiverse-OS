#nullable enable
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    public abstract class CollectionHandler : AttributeDrawer<CollectionHandler, CustomCollectionHandlerAttribute>, IList
    {
        protected CollectionHandler(Type resolvedTargetType, IEnumerable targetCollection)
        {
            this.resolvedTargetType = resolvedTargetType;
            this.targetCollection = targetCollection;
        }

        public Type resolvedTargetType { get; }
        public IEnumerable targetCollection { get; }
        
        public abstract object? this[int index] { get; set; }
        
        public abstract int count { get; }
        int ICollection.Count => count;
        
        public abstract bool isReadOnly { get; }
        bool IList.IsReadOnly => isReadOnly;
        
        public abstract bool isFixedSize { get; }
        bool IList.IsFixedSize => isFixedSize;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public abstract int Add(object? value);
        public abstract void Insert(int index, object value);
        
        public abstract void Remove(object value);
        public abstract void RemoveAt(int index);
        
        public abstract void Clear();
        
        public abstract bool Contains(object? value);
        public abstract int IndexOf(object value);

        public abstract void CopyTo(Array array, int index);

        public IEnumerator GetEnumerator() => targetCollection.GetEnumerator();

        public virtual void SynchronizeCollections() { }
        public virtual void UpdateSourceCollections() { }
    }
}
