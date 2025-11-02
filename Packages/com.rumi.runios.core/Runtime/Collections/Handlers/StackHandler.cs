#nullable enable
using System;
using System.Collections;

namespace RuniOS.Collections.Handlers
{
    [CustomCollectionHandler(typeof(Stack))]
    public class StackHandler : IEnumerableHandler
    {
        public StackHandler(Type resolvedTargetType, IEnumerable targetCollection) : base(resolvedTargetType, targetCollection) { }
        
        public override bool isReadOnly => false;
        
        public override bool isFixedSize => false;

        public override void UpdateSourceCollections()
        {
            Stack queue = (Stack)targetCollection;
            queue.Clear();

            for (int i = synchronizedList.Count - 1; i >= 0; i--)
                queue.Push(synchronizedList[i]);
        }
    }
}
