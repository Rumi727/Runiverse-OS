#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;

namespace RuniOS.Sounds
{
    sealed partial class SoundSystemManager
    {
        [Awaken]
        static void Awaken() => RuniPlayerLoop.onPostLateUpdate += Update;

        static void Update() => SoundSystem.main.Execute(system => system.Update());
    }
}