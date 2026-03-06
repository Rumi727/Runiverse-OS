#nullable enable
namespace RuniOS.Sounds
{
    public static class SoundWaveManager
    {
#if UNITY_EDITOR
        public static FMOD.System currentSystem { get; internal set; }
#else
        public static FMOD.System currentSystem => FMODUnity.RuntimeManager.CoreSystem;
#endif
    }
}