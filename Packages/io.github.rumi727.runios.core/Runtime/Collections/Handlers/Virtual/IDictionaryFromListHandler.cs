#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using System.Collections;

namespace RuniOS.Collections.Handlers.Virtual
{
    [CollectionHandler(typeof(IDictionary), useForChildren = true)]
    public class IDictionaryFromListHandler(IEnumerable targetCollection) : VirtualListHandler(targetCollection)
    {
        public override bool isReadOnly => ((IDictionary)targetCollection).IsReadOnly;
        
        public override bool isFixedSize => ((IDictionary)targetCollection).IsFixedSize;
        
        public override void SynchronizeCollections()
        {
            if (IsDuplicate())
                return;

            base.SynchronizeCollections();
        }

        protected override void UpdateSourceCollections()
        {
            IDictionary dictionary = (IDictionary)targetCollection;
            
            dictionary.Clear();
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);
                dictionary.Add(entry.Key!, entry.Value);
            }
        }
        
        readonly HashSet<object?> tempKeyTable = new();
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