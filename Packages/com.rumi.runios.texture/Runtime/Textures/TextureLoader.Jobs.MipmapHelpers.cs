#nullable enable
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
    }
}