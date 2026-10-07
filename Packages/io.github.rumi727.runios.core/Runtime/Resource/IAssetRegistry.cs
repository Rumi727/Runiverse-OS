#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    /// <summary>
    /// Represents a registry that maps resource identifiers to asset handles.<br/>
    /// 리소스 식별자를 에셋 핸들에 매핑하는 레지스트리를 나타냅니다.
    /// </summary>
    public interface IAssetRegistry : IEnumerable<KeyValuePair<Identifier, IAssetHandle>>
    {
        /// <summary>
        /// Gets the unique identifier of this registry.<br/>
        /// 이 레지스트리의 고유 식별자를 가져옵니다.
        /// </summary>
        Identifier registryId { get; }

        /// <summary>
        /// Gets the priority used to order registries that provide the same <see cref="providedAssetType"/> during <see cref="AssetRefMode.automatic"/> resolution.<br/>
        /// <see cref="AssetRefMode.automatic"/> 확인 과정에서 동일한 <see cref="providedAssetType"/>을 제공하는 레지스트리의 순서를 정하는 우선순위를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Registries with higher values are searched first. The value must remain unchanged after registration.<br/>
        /// 값이 클수록 먼저 탐색하며, 레지스트리 등록 후에는 값을 변경하지 않아야 합니다.
        /// <br/><br/>
        /// The relative order of registries with equal priorities is unspecified.<br/>
        /// 동일한 우선순위 레지스트리 사이의 상대적인 순서는 지정되지 않습니다.
        /// </remarks>
        int priority { get; }

        /// <summary>
        /// Gets the asset type this registry promises to provide through its handles.<br/>
        /// 이 레지스트리가 핸들을 통해 제공한다고 약속하는 에셋 타입을 가져옵니다.
        /// </summary>
        Type providedAssetType { get; }

        /// <summary>
        /// Gets the type of handles stored by this registry.<br/>
        /// 이 레지스트리가 저장하는 핸들의 타입을 가져옵니다.
        /// </summary>
        Type handleType { get; }

        /// <summary>
        /// Gets whether this registry is currently loading resources.<br/>
        /// 이 레지스트리가 현재 리소스를 로드하고 있는지 여부를 가져옵니다.
        /// </summary>
        bool isLoading { get; }
        
        /// <summary>
        /// Gets the handle registered for the specified identifier, or <see langword="null"/> when no handle is registered.<br/>
        /// 지정된 식별자로 등록된 핸들을 가져오며, 등록된 핸들이 없으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <param name="key">
        /// The identifier of the asset to retrieve.<br/>
        /// 가져올 에셋의 식별자입니다.
        /// </param>
        /// <value>
        /// The matching asset handle, or <see langword="null"/> when the identifier is not registered.<br/>
        /// 일치하는 에셋 핸들이며, 식별자가 등록되지 않았으면 <see langword="null"/>입니다.
        /// </value>
        IAssetHandle? this[Identifier key] { get; }

        /// <summary>
        /// Gets the identifiers of all registered assets.<br/>
        /// 등록된 모든 에셋의 식별자를 가져옵니다.
        /// </summary>
        IEnumerable<Identifier> keys { get; }

        /// <summary>
        /// Gets all handles registered by this registry.<br/>
        /// 이 레지스트리에 등록된 모든 핸들을 가져옵니다.
        /// </summary>
        IEnumerable<IAssetHandle> handles { get; }

        /// <summary>
        /// Gets the number of handles registered by this registry.<br/>
        /// 이 레지스트리에 등록된 핸들의 수를 가져옵니다.
        /// </summary>
        int count { get; }

        /// <summary>
        /// Reloads all asset handles registered by this registry from the specified resource packs.<br/>
        /// 지정된 리소스 팩을 기반으로 이 레지스트리에 등록된 모든 에셋 핸들을 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">
        /// The resource packs used for loading.<br/>
        /// 로드에 사용할 리소스 팩 컬렉션입니다.
        /// </param>
        /// <param name="progress">
        /// The object that receives progress updates, or <see langword="null"/> when progress reporting is not required.<br/>
        /// 작업 진행률을 전달받는 객체이며, 진행률 보고가 필요하지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the reload.<br/>
        /// 다시 로드 작업의 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);

        /// <summary>
        /// Determines whether the specified identifier is registered.<br/>
        /// 지정된 식별자가 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="key">
        /// The identifier to check.<br/>
        /// 확인할 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the identifier is registered; otherwise, <see langword="false"/>.<br/>
        /// 식별자가 등록되어 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        bool ContainsKey(Identifier key);

        /// <summary>
        /// Tries to retrieve the handle registered for the specified identifier.<br/>
        /// 지정된 식별자로 등록된 핸들을 가져옵니다.
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
        bool TryGetHandle(Identifier key, [NotNullWhen(true)] out IAssetHandle? handle);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}