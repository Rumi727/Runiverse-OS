#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using System.Collections.Generic;

namespace RuniOS.Resource.Languages
{
    public sealed class LanguageAssetHandle : AssetHandle
    {
        public override bool isAlwaysLoaded => true;

        public LanguageAssetHandle(IOHandler ioHandler) : base(ioHandler) { }

        protected override async UniTask<object?> Load()
        {
            if (await ioHandler.FileExists())
            {
                string json = await ioHandler.ReadAllText();
                return JsonConvert.DeserializeObject<Dictionary<string, string>?>(json);
            }
            
            return false;
        }

        protected override void Unload() { }
        
        protected override AssetScope CreateScope(object asset) => new LanguageAssetScope(this, ((Dictionary<string, string>)asset).AsReadOnly());
    }
}
