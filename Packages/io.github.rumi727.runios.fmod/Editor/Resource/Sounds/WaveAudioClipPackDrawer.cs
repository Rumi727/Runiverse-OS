#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.Sounds;
using RuniOS.IO;
using RuniOS.Sounds;
using System.Collections.Immutable;
using System.Globalization;

namespace RuniOS.Editor.Resource.Sounds
{
    public sealed class WaveAudioClipPackDrawer(ImmutableArray<IONode> targets) : RuniAudioClipPackDrawer<WaveAudioClip>(targets)
    {
        public static GUIStyle shadowLabelStyle => _shadowLabelStyle ??= "PreOverlayLabel";
        static GUIStyle? _shadowLabelStyle;

        // 대상이 없거나 불러오지 못하면 AudioPreview가 null을 돌려줍니다.
        public override WaveAudioClip? targetClip => target is { } node ? preview.GetAudio(node) : null;

        readonly AudioPreview preview = new AudioPreview();
        readonly Dictionary<IONode, RenderTexture> loadedTextures = [];

        // 전체 길이를 1로 본 보이는 구간의 시작과 길이입니다. 없으면 전체를 봅니다.
        readonly Dictionary<IONode, (double start, double size)> views = [];
        WaveformRenderer? waveformRenderer;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.Any() && relativePaths.All(x => IsMatch(x, "sounds", IPatternMatcher.musicMatcher));

        protected override UniTask OnEnable()
        {
            AudioPreview.onLoadedAudio += OnLoadedAudio;
            return UniTask.CompletedTask;
        }

        protected override void OnDisable()
        {
            foreach (RenderTexture texture in loadedTextures.Values)
                Object.DestroyImmediate(texture);

            waveformRenderer?.Dispose();

            preview.Dispose();
            AudioPreview.onLoadedAudio -= OnLoadedAudio;
        }

        void OnLoadedAudio(IONode node) => Repaint();

        protected override void DrawInfoLayout()
        {
            base.DrawInfoLayout();
            DrawInfoFieldLayout("runios-editor:gui.samples", targetClip?.samples.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.frequency", targetClip?.frequency.ToString(CultureInfo.InvariantCulture) ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.channel", targetClip?.channel.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.bits", targetClip?.bits.ToString() ?? "―");
            DrawInfoFieldLayout("runios-editor:gui.format", targetClip?.pcmFormat.ToString() ?? "―");
        }

        protected override bool HasPreviewGUI() => true;

        protected override void OnPreviewGUI(Rect r, IONode node, GUIStyle background)
        {
            WaveAudioClip? clip = preview.GetAudio(node);
            if (clip == null)
                return;

            RenderTexture? texture = loadedTextures.GetValueOrDefault(node);
            (double start, double size) view = views.GetValueOrDefault(node, (0d, 1d));
            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, GUIContent.none, false, false, false, false);

                int width = Max(1, (int)r.width);
                int height = Max(1, (int)r.height);

                if ((texture == null || texture.width != width || texture.height != height || !texture.IsCreated()) && preview.TryGetWaveform(node, out Waveform? waveform))
                {
                    if (texture == null || texture.width != width || texture.height != height)
                    {
                        if (texture != null)
                            Object.DestroyImmediate(texture);

                        texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { hideFlags = HideFlags.HideAndDontSave };
                        loadedTextures[node] = texture;
                    }

                    // 어셈블리 로드 중 호출되는 OnEnable에서는 네이티브 오브젝트를 만들 수 없으므로 처음 그릴 때 만듭니다.
                    waveformRenderer ??= new WaveformRenderer();
                    double lastSample = waveform.sampleCount - 1;
                    waveformRenderer.Render(waveform, view.start * lastSample, Min((view.start + view.size) * lastSample, lastSample), texture, Color.darkOrange);
                }
            }

            if (texture != null)
                GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);

