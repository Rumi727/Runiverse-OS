#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;

namespace RuniOS.Resource
{
    public sealed class AssetImportSettings<T>(IONode node, FileMetaData? metaData) : IAssetImportSettings<T>
    {
        public IONode node { get; } = node;
        public FileMetaData? metaData { get; private set; } = metaData;

        public T? value { get; private set; }

        public async UniTask Reload()
        {
            if (await node.file.GetEntry() is not { } entry)
            {
                value = default;
                metaData = null;

                return;
            }

            try
            {
                string text = await node.file.ReadAllText();
                value = JsonConvert.DeserializeObject<T>(text);
                metaData = entry.metaData;
            }
            catch (Exception e)
            {
                value = default;
                metaData = null;

                Debug.LogError($"Failed to load import settings at path {entry.path}! The exception is: {e}");
            }
        }

        public bool IsSameTarget(IAssetImportSettings other)
        {
            if (other is not AssetImportSettings<T> otherHandle)
                return false;

            return GetType() == other.GetType() && node.IsSameTarget(otherHandle.node) && metaData == otherHandle.metaData;
        }
    }
}