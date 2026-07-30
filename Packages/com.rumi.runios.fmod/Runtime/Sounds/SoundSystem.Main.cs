#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;
using UnityEngine.Scripting;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        [Awaken]
        [Preserve]
        static void Awaken() => RuniPlayerLoop.onPostLateUpdate += main.Update;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInit()
        {
            UnityEditor.EditorApplication.update += EditorUpdate;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += EditorDispose;
        }

        static void EditorUpdate()
        {
            if (!Kernel.isPlaying || UnityEditor.EditorApplication.isPaused)
                main.Update();
        }

        static void EditorDispose()
        {
            main.Dispose();

            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= EditorDispose;
        }
#endif
    }
}