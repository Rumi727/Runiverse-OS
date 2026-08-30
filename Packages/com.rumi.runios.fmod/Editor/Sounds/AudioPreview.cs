#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Sounds;
using RuniOS.Threading;
using System.Runtime.CompilerServices;

namespace RuniOS.Editor.Sounds
{
    public sealed partial class AudioPreview : IDisposable
    {
        static readonly object resourceMarker = new();

        static readonly ConditionalWeakTable<AudioPreview, object> previews = [];
        static readonly Dictionary<PhysicalPath, WaveAudioClip> loadedClips = [];

        readonly HashSet<PhysicalPath> requestedAudios = [];

        public static event Action<PhysicalPath>? onLoadedAudio;

        public bool isDisposed { get; private set; }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public WaveAudioClip? GetAudio(PhysicalPath path)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(AudioPreview));

            previews.AddOrUpdate(this, resourceMarker);
            if (loadedClips.TryGetValue(path, out WaveAudioClip? clip))
                return clip;

            if (requestedAudios.Add(path))
                LoadAudio(path).Forget();

            return null;
        }

        public void ReturnAudio(PhysicalPath path)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(AudioPreview));

            requestedAudios.Remove(path);
            GarbageCleanup();
        }

        public void Dispose()
        {
            foreach (var item in players)
                item.Value.Stop();

            _players.Clear();

            previews.Remove(this);
            requestedAudios.Clear();

            isDisposed = true;

            GarbageCleanup();

            GC.SuppressFinalize(this);
        }

        ~AudioPreview()
        {
            Debug.RuntimeLogWarning($"{nameof(AudioPreview)} was destroyed without calling the Dispose method.\nNative garbage is currently being cleaned up on the main thread, but this cannot be guaranteed to be completely resolved.\nPlease call the Dispose method before discarding {nameof(AudioPreview)}.");
            EditorThreadDispatcher.ExecuteForget(GarbageCleanup);
        }

        static async UniTaskVoid LoadAudio(PhysicalPath path)
        {
            WaveAudioClip? clip = null;
            try
            {
                clip = await SoundSystem.main.CreateSoundAsync(path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (clip == null)
                return;

            loadedClips.TryAdd(path, clip);
            onLoadedAudio?.Invoke(path);

            GarbageCleanup();
        }

        static void GarbageCleanup()
        {
            List<PhysicalPath> removeList = [];

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var loadedClip in loadedClips)
            {
                if (previews.Where(x => !x.Key.isDisposed).Any(token => token.Key.requestedAudios.Contains(loadedClip.Key)))
                    continue;

                loadedClip.Value.Dispose();
                removeList.Add(loadedClip.Key);
            }

            foreach (var item in removeList)
                loadedClips.Remove(item);
        }

        public static Texture2D WaveformTextureGenerate(WaveAudioClip clip, int width, int height, Color color)
        {
            /*
             * Sample : 채널 수에 영향 받지 않는 샘플 단위
             * Samples : 샘플 단위 길이
             * PCM : 채널 수에 영향 받는 PCM 단위
             * PCMs : PCM 단위 길이
             */

            int channelCount = clip.channel;

            float[] minimums = new float[width * channelCount];
            float[] maximums = new float[width * channelCount];

            Array.Fill(minimums, float.MaxValue);
            Array.Fill(maximums, float.MinValue);

            clip.Execute(clip =>
            {
                clip.GetData((pcmView, channelCount) =>
                {
                    int samples = pcmView.length / channelCount;
                    if (samples == 0)
                        return;

                    const int quality = 256;
                    int pointCount = Min(samples, width * quality);
                    float[] previousPCMs = new float[channelCount];
                    int previousX = 0;
                    bool hasPrevious = false;

                    for (int point = 0; point < pointCount; point++)
                    {
                        float normalized = pointCount != 1 ? (float)point / (pointCount - 1) : 0;
                        float position = 0f.LerpUnclamped(samples - 1f, normalized);

                        int previousSample = (int)position;
                        int nextSample = Min(previousSample + 1, samples - 1);

                        float t = position - previousSample;
                        int x = width != 1 && samples != 1 ? (int)0f.LerpUnclamped(width - 1, position / (samples - 1f)) : 0;

                        for (int channel = 0; channel < channelCount; channel++)
                        {
                            float a = (float)pcmView[(previousSample * channelCount) + channel];
                            float b = (float)pcmView[(nextSample * channelCount) + channel];
                            float pcm = a.LerpUnclamped(b, t);

                            if (!hasPrevious)
                            {
                                int xIndex = (x * channelCount) + channel;
                                minimums[xIndex] = Min(minimums[xIndex], pcm);
                                maximums[xIndex] = Max(maximums[xIndex], pcm);
                            }
                            else
                            {
                                for (int lineX = previousX; lineX <= x; lineX++)
                                {
                                    float lineT = previousX != x ? (float)(lineX - previousX) / (x - previousX) : 1;
                                    float linePcm = previousPCMs[channel].LerpUnclamped(pcm, lineT);
                                    int xIndex = (lineX * channelCount) + channel;

                                    minimums[xIndex] = Min(minimums[xIndex], linePcm);
                                    maximums[xIndex] = Max(maximums[xIndex], linePcm);
                                }
                            }

                            previousPCMs[channel] = pcm;
                        }

                        previousX = x;
                        hasPrevious = true;
                    }
                });
            });

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];

            for (int channel = 0; channel < channelCount; channel++)
            {
                int lane = channelCount - channel - 1;
                int laneHeight = (height / channelCount);
                int laneStart = lane * laneHeight;
                int laneEnd = (lane + 1) * laneHeight;
                int center = laneStart + (laneHeight / 2);

                for (int x = 0; x < width; x++)
                {
                    int xIndex = (x * channelCount) + channel;
                    if (minimums[xIndex].Approximately(float.MaxValue))
                        continue;

                    int yMin = center + (int)(minimums[xIndex] * (laneHeight / 2f));
                    int yMax = center + (int)(maximums[xIndex] * (laneHeight / 2f));
                    yMin = Max(laneStart, Min(laneEnd - 1, yMin));
                    yMax = Max(laneStart, Min(laneEnd - 1, yMax));

                    for (int y = yMin; y <= yMax; y++)
                        pixels[(y * width) + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
        }
    }
}
