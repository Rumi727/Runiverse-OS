#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed class FMODAudioClip : SoundWaveClip
    {
        public FMODAudioClip(Sound sound)
        {
            sound.getDefaults(out float frequency, out _).ThrowIfNotOk();
            this.frequency = frequency;
            
            sound.getMusicNumChannels(out int channel).ThrowIfNotOk();
            this.channel = channel;

            sound.getLength(out uint samples, TIMEUNIT.PCM).ThrowIfNotOk();
            this.samples = samples;

            rawSound = sound;
        }
        
        public Sound rawSound { get; }
        
        public override double length => samples / frequency;
        public override uint samples { get; }
        
        public override float frequency { get; }
        public override int channel { get; }

        public override void Dispose() => rawSound.release().ThrowIfNotOk();
    }
}