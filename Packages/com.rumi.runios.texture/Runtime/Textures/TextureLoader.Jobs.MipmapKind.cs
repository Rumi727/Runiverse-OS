#nullable enable
namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        enum TextureMipmapKind
        {
            byteChannels,
            unsignedShortChannels,
            signedShortChannels,
            unsignedIntPayload,
            signedIntPayload,
            floatChannels,
            doublePayload,
            rgb565
        }
    }
}