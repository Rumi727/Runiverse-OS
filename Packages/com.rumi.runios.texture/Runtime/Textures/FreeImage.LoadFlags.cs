#nullable enable
namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        [Flags]
        public enum LoadFlags
        {
            defaultValue = 0,
            jpegAccurate = 0x0002
        }
    }
}