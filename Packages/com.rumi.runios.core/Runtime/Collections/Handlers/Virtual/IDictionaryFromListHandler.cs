#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using System.Collections;
using System.Collections.Generic;

namespace RuniOS.Collections.Handlers.Virtual
{
    [CustomCollectionHandler(typeof(IDictionary))]
    public class IDictionaryFromListHandler : VirtualListHandler
    {
        public IDictionaryFromListHandler(IEnumerable targetCollection) : base(targetCollection) { }

        public override bool isReadOnly => ((IDictionary)targetCollection).IsReadOnly;
        
        public override bool isFixedSize => ((IDictionary)targetCollection).IsFixedSize;

        public override void UpdateSourceCollections()
        {
            IDictionary dictionary = (IDictionary)targetCollection;
            
            dictionary.Clear();
            for (int i = 0; i < synchronizedList.Count; i++)
            {
                KeyValuePair<object?, object?> entry = EntryHandler.FindEntry(synchronizedList[i]);
                dictionary.Add(entry.Key!, entry.Value);
            }
        }
    }
}