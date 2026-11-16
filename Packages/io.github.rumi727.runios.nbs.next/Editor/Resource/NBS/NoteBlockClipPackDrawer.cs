#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI.NBS;
using RuniOS.Editor.NBS;
using RuniOS.Editor.Resource.Sounds;
using RuniOS.Editor.UIElements.NBS;
using RuniOS.IO;
using RuniOS.NBS;
using RuniOS.Resource;
using RuniOS.UIElements.NBS;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Resource.NBS
{
    /// <summary>
    /// Shows <c>.nbs</c> files in the <c>nbses</c> folder of a resource pack: the song information as label fields, and for a single file a Note Block Studio-like note grid with playback.<br/>
    /// Custom instrument sounds are looked up in the namespace that contains the file.
    /// <br/><br/>
    /// 리소스 팩의 <c>nbses</c> 폴더에 있는 <c>.nbs</c> 파일을 표시합니다. 곡 정보는 라벨 필드로, 파일이 하나면 Note Block Studio 같은 노트 그리드와 재생을 표시합니다.<br/>
    /// 커스텀 악기 사운드는 파일이 들어 있는 네임스페이스에서 찾습니다.
    /// </summary>
    /// <param name="targets">
    /// The files to show.<br/>
    /// 표시할 파일입니다.
    /// </param>
    public sealed class NoteBlockClipPackDrawer(ImmutableArray<IONode> targets) : RuniAudioClipPackDrawer<NoteBlockClip>(targets)
    {
        readonly Dictionary<IONode, NoteBlockClip> clips = [];

        // Created with the drawer, because the inspector can ask for the toolbar before OnEnable runs. Prototypes without a target have none.
        readonly NBSPreview? preview = targets.Length == 1 ? new NBSPreview(GetNamespace(targets[0])) : null;

        NBSNoteGridView? grid;
        NBSPreviewToolbar? toolbar;

        // Loading continues after the drawer is disabled, and the clips loaded then are destroyed instead of kept.
        bool isDisabled;

        /// <inheritdoc/>
        public override NoteBlockClip? targetClip => target is { } node ? clips.GetValueOrDefault(node) : null;

        /// <inheritdoc/>
        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.Any() && relativePaths.All(static x => IsMatch(x, "nbses", IPatternMatcher.nbsMatcher));

        /// <inheritdoc/>
        protected override async UniTask OnEnable()
        {
            foreach (IONode node in targets)
            {
                NoteBlockClip clip;
                try
                {
                    clip = await NoteBlockClip.LoadAsync(node);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    continue;
                }

                clip.hideFlags = HideFlags.HideAndDontSave;
                if (isDisabled)
                {
                    Object.DestroyImmediate(clip);
                    return;
                }

                clips[node] = clip;
            }

            if (targetClip is { } loaded)
            {
                ShowClip(loaded);

                if (NBSPreview.autoPlay)
                    preview!.GetOrCreatePlayer(loaded).Play();
            }

            Repaint();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            isDisabled = true;
            preview?.Dispose();

            foreach (NoteBlockClip clip in clips.Values)
                Object.DestroyImmediate(clip);

            clips.Clear();
        }

        /// <inheritdoc/>
        protected override void DrawInfoLayout()
        {
            if (targetClip is { } clip)
                NBSClipInfoGUI.DrawLayout(clip);
            else
                base.DrawInfoLayout();
        }

        /// <inheritdoc/>
        protected override bool HasPreviewGUI() => true;

        /// <inheritdoc/>
        protected override VisualElement? CreatePreview(IONode node)
        {
            grid = new NBSNoteGridView();
            if (targetClip is { } clip)
                ShowClip(clip);

            return grid;
        }

        /// <inheritdoc/>
        protected override VisualElement? CreatePreviewSettings(IONode node)
        {
            if (preview == null)
                return null;

            toolbar = new NBSPreviewToolbar(preview) { clip = targetClip };
            return toolbar;
        }

        /// <inheritdoc/>
        protected override void OnPreviewGUI(Rect r, IONode node, GUIStyle background)
        {
            // Several targets are drawn as a grid of cells, each showing the song name.
            string name = clips.TryGetValue(node, out NoteBlockClip? clip) && clip.header.name.Length > 0 ? clip.header.name : node.name;
            EditorGUI.DropShadowLabel(r, name);
        }

        void ShowClip(NoteBlockClip clip)
        {
            if (grid != null)
            {
                grid.clip = clip;
                grid.player = preview?.GetOrCreatePlayer(clip);
            }

            toolbar?.clip = clip;
        }

        // A resource path is assets/<namespace>/..., and the custom instruments of a song are in its namespace.
        static string GetNamespace(IONode node)
        {
            string[] parts = node.path.value.Split('/');
            return parts.Length > 2 && parts[0] == ResourcePack.assetsFolderName ? parts[1] : Identifier.defaultNamespace;
        }
    }
}
