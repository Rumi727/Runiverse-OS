#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Sounds;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace RuniOS.Editor.Sounds
{
    public sealed partial class AudioPreview : IDisposable
    {
        static readonly object resourceMarker = new();

        static readonly ConditionalWeakTable<AudioPreview, object> previews = [];
        static readonly Dictionary<IONode, WaveAudioClip> loadedClips = [];
        static readonly Dictionary<IONode, NativeArray<float>> loadedPlanarSamples = [];

        readonly HashSet<IONode> requestedAudios = [];

        public static event Action<IONode>? onLoadedAudio;

        public bool isDisposed { get; private set; }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public WaveAudioClip? GetAudio(IONode node)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            previews.AddOrUpdate(this, resourceMarker);
            if (loadedClips.TryGetValue(node, out WaveAudioClip? clip))
                return clip;

            if (requestedAudios.Add(node))
                LoadAudio(node).Forget();

            return null;
        }

        /// <summary>
        /// Gets the planar samples of the audio loaded by <see cref="GetAudio"/>.<br/>
        /// <see cref="GetAudio"/>로 불러온 오디오의 planar 샘플을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The samples are owned by <see cref="AudioPreview"/> and are disposed together with the audio.<br/>
        /// 샘플은 <see cref="AudioPreview"/>가 소유하며 오디오와 함께 해제됩니다.
        /// </remarks>
        public bool TryGetPlanarSamples(IONode node, out NativeArray<float> samples)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            return loadedPlanarSamples.TryGetValue(node, out samples);
        }

        public void ReturnAudio(IONode node)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            requestedAudios.Remove(node);
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

        static async UniTaskVoid LoadAudio(IONode node)
        {
            WaveAudioClip? clip = null;
            try
            {
                clip = await SoundSystem.main.CreateSoundAsync(node);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (clip == null)
                return;

            if (loadedClips.TryAdd(node, clip))
            {
                try
                {
                    loadedPlanarSamples.Add(node, clip.CreatePlanarSamples(Allocator.Persistent));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            onLoadedAudio?.Invoke(node);

            GarbageCleanup();
        }

        static void GarbageCleanup()
        {
            List<IONode> removeList = [];

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var loadedClip in loadedClips)
            {
                if (previews.Where(x => !x.Key.isDisposed).Any(token => token.Key.requestedAudios.Contains(loadedClip.Key)))
                    continue;

                loadedClip.Value.Dispose();
                if (loadedPlanarSamples.Remove(loadedClip.Key, out NativeArray<float> samples))
                    samples.Dispose();

                removeList.Add(loadedClip.Key);
            }

            foreach (var item in removeList)
                loadedClips.Remove(item);
        }
    }
}
