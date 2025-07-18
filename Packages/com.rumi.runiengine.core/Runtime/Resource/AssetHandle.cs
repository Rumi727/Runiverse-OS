#nullable enable
using Cysharp.Threading.Tasks;
using RuniEngine.IO;
using System;
using System.Collections.Generic;

namespace RuniEngine.Resource
{
    public abstract class AssetHandle : ICloneable
    {
        public IOHandler ioHandler { get; }

        protected internal AssetHandle(IOHandler ioHandler) => this.ioHandler = ioHandler;

        internal readonly List<WeakReference<AssetScope>> assetScopes = new List<WeakReference<AssetScope>>();

        public async UniTask<AssetScope?> GetScope()
        {
            AssetScope? scope = null;
            object? asset = null;

            try
            {
                asset = await Load();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to load asset at path {ioHandler.childFullPath}!");
            }

            if (asset != null)
                scope = CreateScope(asset);

            if (scope != null)
                assetScopes.Add(new WeakReference<AssetScope>(scope));

            return scope;
        }

        internal void ReturnScope(AssetScope assetScope)
        {
            int lastCount = assetScopes.Count;
            assetScopes.RemoveAll(x =>
            {
                if (x.TryGetTarget(out AssetScope outAssetScope))
                    return assetScope == outAssetScope;

                return true;
            });

            if (lastCount == assetScopes.Count)
            {
                Debug.LogWarning("Attempted to return an invalid asset scope!");
                return;
            }

            if (assetScopes.Count <= 0)
            {
                try
                {
                    Unload();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"Failed to unload asset at path {ioHandler.childFullPath}!");
                }
            }
        }

        protected abstract AssetScope CreateScope(object assets);

        protected abstract UniTask<object?> Load();
        protected abstract void Unload();

        public object Clone() => MemberwiseClone();
    }
}
