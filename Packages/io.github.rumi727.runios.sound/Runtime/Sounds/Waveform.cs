#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Holds 16-bit planar samples and a min/max pyramid built from them, and reduces any sample range to waveform columns.<br/>
    /// 16비트 planar 샘플과 그로부터 만든 min/max 피라미드를 보관하고, 임의의 샘플 구간을 파형 열로 축약합니다.
    /// </summary>
    /// <remarks>
    /// Must be created on the main thread. Jobs scheduled by <see cref="ScheduleMinMax"/> must complete before <see cref="Dispose"/>.<br/>
    /// 메인 스레드에서 생성해야 합니다. <see cref="ScheduleMinMax"/>로 스케줄한 잡은 <see cref="Dispose"/> 전에 완료되어야 합니다.
    /// </remarks>
    public sealed class Waveform : IDisposable
    {
        const int branch = 16;

        NativeArray<short> samples;

        // 레벨 l의 원소 하나는 원본 샘플 16^(l+1)개의 (min, max) 짝이며, [offset + 2 * (channel * count + i)]에 저장됩니다.
        NativeArray<short> levels;

        // 레벨마다 levels 안의 (offset, count)입니다.
        NativeArray<int2> levelInfos;

        public int channelCount { get; }
        public int sampleCount { get; }

        /// <summary>
        /// Takes ownership of <paramref name="planarSamples"/> and builds the min/max pyramid.<br/>
        /// <paramref name="planarSamples"/>의 소유권을 가져오고 min/max 피라미드를 만듭니다.
        /// </summary>
        /// <param name="planarSamples">
        /// Samples laid out per channel as <c>[channel * sampleCount + sample]</c>.<br/>
        /// <c>[channel * sampleCount + sample]</c>로 채널별 배치된 샘플입니다.
        /// </param>
        /// <param name="channelCount"></param>
        public Waveform(NativeArray<short> planarSamples, int channelCount)
        {
            samples = planarSamples;
            this.channelCount = channelCount;
            sampleCount = planarSamples.Length / channelCount;

            List<int2> infos = [];
            int count = sampleCount;
            int length = 0;
            while (count >= branch)
            {
                count = ((count + branch) - 1) / branch;
                infos.Add(new int2(length, count));
                length = checked(length + (count * channelCount * 2));
            }

            levelInfos = new NativeArray<int2>(infos.Count, Allocator.Persistent);
            for (int i = 0; i < infos.Count; i++)
                levelInfos[i] = infos[i];

            levels = new NativeArray<short>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // 잡이 levels에 쓰는 동안에는 서브 배열을 만들 수 없으므로 레벨별 구간을 먼저 만들어 둡니다.
            NativeArray<short>[] views = new NativeArray<short>[infos.Count];
            for (int level = 0; level < infos.Count; level++)
                views[level] = levels.GetSubArray(infos[level].x, infos[level].y * channelCount * 2);

            JobHandle handle = default;
            for (int level = 0; level < infos.Count; level++)
            {
                int levelCount = infos[level].y;
                int chunksPerChannel = ((levelCount + buildChunkSize) - 1) / buildChunkSize;
                if (level == 0)
                {
                    handle = new BuildFromSamplesJob
                    {
                        source = samples,
                        output = views[level],
                        sourceCount = sampleCount,
                        count = levelCount,
                        chunksPerChannel = chunksPerChannel
                    }.Schedule(chunksPerChannel * channelCount, 16, handle);
                }
                else
                {
                    handle = new BuildFromLevelJob
                    {
                        source = views[level - 1],
                        output = views[level],
                        sourceCount = infos[level - 1].y,
                        count = levelCount,
                        chunksPerChannel = chunksPerChannel
                    }.Schedule(chunksPerChannel * channelCount, 16, handle);
                }
            }

            handle.Complete();
        }

        /// <summary>
        /// Schedules a job that computes the minimum and maximum of each column when the range from <paramref name="startSample"/> to <paramref name="endSample"/> is spread over <paramref name="width"/> columns.<br/>
        /// <paramref name="startSample"/>부터 <paramref name="endSample"/>까지의 구간을 <paramref name="width"/>개의 열로 펼쳤을 때 각 열의 최솟값과 최댓값을 계산하는 잡을 스케줄합니다.
        /// </summary>
        /// <remarks>
        /// The range must satisfy <c>0 &lt;= startSample &lt;= endSample &lt;= sampleCount - 1</c>. Values are normalized to -1..1.<br/>
        /// 구간은 <c>0 &lt;= startSample &lt;= endSample &lt;= sampleCount - 1</c>을 만족해야 합니다. 값은 -1..1로 정규화됩니다.
        /// </remarks>
        /// <param name="width"></param>
        /// <param name="result">
        /// Receives <c>(min, max)</c> at <c>[channel * width + x]</c>. Columns with no samples receive a minimum greater than the maximum.<br/>
        /// <c>[channel * width + x]</c>에 <c>(min, max)</c>를 받습니다. 샘플이 없는 열은 최솟값이 최댓값보다 큽니다.
        /// </param>
        /// <param name="startSample"></param>
        /// <param name="endSample"></param>
        /// <param name="dependency"></param>
        public JobHandle ScheduleMinMax(double startSample, double endSample, int width, NativeArray<float2> result, JobHandle dependency = default)
        {
            return new MinMaxJob
            {
                samples = samples,
                levels = levels,
                levelInfos = levelInfos,
                result = result,
                sampleCount = sampleCount,
                width = width,
                startSample = startSample,
                scale = (endSample - startSample) / width
            }.Schedule(channelCount * width, 16, dependency);
        }

        public void Dispose()
        {
            samples.Dispose();
            levels.Dispose();
            levelInfos.Dispose();
        }

        /*
         * 피라미드 생성 잡은 Execute 하나가 한 채널 안의 원소 buildChunkSize개를 맡습니다.
         * 원소 하나(16개 비교)마다 Execute를 부르면 채널을 구하는 나눗셈, 런타임 길이 루프의 진입 비용, 벡터 수평 축약이 실제 비교보다 비싸므로
         * 나눗셈과 진입 비용을 묶음당 한 번으로 줄입니다. 꽉 찬 블록은 반복 횟수가 상수인 루프로 처리해 Burst가 펼쳐서 벡터화하게 하고,
         * 채널 끝의 남은 샘플만 다루는 마지막 블록만 일반 루프로 처리합니다.
         */
        const int buildChunkSize = 64;

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

                float startValue = Sample(offset, start);
                float endValue = Sample(offset, end);

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

            float Sample(int offset, double position)
            {
                int previous = (int)position;
                if (previous >= sampleCount - 1)
                    return samples[(offset + sampleCount) - 1] / 32768f;

                float t = (float)(position - previous);
                return math.lerp(samples[offset + previous] / 32768f, samples[offset + previous + 1] / 32768f, t);
            }
        }
    }
}
