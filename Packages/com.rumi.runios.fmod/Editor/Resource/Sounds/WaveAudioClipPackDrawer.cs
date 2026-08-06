#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Editor.Sounds;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Sounds;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;

namespace RuniOS.Editor.Resource.Sounds
{
    public sealed class WaveAudioClipPackDrawer : RuniAudioClipPackDrawer<WaveAudioClip>
    {
        public static GUIStyle shadowLabelStyle => _shadowLabelStyle ??= "PreOverlayLabel";
        static GUIStyle? _shadowLabelStyle;

        public WaveAudioClipPackDrawer(PhysicalPath rootPath, ImmutableArray<RuniPath> relativePaths) : base(rootPath, relativePaths)
        {
            if (relativePaths.IsEmpty() || relativePaths.TwoOrMore())
                return;

            PhysicalPath soundPath = rootPath / relativePaths.First();
            if (!File.Exists(soundPath))
                return;

            targetSoundPath = soundPath;
        }

        public PhysicalPath? targetSoundPath { get; }
        public override WaveAudioClip? targetClip => targetSoundPath != null ? preview.GetAudio(targetSoundPath.Value) : null;

        readonly AudioPreview preview = new AudioPreview();
        readonly Dictionary<PhysicalPath, Texture2D> loadedTextures = [];

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => IsMatch(x, "sounds", WildcardPatterns.musicFileFilter));

        protected override void OnEnable() => AudioPreview.onLoadedAudio += Repaint;

        protected override void OnDisable()
        {
            foreach (Texture2D texture in loadedTextures.Values)
                Object.DestroyImmediate(texture);

            preview.Dispose();
            AudioPreview.onLoadedAudio -= Repaint;
        }

        protected override void DrawInfoLayout()
        {
            base.DrawInfoLayout();
            DrawInfoFieldLayout("runios-editor:gui.samples", targetClip?.samples.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.frequency", targetClip?.frequency.ToString(CultureInfo.InvariantCulture) ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.channel", targetClip?.channel.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.bits", targetClip?.bits.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.bits", targetClip?.pcmFormat.ToString() ?? "―");
        }

        protected override bool HasPreviewGUI() => true;

        protected override void OnPreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background)
        {
            PhysicalPath soundPath = rootPath / relativePath;
            WaveAudioClip? clip = preview.GetAudio(soundPath);
            if (clip == null)
                return;

            Texture2D? texture = loadedTextures.GetValueOrDefault(soundPath);
            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, false, false);

                int width = Max(1, (int)r.width);
                int height = Max(1, (int)r.height);

                if (texture == null || texture.width != width || texture.height != height)
                {
                    if (texture != null)
                        Object.DestroyImmediate(texture);

                    texture = AudioPreview.WaveformTextureGenerate(clip, width, height, Color.darkOrange);
                    loadedTextures[soundPath] = texture;
                }
            }

            GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);

            AudioPreview.Player player = preview.GetOrCreatePlayer(soundPath);
            if (player.isPlaying)
            {
                Rect cursorRect = r;
                cursorRect.x = (r.x - 1).Lerp(r.width - 1, (player.time / player.length).ClampToFloat());
                cursorRect.width = 2;

                EditorGUI.DrawRect(cursorRect, Color.white);

                BeginRichText(shadowLabelStyle);
                BeginAlignment(TextAnchor.UpperCenter, shadowLabelStyle);
                EditorGUI.DropShadowLabel(r, RichNumberMSpace(TimeUtility.ToTimeString(player.time)));
                EndAlignment(shadowLabelStyle);
                EndRichText(shadowLabelStyle);

                Repaint();
            }

            BeginAlignment(TextAnchor.UpperLeft, shadowLabelStyle);
            Rect channelTextRect = r;
            channelTextRect.xMin += 3;
            channelTextRect.height = r.height / clip.channel;
            for (int i = 0; i < clip.channel; i++)
            {
                channelTextRect.y = r.y + (channelTextRect.height * i);
                EditorGUI.DropShadowLabel(channelTextRect, $"{GetTextOrKey("runios-editor:gui.channel")} {i}");
            }
            EndAlignment(shadowLabelStyle);
        }

        protected override void OnInteractivePreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background)
        {
            OnPreviewGUI(r, rootPath, relativePath, background);

            if (r.Contains(Event.current.mousePosition) && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                PhysicalPath soundPath = rootPath / relativePath;
                AudioPreview.Player targetPlayer = preview.GetOrCreatePlayer(soundPath);

                foreach (var player in preview.players)
                {
                    if (targetPlayer == player.Value)
                        player.Value.time = (r.x - 1).InverseLerp(r.width - 1, Event.current.mousePosition.x) * player.Value.length;
                    else
                        player.Value.Stop();
                }
            }
        }

        protected override void OnPreviewSettings()
        {
            bool isPlaying = preview.players.Any(x => x.Value.isPlaying);
            EditorGUI.BeginDisabledGroup(!isPlaying && targetSoundPath == null);

            EditorGUI.BeginChangeCheck();
            isPlaying = GUILayout.Toggle(isPlaying, PlaybackController.playButtonText, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                    preview.GetOrCreatePlayer(targetSoundPath!.Value).Play();
                else
                {
                    foreach (var player in preview.players)
                        player.Value.Stop();
                }
            }

            EditorGUI.EndDisabledGroup();

            AudioPreview.globalLoop = GUILayout.Toggle(AudioPreview.globalLoop, PlaybackController.loopButtonText, EditorStyles.toolbarButton);

            Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(75));
            AudioPreview.globalVolume = GUI.HorizontalSlider(sliderRect, AudioPreview.globalVolume, 0, 1);
            GUI.Box(sliderRect, TempContent("", $"{GetTextOrKey("runios-editor:gui.volume")}: {(AudioPreview.globalVolume * 100).Floor()}"), GUIStyle.none);
        }
    }
}