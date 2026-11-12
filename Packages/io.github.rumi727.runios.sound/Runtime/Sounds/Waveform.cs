#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public static class Waveform
    {
        /// <summary>
        /// Schedules a job that computes the minimum and maximum of each column of the waveform.<br/>
        /// 파형의 각 열의 최솟값과 최댓값을 계산하는 잡을 스케줄합니다.
        /// </summary>
        /// <param name="planarSamples">
        /// Samples laid out per channel as <c>[channel * sampleCount + sample]</c>.<br/>
        /// <c>[channel * sampleCount + sample]</c>로 채널별 배치된 샘플입니다.
        /// </param>
        /// <param name="result">
        /// Receives <c>(min, max)</c> at <c>[channel * width + x]</c>. Columns with no samples receive a minimum greater than the maximum.<br/>
        /// <c>[channel * width + x]</c>에 <c>(min, max)</c>를 받습니다. 샘플이 없는 열은 최솟값이 최댓값보다 큽니다.
        /// </param>
        public static JobHandle ScheduleMinMax(NativeArray<float> planarSamples, int channelCount, int width, NativeArray<float2> result, JobHandle dependency = default)
        {
            return new MinMaxJob
            {
                samples = planarSamples,
                result = result,
                sampleCount = planarSamples.Length / channelCount,
                width = width
            }.Schedule(channelCount * width, 16, dependency);
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct MinMaxJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> samples;
            [WriteOnly] public NativeArray<float2> result;
            public int sampleCount;
            public int width;

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
                double scale = (double)(sampleCount - 1) / width;
                double start = x * scale;
                double end = (x + 1) * scale;

                float startValue = Sample(offset, start);
                float endValue = Sample(offset, end);

                float min = math.min(startValue, endValue);
                float max = math.max(startValue, endValue);

                int first = (int)start + 1;
                int count = math.min((int)end, sampleCount - 1) - first + 1;
                if (count > 0)
                {
                    NativeArray<float> range = samples.GetSubArray(offset + first, count);

                    // 스칼라 min/max 누적은 Burst가 리덕션으로 인식하지 못해 자동 벡터화되지 않으므로 float4로 직접 누적합니다.
                    int vectorCount = count >> 2;
                    if (vectorCount > 0)
                    {
                        NativeArray<float4> vectors = range.GetSubArray(0, vectorCount << 2).Reinterpret<float4>(sizeof(float));
                        float4 min4 = min;
                        float4 max4 = max;
                        for (int i = 0; i < vectorCount; i++)
                        {
                            float4 value = vectors[i];
                            min4 = math.min(min4, value);
                            max4 = math.max(max4, value);
                        }

                        min = math.cmin(min4);
                        max = math.cmax(max4);
                    }

                    for (int i = vectorCount << 2; i < count; i++)
                    {
                        float value = range[i];
                        min = math.min(min, value);
                        max = math.max(max, value);
                    }
                }

                result[index] = new float2(min, max);
            }

            float Sample(int offset, double position)
            {
                int previous = (int)position;
                if (previous >= sampleCount - 1)
                    return samples[offset + sampleCount - 1];

                float t = (float)(position - previous);
                return math.lerp(samples[offset + previous], samples[offset + previous + 1], t);
            }
        }
    }
}
