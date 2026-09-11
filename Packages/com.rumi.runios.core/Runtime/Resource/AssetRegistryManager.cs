#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Linq;

namespace RuniOS.Resource
{
    /// <summary>
    /// Manages registered asset registries and the cached provider order used for automatic lookup.<br/>
    /// 등록된 에셋 레지스트리와 자동 탐색에 사용하는 제공자 순서 캐시를 관리합니다.
    /// </summary>
    public static class AssetRegistryManager
    {
        static readonly HashSet<IAssetRegistry> registries = [];
        static readonly Dictionary<Identifier, IAssetRegistry> registriesById = [];
        static readonly Dictionary<Type, IAssetRegistry> registriesByClassType = [];
        static readonly Dictionary<Type, HashSet<IAssetRegistry>> registriesByProvidedAssetType = [];
        static readonly Dictionary<Type, IAssetRegistry[]> orderedRegistriesByProvidedAssetType = [];

        /// <summary>
        /// Creates and registers an asset registry of the specified type.<br/>
        /// 지정된 타입의 에셋 레지스트리를 생성하고 등록합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The type of registry to create and register.<br/>
        /// 생성하고 등록할 레지스트리의 타입입니다.
        /// </typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another registered registry already uses the same <see cref="IAssetRegistry.registryId"/>.<br/>
        /// 등록된 다른 레지스트리가 같은 <see cref="IAssetRegistry.registryId"/>를 사용하면 발생합니다.
        /// </exception>
        public static void Register<T>() where T : IAssetRegistry, new()
        {
            T registry = new T();
            if (!registriesById.TryAdd(registry.registryId, registry))
                throw new InvalidOperationException($"Registry ID conflict: {registry.registryId}");

            registries.Add(registry);
            registriesByClassType[registry.GetType()] = registry;

            if (!registriesByProvidedAssetType.TryGetValue(registry.providedAssetType, out HashSet<IAssetRegistry> registrySet))
                registriesByProvidedAssetType[registry.providedAssetType] = registrySet = [];

            registrySet.Add(registry);
            orderedRegistriesByProvidedAssetType[registry.providedAssetType] = [..registrySet.OrderByDescending(x => x.priority)];
        }

        /// <summary>
        /// Unregisters the asset registry of the specified type when it is registered.<br/>
        /// 지정된 타입의 에셋 레지스트리가 등록되어 있으면 등록을 해제합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The type of registry to unregister.<br/>
        /// 등록을 해제할 레지스트리의 타입입니다.
        /// </typeparam>
        public static void Unregister<T>() where T : IAssetRegistry, new()
        {
            T? registry = Get<T>();
            if (registry == null)
                return;

            registries.Remove(registry);

            registriesById.Remove(registry.registryId);
            registriesByClassType.Remove(registry.GetType());

            if (registriesByProvidedAssetType.TryGetValue(registry.providedAssetType, out HashSet<IAssetRegistry> registrySet))
            {
                registrySet.Remove(registry);
                if (registrySet.Count == 0)
                {
                    registriesByProvidedAssetType.Remove(registry.providedAssetType);
                    orderedRegistriesByProvidedAssetType.Remove(registry.providedAssetType);
                }
                else
                    orderedRegistriesByProvidedAssetType[registry.providedAssetType] = [..registrySet.OrderByDescending(x => x.priority)];
            }
        }

        /// <summary>
        /// Gets the registry registered with the specified identifier.<br/>
        /// 지정된 식별자로 등록된 레지스트리를 가져옵니다.
        /// </summary>
        /// <param name="registryId">
        /// The identifier of the registry to retrieve.<br/>
        /// 가져올 레지스트리의 식별자입니다.
        /// </param>
        /// <returns>
        /// The matching registry, or <see langword="null"/> when no registry is registered with the identifier.<br/>
        /// 일치하는 레지스트리를 반환하며, 해당 식별자로 등록된 레지스트리가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IAssetRegistry? Get(Identifier registryId) => registriesById.GetValueOrDefault(registryId);

        /// <summary>
        /// Gets the registry registered with the specified registry type.<br/>
        /// 지정된 레지스트리 타입으로 등록된 레지스트리를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The registry type to retrieve.<br/>
        /// 가져올 레지스트리의 타입입니다.
        /// </typeparam>
        /// <returns>
        /// The matching registry, or <see langword="null"/> when the type is not registered.<br/>
        /// 일치하는 레지스트리를 반환하며, 해당 타입이 등록되지 않았으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static T? Get<T>() where T : IAssetRegistry => (T?)Get(typeof(T));

        /// <summary>
        /// Gets the registry registered with the specified registry type.<br/>
        /// 지정된 레지스트리 타입으로 등록된 레지스트리를 가져옵니다.
        /// </summary>
        /// <param name="registryType">
        /// The type of the registry to retrieve.<br/>
        /// 가져올 레지스트리의 타입입니다.
        /// </param>
        /// <returns>
        /// The matching registry, or <see langword="null"/> when the type is not registered.<br/>
        /// 일치하는 레지스트리를 반환하며, 해당 타입이 등록되지 않았으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IAssetRegistry? Get(Type registryType) => registriesByClassType.GetValueOrDefault(registryType);

