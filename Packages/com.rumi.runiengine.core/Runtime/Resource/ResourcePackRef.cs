#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniEngine.IO;
using System;
using System.Collections.Generic;

namespace RuniEngine.Resource
{
    public sealed class ResourcePackRef
    {
        static readonly Dictionary<PackIdentifier, ResourcePackRef> _resourcePacks = new();
        public static IReadOnlyDictionary<PackIdentifier, ResourcePackRef> resourcePacks { get; } = _resourcePacks.AsReadOnly();



        public static readonly FilePath assetsFolderName = "assets";
        public static readonly FilePath infoPath = "pack.json";

        public static readonly ResourcePackRef emptyPack = new ResourcePackRef();
        static ResourcePackRef? defaultPack;

        ResourcePackRef()
        {
            rootFolder = IOHandler.empty;
            assetFolder = IOHandler.empty;
            infoFile = IOHandler.empty;

            info = new ResourcePackInfo(string.Empty);
        }

        ResourcePackRef(IOHandler folder)
        {
            rootFolder = folder;
            assetFolder = folder.CreateChild(assetsFolderName);
            infoFile = folder.CreateChild(infoPath);
        }

        public static async UniTask<ResourcePackRef> GetDefaultPack() => (defaultPack ??= await Create(PackIdentifier.Create("vanilla"), StreamingIOHandler.instance)) ?? emptyPack;

        public static UniTask<ResourcePackRef?> Create(FileIOHandler handler) => Create(PackIdentifier.Create(handler.realPath), handler);
        public static async UniTask<ResourcePackRef?> Create(PackIdentifier packIdentifier, IOHandler handler)
        {
            ResourcePackRef resourcePack = new ResourcePackRef(handler);
            try
            {
                resourcePack.info = JsonConvert.DeserializeObject<ResourcePackInfo>(await resourcePack.infoFile.ReadAllText());
                resourcePack.isValid = true;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                resourcePack.isValid = false;
            }

            if (!resourcePack.isValid)
                return null;

            _resourcePacks.Add(packIdentifier, resourcePack);

            return resourcePack;
        }

        public IOHandler rootFolder { get; }
        public IOHandler assetFolder { get; }
        public IOHandler infoFile { get; }

        public ResourcePackInfo info { get; private set; }

        public bool isValid { get; private set; }
    }
}
