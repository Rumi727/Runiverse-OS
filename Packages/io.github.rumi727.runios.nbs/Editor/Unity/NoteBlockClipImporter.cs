#nullable enable
using RuniOS.NBS;
using UnityEditor.AssetImporters;

namespace RuniOS.Editor.NBS
{
    /// <summary>
    /// Imports <c>.nbs</c> files as <see cref="NoteBlockClip"/> assets.<br/>
    /// <c>.nbs</c> 파일을 <see cref="NoteBlockClip"/> 에셋으로 가져옵니다.
    /// </summary>
    [ScriptedImporter(1, "nbs")]
    public sealed class NoteBlockClipImporter : ScriptedImporter
    {
        /// <inheritdoc/>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using FileStream stream = File.OpenRead(ctx.assetPath);

            NoteBlockClip clip = NoteBlockClip.Create(stream);
            clip.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            ctx.AddObjectToAsset("main", clip);
            ctx.SetMainObject(clip);
        }
    }
}
