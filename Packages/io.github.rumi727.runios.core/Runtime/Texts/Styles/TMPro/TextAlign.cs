#nullable enable
namespace RuniOS.Texts.Styles.TMPro
{
    /// <summary>
    /// Specifies horizontal text alignment.<br/>
    /// 가로 텍스트 정렬 방식을 지정합니다.
    /// </summary>
    public enum TextAlign
    {
        /// <summary>
        /// Aligns text to the left edge.<br/>
        /// 텍스트를 왼쪽 가장자리에 맞춥니다.
        /// </summary>
        Left,

        /// <summary>
        /// Aligns text to the center.<br/>
        /// 텍스트를 가운데에 맞춥니다.
        /// </summary>
        Center,

        /// <summary>
        /// Aligns text to the right edge.<br/>
        /// 텍스트를 오른쪽 가장자리에 맞춥니다.
        /// </summary>
        Right,

        /// <summary>
        /// Justifies text across the available width.<br/>
        /// 사용 가능한 너비에 맞게 텍스트를 양쪽 정렬합니다.
        /// </summary>
        Justified,

        /// <summary>
        /// Flushes text according to renderer-specific alignment behavior.<br/>
        /// 렌더러별 정렬 동작에 따라 텍스트를 flush 정렬합니다.
        /// </summary>
        Flush
    }
}
