#nullable enable
using RuniOS.Editor.NBS;
using RuniOS.NBS;
using RuniOS.UIElements;
using RuniOS.UIElements.NBS;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.NBS
{
    /// <summary>
    /// The preview controls of a <see cref="NoteBlockClip"/>: play, auto play, loop, the follow mode of <see cref="NBSNoteGridView"/>, and the volume.<br/>
    /// <see cref="NoteBlockClip"/> 미리보기 컨트롤입니다. 재생, 자동 재생, 반복, <see cref="NBSNoteGridView"/>의 따라가기 방식, 볼륨을 표시합니다.
    /// </summary>
    /// <remarks>
    /// Without a loop, the player is stopped once it passes the end, because the preview transport keeps running past it.<br/>
    /// 미리보기 트랜스포트는 끝을 지나도 계속 진행하므로, 반복하지 않으면 끝을 지난 플레이어를 멈춥니다.
    /// </remarks>
    public class NBSPreviewToolbar : VisualElement
    {
        public const string ussClassName = "runios-nbs-preview-toolbar";
        public const string buttonUssClassName = ussClassName + "__button";
        public const string iconUssClassName = ussClassName + "__icon";
        public const string volumeUssClassName = ussClassName + "__volume";

        readonly NBSPreview preview;
        readonly ToolbarToggle play;
        readonly ToolbarToggle autoPlay;
        readonly ToolbarToggle loop;
        readonly ToolbarMenu follow;
        readonly Image followIcon;
        readonly Slider volume;

        // The volume the tooltip shows, or -1 when the tooltip must be made again.
        int shownVolume = -1;

        /// <summary>
        /// Initializes the controls for clips played by <paramref name="preview"/>.<br/>
        /// <paramref name="preview"/>가 재생하는 클립의 컨트롤을 초기화합니다.
        /// </summary>
        /// <param name="preview">
        /// The preview that plays the clip.<br/>
        /// 클립을 재생하는 미리보기입니다.
        /// </param>
        public NBSPreviewToolbar(NBSPreview preview)
        {
            this.preview = preview;

            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            play = CreateToggle("play", "Play");
            play.RegisterValueChangedCallback(evt =>
            {
                if (clip == null)
                    return;

                if (evt.newValue)
                    preview.GetOrCreatePlayer(clip).Play();
                else
                    player?.Stop();
            });

            autoPlay = CreateToggle("auto-play", "preAudioAutoPlayOff");
            autoPlay.RegisterValueChangedCallback(evt => NBSPreview.autoPlay = evt.newValue);

            loop = CreateToggle("loop", "preAudioLoopOff");
            loop.RegisterValueChangedCallback(evt => player?.loop = evt.newValue);

            follow = new ToolbarMenu { name = "follow" };
            follow.AddToClassList(buttonUssClassName);
            followIcon = new Image { pickingMode = PickingMode.Ignore };
            followIcon.AddToClassList(iconUssClassName);
            follow.Insert(0, followIcon);
            Add(follow);

            volume = new Slider(0, 1) { name = "volume" };
            volume.AddToClassList(volumeUssClassName);
            volume.RegisterValueChangedCallback(evt => NBSPreview.globalVolume = evt.newValue);
            Add(volume);

            this.AddManipulator(new LocalizationManipulator(OnLanguageChanged));
            schedule.Execute(Update).Every(0);
        }

        /// <summary>
        /// Gets or sets the clip that the controls play, or <see langword="null"/> to disable them.<br/>
        /// 컨트롤이 재생하는 클립을 가져오거나 설정하며, 컨트롤을 비활성화하려면 <see langword="null"/>입니다.
        /// </summary>
        public NoteBlockClip? clip { get; set; }

        NBSPreview.Player? player => clip != null && !preview.isDisposed && preview.players.TryGetValue(clip, out NBSPreview.Player? value) ? value : null;

        ToolbarToggle CreateToggle(string name, string iconName)
        {
            ToolbarToggle toggle = new() { name = name };
            toggle.AddToClassList(buttonUssClassName);

            Image icon = new() { image = EditorGUIUtility.IconContent(iconName).image, pickingMode = PickingMode.Ignore };
            icon.AddToClassList(iconUssClassName);
            toggle.Add(icon);

            Add(toggle);
            return toggle;
        }

        void OnLanguageChanged()
        {
            play.tooltip = GetTextOrKey("runios-editor:gui.play");
            autoPlay.tooltip = GetTextOrKey("runios-editor:gui.auto_play");
            loop.tooltip = GetTextOrKey("runios-editor:gui.loop");
            follow.tooltip = GetTextOrKey("runios-editor:gui.follow");
            shownVolume = -1;

            follow.menu.ClearItems();
            AddFollowItem(NBSFollowMode.none, "runios-editor:gui.follow.none");
            AddFollowItem(NBSFollowMode.tickByTick, "runios-editor:gui.follow.tick");
            AddFollowItem(NBSFollowMode.pageByPage, "runios-editor:gui.follow.page");
            AddFollowItem(NBSFollowMode.frameByFrame, "runios-editor:gui.follow.frame");

            void AddFollowItem(NBSFollowMode mode, string key) =>
                follow.menu.AppendAction(GetTextOrKey(key), _ => NBSNoteGridView.followMode = mode, _ => NBSNoteGridView.followMode == mode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        void Update()
        {
            NBSPreview.Player? current = player;

            // Without a loop the transport keeps running past the end, so the preview stops there.
            if (current is { isPlaying: true, loop: false } && current.time >= current.length)
                current.Stop();

            bool isPlaying = current?.isPlaying == true;

            play.SetEnabled(clip != null && current?.isLoading != true);
            play.SetValueWithoutNotify(isPlaying);

            autoPlay.SetValueWithoutNotify(NBSPreview.autoPlay);

            // The loop follows the file until the instruments are loaded.
            loop.SetEnabled(current is { isLoaded: true });
            loop.SetValueWithoutNotify(current?.loop ?? (clip != null && clip.header.loop));

            followIcon.image = EditorGUIUtility.IconContent(NBSNoteGridView.followMode == NBSFollowMode.none ? "Unlinked" : "Linked").image;

            volume.SetValueWithoutNotify(NBSPreview.globalVolume);

            // Update runs every frame, so the tooltip string is made only when the volume it shows changes.
            int shown = (int)(NBSPreview.globalVolume * 100).Floor();
            if (shown != shownVolume)
            {
                shownVolume = shown;
                volume.tooltip = $"{GetTextOrKey("runios-editor:gui.volume")}: {shown}";
            }
        }
    }
}
