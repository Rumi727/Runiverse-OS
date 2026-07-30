namespace RuniOS.Sounds
{
    public interface ILoopablePlayer : IPlayable
    {
        public bool loop { get; set; }
        public double loopStart { get; set; }
        public double loopEnd { get; set; }
    }
}