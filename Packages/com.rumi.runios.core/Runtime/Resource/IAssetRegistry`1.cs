#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    /// <summary>
    /// Represents a registry that exposes strongly typed asset handles.<br/>
    /// 타입이 지정된 에셋 핸들을 노출하는 레지스트리를 나타냅니다.
    /// </summary>
    /// <typeparam name="THandle">
    /// The type of handle exposed by the registry.<br/>
    /// 레지스트리가 노출하는 핸들의 타입입니다.
    /// </typeparam>
    public interface IAssetRegistry<THandle> : IAssetRegistry where THandle : IAssetHandle
    {
        Type IAssetRegistry.handleType => typeof(THandle);

        /// <summary>
        /// Gets the strongly typed handle registered for the specified identifier.<br/>
        /// 지정된 식별자로 등록된 타입화된 핸들을 가져옵니다.
        /// </summary>
        /// <param name="key">
        /// The identifier of the asset to retrieve.<br/>
        /// 가져올 에셋의 식별자입니다.
        /// </param>
        /// <value>
        /// The matching handle, or <see langword="null"/> when the identifier is not registered.<br/>
        /// 일치하는 핸들이며, 식별자가 등록되지 않았으면 <see langword="null"/>입니다.
        /// </value>
        new THandle? this[Identifier key] { get; }
        IAssetHandle? IAssetRegistry.this[Identifier key] => this[key];

        /// <summary>
        /// Gets all strongly typed handles registered by this registry.<br/>
        /// 이 레지스트리에 등록된 모든 타입화된 핸들을 가져옵니다.
        /// </summary>
        new IEnumerable<THandle> handles { get; }
        IEnumerable<IAssetHandle> IAssetRegistry.handles => handles.Cast<IAssetHandle>();

        /// <summary>
        /// Tries to retrieve the strongly typed handle registered for the specified identifier.<br/>
        /// 지정된 식별자로 등록된 타입화된 핸들을 가져옵니다.
        /// </summary>
        /// <param name="key">
        /// The identifier to search for.<br/>
        /// 검색할 식별자입니다.
        /// </param>
        /// <param name="handle">
        /// When this method returns, contains the matching handle if one is registered; otherwise, <see langword="null"/>.<br/>
        /// 메서드가 반환될 때 일치하는 핸들이 등록되어 있으면 해당 핸들을 포함하고, 그렇지 않으면 <see langword="null"/>을 포함합니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching handle is found; otherwise, <see langword="false"/>.<br/>
        /// 일치하는 핸들을 찾으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
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

        /// <summary>
        /// Returns an enumerator for the strongly typed registered handles.<br/>
        /// 등록된 타입화된 핸들을 열거하는 열거자를 반환합니다.
        /// </summary>
        new IEnumerator<KeyValuePair<Identifier, THandle>> GetEnumerator();
        IEnumerator<KeyValuePair<Identifier, IAssetHandle>> IEnumerable<KeyValuePair<Identifier, IAssetHandle>>.GetEnumerator()
        {
            foreach (var item in this)
                yield return KeyValuePair.Create<Identifier, IAssetHandle>(item.Key, item.Value);
        }
    }
}