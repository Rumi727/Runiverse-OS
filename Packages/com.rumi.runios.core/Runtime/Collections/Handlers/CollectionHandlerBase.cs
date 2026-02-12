#nullable enable
using RuniOS.Collections.Handlers.Virtual;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    public abstract class CollectionHandlerBase : ICollection
    {
        public static bool HandlerCheck<TDrawer>(Type targetType) where TDrawer : CollectionHandlerBase => typeof(TDrawer).IsAssignableFrom(AttributeTypeResolver<TDrawer, CustomCollectionHandlerAttribute>.FindDrawerType(targetType));

        public static CollectionHandlerBase FindCollectionHandler(IEnumerable targetCollection)
        {
            Type? handlerType = AttributeTypeResolver<CollectionHandlerBase, CustomCollectionHandlerAttribute>.FindDrawerType(targetCollection.GetType());
            if (handlerType != null)
                return (CollectionHandlerBase)Activator.CreateInstance(handlerType, targetCollection);

            return new VirtualListHandler(targetCollection);
        }
        
        protected CollectionHandlerBase(IEnumerable targetCollection) => this.targetCollection = targetCollection;

        public IEnumerable targetCollection { get; }
        
        public abstract int count { get; }
        int ICollection.Count => count;
        
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public abstract void CopyTo(Array array, int index);

        public virtual IEnumerator GetEnumerator() => targetCollection.GetEnumerator();

        public virtual void SynchronizeCollections() { }
        protected virtual void UpdateSourceCollections() { }
    }
}