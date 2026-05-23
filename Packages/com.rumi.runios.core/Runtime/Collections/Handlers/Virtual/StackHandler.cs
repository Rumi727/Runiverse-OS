#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual
{
    [CustomCollectionHandler(typeof(Stack))]
    public class StackHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        protected override void UpdateSourceCollections()
        {
            Stack queue = (Stack)targetCollection;
            queue.Clear();

            for (int i = synchronizedList.Count - 1; i >= 0; i--)
                queue.Push(synchronizedList[i]);
        }
    }
}