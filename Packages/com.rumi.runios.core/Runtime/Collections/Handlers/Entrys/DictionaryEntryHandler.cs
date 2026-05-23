#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Entrys
{
    [CustomEntryHandler(typeof(DictionaryEntry))]
    public class DictionaryEntryHandler(object targetEntry) : EntryHandler(targetEntry)
    {
        protected override object key => ((DictionaryEntry)targetEntry).Key;
        protected override object? value => ((DictionaryEntry)targetEntry).Value;

        public override object CreateInstance(object? key, object? value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            return new DictionaryEntry(key, value);
        }
    }
}