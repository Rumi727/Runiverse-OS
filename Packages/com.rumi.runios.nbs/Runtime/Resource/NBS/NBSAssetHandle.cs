#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.NBS;

namespace RuniOS.Resource.NBS
{
    /// <summary>
    /// Loads and scope-tracks one NBS resource file.<br/>
    /// NBS 리소스 파일 하나를 로드하고 스코프를 추적합니다.
    /// </summary>
    public sealed class NBSAssetHandle(IONode node, FileMetaData metaData) : AssetHandle<NoteBlockClip>(node, metaData)
    {
        protected override async UniTask<NoteBlockClip> Load()
        {
            await using Stream stream = await node.file.OpenRead();
            return await UniTask.RunOnThreadPool(() => NBSReader.Read(stream));
        }

        protected override void Unload() { }
    }
}
