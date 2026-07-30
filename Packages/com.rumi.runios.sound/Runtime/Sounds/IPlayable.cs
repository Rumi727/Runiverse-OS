namespace RuniOS.Sounds
{
    public interface IPlayable
    {
        public bool isPlaying { get; }
        public bool isPaused { get; set; }

        public double time { get; set; }
        public double length { get; }

        public float tempo { get; set; }

        public void Play(double startTime = 0);
        public void Stop();

        public void Pause();
        public void UnPause();
    }
}