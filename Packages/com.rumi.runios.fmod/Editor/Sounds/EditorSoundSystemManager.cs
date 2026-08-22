#nullable enable
using FMODUnity;
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Linq;
using RuniOS.Sounds;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor.Sounds
{
    static partial class EditorSoundSystemManager
    {
        static StudioListener?[]? studioListeners;
        static readonly ConditionalWeakTable<SceneView, SceneViewExtra> sceneViewExtras = new();

        // ReSharper disable once ClassNeverInstantiated.Local
        class SceneViewExtra { public Vector3 lastPosition; }

        [OnCodeLoaded]
        static void OnCodeLoaded()
        {
            EditorApplication.update += Update;
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            EditorApplication.update -= Update;
            ObjectChangeEvents.changesPublished -= ChangesPublished;
        }

        static void ChangesPublished(ref ObjectChangeEventStream stream) => UpdateGameView();

        static void Update()
        {
            if (Kernel.isPlayingAndNotPaused || studioListeners == null)
                return;

            bool isGameView = PlayModeViewBridge.s_PlayModeViews.Any(x => EditorWindow.focusedWindow == x?.__instance);
            IEnumerable<AudioSpatialState> attributes;

            if (isGameView)
            {
                attributes = studioListeners
                    .WhereNotNull()
                    .Where(x => x.isActiveAndEnabled)
                    .Select(x => new AudioSpatialState(x.transform));
            }
            else
            {
                attributes = SceneView.sceneViews
                    .OfType<SceneView>()
                    .Where(x => EditorWindowBridge.__GetInstanceFrom(x).IsSelectedTab())
                    .Select(x =>
                    {
                        SceneViewExtra extra = sceneViewExtras.GetOrCreateValue(x);

                        Vector3 position = x.camera.transform.position;
                        Vector3 velocity = Vector3.zero;
                        float deltaTime = Time.unscaledDeltaTime;

                        if (float.IsNormal(deltaTime))
                        {
                            velocity = (position - extra.lastPosition) / deltaTime;
                            velocity = velocity.ClampMagnitude(20);
                        }

                        return new AudioSpatialState(x.camera.transform, velocity);
                    });
            }

            if (!attributes.Any())
                attributes = Enumerable.Repeat(new AudioSpatialState(), 1);

            SoundSystem.main.Execute((system, attributes) =>
            {
                system.listeners.count = attributes.Count();

                int i = 0;
                foreach (var state in attributes)
                {
                    system.listeners[i] = state;
                    i++;
                }
            }, attributes);

            foreach (var item in SceneView.sceneViews.OfType<SceneView>())
                sceneViewExtras.GetOrCreateValue(item).lastPosition = item.camera.transform.position;
        }

        [OnCodeInitializing]
        [MemberNotNull(nameof(studioListeners))]
        static void UpdateGameView()
        {
            if (Kernel.isPlaying)
                studioListeners = [];
            else
                studioListeners = Object.FindObjectsByType<StudioListener>();
        }
    }
}
