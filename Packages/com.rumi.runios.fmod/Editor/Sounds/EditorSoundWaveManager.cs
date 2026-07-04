#nullable enable
using FMOD;
using FMODUnity;
using RuniOS.Sounds;
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RuniOS.Editor.Sounds
{
    [InitializeOnLoad]
    public static class EditorSoundWaveManager
    {
        static EditorSoundWaveManager()
        {
            Factory.System_Create(out FMOD.System newSystem).ThrowIfNotOk();

            newSystem.init(4095, INITFLAGS.NORMAL, IntPtr.Zero).ThrowIfNotOk();
            currentSystem = newSystem;

            EditorApplication.update += UpdateEditorSystem;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseEditorSystem;

            currentSystem = newSystem;
            SoundWaveManager.currentSystem = currentSystem;

            UpdateGameView();
            ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream _) => UpdateGameView();
        }
        
        public static FMOD.System currentSystem { get; }

        static StudioListener?[] studioListeners;
        static readonly ConditionalWeakTable<SceneView, SceneViewExtra> sceneViewExtras = new();

        // ReSharper disable once ClassNeverInstantiated.Local
        class SceneViewExtra { public Vector3 lastPosition; }

        static void UpdateEditorSystem()
        {
            if (Kernel.isPlaying)
                SoundWaveManager.currentSystem = RuntimeManager.CoreSystem;
            else
                SoundWaveManager.currentSystem = currentSystem;

            currentSystem.update().LogErrorIfNotOk();

            bool isGameView = PlayModeViewBridge.s_PlayModeViews.Any(x => EditorWindow.focusedWindow == x?.__instance);
            IEnumerable<ATTRIBUTES_3D> attributes;

            if (isGameView)
                attributes = studioListeners.WhereNotNull().Select(x => x.transform.To3DAttributes());
            else
            {
                attributes = SceneView.sceneViews
                    .OfType<SceneView>()
                    .Where(x => EditorWindowBridge.__GetInstanceFrom(x).IsSelectedTab())
                    .Select(x =>
                    {
                        SceneViewExtra extra = sceneViewExtras.GetOrCreateValue(x);
                        
                        Vector3 position = x.camera.transform.position;
                        Vector3 velocity = (position - extra.lastPosition) / Time.unscaledDeltaTime;
                        velocity = Vector3.ClampMagnitude(velocity, 20.0f);
                        
                        return x.camera.transform.To3DAttributes(velocity);
                    });
            }

            if (!attributes.Any())
                attributes = Enumerable.Repeat(Vector3.zero.To3DAttributes(), 1);
            
            if (currentSystem.set3DNumListeners(attributes.Count()).LogErrorIfNotOk() != RESULT.OK)
                return;

            int i = 0;
            foreach (var attributes3D in attributes)
            {
                VECTOR position = attributes3D.position;
                VECTOR velocity = attributes3D.velocity;
                VECTOR forward = attributes3D.forward;
                VECTOR up = attributes3D.up;
            
                currentSystem.set3DListenerAttributes(i, ref position, ref velocity, ref forward, ref up);
                i++;
            }

            foreach (var item in SceneView.sceneViews.OfType<SceneView>())
                sceneViewExtras.GetOrCreateValue(item).lastPosition = item.camera.transform.position;
        }

        static void ReleaseEditorSystem()
        {
            currentSystem.release().LogErrorIfNotOk();
            
            EditorApplication.update -= UpdateEditorSystem;
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseEditorSystem;
        }

        [MemberNotNull(nameof(studioListeners))]
        static void UpdateGameView() => studioListeners = Object.FindObjectsByType<StudioListener>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInit()
        {
            SoundWaveManager.currentSystem = RuntimeManager.CoreSystem;
            Kernel.quitting += () => SoundWaveManager.currentSystem = currentSystem;
        }
    }
}