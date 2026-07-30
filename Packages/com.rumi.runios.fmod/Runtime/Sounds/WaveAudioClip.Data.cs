#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        public int subSoundCount => UseNative(sound =>
        {
            sound.getNumSubSounds(out int count).ThrowIfNotOk();
            return count;
        });

        public int tagCount => GetTagCounts().count;
        public int updatedTagCount => GetTagCounts().updatedCount;

        public int syncPointCount => UseNative(sound =>
        {
            sound.getNumSyncPoints(out int count).ThrowIfNotOk();
            return count;
        });

        public uint ReadData(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return UseNative(sound =>
            {
                sound.readData(buffer, out uint read).ThrowIfNotOk();
                return read;
            });
        }

        public void SeekData(uint sample) => UseNative(sound => sound.seekData(sample).ThrowIfNotOk());

        (int count, int updatedCount) GetTagCounts() => UseNative(sound =>
        {
            sound.getNumTags(out int count, out int updatedCount).ThrowIfNotOk();
            return (count, updatedCount);
        });
    }
}