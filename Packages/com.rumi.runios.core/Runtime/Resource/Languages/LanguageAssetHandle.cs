#nullable enable
using RuniOS.IO;
using RuniOS.Localizations;

namespace RuniOS.Resource.Languages
{
    sealed class LanguageAssetHandle : InstanceAssetHandle<LocalizationData>
    {
        internal LanguageAssetHandle(LocalizationData assetObject, IONode node, IOMetaData metaData) : base(assetObject)
        {
            this.node = node;
            this.metaData = metaData;
        }

        public IONode node { get; }
        public IOMetaData metaData { get; }

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other) || other is not LanguageAssetHandle otherHandle)
                return false;
            
            return node.IsSameTarget(otherHandle.node) && metaData == otherHandle.metaData;
        }
    }
}