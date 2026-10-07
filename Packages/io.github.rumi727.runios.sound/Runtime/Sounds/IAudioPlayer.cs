namespace RuniOS.Sounds
{
    public interface IAudioPlayer
    {
        public bool isPlaying { get; }

        public float volume { get; set; }
    }
}