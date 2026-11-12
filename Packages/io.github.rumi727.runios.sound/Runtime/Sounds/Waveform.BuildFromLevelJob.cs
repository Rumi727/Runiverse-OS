#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        [BurstCompile]
        struct BuildFromLevelJob : IJobParallelFor
        {
            // 같은 levels 배열에서 이전 레벨 구간을 읽고 다음 레벨 구간에 씁니다. 두 구간은 겹치지 않습니다.
            [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<short> source;
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
                    int offset = (sourceOffset + begin) * 2;

                    int min = int.MaxValue;
                    int max = int.MinValue;
                    if (begin + branch <= sourceCount)
                    {
                        for (int j = 0; j < branch; j++)
                        {
                            min = math.min(min, source[offset + (j * 2)]);
                            max = math.max(max, source[offset + (j * 2) + 1]);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < sourceCount - begin; j++)
                        {
                            min = math.min(min, source[offset + (j * 2)]);
                            max = math.max(max, source[offset + (j * 2) + 1]);
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
