#nullable enable
using RuniOS.Resource;

namespace RuniOS.Sounds
{
    public static class AudioPlayerManager
    {
        static readonly Dictionary<Identifier, Func<Identifier, RuniAudioSource>> playerConstructors = [];

        public static void Register(Identifier registryId, Func<Identifier, RuniAudioSource> constructor)
        {
            if (!playerConstructors.TryAdd(registryId, constructor))
                throw new InvalidOperationException($"Registry ID conflict: {registryId}");
        }

        public static void Unregister(Identifier registryKey) => playerConstructors.Remove(registryKey);

        public static RuniAudioSource? Create(ResourceKey resourceKey) => playerConstructors.GetValueOrDefault(resourceKey.registryId)?.Invoke(resourceKey.assetId);
    }
}