namespace RuniOS.Sounds
{
    public interface ITempoControl : IAudioPlayer
    {
        public float tempo { get; set; }
    }
}