#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        [BurstCompile]
        struct SignedIntMipmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> input;
            [WriteOnly] public NativeArray<int> output;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int channelCount;

            public void Execute(int outputIndex)
            {
                GetSampleIndices(outputIndex, inputWidth, inputHeight, outputWidth, out int first, out int second, out int third, out int fourth);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    long total = input[(first * channelCount) + channel];
                    total += input[(second * channelCount) + channel];
                    total += input[(third * channelCount) + channel];
                    total += input[(fourth * channelCount) + channel];
                    output[(outputIndex * channelCount) + channel] = (int)(total / 4L);
                }
            }
        }
    }
}