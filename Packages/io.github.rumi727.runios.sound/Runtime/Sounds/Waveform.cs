#nullable enable
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Holds 16-bit planar samples and a min/max pyramid built from them, and reduces any sample range to waveform columns.<br/>
    /// 16비트 planar 샘플과 그로부터 만든 min/max 피라미드를 보관하고, 임의의 샘플 구간을 파형 열로 축약합니다.
    /// </summary>
    /// <remarks>
    /// Must be created, used, and disposed on the main thread. <see cref="Dispose"/> waits for jobs scheduled by the Schedule methods before releasing memory.<br/>
    /// 메인 스레드에서 생성, 사용, 해제해야 합니다. <see cref="Dispose"/>는 메모리를 해제하기 전에 Schedule 메서드들로 스케줄한 잡을 기다립니다.
    /// </remarks>
    public sealed partial class Waveform : IDisposable
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

            // 열이 짧을 때만 묶습니다. MinMaxJob이 원본을 순서대로 훑는 기준과 같습니다.
            double scale = (endSample - startSample) / width;
            int columnsPerChunk = scale <= branch * 2 ? minMaxChunkSize : 1;
            int chunksPerChannel = ((width + columnsPerChunk) - 1) / columnsPerChunk;

            JobHandle handle = new MinMaxJob
            {
                samples = samples,
                levels = levels,
                levelInfos = levelInfos,
                result = result,
                sampleCount = sampleCount,
                width = width,
                columnsPerChunk = columnsPerChunk,
                chunksPerChannel = chunksPerChannel,
                startSample = startSample,
                scale = scale
            }.Schedule(chunksPerChannel * channelCount, columnsPerChunk == 1 ? 16 : 1, dependency);

            pendingJobs = JobHandle.CombineDependencies(pendingJobs, handle);
            return handle;
        }

        /// <summary>
        /// Schedules a job that computes an approximate minimum and maximum of each column, cheaper than <see cref="ScheduleMinMax"/> when columns are long.<br/>
        /// 열이 길 때 <see cref="ScheduleMinMax"/>보다 싸게, 각 열의 최솟값과 최댓값을 근사로 계산하는 잡을 스케줄합니다.
        /// </summary>
        /// <remarks>
        /// When a column covers 16 samples or more, each column takes the minimum and maximum of the samples in the pyramid nodes that overlap it, at the coarsest level whose node is no longer than a column.
        /// So the range of each column is widened outward by less than one column on each side, and the interpolated values at the column edges are not included.
        /// Otherwise the result is the same as <see cref="ScheduleMinMax"/>. The arguments, the result layout, and the exceptions follow <see cref="ScheduleMinMax"/>.<br/>
        /// 열 하나가 샘플 16개 이상을 덮으면, 노드가 열보다 길지 않은 가장 거친 레벨에서 열과 겹치는 피라미드 노드들에 든 샘플의 최솟값과 최댓값을 구합니다.
        /// 따라서 각 열의 구간은 양쪽으로 열 하나보다 짧은 만큼 넓어지고, 열 경계의 보간 값은 포함되지 않습니다.
        /// 그렇지 않으면 <see cref="ScheduleMinMax"/>와 같은 결과입니다. 인자, 결과 배치, 예외는 <see cref="ScheduleMinMax"/>를 따릅니다.
        /// </remarks>
        /// <param name="startSample"></param>
        /// <param name="endSample"></param>
        /// <param name="width"></param>
        /// <param name="result"></param>
        /// <param name="dependency"></param>
        /// <exception cref="ObjectDisposedException">This waveform has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is negative, or the range does not satisfy the contract.</exception>
        /// <exception cref="ArgumentException"><paramref name="result"/> has fewer than <c>channelCount * width</c> elements.</exception>
        public JobHandle ScheduleApproximateMinMax(double startSample, double endSample, int width, NativeArray<float2> result, JobHandle dependency = default)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            ArgumentOutOfRangeException.ThrowIfNegative(width);

            // 부정 비교로 쓴 것은 NaN도 거부하기 위해서입니다.
            if (sampleCount > 0 && !(startSample >= 0 && startSample <= endSample && endSample <= sampleCount - 1))
                throw new ArgumentOutOfRangeException(nameof(startSample), $"The range [{startSample}, {endSample}] must satisfy 0 <= startSample <= endSample <= {sampleCount - 1}.");

            int columnCount = checked(channelCount * width);
            if (!result.IsCreated || result.Length < columnCount)
                throw new ArgumentException($"The result array must have at least {columnCount} elements (channelCount * width).", nameof(result));

            // 열이 가장 작은 노드(16샘플)보다 짧으면 노드에 맞출 수 없으므로 정확한 축약을 씁니다. 이때 읽을 샘플이 적어 정확한 축약도 쌉니다.
            double scale = (endSample - startSample) / width;
            if (sampleCount == 0 || levelInfos.Length == 0 || !(scale >= branch))
                return ScheduleMinMax(startSample, endSample, width, result, dependency);

            // 노드가 열보다 길지 않은 가장 거친 레벨을 고릅니다.
            int level = 0;
            int nodeSize = branch;
            while (level + 1 < levelInfos.Length && (long)nodeSize * branch <= scale)
            {
                level++;
                nodeSize *= branch;
            }

            JobHandle handle = new ApproximateMinMaxJob
            {
                levels = levels,
                result = result,
                width = width,
                startSample = startSample,
                scale = scale,
                levelOffset = levelInfos[level].x,
                levelCount = levelInfos[level].y,
                nodeSize = nodeSize
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
