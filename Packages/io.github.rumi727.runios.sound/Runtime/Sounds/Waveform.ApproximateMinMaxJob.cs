#nullable enable
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RuniOS.Sounds
{
    public sealed partial class Waveform
    {
        // 열마다 한 레벨에서 열과 겹치는 노드들만 합칩니다. 노드가 열보다 길지 않으므로 열당 노드 1~17개만 읽습니다.
        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct ApproximateMinMaxJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<short> levels;
            [WriteOnly] public NativeArray<float2> result;
            public int width;
            public double startSample;
            public double scale;

            // 고른 레벨의 levels 안 시작 위치, 원소 개수, 원소 하나가 덮는 원본 샘플 수입니다.
            public int levelOffset;
            public int levelCount;
            public int nodeSize;

            public void Execute(int index)
            {
                int channel = index / width;
                int x = index - (channel * width);

                double start = startSample + (x * scale);
                double end = startSample + ((x + 1) * scale);

                int first = (int)start / nodeSize;
                int last = math.min((int)end / nodeSize, levelCount - 1);
                int baseIndex = levelOffset + (channel * levelCount * 2);

                int min = int.MaxValue;
                int max = int.MinValue;
                for (int i = first; i <= last; i++)
                {
                    min = math.min(min, levels[baseIndex + (i * 2)]);
                    max = math.max(max, levels[baseIndex + (i * 2) + 1]);
                }

                result[index] = new float2(min / 32768f, max / 32768f);
            }
        }
    }
}
