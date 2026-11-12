#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct MinMaxJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> samples;
            [ReadOnly] public NativeArray<short> levels;
            [ReadOnly] public NativeArray<int2> levelInfos;
            [WriteOnly] public NativeArray<float2> result;
            public int sampleCount;
            public int width;
            public double startSample;
            public double scale;

            public void Execute(int index)
            {
                if (sampleCount == 0)
                {
                    result[index] = new float2(float.MaxValue, float.MinValue);
                    return;
                }

                int channel = index / width;
                int x = index - (channel * width);
                int offset = channel * sampleCount;

                // 샘플 사이를 직선으로 이은 파형에서 열 x가 덮는 [start, end] 구간의 최솟값과 최댓값을 구합니다.
                double start = startSample + (x * scale);
                double end = startSample + ((x + 1) * scale);

                float startValue = Sample(samples, sampleCount, offset, start);
                float endValue = Sample(samples, sampleCount, offset, end);

                float min = math.min(startValue, endValue);
                float max = math.max(startValue, endValue);

                int first = (int)start + 1;
                int last = math.min((int)end, sampleCount - 1);
                if (first <= last)
                {
                    Reduce(channel, first, last + 1, out int innerMin, out int innerMax);
                    min = math.min(min, innerMin / 32768f);
                    max = math.max(max, innerMax / 32768f);
                }

                result[index] = new float2(min, max);
            }

            // 원본 샘플 [lo, hi)를 레벨마다 정렬되지 않은 양 끝만 소비하고 나머지는 상위 레벨로 올려 보내며 축약합니다.
            void Reduce(int channel, int lo, int hi, out int min, out int max)
            {
                min = int.MaxValue;
                max = int.MinValue;

                int level = -1;
                int count = sampleCount;
                int baseIndex = channel * sampleCount;

                while (level != levelInfos.Length - 1 && hi - lo > branch * 2)
                {
                    for (; lo % branch != 0; lo++)
                        Take(level, baseIndex, lo, ref min, ref max);

                    for (; hi % branch != 0 && hi != count; hi--)
                        Take(level, baseIndex, hi - 1, ref min, ref max);

                    int2 next = levelInfos[level + 1];
                    lo /= branch;
                    hi = hi == count ? next.y : hi / branch;

                    level++;
                    count = next.y;
                    baseIndex = next.x + (channel * count * 2);
                }

                for (int i = lo; i < hi; i++)
                    Take(level, baseIndex, i, ref min, ref max);
            }

            void Take(int level, int baseIndex, int i, ref int min, ref int max)
            {
                if (level < 0)
                {
                    int value = samples[baseIndex + i];
                    min = math.min(min, value);
                    max = math.max(max, value);
                }
                else
                {
                    min = math.min(min, levels[baseIndex + (i * 2)]);
                    max = math.max(max, levels[baseIndex + (i * 2) + 1]);
                }
            }
        }
    }
}
