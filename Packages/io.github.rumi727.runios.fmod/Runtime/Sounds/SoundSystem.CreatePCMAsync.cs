#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public UniTask<WaveAudioClip?> CreatePCMAsync(byte[] pcm, int channel, int frequency, PCMFormat format)
        {
            ThrowIfSystemLockHeld();
            ThrowIfInvalidPCMFormat(channel, frequency);

            return UniTask.RunOnThreadPool(() =>
            {
                Execute(system => system.CreatePCM(pcm, channel, frequency, format), out WaveAudioClip? clip);
                return clip;
            });
        }
    }
}
