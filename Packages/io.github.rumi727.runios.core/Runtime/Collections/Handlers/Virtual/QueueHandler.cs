#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual
{
    [CollectionHandler(typeof(Queue), useForChildren = true)]
    public class QueueHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        protected override void UpdateSourceCollections()
        {
            Queue queue = (Queue)targetCollection;
            queue.Clear();

            for (int i = 0; i < synchronizedList.Count; i++)
                queue.Enqueue(synchronizedList[i]);
        }
    }
}