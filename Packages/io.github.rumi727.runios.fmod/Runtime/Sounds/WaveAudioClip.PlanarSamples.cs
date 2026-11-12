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
        /// Converts the whole PCM data to normalized floats laid out per channel (<c>[channel * samples + sample]</c>).<br/>
        /// 전체 PCM 데이터를 채널별로 배치한(<c>[channel * samples + sample]</c>) 정규화된 float로 변환합니다.
        /// </summary>
        /// <remarks>
        /// Schedules Burst jobs and waits for them, so it must be called from the main thread. The caller owns the returned array.<br/>
        /// Burst 잡을 스케줄하고 완료까지 기다리므로 메인 스레드에서 호출해야 합니다. 반환된 배열은 호출자가 소유합니다.
        /// </remarks>
        public unsafe NativeArray<float> CreatePlanarSamples(Allocator allocator)
        {
            int sampleCount = checked((int)samples);
            NativeArray<float> output = new NativeArray<float>(checked(sampleCount * channel), allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                GetRawData(0, (uint)output.Length, (source, channelCount) =>
                {
                    fixed (byte* pointer = source)
                    {
                        const int batchCount = 4096;
                        JobHandle handle = pcmFormat!.Value switch
                        {
                            PCMFormat.PCM8 => new PCM8PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(sampleCount, batchCount),
                            PCMFormat.PCM16 => new PCM16PlanarJob { source = (short*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(sampleCount, batchCount),
                            PCMFormat.PCM24 => new PCM24PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(sampleCount, batchCount),
                            PCMFormat.PCM32 => new PCM32PlanarJob { source = (int*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(sampleCount, batchCount),
                            PCMFormat.Float => new FloatPlanarJob { source = (float*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(sampleCount, batchCount),
                            _ => throw new InvalidOperationException($"FMOD returned unsupported sample format {pcmFormat}.")
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

        [BurstCompile]
        unsafe struct PCM8PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public byte* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                byte* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = unchecked((sbyte)frame[channel]) / 128f;
            }
        }

        [BurstCompile]
        unsafe struct PCM16PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public short* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                short* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = frame[channel] / 32768f;
            }
        }

        [BurstCompile]
        unsafe struct PCM24PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public byte* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                byte* frame = source + (sample * channelCount * 3);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    byte* bytes = frame + (channel * 3);

                    // 상위 바이트를 int의 최상위에 두고 산술 시프트해 부호를 확장합니다.
                    int value = ((bytes[0] << 8) | (bytes[1] << 16) | (bytes[2] << 24)) >> 8;
                    output[(channel * sampleCount) + sample] = value / 8388608f;
                }
            }
        }

        [BurstCompile]
        unsafe struct PCM32PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public int* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                int* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = frame[channel] / 2147483648f;
            }
        }

        [BurstCompile]
        unsafe struct FloatPlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public float* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                float* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = frame[channel];
            }
        }
    }
}
