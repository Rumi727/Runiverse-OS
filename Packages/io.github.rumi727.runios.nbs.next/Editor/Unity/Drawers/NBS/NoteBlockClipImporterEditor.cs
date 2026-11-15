#nullable enable
using RuniOS.Editor.NBS;
using UnityEditor.AssetImporters;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Draws <see cref="NoteBlockClipImporter"/>. The imported clip is shown in the preview area rather than as a separate imported object.<br/>
    /// <see cref="NoteBlockClipImporter"/>를 그립니다. 가져온 클립은 별도의 가져온 오브젝트가 아니라 미리보기 영역에 표시됩니다.
    /// </summary>
    [CustomEditor(typeof(NoteBlockClipImporter))]
    public sealed class NoteBlockClipImporterEditor : ScriptedImporterEditor
    {
        /// <inheritdoc/>
        public override bool showImportedObject => false;

        /// <inheritdoc/>
        protected override bool needsApplyRevert => false;
    }
}
