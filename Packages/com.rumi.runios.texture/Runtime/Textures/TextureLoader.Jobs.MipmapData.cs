#nullable enable
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        readonly record struct TextureMipmapData(NativeArray<byte>[] levels, JobHandle dependency) : IDisposable
        {
            public NativeArray<byte>[] levels { get; } = levels;
            public JobHandle dependency { get; } = dependency;
            public int count => levels.Length + 1;

            public void Dispose()
            {
                dependency.Complete();
                for (int index = 0; index < levels.Length; index++)
                {
                    if (levels[index].IsCreated)
                        levels[index].Dispose();
                }
            }
        }
    }
}