#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual;

[CustomCollectionHandler(typeof(Queue))]
public class QueueHandler : VirtualListHandler
{
    public QueueHandler(IEnumerable targetCollection) : base(targetCollection) { }
        
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