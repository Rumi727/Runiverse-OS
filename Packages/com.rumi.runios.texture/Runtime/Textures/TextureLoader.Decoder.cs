#nullable enable
using System.Buffers;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        static class FreeImageDecoder
        {
            public static unsafe DecodedImage Decode(ReadOnlyMemory<byte> encodedData)
            {
                MemoryHandle pinnedData = default;
                FreeImage.Memory memory = default;
                FreeImage.Bitmap bitmap = default;

                try
                {
                    pinnedData = encodedData.Pin();
                    memory = FreeImage.Memory.Open((IntPtr)pinnedData.Pointer, checked((uint)encodedData.Length));
                    if (memory.isInvalid)
                        throw new InvalidDataException("FreeImage could not open the encoded image buffer.");

                    FreeImage.Format format = memory.GetFileType(encodedData.Length);
                    if (format == FreeImage.Format.unknown)
                        throw new InvalidDataException("FreeImage could not determine the encoded image format.");

                    FreeImage.LoadFlags loadFlags = format == FreeImage.Format.jpeg ? FreeImage.LoadFlags.jpegAccurate : FreeImage.LoadFlags.defaultValue;
                    bitmap = FreeImage.Bitmap.LoadFromMemory(format, memory, loadFlags);

                    if (bitmap.isInvalid)
                        throw new InvalidDataException($"FreeImage could not decode the {format} image.");

                    return DecodeBitmap(ref bitmap);
                }
                finally
                {
                    bitmap.Dispose();
                    memory.Dispose();
                    pinnedData.Dispose();
                }
            }

            static DecodedImage DecodeBitmap(ref FreeImage.Bitmap bitmap)
            {
                FreeImage.ImageType imageType = bitmap.imageType;
                int bitsPerPixel = bitmap.bitsPerPixel;

                if (imageType == FreeImage.ImageType.bitmap)
                {
                    FreeImage.ColorType colorType = bitmap.colorType;
                    bool hasAlpha = colorType == FreeImage.ColorType.rgbAlpha || bitmap.transparencyCount > 0 || bitmap.isTransparent;
                    bool isGrayscale = colorType is FreeImage.ColorType.minIsBlack or FreeImage.ColorType.minIsWhite;
                    bool isPalette = colorType == FreeImage.ColorType.palette;

                    if (isPalette && !hasAlpha && IsGrayscalePalette(bitmap, bitsPerPixel))
                        return DecodePaletteGrayscale(bitmap, bitsPerPixel);

                    if (isGrayscale && !hasAlpha && bitsPerPixel is 1 or 2 or 4 or 8)
                        return DecodeGrayscale(bitmap, bitsPerPixel, colorType == FreeImage.ColorType.minIsWhite);

                    bool isSupportedPackedColor = bitsPerPixel == 16 && IsRgb565Or555(bitmap);
                    bool isCanonicalRgba = bitsPerPixel == 32 && colorType == FreeImage.ColorType.rgbAlpha;
                    bool convertToRgb = colorType == FreeImage.ColorType.cmyk
                        || isPalette
                        || (bitsPerPixel <= 8 && !isGrayscale)
                        || (bitsPerPixel == 16 && !isGrayscale && !isSupportedPackedColor)
                        || (bitsPerPixel == 32 && !hasAlpha);

                    if ((hasAlpha && !isCanonicalRgba) || convertToRgb)
                    {
                        FreeImage.Bitmap converted = hasAlpha ? bitmap.ConvertTo32Bits() : bitmap.ConvertTo24Bits();
                        if (converted.isInvalid)
                            throw new InvalidDataException($"FreeImage could not normalize a {bitsPerPixel}-bit {colorType} bitmap.");

                        bitmap.Dispose();
                        bitmap = converted;

                        imageType = bitmap.imageType;
                        bitsPerPixel = bitmap.bitsPerPixel;
                    }
                }

                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                TextureFormat textureFormat;
                TextureMipmapKind mipmapKind;
                int bytesPerPixel;
                bool normalizeBitmap = false;
                bool convertRgb555ToRgb565 = false;
                bool expandRgbFloat = false;
                bool invertUnsignedShorts = false;

                switch (imageType)
                {
                    case FreeImage.ImageType.bitmap when bitsPerPixel == 16:
                    {
                        FreeImage.ColorType colorType = bitmap.colorType;
                        if (colorType is FreeImage.ColorType.minIsBlack or FreeImage.ColorType.minIsWhite)
                        {
                            textureFormat = TextureFormat.R16;
                            mipmapKind = TextureMipmapKind.unsignedShortChannels;
                            bytesPerPixel = 2;
                            invertUnsignedShorts = colorType == FreeImage.ColorType.minIsWhite;
                        }
                        else if (IsRgb565Or555(bitmap))
                        {
                            textureFormat = TextureFormat.RGB565;
                            mipmapKind = TextureMipmapKind.rgb565;
                            bytesPerPixel = 2;
                            normalizeBitmap = true;
                            convertRgb555ToRgb565 = bitmap.redMask == 0x7C00;
                        }
                        else
                            throw new NotSupportedException("Unsupported 16-bit bitmap channel masks.");

                        break;
                    }
                    case FreeImage.ImageType.bitmap when bitsPerPixel == 24:
                    {
                        textureFormat = TextureFormat.RGB24;
                        mipmapKind = TextureMipmapKind.byteChannels;
                        bytesPerPixel = 3;
                        normalizeBitmap = true;
                        break;
                    }
                    case FreeImage.ImageType.bitmap when bitsPerPixel == 32:
                    {
                        textureFormat = TextureFormat.RGBA32;
                        mipmapKind = TextureMipmapKind.byteChannels;
                        bytesPerPixel = 4;
                        normalizeBitmap = true;
                        break;
                    }
                    case FreeImage.ImageType.uint16:
                    {
                        textureFormat = TextureFormat.R16;
                        mipmapKind = TextureMipmapKind.unsignedShortChannels;
                        bytesPerPixel = 2;
                        break;
                    }
                    case FreeImage.ImageType.int16:
                    {
                        textureFormat = TextureFormat.R16_SIGNED;
                        mipmapKind = TextureMipmapKind.signedShortChannels;
                        bytesPerPixel = 2;
                        break;
                    }
                    case FreeImage.ImageType.uint32:
                    {
                        textureFormat = TextureFormat.RG32;
                        mipmapKind = TextureMipmapKind.unsignedIntPayload;
                        bytesPerPixel = 4;
                        break;
                    }
                    case FreeImage.ImageType.int32:
                    {
                        textureFormat = TextureFormat.RG32_SIGNED;
                        mipmapKind = TextureMipmapKind.signedIntPayload;
                        bytesPerPixel = 4;
                        break;
                    }
                    case FreeImage.ImageType.float32:
                    {
                        textureFormat = TextureFormat.RFloat;
                        mipmapKind = TextureMipmapKind.floatChannels;
                        bytesPerPixel = 4;
                        break;
                    }
                    case FreeImage.ImageType.float64:
                    {
                        textureFormat = TextureFormat.RGFloat;
                        mipmapKind = TextureMipmapKind.doublePayload;
                        bytesPerPixel = 8;
                        break;
                    }
                    case FreeImage.ImageType.complex:
                    {
                        textureFormat = TextureFormat.RGBAFloat;
                        mipmapKind = TextureMipmapKind.doublePayload;
                        bytesPerPixel = 16;
                        break;
                    }
                    case FreeImage.ImageType.rgb16:
                    {
                        textureFormat = TextureFormat.RGB48;
                        mipmapKind = TextureMipmapKind.unsignedShortChannels;
                        bytesPerPixel = 6;
                        break;
                    }
                    case FreeImage.ImageType.rgba16:
                    {
                        textureFormat = TextureFormat.RGBA64;
                        mipmapKind = TextureMipmapKind.unsignedShortChannels;
                        bytesPerPixel = 8;
                        break;
                    }
                    case FreeImage.ImageType.rgbFloat:
                    {
                        textureFormat = TextureFormat.RGBAFloat;
                        mipmapKind = TextureMipmapKind.floatChannels;
                        bytesPerPixel = 16;
                        expandRgbFloat = true;
                        break;
                    }
                    case FreeImage.ImageType.rgbaFloat:
                    {
                        textureFormat = TextureFormat.RGBAFloat;
                        mipmapKind = TextureMipmapKind.floatChannels;
                        bytesPerPixel = 16;
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Unsupported FreeImage type and bit-depth combination: {imageType}, {bitsPerPixel} bpp.");
                }

                NativeArray<byte> pixels;
                if (normalizeBitmap)
                    pixels = CopyBitmapPixels(bitmap, bytesPerPixel, FreeImage.isLittleEndian, convertRgb555ToRgb565);
                else if (expandRgbFloat)
                    pixels = CopyRgbFloatPixels(bitmap);
                else
                    pixels = CopyRows(bitmap, bytesPerPixel);

                if (invertUnsignedShorts)
                    InvertUnsignedShorts(pixels);

                return new DecodedImage(width, height, textureFormat, mipmapKind, pixels, bytesPerPixel);
            }

            static bool IsRgb565Or555(FreeImage.Bitmap bitmap)
            {
                uint redMask = bitmap.redMask;
                uint greenMask = bitmap.greenMask;
                uint blueMask = bitmap.blueMask;

                return (redMask == 0xF800 && greenMask == 0x07E0 && blueMask == 0x001F)
                    || (redMask == 0x7C00 && greenMask == 0x03E0 && blueMask == 0x001F);
            }

            static unsafe bool IsGrayscalePalette(FreeImage.Bitmap bitmap, int bitsPerPixel)
            {
                IntPtr palette = bitmap.palette;
                if (palette == IntPtr.Zero)
                    return false;

                uint colorCount = bitmap.usedColors;
                if (colorCount == 0)
                    colorCount = (uint)(1 << Min(bitsPerPixel, 8));

                byte* paletteBytes = (byte*)palette;
                for (uint index = 0; index < colorCount; index++)
                {
                    byte blue = paletteBytes[index * 4];
                    byte green = paletteBytes[(index * 4) + 1];
                    byte red = paletteBytes[(index * 4) + 2];
                    if (red != green || red != blue)
                        return false;
                }

                return true;
            }

            static unsafe DecodedImage DecodePaletteGrayscale(FreeImage.Bitmap bitmap, int bitsPerPixel)
            {
                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                IntPtr palette = bitmap.palette;
                if (palette == IntPtr.Zero)
                    throw new InvalidDataException("The paletted bitmap does not contain a palette.");

                NativeArray<byte> pixels = new NativeArray<byte>(checked(width * height), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                try
                {
                    byte* source = (byte*)bitmap.bits;
                    byte* paletteBytes = (byte*)palette;
                    byte* destination = (byte*)pixels.GetUnsafePtr();
                    int pitch = bitmap.pitch;
                    if (source == null || pitch <= 0)
                        throw new InvalidDataException("FreeImage returned an invalid palette pixel buffer.");

                    for (int y = 0; y < height; y++)
                    {
                        byte* sourceRow = source + (y * pitch);
                        byte* destinationRow = destination + (y * width);
                        for (int x = 0; x < width; x++)
                        {
                            byte paletteIndex = ReadPackedValue(sourceRow, x, bitsPerPixel);
                            destinationRow[x] = paletteBytes[(paletteIndex * 4) + 2];
                        }
                    }

                    return new DecodedImage(width, height, TextureFormat.R8, TextureMipmapKind.byteChannels, pixels, 1);
                }
                catch
                {
                    pixels.Dispose();
                    throw;
                }
            }

            static unsafe DecodedImage DecodeGrayscale(FreeImage.Bitmap bitmap, int bitsPerPixel, bool invert)
            {
                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                NativeArray<byte> pixels = new NativeArray<byte>(checked(width * height), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                try
                {
                    byte* source = (byte*)bitmap.bits;
                    byte* destination = (byte*)pixels.GetUnsafePtr();
                    int pitch = bitmap.pitch;
                    if (source == null || pitch <= 0)
                        throw new InvalidDataException("FreeImage returned an invalid grayscale pixel buffer.");

                    int sourceMaximum = (1 << bitsPerPixel) - 1;

                    for (int y = 0; y < height; y++)
                    {
                        byte* sourceRow = source + (y * pitch);
                        byte* destinationRow = destination + (y * width);
                        for (int x = 0; x < width; x++)
                        {
                            int value = ReadPackedValue(sourceRow, x, bitsPerPixel);
                            value = (value * byte.MaxValue) / sourceMaximum;
                            destinationRow[x] = (byte)(invert ? byte.MaxValue - value : value);
                        }
                    }

                    return new DecodedImage(
                        width,
                        height,
                        TextureFormat.R8,
                        TextureMipmapKind.byteChannels,
                        pixels,
                        1);
                }
                catch
                {
                    pixels.Dispose();
                    throw;
                }
            }

            static unsafe byte ReadPackedValue(byte* source, int x, int bitsPerPixel) => bitsPerPixel switch
            {
                1 => (byte)((source[x >> 3] >> (7 - (x & 7))) & 1),
                2 => (byte)((source[(x * 2) >> 3] >> (6 - ((x * 2) & 7))) & 3),
                4 => (byte)((source[x >> 1] >> ((x & 1) == 0 ? 4 : 0)) & 0xF),
                8 => source[x],
                _ => throw new ArgumentOutOfRangeException(nameof(bitsPerPixel))
            };

            static unsafe NativeArray<byte> CopyRows(FreeImage.Bitmap bitmap, int bytesPerPixel)
            {
                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                int rowByteCount = checked(width * bytesPerPixel);
                int pitch = bitmap.pitch;
                IntPtr bits = bitmap.bits;
                if (bits == IntPtr.Zero || pitch < rowByteCount)
                    throw new InvalidDataException("FreeImage returned an invalid pixel buffer.");

                NativeArray<byte> pixels = new NativeArray<byte>(checked(rowByteCount * height), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                try
                {
                    byte* source = (byte*)bits;
                    byte* destination = (byte*)pixels.GetUnsafePtr();
                    for (int y = 0; y < height; y++)
                        UnsafeUtility.MemCpy(destination + (y * rowByteCount), source + (y * pitch), rowByteCount);

                    return pixels;
                }
                catch
                {
                    pixels.Dispose();
                    throw;
                }
            }

            static unsafe NativeArray<byte> CopyBitmapPixels(FreeImage.Bitmap bitmap, int bytesPerPixel, bool swapRedBlue, bool convertRgb555ToRgb565)
            {
                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                int bitsPerPixel = bitmap.bitsPerPixel;
                int sourceBytesPerPixel = bitsPerPixel / 8;
                int sourceRowByteCount = checked(width * sourceBytesPerPixel);
                int destinationRowByteCount = checked(width * bytesPerPixel);
                int pitch = bitmap.pitch;
                IntPtr bits = bitmap.bits;
                if (bits == IntPtr.Zero || pitch < sourceRowByteCount)
                    throw new InvalidDataException("FreeImage returned an invalid pixel buffer.");

                NativeArray<byte> pixels = new NativeArray<byte>(checked(destinationRowByteCount * height), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                try
                {
                    byte* source = (byte*)bits;
                    byte* destination = (byte*)pixels.GetUnsafePtr();
                    for (int y = 0; y < height; y++)
                    {
                        byte* sourceRow = source + (y * pitch);
                        byte* destinationRow = destination + (y * destinationRowByteCount);

                        for (int x = 0; x < width; x++)
                        {
                            byte* sourcePixel = sourceRow + (x * sourceBytesPerPixel);
                            byte* destinationPixel = destinationRow + (x * bytesPerPixel);

                            if (bitsPerPixel == 16)
                            {
                                ushort value = ((ushort*)sourcePixel)[0];
                                if (convertRgb555ToRgb565)
                                    value = ConvertRgb555ToRgb565(value);

                                ((ushort*)destinationPixel)[0] = value;
                                continue;
                            }

                            if (swapRedBlue)
                            {
                                destinationPixel[0] = sourcePixel[2];
                                destinationPixel[1] = sourcePixel[1];
                                destinationPixel[2] = sourcePixel[0];
                                if (bitsPerPixel == 32)
                                    destinationPixel[3] = sourcePixel[3];
                            }
                            else
                                UnsafeUtility.MemCpy(destinationPixel, sourcePixel, bytesPerPixel);
                        }
                    }

                    return pixels;
                }
                catch
                {
                    pixels.Dispose();
                    throw;
                }
            }

            static unsafe NativeArray<byte> CopyRgbFloatPixels(FreeImage.Bitmap bitmap)
            {
                int width = checked((int)bitmap.width);
                int height = checked((int)bitmap.height);
                if (width <= 0 || height <= 0)
                    throw new InvalidDataException("The decoded image has invalid dimensions.");

                const int sourceBytesPerPixel = 12;
                const int destinationBytesPerPixel = 16;

                int sourceRowByteCount = checked(width * sourceBytesPerPixel);
                int destinationRowByteCount = checked(width * destinationBytesPerPixel);
                int pitch = bitmap.pitch;
                IntPtr bits = bitmap.bits;
                if (bits == IntPtr.Zero || pitch < sourceRowByteCount)
                    throw new InvalidDataException("FreeImage returned an invalid pixel buffer.");

                NativeArray<byte> pixels = new NativeArray<byte>(checked(destinationRowByteCount * height), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                try
                {
                    byte* source = (byte*)bits;
                    byte* destination = (byte*)pixels.GetUnsafePtr();
                    for (int y = 0; y < height; y++)
                    {
                        float* sourceRow = (float*)(source + (y * pitch));
                        float* destinationRow = (float*)(destination + (y * destinationRowByteCount));
                        for (int x = 0; x < width; x++)
                        {
                            float* sourcePixel = sourceRow + (x * 3);
                            float* destinationPixel = destinationRow + (x * 4);
                            destinationPixel[0] = sourcePixel[0];
                            destinationPixel[1] = sourcePixel[1];
                            destinationPixel[2] = sourcePixel[2];
                            destinationPixel[3] = 1f;
                        }
                    }

                    return pixels;
                }
                catch
                {
                    pixels.Dispose();
                    throw;
                }
            }

            static ushort ConvertRgb555ToRgb565(ushort value)
            {
                int red = (value >> 10) & 0x1F;
                int green = (value >> 5) & 0x1F;
                int blue = value & 0x1F;
                green = (green << 1) | (green >> 4);
                return (ushort)((red << 11) | (green << 5) | blue);
            }

            static unsafe void InvertUnsignedShorts(NativeArray<byte> pixels)
            {
                ushort* values = (ushort*)pixels.GetUnsafePtr();
                int valueCount = pixels.Length / sizeof(ushort);
                for (int index = 0; index < valueCount; index++)
                    values[index] = (ushort)(ushort.MaxValue - values[index]);
            }
        }
    }
}