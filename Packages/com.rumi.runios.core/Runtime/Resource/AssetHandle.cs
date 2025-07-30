#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System;
using System.Collections.Generic;

namespace RuniOS.Resource
{
    public abstract class AssetHandle
    {
        public IOHandler ioHandler { get; }

        public object? assetObject { get; private set; }


        protected AssetHandle(IOHandler ioHandler) => this.ioHandler = ioHandler;

        internal readonly List<WeakReference<AssetScope>> assetScopes = new List<WeakReference<AssetScope>>();

        public async UniTask<AssetScope?> GetScope()
        {
            AssetScope? scope = null;
            if (assetObject == null)
                await Reload();

            if (assetObject != null)
                scope = CreateScope(assetObject);

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
                    assetObject = null;
                    Unload().Forget();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"Failed to unload asset at path {ioHandler.fullPath}!");
                }
            }
        }

        public async UniTask Reload()
        {
            try
            {
                assetObject = await Load();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to load asset at path {ioHandler.fullPath}!");
            }
        }

        protected abstract AssetScope CreateScope(object assets);

        protected abstract UniTask<object?> Load();
        protected abstract UniTask Unload();
    }
}
