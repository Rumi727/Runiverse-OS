#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public interface IAssetRegistry<THandle> : IAssetRegistry where THandle : class, IAssetHandle
    {
        Type IAssetRegistry.handleType => typeof(THandle);
        
        new THandle? this[Identifier key] { get; }
        IAssetHandle? IAssetRegistry.this[Identifier key] => this[key];
        
        new IEnumerable<THandle> handles { get; }
        IEnumerable<IAssetHandle> IAssetRegistry.handles => handles;

        bool TryGetHandle(Identifier key, [NotNullWhen(true)] out THandle? handle);
        bool IAssetRegistry.TryGetHandle(Identifier key, [NotNullWhen(true)] out IAssetHandle? handle)
        {
            if (TryGetHandle(key, out THandle? genericValue))
            {
                handle = genericValue;
                return true;
            }
            
            handle = null!;
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