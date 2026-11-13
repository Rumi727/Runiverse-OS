#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        /*
         * MinMaxJob은 Execute 하나가 한 채널 안의 연속한 열 columnsPerChunk개를 맡습니다.
         * 열이 짧으면 열마다의 진입 비용(채널 나눗셈, 양 끝 보간의 띄엄띄엄 읽기, 축약 진입)이 실제로 읽는 샘플보다 비싸므로 minMaxChunkSize개씩 묶어 묶음당 한 번으로 줄입니다.
         * 짧은 열은 묶음 구간의 원본을 한 번 순서대로 훑으며 열 경계마다 결과를 쓰고, 이웃한 두 열이 함께 쓰는 경계 보간 값은 한 번만 계산합니다.
         * 긴 열은 열 하나의 작업이 진입 비용보다 훨씬 커서 묶어도 이득이 없고 작업 단위만 줄어 스레드 사이의 분배가 나빠지므로 묶지 않습니다.
         */
        const int minMaxChunkSize = 64;

        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct MinMaxJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> samples;
            [ReadOnly] public NativeArray<short> levels;
            [ReadOnly] public NativeArray<int2> levelInfos;
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float2> result;
            public int sampleCount;
            public int width;
            public int columnsPerChunk;
            public int chunksPerChannel;
            public double startSample;
            public double scale;

            public void Execute(int chunk)
            {
                int channel = chunk / chunksPerChannel;
                int firstColumn = (chunk - (channel * chunksPerChannel)) * columnsPerChunk;
                int lastColumn = math.min(firstColumn + columnsPerChunk, width);
                int resultOffset = channel * width;

                if (sampleCount == 0)
                {
                    for (int x = firstColumn; x < lastColumn; x++)
                        result[resultOffset + x] = new float2(float.MaxValue, float.MinValue);

                    return;
                }

                int offset = channel * sampleCount;

                // 샘플 사이를 직선으로 이은 파형에서 열 x가 덮는 [start, end] 구간의 최솟값과 최댓값을 구합니다.
                // 열 x의 start는 열 x - 1의 end와 같은 식이므로 이어서 써도 열마다 따로 계산한 값과 같습니다.
                double start = startSample + (firstColumn * scale);
                float startValue = Sample(samples, sampleCount, offset, start);

                // 축약이 원본을 직접 훑는 길이(branch * 2) 이하의 열은 피라미드를 쓰지 않으므로 원본을 한 번 순서대로 훑습니다.
                bool sequential = scale <= branch * 2;
                int next = (int)start + 1;

                for (int x = firstColumn; x < lastColumn; x++)
                {
                    double end = startSample + ((x + 1) * scale);
                    float endValue = Sample(samples, sampleCount, offset, end);

                    float min = math.min(startValue, endValue);
                    float max = math.max(startValue, endValue);

                    int first = (int)start + 1;
                    int last = math.min((int)end, sampleCount - 1);
                    if (sequential)
                    {
                        int innerMin = int.MaxValue;
                        int innerMax = int.MinValue;
                        for (; next <= last; next++)
                        {
                            int value = samples[offset + next];
                            innerMin = math.min(innerMin, value);
                            innerMax = math.max(innerMax, value);
                        }

                        if (innerMin <= innerMax)
                        {
                            min = math.min(min, innerMin / 32768f);
                            max = math.max(max, innerMax / 32768f);
                        }
                    }
                    else if (first <= last)
                    {
                        Reduce(channel, first, last + 1, out int innerMin, out int innerMax);
                        min = math.min(min, innerMin / 32768f);
                        max = math.max(max, innerMax / 32768f);
                    }

                    result[resultOffset + x] = new float2(min, max);

                    start = end;
                    startValue = endValue;
                }
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
