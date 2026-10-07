#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        public int musicChannelCount => UseNative(sound =>
        {
            sound.getMusicNumChannels(out int count).ThrowIfNotOk();
            return count;
        });

        public float musicSpeed
        {
            get => UseNative(sound =>
            {
                sound.getMusicSpeed(out float speed).ThrowIfNotOk();
                return speed;
            });
            set => UseNative(sound => sound.setMusicSpeed(value).ThrowIfNotOk());
        }

        public float GetMusicChannelVolume(int channel) => UseNative(sound =>
        {
            sound.getMusicChannelVolume(channel, out float volume).ThrowIfNotOk();
            return volume;
        });

        public void SetMusicChannelVolume(int channel, float volume) => UseNative(sound => sound.setMusicChannelVolume(channel, volume).ThrowIfNotOk());
    }
}