#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        [BurstCompile]
        struct UnsignedIntMipmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<uint> input;
            [WriteOnly] public NativeArray<uint> output;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int channelCount;

            public void Execute(int outputIndex)
            {
                GetSampleIndices(outputIndex, inputWidth, inputHeight, outputWidth, out int first, out int second, out int third, out int fourth);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    ulong total = input[(first * channelCount) + channel];
                    total += input[(second * channelCount) + channel];
                    total += input[(third * channelCount) + channel];
                    total += input[(fourth * channelCount) + channel];
                    output[(outputIndex * channelCount) + channel] = (uint)(total / 4UL);
                }
            }
        }
    }
}