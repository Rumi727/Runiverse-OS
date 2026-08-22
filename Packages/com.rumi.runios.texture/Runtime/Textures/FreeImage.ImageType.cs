#nullable enable
namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        public enum ImageType
        {
            unknown = 0,
            bitmap = 1,
            uint16 = 2,
            int16 = 3,
            uint32 = 4,
            int32 = 5,
            float32 = 6,
            float64 = 7,
            complex = 8,
            rgb16 = 9,
            rgba16 = 10,
            rgbFloat = 11,
            rgbaFloat = 12
        }
    }
}