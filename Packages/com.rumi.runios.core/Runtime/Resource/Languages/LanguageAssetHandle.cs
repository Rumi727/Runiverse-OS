#nullable enable
using RuniOS.IO;
using RuniOS.Localizations;

namespace RuniOS.Resource.Languages
{
    sealed class LanguageAssetHandle : InstanceAssetHandle<LocalizationData>
    {
        internal LanguageAssetHandle(LocalizationData assetObject, IOHandler ioHandler, FileMetaData metaData) : base(assetObject)
        {
            this.ioHandler = ioHandler;
            this.metaData = metaData;
        }

        public IOHandler ioHandler { get; }
        public FileMetaData metaData { get; }

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other) || other is not LanguageAssetHandle otherHandle)
                return false;
            
            return ioHandler.IsSameTarget(otherHandle.ioHandler) && metaData == otherHandle.metaData;
        }
    }
}