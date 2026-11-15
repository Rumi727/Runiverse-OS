#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.NBS;

namespace RuniOS.Resource.NBS
{
    /// <summary>
    /// Loads one NBS file of a resource pack as a <see cref="NoteBlockClip"/>.<br/>
    /// 리소스팩의 NBS 파일 하나를 <see cref="NoteBlockClip"/>으로 로드합니다.
    /// </summary>
    /// <param name="node">
    /// The NBS file.<br/>
    /// NBS 파일입니다.
    /// </param>
    /// <param name="fileMetaData">
    /// The metadata used to detect file changes on reload.<br/>
    /// 리로드할 때 파일 변경을 감지하는 데 사용하는 메타데이터입니다.
    /// </param>
    public sealed class NBSAssetHandle(IONode node, FileMetaData fileMetaData) : AssetHandle<NoteBlockClip>(node, fileMetaData)
    {
        /// <inheritdoc/>
        protected override async UniTask<NoteBlockClip?> Load()
        {
            NoteBlockClip clip = await NoteBlockClip.LoadAsync(node);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        /// <inheritdoc/>
        protected override void Unload(NoteBlockClip unloadedAsset) => Object.DestroyImmediate(unloadedAsset);
    }
}
