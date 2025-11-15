#nullable enable
using System.Collections;

namespace RuniOS.Collections.Handlers.Entrys
{
    [CustomEntryHandler(typeof(DictionaryEntry))]
    public class DictionaryEntryHandler : EntryHandler
    {
        public DictionaryEntryHandler(object targetEntry) : base(targetEntry) { }

        protected override object key => ((DictionaryEntry)targetEntry).Key;
        protected override object? value => ((DictionaryEntry)targetEntry).Value;

        public override object CreateInstance(object? key, object? value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            return new DictionaryEntry(key, value);
        }
    }
}