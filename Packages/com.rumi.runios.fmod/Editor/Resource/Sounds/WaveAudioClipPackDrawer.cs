#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Sounds;
using System.IO;

namespace RuniOS.Editor.Resource.Sounds
{
    public class WaveAudioClipPackDrawer : RuniAudioClipPackDrawer<WaveAudioClip>
    {
        readonly Dictionary<PhysicalPath, WaveAudioClip> loadedClips = [];
        readonly Dictionary<PhysicalPath, Texture2D> loadedTextures = [];
        readonly HashSet<PhysicalPath> loadingClips = [];
        readonly HashSet<PhysicalPath> failedClips = [];

        public override WaveAudioClip? targetClip => _targetClip;
        WaveAudioClip? _targetClip;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => IsMatch(x, "sounds", WildcardPatterns.musicFileFilter));

        protected override void OnEnable(PhysicalPath rootPath, IReadOnlyList<RuniPath> relativePaths)
        {
            if (relativePaths.IsEmpty() || relativePaths.TwoOrMore())
                return;

            PhysicalPath soundPath = rootPath / relativePaths.First();
            if (!File.Exists(soundPath))
                return;

            LoadClip(soundPath, true).Forget();
        }

        async UniTask LoadClip(PhysicalPath soundPath, bool assignTarget)
        {
            if (failedClips.Contains(soundPath))
                return;

            if (loadedClips.TryGetValue(soundPath, out WaveAudioClip? loadedClip))
            {
                if (assignTarget)
                    _targetClip = loadedClip;

                return;
            }

            if (!loadingClips.Add(soundPath))
                return;

            WaveAudioClip? clip = await SoundSystem.main.CreateSoundAsync(soundPath);
            loadingClips.Remove(soundPath);

            if (!isEnabled)
            {
                clip?.Dispose();
                return;
            }

            if (clip == null)
            {
                failedClips.Add(soundPath);
                Repaint();
                return;
            }

            if (!loadedClips.TryAdd(soundPath, clip))
            {
                clip.Dispose();
                return;
            }

            if (assignTarget)
                _targetClip = clip;

            Repaint();
        }

        protected override void DrawInfoLayout()
        {
            base.DrawInfoLayout();
            DrawInfoFieldLayout("runios-editor:gui.samples", targetClip != null ? targetClip.samples : "―");
            DrawInfoFieldLayout("runios-editor:gui.frequency", targetClip != null ? targetClip.frequency : "―");
            DrawInfoFieldLayout("runios-editor:gui.channel", targetClip != null ? targetClip.channel : "―");
            DrawInfoFieldLayout("runios-editor:gui.bits", targetClip != null ? targetClip.bits : "―");
            DrawInfoFieldLayout("runios-editor:gui.bits", targetClip?.pcmFormat.ToString() ?? "―");
        }

        protected override void OnDisable()
        {
            _targetClip = null;

            foreach (WaveAudioClip clip in loadedClips.Values)
                clip.Dispose();

            loadedClips.Clear();
            loadingClips.Clear();
            failedClips.Clear();

            foreach (Texture2D texture in loadedTextures.Values)
                Object.DestroyImmediate(texture);

            loadedTextures.Clear();
        }

        protected override bool HasPreviewGUI() => true;

        protected override void OnPreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background)
        {
            PhysicalPath soundPath = rootPath / relativePath;
            Texture2D? texture = loadedTextures.GetValueOrDefault(soundPath);

            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, false, false);

                if (!loadedClips.TryGetValue(soundPath, out WaveAudioClip? clip))
                {
                    LoadClip(soundPath, false).Forget();
                    return;
                }

                int width = Max(1, (int)r.width);
                int height = Max(1, (int)r.height);

                if (texture == null || texture.width != width || texture.height != height)
                {
                    if (texture != null)
                        Object.DestroyImmediate(texture);

                    texture = WaveformTextureGenerator.Create(clip, width, height, new Color(1f, 0.5f, 0f, 1f));
                    loadedTextures[soundPath] = texture;
                }
            }

            GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);
        }
    }
}
