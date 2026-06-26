#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CustomCollectionHandler(typeof(ISet<>))]
    public class ISetHandler<T>(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => ((ICollection<T>)targetCollection).IsReadOnly;
        public override bool isFixedSize => isReadOnly;

        public override void SynchronizeCollections()
        {
            if (IsDuplicate())
                return;

            base.SynchronizeCollections();
        }

        protected override void UpdateSourceCollections()
        {
            if (IsDuplicate())
                return;

            ((ICollection<T>)targetCollection).Clear();
            for (int i = 0; i < synchronizedList.Count; i++)
                ((ISet<T>)targetCollection).Add((T)synchronizedList[i]);
        }
        
        readonly HashSet<object?> tempKeyTable = [];
        bool IsDuplicate()
        {
            tempKeyTable.Clear();

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                if (!tempKeyTable.Add(synchronizedList[i]))
                    return true;
            }

            return false;
        }
    }
}