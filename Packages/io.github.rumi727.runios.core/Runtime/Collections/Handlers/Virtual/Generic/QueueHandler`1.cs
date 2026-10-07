#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CollectionHandler(typeof(Queue<>), useForChildren = true)]
    public class QueueHandler<T>(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => false;
        public override bool isFixedSize => false;

        protected override void UpdateSourceCollections()
        {
            ((Queue<T>)targetCollection).Clear();
            for (int i = 0; i < synchronizedList.Count; i++)
                ((Queue<T>)targetCollection).Enqueue((T)synchronizedList[i]);
        }
    }
}