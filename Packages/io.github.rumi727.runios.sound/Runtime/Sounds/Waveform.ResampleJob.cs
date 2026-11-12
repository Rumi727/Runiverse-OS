#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct ResampleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> samples;
            [WriteOnly] public NativeArray<float> result;
            public int sampleCount;
            public int count;
            public double startSample;
            public double scale;

            public void Execute(int index)
            {
                if (sampleCount == 0)
                {
                    result[index] = 0;
                    return;
                }

                int channel = index / count;
                int i = index - (channel * count);

                result[index] = Sample(samples, sampleCount, channel * sampleCount, startSample + (i * scale));
            }
        }
    }
}
