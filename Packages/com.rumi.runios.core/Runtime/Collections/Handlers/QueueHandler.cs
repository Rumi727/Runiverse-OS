#nullable enable
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(Queue))]
    public class QueueHandler : IEnumerableHandler
    {
        public QueueHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }
        
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        public override void UpdateSourceCollections()
        {
            Queue queue = (Queue)targetCollection;
            queue.Clear();

            for (int i = 0; i < synchronizedList.Count; i++)
                queue.Enqueue(synchronizedList[i]);
        }
    }
}
