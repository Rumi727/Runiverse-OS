#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public async UniTask<WaveAudioClip> CreateSoundAsync(IONode node, bool keepCompressed = false)
        {
            ThrowIfSystemLockHeld();

            byte[] data = await node.file.ReadAllBytes();
            return await CreateSoundAsync(data, keepCompressed);
        }

        public async UniTask<WaveAudioClip> CreateSoundAsync(Stream stream, bool keepCompressed = false)
        {
            ThrowIfSystemLockHeld();

            byte[] data = await stream.ReadToEndAsync();
            return await CreateSoundAsync(data, keepCompressed);
        }

        public UniTask<WaveAudioClip> CreateSoundAsync(byte[] data, bool keepCompressed = false)
        {
            ThrowIfSystemLockHeld();
            return UniTask.RunOnThreadPool(() => CreateSound(data, keepCompressed));
        }
    }
}
