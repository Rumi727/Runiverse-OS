#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniEngine.IO;
using System.Collections.Generic;

namespace RuniEngine.Resource
{
    public class LanguageAssetHandle : AssetHandle
    {
        public LanguageAssetHandle(IOHandler ioHandler) : base(ioHandler) { }

        protected override AssetScope CreateScope(object asset) => new LanguageAssetScope(this, (Dictionary<string, string>)asset);

        protected override async UniTask<object?> Load()
        {
            string json = await ioHandler.ReadAllText();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        protected override void Unload() { }
    }
}
