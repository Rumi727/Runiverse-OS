#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using System;

namespace RuniOS.Resource
{
    /// <summary>
    /// 리소스팩의 참조입니다.
    /// 실제 데이터는 레지스트리에 담깁니다.
    /// </summary>
    public sealed class ResourcePack : IDisposable
    {
        public static readonly FilePath assetsFolderName = "assets";
        public static readonly FilePath infoPath = "pack.json";

        public static readonly ResourcePack emptyPack = new ResourcePack();
        static ResourcePack? defaultPack;

        public static readonly PackIdentifier defaultPackIdentifier = PackIdentifier.CreateByID("vanilla");

        ResourcePack()
        {
            identifier = PackIdentifier.empty;
            
            rootFolder = IOHandler.empty;
            assetFolder = IOHandler.empty;
            infoFile = IOHandler.empty;

            metaData = new PackMetaData(string.Empty);
        }

        ResourcePack(PackIdentifier identifier, IOHandler folder)
        {
            this.identifier = identifier;
            
            rootFolder = folder.Recreate();
            assetFolder = folder.CreateChild(assetsFolderName);
            infoFile = folder.CreateChild(infoPath);
        }

        public static async UniTask<ResourcePack> GetDefaultPack() => (defaultPack ??= await Create(PackIdentifier.CreateByID("vanilla"), StreamingIOHandler.instance)) ?? emptyPack;

        public static UniTask<ResourcePack?> Create(FileIOHandler handler) => Create(PackIdentifier.CreateByPath(handler.targetPath), handler);
        public static async UniTask<ResourcePack?> Create(PackIdentifier packIdentifier, IOHandler handler)
        {
            ResourcePack resourcePack = new ResourcePack(packIdentifier, handler.Recreate());
            if (!await resourcePack.infoFile.FileExists())
                return null;
            
            try
            {
                resourcePack.metaData = JsonConvert.DeserializeObject<PackMetaData>(await resourcePack.infoFile.ReadAllText());
                resourcePack.isValid = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                resourcePack.isValid = false;
            }

            if (!resourcePack.isValid)
                return null;

            ResourceManager.internalLoadedResourcePacks.Add(packIdentifier, resourcePack);
            return resourcePack;
        }
        
        public PackIdentifier identifier { get; }

        public IOHandler rootFolder { get; }
        public IOHandler assetFolder { get; }
        public IOHandler infoFile { get; }

        public PackMetaData metaData { get; private set; }

        public bool isValid { get; private set; }

        public void Dispose()
        {
            isValid = false;
            ResourceManager.internalLoadedResourcePacks.Remove(identifier);
        }
    }
}
