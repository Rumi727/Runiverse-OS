#nullable enable
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Textures
{
    public static partial class TextureLoader
    {
        static class TextureMipmapScheduler
        {
            public static TextureMipmapData Schedule(DecodedImage decodedImage, int mipmapCount)
            {
                int actualMipmapCount = GetMipmapCount(decodedImage.width, decodedImage.height, mipmapCount);
                NativeArray<byte>[] levels = actualMipmapCount == 1 ? [] : new NativeArray<byte>[actualMipmapCount - 1];
                JobHandle dependency = default;

                try
                {
                    int inputWidth = decodedImage.width;
                    int inputHeight = decodedImage.height;
                    NativeArray<byte> input = decodedImage.pixels;

                    for (int level = 1; level < actualMipmapCount; level++)
                    {
                        int outputWidth = Max(1, inputWidth >> 1);
                        int outputHeight = Max(1, inputHeight >> 1);
                        NativeArray<byte> output = new NativeArray<byte>
                        (
                            checked(outputWidth * outputHeight * decodedImage.bytesPerPixel),
                            Allocator.Persistent,
                            NativeArrayOptions.UninitializedMemory
                        );
                        levels[level - 1] = output;

                        dependency = ScheduleLevel
                        (
                            decodedImage,
                            input,
                            output,
                            inputWidth,
                            inputHeight,
                            outputWidth,
                            dependency
                        );

                        input = output;
                        inputWidth = outputWidth;
                        inputHeight = outputHeight;
                    }

                    return new TextureMipmapData(levels, dependency);
                }
                catch
                {
                    dependency.Complete();
                    DisposeLevels(levels);
                    throw;
                }
            }

            static JobHandle ScheduleLevel
            (
                DecodedImage decodedImage,
                NativeArray<byte> input,
                NativeArray<byte> output,
                int inputWidth,
                int inputHeight,
                int outputWidth,
                JobHandle dependency
            )
            {
                int outputPixelCount = output.Length / decodedImage.bytesPerPixel;
                int batchSize = Max(1, outputWidth);

                return decodedImage.mipmapKind switch
                {
                    TextureMipmapKind.byteChannels => new ByteMipmapJob
                    {
                        input = input,
                        output = output,
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.unsignedShortChannels => new UnsignedShortMipmapJob
                    {
                        input = input.Reinterpret<ushort>(),
                        output = output.Reinterpret<ushort>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(ushort)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.signedShortChannels => new SignedShortMipmapJob
                    {
                        input = input.Reinterpret<short>(),
                        output = output.Reinterpret<short>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(short)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.unsignedIntPayload => new UnsignedIntMipmapJob
                    {
                        input = input.Reinterpret<uint>(),
                        output = output.Reinterpret<uint>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(uint)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.signedIntPayload => new SignedIntMipmapJob
                    {
                        input = input.Reinterpret<int>(),
                        output = output.Reinterpret<int>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(int)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.floatChannels => new FloatMipmapJob
                    {
                        input = input.Reinterpret<float>(),
                        output = output.Reinterpret<float>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(float)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.doublePayload => new DoubleMipmapJob
                    {
                        input = input.Reinterpret<double>(),
                        output = output.Reinterpret<double>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth,
                        channelCount = decodedImage.bytesPerPixel / sizeof(double)
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    TextureMipmapKind.rgb565 => new Rgb565MipmapJob
                    {
                        input = input.Reinterpret<ushort>(),
                        output = output.Reinterpret<ushort>(),
                        inputWidth = inputWidth,
                        inputHeight = inputHeight,
                        outputWidth = outputWidth
                    }.Schedule(outputPixelCount, batchSize, dependency),
                    _ => throw new NotSupportedException($"Unsupported mipmap kernel: {decodedImage.mipmapKind}.")
                };
            }

            static int GetMipmapCount(int width, int height, int mipmapCount)
            {
                int maximumCount = 1;
                int maximumDimension = Max(width, height);

                while (maximumDimension > 1)
                {
                    maximumDimension >>= 1;
                    maximumCount++;
                }

                if (mipmapCount <= 0)
                    return maximumCount;
                if (mipmapCount == 1)
                    return 1;
                if (mipmapCount <= maximumCount)
                    return mipmapCount;

                throw new ArgumentOutOfRangeException(nameof(mipmapCount), mipmapCount, $"Mipmap count cannot exceed {maximumCount} for a {width}x{height} image.");
            }

            static void DisposeLevels(NativeArray<byte>[] levels)
            {
                for (int index = 0; index < levels.Length; index++)
                {
                    if (levels[index].IsCreated)
                        levels[index].Dispose();
                }
            }
        }
    }
}
