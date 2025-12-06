#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Linq;

namespace RuniOS.Resource
{
    public static class AssetRegistryManager
    {
        static readonly HashSet<AssetRegistry> registries = new();
        static readonly Dictionary<Identifier, AssetRegistry> registriesById = new();
        static readonly Dictionary<Type, AssetRegistry> registriesByClassType = new();
        static readonly Dictionary<Type, HashSet<AssetRegistry>> registriesByAssetType = new();
        static readonly Dictionary<Type, AssetRegistry> defaultRegistriesByAssetType = new();

        public static void Register<T>() where T : AssetRegistry, new() => Register(new T());

        public static void Register(AssetRegistry registry)
        {
            if (!registriesById.TryAdd(registry.registryId, registry))
                throw new InvalidOperationException($"Registry ID conflict: {registry.registryId}");

            registries.Add(registry);
            registriesByClassType[registry.GetType()] = registry;

            if (!registriesByAssetType.TryGetValue(registry.assetType, out var list))
                registriesByAssetType[registry.assetType] = list = new HashSet<AssetRegistry>();
            
            list.Add(registry);

            if (registry.isDefault)
            {
                if (defaultRegistriesByAssetType.TryGetValue(registry.assetType, out var currentDefault))
                    Debug.LogWarning($"Default registry for {registry.assetType.Name} replaced: {currentDefault.registryId} -> {registry.registryId}");
                
                defaultRegistriesByAssetType[registry.assetType] = registry;
            }
        }

        public static void Unregister(AssetRegistry registry)
        {
            registries.Remove(registry);
            
            registriesById.Remove(registry.registryId);
            registriesByClassType.Remove(registry.GetType());

            if (registriesByAssetType.TryGetValue(registry.assetType, out var list))
            {
                list.Remove(registry);
                if (list.Count == 0)
                    registriesByAssetType.Remove(registry.assetType);
            }

            if (defaultRegistriesByAssetType.TryGetValue(registry.assetType, out var currentDefault) && currentDefault == registry)
                defaultRegistriesByAssetType.Remove(registry.assetType);
        }

        public static AssetRegistry? Get(Identifier registryId) => registriesById.GetValueOrDefault(registryId);

        public static T? Get<T>() where T : AssetRegistry => (T?)Get(typeof(T));
        public static AssetRegistry? Get(Type registryType) => registriesByClassType.GetValueOrDefault(registryType);

        public static AssetRegistry? GetDefaultForAsset<TAsset>() => GetDefaultForAsset(typeof(TAsset));
        public static AssetRegistry? GetDefaultForAsset(Type assetType) => defaultRegistriesByAssetType.GetValueOrDefault(assetType);

        public static ReadOnlySet<AssetRegistry> GetAll() => registries.AsReadOnly();
        
        public static ReadOnlySet<AssetRegistry> GetAllForAsset(Type assetType)
        {
            if (registriesByAssetType.TryGetValue(assetType, out var list))
                return list.AsReadOnly();
            
            return ReadOnlySet<AssetRegistry>.empty;
        }
    }
}