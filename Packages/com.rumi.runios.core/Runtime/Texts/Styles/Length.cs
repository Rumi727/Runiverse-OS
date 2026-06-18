#nullable enable
using System.Globalization;

namespace RuniOS.Texts.Styles
{
    /// <summary>
    /// Represents a numeric text length with an associated unit.<br/>
    /// 단위가 연결된 숫자 텍스트 길이를 나타냅니다.
    /// </summary>
    /// <param name="value">
    /// The numeric length value.<br/>
    /// 숫자 길이 값입니다.
    /// </param>
    /// <param name="unit">
    /// The unit used by the length value.<br/>
    /// 길이 값이 사용하는 단위입니다.
    /// </param>
    [Serializable]
    public struct Length(float value, LengthUnit unit = LengthUnit.Pixels)
    {
        /// <summary>
        /// Gets or sets the numeric length value.<br/>
        /// 숫자 길이 값을 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public float value { get; set; } = value;

        /// <summary>
        /// Gets or sets the unit used by this length.<br/>
        /// 이 길이가 사용하는 단위를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public LengthUnit unit { get; set; } = unit;

        /// <summary>
        /// Creates a length measured in pixels.<br/>
        /// 픽셀 단위의 길이를 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The length value in pixels.<br/>
        /// 픽셀 단위의 길이 값입니다.
        /// </param>
        /// <returns>
        /// A pixel-based length.<br/>
        /// 픽셀 기반 길이를 반환합니다.
        /// </returns>
        public static Length Pixels(float value) => new Length(value);

        /// <summary>
        /// Creates a length measured relative to the current font size.<br/>
        /// 현재 글꼴 크기에 상대적인 길이를 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The font-relative length value.<br/>
        /// 글꼴 기준 상대 길이 값입니다.
        /// </param>
        /// <returns>
        /// A font-relative length.<br/>
        /// 글꼴 기준 상대 길이를 반환합니다.
        /// </returns>
        public static Length Font(float value) => new Length(value, LengthUnit.Font);

        /// <summary>
        /// Creates a length measured as a percentage.<br/>
        /// 백분율 단위의 길이를 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The percentage length value.<br/>
        /// 백분율 길이 값입니다.
        /// </param>
        /// <returns>
        /// A percentage-based length.<br/>
        /// 백분율 기반 길이를 반환합니다.
        /// </returns>
        public static Length Percent(float value) => new Length(value, LengthUnit.Percent);

        /// <summary>
        /// Converts this length to the rich-text representation for its unit.<br/>
        /// 이 길이를 단위에 맞는 rich text 표현으로 변환합니다.
        /// </summary>
        /// <returns>
        /// The invariant-culture string representation of this length.<br/>
        /// 이 길이의 고정 culture 문자열 표현을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="unit"/> is not a defined <see cref="LengthUnit"/> value.<br/>
        /// <see cref="unit"/>이 정의된 <see cref="LengthUnit"/> 값이 아닌 경우 발생합니다.
        /// </exception>
        public override string ToString() => unit switch
        {
            LengthUnit.Pixels => value.ToString(CultureInfo.InvariantCulture),
            LengthUnit.Font => value + "em",
            LengthUnit.Percent => value + "%",
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

        public static implicit operator Length(float value) => Pixels(value);
    }
}
