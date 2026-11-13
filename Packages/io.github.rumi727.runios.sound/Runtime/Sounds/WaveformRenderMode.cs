#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// How <see cref="WaveformRenderer"/> draws a zoomed-out waveform, where one pixel covers two or more samples.<br/>
    /// 픽셀 하나가 샘플 2개 이상을 덮는 축소 상태의 파형을 <see cref="WaveformRenderer"/>가 그리는 방식입니다.
    /// </summary>
    /// <remarks>
    /// When one pixel covers one sample or fewer, both modes draw the waveform as a line.<br/>
    /// 픽셀 하나가 샘플 1개 이하를 덮으면 두 방식 모두 파형을 선으로 그립니다.
    /// </remarks>
    public enum WaveformRenderMode
    {
        /// <summary>
        /// Shades each height by how much of the pixel's time the sound spends there, so a lone instantaneous peak is faint and dense sound is solid.<br/>
        /// 각 높이를 픽셀 시간 중 소리가 그 높이에 머문 비율로 칠하므로, 홀로 튄 순간 피크는 흐리고 몰린 소리는 진하게 보입니다.
        /// </summary>
        Density,

        /// <summary>
        /// Fills between outlines that connect each pixel's minimum and maximum, so every peak is drawn at full strength.<br/>
        /// 픽셀마다의 최솟값과 최댓값을 이은 외곽선 사이를 채우므로, 모든 피크를 온전한 진하기로 그립니다.
        /// </summary>
        Peak
    }
}
