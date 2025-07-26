#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniEngine.IO;
using System;

namespace RuniEngine.Resource
{
    public sealed class ResourcePackReference : IDisposable
    {
        public static readonly FilePath assetsFolderName = "assets";
        public static readonly FilePath infoPath = "pack.json";

        public static readonly ResourcePackReference emptyPack = new ResourcePackReference();
        static ResourcePackReference? defaultPack;

        ResourcePackReference()
        {
            identifier = PackIdentifier.empty;
            
            rootFolder = IOHandler.empty;
            assetFolder = IOHandler.empty;
            infoFile = IOHandler.empty;

            metaData = new PackMetaData(string.Empty);
        }

        ResourcePackReference(PackIdentifier identifier, IOHandler folder)
        {
            this.identifier = identifier;
            
            rootFolder = folder;
            assetFolder = folder.CreateChild(assetsFolderName);
            infoFile = folder.CreateChild(infoPath);
        }

        public static async UniTask<ResourcePackReference> GetDefaultPack() => (defaultPack ??= await Create(PackIdentifier.CreateByID("vanilla"), StreamingIOHandler.instance)) ?? emptyPack;

        public static UniTask<ResourcePackReference?> Create(FileIOHandler handler) => Create(PackIdentifier.CreateByPath(handler.targetPath), handler);
        public static async UniTask<ResourcePackReference?> Create(PackIdentifier packIdentifier, IOHandler handler)
        {
            ResourcePackReference resourcePack = new ResourcePackReference(packIdentifier, handler.Recreate());
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
