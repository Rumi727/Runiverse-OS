#nullable enable
using FMOD;
using System.Runtime.InteropServices;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public WaveAudioClip CreatePCM(byte[] pcm, int channel, int frequency, PCMFormat format)
        {
            ThrowIfInvalidPCMFormat(channel, frequency);
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();
                return CreatePCMUnsafe(pcm, channel, frequency, (SOUND_FORMAT)format);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        static void ThrowIfInvalidPCMFormat(int channel, int frequency)
        {
            if (channel <= 0)
                throw new ArgumentOutOfRangeException(nameof(channel), channel, "PCM channel count must be greater than zero.");

            if (frequency <= 0)
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "PCM frequency must be greater than zero.");
        }

        WaveAudioClip CreatePCMUnsafe(byte[] pcm, int channel, int frequency, SOUND_FORMAT format)
        {
            CREATESOUNDEXINFO exInfo = new()
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = checked((uint)pcm.Length),
                numchannels = channel,
                defaultfrequency = frequency,
                format = format
            };

            native.createSound(pcm, MODE.OPENMEMORY | MODE.OPENRAW | MODE.CREATESAMPLE | MODE._3D, ref exInfo, out Sound sound).ThrowIfNotOk();
            return WaveAudioClip.Unsafe.CreateInstance(this, sound);
        }
    }
}
