#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        [Awaken]
        static void Awaken() => RuniPlayerLoop.onPostLateUpdate += RuntimeUpdate;

#if UNITY_EDITOR
        [OnCodeLoaded]
        static void OnCodeLoaded() => UnityEditor.EditorApplication.update += EditorUpdate;

        static void EditorUpdate()
        {
            if (!Kernel.isPlaying || UnityEditor.EditorApplication.isPaused)
                RuntimeUpdate();
        }

        [OnCodeUnloading]
        static void EditorDispose()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            main.Dispose();
        }
#endif

        static void RuntimeUpdate() => main.Execute(system => system.Update());
    }
}