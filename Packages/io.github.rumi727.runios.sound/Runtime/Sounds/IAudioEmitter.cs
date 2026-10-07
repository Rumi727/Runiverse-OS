namespace RuniOS.Sounds
{
    public interface IAudioEmitter : IAudioPlayer
    {
        public float panStereo { set; }

        public float spatialBlend { get; set; }
        public float dopplerLevel { get; set; }
        public float spread { get; set; }

        public float minDistance { get; set; }
        public float maxDistance { get; set; }
    }
}