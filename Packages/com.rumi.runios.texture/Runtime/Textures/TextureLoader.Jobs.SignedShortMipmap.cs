#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        [BurstCompile]
        struct SignedShortMipmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> input;
            [WriteOnly] public NativeArray<short> output;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int channelCount;

            public void Execute(int outputIndex)
            {
                GetSampleIndices(outputIndex, inputWidth, inputHeight, outputWidth, out int first, out int second, out int third, out int fourth);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    int total = input[(first * channelCount) + channel];
                    total += input[(second * channelCount) + channel];
                    total += input[(third * channelCount) + channel];
                    total += input[(fourth * channelCount) + channel];
                    output[(outputIndex * channelCount) + channel] = (short)(total / 4);
                }
            }
        }
    }
}