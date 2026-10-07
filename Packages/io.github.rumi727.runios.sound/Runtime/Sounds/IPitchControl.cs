namespace RuniOS.Sounds
{
    public interface IPitchControl : IAudioPlayer
    {
        public float pitch { get; set; }
    }
}