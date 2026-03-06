#nullable enable
using RuniOS.Resource;

namespace RuniOS.Sounds
{
    public record struct SoundClipRef(ResourceKey key)
    {
        public static implicit operator SoundClipRef(ResourceKey key) => new SoundClipRef(key);
        public static implicit operator ResourceKey(SoundClipRef sound) => sound.key;
    }
}