#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CollectionHandler(typeof(Stack<>), useForChildren = true)]
    public class StackHandler<T>(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => false;
        public override bool isFixedSize => false;

        protected override void UpdateSourceCollections()
        {
            ((Stack<T>)targetCollection).Clear();
            for (int i = synchronizedList.Count - 1; i >= 0; i--)
                ((Stack<T>)targetCollection).Push((T)synchronizedList[i]);
        }
    }
}