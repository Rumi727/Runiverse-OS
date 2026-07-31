#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Linq;

namespace RuniOS.Resource
{
    public static class AssetRegistryManager
    {
        static readonly HashSet<IAssetRegistry> registries = [];
        static readonly Dictionary<Identifier, IAssetRegistry> registriesById = [];
        static readonly Dictionary<Type, IAssetRegistry> registriesByClassType = [];
        static readonly Dictionary<Type, HashSet<IAssetRegistry>> registriesByAssetType = [];
        static readonly Dictionary<Type, IAssetRegistry> defaultRegistriesByAssetType = [];

        public static void Register<T>() where T : IAssetRegistry, new()
        {
            T registry = new T();
            if (!registriesById.TryAdd(registry.registryId, registry))
                throw new InvalidOperationException($"Registry ID conflict: {registry.registryId}");

            registries.Add(registry);
            registriesByClassType[registry.GetType()] = registry;

            if (!registriesByAssetType.TryGetValue(registry.assetType, out var list))
                registriesByAssetType[registry.assetType] = list = [];

            list.Add(registry);

            if (registry.isDefault)
            {
                if (defaultRegistriesByAssetType.TryGetValue(registry.assetType, out var currentDefault))
                    Debug.RuntimeLogWarning($"Default registry for {registry.assetType.Name} replaced: {currentDefault.registryId} -> {registry.registryId}");

                defaultRegistriesByAssetType[registry.assetType] = registry;
            }
        }

        public static void Unregister<T>() where T : IAssetRegistry, new()
        {
            T? registry = Get<T>();
            if (registry == null)
                return;

            registries.Remove(registry);

            registriesById.Remove(registry.registryId);
            registriesByClassType.Remove(registry.GetType());

            if (registriesByAssetType.TryGetValue(registry.assetType, out var list))
            {
                list.Remove(registry);
                if (list.Count == 0)
                    registriesByAssetType.Remove(registry.assetType);
            }

            defaultRegistriesByAssetType.Remove(registry.assetType);
        }

        public static IAssetRegistry? Get(Identifier registryId) => registriesById.GetValueOrDefault(registryId);

        public static T? Get<T>() where T : IAssetRegistry => (T?)Get(typeof(T));
        public static IAssetRegistry? Get(Type registryType) => registriesByClassType.GetValueOrDefault(registryType);

        public static IAssetRegistry? GetDefaultForAsset<TAsset>() => GetDefaultForAsset(typeof(TAsset));
        public static IAssetRegistry? GetDefaultForAsset(Type assetType) => defaultRegistriesByAssetType.GetValueOrDefault(assetType);

        public static ReadOnlySet<IAssetRegistry> GetAll() => registries.AsReadOnly();

        public static ReadOnlySet<IAssetRegistry> GetAllForAsset(Type assetType)
        {
            if (registriesByAssetType.TryGetValue(assetType, out var list))
                return list.AsReadOnly();

            return ReadOnlySet<IAssetRegistry>.empty;
        }
    }
}