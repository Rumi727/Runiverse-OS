#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        [BurstCompile]
        struct ByteMipmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<byte> input;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<byte> output;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int channelCount;
            public bool sRgb;

            public void Execute(int outputIndex)
            {
                GetSampleIndices(outputIndex, inputWidth, inputHeight, outputWidth, out int first, out int second, out int third, out int fourth);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    byte firstValue = input[(first * channelCount) + channel];
                    byte secondValue = input[(second * channelCount) + channel];
                    byte thirdValue = input[(third * channelCount) + channel];
                    byte fourthValue = input[(fourth * channelCount) + channel];

                    output[(outputIndex * channelCount) + channel] = sRgb && channel < 3
                        ? (byte)AverageSrgb(firstValue, secondValue, thirdValue, fourthValue, byte.MaxValue)
                        : (byte)(((uint)firstValue + secondValue + thirdValue + fourthValue) / 4u);
                }
            }
        }
    }
}
