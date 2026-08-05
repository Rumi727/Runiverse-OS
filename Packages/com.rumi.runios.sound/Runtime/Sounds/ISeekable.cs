namespace RuniOS.Sounds
{
    public interface ISeekable : IAudioPlayer
    {
        public double time { get; set; }
        public double length { get; }
    }
}