        /// <summary>
        /// Gets a handle by searching registries that provide <typeparamref name="TAsset"/> in descending priority order.<br/>
        /// <typeparamref name="TAsset"/>를 제공하는 레지스트리를 우선순위 내림차순으로 탐색하여 핸들을 가져옵니다.
        /// </summary>
        /// <typeparam name="TAsset">
        /// The asset type promised by the compatible registries.<br/>
        /// 호환 레지스트리가 제공한다고 약속한 에셋 타입입니다.
        /// </typeparam>
        /// <param name="assetId">
        /// The identifier to search for in each compatible registry.<br/>
        /// 호환 레지스트리에서 검색할 에셋 식별자입니다.
        /// </param>
        /// <returns>
        /// The first handle containing <paramref name="assetId"/>, or <see langword="null"/> when no registry contains it.<br/>
        /// <paramref name="assetId"/>를 포함한 첫 핸들을 반환하며, 포함한 레지스트리가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Each compatible registry is checked until the identifier is found; a higher-priority registry that does not contain the identifier does not stop the search.<br/>
        /// 식별자를 찾을 때까지 호환 레지스트리를 차례로 확인하며, 높은 우선순위의 레지스트리에 식별자가 없더라도 탐색을 중단하지 않습니다.
        /// </remarks>
        public static IAssetHandle<TAsset>? GetHandle<TAsset>(Identifier assetId) where TAsset : notnull => GetHandle(typeof(TAsset), assetId) as IAssetHandle<TAsset>;

        /// <summary>
        /// Gets a handle by searching registries that provide the specified asset type in descending priority order.<br/>
        /// 지정된 에셋 타입을 제공하는 레지스트리를 우선순위 내림차순으로 탐색하여 핸들을 가져옵니다.
        /// </summary>
        /// <param name="providedAssetType">
        /// The asset type promised by the registries to search.<br/>
        /// 검색할 레지스트리들이 제공한다고 약속한 에셋 타입입니다.
        /// </param>
        /// <param name="assetId">
        /// The identifier to search for in each compatible registry.<br/>
        /// 호환 레지스트리에서 검색할 에셋 식별자입니다.
        /// </param>
        /// <returns>
        /// The first handle containing <paramref name="assetId"/>, or <see langword="null"/> when no registry contains it.<br/>
        /// <paramref name="assetId"/>를 포함한 첫 핸들을 반환하며, 포함한 레지스트리가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Each compatible registry is checked until the identifier is found; a higher-priority registry that does not contain the identifier does not stop the search.<br/>
        /// 식별자를 찾을 때까지 호환 레지스트리를 차례로 확인하며, 높은 우선순위의 레지스트리에 식별자가 없더라도 탐색을 중단하지 않습니다.
        /// </remarks>
        public static IAssetHandle? GetHandle(Type providedAssetType, Identifier assetId)
        {
            if (!orderedRegistriesByProvidedAssetType.TryGetValue(providedAssetType, out IAssetRegistry[] candidates))
                return null;

            foreach (IAssetRegistry registry in candidates)
            {
                if (registry.TryGetHandle(assetId, out IAssetHandle? handle))
                    return handle;
            }

            return null;
        }

        /// <summary>
        /// Gets all registered asset registries.<br/>
        /// 등록된 모든 에셋 레지스트리를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A read-only set containing all registered registries.<br/>
        /// 등록된 모든 레지스트리를 포함하는 읽기 전용 집합을 반환합니다.
        /// </returns>
        public static ReadOnlySet<IAssetRegistry> GetAll() => registries.AsReadOnly();

        /// <summary>
        /// Gets all registries that promise to provide the specified asset type.<br/>
        /// 지정된 에셋 타입을 제공한다고 약속한 모든 레지스트리를 가져옵니다.
        /// </summary>
        /// <param name="providedAssetType">
        /// The asset type promised by the matching registries.<br/>
        /// 일치하는 레지스트리들이 제공한다고 약속한 에셋 타입입니다.
        /// </param>
        /// <returns>
        /// A read-only set of matching registries, or an empty set when none are registered.<br/>
        /// 일치하는 레지스트리의 읽기 전용 집합을 반환하며, 등록된 레지스트리가 없으면 빈 집합을 반환합니다.
        /// </returns>
        public static ReadOnlySet<IAssetRegistry> GetAllForAsset(Type providedAssetType)
        {
            if (registriesByProvidedAssetType.TryGetValue(providedAssetType, out var list))
                return list.AsReadOnly();

            return ReadOnlySet<IAssetRegistry>.empty;
        }
    }
}