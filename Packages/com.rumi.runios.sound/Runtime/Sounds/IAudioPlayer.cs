namespace RuniOS.Sounds
{
    public interface IAudioPlayer : ILoopablePlayer
    {
        public float volume { get; set; }
        public float pitch { get; set; }

        public float panStereo { get; set; }

        public bool isPitchSupported { get; }
    }
}