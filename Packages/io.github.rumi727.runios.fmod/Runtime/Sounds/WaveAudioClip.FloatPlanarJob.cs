#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
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
