#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Sounds;
using RuniOS.Threading;
using System.Collections.Immutable;
using System.IO;

namespace RuniOS.Editor.Resource.Sounds
{
    public sealed class WaveAudioClipPackDrawer(PhysicalPath rootPath, ImmutableArray<RuniPath> relativePaths) : RuniAudioClipPackDrawer<WaveAudioClip>(rootPath, relativePaths)
    {
        public static GUIStyle shadowLabelStyle => _shadowLabelStyle ??= "PreOverlayLabel";
        static GUIStyle? _shadowLabelStyle;

        public bool isPlaying => _isPlaying;
        bool _isPlaying;

        public bool loop
        {
            get => _loop;
            set
            {
                if (_loop == value)
                    return;

                _loop = value;
                try
                {
                    if (channel != null)
                        channel.loop = value;
                }
                catch (ObjectDisposedException)
                {
                    Stop();
                }
            }
        }
        static bool _loop;

        public float volume
        {
            get => _volume;
            set
            {
                if (_volume.Approximately(value))
                    return;

                _volume = value;

                try
                {
                    if (channel != null)
                        channel.volume = value;
                }
                catch (ObjectDisposedException)
                {
                    Stop();
                }
            }
        }
        static float _volume = 0.5f;

        public double time
        {
            get
            {
                try
                {
                    return channel?.time ?? 0;
                }
                catch (ObjectDisposedException)
                {
                    Stop();
                    return 0;
                }
            }
            set
            {
                try
                {
                    if (channel != null)
                        channel.time = value;
                    else
                        Play(value);
                }
                catch (ObjectDisposedException)
                {
                    Stop();
                    Play(value);
                }
            }
        }

        readonly Dictionary<PhysicalPath, WaveAudioClip> loadedClips = [];
        readonly Dictionary<PhysicalPath, Texture2D> loadedTextures = [];
        readonly HashSet<PhysicalPath> loadingClips = [];
        readonly HashSet<PhysicalPath> failedClips = [];

        public override WaveAudioClip? targetClip => _targetClip;
        WaveAudioClip? _targetClip;

        SoundChannel? channel;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => IsMatch(x, "sounds", WildcardPatterns.musicFileFilter));

        protected override void OnEnable()
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

            WaveAudioClip? clip = null;
            try
            {
                clip = await SoundSystem.main.CreateSoundAsync(soundPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

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

            if (targetClip == null || relativePaths.Length > 1)
                return;

            BeginAlignment(TextAnchor.UpperLeft, shadowLabelStyle);
            Rect channelTextRect = r;
            channelTextRect.xMin += 3;
            channelTextRect.height = r.height / targetClip.channel;
            for (int i = 0; i < targetClip.channel; i++)
            {
                channelTextRect.y = r.y + (channelTextRect.height * i);
                EditorGUI.DropShadowLabel(channelTextRect, $"{GetTextOrKey("runios-editor:gui.channel")} {i}");
            }
            EndAlignment(shadowLabelStyle);

            if (!isPlaying)
                return;

            BeginRichText(shadowLabelStyle);
            BeginAlignment(TextAnchor.UpperCenter, shadowLabelStyle);
            EditorGUI.DropShadowLabel(r, RichNumberMSpace(TimeUtility.ToTimeString(time)));
            EndAlignment(shadowLabelStyle);
            EndRichText(shadowLabelStyle);

            Rect cursorRect = r;
            cursorRect.x = r.x.Lerp(r.width, (time / targetClip.length).ClampToFloat());
            cursorRect.width = 2;

            EditorGUI.DrawRect(cursorRect, Color.white);
            Repaint();
        }

        protected override void OnInteractivePreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background)
        {
            OnPreviewGUI(r, rootPath, relativePath, background);

            if (targetClip == null || relativePaths.Length > 1)
                return;

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                time = r.x.InverseLerp(r.width, Event.current.mousePosition.x) * targetClip.length;
        }

        protected override void OnPreviewSettings()
        {
            if (targetClip == null || relativePaths.Length > 1)
                return;

            EditorGUI.BeginChangeCheck();
            bool isPlaying = GUILayout.Toggle(this.isPlaying, PlaybackController.playButtonText, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                    Play();
                else
                    Stop();
            }

            loop = GUILayout.Toggle(loop, PlaybackController.loopButtonText, EditorStyles.toolbarButton);

            Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(75));
            volume = GUI.HorizontalSlider(sliderRect, volume, 0, 1);
            GUI.Box(sliderRect, TempContent("", $"{GetTextOrKey("runios-editor:gui.volume")}: {(volume * 100).Floor()}"), GUIStyle.none);
        }

        public void Play(double startTime = 0)
        {
            if (targetClip == null)
                return;

            if (!isPlaying)
            {
                try
                {
                    SoundSystem.main.Execute(system => system.PlaySound(targetClip, true), out channel);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (channel == null)
                    return;

                channel.volume = volume;
                channel.loop = loop;

                channel.onStop += OnStop;
            }

            _isPlaying = true;

            if (channel != null)
            {
                try
                {
                    channel.time = startTime;
                    channel.isPaused = false;
                }
                catch (ObjectDisposedException)
                {
                    Stop();
                    Play(startTime);

                    return;
                }
            }
        }

        void OnStop(SoundChannel channel)
        {
            channel.onStop -= OnStop;

            ThreadDispatcher.ExecuteForget(() =>
            {
                // onStop 이벤트는 FMOD 스레드에서 호출되기 때문에 메인 스레드로 이동하기 전에 새 채널이 만들어질 수 있습니다.
                if (channel != this.channel)
                    return;

                Stop();
            });
        }

        public void Stop()
        {
            _isPlaying = false;

            if (channel != null)
            {
                channel.onStop -= OnStop;
                channel.Stop();
            }

            channel = null;
        }
    }
}