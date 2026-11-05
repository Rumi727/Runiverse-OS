#nullable enable
namespace RuniOS.Collections.Handlers.Entrys.Generic
{
    [EntryHandler(typeof(KeyValuePair<,>), useForChildren = true)]
    public class KeyValuePairHandler<TKey, TValue>(object targetEntry) : EntryHandler(targetEntry)
    {
        protected override object? key => ((KeyValuePair<TKey, TValue>)targetEntry).Key;
        protected override object? value => ((KeyValuePair<TKey, TValue>)targetEntry).Value;

        public override object CreateInstance(object? key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new KeyValuePair<TKey, TValue>((TKey)key, (TValue)value!);
        }
    }
}