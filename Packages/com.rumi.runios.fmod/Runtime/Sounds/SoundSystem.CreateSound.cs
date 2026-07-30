#nullable enable
using FMOD;
using System.IO;
using System.Runtime.InteropServices;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public WaveAudioClip CreateSound(byte[] data, bool keepCompressed = false)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return CreateSoundUnsafe(data, keepCompressed);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public WaveAudioClip CreateSound(Stream stream, bool keepCompressed = false)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return CreateSoundUnsafe(stream.ReadToEnd(), keepCompressed);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        WaveAudioClip CreateSoundUnsafe(byte[] data, bool keepCompressed)
        {
            CREATESOUNDEXINFO exInfo = new()
            {
                cbsize = Marshal.SizeOf<CREATESOUNDEXINFO>(),
                length = checked((uint)data.Length)
            };

            MODE mode = MODE.OPENMEMORY | (keepCompressed ? MODE.CREATECOMPRESSEDSAMPLE : MODE.CREATESAMPLE);
            native.createSound(data, mode | MODE._3D, ref exInfo, out Sound sound).ThrowIfNotOk();

            return new WaveAudioClip(this, sound);
        }
    }
}
