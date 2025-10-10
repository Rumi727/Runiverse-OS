#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System;
using System.Collections.Generic;

namespace RuniOS.Resource
{
    public abstract class SimpleAssetRegistry : AssetRegistry
    {
        public sealed override bool isLoading => _isLoading;
        bool _isLoading;
        
        public abstract WildcardPatterns assetFilter { get; }
        
        protected abstract AssetHandle CreateHandle(IOHandler ioHandler, string md5Hash);

        public sealed override async UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }

            _isLoading = true;
            BeginTracking();

            try
            {
                progress?.Report(0);

                List<UniTask> uniTasks = new List<UniTask>();
                int count = 0;

                foreach (var resourcePack in resourcePacks)
                {
                    await foreach ((string nameSpace, IOHandler registryHandler) in GetRegistryFolder(resourcePack))
                    {
                        foreach (var ioHandler in await registryHandler.GetFileHandlers(assetFilter))
                        {
                            uniTasks.Add(UniTask.Defer(Method));

                            async UniTask Method()
                            {
                                try
                                {
                                    string name = ioHandler.fullPath.GetFileNameWithoutExtension();
                                    await OnAssetLoop(new Identifier(nameSpace, name), ioHandler, CreateHandle(ioHandler, BitConverter.ToString(await ioHandler.GetMD5Hash())));
                                }
                                catch (Exception e)
                                {
                                    Debug.Log($"An exception occurred while loading {ioHandler.fullPath} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                                }

                                progress?.Report((float)++count / uniTasks.Count);
                            }
                        }
                    }
                }

                await UniTask.WhenAll(uniTasks);
            }
            finally
            {
                try
                {
                    progress?.Report(1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                EndTracking();
                _isLoading = false;
            }
        }

        protected virtual UniTask OnAssetLoop(Identifier identifier, IOHandler ioHandler, AssetHandle assetHandle)
        {
            RecordAssetHandle(identifier, assetHandle);
            return UniTask.CompletedTask;
        }
    }
}
