#nullable enable
using RuniOS.Collections.Generic;

namespace RuniOS.Collections.Handlers.Entrys.Generic
{
    [CustomEntryHandler(typeof(ISerializableKeyValuePair<,>))]
    public class ISerializableKeyValuePairHandler<TKey, TValue>(object targetEntry) : EntryHandler(targetEntry)
    {
        protected override object? key => ((ISerializableKeyValuePair<TKey, TValue>)targetEntry).Key;
        protected override object? value => ((ISerializableKeyValuePair<TKey, TValue>)targetEntry).Value;

        public override object CreateInstance(object? key, object? value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            return ((ISerializableKeyValuePair<TKey, TValue>)targetEntry).CreateInstance((TKey)key, (TValue)value!);
        }
    }
}