#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using System.Collections.Generic;

namespace RuniOS.Resource.Languages
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

        protected override UniTask Unload() => UniTask.CompletedTask;
    }
}
