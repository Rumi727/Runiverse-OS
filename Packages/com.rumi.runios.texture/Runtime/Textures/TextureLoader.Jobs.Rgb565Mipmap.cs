#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        [BurstCompile]
        struct Rgb565MipmapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ushort> input;
            [WriteOnly] public NativeArray<ushort> output;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;

            public void Execute(int outputIndex)
            {
                GetSampleIndices(outputIndex, inputWidth, inputHeight, outputWidth, out int first, out int second, out int third, out int fourth);

                ushort firstValue = input[first];
                ushort secondValue = input[second];
                ushort thirdValue = input[third];
                ushort fourthValue = input[fourth];
                int red = ((firstValue >> 11) & 0x1F) + ((secondValue >> 11) & 0x1F)
                    + ((thirdValue >> 11) & 0x1F) + ((fourthValue >> 11) & 0x1F);
                int green = ((firstValue >> 5) & 0x3F) + ((secondValue >> 5) & 0x3F)
                    + ((thirdValue >> 5) & 0x3F) + ((fourthValue >> 5) & 0x3F);
                int blue = (firstValue & 0x1F) + (secondValue & 0x1F)
                    + (thirdValue & 0x1F) + (fourthValue & 0x1F);

                output[outputIndex] = (ushort)(((red / 4) << 11) | ((green / 4) << 5) | (blue / 4));
            }
        }
    }
}