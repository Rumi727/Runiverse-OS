#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        /// <summary>
        /// Converts the whole PCM data to 16-bit samples laid out per channel (<c>[channel * samples + sample]</c>).<br/>
        /// 전체 PCM 데이터를 채널별로 배치한(<c>[channel * samples + sample]</c>) 16비트 샘플로 변환합니다.
        /// </summary>
        /// <remarks>
        /// Formats other than 16-bit PCM are quantized to 16 bits. Schedules Burst jobs and waits for them, so it must be called from the main thread. The caller owns the returned array.<br/>
        /// 16비트 PCM이 아닌 포맷은 16비트로 양자화됩니다. Burst 잡을 스케줄하고 완료까지 기다리므로 메인 스레드에서 호출해야 합니다. 반환된 배열은 호출자가 소유합니다.
        /// </remarks>
        public unsafe NativeArray<short> CreatePlanarSamples(Allocator allocator)
        {
            int sampleCount = checked((int)samples);
            NativeArray<short> output = new NativeArray<short>(checked(sampleCount * channel), allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                GetRawData(0, (uint)output.Length, (source, channelCount) =>
                {
                    // 잡은 원본을 포인터로 읽으므로, FMOD가 돌려준 데이터가 출력 전체를 채울 만큼인지 스케줄 전에 확인합니다.
                    if (channelCount != channel)
                        throw new InvalidOperationException($"FMOD returned {channelCount} channels, but the clip has {channel} channels.");

                    int bytesPerSample = pcmFormat!.Value switch
                    {
                        PCMFormat.PCM8 => 1,
                        PCMFormat.PCM16 => 2,
                        PCMFormat.PCM24 => 3,
                        PCMFormat.PCM32 => 4,
                        PCMFormat.Float => 4,
                        _ => throw new InvalidOperationException($"FMOD returned unsupported sample format {pcmFormat}.")
                    };

                    if (source.Length < (long)output.Length * bytesPerSample)
                        throw new InvalidOperationException($"FMOD returned {source.Length} bytes, but {(long)output.Length * bytesPerSample} bytes are required.");

                    fixed (byte* pointer = source)
                    {
                        int chunkCount = ((sampleCount + planarChunkSize) - 1) / planarChunkSize;
                        JobHandle handle = pcmFormat!.Value switch
                        {
                            // ReSharper disable AccessToDisposedClosure
                            PCMFormat.PCM8 => new PCM8PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM16 => new PCM16PlanarJob { source = (short*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM24 => new PCM24PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM32 => new PCM32PlanarJob { source = (int*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.Float => new FloatPlanarJob { source = (float*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            _ => throw new InvalidOperationException($"FMOD returned unsupported sample format {pcmFormat}.")
                            // ReSharper restore AccessToDisposedClosure
                        };

                        // FMOD lock이 풀리기 전에 원본 포인터 사용을 끝내야 합니다.
                        handle.Complete();
                    }
                });
            }
            catch
            {
                output.Dispose();
                throw;
            }

            return output;
        }

        /*
         * 인터리브(L R L R ...)를 planar(L L ... R R ...)로 바꾸는 것은 전치이므로 읽기와 쓰기 중 한쪽은 반드시 간격이 생깁니다.
         * 모든 포맷의 잡은 다음 구조를 공유합니다.
         * - Execute 하나가 샘플 구간 하나를 맡습니다. 샘플 루프가 잡 디스패처가 아닌 잡 코드 안에 있어야 Burst가 그 루프를 벡터화할 수 있습니다.
         * - 채널을 바깥 루프에 두어 쓰기를 연속으로 만듭니다. 채널마다 같은 원본 구간을 다시 읽지만 구간이 작아 캐시에서 읽힙니다.
         * - 스테레오는 읽기 간격이 상수 2가 되도록 따로 처리합니다. 간격이 상수여야 연속으로 읽은 뒤 셔플로 나누는 벡터 코드가 나오며,
         *   런타임 간격(channelCount)으로는 벡터화되지 않습니다. 대부분의 클립이 스테레오이므로 이 경로 하나만 둡니다.
         *   이 경로는 인덱스를 곱하지 않고 포인터를 전진시킵니다. 32비트 곱(i * 2)은 넘칠 수 있어 Burst가 연속 접근임을 증명하지 못하고
         *   레인마다 따로 읽는 코드를 만들지만, 64비트 포인터 증가는 그런 문제가 없습니다.
         */
        const int planarChunkSize = 16384;

        [BurstCompile]
        unsafe struct PCM8PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public byte* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int chunk)
            {
                int start = chunk * planarChunkSize;
                int end = Math.Min(start + planarChunkSize, sampleCount);

                if (channelCount == 2)
                {
                    byte* frame = source + ((long)start * 2);
                    for (int i = start; i < end; i++, frame += 2)
                    {
                        output[i] = Convert(frame[0]);
                        output[sampleCount + i] = Convert(frame[1]);
                    }

                    return;
                }

                for (int channel = 0; channel < channelCount; channel++)
                {
                    int offset = channel * sampleCount;
                    for (int i = start; i < end; i++)
                        output[offset + i] = Convert(source[(i * channelCount) + channel]);
                }
            }

            static short Convert(byte value) => (short)(unchecked((sbyte)value) << 8);
        }

        [BurstCompile]
        unsafe struct PCM16PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public short* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int chunk)
            {
                int start = chunk * planarChunkSize;
                int end = Math.Min(start + planarChunkSize, sampleCount);

                if (channelCount == 2)
                {
                    short* frame = source + ((long)start * 2);
                    for (int i = start; i < end; i++, frame += 2)
                    {
                        output[i] = frame[0];
                        output[sampleCount + i] = frame[1];
                    }

                    return;
                }

                for (int channel = 0; channel < channelCount; channel++)
                {
                    int offset = channel * sampleCount;
                    for (int i = start; i < end; i++)
                        output[offset + i] = source[(i * channelCount) + channel];
                }
            }
        }

        [BurstCompile]
        unsafe struct PCM24PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public byte* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int chunk)
            {
                int start = chunk * planarChunkSize;
                int end = Math.Min(start + planarChunkSize, sampleCount);

                if (channelCount == 2)
                {
                    byte* frame = source + ((long)start * 2 * 3);
                    for (int i = start; i < end; i++, frame += 2 * 3)
                    {
                        output[i] = Convert(frame);
                        output[sampleCount + i] = Convert(frame + 3);
                    }

                    return;
                }

                for (int channel = 0; channel < channelCount; channel++)
                {
                    int offset = channel * sampleCount;
                    for (int i = start; i < end; i++)
                        output[offset + i] = Convert(source + (((i * channelCount) + channel) * 3));
                }
            }

            // 리틀 엔디언 24비트 샘플의 상위 2바이트만 사용합니다.
            static short Convert(byte* bytes) => (short)(bytes[1] | (bytes[2] << 8));
        }

        [BurstCompile]
        unsafe struct PCM32PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public int* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int chunk)
            {
                int start = chunk * planarChunkSize;
                int end = Math.Min(start + planarChunkSize, sampleCount);

                if (channelCount == 2)
                {
                    int* frame = source + ((long)start * 2);
                    for (int i = start; i < end; i++, frame += 2)
                    {
                        output[i] = Convert(frame[0]);
                        output[sampleCount + i] = Convert(frame[1]);
                    }

                    return;
                }

                for (int channel = 0; channel < channelCount; channel++)
                {
                    int offset = channel * sampleCount;
                    for (int i = start; i < end; i++)
                        output[offset + i] = Convert(source[(i * channelCount) + channel]);
                }
            }

            static short Convert(int value) => (short)(value >> 16);
        }

        [BurstCompile]
        unsafe struct FloatPlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public float* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int chunk)
            {
                int start = chunk * planarChunkSize;
                int end = Math.Min(start + planarChunkSize, sampleCount);

                if (channelCount == 2)
                {
                    float* frame = source + ((long)start * 2);
                    for (int i = start; i < end; i++, frame += 2)
                    {
                        output[i] = Convert(frame[0]);
                        output[sampleCount + i] = Convert(frame[1]);
                    }

                    return;
                }

                for (int channel = 0; channel < channelCount; channel++)
                {
                    int offset = channel * sampleCount;
                    for (int i = start; i < end; i++)
                        output[offset + i] = Convert(source[(i * channelCount) + channel]);
                }
            }

            // float 샘플은 ±1을 넘을 수 있으므로 short로 변환하기 전에 범위를 제한합니다.
            static short Convert(float value)
            {
                value *= 32768f;
                return (short)(value >= short.MaxValue ? short.MaxValue : value <= short.MinValue ? short.MinValue : value);
            }
        }
    }
}