            AudioPreview.Player player = preview.GetOrCreatePlayer(node);
            if (player.isPlaying)
            {
                // 커서는 보이는 구간 안에 있을 때만 그립니다.
                double position = ((player.time / player.length) - view.start) / view.size;
                if (position is >= 0 and <= 1)
                {
                    Rect cursorRect = r;
                    cursorRect.x = (r.x - 1).Lerp(r.width - 1, position.ClampToFloat());
                    cursorRect.width = 2;

                    EditorGUI.DrawRect(cursorRect, Color.white);
                }

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
                EditorGUI.DropShadowLabel(channelTextRect, $"{GetTextOrKey("runios-editor:gui.channel")} {i + 1}");
            }
            EndAlignment(shadowLabelStyle);
        }

        protected override void OnInteractivePreviewGUI(Rect r, IONode node, GUIStyle background)
        {
            // 스크롤바는 확대했을 때만 그려져 이후 컨트롤 ID를 바꾸므로, 이동에 쓰는 ID를 가장 먼저 받습니다.
            int panControlID = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            (double start, double size) view = views.GetValueOrDefault(node, (0d, 1d));
            bool isZoomed = view.size < 1;

            Rect waveformRect = r;
            Rect scrollbarRect = r;
            if (isZoomed)
            {
                scrollbarRect.yMin = r.yMax - GUI.skin.horizontalScrollbar.fixedHeight;
                waveformRect.yMax = scrollbarRect.yMin;
            }

            OnPreviewGUI(waveformRect, node, background);

            if (isZoomed)
            {
                EditorGUI.BeginChangeCheck();
                float scroll = GUI.HorizontalScrollbar(scrollbarRect, (float)view.start, (float)view.size, 0, 1);
                if (EditorGUI.EndChangeCheck())
                    SetView(node, scroll, view.size);
            }

            switch (evt.GetTypeForControl(panControlID))
            {
                // 마우스 아래의 위치를 고정한 채 확대하고 축소합니다. 보이는 구간은 최소 한 샘플입니다.
                case EventType.ScrollWheel when preview.GetAudio(node) is { samples: > 1 } clip && waveformRect.Contains(evt.mousePosition):
                {
                    double mouse = waveformRect.x.InverseLerp(waveformRect.xMax, evt.mousePosition.x);
                    double anchor = view.start + (mouse * view.size);
                    double size = (view.size * 1.1.Pow((double)evt.delta.y)).Clamp(1d / (clip.samples - 1), 1);

                    SetView(node, anchor - (mouse * size), size);
                    evt.Use();
                    break;
                }
                case EventType.MouseDown when evt.button == 2 && isZoomed && waveformRect.Contains(evt.mousePosition):
                    GUIUtility.hotControl = panControlID;
                    evt.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == panControlID:
                    SetView(node, view.start - (evt.delta.x / waveformRect.width * view.size), view.size);
                    evt.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == panControlID:
                    GUIUtility.hotControl = 0;
                    evt.Use();
                    break;
            }

            // 가운데 버튼은 이동에 쓰므로 탐색하지 않습니다.
            if (evt.button != 2 && waveformRect.Contains(evt.mousePosition) && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag))
            {
                AudioPreview.Player targetPlayer = preview.GetOrCreatePlayer(node);

                foreach (var player in preview.players)
                {
                    if (targetPlayer == player.Value)
                        player.Value.time = (view.start + ((waveformRect.x - 1).InverseLerp(waveformRect.width - 1, evt.mousePosition.x) * view.size)) * player.Value.length;
                    else
                        player.Value.Stop();
                }
            }
        }

        // 텍스처를 해제해 다음 Repaint에서 새 구간으로 다시 그리게 합니다.
        void SetView(IONode node, double start, double size)
        {
            views[node] = (start.Clamp(0, 1 - size), size);

            if (loadedTextures.TryGetValue(node, out RenderTexture? texture))
                texture.Release();

            Repaint();
        }

        protected override void OnPreviewSettings()
        {
            bool isPlaying = preview.players.Any(x => x.Value.isPlaying);
            EditorGUI.BeginDisabledGroup(!isPlaying && target == null);

            EditorGUI.BeginChangeCheck();
            isPlaying = GUILayout.Toggle(isPlaying, EditorGUIUtility.IconContent("Play"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                    preview.GetOrCreatePlayer(target!.Value).Play();
                else
                {
                    foreach (var player in preview.players)
                        player.Value.Stop();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            bool autoPlay = GUILayout.Toggle(AudioPreview.autoPlay, EditorGUIUtility.IconContent("preAudioAutoPlayOff"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
                AudioPreview.autoPlay = autoPlay;

            EditorGUI.BeginChangeCheck();
            bool globalLoop = GUILayout.Toggle(AudioPreview.globalLoop, EditorGUIUtility.IconContent("preAudioLoopOff"), EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
                AudioPreview.globalLoop = globalLoop;

            Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(75));
            AudioPreview.globalVolume = GUI.HorizontalSlider(sliderRect, AudioPreview.globalVolume, 0, 1);
            GUI.Box(sliderRect, TempContent("", $"{GetTextOrKey("runios-editor:gui.volume")}: {(AudioPreview.globalVolume * 100).Floor()}"), GUIStyle.none);
        }
    }
}