#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RuniEngine.Resource
{
    public static class ResourceManager
    {
        static readonly Dictionary<Type, AssetRegistry> _assetRegistries = new();
        public static IReadOnlyDictionary<Type, AssetRegistry> assetRegistries { get; } = _assetRegistries.AsReadOnly();

        public static void RegisterAssetRegistry<T>() where T : AssetRegistry, new() => RegisterAssetRegistry(typeof(T));
        public static void RegisterAssetRegistry(Type type)
        {
            if (type.IsAbstract)
                throw new ArgumentException($"Type '{type.FullName}' cannot be abstract.", nameof(type));

            if (!type.IsSubtypeOf(typeof(AssetRegistry)))
                throw new ArgumentException($"Type '{type.FullName}' must inherit from AssetRegistry.", nameof(type));

            if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                throw new ArgumentException($"Type '{type.FullName}' must have a public parameterless constructor.", nameof(type));

            if (!_assetRegistries.ContainsKey(type))
                _assetRegistries.Add(type, (AssetRegistry)Activator.CreateInstance(type));
        }

        public static void UnregisterAssetRegistry<T>() where T : AssetRegistry, new() => UnregisterAssetRegistry(typeof(T));
        public static void UnregisterAssetRegistry(Type type) => _assetRegistries.Remove(type);



        public static AssetHandle? GetAssetHandle<TRegistry>(Identifier identifier) where TRegistry : AssetRegistry => GetAssetHandle(typeof(TRegistry), identifier);

        public static AssetHandle? GetAssetHandle(Type registryType, Identifier identifier)
        {
            if (assetRegistries.TryGetValue(registryType, out AssetRegistry assetRegistry) && assetRegistry.assetHandles.TryGetValue(identifier, out AssetHandle handle))
                return handle;

            return null;
        }

        public static async UniTask<AssetScope?> GetAssetScope<TRegistry>(Identifier identifier) where TRegistry : AssetRegistry => await GetAssetScope(typeof(TRegistry), identifier);

        public static UniTask<AssetScope?> GetAssetScope(Type registryType, Identifier identifier)
        {
            if (assetRegistries.TryGetValue(registryType, out AssetRegistry assetRegistry) && assetRegistry.assetHandles.TryGetValue(identifier, out AssetHandle handle))
                return handle.GetScope();

            return new UniTask<AssetScope?>(null);
        }
    }
}
