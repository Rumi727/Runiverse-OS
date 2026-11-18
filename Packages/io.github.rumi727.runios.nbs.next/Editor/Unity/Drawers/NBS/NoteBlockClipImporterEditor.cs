#nullable enable
using RuniOS.Editor.IMGUI.NBS;
using RuniOS.Editor.NBS;
using RuniOS.NBS;
using UnityEditor.AssetImporters;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Draws <see cref="NoteBlockClipImporter"/>. The information of the imported clip is shown as label fields, and the clip itself is shown in the preview area rather than as a separate imported object.<br/>
    /// <see cref="NoteBlockClipImporter"/>를 그립니다. 가져온 클립의 정보는 라벨 필드로 표시하고, 클립 자체는 별도의 가져온 오브젝트가 아니라 미리보기 영역에 표시합니다.
    /// </summary>
    [CustomEditor(typeof(NoteBlockClipImporter))]
    public sealed class NoteBlockClipImporterEditor : ScriptedImporterEditor
    {
        /// <inheritdoc/>
        public override bool showImportedObject => false;

        /// <inheritdoc/>
        protected override bool needsApplyRevert => false;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            // ReSharper disable once Unity.NoNullPatternMatching
            if (assetTarget != null && assetTarget is NoteBlockClip clip)
                NBSClipInfoGUI.DrawLayout(clip);
        }
    }
}
