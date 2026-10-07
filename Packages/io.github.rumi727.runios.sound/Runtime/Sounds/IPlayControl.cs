namespace RuniOS.Sounds
{
    public interface IPlayControl : IAudioPlayer
    {
        public void Play(double startTime = 0);
    }
}