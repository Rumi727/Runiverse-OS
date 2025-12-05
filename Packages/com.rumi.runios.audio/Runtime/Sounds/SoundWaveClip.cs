#nullable enable
namespace RuniOS.Sounds
{
    public abstract class SoundWaveClip : RuniAudioClip
    {
        public override double length => samples / frequency;
        public abstract uint samples { get; }
        
        public abstract float frequency { get; }
        public abstract int channel { get; }
    }
}