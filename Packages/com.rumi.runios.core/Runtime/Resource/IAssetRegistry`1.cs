#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public interface IAssetRegistry<THandle> : IAssetRegistry where THandle : class, IAssetHandle
    {
        Type IAssetRegistry.handleType => typeof(THandle);
        
        new THandle this[Identifier key] { get; }
        IAssetHandle IReadOnlyDictionary<Identifier, IAssetHandle>.this[Identifier key] => this[key];

        new IEnumerable<Identifier> Keys { get; }
        IEnumerable<Identifier> IReadOnlyDictionary<Identifier, IAssetHandle>.Keys => Keys;
        
        new IEnumerable<THandle> Values { get; }
        IEnumerable<IAssetHandle> IReadOnlyDictionary<Identifier, IAssetHandle>.Values => Values;

        bool TryGetValue(Identifier key, [NotNullWhen(true)] out THandle? value);
        bool IReadOnlyDictionary<Identifier, IAssetHandle>.TryGetValue(Identifier key, out IAssetHandle value)
        {
            if (TryGetValue(key, out THandle? genericValue))
            {
                value = genericValue;
                return true;
            }
            
            value = null!;
            return false;
        }

        new IEnumerator<KeyValuePair<Identifier, THandle>> GetEnumerator();
        IEnumerator<KeyValuePair<Identifier, IAssetHandle>> IEnumerable<KeyValuePair<Identifier, IAssetHandle>>.GetEnumerator()
        {
            foreach (var item in this)
                yield return KeyValuePair.Create<Identifier, IAssetHandle>(item.Key, item.Value);
        }
    }
}