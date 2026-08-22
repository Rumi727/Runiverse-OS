#nullable enable
using Unity.Collections;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        readonly record struct DecodedImage
        (
            int width,
            int height,
            TextureFormat textureFormat,
            TextureMipmapKind mipmapKind,
            NativeArray<byte> pixels,
            int bytesPerPixel
        ) : IDisposable
        {
            public void Dispose() => pixels.Dispose();
        }
    }
}