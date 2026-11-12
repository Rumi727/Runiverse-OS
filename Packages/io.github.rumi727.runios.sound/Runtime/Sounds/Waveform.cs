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
    /// Must be created, used, and disposed on the main thread. <see cref="Dispose"/> waits for jobs scheduled by <see cref="ScheduleMinMax"/> and <see cref="ScheduleResample"/> before releasing memory.<br/>
    /// 메인 스레드에서 생성, 사용, 해제해야 합니다. <see cref="Dispose"/>는 메모리를 해제하기 전에 <see cref="ScheduleMinMax"/>와 <see cref="ScheduleResample"/>로 스케줄한 잡을 기다립니다.
    /// </remarks>
    public sealed class Waveform : IDisposable
    {
        const int branch = 16;

        NativeArray<short> samples;

        // 레벨 l의 원소 하나는 원본 샘플 16^(l+1)개의 (min, max) 짝이며, [offset + 2 * (channel * count + i)]에 저장됩니다.
        NativeArray<short> levels;

        // 레벨마다 levels 안의 (offset, count)입니다.
        NativeArray<int2> levelInfos;

        // Dispose가 해제 전에 기다릴, 이 파형의 배열을 사용하는 잡들입니다.
        JobHandle pendingJobs;
        bool isDisposed;

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
        /// <remarks>
        /// Ownership is taken even when the constructor throws, so <paramref name="planarSamples"/> is disposed in that case.<br/>
        /// 생성자가 예외를 던져도 소유권은 넘어오므로, 그 경우 <paramref name="planarSamples"/>는 해제됩니다.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="planarSamples"/> is not created, or its length is not a multiple of <paramref name="channelCount"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="channelCount"/> is less than or equal to 0.</exception>
        public Waveform(NativeArray<short> planarSamples, int channelCount)
        {
            samples = planarSamples;
            try
            {
                if (!planarSamples.IsCreated)
                    throw new ArgumentException("The planar samples are not created.", nameof(planarSamples));

                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelCount);

                if (planarSamples.Length % channelCount != 0)
                    throw new ArgumentException($"The planar sample length {planarSamples.Length} is not a multiple of the channel count {channelCount}.", nameof(planarSamples));

                this.channelCount = channelCount;
                sampleCount = planarSamples.Length / channelCount;

                BuildLevels();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        void BuildLevels()
        {
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

            // 스케줄 도중 실패해도 Dispose가 이미 스케줄된 잡을 기다리도록 체인을 pendingJobs에 둡니다.
            for (int level = 0; level < infos.Count; level++)
            {
                int levelCount = infos[level].y;
                int chunksPerChannel = ((levelCount + buildChunkSize) - 1) / buildChunkSize;
                if (level == 0)
                {
                    pendingJobs = new BuildFromSamplesJob
                    {
                        source = samples,
                        output = views[level],
                        sourceCount = sampleCount,
                        count = levelCount,
                        chunksPerChannel = chunksPerChannel
                    }.Schedule(chunksPerChannel * channelCount, 16, pendingJobs);
                }
                else
                {
                    pendingJobs = new BuildFromLevelJob
                    {
                        source = views[level - 1],
                        output = views[level],
                        sourceCount = infos[level - 1].y,
                        count = levelCount,
                        chunksPerChannel = chunksPerChannel
                    }.Schedule(chunksPerChannel * channelCount, 16, pendingJobs);
                }
            }

            pendingJobs.Complete();
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
        /// <exception cref="ObjectDisposedException">This waveform has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is negative, or the range does not satisfy the contract.</exception>
        /// <exception cref="ArgumentException"><paramref name="result"/> has fewer than <c>channelCount * width</c> elements.</exception>
        public JobHandle ScheduleMinMax(double startSample, double endSample, int width, NativeArray<float2> result, JobHandle dependency = default)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            ArgumentOutOfRangeException.ThrowIfNegative(width);

            // 샘플이 없으면 잡이 원본을 읽지 않고 모든 열을 빈 열로 채우므로 구간을 검사하지 않습니다.
            // 부정 비교로 쓴 것은 NaN도 거부하기 위해서입니다.
            if (sampleCount > 0 && !(startSample >= 0 && startSample <= endSample && endSample <= sampleCount - 1))
                throw new ArgumentOutOfRangeException(nameof(startSample), $"The range [{startSample}, {endSample}] must satisfy 0 <= startSample <= endSample <= {sampleCount - 1}.");

            int columnCount = checked(channelCount * width);
            if (!result.IsCreated || result.Length < columnCount)
                throw new ArgumentException($"The result array must have at least {columnCount} elements (channelCount * width).", nameof(result));

            JobHandle handle = new MinMaxJob
            {
                samples = samples,
                levels = levels,
                levelInfos = levelInfos,
                result = result,
                sampleCount = sampleCount,
                width = width,
                startSample = startSample,
                scale = (endSample - startSample) / width
            }.Schedule(columnCount, 16, dependency);

            pendingJobs = JobHandle.CombineDependencies(pendingJobs, handle);
            return handle;
        }

        /// <summary>
        /// Schedules a job that samples the waveform at <paramref name="count"/> evenly spaced positions from <paramref name="startSample"/> to <paramref name="endSample"/>, both inclusive.<br/>
        /// <paramref name="startSample"/>부터 <paramref name="endSample"/>까지 양 끝을 포함해 균등한 간격의 <paramref name="count"/>개 위치에서 파형을 샘플링하는 잡을 스케줄합니다.
        /// </summary>
        /// <remarks>
        /// Values between samples are linearly interpolated, the same way <see cref="ScheduleMinMax"/> treats column edges, so sampling <c>width + 1</c> positions over the same range gives the value at each column edge.
        /// When <paramref name="count"/> is 1, the only position is <paramref name="startSample"/>.
        /// The range must satisfy <c>0 &lt;= startSample &lt;= endSample &lt;= sampleCount - 1</c>. Values are normalized to -1..1.<br/>
        /// 샘플 사이의 값은 <see cref="ScheduleMinMax"/>가 열 경계를 다루는 것과 같은 방식으로 선형 보간하므로, 같은 구간을 <c>width + 1</c>개 위치로 샘플링하면 각 열 경계의 값이 됩니다.
        /// <paramref name="count"/>가 1이면 위치는 <paramref name="startSample"/> 하나입니다.
        /// 구간은 <c>0 &lt;= startSample &lt;= endSample &lt;= sampleCount - 1</c>을 만족해야 합니다. 값은 -1..1로 정규화됩니다.
        /// </remarks>
        /// <param name="count"></param>
        /// <param name="result">
        /// Receives the value at <c>[channel * count + i]</c>. Receives 0 when there are no samples.<br/>
        /// <c>[channel * count + i]</c>에 값을 받습니다. 샘플이 없으면 0을 받습니다.
        /// </param>
        /// <param name="startSample"></param>
        /// <param name="endSample"></param>
        /// <param name="dependency"></param>
        /// <exception cref="ObjectDisposedException">This waveform has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative, or the range does not satisfy the contract.</exception>
        /// <exception cref="ArgumentException"><paramref name="result"/> has fewer than <c>channelCount * count</c> elements.</exception>
        public JobHandle ScheduleResample(double startSample, double endSample, int count, NativeArray<float> result, JobHandle dependency = default)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            // 샘플이 없으면 잡이 원본을 읽지 않고 0을 채우므로 구간을 검사하지 않습니다.
            // 부정 비교로 쓴 것은 NaN도 거부하기 위해서입니다.
            if (sampleCount > 0 && !(startSample >= 0 && startSample <= endSample && endSample <= sampleCount - 1))
                throw new ArgumentOutOfRangeException(nameof(startSample), $"The range [{startSample}, {endSample}] must satisfy 0 <= startSample <= endSample <= {sampleCount - 1}.");

            int pointCount = checked(channelCount * count);
            if (!result.IsCreated || result.Length < pointCount)
                throw new ArgumentException($"The result array must have at least {pointCount} elements (channelCount * count).", nameof(result));

            JobHandle handle = new ResampleJob
            {
                samples = samples,
                result = result,
                sampleCount = sampleCount,
                count = count,
                startSample = startSample,
                scale = count > 1 ? (endSample - startSample) / (count - 1) : 0
            }.Schedule(pointCount, 64, dependency);

            pendingJobs = JobHandle.CombineDependencies(pendingJobs, handle);
            return handle;
        }

        public void Dispose()
        {
            // 잡이 읽는 중인 배열을 해제하지 않도록 먼저 기다립니다. 생성 도중 실패했을 때는 만들어진 배열만 해제합니다.
            pendingJobs.Complete();

            if (samples.IsCreated)
                samples.Dispose();

            if (levels.IsCreated)
                levels.Dispose();

            if (levelInfos.IsCreated)
                levelInfos.Dispose();

            isDisposed = true;
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

        // MinMaxJob의 열 경계와 ResampleJob이 같은 값을 내도록 두 잡이 이 보간을 함께 씁니다.
        static float Sample(NativeArray<short> samples, int sampleCount, int offset, double position)
        {
            int previous = (int)position;
            if (previous >= sampleCount - 1)
                return samples[(offset + sampleCount) - 1] / 32768f;

            float t = (float)(position - previous);
            return math.lerp(samples[offset + previous] / 32768f, samples[offset + previous + 1] / 32768f, t);
        }
    }
}
