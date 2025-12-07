#nullable enable
using RuniOS.IO;
using RuniOS.Localizations;
using System.Collections.Immutable;

namespace RuniOS.Resource.Languages
{
    sealed class LanguageAssetHandle : InstanceAssetHandle<LocalizationData>
    {
        internal LanguageAssetHandle(LocalizationData assetObject, IOHandler ioHandler, ImmutableArray<byte> md5Hash) : base(assetObject)
        {
            this.ioHandler = ioHandler;
            this.md5Hash = md5Hash;
        }

        public IOHandler ioHandler { get; }
        public ImmutableArray<byte> md5Hash { get; }

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other) || other is not LanguageAssetHandle otherHandle)
                return false;
            
            return ioHandler.IsSameTarget(otherHandle.ioHandler) && md5Hash.SequenceEqual(otherHandle.md5Hash);
        }
    }
}