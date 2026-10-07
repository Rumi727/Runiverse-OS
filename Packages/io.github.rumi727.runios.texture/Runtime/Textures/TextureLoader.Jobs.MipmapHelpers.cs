#nullable enable
using Unity.Mathematics;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        static void GetSampleIndices(int outputIndex, int inputWidth, int inputHeight, int outputWidth, out int first, out int second, out int third, out int fourth)
        {
            int outputX = outputIndex % outputWidth;
            int outputY = outputIndex / outputWidth;
            int firstX = outputX * 2;
            int firstY = outputY * 2;
            int secondX = Min(firstX + 1, inputWidth - 1);
            int secondY = Min(firstY + 1, inputHeight - 1);

            first = (firstY * inputWidth) + firstX;
            second = (firstY * inputWidth) + secondX;
            third = (secondY * inputWidth) + firstX;
            fourth = (secondY * inputWidth) + secondX;
        }

        static int AverageSrgb(int first, int second, int third, int fourth, int maximumValue)
        {
            float scale = 1f / maximumValue;
            float average = (SrgbToLinear(first * scale)
                + SrgbToLinear(second * scale)
                + SrgbToLinear(third * scale)
                + SrgbToLinear(fourth * scale)) * 0.25f;

            return (int)math.round(LinearToSrgb(average) * maximumValue);
        }

        static float SrgbToLinear(float value) => value <= 0.04045f
            ? value / 12.92f
            : math.pow((value + 0.055f) / 1.055f, 2.4f);

        static float LinearToSrgb(float value) => value <= 0.0031308f
            ? value * 12.92f
            : (1.055f * math.pow(value, 1f / 2.4f)) - 0.055f;
    }
}
