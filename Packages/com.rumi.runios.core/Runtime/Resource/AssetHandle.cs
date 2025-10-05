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
        
        public bool isLoading { get; private set; }

        

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
            bool scopeFound = false;
            for (int i = assetScopes.Count - 1; i >= 0; i--)
            {
                WeakReference<AssetScope> weakRef = assetScopes[i];
        
                if (weakRef.TryGetTarget(out AssetScope outAssetScope))
                {
                    // 1. 현재 제거하려는 Scope를 찾았을 경우
                    if (assetScope == outAssetScope)
                    {
                        assetScopes.RemoveAt(i);
                        scopeFound = true;
                        
                        break; 
                    }
                }
                else
                {
                    // 2. WeakReference가 만료되었을 경우 (GC된 경우)
                    // 청소 목적으로 제거
                    assetScopes.RemoveAt(i);
                }
            }

            if (!scopeFound)
            {
                Debug.LogWarning
                (
                    $"Invalid or already-returned AssetScope detected! Scope for asset '{ioHandler.fullPath}' was not found in the handle's list.\n" +
                    "Possible causes: 1. Scope was returned twice. 2. Scope was disposed outside of its lifecycle."
                );
                
                return;
            }

            if (assetScopes.Count <= 0)
            {
                try
                {
                    assetObject = null;
                    Unload();
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
            if (isLoading)
                return;
            
            isLoading = true;

            try
            {
                assetObject = await Load();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to load asset at path {ioHandler.fullPath}!");
            }
            finally
            {
                isLoading = false;
            }
        }

        protected abstract AssetScope CreateScope(object assets);

        protected abstract UniTask<object?> Load();
        protected abstract void Unload();
    }
}
