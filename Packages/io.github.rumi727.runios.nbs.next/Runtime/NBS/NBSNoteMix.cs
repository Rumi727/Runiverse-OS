#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the volume, stereo position, and fine pitch that Note Block Studio uses to play a note.<br/>
    /// Note Block Studio가 노트를 재생할 때 사용하는 음량, 스테레오 위치, 미세 피치를 저장합니다.
    /// </summary>
    /// <param name="volume">
    /// The linear gain where <c>1</c> is the original volume.<br/>
    /// <c>1</c>이 원래 음량인 선형 게인입니다.
    /// </param>
    /// <param name="panning">
    /// The stereo position from <c>-1</c> (left) to <c>1</c> (right). Values outside the range can occur.<br/>
    /// <c>-1</c>(왼쪽)부터 <c>1</c>(오른쪽)까지의 스테레오 위치입니다. 범위를 벗어난 값이 나올 수 있습니다.
    /// </param>
    /// <param name="pitch">
    /// The fine pitch in cents.<br/>
    /// 센트 단위의 미세 피치입니다.
    /// </param>
    public readonly record struct NBSNoteMix(float volume, float panning, float pitch);
}
