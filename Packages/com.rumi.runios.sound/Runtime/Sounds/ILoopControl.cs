namespace RuniOS.Sounds
{
    public interface ILoopControl : IAudioPlayer
    {
        public bool loop { get; set; }
        public double loopStart { get; set; }
        public double loopEnd { get; set; }
    }
}