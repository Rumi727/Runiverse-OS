#nullable enable
using RuniOS.Collections.Handlers.Virtual;
using RuniOS.Reflection;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    public abstract partial class ListHandlerBase(IEnumerable targetCollection) : CollectionHandlerBase(targetCollection), IList
    {
        [GenerateAttributedTypeRegistry]
        public static partial AttributedTypeRegistry<CollectionHandlerAttribute> listRegistry { get; }

        public static ListHandlerBase FindListHandler(IEnumerable targetCollection)
        {
            Type? handlerType = listRegistry.Resolve(targetCollection.GetType());
            if (handlerType != null && typeof(ListHandlerBase).IsAssignableFrom(handlerType))
                return (ListHandlerBase)Activator.CreateInstance(handlerType, targetCollection);

            return new VirtualListHandler(targetCollection);
        }

        public abstract object? this[int index] { get; set; }
        
        public abstract bool isReadOnly { get; }
        bool IList.IsReadOnly => isReadOnly;
        
        public abstract bool isFixedSize { get; }
        bool IList.IsFixedSize => isFixedSize;
        
        public abstract int Add(object? value);
        public abstract void Insert(int index, object value);
        
        public abstract void Remove(object value);
        public abstract void RemoveAt(int index);
        
        public abstract void Clear();
        
        public abstract bool Contains(object? value);
        public abstract int IndexOf(object value);
    }
}
