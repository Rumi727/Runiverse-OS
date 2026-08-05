#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        public delegate void GetDataAction(PCMDoubleView samples, int channelCount);

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

        public void GetData(GetDataAction action) => GetData(0, checked(samples * (uint)channel), action);

        public void GetData(uint offset, GetDataAction action)
        {
            uint total = checked(samples * (uint)channel);
            GetData(offset, offset <= total ? total - offset : 0, action);
        }

        public void GetData(uint offset, uint length, GetDataAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (isStream)
                throw new InvalidOperationException("FMOD stream sound data cannot be locked.");

            if (keepCompressed)
                throw new InvalidOperationException("Compressed FMOD sound data cannot be converted to PCM samples.");

            if (pcmFormat == null)
                throw new InvalidOperationException("PCM format is null.");

            ulong totalSampleCount = (ulong)samples * (uint)channel;
            if ((ulong)offset + length > totalSampleCount)
                throw new ArgumentOutOfRangeException(nameof(length), length, "The requested sample range exceeds the clip data.");

            nativeLock.EnterReadLock();
            try
            {
                ThrowIfDisposedUnsafe();

                if (length == 0)
                {
                    action.Invoke(new PCMDoubleView(ReadOnlySpan<byte>.Empty, pcmFormat!.Value), channel);
                    return;
                }

                int bytesPerSample = bits / 8;
                uint byteOffset = checked(offset * (uint)bytesPerSample);
                uint byteLength = checked(length * (uint)bytesPerSample);

                native.@lock(byteOffset, byteLength, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2).ThrowIfNotOk();
                try
                {
                    unsafe
                    {
                        ReadOnlySpan<byte> source = new(ptr1.ToPointer(), checked((int)len1));
                        action.Invoke(new PCMDoubleView(source, pcmFormat!.Value), channel);
                    }
                }
                finally
                {
                    native.unlock(ptr1, ptr2, len1, len2).ThrowIfNotOk();
                }
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

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
