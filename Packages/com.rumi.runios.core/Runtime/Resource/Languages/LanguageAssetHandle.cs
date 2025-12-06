#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Linq;
using System.Collections.Immutable;

namespace RuniOS.Resource.Languages
{
    public sealed class LanguageAssetHandle : AssetHandle<IReadOnlyDictionary<string, string>>
    {
        public LanguageAssetHandle(IOHandler ioHandler, ImmutableArray<byte> md5Hash) : base(ioHandler, md5Hash) { }

        protected override async UniTask<IReadOnlyDictionary<string, string>?> Load()
        {
            if (await ioHandler.FileExists())
            {
                string json = await ioHandler.ReadAllText();
                return JsonConvert.DeserializeObject<Dictionary<string, string>?>(json)?.AsReadOnly();
            }
            
            return null;
        }

        protected override void Unload() { }
    }
}