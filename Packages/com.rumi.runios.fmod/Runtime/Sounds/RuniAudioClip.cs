#nullable enable
namespace RuniOS.Sounds
{
    public abstract class RuniAudioClip : IDisposable
    {
        public abstract double length { get; }
        public abstract void Dispose();
    }
}