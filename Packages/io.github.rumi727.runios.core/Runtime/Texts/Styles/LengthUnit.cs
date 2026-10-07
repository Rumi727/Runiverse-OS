#nullable enable
namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Specifies the unit used by a text length value.<br/>
    /// 텍스트 길이 값이 사용하는 단위를 지정합니다.
    /// </summary>
    [Serializable]
    public enum LengthUnit
    {
        /// <summary>
        /// The value is measured in pixels.<br/>
        /// 값이 픽셀 단위로 측정됩니다.
        /// </summary>
        Pixels,

        /// <summary>
        /// The value is measured relative to the current font size.<br/>
        /// 값이 현재 글꼴 크기에 상대적으로 측정됩니다.
        /// </summary>
        Font,

        /// <summary>
        /// The value is measured as a percentage.<br/>
        /// 값이 백분율 단위로 측정됩니다.
        /// </summary>
        Percent
    }
}
