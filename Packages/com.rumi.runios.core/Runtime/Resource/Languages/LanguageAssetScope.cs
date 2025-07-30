using System.Collections.Generic;

namespace RuniOS.Resource.Languages
{
    public class LanguageAssetScope : AssetScope
    {
        public Dictionary<string, string> texts;

        protected internal LanguageAssetScope(AssetHandle handle, Dictionary<string, string> asset) : base(handle) => texts = asset;
    }
}
