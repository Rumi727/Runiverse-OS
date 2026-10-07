namespace RuniOS.Sounds
{
    public interface IPausable : IAudioPlayer
    {
        public bool isPaused { get; set; }

        public void Pause();
        public void UnPause();
    }
}