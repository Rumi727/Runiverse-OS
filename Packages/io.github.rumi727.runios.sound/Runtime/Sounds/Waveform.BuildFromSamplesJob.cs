#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        [BurstCompile]
        struct BuildFromSamplesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> source;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<short> output;
            public int sourceCount;
            public int count;
            public int chunksPerChannel;

            public void Execute(int chunk)
            {
                int channel = chunk / chunksPerChannel;
                int first = (chunk - (channel * chunksPerChannel)) * buildChunkSize;
                int last = math.min(first + buildChunkSize, count);
                int sourceOffset = channel * sourceCount;
                int outputOffset = channel * count;

                for (int i = first; i < last; i++)
                {
                    int begin = i * branch;
                    int offset = sourceOffset + begin;

                    int min = int.MaxValue;
                    int max = int.MinValue;
                    if (begin + branch <= sourceCount)
                    {
                        for (int j = 0; j < branch; j++)
                        {
                            int value = source[offset + j];
                            min = math.min(min, value);
                            max = math.max(max, value);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < sourceCount - begin; j++)
                        {
                            int value = source[offset + j];
                            min = math.min(min, value);
                            max = math.max(max, value);
                        }
                    }

                    // ReSharper disable IntVariableOverflowInUncheckedContext
                    output[(outputOffset + i) * 2] = (short)min;
                    output[((outputOffset + i) * 2) + 1] = (short)max;
                    // ReSharper restore IntVariableOverflowInUncheckedContext
                }
            }
        }
    }
}
