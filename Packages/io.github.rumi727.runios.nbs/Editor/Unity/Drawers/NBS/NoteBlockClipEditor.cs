#nullable enable
using RuniOS.Editor.IMGUI.NBS;
using RuniOS.Editor.NBS;
using RuniOS.Editor.UIElements.NBS;
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.NBS;
using RuniOS.Resource;
using RuniOS.UIElements.NBS;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Shows a <see cref="NoteBlockClip"/>: its information as label fields, and a Note Block Studio-like note grid with playback in the preview area.<br/>
    /// <see cref="NoteBlockClip"/>을 표시합니다. 정보는 라벨 필드로, 미리보기 영역에는 Note Block Studio 같은 노트 그리드와 재생을 표시합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="NoteBlockClipImporterEditor"/> draws the information itself because it does not show the imported object, and its preview comes from this editor.<br/>
    /// <see cref="NoteBlockClipImporterEditor"/>는 가져온 오브젝트를 표시하지 않으므로 정보를 직접 그리며, 미리보기는 이 에디터의 것을 씁니다.
    /// </remarks>
    [CustomEditor(typeof(NoteBlockClip))]
    public sealed class NoteBlockClipEditor : CustomInspectorBase<NoteBlockClip>
    {
        NBSPreview preview = null!;

        protected override void OnEnable()
        {
            base.OnEnable();

            preview = new NBSPreview(Identifier.defaultNamespace);

            if (NBSPreview.autoPlay && target != null)
                preview.GetOrCreatePlayer(target).Play();
        }

        protected override void OnDisable() => preview.Dispose();

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            if (target != null)
                NBSClipInfoGUI.DrawLayout(target);
        }

        /// <inheritdoc/>
        public override bool HasPreviewGUI() => true;

        /// <inheritdoc/>
        /// <remarks>
        /// The controls go to the toolbar of the preview header, because Unity does not call <see cref="UnityEditor.Editor.OnPreviewSettings"/> for a UI Toolkit preview.<br/>
        /// Unity는 UI Toolkit 미리보기에 <see cref="UnityEditor.Editor.OnPreviewSettings"/>를 호출하지 않으므로 컨트롤을 미리보기 헤더의 툴바에 넣습니다.
        /// </remarks>
        public override VisualElement CreatePreview(VisualElement inspectorPreviewWindow)
        {
            NBSNoteGridView grid = new() { clip = target, player = target != null ? preview.GetOrCreatePlayer(target) : null };
            inspectorPreviewWindow.Add(grid);

            inspectorPreviewWindow.Q("toolbar")?.Add(new NBSPreviewToolbar(preview) { clip = target });
            return grid;
        }
    }
}
