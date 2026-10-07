#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual.Generic
{
    [CollectionHandler(typeof(IDictionary<,>), useForChildren = true)]
    public class IDictionaryFromListHandler<TKey, TValue>(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)targetCollection).IsReadOnly;
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

            ((ICollection<KeyValuePair<TKey, TValue>>)targetCollection).Clear();
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);
                ((IDictionary<TKey, TValue>)targetCollection).Add((TKey)entry.Key!, (TValue)entry.Value!);
            }
        }

        readonly HashSet<object?> tempKeyTable = [];
        bool IsDuplicate()
        {
            tempKeyTable.Clear();

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);
                if (!tempKeyTable.Add(entry.Key))
                    return true;
            }

            return false;
        }
    }
}