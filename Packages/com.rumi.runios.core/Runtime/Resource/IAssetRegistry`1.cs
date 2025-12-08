#nullable enable
namespace RuniOS.Resource
{
    public interface IAssetRegistry<THandle> : IAssetRegistry, IReadOnlyDictionary<Identifier, THandle> where THandle : class, IAssetHandle
    {
        new THandle this[Identifier key] { get; }
        THandle IReadOnlyDictionary<Identifier, THandle>.this[Identifier key] => this[key];
        IAssetHandle IReadOnlyDictionary<Identifier, IAssetHandle>.this[Identifier key] => this[key];
        
        Type IAssetRegistry.handleType => typeof(THandle);

        new int Count { get; }
        int IReadOnlyCollection<KeyValuePair<Identifier, THandle>>.Count => Count;
        int IReadOnlyCollection<KeyValuePair<Identifier, IAssetHandle>>.Count => Count;

        new IEnumerable<Identifier> Keys { get; }
        IEnumerable<Identifier> IReadOnlyDictionary<Identifier, THandle>.Keys => Keys;
        IEnumerable<Identifier> IReadOnlyDictionary<Identifier, IAssetHandle>.Keys => Keys;
        
        new IEnumerable<THandle> Values { get; }
        IEnumerable<THandle> IReadOnlyDictionary<Identifier, THandle>.Values => Values;
        IEnumerable<IAssetHandle> IReadOnlyDictionary<Identifier, IAssetHandle>.Values => Values;

        new bool ContainsKey(Identifier key);
        bool IReadOnlyDictionary<Identifier, THandle>.ContainsKey(Identifier key) => ContainsKey(key);
        bool IReadOnlyDictionary<Identifier, IAssetHandle>.ContainsKey(Identifier key) => ContainsKey(key);

        bool IReadOnlyDictionary<Identifier, IAssetHandle>.TryGetValue(Identifier key, out IAssetHandle value)
        {
            bool result = TryGetValue(key, out THandle? genericValue);
            value = genericValue!;
            return result;
        }

        new IEnumerator<KeyValuePair<Identifier, THandle>> GetEnumerator();
        IEnumerator<KeyValuePair<Identifier, THandle>> IEnumerable<KeyValuePair<Identifier, THandle>>.GetEnumerator() => GetEnumerator();
        IEnumerator<KeyValuePair<Identifier, IAssetHandle>> IEnumerable<KeyValuePair<Identifier, IAssetHandle>>.GetEnumerator()
        {
            foreach (var item in this)
                yield return KeyValuePair.Create<Identifier, IAssetHandle>(item.Key, item.Value);
        }
    }
}