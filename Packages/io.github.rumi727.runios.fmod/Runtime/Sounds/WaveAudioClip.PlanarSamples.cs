#nullable enable
using Unity.Collections;
using Unity.Jobs;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        /// <summary>
        /// Converts the whole PCM data to 16-bit samples laid out per channel (<c>[channel * samples + sample]</c>).<br/>
        /// 전체 PCM 데이터를 채널별로 배치한(<c>[channel * samples + sample]</c>) 16비트 샘플로 변환합니다.
        /// </summary>
        /// <remarks>
        /// Formats other than 16-bit PCM are quantized to 16 bits. Schedules Burst jobs and waits for them, so it must be called from the main thread. The caller owns the returned array.<br/>
        /// 16비트 PCM이 아닌 포맷은 16비트로 양자화됩니다. Burst 잡을 스케줄하고 완료까지 기다리므로 메인 스레드에서 호출해야 합니다. 반환된 배열은 호출자가 소유합니다.
        /// </remarks>
        public unsafe NativeArray<short> CreatePlanarSamples(Allocator allocator)
        {
            int sampleCount = checked((int)samples);
            NativeArray<short> output = new NativeArray<short>(checked(sampleCount * channel), allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                GetRawData(0, (uint)output.Length, (source, channelCount) =>
                {
                    // 잡은 원본을 포인터로 읽으므로, FMOD가 돌려준 데이터가 출력 전체를 채울 만큼인지 스케줄 전에 확인합니다.
                    if (channelCount != channel)
                        throw new InvalidOperationException($"FMOD returned {channelCount} channels, but the clip has {channel} channels.");

                    int bytesPerSample = bits / 8;

                    // ReSharper disable AccessToDisposedClosure
                    if (source.Length < (long)output.Length * bytesPerSample)
                        throw new InvalidOperationException($"FMOD returned {source.Length} bytes, but {(long)output.Length * bytesPerSample} bytes are required.");
                    // ReSharper restore AccessToDisposedClosure

                    fixed (byte* pointer = source)
                    {
                        int chunkCount = ((sampleCount + planarChunkSize) - 1) / planarChunkSize;
                        JobHandle handle = pcmFormat!.Value switch
                        {
                            // ReSharper disable AccessToDisposedClosure
                            PCMFormat.PCM8 => new PCM8PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM16 => new PCM16PlanarJob { source = (short*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM24 => new PCM24PlanarJob { source = pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.PCM32 => new PCM32PlanarJob { source = (int*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            PCMFormat.Float => new FloatPlanarJob { source = (float*)pointer, output = output, sampleCount = sampleCount, channelCount = channelCount }.Schedule(chunkCount, 1),
                            _ => throw new InvalidOperationException($"FMOD returned unsupported sample format {pcmFormat}.")
                            // ReSharper restore AccessToDisposedClosure
                        };

                        // FMOD lock이 풀리기 전에 원본 포인터 사용을 끝내야 합니다.
                        handle.Complete();
                    }
                });
            }
            catch
            {
                output.Dispose();
                throw;
            }

            return output;
        }

        /*
         * 인터리브(L R L R ...)를 planar(L L ... R R ...)로 바꾸는 것은 전치이므로 읽기와 쓰기 중 한쪽은 반드시 간격이 생깁니다.
         * 모든 포맷의 잡은 다음 구조를 공유합니다.
         * - Execute 하나가 샘플 구간 하나를 맡습니다. 샘플 루프가 잡 디스패처가 아닌 잡 코드 안에 있어야 Burst가 그 루프를 벡터화할 수 있습니다.
         * - 채널을 바깥 루프에 두어 쓰기를 연속으로 만듭니다. 채널마다 같은 원본 구간을 다시 읽지만 구간이 작아 캐시에서 읽힙니다.
         * - 스테레오는 읽기 간격이 상수 2가 되도록 따로 처리합니다. 간격이 상수여야 연속으로 읽은 뒤 셔플로 나누는 벡터 코드가 나오며,
         *   런타임 간격(channelCount)으로는 벡터화되지 않습니다. 대부분의 클립이 스테레오이므로 이 경로 하나만 둡니다.
         *   이 경로는 인덱스를 곱하지 않고 포인터를 전진시킵니다. 32비트 곱(i * 2)은 넘칠 수 있어 Burst가 연속 접근임을 증명하지 못하고
         *   레인마다 따로 읽는 코드를 만들지만, 64비트 포인터 증가는 그런 문제가 없습니다.
         */
        const int planarChunkSize = 16384;
    }
}
