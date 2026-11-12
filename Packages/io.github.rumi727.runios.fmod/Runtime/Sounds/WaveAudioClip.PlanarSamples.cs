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
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                byte* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = (short)(unchecked((sbyte)frame[channel]) << 8);
            }
        }

        [BurstCompile]
        unsafe struct PCM16PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public short* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                short* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = frame[channel];
            }
        }

        [BurstCompile]
        unsafe struct PCM24PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public byte* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                byte* frame = source + (sample * channelCount * 3);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    // 리틀 엔디언 24비트 샘플의 상위 2바이트만 사용합니다.
                    byte* bytes = frame + (channel * 3);
                    output[(channel * sampleCount) + sample] = (short)(bytes[1] | (bytes[2] << 8));
                }
            }
        }

        [BurstCompile]
        unsafe struct PCM32PlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public int* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                int* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                    output[(channel * sampleCount) + sample] = (short)(frame[channel] >> 16);
            }
        }

        [BurstCompile]
        unsafe struct FloatPlanarJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public float* source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sampleCount;
            public int channelCount;

            public void Execute(int sample)
            {
                float* frame = source + (sample * channelCount);
                for (int channel = 0; channel < channelCount; channel++)
                {
                    // float 샘플은 ±1을 넘을 수 있으므로 short로 변환하기 전에 범위를 제한합니다.
                    float value = frame[channel] * 32768f;
                    output[(channel * sampleCount) + sample] = (short)(value >= short.MaxValue ? short.MaxValue : value <= short.MinValue ? short.MinValue : value);
                }
            }
        }
    }
}
