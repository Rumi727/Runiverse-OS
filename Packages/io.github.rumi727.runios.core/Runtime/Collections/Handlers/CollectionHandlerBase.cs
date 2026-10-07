#nullable enable
using RuniOS.Collections.Handlers.Virtual;
using RuniOS.Reflection;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    public abstract partial class CollectionHandlerBase(IEnumerable targetCollection) : ICollection
    {
        [GenerateAttributedTypeRegistry]
        public static partial AttributedTypeRegistry<CollectionHandlerAttribute> collectionRegistry { get; }

        public static bool HandlerCheck<TDrawer>(Type targetType) where TDrawer : CollectionHandlerBase => typeof(TDrawer).IsAssignableFrom(collectionRegistry.Resolve(targetType));

        public static CollectionHandlerBase FindCollectionHandler(IEnumerable targetCollection)
        {
            Type? handlerType = collectionRegistry.Resolve(targetCollection.GetType());
            if (handlerType != null)
                return (CollectionHandlerBase)Activator.CreateInstance(handlerType, targetCollection);

            return new VirtualListHandler(targetCollection);
        }

        public IEnumerable targetCollection { get; } = targetCollection;

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
