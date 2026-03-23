#nullable enable
using RuniOS.IO;
using RuniOS.Localizations;

namespace RuniOS.Resource.Languages
{
    sealed class LanguageAssetHandle : InstanceAssetHandle<LocalizationData>
    {
        internal LanguageAssetHandle(LocalizationData assetObject, IIOEntry entry, FileMetaData metaData) : base(assetObject)
        {
            this.entry = entry;
            this.metaData = metaData;
        }

        public IIOEntry entry { get; }
        public FileMetaData metaData { get; }

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other) || other is not LanguageAssetHandle otherHandle)
                return false;
            
            return entry.IsSameTarget(otherHandle.entry) && metaData == otherHandle.metaData;
        }
    }
}