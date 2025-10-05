#nullable enable
using System.Collections.Generic;

namespace RuniOS.Resource.Languages
{
    public sealed class LanguageAssetScope : AssetScope
    {
        public IReadOnlyDictionary<string, string> texts { get; }

        internal LanguageAssetScope(AssetHandle handle, IReadOnlyDictionary<string, string> asset) : base(handle) => texts = asset;
    }
}
